namespace GazeStream.Utilities.Save
{
    public interface IFileSaver
    {
        void SaveToJson<T>(string path, T content);
        T LoadFromJson<T>(string path, T defaultValue);
        void DeleteDirectory(string path);
        void DeleteFile(string path);
        void SaveToBinary<T>(string path, T content);
        T LoadFromBinary<T>(string path, T defaultValue);

    }
}

