// DriveWatcherEventArgs.cs
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

    public sealed class DriveWatcherEventArgs : EventArgs {

        public string SystemDriveLetter { get; private set; }
        public Dictionary<string, DriveInfo> Drives { get; private set; }

        public string SystemDrive {
            get {
                return string.Format("{0}:\\", SystemDriveLetter);
            }
        }

        public DriveWatcherEventArgs(IEnumerable<string> drives) {
            SystemDriveLetter = FetchSystemDriveLetter();
            Drives = FetchAllDriveInfo(drives);
        }

        private Dictionary<string, DriveInfo> FetchAllDriveInfo(IEnumerable<string> drives) {
            var retval = new Dictionary<string, DriveInfo>();

            foreach (var drive in drives) {
                string letter = FetchDriveLetter(drive);
                var info = new DriveInfo(letter);

                if (!retval.ContainsKey(letter)) {
                    retval.Add(letter, info);
                }
            }

            return retval;
        }

        public string FetchDriveLetter(string drive) {
            return new string(drive.ToCharArray().Take(1).ToArray());
        }

        public string FetchSystemDriveLetter() {
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            return new string(systemFolder.ToCharArray().Take(1).ToArray()); // E.g. takes c
        }
    }
}
