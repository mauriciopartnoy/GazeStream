public static class aSeeResults
{
    public static string StartResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "The path provided is not UTF8 encoded. Ensure UTF8 format."; break;
            case -4: message = resultCode + "Failed to initialize camera. Check USB connection."; break;
            case -1112: message = resultCode + "No camera found or camera initialization failed. Check USB connection."; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string StopResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "Initialization failed or no initialization. Please call _7i_start first."; break;
            case -3: message =resultCode +  "SDK version number does not match. Check the SDK version."; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string StartTrackingResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "Initialization failed. Please call _7i_start first"; break;
            case -3: message =resultCode +  "SDK version number does not match. Check the SDK version."; break;
            default: message = resultCode + "Internal error. Call the manufacturer."; break;

        }
        return message;
    }

    public static string StopTrackingResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            default: message = resultCode + "Internal error. Call the manufacturer."; break;
        }
        return message;
    }

    public static string SetSmoothResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "Parameter setting failed. Check parameter valid range."; break;
            case -71: message = resultCode + "Parameter setting error. Check parameter valid range."; break;
        }
        return message;
    }

    public static string StartCalibrationResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "Initialization failed. Please call _7i_start first"; break;
            case -212: message = resultCode + "Parameter setting error. Check parameter valid range."; break;
            case -7001: message = resultCode + "Parameter setting error. Check parameter valid range."; break;
        }
        return message;
    }

    public static string StartCalibrationPointResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message =resultCode +  "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "Initialization failed. Please call _7i_start_calibration first"; break;
            case -221222: message = resultCode + "Parameter setting error. Check parameter valid range."; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string CancelCalibrationResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "Initialization failed. Please call _7i_start_calibration first"; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string ComputeCalibrationResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "Initialization failed. Please call _7i_start_calibration first"; break;
            case -253: message = resultCode + "Parameter error. Coefficient cannot be empty."; break;
            case -251252: message = resultCode + "Failed to calculate coefficient. Recalibrate."; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string CompleteCalibrationResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "Initialization failed. Please call _7i_start_calibration first"; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string GetCalibrationScoreResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "Fail."; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string SetCalibrationModeResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "Fail."; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string SetGazeCallbackResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -1: message = resultCode + "No authorization. Please connect the dongle first."; break;
            case -2: message = resultCode + "Callback function is empty. Check function definition."; break;
            default: message = resultCode + "Internal error. Contact the manufacturer."; break;
        }
        return message;
    }

    public static string DisconnectResultToString(int result)
    {
        string resultCode = $"Result Code: [{result}] ";
        string message = resultCode + "Result code not found.";
        switch (result)
        {
            case 0: message = resultCode + "Success."; break;
            case -21: message = resultCode + "Device disconnect failed."; break;
            case -22: message = resultCode + "Device not found. Contact the manufacturer."; break;
            default: message = resultCode + $"Internal error. Contact the manufacturer."; break;
        }
        return message;
    }
}
