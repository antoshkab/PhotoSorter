using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;

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
            }
        }

        public bool UseSubFolder { get; set; }

        public string CreateByMonth { get; set; }

        private string _savePath;

        public string SavePath
        {
            get { return _savePath; }
            private set
            {
                _savePath = value; 
                OnPropertyChanged();
            }
        }

        private TaskbarItemProgressState _progressState;

        public TaskbarItemProgressState ProgressState
        {
            get { return _progressState; }
            private set
            {
                _progressState = value; 
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ProcessedFiles { get; private set; }

        private readonly BackgroundWorker _backgroundWorker;

        public ActionCommand SelectPhotoPathCommand
        {
            get { throw new NotImplementedException();}
        }

        public ActionCommand SelectSavePathCommand
        {
            get { throw new NotImplementedException();}
        }

        public ActionCommand StartProcessingCommand
        {
            get { throw new NotImplementedException();}
        }

        public ActionCommand StopProcessingCommand
        {
            get { throw new NotImplementedException();}
        }

        public MainViewModel()
        {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            _backgroundWorker.ProgressChanged += BackgroundWorkerOnProgressChanged;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;
            ProcessedFiles = new ObservableCollection<string>();
        }

        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
