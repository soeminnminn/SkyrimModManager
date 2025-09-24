using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BsaBrowser.Commons
{
    public class ClipboardChangedEventArgs : EventArgs
    {
        public readonly IDataObject DataObject;

        public ClipboardChangedEventArgs(IDataObject dataObject)
        {
            DataObject = dataObject;
        }
    }

    internal class ClipboardHook : IDisposable
    {
        #region PInvoke
        //private const int GWL_WNDPROC = -4;

        //// P/Invoke for window subclassing
        //private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        //[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int nIndex);

        //[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_DRAWCLIPBOARD = 0x308;
        private const int WM_CHANGECBCHAIN = 0x030D;

        [DllImport("User32.dll")]
        private static extern nint SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);
        #endregion

        #region Varaibles
        private readonly IntPtr hWnd;
        private HwndSource hwndSource = null;
        private HwndSourceHook hwndSourceHook = null;
        private IntPtr nextClipboardViewer = IntPtr.Zero;

        /// <summary>
        /// Clipboard contents changed.
        /// </summary>
        public event EventHandler<ClipboardChangedEventArgs> ClipboardChanged;
        #endregion

        #region Constructors
        public ClipboardHook(Window window) 
        {
            // Create an instance of WindowInteropHelper for the current window
            WindowInteropHelper helper = new WindowInteropHelper(window);

            // Get the window handle (HWND)
            hWnd = helper.Handle;

            if (hWnd != IntPtr.Zero)
            {
                nextClipboardViewer = (IntPtr)SetClipboardViewer(hWnd);

                hwndSource = HwndSource.FromHwnd(hWnd);
                hwndSourceHook = new HwndSourceHook(WndProc);
                hwndSource.AddHook(hwndSourceHook);
            }
        }
        #endregion

        #region Properties
        public IntPtr Handle
        {
            get => hWnd;
        }
        #endregion

        #region Methods
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    OnClipboardChanged();
                    SendMessage(nextClipboardViewer, msg, wParam, lParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (wParam == nextClipboardViewer)
                        nextClipboardViewer = lParam;
                    else
                        SendMessage(nextClipboardViewer, msg, wParam, lParam);
                    break;

                default:
                    break;
            }

            handled = false;
            return IntPtr.Zero;
        }

        private void OnClipboardChanged()
        {
            try
            {
                IDataObject iData = Clipboard.GetDataObject();
                if (ClipboardChanged != null)
                {
                    ClipboardChanged(this, new ClipboardChangedEventArgs(iData));
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            try
            {
                ChangeClipboardChain(hWnd, nextClipboardViewer);

                if (hwndSource != null && hwndSourceHook != null)
                {
                    hwndSource.RemoveHook(hwndSourceHook);
                    hwndSourceHook = null;
                }
            }
            catch
            { }
        }

        public static bool HasSameData(IDataObject dataObj, string format)
        {
            if (dataObj != null && Clipboard.ContainsData(format))
            {
                var data = dataObj.GetData(format);                
                var clipData = Clipboard.GetData(format);
                if (data == null && clipData == null) return true;

                if (data is string dataStr && clipData is string clipDataStr)
                {
                    return dataStr == clipDataStr;
                }
                else if (data is string[] dataArr && clipData is string[] clipDataArr)
                {
                    if (dataArr.Length == clipDataArr.Length)
                    {
                        bool same = true;
                        for (var i = 0; i < dataArr.Length; i++)
                        {
                            same = same && dataArr[i] == clipDataArr[i];
                        }

                        return same;
                    }

                    return dataArr == clipDataArr;
                }
                else
                {
                    return data == clipData;
                }
            }

            return false;
        }
        #endregion
    }
}
