// SystemWatch.cs
// -------------------------------------------
// 
// Copyright (c) 2017 Stuart Harrson
// All rights reserved.
//
// This code will run two seperate thread for checking changes in logical drives
// and system file changes. Much more lightweight than the FileSystemWatcher .NET
// version
// 
// -------------------------------------------
//
// This code is licensed under the MIT License.
// More info: https://github.com/Bigbudddo/SystemFileNightsWatch/blob/master/LICENSE
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace SystemFileNightsWatch {

    public sealed class SystemWatch : IDisposable, INotifyPropertyChanged {

        private bool _startLogicalDriveWatcher = true;
        private bool _startFileSystemWatcher = true;
        private bool _runThreads = true;
        private bool _isBackground = true;
        private bool _hasChangedDirectory = false;
        private int _threadSleepTimeout = 1000;
        private string _monitorDirectory = string.Empty;
        private Thread _logicalDriveWatcher;
        private Thread _fileSystemWatcher;

        private IEnumerable<string> _currentLogicalDrives;
        private IEnumerable<string> _currentSystemFiles;

        public delegate void LogicalDriveWatchHandler(object sender, DriveWatcherEventArgs e);
        public delegate void FileSystemWatchHandler(object sender, FileWatcherEventArgs e);

        public event PropertyChangedEventHandler PropertyChanged;
        public event LogicalDriveWatchHandler DrivesDetected;
        public event FileSystemWatchHandler FilesChanged;
        public event FileSystemWatchHandler DirectoryChanged;

        public bool IsRunningLogicalDriveWatcher {
            get { return _startLogicalDriveWatcher; }
            set {
                if (_startLogicalDriveWatcher != value) {
                    _startLogicalDriveWatcher = value;
                    NotifyPropertyChanged("IsRunningLogicalDriveWatcher");

                    if (!_startLogicalDriveWatcher && _logicalDriveWatcher != null && _logicalDriveWatcher.IsAlive) {
                        StopLogicalDriveWatcher();
                    }
                }
            }
        }

        public bool IsRunningFileSystemWatcher {
            get { return _startFileSystemWatcher; }
            set {
                if (_startFileSystemWatcher != value) {
                    _startFileSystemWatcher = value;
                    NotifyPropertyChanged("IsRunningFileSystemWatcher");

                    if (!_startFileSystemWatcher && _fileSystemWatcher != null && _fileSystemWatcher.IsAlive) {
                        StopFileSystemWatcher();
                    }
                }
            }
        }

        public int Timeout {
            get { return _threadSleepTimeout; }
            set {
                if (_threadSleepTimeout != value) {
                    _threadSleepTimeout = value;
                    NotifyPropertyChanged("Timeout");
                }
            }
        }

        public string MonitoredDirectory {
            get {
                return _monitorDirectory;
            }
            private set {
                string theValue = value;

                if (theValue.Length == 2) {
                    theValue += "\\";
                }

                if (_monitorDirectory != theValue) {
                    _monitorDirectory = theValue;
                    NotifyPropertyChanged("MonitoredDirectory");

                    try {
                        if (DirectoryChanged != null) {
                            var args = new FileWatcherEventArgs(theValue, theValue);
                            Application.Current.Dispatcher.Invoke(() => {
                                DirectoryChanged(this, args);
                                _hasChangedDirectory = false;
                            });
                        }
                    }
                    catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }

        public SystemWatch() {
            Initialise();
        }

        public SystemWatch(string initFolder) {
            Initialise();
            _monitorDirectory = initFolder;
        }

        public void NotifyPropertyChanged(string propName) {
            if (this.PropertyChanged != null) {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public void ChangeDirectory(string path) {
            if (String.IsNullOrWhiteSpace(path)) {
                return;
            }

            if (!ValidateFolderPath(path)) {
                return;
            }

            _hasChangedDirectory = true;
            MonitoredDirectory = path;
        }

        public void ChangeDirectoryUp() {
            if (String.IsNullOrEmpty(_monitorDirectory)) {
                return;
            }

            string[] aMonitoredFolder = _monitorDirectory.Split('\\');
            string newMonitoredFolder = string.Join("\\", aMonitoredFolder.Take(aMonitoredFolder.Length - 1));

            _hasChangedDirectory = true;
            MonitoredDirectory = newMonitoredFolder;
        }

        public void StartAll() {
            _runThreads = true;
            StartLogicalDriveWatcher();
            StartFileSystemWatcher();
        }

        public void StartLogicalDriveWatcher() {
            if (!_startLogicalDriveWatcher) {
                return;
            }

            if (_logicalDriveWatcher != null) {
                _runThreads = true;
                _logicalDriveWatcher.Start();
            }
        }

        public void StartFileSystemWatcher() {
            if (!_startFileSystemWatcher) {
                return;
            }

            if (_fileSystemWatcher != null) {
                _runThreads = true;
                _fileSystemWatcher.Start();
            }
        }

        public void StopAll() {
            _runThreads = false;
            StopLogicalDriveWatcher();
            StopFileSystemWatcher();
        }

        public void StopLogicalDriveWatcher() {
            if (_logicalDriveWatcher != null) {
                _runThreads = false;
                _logicalDriveWatcher.Join();
            }
        }

        public void StopFileSystemWatcher() {
            if (_fileSystemWatcher != null) {
                _runThreads = false;
                _fileSystemWatcher.Join();
            }
        }

        public void Dispose() {
            _runThreads = false;
            StopAll();
        }

        private bool ValidateFolderPath(string path) {
            FileAttributes fAtt = File.GetAttributes(path);
            return (fAtt.HasFlag(FileAttributes.Directory));
        }

        private void Initialise() {
            _currentLogicalDrives = Enumerable.Empty<string>();
            _currentSystemFiles = Enumerable.Empty<string>();

            _logicalDriveWatcher = new Thread(new ThreadStart(LogicalDriveWatcherThread));
            _logicalDriveWatcher.IsBackground = _isBackground;

            _fileSystemWatcher = new Thread(new ThreadStart(FileSystemWatcherThread));
            _fileSystemWatcher.IsBackground = _isBackground;
        }

        private void LogicalDriveWatcherThread() {
            while (_runThreads) {
                IEnumerable<string> newList = Directory.GetLogicalDrives();

                if (newList.Count() != _currentLogicalDrives.Count()) {
                    _currentLogicalDrives = newList;
                    var args = new DriveWatcherEventArgs(_currentLogicalDrives);

                    try {
                        Application.Current.Dispatcher.Invoke(() => {
                            if (DrivesDetected != null) {
                                DrivesDetected(this, args);
                            }
                        });
                    }
                    catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }
                }

                Thread.Sleep(_threadSleepTimeout);
            }
        }

        private void FileSystemWatcherThread() {
            while (_runThreads) {
                if (_hasChangedDirectory) {
                    Thread.Sleep(_threadSleepTimeout);
                    continue;
                }

                var files = new List<string>();
                lock (_monitorDirectory) {
                    files.AddRange(Directory.EnumerateDirectories(_monitorDirectory));
                    files.AddRange(Directory.EnumerateFiles(_monitorDirectory));
                }

                // There is two ways we have changes..
                // 1. There has been a file deleted, or added
                // 2. The file name has been changed (harder)

                bool isChanges = false;

                // Handle option 1...
                if (files.Count() != _currentSystemFiles.Count()) {
                    isChanges = true;
                    _currentSystemFiles = files;
                }

                // Handle option 2...
                if (!isChanges) {
                    // No point is executing this code if we already know we have changes
                    // from a much simpiler function
                    foreach (var currFile in _currentSystemFiles) {
                        if (isChanges) {
                            break;
                        }

                        if (!files.Contains(currFile)) {
                            isChanges = true;
                            _currentSystemFiles = files;
                        }
                    }
                }

                if (isChanges) {
                    var args = new FileWatcherEventArgs(_monitorDirectory, _currentSystemFiles);

                    try {
                        Application.Current.Dispatcher.Invoke(() => {
                            if (FilesChanged != null) {
                                FilesChanged(this, args);
                            }
                        });
                    }
                    catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }
                }

                Thread.Sleep(_threadSleepTimeout);
            }
        }
    }
}