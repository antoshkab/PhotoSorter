using System;
using System.Windows.Forms;

namespace PhotoSorter
{
    internal class OldWindow : IWin32Window
    {
        readonly IntPtr _handle;
        public OldWindow(IntPtr handle)
        {
            _handle = handle;
        }

        #region IWin32Window Members
        IntPtr IWin32Window.Handle
        {
            get { return _handle; }
        }
        #endregion
    }
}
