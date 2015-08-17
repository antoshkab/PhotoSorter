using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoSorter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            var isNewInstance = false;
            _mutex = new Mutex(true, "PhotoSorter", out isNewInstance);
            if (!isNewInstance)
            {
                Shutdown();
                return;
            }
            base.OnStartup(e);
        }
    }
}
