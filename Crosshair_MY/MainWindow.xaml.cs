#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

// =========================================================
// 🛠️ MƏCBURİ TƏYİNATLAR
// =========================================================
using Canvas = System.Windows.Controls.Canvas;
using Image = System.Windows.Controls.Image;
using Rectangle = System.Windows.Shapes.Rectangle;
using Ellipse = System.Windows.Shapes.Ellipse;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using RotateTransform = System.Windows.Media.RotateTransform;
using TranslateTransform = System.Windows.Media.TranslateTransform;
using TransformGroup = System.Windows.Media.TransformGroup;
// =========================================================

namespace Crosshair_MY
{
    public partial class MainWindow : Window
    {
        // HOTKEY ID-ləri
        private const int WM_HOTKEY = 0x0312;
        private const int HK_TOGGLE_GUIDE = 9001; // F2
        private const int HK_EXIT = 9002;         // F3
        private const int HK_RESET = 9003;        // F4
        private const int HK_CLEAN_RAM = 9004;    // F5
        private const int HK_OPEN_EDITOR = 9005;  // F6
        private const int HK_NEXT_STYLE = 9006;   // Z
        private const int HK_NEXT_COLOR = 9007;   // X
        private const int HK_CYCLE_SIZE = 9008;   // C

        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public CrosshairSettings _settings = new CrosshairSettings();
        public HotkeyOverlay _hotkeyWindow;
        public ControlWindow _editorWindow;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // 🛠️ VACİB DÜZƏLİŞ: Bu sətirlər düymə mesajlarını qəbul etmək üçün mütləqdir.
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(HwndHook);

            RegisterHotkeys();

            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);

