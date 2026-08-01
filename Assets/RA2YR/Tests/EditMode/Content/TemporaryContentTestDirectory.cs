using System;
using System.Collections.Generic;
using System.IO;

namespace RA2YR.Tests.EditMode.Content
{
    internal sealed class TemporaryContentTestDirectory : IDisposable
    {
        public TemporaryContentTestDirectory()
        {
            RootPath = Path.Combine(
                Path.GetTempPath(),
                "RA2YR.Content.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public string CreateDirectory(string relativePath)
        {
            string path = GetPath(relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        public string WriteText(string relativePath, string content)
        {
            string path = GetPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
            return path;
        }

        public string WriteBytes(string relativePath, byte[] content)
        {
            string path = GetPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, content);
            return path;
        }

        public string GetPath(string relativePath)
        {
            string path = Path.GetFullPath(Path.Combine(RootPath, relativePath));
            string prefix = RootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? RootPath
                : RootPath + Path.DirectorySeparatorChar;
            StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!path.StartsWith(prefix, comparison) &&
                !string.Equals(path, RootPath, comparison))
            {
                throw new ArgumentException("The test path escapes the temporary root.", nameof(relativePath));
            }

            return path;
        }

        public void Dispose()
        {
            if (!Directory.Exists(RootPath))
            {
                return;
            }

            var pending = new Stack<DirectoryInfo>();
            var visited = new Stack<DirectoryInfo>();
            pending.Push(new DirectoryInfo(RootPath));
            while (pending.Count > 0)
            {
                DirectoryInfo directory = pending.Pop();
                visited.Push(directory);
                foreach (FileSystemInfo entry in directory.GetFileSystemInfos())
                {
                    entry.Refresh();
                    bool isReparse = (entry.Attributes & FileAttributes.ReparsePoint) != 0;
                    if (entry is DirectoryInfo childDirectory)
                    {
                        if (isReparse)
                        {
                            Directory.Delete(childDirectory.FullName, false);
                        }
                        else
                        {
                            pending.Push(childDirectory);
                        }
                    }
                    else
                    {
                        File.SetAttributes(entry.FullName, FileAttributes.Normal);
                        File.Delete(entry.FullName);
                    }
                }
            }

            while (visited.Count > 0)
            {
                Directory.Delete(visited.Pop().FullName, false);
            }
        }
    }
}
