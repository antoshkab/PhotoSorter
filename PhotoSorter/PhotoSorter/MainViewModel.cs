using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
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
        private string _photoPath;

        public string PhotoPath
        {
            get { return _photoPath; }
            private set
            {
                _photoPath = value; 
                OnPropertyChanged();
                ConfigurationManager.AppSettings.Set("PhotoPath", _photoPath);
            }
        }

        private bool _useSubFolder;

        public bool UseSubFolder
        {
            get { return _useSubFolder; }
            set
            {
                _useSubFolder = value;
                OnPropertyChanged();
                ConfigurationManager.AppSettings.Set("UseSubFolder", _useSubFolder.ToString());
            }
        }

        private bool _createByMonth;

        public bool CreateByMonth
        {
            get { return _createByMonth; }
            set
            {
                _createByMonth = value;
                OnPropertyChanged();
                ConfigurationManager.AppSettings.Set("CreateByMonth", _createByMonth.ToString());
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
                ConfigurationManager.AppSettings.Set("SavePath", _savePath);
            }
        }

        public bool ProcessStarted
        {
            get { return _backgroundWorker.IsBusy; }
        }

        public int ProcessValue { get; private set; }

        private int _fileCount;

        public int FileCount
        {
            get { return _fileCount; }
        }

        public TaskbarItemProgressState TaskbarItemProgressState { get; private set; }

        public ObservableCollection<string> ProcessedFiles { get; private set; }

        private readonly BackgroundWorker _backgroundWorker;
        private bool _needStop;

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
            get { return new ActionCommand(o => StartProcessing(), o => !ProcessStarted); }
        }

        public ActionCommand StopProcessingCommand
        {
            get { return new ActionCommand(o => StopProcessing(), o => ProcessStarted); }
        }

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
            _useSubFolder = Convert.ToBoolean(ConfigurationManager.AppSettings["UseSubFolder"]);
            _createByMonth = Convert.ToBoolean(ConfigurationManager.AppSettings["CreateByMonth"]);
            _savePath = ConfigurationManager.AppSettings["SavePath"];
        }

        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Сортировка завершена.", Application.Current.MainWindow.Title, MessageBoxButton.OK,
                MessageBoxImage.Information);
            TaskbarItemProgressState = TaskbarItemProgressState.None;
            OnPropertyChanged("TaskbarItemProgressState");
            ProcessValue = 0;
            OnPropertyChanged("ProcessValue");
            _fileCount = 0;
            OnPropertyChanged("FileCount");
        }

        private void StartProcessing()
        {
            TaskbarItemProgressState = TaskbarItemProgressState.Indeterminate;
            OnPropertyChanged("TaskbarItemProgressState");
            _backgroundWorker.RunWorkerAsync();
        }

        private void StopProcessing()
        {
            _needStop = true;
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

        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                TaskbarItemProgressState = TaskbarItemProgressState.Normal;
                OnPropertyChanged("TaskbarItemProgressState");
                OnPropertyChanged("FileCount");
                return;
            }
            ProcessValue = e.ProgressPercentage;
            OnPropertyChanged("ProcessValue");
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            _needStop = false;
            var dirInfo = new DirectoryInfo(PhotoPath);
            var files = dirInfo.GetFiles("*.jpg", SearchOption.AllDirectories);
            _fileCount = files.Length;
            _backgroundWorker.ReportProgress(0);
            for (var i = 0; i < files.Length; i++)
            {
                if (_needStop)
                    break;
                var folderName = GetFolderName(files[i].FullName);
                var path = Path.Combine(SavePath, folderName);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.Move(files[i].FullName, Path.Combine(path, files[i].Name));
                _backgroundWorker.ReportProgress(i + 1);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
                var decoder = BitmapDecoder.Create(photo, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
                var bitmapMetadata = (BitmapMetadata)decoder.Frames[0].Metadata;
                if (bitmapMetadata != null)
                {
                    var dt = Convert.ToDateTime(bitmapMetadata.DateTaken);
                    photo.Flush();
                    photo.Close();
                    return _createByMonth ? dt.ToString("MM.yyyy") : dt.ToString("yyyy");
                }
                photo.Flush();
                photo.Close();
                var fi = new FileInfo(filePath);
                return _createByMonth ? fi.CreationTime.ToString("MM.yyyy") : fi.CreationTime.ToString("yyyy");
            }
        }
    }
}