            ShowHotkeyOverlay();
            DrawCrosshair(_settings);
        }

        private void ShowHotkeyOverlay()
        {
            _hotkeyWindow = new HotkeyOverlay();
            _hotkeyWindow.Left = SystemParameters.PrimaryScreenWidth - _hotkeyWindow.Width - 20;
            _hotkeyWindow.Top = 20;
            _hotkeyWindow.Owner = this;
            _hotkeyWindow.Show();
        }

        // 🛠️ DÜYMƏLƏRİN FUNKSİYALARI
        private void CycleStyle()
        {
            if (_settings.Layers.Count == 0) return;
            var layer = _settings.Layers[0];
            int nextType = ((int)layer.Type + 1) % Enum.GetValues(typeof(LayerType)).Length;
            layer.Type = (LayerType)nextType;
            DrawCrosshair(_settings);
        }

        private void CycleColor()
        {
            if (_settings.Layers.Count == 0) return;
            string[] colors = { "#FF00FF00", "#FFFF0000", "#FF0000FF", "#FFFFFFFF", "#FFFFFF00", "#FFFF00FF" };
            var layer = _settings.Layers[0];
            int currentIndex = Array.IndexOf(colors, layer.ColorHex);
            layer.ColorHex = colors[(currentIndex + 1) % colors.Length];
            DrawCrosshair(_settings);
        }

        private void CycleSize()
        {
            if (_settings.Layers.Count == 0) return;
            var layer = _settings.Layers[0];
            layer.Length = (layer.Length >= 40) ? 5 : layer.Length + 5;
            DrawCrosshair(_settings);
        }

        private void CleanRAM()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void DrawCrosshair(CrosshairSettings s)
        {
            if (_overlay == null) return;
            Canvas rootCanvas = null;
            if (_overlay.Children.Count > 0 && _overlay.Children[0] is Canvas)
                rootCanvas = _overlay.Children[0] as Canvas;

            if (rootCanvas == null)
            {
                rootCanvas = new Canvas { Width = 200, Height = 200 };
                _overlay.Children.Add(rootCanvas);
            }

            rootCanvas.Children.Clear();
            double cx = 100; double cy = 100;

            foreach (var layer in s.Layers)
            {
                if (!layer.Enabled) continue;
                Canvas layerCanvas = new Canvas { Width = 200, Height = 200 };
                Brush brush = (Brush)new BrushConverter().ConvertFrom(layer.ColorHex);
                double finalRotation = layer.Rotation;

                switch (layer.Type)
                {
                    case LayerType.Cross:
                        DrawRect(layerCanvas, cx - layer.Thickness / 2, cy - layer.Gap - layer.Length, layer.Thickness, layer.Length, brush, layer.Opacity, layer);
                        DrawRect(layerCanvas, cx - layer.Thickness / 2, cy + layer.Gap, layer.Thickness, layer.Length, brush, layer.Opacity, layer);
                        DrawRect(layerCanvas, cx - layer.Gap - layer.Length, cy - layer.Thickness / 2, layer.Length, layer.Thickness, brush, layer.Opacity, layer);
                        DrawRect(layerCanvas, cx + layer.Gap, cy - layer.Thickness / 2, layer.Length, layer.Thickness, brush, layer.Opacity, layer);
                        break;
                    case LayerType.Dot:
                        var dot = new Ellipse { Width = layer.Thickness * 2, Height = layer.Thickness * 2, Fill = brush, Opacity = layer.Opacity };
                        Canvas.SetLeft(dot, cx - layer.Thickness); Canvas.SetTop(dot, cy - layer.Thickness);
                        layerCanvas.Children.Add(dot);
                        break;
                    case LayerType.Circle:
                        var circle = new Ellipse { Width = layer.Length * 2, Height = layer.Length * 2, Stroke = brush, StrokeThickness = layer.Thickness, Opacity = layer.Opacity };
                        Canvas.SetLeft(circle, cx - layer.Length); Canvas.SetTop(circle, cy - layer.Length);
                        layerCanvas.Children.Add(circle);
                        break;
                    case LayerType.Square:
                        var sq = new Rectangle { Width = layer.Length * 2, Height = layer.Length * 2, Stroke = brush, StrokeThickness = layer.Thickness, Opacity = layer.Opacity };
                        Canvas.SetLeft(sq, cx - layer.Length); Canvas.SetTop(sq, cy - layer.Length);
                        layerCanvas.Children.Add(sq);
                        break;

                    // 🛠️ YENİ ƏLAVƏ EDİLDİ: PNG Şəkillərini çəkmək üçün məntiq
                    case LayerType.Image:
                        if (!string.IsNullOrEmpty(layer.ImagePath) && System.IO.File.Exists(layer.ImagePath))
                        {
                            try
                            {
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(layer.ImagePath, UriKind.Absolute);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Şəklin yoxa çıxmaması üçün vacibdir
                                bitmap.EndInit();

                                var img = new Image
                                {
                                    Source = bitmap,
                                    Width = layer.Length * 2,
                                    Height = layer.Length * 2,
                                    Opacity = layer.Opacity,
                                    Stretch = System.Windows.Media.Stretch.Uniform
                                };

                                Canvas.SetLeft(img, cx - layer.Length);
                                Canvas.SetTop(img, cy - layer.Length);
                                layerCanvas.Children.Add(img);
                            }
                            catch { }
                        }
                        break;
                }

                TransformGroup tg = new TransformGroup();
                tg.Children.Add(new RotateTransform(finalRotation, cx, cy));
                tg.Children.Add(new TranslateTransform(layer.OffsetX, layer.OffsetY));
                layerCanvas.RenderTransform = tg;
                rootCanvas.Children.Add(layerCanvas);
            }
        }

        private void DrawRect(Canvas c, double x, double y, double w, double h, Brush color, double opacity, CrosshairLayer layer)
        {
            if (layer.HasOutline)
            {
                var outRect = new Rectangle { Width = w + (layer.OutlineThickness * 2), Height = h + (layer.OutlineThickness * 2), Fill = Brushes.Black, Opacity = opacity };
                Canvas.SetLeft(outRect, x - layer.OutlineThickness); Canvas.SetTop(outRect, y - layer.OutlineThickness); c.Children.Add(outRect);
            }
            var r = new Rectangle { Width = w, Height = h, Fill = color, Opacity = opacity };
            Canvas.SetLeft(r, x); Canvas.SetTop(r, y); c.Children.Add(r);
        }

        private void RegisterHotkeys()
        {
            var helper = new WindowInteropHelper(this);
            IntPtr handle = helper.Handle;
            RegisterHotKey(handle, HK_TOGGLE_GUIDE, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F2));
            RegisterHotKey(handle, HK_EXIT, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F3));
            RegisterHotKey(handle, HK_RESET, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F4));
            RegisterHotKey(handle, HK_CLEAN_RAM, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F5));
            RegisterHotKey(handle, HK_OPEN_EDITOR, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F6));
            RegisterHotKey(handle, HK_NEXT_STYLE, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.Z));
            RegisterHotKey(handle, HK_NEXT_COLOR, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.X));
            RegisterHotKey(handle, HK_CYCLE_SIZE, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.C));
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            for (int i = 9001; i <= 9008; i++) UnregisterHotKey(helper.Handle, i);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                switch (wParam.ToInt32())
                {
                    case HK_TOGGLE_GUIDE:
                        if (_hotkeyWindow != null) _hotkeyWindow.Visibility = _hotkeyWindow.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                        break;
                    case HK_EXIT:
                        System.Windows.Application.Current.Shutdown();
                        break;
                    case HK_RESET:
                        if (_settings.Layers.Count > 0) { _settings.Layers[0].Length = 10; _settings.Layers[0].Thickness = 2; DrawCrosshair(_settings); }
                        break;
                    case HK_CLEAN_RAM:
                        CleanRAM();
                        break;
                    case HK_OPEN_EDITOR:
                        if (_editorWindow == null || !_editorWindow.IsLoaded)
                        {
                            _editorWindow = new ControlWindow(this);
                        }
                        _editorWindow.Show();
                        _editorWindow.Activate();
                        break;

                    case HK_NEXT_STYLE:
                        if (_hotkeyWindow != null && _hotkeyWindow.Visibility == Visibility.Visible) CycleStyle();
                        break;
                    case HK_NEXT_COLOR:
                        if (_hotkeyWindow != null && _hotkeyWindow.Visibility == Visibility.Visible) CycleColor();
                        break;
                    case HK_CYCLE_SIZE:
                        if (_hotkeyWindow != null && _hotkeyWindow.Visibility == Visibility.Visible) CycleSize();
                        break;
                }
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}