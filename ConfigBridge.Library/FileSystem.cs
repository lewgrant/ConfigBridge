using System.IO;

namespace ConfigBridge.Library
{
    public class FileSystem : IFileSystem
    {
        public bool Exists(string path) => File.Exists(path);
        public string ReadAllText(string path) => File.ReadAllText(path);
    }
}