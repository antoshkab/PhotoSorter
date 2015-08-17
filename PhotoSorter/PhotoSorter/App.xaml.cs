using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoSorter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var isNewInstance = false;
            _mutex = new Mutex(true, string.Format("{0}/{1}", assembly.GetType().GUID, assembly.GetType().FullName),
                out isNewInstance);
            if (!isNewInstance)
            {
                _mutex = null;
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Dispose();
            base.OnExit(e);
        }

        private void Dispose(Boolean disposing)
        {
            if (disposing && (_mutex != null))
            {
                _mutex.ReleaseMutex();
                _mutex.Close();
                _mutex = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
