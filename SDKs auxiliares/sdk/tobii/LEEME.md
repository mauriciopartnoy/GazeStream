# DLL de Tobii (x64)

Copiar acá, desde la carpeta de salida de GazeStream:

- `tobii_interaction_lib_c.dll` — API C (`IL_*`) que se llama por `dart:ffi`.
- `tobii_interaction_lib.dll` — dependencia de la anterior.
- `tobii_stream_engine.dll` — dependencia de la anterior; también tiene `tobii_calibration_*`.

Validadas el 14/10/2026: x64, la cadena `_c → interaction_lib → stream_engine`
carga completa con `LoadLibraryEx` y exporta los `IL_*` esperados. Del
sistema: runtime de Visual C++ 2015–2022 x64. El dispositivo además necesita el
servicio de Tobii instalado (Stream Engine se conecta a él; la carga de la DLL
no lo verifica).

No hay headers C: las firmas se extrajeron del wrapper .NET v1.0.2.1 y están
en `../../tobii/INTERACTION_LIB_C_API.md`. Si se cambian estas DLL por otra
versión, hay que volver a extraerlas.

No hacen falta: `tobii_interaction_lib_cs.dll` y `tobii_interaction_lib_wpf.dll`
(son wrappers .NET), ni `TobiiGazeCore64.dll`.
