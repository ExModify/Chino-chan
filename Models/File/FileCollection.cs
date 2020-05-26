using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.File
{
    public class FileCollection
    {
        public int Count
        {
            get
            {
                return Files.Count;
            }
        }
        public List<string> Files { get; private set; }

        public string Name { get; private set; }
        public string Path { get; private set; }
        public bool TitleIncludeName { get; private set; }
        public bool IsNsfw { get; private set; }
        public bool SearchSubDirs { get; private set; }
        
        private FileSystemWatcher Watcher;

        public FileCollection(string Name, string Path, bool TitleIncludeName, bool IsNsfw, bool SearchSubDirs)
        {
            Files = new List<string>();

            if (!Path.EndsWith("\\"))
                Path += "\\";

            this.Name = Name;
            this.Path = Path;
            this.TitleIncludeName = TitleIncludeName;
            this.IsNsfw = IsNsfw;
            this.SearchSubDirs = SearchSubDirs;

            UpdateFilepath();
        }

        public string RandomFile()
        {
            if (Count == 0)
                return "";

            string File = "";
            do
            {
                if (File != "")
                {
                    Files.Remove(File);
                    if (Files.Count == 0)
                    {
                        File = "";
                        break;
                    }
                }
                File = Files[Global.Random.Next(Count)];
            }
            while (!System.IO.File.Exists(File));
            
            return File;
        }
        public int IndexOf(string File, bool CaseSensitive)
        {
            var _File = File;
            if (!CaseSensitive)
                _File = _File.ToLower();
            
            for (int i = 0; i < Count; i++)
            {
                var CurrentFile = Files[i];
                if (!CaseSensitive)
                {
                    CurrentFile = CurrentFile.ToLower();
                }

                var Found = CurrentFile == File
                    || System.IO.Path.GetFileName(CurrentFile) == File
                    || System.IO.Path.GetFileName(CurrentFile) == System.IO.Path.GetFileName(File)
                    || System.IO.Path.GetFileNameWithoutExtension(CurrentFile) == File
                    || System.IO.Path.GetFileNameWithoutExtension(CurrentFile) == System.IO.Path.GetFileNameWithoutExtension(File);
                
                if (Found)
                    return i;
            }

            return -1;
        }
        public void Delete(string File)
        {
            System.IO.File.Delete(Path + File);
        }

        private void UpdateFilepath()
        {
            if (Watcher != null)
                Watcher.Dispose();

            Files.Clear();
            Files.AddRange(Directory.EnumerateFiles(Path, "*", (SearchSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)));

            Watcher = new FileSystemWatcher(Path)
            {
                IncludeSubdirectories = SearchSubDirs
            };
            Watcher.Created += (sender, Args) =>
            {
                Files.Add(Args.FullPath);
            };
            Watcher.Deleted += (sender, Args) =>
            {
                Files.Remove(Args.FullPath);
            };
            Watcher.BeginInit();
        }
    }
}
