using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using PhotoSorter.Properties;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace PhotoSorter
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields and properties. Events and delegate

        private string _photoPath;

        public string PhotoPath
        {
            get { return _photoPath; }
            private set
            {
                _photoPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                Settings.Default.PhotoPath = value;
                Settings.Default.Save();
            }
        }

        private string _savePath;

        public string SavePath
        {
            get { return _savePath; }
            private set
            {
                _savePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                Settings.Default.SavePath = value;
                Settings.Default.Save();
            }
        }

        private bool _moveFiles;

        public bool MoveFiles
        {
            get { return _moveFiles; }
            set
            {
                _moveFiles = value;
                OnPropertyChanged();
                Settings.Default.MoveFiles = value;
                Settings.Default.Save();
            }
        }

        public bool ProcessStarted
        {
            get
            {
                return _processStarted;
            }
            private set
            {
                _processStarted = value;
                OnPropertyChanged();
            }
        }

        public bool ProcessNotStarted
        {
            get
            {
                return _processNotStarted;
            }
            private set
            {
                _processNotStarted = value;
                OnPropertyChanged();
            }
        }

        public bool IsValid
        {
            get
            {
                return Directory.Exists(PhotoPath) &&
                       Directory.Exists(SavePath) &&
                       !string.IsNullOrWhiteSpace(DirMask) && 
                       !DirMask.ToCharArray().Any(ch => Path.GetInvalidPathChars().Contains(ch)) &&
                       !string.IsNullOrWhiteSpace(ExtensionFilter);
            }
        }

        public string SampleDirName
        {
            get 
            {
                try
                {
                    if (DirMask.ToCharArray().Any(ch => Path.GetInvalidPathChars().Contains(ch)))
                        throw new FormatException("Содержаться некорректные символы");
                    return $"Пример: {DateTime.Now.ToString(_dirMask)}";
                }
                catch
                {
                    return Resources.NotCorrectMask;
                }
            }
        }

        private string _dirMask;

        public string DirMask
        {
            get { return _dirMask; }
            set
            {
                _dirMask = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SampleDirName));
                OnPropertyChanged(nameof(IsValid));
                Settings.Default.DirMask = value;
                Settings.Default.Save();
            }
        }

        public double ProgressPrecent => ProgressValue != 0 ? ProgressValue / 100 : 0;

        public double ProgressValue
        {
            get
            {
                return _progressValue;
            }
            private set
            {
                _progressValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPrecent));
            }
        }
        private double _fileCount;
        public TaskbarItemProgressState TaskbarItemProgressState
        {
            get
            {
                return _taskbarItemProgressState;
            }
            private set
            {
                _taskbarItemProgressState = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ProcessedFiles { get; }
        private bool _needStop;
        private string _extensionFilter;
        private bool _processStarted;
        private bool _processNotStarted;
        private double _progressValue;
        private TaskbarItemProgressState _taskbarItemProgressState;
        private bool _searchInSubFolder;

        public ActionCommand SelectPhotoPathCommand
        {
            get { return new ActionCommand(o => SelectPhotoPath(), o => !ProcessStarted); }
        }

        public ActionCommand SelectSavePathCommand
        {
            get { return new ActionCommand(o => SelectSavePath(), o => !ProcessStarted); }
        }

        public ActionCommand StartProcessingCommand
        {
            get { return new ActionCommand(o => StartProcessing(), o => !ProcessStarted && IsValid); }
        }

        public ActionCommand StopProcessingCommand
        {
            get { return new ActionCommand(o => StopProcessing(), o => ProcessStarted); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string ExtensionFilter
        {
            get { return _extensionFilter; }
            set
            {
                _extensionFilter = value;
                OnPropertyChanged();
                Settings.Default.ExtensionFilter = value;
                Settings.Default.Save();
            }
        }

        public bool SearchInSubFolder
        {
            get
            {
                return _searchInSubFolder;
            }
            set
            {
                _searchInSubFolder = value;
                OnPropertyChanged();
                Settings.Default.SearchInSubFolder = value;
                Settings.Default.Save();
            }
        }

        #endregion

        #region Constructors

        public MainViewModel()
        {
            ProcessedFiles = new ObservableCollection<string>();
            _photoPath = Settings.Default.PhotoPath;
            _dirMask = Settings.Default.DirMask;
            _savePath = Settings.Default.SavePath;
            _moveFiles = Settings.Default.MoveFiles;
            _extensionFilter = Settings.Default.ExtensionFilter;
        }

        #endregion

        #region Method

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private string GetFolderName(string filePath)
        {
            using (var photo = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    var decoder = BitmapDecoder.Create(photo, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
                    var bitmapMetadata = (BitmapMetadata)decoder.Frames[0].Metadata;
                    if (bitmapMetadata != null)
                    {
                        var dt = Convert.ToDateTime(bitmapMetadata.DateTaken);
                        photo.Flush();
                        photo.Close();
                        return dt.ToString(_dirMask);
                    }
                    photo.Flush();
                    photo.Close();
                    var fi = new FileInfo(filePath);
                    return fi.CreationTime.ToString(_dirMask);
                }
                catch
                {
                    photo.Flush();
                    photo.Close();
                    var fi = new FileInfo(filePath);
                    return fi.CreationTime.ToString(_dirMask);
                }
            }
        }


        private void SelectPhotoPath()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = Resources.SelectPhotoPath;
                dlg.ShowNewFolderButton = true;
                dlg.SelectedPath = PhotoPath;
                var oldWindow = new OldWindow(new WindowInteropHelper(Application.Current.MainWindow).Handle);
                if (dlg.ShowDialog(oldWindow) != DialogResult.OK)
                    return;
                PhotoPath = dlg.SelectedPath;
            }
        }


        private void SelectSavePath()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = Resources.SelectSavePhotoPath;
                dlg.ShowNewFolderButton = true;
                dlg.SelectedPath = SavePath;
                var oldWindow = new OldWindow(new WindowInteropHelper(Application.Current.MainWindow).Handle);
                if (dlg.ShowDialog(oldWindow) != DialogResult.OK)
                    return;
                SavePath = dlg.SelectedPath;
            }
        }


        private async void StartProcessing()
        {
            ProcessedFiles.Clear();
            TaskbarItemProgressState = TaskbarItemProgressState.Normal;
            OnPropertyChanged(nameof(TaskbarItemProgressState));
            ProcessStarted = true;
            ProcessNotStarted = false;
            await StartSortingAsync();
            TaskbarItemProgressState = TaskbarItemProgressState.None;
            ProgressValue = 0;
            _fileCount = 0;
            ProcessStarted = false;
            ProcessNotStarted = true;
            MessageBox.Show(Resources.SortComplite, Application.Current.MainWindow.Title, MessageBoxButton.OK,
                    MessageBoxImage.Information);
        }


        private void StopProcessing()
        {
            _needStop = true;
        }

        #endregion

        #region HandlesEvent

        private Task StartSortingAsync()
        {
            return Task.Factory.StartNew(RunSorting, TaskCreationOptions.LongRunning);
        }

        private void RunSorting()
        {
            _needStop = false;
            var dirInfo = new DirectoryInfo(PhotoPath);
            var files = new List<FileInfo>();
            var extensions = _extensionFilter.Split(new[]
            {
                ';'
            }, StringSplitOptions.RemoveEmptyEntries);
            if (!extensions.Any())
                return;
            Array.ForEach(extensions, ext => files.AddRange(dirInfo.GetFiles(ext, SearchInSubFolder ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)));
            _fileCount = files.Count;
            for (var i = 0; i < files.Count; i++)
            {
                if (_needStop)
                    break;
                try
                {
                    var path = Path.Combine(SavePath, GetFolderName(files[i].FullName));
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    if (_moveFiles)
                        File.Move(files[i].FullName, Path.Combine(path, files[i].Name));
                    else
                        File.Copy(files[i].FullName, Path.Combine(path, files[i].Name), false);
                }
                catch (Exception ex)
                {
                    ReportProgress(i + 1, ex.Message);
                    continue;
                }
                ReportProgress(i + 1, files[i].Name);
            }
        }


        private void ReportProgress(int progressPrecent, string fileName)
        {
            ProgressValue = progressPrecent / _fileCount * 100d;
            Application.Current.Dispatcher.Invoke(() => ProcessedFiles.Add(fileName), DispatcherPriority.DataBind);
        }

        #endregion
    }
}