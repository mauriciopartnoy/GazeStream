//using System.Collections;
//using System;
//using System.Runtime.InteropServices;
//using System.IO;
//using System.Numerics;
//using System.Diagnostics;
//using JoacoDesktop.Utilities.Events;
//using JoacoDesktop.Utilities.Save;
//using JoacoDesktop.AppData;
//public class CalibrationInvensun
//{
//    public static int CalibrationPointProgress { get; private set; }
//    public static float CalibrationPointProgress01 { get; private set; }

//    public string UserID { get; set; } = "Default";
//    public const string LAST_CALIBRATION_KEY = "LastCalibrationKey";
//    public const string LAST_CALIBRATION_EYES_KEY = "LastCalibrationEyesKey";
//    public const string LAST_CALIBRATION_POINTS_KEY = "LastCalibrationPointsKey";
//    public const string LAST_CALIBRATION_FILTER_KEY = "LastCalibrationFilterKey";

//    Vector2[] points3 = {
//                new Vector2(0.05f, 0.95f),
//                new Vector2(0.95f, 0.95f),
//                new Vector2(0.50f, 0.05f)};

//    Vector2[] points5 = {
//                new Vector2(0.50f, 0.50f),
//                new Vector2(0.05f, 0.95f),
//                new Vector2(0.95f, 0.95f),
//                new Vector2(0.05f, 0.05f),
//                new Vector2(0.95f, 0.05f)};

//    Vector2[] points9 = {
//                new Vector2(0.50f, 0.50f),
//                new Vector2(0.95f, 0.50f),
//                new Vector2(0.05f, 0.95f),
//                new Vector2(0.50f, 0.95f),
//                new Vector2(0.95f, 0.95f),
//                new Vector2(0.05f, 0.05f),
//                new Vector2(0.50f, 0.05f),
//                new Vector2(0.95f, 0.05f),
//                new Vector2(0.05f, 0.50f)};

//    bool isCalibrating;
//    bool calibrationComplete;
//    static bool pointComplete;

//    public static ASeeTracker.processCallback processCB = new ASeeTracker.processCallback(process_callback);
//    public static ASeeTracker.finishCallback finishCB = new ASeeTracker.finishCallback(finish_callback);
//    public static ASeeTracker.gazeCallback gazeCB = new ASeeTracker.gazeCallback(OnGazeCallback);
//    public static _7i_eye_data_ex_t eyesData;

//    static GameObject currentPoint;

//    public static void process_callback(int index, int percent, IntPtr context)
//    {
//        CalibrationPointProgress = percent;
//        CalibrationPointProgress01 = (float)percent / 100f;
//        Debug.WriteLine($"process: {index},{percent}");
//    }

//    public static void OnGazeCallback(ref _7i_eye_data_ex_t eyes, IntPtr context)
//    {
//        eyesData = eyes;

//    }

//    public static void finish_callback(int index, int error, IntPtr context)
//    {
//        CalibrationPointProgress01 = 0;
//        CalibrationPointProgress = 0;
//        Debug.WriteLine($"finish: {index}, {error}");
//        pointComplete = true;
//    }

//    private void Start()
//    {
//        LoadToggleOptions();
//    }

//    private void LoadToggleOptions()
//    {
//        eyesToggleGroup.SetToggleOptionWithoutNotify(SaveManager.GetSystemSetting(LAST_CALIBRATION_EYES_KEY, 0));
//        calibrationPointsToggleGroup.SetToggleOptionWithoutNotify(SaveManager.GetSystemSetting(LAST_CALIBRATION_POINTS_KEY, 0));
//        filterToggleGroup.SetToggleOptionWithoutNotify(GetSmoothOption());
//    }

//    private void Update()
//    {

//        if (Input.GetKeyDown(KeyCode.Escape))
//        {
//            CancelCalibration();
//        }
//    }

//    public void CancelCalibration()
//    {
//        if (!isCalibrating) return;
//        isCalibrating = false;
//        Cursor.visible = true;
//        fadeText.text = "";
//        CleanCalibrationPointGraphic();
//        Disconnect();
//        LoadToggleOptions();

//    }

//    private static void CleanCalibrationPointGraphic()
//    {
//        if (currentPoint != null)
//        {
//            Destroy(currentPoint.gameObject);
//        }
//    }

//    public void CalibrateUsingCurrentOptions()
//    {
//        StartCalibration(calibrationPointsToggleGroup.CurrentIndex, eyesToggleGroup.CurrentIndex);
//    }


//    void StartCalibration(int pointArrayIndex, int eyes = 0)
//    {
//        if (isCalibrating) return;
//        SaveManager.SetSystemSetting(LAST_CALIBRATION_POINTS_KEY, pointArrayIndex);
//        SaveManager.SetSystemSetting(LAST_CALIBRATION_EYES_KEY, eyes);
//        SaveManager.SaveSystemSettings();

//        calibrationPointsToggleGroup.SetToggleOptionWithoutNotify(pointArrayIndex);
//        eyesToggleGroup.SetToggleOptionWithoutNotify(eyes);

