using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Crosshair_MY
{
    public partial class HotkeyOverlay : Window
    {
        public HotkeyOverlay()
        {
            InitializeComponent();
            Loaded += EnableClickThrough;
        }

        // 🔹 MAIN WINDOW-dan çağırılacaq
        public void UpdateShape(string shapeName)
        {
            F4Text.Text = $"F4   Shape: {shapeName}";
        }

        public void UpdateSize(int size)
        {
            F5Text.Text = $"F5   Size: {size}";
        }

        public void UpdateColor(string colorName)
        {
            F6Text.Text = $"F6   Color: {colorName}";
        }

        // 🔹 Click-through
        private void EnableClickThrough(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
