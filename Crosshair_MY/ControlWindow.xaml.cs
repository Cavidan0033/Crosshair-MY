#nullable disable

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// =========================================================
// 🛠️ ALIASLAR
// =========================================================
using Canvas = System.Windows.Controls.Canvas;
using Button = System.Windows.Controls.Button;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using TextBox = System.Windows.Controls.TextBox;
using Rectangle = System.Windows.Shapes.Rectangle;
using Ellipse = System.Windows.Shapes.Ellipse;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using BrushConverter = System.Windows.Media.BrushConverter;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

// WinForms
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace Crosshair_MY
{
    public partial class ControlWindow : Window
    {
        private MainWindow _main;
        private bool _isInitialized = false;
        private List<ProfileData> _profiles = new List<ProfileData>();
        private string _profilePath = "profiles.json";
        private DispatcherTimer _sessionTimer = new DispatcherTimer();
        private DateTime _startTime = DateTime.Now;
        private const string CurrentVersion = "2.5 (Stable)";

        private CrosshairLayer _selectedLayer = null;
        private WinForms.NotifyIcon _notifyIcon;
        private double _currentHue = 0;

        public ControlWindow(MainWindow main)
        {
            InitializeComponent();
            _main = main;
            _startTime = DateTime.Now;
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }

            InitializeTrayIcon();
            LoadSettings();
            LoadProfilesFromFile();
            SetupDashboardTimers();

            // 🛠️ WORKSHOP İNİTİALİZATİON
            InitializeWorkshop();

            if (ChkStartup != null) ChkStartup.IsChecked = IsStartupEnabled();
            if (TxtAppVersion != null) TxtAppVersion.Text = $"Crosshair MY v{CurrentVersion}";

            this.Closing += ControlWindow_Closing;
            SwitchView("Editor");
            UpdateDashboardInfo();
            _isInitialized = true;
        }

        private void UpdateRamUsage()
        {
            long mem = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
            if (TxtRamUsage != null) TxtRamUsage.Text = mem + " MB";
        }

        // 🔹 WORKSHOP (EMALATXANA) MƏNTİQİ
        private void InitializeWorkshop()
        {
            if (DrawingGrid == null) return;
            DrawingGrid.Children.Clear();

            for (int i = 0; i < 1024; i++)
            {
                var pixel = new Rectangle
                {
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    StrokeThickness = 0.2
                };

                pixel.MouseEnter += (s, e) =>
                {
                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                        ((Rectangle)s).Fill = ColorPreview.Background;
                    else if (Mouse.RightButton == MouseButtonState.Pressed)
                        ((Rectangle)s).Fill = Brushes.Transparent;
                };

                pixel.MouseDown += (s, e) =>
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                        ((Rectangle)s).Fill = ColorPreview.Background;
                    else if (e.RightButton == MouseButtonState.Pressed)
                        ((Rectangle)s).Fill = Brushes.Transparent;
                };

                DrawingGrid.Children.Add(pixel);
            }
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            foreach (Rectangle pixel in DrawingGrid.Children) pixel.Fill = Brushes.Transparent;
        }

        private void ImportToWorkshop_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new Drawing.Bitmap(dlg.FileName);
                    var resized = new Drawing.Bitmap(bitmap, new Drawing.Size(32, 32));

                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 32; x++)
                        {
                            var color = resized.GetPixel(x, y);
                            int index = y * 32 + x;
                            if (index < DrawingGrid.Children.Count)
                            {
                                var rect = (Rectangle)DrawingGrid.Children[index];
                                if (color.A < 10) rect.Fill = Brushes.Transparent;
                                else rect.Fill = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
                            }
                        }
                    }
                }
                catch { ShowCustomMessage("ERROR", "Şəkil skan edilə bilmədi!"); }
            }
        }

        private void ApplyWorkshop_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLayer == null) return;

            string tempPath = Path.Combine(Path.GetTempPath(), "custom_crosshair.png");

            try
            {
                int size = 32;
                var target = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);

                // ✅ DÜZƏLİŞ: StrokeThickness xətasını həll etmək üçün dövr istifadə olunur
                foreach (var child in DrawingGrid.Children)
                {
                    if (child is Rectangle rect) rect.StrokeThickness = 0;
                }

                target.Render(DrawingGrid);

                // ✅ DÜZƏLİŞ: Rəndərdən sonra xətləri geri qaytarırıq
                foreach (var child in DrawingGrid.Children)
                {
                    if (child is Rectangle rect) rect.StrokeThickness = 0.2;
                }

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(target));
                using (var stream = File.Create(tempPath)) encoder.Save(stream);

                _selectedLayer.ImagePath = tempPath;
                _selectedLayer.Type = LayerType.Image;
                StyleCombo.SelectedIndex = 6;
                _main.DrawCrosshair(_main._settings);
                ShowCustomMessage("SUCCESS", "Workshop Nişangahı Tətbiq Edildi!");
            }
            catch (Exception ex)
            {
                // ✅ DÜZƏLİŞ: MessageBox ambiguity xətası tam adreslə həll olundu
                System.Windows.MessageBox.Show("Xəta baş verdi: " + ex.Message);
            }
        }

        // 🔹 COLOR PICKER LOGIC
        private void ColorPreview_MouseUp(object sender, MouseButtonEventArgs e) { if (ColorPalettePopup != null) ColorPalettePopup.IsOpen = !ColorPalettePopup.IsOpen; e.Handled = true; }
        private void HueSlider_MouseDown(object sender, MouseButtonEventArgs e) => UpdateHue(e);
        private void HueSlider_MouseMove(object sender, MouseEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) UpdateHue(e); }

        private void ChkOutline_Click(object sender, RoutedEventArgs e) => Settings_Changed(null, null);

        private void UpdateHue(MouseEventArgs e)
        {
            if (HueSlider == null || CanvasBackground == null) return;
            double width = HueSlider.ActualWidth;
            if (width <= 0) return;
            Point mousePos = e.GetPosition(HueSlider);
            _currentHue = (mousePos.X / width) * 360;
            if (_currentHue < 0) _currentHue = 0; if (_currentHue > 360) _currentHue = 360;
            CanvasBackground.Fill = new SolidColorBrush(ColorFromAhsv(255, _currentHue, 1, 1));
            UpdateFinalColor(e.GetPosition(ColorCanvas));
        }

        private void ColorCanvas_MouseDown(object sender, MouseButtonEventArgs e) => UpdateFinalColor(e.GetPosition(ColorCanvas));
        private void ColorCanvas_MouseMove(object sender, MouseEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) UpdateFinalColor(e.GetPosition(ColorCanvas)); }

        private void UpdateFinalColor(Point p)
        {
            if (ColorCanvas == null || ColorCursor == null) return;
            double w = ColorCanvas.ActualWidth; double h = ColorCanvas.ActualHeight;
            if (w <= 0 || h <= 0) return;
            double s = p.X / w; double v = 1 - (p.Y / h);
            if (s < 0) s = 0; if (s > 1) s = 1; if (v < 0) v = 0; if (v > 1) v = 1;
            Canvas.SetLeft(ColorCursor, s * w); Canvas.SetTop(ColorCursor, (1 - v) * h);
            Color finalColor = ColorFromAhsv(255, _currentHue, s, v);
            string hex = $"#{finalColor.A:X2}{finalColor.R:X2}{finalColor.G:X2}{finalColor.B:X2}";
            if (TxtHexColor != null) TxtHexColor.Text = hex;
        }

        private Color ColorFromAhsv(byte a, double h, double s, double v)
        {
            double r = 0, g = 0, b = 0;
            if (s == 0) { r = g = b = v; }
            else
            {
                double sector = h / 60.0; int i = (int)Math.Floor(sector); double f = sector - i;
                double p = v * (1 - s); double q = v * (1 - s * f); double t = v * (1 - s * (1 - f));
                switch (i % 6)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    case 5: r = v; g = p; b = q; break;
                }
            }
            return Color.FromArgb(a, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private void PaletteColor_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.Background is SolidColorBrush brush) { if (TxtHexColor != null) TxtHexColor.Text = brush.Color.ToString(); if (ColorPalettePopup != null) ColorPalettePopup.IsOpen = false; } }

        private void TxtHexColor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_isInitialized || _selectedLayer == null || TxtHexColor == null) return;
            string hex = TxtHexColor.Text;
            if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#")) { hex = "#" + hex.Replace("#", ""); _isInitialized = false; TxtHexColor.Text = hex; _isInitialized = true; TxtHexColor.CaretIndex = hex.Length; }
            if (hex.Length == 7 || hex.Length == 9) { try { var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(hex); if (brush != null) { _selectedLayer.ColorHex = hex; ColorPreview.Background = brush; _main.DrawCrosshair(_main._settings); } } catch { } }
        }

        private void UpdateLabels()
        {
            if (!_isInitialized) return;
            if (LblLength != null && LengthSlider != null) LblLength.Text = LengthSlider.Value.ToString("0");
            if (LblThick != null && ThickSlider != null) LblThick.Text = ThickSlider.Value.ToString("0");
            if (LblGap != null && GapSlider != null) LblGap.Text = GapSlider.Value.ToString("0");
            if (LblOpacity != null && OpacitySlider != null) LblOpacity.Text = OpacitySlider.Value.ToString("0") + "%";
            if (LblRotation != null && RotationSlider != null) LblRotation.Text = RotationSlider.Value.ToString("0") + "°";
            if (LblOutlineThick != null && OutlineSlider != null) LblOutlineThick.Text = OutlineSlider.Value.ToString("0.0");
        }

        private void UpdateUIFromLayer(CrosshairLayer layer)
        {
            if (layer == null) return; _isInitialized = false;
            if (LengthSlider != null) LengthSlider.Value = layer.Length;
            if (ThickSlider != null) ThickSlider.Value = layer.Thickness;
            if (GapSlider != null) GapSlider.Value = layer.Gap;
            if (OpacitySlider != null) OpacitySlider.Value = layer.Opacity * 100;
            if (RotationSlider != null) RotationSlider.Value = layer.Rotation;
            if (TxtHexColor != null) TxtHexColor.Text = layer.ColorHex;
            if (ColorPreview != null) ColorPreview.Background = layer.ColorBrush;
            if (StyleCombo != null)
            {
                StyleCombo.SelectedIndex = (int)layer.Type;
                if (BtnBrowseImage != null) BtnBrowseImage.Visibility = (layer.Type == LayerType.Image) ? Visibility.Visible : Visibility.Collapsed;
            }
            _isInitialized = true; UpdateLabels();
        }

        private void Settings_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized || _main == null || _selectedLayer == null) return;
            _selectedLayer.Length = LengthSlider.Value;
            _selectedLayer.Thickness = ThickSlider.Value;
            _selectedLayer.Gap = GapSlider.Value;
            _selectedLayer.Opacity = OpacitySlider.Value / 100.0;
            _selectedLayer.Rotation = RotationSlider.Value;

            if (OutlineSlider != null) _selectedLayer.OutlineThickness = OutlineSlider.Value;
            if (ChkOutline != null) _selectedLayer.HasOutline = ChkOutline.IsChecked ?? false;

            UpdateLabels();
            _main.DrawCrosshair(_main._settings);
        }

        // 🔹 SYSTEM & CLOSING
        private void ControlWindow_Closing(object sender, CancelEventArgs e) { e.Cancel = true; this.Hide(); }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new WinForms.NotifyIcon();
            try { _notifyIcon.Icon = new Drawing.Icon("logo.ico"); } catch { _notifyIcon.Icon = Drawing.SystemIcons.Application; }
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Crosshair MY";
            _notifyIcon.DoubleClick += (s, args) => { this.Show(); this.WindowState = WindowState.Normal; this.Activate(); };
            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (s, e) => { this.Show(); this.WindowState = WindowState.Normal; });
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => { _notifyIcon.Dispose(); System.Windows.Application.Current.Shutdown(); });
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Hide();
        private bool IsStartupEnabled() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) return key?.GetValue("CrosshairMY") != null; } catch { return false; } }
        private void ChkStartup_Changed(object sender, RoutedEventArgs e) { if (!_isInitialized) return; try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) { if (ChkStartup.IsChecked == true) { string path = Process.GetCurrentProcess().MainModule?.FileName; if (!string.IsNullOrEmpty(path)) key.SetValue("CrosshairMY", "\"" + path + "\""); } else key.DeleteValue("CrosshairMY", false); } } catch { } }
        private void RefreshLayerList() { if (ListLayers == null) return; ListLayers.Items.Clear(); int index = 1; foreach (var layer in _main._settings.Layers) ListLayers.Items.Add($"Layer {index++} ({layer.Type})"); if (_selectedLayer != null) { int selIndex = _main._settings.Layers.IndexOf(_selectedLayer); if (selIndex >= 0) ListLayers.SelectedIndex = selIndex; } }
        private void ListLayers_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (ListLayers.SelectedIndex < 0 || ListLayers.SelectedIndex >= _main._settings.Layers.Count) return; _selectedLayer = _main._settings.Layers[ListLayers.SelectedIndex]; UpdateUIFromLayer(_selectedLayer); }
        private void AddLayer_Click(object sender, RoutedEventArgs e) { var newLayer = new CrosshairLayer { Name = "New Layer", Type = LayerType.Cross, Length = 10, Thickness = 2 }; _main._settings.Layers.Add(newLayer); _selectedLayer = newLayer; RefreshLayerList(); UpdateUIFromLayer(newLayer); _main.DrawCrosshair(_main._settings); }
        private void RemoveLayer_Click(object sender, RoutedEventArgs e) { if (_selectedLayer != null && _main._settings.Layers.Count > 1) { _main._settings.Layers.Remove(_selectedLayer); _selectedLayer = _main._settings.Layers[0]; RefreshLayerList(); UpdateUIFromLayer(_selectedLayer); _main.DrawCrosshair(_main._settings); } }

        private void LoadSettings() { if (StyleCombo == null) return; StyleCombo.Items.Clear(); string[] styles = { "Cross (+)", "Circle (O)", "Dot (.)", "Square ([])", "X-Shape (x)", "Triangle (▲)", "Image (PNG)" }; foreach (var s in styles) StyleCombo.Items.Add(s); if (_main != null && _main._settings != null) { if (_main._settings.Layers.Count == 0) _main._settings.Layers.Add(new CrosshairLayer()); _selectedLayer = _main._settings.Layers[0]; RefreshLayerList(); UpdateUIFromLayer(_selectedLayer); } }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLayer == null) return;
            var dlg = new OpenFileDialog { DefaultExt = ".png", Filter = "Images|*.png;*.jpg;*.jpeg;*.ico" };
            if (dlg.ShowDialog() == true)
            {
                _selectedLayer.ImagePath = dlg.FileName;
                _selectedLayer.Type = LayerType.Image;
                _selectedLayer.Length = 50;
                UpdateUIFromLayer(_selectedLayer);
                _main.DrawCrosshair(_main._settings);
            }
        }

        private void StyleCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!_isInitialized || _selectedLayer == null) return; _selectedLayer.Type = (LayerType)StyleCombo.SelectedIndex; if (BtnBrowseImage != null) BtnBrowseImage.Visibility = (_selectedLayer.Type == LayerType.Image) ? Visibility.Visible : Visibility.Collapsed; RefreshLayerList(); _main.DrawCrosshair(_main._settings); }
        private void LoadProfilesFromFile() { if (File.Exists(_profilePath)) { try { var l = JsonSerializer.Deserialize<List<ProfileData>>(File.ReadAllText(_profilePath)); if (l != null) _profiles = l; } catch { } } ListProfiles.Items.Clear(); foreach (var p in _profiles) ListProfiles.Items.Add(p.Name); }
        private void ListProfiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (ListProfiles.SelectedIndex < 0) return; var p = _profiles[ListProfiles.SelectedIndex]; _main._settings.Layers.Clear(); foreach (var l in p.Layers) _main._settings.Layers.Add(new CrosshairLayer { Name = l.Name, Type = l.Type, Enabled = l.Enabled, Length = l.Length, Thickness = l.Thickness, Gap = l.Gap, Rotation = l.Rotation, ColorHex = l.ColorHex, Opacity = l.Opacity, ImagePath = l.ImagePath }); _selectedLayer = _main._settings.Layers[0]; RefreshLayerList(); UpdateUIFromLayer(_selectedLayer); _main.DrawCrosshair(_main._settings); UpdateDashboardInfo(); }
        private void SaveProfile_Click(object sender, RoutedEventArgs e) { string name = TxtProfileName.Text; if (string.IsNullOrWhiteSpace(name)) return; var newProfile = new ProfileData { Name = name }; foreach (var l in _main._settings.Layers) newProfile.Layers.Add(new CrosshairLayer { Name = l.Name, Type = l.Type, Enabled = l.Enabled, Length = l.Length, Thickness = l.Thickness, Gap = l.Gap, Rotation = l.Rotation, ColorHex = l.ColorHex, Opacity = l.Opacity, ImagePath = l.ImagePath }); _profiles.Add(newProfile); SaveProfilesToDisk(); LoadProfilesFromFile(); ShowCustomMessage("SUCCESS", "Profile Saved!"); }
        private void DeleteProfile_Click(object sender, RoutedEventArgs e) { if (ListProfiles.SelectedIndex >= 0) { _profiles.RemoveAt(ListProfiles.SelectedIndex); SaveProfilesToDisk(); LoadProfilesFromFile(); } }
        private void SaveProfilesToDisk() { try { File.WriteAllText(_profilePath, JsonSerializer.Serialize(_profiles)); } catch { } }
        private void ExportProfile_Click(object sender, RoutedEventArgs e) { if (ListProfiles.SelectedIndex < 0) return; string j = JsonSerializer.Serialize(_profiles[ListProfiles.SelectedIndex]); System.Windows.Clipboard.SetText("CMY:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(j))); ShowCustomMessage("COPIED", "Code copied!"); }
        private void ImportProfile_Click(object sender, RoutedEventArgs e) { try { string t = System.Windows.Clipboard.GetText(); if (!t.StartsWith("CMY:")) return; var p = JsonSerializer.Deserialize<ProfileData>(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(t.Substring(4)))); if (p != null) { p.Name += " (Import)"; _profiles.Add(p); SaveProfilesToDisk(); LoadProfilesFromFile(); } } catch { } }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2: Action_Toggle_Click(this, null); break;
                case Key.F3: System.Windows.Application.Current.Shutdown(); break;
                case Key.F4: Reset_Click(this, null); break;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e) { if (_selectedLayer != null) { LengthSlider.Value = 10; ThickSlider.Value = 2; GapSlider.Value = 4; OpacitySlider.Value = 100; RotationSlider.Value = 0; TxtHexColor.Text = "#FF00FF00"; } }
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.OriginalSource == ColorPreview) return; if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            // Əgər pəncərə böyüdülübsə, normal ölçüyə qaytar, yoxsa maksimizasiya et
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
        private void ChkMinimized_Changed(object sender, RoutedEventArgs e)
        {
            // 🛠️ VACİB: Pəncərə tam açılmadan (InitializeComponent bitmədən) 
            // bu kodun işləməsinə icazə vermirik. Xətanın qarşısını alan əsas hissə budur.
            if (!_isInitialized) return;

            if (ChkMinimized.IsChecked == true)
            {
                // Burada gələcəkdə proqramı kiçildilmiş halda başlatmaq ayarını yadda saxlayacaqsan
            }
        }
        private void SetupDashboardTimers()
        {
            _sessionTimer.Interval = TimeSpan.FromSeconds(1);
            _sessionTimer.Tick += (s, e) =>
            {
                if (TxtSessionTime != null) TxtSessionTime.Text = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");
                UpdateRamUsage();
            };
            _sessionTimer.Start();
        }

        private void UpdateDashboardInfo() { if (TxtResolution != null) TxtResolution.Text = $"{SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}"; }
        private void ShowCustomMessage(string title, string message) { MsgTitle.Text = title; MsgText.Text = message; CustomMessageOverlay.Visibility = Visibility.Visible; }
        private void BtnCloseMessage_Click(object sender, RoutedEventArgs e) => CustomMessageOverlay.Visibility = Visibility.Collapsed;
        private void Nav_Click(object sender, RoutedEventArgs e) { if (sender is Button btn) SwitchView(btn.Content.ToString()); }

        private void SwitchView(string view)
        {
            if (View_Essentials == null || View_Profiles == null || View_Maker == null) return;
            View_Essentials.Visibility = View_Editor.Visibility = View_Profiles.Visibility = View_Settings.Visibility = View_Maker.Visibility = Visibility.Collapsed;

            if (view == "Essentials") View_Essentials.Visibility = Visibility.Visible;
            else if (view == "Editor") View_Editor.Visibility = Visibility.Visible;
            else if (view == "Profiles") View_Profiles.Visibility = Visibility.Visible;
            else if (view == "Settings") View_Settings.Visibility = Visibility.Visible;
            else if (view == "Workshop") View_Maker.Visibility = Visibility.Visible;

            TxtPageTitle.Text = view.ToUpper();
        }

        private void Action_Toggle_Click(object sender, RoutedEventArgs e) { if (_main._overlay != null) { bool v = _main._overlay.Visibility == Visibility.Visible; _main._overlay.Visibility = v ? Visibility.Collapsed : Visibility.Visible; StatusLabel.Text = v ? "INACTIVE" : "ACTIVE"; StatusLabel.Foreground = v ? Brushes.Red : Brushes.Lime; } }
    }
}