//        Debug.Log("Eyes: " + eyes);

//        Vector2[] points = GetCalibrationPointsArray(pointArrayIndex);
//        isCalibrating = true;
//        calibrationComplete = false;
//#if !UNITY_EDITOR
//        Cursor.visible = false;
//#endif
//        eyeDisplay.gameObject.SetActive(false);
//        onCalibrationStart.Invoke();

//        if (!TryConnectForCalibrationA11())
//        {
//            isCalibrating = false;
//            Cursor.visible = true;
//            Debug.Log("Could not connect to an Invensun device");
//            return;
//        }

//        StartCoroutine(CStartCalibration(points));
//    }


//    IEnumerator CStartCalibration(Vector2[] points)
//    {
//        menuManager.CloseCurrentPage();
//        while (menuManager.ActivePage != null && menuManager.ActivePage.IsBusy)
//        {
//            yield return null;
//        }

//        yield return FadeMessage("Estos son tus ojos.");
//        yield return ShowEyeDisplay();
//        yield return FadeMessage("Mira los puntos hasta hacerlos desaparecer.");
//        yield return CCalibration(points);
//    }

//    IEnumerator ShowEyeDisplay()
//    {
//        eyeDisplay.gameObject.SetActive(true);
//        bool buttonPressed = false;
//        while (!buttonPressed)
//        {
//            eyeDisplay.UpdateEyeDisplay(eyesData);
//            if (Input.anyKeyDown)
//            {
//                buttonPressed = true;
//            }
//            yield return null;
//        }
//        eyeDisplay.gameObject.SetActive(false);
//    }

//    public bool TryConnectForCalibrationA11()
//    {
//        GazeManager.I.DisconnectDevice();

//        int startResult = ASeeTracker._7i_start(AppPaths.EyetrackerConfigPath);
//        ASeeTracker._7i_set_gaze_callback(Marshal.GetFunctionPointerForDelegate(gazeCB), IntPtr.Zero);

//        Debug.WriteLine("Start result: " + startResult);
//        if (startResult == 0)
//        {
//            log.text = "";
//            return true;
//        }
//        else
//        {
//            log.text = "¡Algo salió mal! No fue posible iniciar el dispositivo Joaco.";
//            return false;
//        }
//    }

//    IEnumerator CCalibration(Vector2[] points, int eyes = 0)
//    {
//        isCalibrating = true;
//        calibrationComplete = false;

//        Acá se va a guardar el resultado de la calibración. Podemos guardar el coefficient.buf como un byte[] para usar mas tarde.
//        _7i_coefficient_t coefficient = new _7i_coefficient_t();
//        int pointIndex = 1;
//        int pointCount = points.Length;


//        Seteamos el modo antes de iniciar. 0: ambos ojos, 1: ojo izq, 2: ojo der.
//        int setModeResult = ASeeTracker._7i_set_calibration_mode(eyes);
//        Debug.Log("Set Mode result: " + setModeResult);


//        Inicio de calibración
//        int result = ASeeTracker._7i_start_calibration(pointCount);

//        for (int i = 0; isCalibrating && i < pointCount; i++)
//        {
//            pointComplete = false;

//            /*
//             Primero mostramos el gráfico. Su posición en el eje Y está invertido ya que el sistema de coordenadas
//             del Viewport de Unity tiene el origen (0,0) ABAJO-IZQ vs Invensun que lo tiene ARRIBA-IZQ.
//             El usuario tiene 2 segundos para mirar hacia el punto antes de que inicie la calibración del mismo.
//             */
//            SpawnCalibrationPointGraphic(new Vector2(points[i].x, 1f - points[i].y));
//            yield return new WaitForSeconds(2);

//            Start Point
//            _7i_point2d_t point = new _7i_point2d_t();
//            point.x = points[i].x;
//            point.y = points[i].y;
//            result = ASeeTracker._7i_start_calibration_point(pointIndex, ref point,
//            Marshal.GetFunctionPointerForDelegate(processCB), IntPtr.Zero,
//            Marshal.GetFunctionPointerForDelegate(finishCB), IntPtr.Zero);
//            Debug.Log($"_7i_start_calibration_point: {pointIndex}, {result}");


//            while (!pointComplete)
//            {
//                yield return null;
//            }

//            ++pointIndex;
//        }

//        En caso de interrupción
//        if (pointIndex != pointCount + 1)
//        {
//            result = ASeeTracker._7i_cancel_calibration();
//            Console.WriteLine("_7i_cancel_calibration: {0:D}", result);
//            CancelCalibration();
//        }

//        En caso de exito
//        int calibration_success = ASeeTracker._7i_compute_calibration(ref coefficient);
//        Debug.Log($"_7i_compute_calibration: {calibration_success}");
//        result = ASeeTracker._7i_complete_calibration();
//        Debug.Log($"_7i_complete_calibration: {result}");

