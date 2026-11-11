# DLL de Joaco A11 (x64)

Validadas el 14/10/2026 (imports con `objdump -p` y carga real con `LoadLibraryEx`
en proceso x64, sin llamar funciones):

```text
aSeeX.dll  (API _7i_*, la que se llama por dart:ffi)
└─ USERSDK.dll  (aSeeX la importa por ordinal)
   ├─ GazeAfterProcess.dll
   ├─ FdDetect.dll  → VCOMP140.DLL (sistema)
   └─ nlopt.dll
```

`libnlopt-0.dll` y `opencv_world320.dll` NO hacen falta: ninguna DLL de la
cadena las importa.

Del sistema: runtime de Visual C++ 2015–2022 x64 (`MSVCP140`, `VCRUNTIME140`,
`VCRUNTIME140_1`, `VCOMP140`). USERSDK habla con el dispositivo por
`HID.DLL`/`SETUPAPI.dll`, sin driver propio en esta cadena.

El contrato nativo (firmas y structs) está en `../../joaco_a11/aSeeTracker.cs`:
no hay header `.h`, así que ese archivo es la única referencia.
