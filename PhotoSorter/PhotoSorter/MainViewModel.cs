using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
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
                OnPropertyChanged("IsValid");
                ConfigurationManager.AppSettings.Set("PhotoPath", _photoPath);
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
                OnPropertyChanged("IsValid");
                ConfigurationManager.AppSettings.Set("SavePath", _savePath);
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
                ConfigurationManager.AppSettings.Set("MoveFiles", _moveFiles.ToString());
            }
        }

        public bool ProcessStarted
        {
            get { return _backgroundWorker.IsBusy; }
        }

        public bool ProcessNotStarted
        {
            get { return !ProcessStarted; }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(PhotoPath) && Directory.Exists(PhotoPath) &&
                       !string.IsNullOrWhiteSpace(SavePath) && Directory.Exists(SavePath) &&
                       !string.IsNullOrWhiteSpace(DirMask) && !DirMask.ToCharArray().Any(ch => Path.GetInvalidPathChars().Contains(ch)) &&
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
                        throw new FormatException();
                    return string.Format("Пример: {0}", DateTime.Now.ToString(_dirMask));
                }
                catch
                {
                    return "Некорректная маска";
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
                OnPropertyChanged("SampleDirName");
                OnPropertyChanged("IsValid");
                ConfigurationManager.AppSettings.Set("DirMask", _dirMask);
            }
        }

        public double ProgressPrecent
        {
            get { return ProgressValue != 0 ? ProgressValue / 100 : 0; }
        }

        public double ProgressValue { get; private set; }
        private double _fileCount;
        public TaskbarItemProgressState TaskbarItemProgressState { get; private set; }
        public ObservableCollection<string> ProcessedFiles { get; private set; }
        private readonly BackgroundWorker _backgroundWorker;
        private bool _needStop;
        private string _extensionFilter;

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
                ConfigurationManager.AppSettings.Set("ExtensionFilter", _extensionFilter);
            }
        }

        #endregion

        #region Constructors

        public MainViewModel()
        {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            _backgroundWorker.ProgressChanged += BackgroundWorkerOnProgressChanged;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;

            ProcessedFiles = new ObservableCollection<string>();
            _photoPath = ConfigurationManager.AppSettings["PhotoPath"];
            _dirMask = ConfigurationManager.AppSettings["DirMask"];
            _savePath = ConfigurationManager.AppSettings["SavePath"];
            _moveFiles = Convert.ToBoolean(ConfigurationManager.AppSettings["MoveFiles"]);
            _extensionFilter = ConfigurationManager.AppSettings["ExtensionFilter"];
        }

        #endregion

        #region Method

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
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
                dlg.Description = "Выбор папки с фотографиями";
                dlg.ShowNewFolderButton = true;
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
                dlg.Description = "Выбор папки для сохранения фотографий";
                dlg.ShowNewFolderButton = true;
                var oldWindow = new OldWindow(new WindowInteropHelper(Application.Current.MainWindow).Handle);
                if (dlg.ShowDialog(oldWindow) != DialogResult.OK)
                    return;
                SavePath = dlg.SelectedPath;
            }
        }


        private void StartProcessing()
        {
            ProcessedFiles.Clear();
            TaskbarItemProgressState = TaskbarItemProgressState.Normal;
            OnPropertyChanged("TaskbarItemProgressState");
            _backgroundWorker.RunWorkerAsync();
            OnPropertyChanged("ProcessStarted");
            OnPropertyChanged("ProcessNotStarted");
        }


        private void StopProcessing()
        {
            _needStop = true;
        }

        #endregion

        #region HandlesEvent

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            _needStop = false;
            var dirInfo = new DirectoryInfo(PhotoPath);
            var files = new List<FileInfo>();
            var extensions = _extensionFilter.Split(new char[]
                                                    {
                                                            ';'
                                                    }, StringSplitOptions.RemoveEmptyEntries);
            if (!extensions.Any())
                return;
            Array.ForEach(extensions, ext => files.AddRange(dirInfo.GetFiles(ext, SearchOption.AllDirectories)));
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
                    _backgroundWorker.ReportProgress(i + 1, string.Format("{0} {1}", ex.Message, files[i].Name));
                    continue;
                }
                _backgroundWorker.ReportProgress(i + 1, files[i].Name);
            }
        }


        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressValue = e.ProgressPercentage / _fileCount * 100d;
            OnPropertyChanged("ProgressValue");
            OnPropertyChanged("ProgressPrecent");
            ProcessedFiles.Add(e.UserState.ToString());
        }


        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Сортировка завершена.", Application.Current.MainWindow.Title, MessageBoxButton.OK,
                    MessageBoxImage.Information);
            TaskbarItemProgressState = TaskbarItemProgressState.None;
            OnPropertyChanged("TaskbarItemProgressState");
            ProgressValue = 0;
            OnPropertyChanged("ProgressPrecent");
            OnPropertyChanged("ProgressValue");
            _fileCount = 0;
            OnPropertyChanged("ProcessStarted");
            OnPropertyChanged("ProcessNotStarted");
        }

        #endregion
    }
}