//        Puntaje para la calibración de cada ojo.
//        float left_score = 0;
//        float right_scroe = 0;
//        ASeeTracker._7i_get_calibration_score(ref left_score, ref right_scroe);
//        Console.WriteLine("_7i_complete_calibration: {0:G}, {1:G}", left_score, right_scroe);

//        Limpieza de gráficos
//        if (currentPoint != null)
//        {
//            Destroy(currentPoint);
//        }

//        OnSuccess
//        if (0 == calibration_success)
//        {
//            Debug.Log("Calibration success!");
//            Disconnect();
//            yield return FadeMessage("¡La calibración fue exitosa!");

//            GameManager.I.UpdateParticipantCalibrationData(coefficient.buf, eyes, GetSmoothValueFromToggle(), pointCount);
//            GlobalEvents.OnCalibrationSuccess.Invoke(coefficient.buf);
//            SaveUserCalibration(UserID, coefficient.buf);
//            GazeManager.I.TryInitializeGazeDevice();
//            menuManager.OpenPage(mainPage);
//            onCalibrationFinish.Invoke();
//        }

//        Cleanup
//        Cursor.visible = true;
//        calibrationComplete = true;
//        isCalibrating = false;
//        LoadToggleOptions();
//    }


//    void SaveUserCalibration(string userID, byte[] buf)
//    {
//        string id = UserID;
//        if (string.IsNullOrEmpty(UserID))
//        {
//            id = "Default";
//        }
//        SaveManager.SetSystemSetting(GetUserCalibrationKey(id), buf);
//        SaveManager.SetSystemSetting(LAST_CALIBRATION_KEY, buf);
//        SaveManager.SaveSystemSettings();
//    }

//    byte[] LoadUserCalibration(string userID)
//    {
//        return SaveManager.GetSystemSetting(GetUserCalibrationKey(userID), new byte[0]);
//    }

//    string GetUserCalibrationKey(string userID)
//    {
//        return "UserCalibrationBuf_" + UserID;
//    }

//    void SpawnCalibrationPointGraphic(Vector2 pos)
//    {
//        if (currentPoint != null)
//        {
//            Destroy(currentPoint);
//        }

//        SpawnAtViewportPosition(pos, pointContainer, Helper.MainCam);
//    }

//    public void SpawnAtViewportPosition(Vector2 viewportPos, RectTransform container, Camera cam)
//    {
//        Vector2 worldPos = cam.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, cam.nearClipPlane));
//        RectTransform spawnedUI = Instantiate(pointTemplate, worldPos, Quaternion.identity, container).GetComponent<RectTransform>();
//        currentPoint = spawnedUI.gameObject;
//        Vector2 localPoint;
//        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, cam.WorldToScreenPoint(worldPos), cam, out localPoint);
//        spawnedUI.anchoredPosition = localPoint;
//    }

//    public void Disconnect()
//    {
//        ASeeTracker._7i_cancel_calibration();
//        ASeeTracker._7i_stop_tracking();
//        ASeeTracker._7i_stop();
//        ASeeTracker._7i_device_disconnect();
//    }

//    public void SetSmoothBajoMedioAlto(int value)
//    {
//        switch (value)
//        {
//            case 0: GazeDeviceA11.SetSmooth(1); break; //SinFiltro
//            case 0: GazeDeviceA11.SetSmooth(3); break;
//            case 1: GazeDeviceA11.SetSmooth(7); break;
//            case 2: GazeDeviceA11.SetSmooth(10); break;
//        }

//        filterToggleGroup.SetToggleOptionWithoutNotify(GetSmoothOption());
//    }

//    public int GetSmoothOption()
//    {
//        int smooth = SaveManager.GetSystemSetting(GazeDeviceA11.SMOOTH_VALUE_KEY, 0);
//        int option = 0;
//        switch (smooth)
//        {
//            case <= 3: option = 0; break;
//            case <= 7: option = 1; break;
//            case <= 10: option = 2; break;
//        }
//        return option;
//    }

//    int GetSmoothValueFromToggle()
//    {
//        int option = 10;
//        switch (filterToggleGroup.CurrentIndex)
//        {
//            case 0: option = 3; break;
//            case 1: option = 7; break;
//            case 2: option = 10; break;
//        }
//        return option;
//    }


//    Vector2[] GetCalibrationPointsArray(int index)
//    {
//        Vector2[] points = default;
//        switch (index)
//        {
//            case 0: points = points3; break;
//            case 1: points = points5; break;
//            case 2: points = points9; break;

//        }
//        return points;
//    }

//    int GetCalibrationPointsCount()
//    {
//        int option = 0;
//        switch (filterToggleGroup.CurrentIndex)
//        {
//            case 0: option = 3; break;
//            case 1: option = 5; break;
//            case 2: option = 9; break;
//        }
//        return option;
//    }

//    string GetEyeAsString(int eyes)
//    {
//        string ojos = "Desconocido";
//        switch (eyes)
//        {
//            case 0: ojos = "Ambos"; break;
//            case 1: ojos = "Izquierdo"; break;
//            case 2: ojos = "Derecho"; break;
//        }
//        return ojos;
//    }

//}
