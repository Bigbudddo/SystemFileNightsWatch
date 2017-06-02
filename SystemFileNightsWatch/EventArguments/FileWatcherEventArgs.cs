// SystemFileNightsWatch.cs
// -------------------------------------------
// 
// Copyright (c) 2017 Stuart Harrson
// All rights reserved.
// 
// -------------------------------------------
//
// This code is licensed under the MIT License.
// More info: https://github.com/Bigbudddo/SystemFileNightsWatch/blob/master/LICENSE
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SystemFileNightsWatch {

    // TODO: add property for total size. I will use recursion to find all the files inside each directory and add it all up
    public sealed class FileWatcherEventArgs : EventArgs {

        public long Size { get; private set; }
        public string DriveLetter { get; private set; }
        public string DriveDirectory { get; private set; }
        public Dictionary<string, DirectoryInfo> Directories { get; private set; }
        public Dictionary<string, FileInfo> Files { get; private set; }
        
        public int TotalFolders {
            get {
                return Directories.Count;
            }
        }

        public int TotalFiles {
            get {
                return Files.Count;
            }
        }

        public Dictionary<string, FileSystemInfo> SystemFiles {
            get {
                var retval = new Dictionary<string, FileSystemInfo>();

                foreach (var d in Directories) {
                    if (!retval.ContainsKey(d.Key)) {
                        retval.Add(d.Key, d.Value);
                    }
                }

                foreach (var f in Files) {
                    if (!retval.ContainsKey(f.Key)) {
                        retval.Add(f.Key, f.Value);
                    }
                }

                return retval;
            }
        }

        public FileWatcherEventArgs(string dir, string folder) {
            var files = new List<string>();
            files.AddRange(Directory.EnumerateDirectories(folder));
            files.AddRange(Directory.EnumerateFiles(folder));

            Initialise(dir, files);
        }

        public FileWatcherEventArgs(string dir, IEnumerable<string> systemFiles) {
            Initialise(dir, systemFiles);
        }

        private bool IsDirectory(string path) {
            FileAttributes fAtt = File.GetAttributes(path);
            return (fAtt.HasFlag(FileAttributes.Directory));
        }

        private string FileName(string path) {
            string[] aPath = path.Split('\\');
            return string.Join("\\", aPath.Take(aPath.Length - 1));
        }

        private void Initialise(string dir, IEnumerable<string> systemFiles) {
            DriveLetter = new string(dir.ToCharArray().Take(1).ToArray());
            DriveDirectory = dir;

            var dirs = new Dictionary<string, DirectoryInfo>();
            var files = new Dictionary<string, FileInfo>();

            foreach (var path in systemFiles) {
                if (IsDirectory(path)) {
                    DirectoryInfo info = new DirectoryInfo(path);

                    if (!dirs.ContainsKey(path)) {
                        dirs.Add(path, info);
                    }
                }
                else {
                    FileInfo info = new FileInfo(path);
                    Size += info.Length;

                    if (!files.ContainsKey(path)) {
                        files.Add(path, info);
                    }
                }
            }

            Directories = dirs;
            Files = files;
        }
    }
}
