namespace GazeStream.Utilities.Save
{
    public class LocalFileSaver : IFileSaver
    {
        public void DeleteFile(string path)
        {
            FileOps.DeleteFile(path);
        }
        public void DeleteDirectory(string path)
        {
            FileOps.DeleteDirectory(path);
        }

        public T LoadFromJson<T>(string path, T defaultValue)
        {
            return FileOps.LoadFromJson<T>(path, defaultValue);
        }

        public void SaveToJson<T>(string path, T content)
        {
            FileOps.SaveToJson<T>(path, content);
        }

        public T LoadFromBinary<T>(string path, T defaultValue)
        {
            return FileOps.LoadFromMessagePack<T>(path, defaultValue);
        }

        public void SaveToBinary<T>(string path, T content)
        {
            FileOps.SaveToMessagePack<T>(path, content);
        }
    }
}