namespace ConfigBridge.Library
{
    public interface IFileSystem
    {
        bool Exists(string path);
        string ReadAllText(string path);
    }
}