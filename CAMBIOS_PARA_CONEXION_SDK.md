# Cambios para la conexión de los SDK de eyetrackers

Registro de los cambios hechos en GazeStream (rama `dev`, noviembre 2026) para que funcionen los adaptadores de Joaco (aSee), Skyle, Alea IntelliGaze y Tobii, y del estado de cada uno probado con hardware.

## Estado por dispositivo

| Dispositivo | Adaptador | Estado | Probado con hardware |
|---|---|---|---|
| **Skyle** | `GazeDeviceSkyle` | ✅ Funciona: conexión, mirada y calibración | Sí. Firmware v1.8.2 |
| **Joaco (aSee A8)** | `GazeDeviceA11` | ⚠️ Conecta con su propio SDK (`sdk/joaco/pid_29D0`), elegido automáticamente por PID, y entrega datos de ojos; falta calibrar | Sí. Ver [Joaco](#joaco-asee) |
| **Alea IntelliGaze** | `GazeDeviceIntelligaze` | ⚠️ Compila; falta probar | No: no había cámara ni servidor IntelliGaze |
| **Tobii Eye Tracker 4C** | `GazeDeviceTobiiInteraction` | 🚫 **Obsoleto**, sin soporte por ahora: no conecta porque falta su runtime | Sí. Ver [4C](#prueba-con-tobii-eye-tracker-4c-obsoleto) |
| **Tobii Eye Tracker 5** | `GazeDeviceTobiiInteraction` | ✅ El SDK conecta y entrega mirada válida, presencia y posición de los ojos (requiere Tobii Experience) | Sí. Ver [Tobii](#tobii-interaction-library) |

En el menú de bandeja, **Sistema** → combo de eyetracker, se elige el dispositivo. La opción "Otro" ahora se muestra como "Tobii (Interaction Library)"; el valor guardado en la configuración no cambió.

---

## Cambios generales (`GazeManager`)

- `IsThisAJoacoDevice` devolvía `true` para cualquier dispositivo. Ahora compara con el Joaco real.
- Los filtros (outliers e interpolación) se aplican a todos los dispositivos. Antes solo funcionaban por el error anterior.
- Mientras `IsCalibrating` o `App.IsUpdating`, el bucle giraba sin pausa y ocupaba un núcleo entero. Ahora espera 100 ms.
- `Initialize` se ejecuta dentro de un try/catch: si falta una DLL o un SDK lanza `DllNotFoundException`, `EntryPointNotFoundException` o `BadImageFormatException`, la búsqueda de dispositivos sigue funcionando.
- `CONNECTION_SUCCESS` por WebSocket (evento `OnEyetrackerConnected`) se envía para cualquier dispositivo, no solo para el Joaco.
- `DisconnectDevice` primero pone `GazeDevice` en null y después desconecta, para que no queden referencias a medio liberar.
- La calibración dinámica del Joaco calculaba el mejor preset y lo descartaba. Ahora lo aplica cuando es distinto del actual.
- `RequestCalibration` abre la calibración del Joaco sin conexión solo si el dispositivo elegido es Joaco.
- `TrayService`: el menú **Calibración** se cerraba con error si no había dispositivo conectado. Ahora, sin conexión, abre la ventana del dispositivo elegido (Skyle o Joaco).

---

## Skyle

Archivos: `Eyetracker/GazeDeviceSkyle.cs`, `Eyetracker/Skyle/SkyleClient.cs`, `Windows/SkyleCalibrationWindow.xaml.cs`.

### Causa de que no calibrara

GazeStream llamaba a `skyle_client_start_calibration` con `PointsCount = 0` y `Points = null`. El Skyle responde `CalibrationFinished` a los ~30 ms sin enviar ningún punto, así que la ventana no mostraba nada.

Se comprobó con el dispositivo:
- Sin lista de puntos: `start_calibration` devuelve `Ok` y llega `finished` de inmediato.
- Con los índices `0..8`: llega el primer punto en (77,77).
- Con el índice `4`: el punto aparece en (960,540). La grilla es de 3×3 en píxeles físicos; el 0 está arriba a la izquierda y el 4 en el centro.

### Cambios

- **Puntos de calibración.** `SkyleClient.StartCalibration(byte[] indices)` envía la lista de índices. La memoria de esa lista se mantiene viva hasta la siguiente calibración o el `Dispose`. Correspondencia con las opciones de la app:

  | Opción | Índices |
  |---|---|
  | 3 puntos | 0, 4, 8 |
  | 5 puntos | 0, 2, 4, 6, 8 |
  | 9 puntos | 0 a 8 |

  Las listas de 3 y 5 puntos son una elección propia, no están confirmadas por el fabricante. La ventana igual dibuja cada punto en la coordenada que devuelve el Skyle.
- **Recolección sin bloquear.** `OnCalibrationPoint` hacía `Thread.Sleep(2 s)` en el hilo de I/O de skylelib, lo que frenaba todos los mensajes del dispositivo. Ahora espera 1,5 s con `Task.Delay` y después llama a `skyle_client_collect_calibration_points`.
- **`collect_calibration_points_at`.** skylelib.dll no exporta esta función; llamarla lanza `EntryPointNotFoundException`. Se eliminó `CollectCalibrationPointAt`.
- **Fin de la calibración.** Se terminaba al primer progreso del último índice. Ahora se espera `CalibrationFinished`. También se manejan `CalibrationAborted` y un tiempo máximo de 15 s por punto.
- **Bucle de mirada.** Cancelar con Escape o cerrar la ventana no reanudaba el bucle de `GazeManager`, que se detiene con `OnCalibrationStart`, y la mirada quedaba frenada. Ahora se emite `OnCalibrationCancel`, o `OnCalibrationFinished` al terminar o fallar.
- **WebSocket.** `StartCalibration*` no hacía nada porque la suscripción a `OnStartCalibrationCommand` estaba comentada. Ahora abre la ventana, calibra, responde `CALIBRATION_SUCCESS` o `CALIBRATION_FAILED` y cierra la ventana.
- **Skyle X abierto.** Si Skyle X está abierto, es dueño del USB y GazeStream funciona como cliente del link local (`IsLocalLink`). En ese caso la calibración se delega a Skyle X con `StartHostCalibration`.
- **Conexión.**
  - `IsConnected` era siempre `true`. Ahora `Initialize` espera hasta 5 s a que el enlace llegue a `LinkSynced` o a que lleguen muestras.
  - Si el enlace se pierde más de 5 s, se informa la desconexión.
  - Se crea un `SkyleClient` nuevo en cada conexión, porque uno liberado no se puede reutilizar y "Reiniciar Eyetracker" dejaba el Skyle inutilizable.
  - Se comprobó que desconectar y reconectar dentro del mismo proceso funciona.
- **Datos.** Ahora se completan `RawGazePoint`, con el punto crudo del Skyle, y `UserIsPresent`, que indica si hubo una muestra válida en el último segundo. Los puntos se protegen con un lock porque llegan desde otro hilo.

---

## Joaco (aSee)

Archivos: `Eyetracker/GazeDeviceA11.cs`, `Windows/CalibrationWindow.xaml.cs`.

### Cambios en el adaptador

- **Orden de inicialización.** Antes se llamaba a `_7i_start()` sin haber inicializado el SDK, y ese primer resultado decidía si había conexión. Ahora el orden es:
  1. `_7i_init(config)`
  2. `_7i_device_connect`
  3. Callback de mirada, tamaño de pantalla, `set_data` y `PreFlashData`
  4. `_7i_start`
  5. `_7i_start_tracking(coeficiente)`

  Se revisa el resultado de cada paso. Si alguno falla, se liberan solo los pasos que ya se habían completado (`ReleaseNative`).
- **Rutas de configuración.** Las rutas `./config` eran relativas al directorio de trabajo y fallaban cuando la app arranca con Windows. Ahora se usa `AppContext.BaseDirectory\config`.
- **`PreFlashData`.** Dejaba abierto el archivo que creaba. Ahora lo lee con `File.ReadAllBytes` y omite este paso si el archivo no existe.
- **Coeficiente de calibración.** Se ajusta a 2048 bytes y `HasValidCalibration` refleja el resultado real de `_7i_start_tracking`.
- **Datos entre hilos.** El callback nativo y el bucle comparten los datos con un lock. Una muestra no se reenvía como nueva, y los ojos se muestran aunque todavía no haya calibración.
- **Ventana de calibración.**
  - Se revisa el error de cada punto, con hasta 3 intentos.
  - No se calcula la calibración si se canceló.
  - Se emite `CALIBRATION_FAILED`, que antes nunca se enviaba.

### Por qué no conecta con la cámara actual

Diagnóstico hecho con la cámara conectada. Los logs del SDK están en `bin\x64\Debug\net8.0-windows7.0\config\logs\`, que guarda solo los últimos 5.

- **La cámara.** Windows la detecta como **aSee A8**, USB `VID_3014 & PID_29D0` (PID 10704).
- **Lo que espera el SDK.** El SDK del repo (`USERSDK.dll`, versión `A8_1106.202608.03.14.39`) tiene incrustada una configuración con `"vid": 12308, "pid": 11473`, es decir **PID 0x2CD1**, otro modelo. Busca ese PID, no lo encuentra y `_7i_start` devuelve **-2**:
  ```
  CameraObj::Init error, vid:12308, pid:11473, ret:-2
  ```
- **Con el PID corregido.** El SDK lee `config\SDKConfig.json` si existe. Con un `SDKConfig.json` igual al incrustado pero con `"pid": 10704`, el SDK **abre la cámara**, pero la **rechaza en la verificación de licencia** y `_7i_start` devuelve **-3**:
  ```
  CheckCameraAuthorization() chipId ==
  UserInterfaceApi::StartCamera chipid:, is ellegal device
  ```
  Ese archivo **no** quedó en el proyecto. La verificación de licencia no se intentó saltear.
- **La versión instalada.** La versión oficial instalada (`%LOCALAPPDATA%\GazeStream\current`) tiene exactamente las mismas DLL que el repo y falla igual. No es un problema de estos cambios.
- **El SDK viejo.** El SDK de `C:\Program Files\GazeStream` (GazeStream 1.0) usa una API anterior: no tiene `_7i_init`, se arranca directo con `_7i_start` y necesita un `GearVrCfg.ini`. Sin ese archivo devuelve -2.

#### SDK alternativo que sí conecta (`SDKs auxiliares/sdk/joaco_a11`)

- **Origen.** Son las DLL de otro proyecto, idénticas a las del GazeStream 1.0 de `C:\Program Files\GazeStream`. Se identifican como `11.0.2.006-10704-100033-smooth-20240904_1024_COMPUTECHANGE`; **10704 es el PID de la aSee A8** (`0x29D0`).
- **API distinta.** No tiene `_7i_init`, y **`_7i_start(const char* config_path)` recibe la ruta de la carpeta de configuración**. La primera prueba de este SDK falló (-2, "ruta inválida") porque se llamó a `_7i_start` sin argumento, como lo declara el `aSeeTracker.cs` del proyecto. Las firmas correctas están en `SDKs auxiliares/joaco_a11/aSeeTracker.cs`.
- **Resultado con la cámara conectada.**
  - `_7i_start(config)` → 0
  - `_7i_set_gaze_callback` → 0
  - `_7i_start_tracking` con coeficiente vacío → 0
  - Callbacks de mirada: unos 62 por segundo, con pupilas detectadas en los dos ojos en el 97% de las muestras. El punto de mirada sale (0,0) porque no hay calibración.
- **Observaciones.**
  - Sin cámara, `_7i_start` devuelve **-2112**.
  - `_7i_set_screen_size(344, 194)` devolvió -1; falta ver qué parámetros espera.
  - El SDK busca `EyeTracking.ini`, `GazeCfg.ini` y `GearVrCfg.ini` en la carpeta de configuración. No están, y aun así arranca.
- **Origen de las calibraciones de fábrica.** Las calibraciones de fábrica del código (`GetDefaultCalibrationBuff*`) contienen el texto `20240904_1024_COMPUTECHANGE`, el mismo de la versión de este SDK. GazeStream se desarrolló originalmente con él.

#### Dos SDK del Joaco en paralelo (selección por modelo)

Los dos SDK usan los mismos nombres de DLL, y Windows no puede tener cargados dos módulos con el mismo nombre en un proceso. Descargar uno para probar el otro tampoco es seguro, porque deja hilos y memoria nativa del SDK. Por eso no se prueba "uno y si falla el otro": **se detecta el modelo por su PID USB y se carga solo el SDK que corresponde.**

- **Archivos:**

  | Carpeta | Modelo | API |
  |---|---|---|
  | `sdk/joaco/pid_29D0/` | aSee A8, SDK `11.0.2.006` | Legacy: `_7i_start(config)` |
  | `sdk/joaco/pid_2CD1/` | SDK `A8_1106.202608` (el que estaba en la raíz) | Current: `_7i_init` → `_7i_device_connect` → `_7i_start()` |

  El `.csproj` copia `sdk\joaco\**\*.dll` a la salida y excluye `SDKs auxiliares\**` de la compilación.
- **`Eyetracker/JoacoSdkLoader.cs`:**
  - Enumera los USB presentes con `CM_Get_Device_ID_List` (VID `0x3014`) y elige el perfil por PID. Un PID desconocido usa el primer perfil de la lista.
  - Carga `aSeeX.dll` de esa carpeta con `LoadLibraryEx(..., LOAD_WITH_ALTERED_SEARCH_PATH)`, para que `USERSDK.dll` y las demás dependencias salgan de la misma carpeta.
  - Carga un solo SDK por proceso. Si después se conecta un modelo que usa el otro SDK, informa que hay que reiniciar GazeStream.
- **`Eyetracker/NativeLibraryResolver.cs`:** .NET admite un solo `DllImportResolver` por ensamblado, así que el Skyle (`skylelib`) y el Joaco (`aSeeX.dll`) se registran en uno común.
- **`aSeeTracker.cs`:**
  - Se agregó `_7i_start_with_config(string)`, con el mismo punto de entrada `_7i_start`, para el SDK Legacy.
  - El constructor estático registra el resolvedor antes de la primera llamada.
- **`GazeDeviceA11.Initialize`:**
  - Elige y carga el SDK, y después sigue `StartLegacySdk()` o `StartCurrentSdk()` según el caso.
  - En Legacy no se envía `Configs.dat`, porque es del SDK Current.
  - Sin cámara conectada no carga ningún SDK.
- **`EyesWindow`:** sin un SDK cargado no registra el callback de imagen.
- **Probado con la aSee A8:**
  - detecta `0x29D0` y elige `pid_29D0`;
  - todas las DLL se cargan desde esa carpeta;
  - rechaza cargar el otro SDK;
  - dos ciclos de arranque, tracking y parada funcionan, con unos 62 callbacks por segundo.
- **Falta:**
  - calibrar con la ventana de GazeStream y comprobar la mirada;
  - probar el SDK `pid_2CD1` con una cámara de ese modelo.

#### Calibraciones incompatibles entre los dos SDK (cierre de la app)

- **Síntoma.** En la 1.1.0, al abrir GazeStream con la A8 se encendía la cámara y la app se cerraba. En el Visor de eventos: `AccessViolationException` en `ASeeTracker._7i_start_tracking` ← `GazeDeviceA11.ApplyCoefficient` ← `SetCurrentUserCalibration`. Esa excepción no se puede atrapar en .NET y cierra el proceso.
- **Causa.** Cada coeficiente de calibración lleva el texto de versión del SDK que lo generó:
  - SDK de la A8 (`pid_29D0`): `20240904_1024_COMPUTECHANGE`. Así son las calibraciones de fábrica del código y las 14 primeras guardadas.
  - SDK `pid_2CD1`: `20260602_2048_F300`.

  Se calibró con un Joaco del otro modelo. Ese coeficiente quedó como `LastCalibrationBuff` y como preset, que son compartidos entre modelos, y el SDK de la A8 se cayó al recibirlo. Se reprodujo fuera de GazeStream: las 14 calibraciones de la A8 funcionan y la del otro SDK tira el proceso.
- **Arreglo.**
  - Cada perfil de `JoacoSdkLoader` declara la versión de calibración que acepta, y `JoacoSdkLoader.IsCalibrationCompatible` la compara con la que lleva el coeficiente.
  - `GazeDeviceA11.ApplyCoefficient`, por donde pasan todas las calibraciones (última, presets, de fábrica y la recién calculada), rechaza las de otro SDK y las vacías.
  - `FindBestPresetForThisDevice`, que también usa la calibración dinámica, solo considera presets del SDK cargado.
  - Al arrancar se usa la última calibración si es compatible; si no, el mejor preset compatible; si no, la de fábrica. Con el SDK `pid_2CD1` la de fábrica se rechaza, porque es del otro SDK, y hay que calibrar.
- **Probado.** Con la A8 y la última calibración guardada del otro SDK, la app sigue abierta, con el WebSocket enviando datos a unos 60 Hz.

**Para resolverlo con el SDK actual del proyecto** hay que pedirle al fabricante (aSee / 7invensun) el SDK o el `SDKConfig.json` más la licencia correspondientes a la **aSee A8 con PID 0x29D0**. Otra opción es copiar la carpeta `config` y las DLL (`USERSDK.dll`, `aSeeX.dll`, `FdDetect.dll`, `GazeAfterProcess.dll`) de una PC donde esta cámara sí funcione.

---

## Alea IntelliGaze

Archivo: `Eyetracker/GazeDeviceIntelligaze.cs` (reescrito).

- **Mutex.** Se creaba el mutex `Local\AleaIntelliGaze30` en cada ciclo, lo que puede hacer creer a IntelliGaze que ya hay otra instancia abierta. Ahora solo se comprueba si existe (`Mutex.TryOpenExisting`).
- **`Open`.** Se reintentaba sin límite bloqueando un hilo. Ahora son 3 intentos de 1 s.
- **Callbacks.** Los delegados nativos se guardan en campos para que el GC no los libere.
- **Mirada.**
  - Se toma de `RawData.intelliGazeX/Y` cuando el servidor la entrega.
  - Si no, se usan las fijaciones, igual que antes. Si el modo raw no está disponible (por ejemplo, por licencia), se pasa a modo solo eventos.
- **Presencia.** `UserIsPresent` era siempre `true`. Ahora indica si hubo una muestra válida en el último segundo.
- **Calibración.**
  - Opciones de puntos: 0 → 1 punto, 1 → 5 puntos, 2 → 9 puntos.
  - Emite inicio y fin por WebSocket, con un tiempo máximo de 3 minutos.
  - Pendiente: no se sabe qué significa el valor `data` de `CalibrationDone`, así que por ahora siempre se informa éxito.
- **`EtApi.dll`.** El `HintPath` en `GazeStream.csproj` apuntaba fuera del repo (`..\..\NET_GazeStream\...`). Ahora es `EtApi.dll`.

## Tobii (Interaction Library)

Archivo: `Eyetracker/GazeDeviceTobiiInteraction.cs`.

- **Fuga de memoria.** Se creaba una `InteractionLib` nueva cada 500 ms sin liberarla cuando no había cámara. Ahora se libera si falla la conexión.
- **Detección de conexión.** Las capacidades del SDK solo se actualizan al llamar a `Update()`. Ahora se llama mientras se espera, hasta 2 s, y cada 1 s se comprueba que el stream siga disponible.
- **Hilos.** La librería no es thread-safe: `Update`, las consultas y `Dispose` se serializan con un lock.
- **Calibración.** Esta librería no puede calibrar, así que la calibración responde `CALIBRATION_FAILED`. Se calibra desde Tobii Experience.
### Prueba con Tobii Eye Tracker 5

- **Dispositivo.** Windows lo detecta como USB `VID_2104 & PID_0313`, número de serie `IS5FF-...`. La interfaz "EyeChip" figura sin driver (Código 28) y no hay software de Tobii instalado. Aun así, los SDK del repo lo ven:
  - **Stream Engine 4.6** (`tobii_stream_engine.dll`): enumera el dispositivo `tobii-prp://IS5FF-100203529844`.
  - **Interaction Library**: el stream de mirada queda `Enabled` y llegan unas 33 muestras por segundo.
- **Sin software de Tobii.** Todas las muestras llegaron inválidas y la presencia en `Away`.
- **Instalación.** Al conectar la cámara, Windows instaló automáticamente el driver, **Tobii Experience 4.72** y la app Tobii Eye Tracking. El servicio **Tobii Runtime Service** (`TobiiIS5LEYETRACKER5`) quedó detenido después de instalarse. Mientras está detenido, los SDK no ven la cámara: 0 dispositivos y stream `Unavailable`. Se arranca reconectando el USB, reiniciando, o con `Start-Service TobiiIS5LEYETRACKER5` en una PowerShell **como administrador**.
- **Resultado con el servicio corriendo.** Stream Engine encuentra el dispositivo. Interaction Library entrega mirada **válida** a unas 33 muestras por segundo, presencia `Present` y la posición de los ojos (por ejemplo, ojo izquierdo a unos 707 mm de la cámara).
- **Calibración.** Esta librería no calibra, así que GazeStream delega en la herramienta de Tobii Experience, `C:\Program Files\Tobii\Tobii EyeX\Tobii.Configuration.exe`. Opciones que acepta: `-C` calibrar el perfil actual, `-N` perfil nuevo y calibrar, `-G` invitado, `-S` configurar pantalla, `-P` posición de ojos.
  - **`RequestCalibration`** (WebSocket o menú "Calibración"):
    - Abre la herramienta con `-C`, emite `OnCalibrationStart` y espera a que se cierre.
    - Según el código de salida emite un evento y su callback por WebSocket. Después emite siempre `OnCalibrationFinished`:

      | Código de salida | Evento | Callback por WebSocket |
      |---|---|---|
      | 0 | `OnCalibrationSuccess` | `CALIBRATION_SUCCESS` |
      | 2 | `OnCalibrationCancel` | `CALIBRATION_CANCELLED` |
      | Otro | `OnCalibrationFailed` | `CALIBRATION_FAILED` |
    - Si pasan 5 minutos sin que se cierre, informa el fallo.
  - **`ShowCameraPreview(true)`**: abre la guía de posición de ojos (`-P`). Sin probar.
  - **Prueba con Eye Tracker 5:** una calibración completa cerró a los 49 s con código **0**; una cancelada por el usuario cerró con código **2**.

- **Runtime que se apaga solo.** Si en la app de Tobii se elige **Salir**, se guarda `"SystemQuit": "True"` en `%LOCALAPPDATA%\Tobii\tobii_system_configuration`. Con eso, Tobii Service detiene el runtime en cada inicio de sesión (`Stop software, reason: ExitFlag` en `C:\ProgramData\Tobii\Tobii Interaction\ServiceLog.txt`) y GazeStream deja de ver la cámara. Se resuelve abriendo la app de Tobii y activando el seguimiento ocular. En los equipos de usuarios no se debe cerrar la app de Tobii con "Salir".

### Prueba con Tobii Eye Tracker 4C (obsoleto)

> **Obsoleto.** Ya no se fabrica y la empresa tiene pocas unidades distribuidas. Por ahora no se le da soporte: lo que sigue queda como referencia por si se retoma.

- **Dispositivo.** Windows lo detecta como USB `VID_2104 & PID_0127`, número de serie `IS404-...`. El driver USB ("EyeChip") se instala bien.
- **Resultado.** Ni Stream Engine ni Interaction Library lo ven: 0 dispositivos y stream `Unavailable`.
- **Causa.** El SDK no es el problema: es el mismo para todos los modelos de consumo y habla con el *runtime* de cada cámara. Al conectar el 4C, Windows no instaló un runtime para él; solo existe el de la Eye Tracker 5 (`eyetracker5.inf`, servicio `TobiiIS5LEYETRACKER5`), y Tobii Service solo vigila ese.
- **Si se retoma.**
  - Instalar el software de Tobii para el 4C, que incluye su runtime. Se descarga del sitio de soporte de Tobii.
  - Probar si el runtime de la ET5 y el del 4C pueden convivir en la misma PC.

- **Otros modelos.** Los otros modelos de Tobii (Pro SDK, Dynavox/PCEye con Stream Engine) necesitan su propio adaptador que cumpla `IGazeDevice`, más un valor nuevo en `Eyetracker_Device`.

---

## Hilos, cierre e instancia única

### `GazeManager` (bucles y SDK del dispositivo)
- **Antes:**
  - `Dispose` solo cancelaba el bucle de lectura. `FindGazeDeviceLoop` seguía vivo y podía inicializar un SDK durante el cierre.
  - No se esperaba a ninguno de los dos bucles, y el dispositivo se desconectaba mientras otro hilo le leía datos.
  - Inicializar, leer y desconectar no estaban sincronizados. Al cambiar de dispositivo, una inicialización lenta de la sesión vieja podía dejar conectado el dispositivo anterior.
  - `StartGazeDeviceUpdateLoop` no arrancaba si el bucle cancelado todavía no había terminado, así que después de una calibración corta la mirada podía quedar cortada.
  - Una excepción terminaba el bucle en silencio.
- **Ahora:**
  - Un solo `CancellationTokenSource` por sesión controla los dos bucles.
  - `deviceGate` (`SemaphoreSlim`) serializa `Initialize`, `UpdateData` y `Disconnect`.
  - Si la sesión se cancela durante `Initialize`, el dispositivo recién conectado se libera en lugar de adoptarse.
  - `DisconnectDevice()` desconecta en segundo plano y solo el dispositivo que estaba activo al llamar.
  - Cada vuelta de los bucles atrapa sus errores y sigue.
  - `Dispose` cancela, espera hasta 6 s a los bucles y después libera el SDK.
- **`OverlayInteraction.OnGazeDeviceChanged`:** pasó de `Dispatcher.Invoke` a `BeginInvoke`, porque se llama desde el bucle y podía trabar la espera del cierre.

### `App.OnExit`
- **Antes:**
  - El mutex se liberaba primero.
  - `CancelUpdate()` lanzaba `NullReferenceException` en cada cierre porque `updateCts` es null (`UpdateApp` está desactivado), y eso cortaba el resto de la limpieza.
- **Ahora:** cada paso del cierre está protegido. Orden:
  1. overlays;
  2. actualización;
  3. atajos de teclado;
  4. **GazeManager** (bucles y eyetracker);
  5. **WebSocket** (`INSTANCE_QUIT` y liberar el puerto 3000);
  6. bandeja;
  7. **al final, el mutex**.
- **WebSocket:** si el puerto 3000 está ocupado, `ws.Start()` ya no cierra la app al arrancar.

### Instancia única
- **Mutex `Global\GazeStream.TrayApp.SingleInstance`:** único por equipo, porque la cámara y el puerto son de toda la máquina. Antes era por sesión de Windows.
- **Segunda instancia:** se cierra sin error. Antes llamaba a `ReleaseMutex` sobre un mutex que no le pertenecía y lanzaba una excepción.
- **`--restart`:** espera hasta 15 s a que la instancia anterior libere el mutex. Antes dormía 1 s fijo, y si el cierre tardaba más la app quedaba sin ninguna instancia.
- **Mutex abandonado** (cierre forzado de la instancia anterior): la nueva instancia lo toma.
- **Probado con el `.exe`:**
  - una segunda instancia se cierra en 0,3 s;
  - `--restart` con la anterior abierta espera 15 s y se cierra;
  - `--restart` toma el lugar cuando la anterior termina;
  - "Salir" desde la bandeja con el Joaco conectado termina el proceso del todo y libera el puerto 3000.

## Otros

- Se pasaron a UTF-8 `GazeManager.cs`, `GazeDeviceA11.cs` y `EyesData.cs`, que estaban en Latin-1. Solo cambian los acentos de los comentarios.
- **Logs.** GazeStream no guarda un log propio: los mensajes (`Debug.WriteLine`) se ven en la ventana Output de Visual Studio o con DebugView. El SDK del Joaco sí escribe sus logs en `config\logs\`.
- **Compilación.** `dotnet build -c Debug -p:Platform=x64`. Los SDK nativos son de 64 bits.
