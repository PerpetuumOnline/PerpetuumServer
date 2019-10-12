using System.Collections.Generic;
using System.IO;

namespace Perpetuum.IO
{
    public class FileSystem : IFileSystem
    {
        private readonly string _root;

        public FileSystem(string root)
        {
            _root = root;
        }

        public bool Exists(string path)
        {
            return File.Exists(CreatePath(path));
        }

        public byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(CreatePath(path));
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(CreatePath(path));
        }

        public string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(CreatePath(path));
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(CreatePath(path),bytes);
        }

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
            File.WriteAllLines(CreatePath(path), lines);
        }

        public void AppendAllText(string path, string text)
        {
            File.AppendAllText(CreatePath(path),text);
        }

        public void AppendAllLines(string path, IEnumerable<string> lines)
        {
            File.AppendAllLines(CreatePath(path),lines);
        }

        public void MoveFile(string sourcePath, string targetPath)
        {
            var src = CreatePath(sourcePath);
            var dest = CreatePath(targetPath);

            if (File.Exists(dest))
                File.Delete(dest);

            File.Move(src,dest);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(CreatePath(path));
        }

        public string CreatePath(string path)
        {
            return Path.Combine(_root, path);
        }

        public IEnumerable<string> GetFiles(string path, string mask)
        {
            var fullPath = Path.Combine(_root, path);
            if (!Directory.Exists(fullPath))
            {
                return System.Linq.Enumerable.Empty<string>();
            }
            return Directory.GetFiles(fullPath, mask);
        }

        public override string ToString()
        {
            return $"Root: {_root}";
        }
    }
}