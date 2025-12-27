using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using MessagePack;
public static class FileOps
{
    public static void SaveToJson<T>(string filePath, T content)
    {
        CreateFileDirectory(filePath);
        string json = JsonConvert.SerializeObject(content, Formatting.Indented);
        File.WriteAllText(filePath, json);
        Debug.WriteLine("Saved file at: " + filePath);
    }

    public static T LoadFromJson<T>(string filePath, T defaultValue)
    {
        if (!File.Exists(filePath))
        {
            Debug.WriteLine("File not found at path: " + filePath + " Returning default value.");
            return defaultValue;
        }

        string jSonData = File.ReadAllText(filePath);

        try
        {
            T result = JsonConvert.DeserializeObject<T>(jSonData);
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Deserialization error: " + ex.Message);
            Debug.WriteLine("Offending JSON: " + jSonData);
            return defaultValue;
        }

    }

    public static void SaveToJsonEncrypted<T>(string filePath, T content, string encryptionPass)
    {
        CreateFileDirectory(filePath);
        string json = JsonConvert.SerializeObject(content, Formatting.Indented);
        string encrypted = Cipher.Encrypt(json, encryptionPass);
        File.WriteAllText(filePath, encrypted);
        Debug.WriteLine("Saved file at: " + filePath);
    }

    public static T LoadFromJsonEncrypted<T>(string filePath, T defaultValue, string encryptionPass)
    {
        if (!File.Exists(filePath))
        {
            Debug.WriteLine("File not found at path: " + filePath + " Returning default value.");
            return defaultValue;
        }

        string jSonData = File.ReadAllText(filePath);
        jSonData = Cipher.Decrypt(jSonData, encryptionPass);
        try
        {
            T result = JsonConvert.DeserializeObject<T>(jSonData);
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Deserialization error: " + ex.Message);
            Debug.WriteLine("Offending JSON: " + jSonData);
            return defaultValue;
        }
    }


    public static void DeleteFile(string path)
    {
        if (!File.Exists(path)) return;
        File.Delete(path);
    }

    public static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        DirectoryInfo directory = new DirectoryInfo(path);
        directory.Delete(true);
    }

    public static void OpenFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }

    public static void OpenDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
    public static void OpenDirectoryUsingFilePath(string filePath)
    {
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath)) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = directoryPath,
            UseShellExecute = true
        });
    }

    public static void CreateFileDirectory(string filePath)
    {
        string directoryPath = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(directoryPath);
    }

    public static void SaveToBinary<T>(string filePath, T content)
    {
        try
        {
            CreateFileDirectory(filePath);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                MessagePackSerializer.Typeless.Serialize(fileStream, content);
            }
        }
        catch(Exception ex)
        {
            string logPath = Path.Combine(Path.GetDirectoryName(filePath), "error.log");
            File.WriteAllText(logPath, ex.ToString());
            //Log...?
        }
    }

    public static T LoadFromBinary<T>(string filePath, T defaultValue)
    {
        if (!File.Exists(filePath))
        {
            Debug.WriteLine("File not found at path: " + filePath + " Returning default value.");
            return defaultValue;
        }

        Debug.WriteLine("Loading file at path: " + filePath);

        T result = defaultValue;
        try
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                result = (T)MessagePackSerializer.Typeless.Deserialize(fileStream);
            }
        }
        catch
        {
            result = defaultValue;
        }
        return result;
    }
}
