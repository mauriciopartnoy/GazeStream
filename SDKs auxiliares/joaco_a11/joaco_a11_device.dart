import 'dart:async';

import 'package:logging/logging.dart';

import '../../gaze_event_args.dart';
import '../eyetracker_device.dart';

final _log = Logger('JoacoA11Device');

/// Joaco A11 (aSee) por `aSeeX.dll` (funciones `_7i_*`), vía `dart:ffi`.
/// DLL en `sdk/joaco_a11/`.
///
/// Portar desde `aSeeTracker.cs` / `GazeDeviceA11.cs` de GazeStream:
/// `_7i_start("")` → `_7i_set_gaze_callback` → `_7i_set_data`
/// (`check_calib`, `detect_eyelid`, `smoothWay`) → `_7i_start_tracking(coef)`
/// con el buffer de calibración de 2048 bytes. Verificar el retorno de
/// AMBAS llamadas (GazeStream sólo verifica `_7i_start`).
///
/// El callback de mirada llega desde un hilo nativo: usar
/// `NativeCallable.listener` y mantenerlo vivo mientras el SDK lo use.
/// `recom_gaze.gaze_point` viene normalizado 0–1 → multiplicar por el tamaño
/// lógico de pantalla. El layout de `_7i_eye_data_ex_t` hay que copiarlo
/// exacto de `aSeeTracker.cs`, no inventarlo.
///
/// Calibración: `_7i_set_calibration_mode` → `_7i_start_calibration(n)` →
/// por punto (índice desde 1) `_7i_start_calibration_point` y esperar el
/// finish callback (validar `error`) → `_7i_compute_calibration` →
/// `_7i_complete_calibration` → `_7i_get_calibration_score`.
class JoacoA11Device implements EyeTrackerDevice {
  final _gazeController = StreamController<GazeEventArgs>.broadcast();

  @override
  GazeInputSource get source => GazeInputSource.joacoA11;

  @override
  String get displayName => 'Joaco A11';

  @override
  bool get isConnected => false;

  @override
  bool get supportsCalibration => true;

  @override
  Stream<GazeEventArgs> get gazeStream => _gazeController.stream;

  @override
  Future<bool> connect() async {
    _log.warning('Joaco A11 todavía no implementado (faltan bindings FFI de aSeeX)');
    return false;
  }

  @override
  Future<void> disconnect() async {}

  @override
  Future<bool> calibrate({int points = 9}) async => false;
}
