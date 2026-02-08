using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Crosshair_MY
{
    public partial class MainWindow : Window
    {
        private const int WM_HOTKEY = 0x0312;

        private const int HOTKEY_TOGGLE = 9001;
        private const int HOTKEY_EXIT = 9002;
        private const int HOTKEY_STYLE = 9003;
        private const int HOTKEY_SIZE = 9004;
        private const int HOTKEY_COLOR = 9005;

        private readonly Brush[] _colors =
        {
            Brushes.Red, Brushes.Lime, Brushes.Cyan, Brushes.Yellow, Brushes.Magenta, Brushes.White
        };

        private readonly string[] _colorNames = { "Red", "Green", "Cyan", "Yellow", "Magenta", "White" };
        private readonly string[] _styleNames = { "Classic +", "Dot", "Circle", "CS Style", "Valorant" };

        private int _colorIndex;
        public readonly CrosshairSettings _settings;
        public HotkeyOverlay? _overlay; // ControlWindow görsün deyə public etdik

        public MainWindow()
        {
            InitializeComponent();

            // === BAŞLANĞIC DƏYƏRLƏRİ (Daha kiçik və səliqəli) ===
            _settings = new CrosshairSettings
            {
                Length = 6,          // Uzunluq: 20-dən 6-ya endirdik
                Thickness = 2,       // Qalınlıq: İncə olsun
                Gap = 4,             // Boşluq: Sıx olsun
                DotSize = 4,         // Nöqtə: Balaca olsun
                Opacity = 1.0,       // Tam görünən
                OutlineThickness = 1, // 1px qara kontur (daha aydın görünür)
                Color = Brushes.Lime, // Default Yaşıl
                Style = CrosshairStyle.ClassicPlus
            };

            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RegisterHotkeys();

            _overlay = new HotkeyOverlay
            {
                Left = SystemParameters.PrimaryScreenWidth - 280,
                Top = 20
            };
            _overlay.Show();

            _colorIndex = Array.IndexOf(_colors, _settings.Color);
            if (_colorIndex < 0) _colorIndex = 1; // Default Green

            DrawCrosshair(_settings);
            OpenControlPanel();
        }

        // ================= HOTKEYS & ACTIONS =================
        private void RegisterHotkeys()
        {
            var helper = new WindowInteropHelper(this);
            var source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);

            RegisterHotKey(helper.Handle, HOTKEY_TOGGLE, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F2));
            RegisterHotKey(helper.Handle, HOTKEY_EXIT, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F3));
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_TOGGLE);
            UnregisterHotKey(helper.Handle, HOTKEY_EXIT);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WM_HOTKEY) return IntPtr.Zero;

            switch (wParam.ToInt32())
            {
                case HOTKEY_TOGGLE: ToggleOverlay(); break;
                case HOTKEY_EXIT: Application.Current.Shutdown(); break;
            }
            handled = true;
            return IntPtr.Zero;
        }

        private void ToggleOverlay()
        {
            if (_overlay == null) return;
            _overlay.Visibility = _overlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        // ================= RENDER (ÇƏKİM) SİSTEMİ =================
        public void DrawCrosshair(CrosshairSettings s)
        {
            RootCanvas.Children.Clear();

            double w = RootCanvas.ActualWidth > 0 ? RootCanvas.ActualWidth : 200;
            double h = RootCanvas.ActualHeight > 0 ? RootCanvas.ActualHeight : 200;
            double cx = w / 2;
            double cy = h / 2;

            switch (s.Style)
            {
                case CrosshairStyle.ClassicPlus:
                    DrawLines(cx, cy, s);
                    DrawDot(cx, cy, s.DotSize, s);
                    break;
                case CrosshairStyle.Dot:
                    DrawDot(cx, cy, s.DotSize, s);
                    break;
                case CrosshairStyle.Circle:
                    DrawCircle(cx, cy, s);
                    break;
                case CrosshairStyle.CSStyle:
                    DrawLines(cx, cy, s);
                    break;
                case CrosshairStyle.ValorantStyle:
                    DrawLines(cx, cy, s);
                    DrawDot(cx, cy, s.DotSize - 1, s);
                    break;
            }
        }

        private void DrawLines(double cx, double cy, CrosshairSettings s)
        {
            // Outline (Qara Kontur) varsa, əvvəlcə onu çəkirik (biraz qalın)
            if (s.OutlineThickness > 0)
            {
                var outlineBrush = Brushes.Black;
                double os = s.OutlineThickness;

                // Yuxarı
                DrawRect(cx - (s.Thickness / 2) - os, cy - s.Gap - s.Length - os, s.Thickness + (os * 2), s.Length + (os * 2), outlineBrush, s.Opacity);
                // Aşağı
                DrawRect(cx - (s.Thickness / 2) - os, cy + s.Gap - os, s.Thickness + (os * 2), s.Length + (os * 2), outlineBrush, s.Opacity);
                // Sol
                DrawRect(cx - s.Gap - s.Length - os, cy - (s.Thickness / 2) - os, s.Length + (os * 2), s.Thickness + (os * 2), outlineBrush, s.Opacity);
                // Sağ
                DrawRect(cx + s.Gap - os, cy - (s.Thickness / 2) - os, s.Length + (os * 2), s.Thickness + (os * 2), outlineBrush, s.Opacity);
            }

            // Əsas Rəngli Xətlər
            DrawRect(cx - s.Thickness / 2, cy - s.Gap - s.Length, s.Thickness, s.Length, s.Color, s.Opacity); // Yuxarı
            DrawRect(cx - s.Thickness / 2, cy + s.Gap, s.Thickness, s.Length, s.Color, s.Opacity);             // Aşağı
            DrawRect(cx - s.Gap - s.Length, cy - s.Thickness / 2, s.Length, s.Thickness, s.Color, s.Opacity);  // Sol
            DrawRect(cx + s.Gap, cy - s.Thickness / 2, s.Length, s.Thickness, s.Color, s.Opacity);             // Sağ
        }

        private void DrawDot(double cx, double cy, double size, CrosshairSettings s)
        {
            // Outline Dot
            if (s.OutlineThickness > 0)
            {
                var outlineDot = new Ellipse { Width = size + 2, Height = size + 2, Fill = Brushes.Black, Opacity = s.Opacity };
                Canvas.SetLeft(outlineDot, cx - (size + 2) / 2);
                Canvas.SetTop(outlineDot, cy - (size + 2) / 2);
                RootCanvas.Children.Add(outlineDot);
            }

            var dot = new Ellipse { Width = size, Height = size, Fill = s.Color, Opacity = s.Opacity };
            Canvas.SetLeft(dot, cx - size / 2);
            Canvas.SetTop(dot, cy - size / 2);
            RootCanvas.Children.Add(dot);
        }

        private void DrawCircle(double cx, double cy, CrosshairSettings s)
        {
            if (s.OutlineThickness > 0)
            {
                var outCircle = new Ellipse
                {
                    Width = (s.Length * 2) + 2,
                    Height = (s.Length * 2) + 2,
                    Stroke = Brushes.Black,
                    StrokeThickness = s.Thickness + 2,
                    Opacity = s.Opacity
                };
                Canvas.SetLeft(outCircle, cx - (s.Length * 2 + 2) / 2);
                Canvas.SetTop(outCircle, cy - (s.Length * 2 + 2) / 2);
                RootCanvas.Children.Add(outCircle);
            }

            var circle = new Ellipse
            {
                Width = s.Length * 2,
                Height = s.Length * 2,
                Stroke = s.Color,
                StrokeThickness = s.Thickness,
                Opacity = s.Opacity
            };
            Canvas.SetLeft(circle, cx - s.Length);
            Canvas.SetTop(circle, cy - s.Length);
            RootCanvas.Children.Add(circle);
        }

        private void DrawRect(double x, double y, double w, double h, Brush color, double opacity)
        {
            var r = new Rectangle { Width = w, Height = h, Fill = color, Opacity = opacity };
            Canvas.SetLeft(r, x);
            Canvas.SetTop(r, y);
            RootCanvas.Children.Add(r);
        }

        private void OpenControlPanel()
        {
            var panel = new ControlWindow(this);
            panel.Show();
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}