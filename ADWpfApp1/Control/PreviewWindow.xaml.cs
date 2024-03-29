﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ADWpfApp1
{
    public partial class PreviewWindow : Window
    {
        #region PInvoke

        const int GWL_EXSTYLE = -20;
        const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("User32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("User32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion

        public Action OpenImageAction;

        public PreviewWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }

        public void SetShow()
        {
            this.Opacity = 1;
            Rect workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;
        }

        public void SetHide()
        {
            this.Opacity = 0;
            this.Left = -10000;
            this.Top = -10000;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenImageAction?.Invoke();
        }
    }
}
