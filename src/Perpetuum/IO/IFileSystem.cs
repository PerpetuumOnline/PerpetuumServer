using System.Collections.Generic;

namespace Perpetuum.IO
{
    public interface IFileSystem
    {
        bool Exists(string path);
        byte[] ReadAllBytes(string path);
        string ReadAllText(string path);
        string[] ReadAllLines(string path);

        void WriteAllBytes(string path, byte[] bytes);
        void WriteAllLines(string path,IEnumerable<string> lines);

        void AppendAllText(string path, string text);
        void AppendAllLines(string path, IEnumerable<string> lines);

        void MoveFile(string sourcePath, string targetPath);

        void CreateDirectory(string path);

        string CreatePath(string path);
    }
}