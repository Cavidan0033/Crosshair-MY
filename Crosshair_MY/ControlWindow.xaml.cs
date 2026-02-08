using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32; // Registry (Startup) üçün vacibdir

namespace Crosshair_MY
{
    // Start Minimized ayarını yadda saxlamaq üçün kiçik class
    public class AppSettings
    {
        public bool StartMinimized { get; set; } = false;
    }

    public partial class ControlWindow : Window
    {
        private MainWindow _main;
        private bool _isInitialized = false;
        private List<ProfileData> _profiles = new List<ProfileData>();
        private string _profilePath = "profiles.json";
        private string _settingsPath = "settings.json"; // Ayarlar faylı

        public ControlWindow(MainWindow main)
        {
            InitializeComponent();
            _main = main;

            LoadSettings();         // Slider/Rəng ayarları
            LoadProfilesFromFile(); // Yaddaşdakı profillər
            LoadAppConfig();        // Settings (Minimized) ayarı

            SwitchView("Editor");   // İlk açılan səhifə

            UpdateSystemInfo();     // Ekran ölçüsünü yaz
            CheckStartupStatus();   // Windows Registry yoxla

            // Control Panel-ə klikləyəndə Crosshair-i önə gətir
            this.Activated += (s, e) => { if (_main != null) { _main.Topmost = false; _main.Topmost = true; } };
            _isInitialized = true;
        }

        // ================= NAVİQASİYA =================
        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content != null)
                SwitchView(btn.Content.ToString() ?? "Editor");
        }

        private void SwitchView(string viewName)
        {
            if (View_Essentials == null) return;

            // Hamısını gizlət
            View_Essentials.Visibility = Visibility.Collapsed;
            View_Editor.Visibility = Visibility.Collapsed;
            View_Settings.Visibility = Visibility.Collapsed;
            View_Profiles.Visibility = Visibility.Collapsed;

            ResetButtonStyles();

            switch (viewName)
            {
                case "Essentials":
                    View_Essentials.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "ESSENTIALS";
                    TxtPageSubtitle.Text = "DASHBOARD";
                    HighlightButton(BtnEssentials);
                    break;
                case "Editor":
                    View_Editor.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "EDITOR";
                    TxtPageSubtitle.Text = "STUDIO";
                    HighlightButton(BtnEditor);
                    break;
                case "Profiles":
                    View_Profiles.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "PROFILES";
                    TxtPageSubtitle.Text = "SAVED";
                    HighlightButton(BtnProfiles);
                    break;
                case "Settings":
                    View_Settings.Visibility = Visibility.Visible;
                    TxtPageTitle.Text = "SETTINGS";
                    TxtPageSubtitle.Text = "GENERAL";
                    HighlightButton(BtnSettings);
                    break;
            }
        }

        private void ResetButtonStyles()
        {
            var gray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888"));
            if (BtnEssentials != null) { BtnEssentials.Foreground = gray; BtnEssentials.Background = Brushes.Transparent; BtnEssentials.FontWeight = FontWeights.Normal; }
            if (BtnEditor != null) { BtnEditor.Foreground = gray; BtnEditor.Background = Brushes.Transparent; BtnEditor.FontWeight = FontWeights.Normal; }
            if (BtnProfiles != null) { BtnProfiles.Foreground = gray; BtnProfiles.Background = Brushes.Transparent; BtnProfiles.FontWeight = FontWeights.Normal; }
            if (BtnSettings != null) { BtnSettings.Foreground = gray; BtnSettings.Background = Brushes.Transparent; BtnSettings.FontWeight = FontWeights.Normal; }
        }

        private void HighlightButton(Button btn)
        {
            if (btn == null) return;
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4500"));
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515"));
            btn.FontWeight = FontWeights.Bold;
        }

        // ================= SETTINGS (ÇATIŞMAYAN HİSSƏLƏR ƏLAVƏ EDİLDİ) =================

        // 1. Minimized Mode (Fayla yazmaq)
        private void ChkMinimized_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            SaveAppConfig();
        }

        private void SaveAppConfig()
        {
            var settings = new AppSettings
            {
                StartMinimized = ChkMinimized.IsChecked == true
            };
            try
            {
                string json = JsonSerializer.Serialize(settings);
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }

        private void LoadAppConfig()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        ChkMinimized.IsChecked = settings.StartMinimized;
                    }
                }
                catch { }
            }
        }

        // 2. Windows Startup (Registry)
        private void ChkStartup_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            var mainModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            if (mainModule == null) return;

            string appName = "CrosshairMY";
            string appPath = mainModule.FileName;

            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        if (ChkStartup.IsChecked == true)
                            key.SetValue(appName, appPath);
                        else
                            key.DeleteValue(appName, false);
                    }
                }
            }
            catch { }
        }

        private void CheckStartupStatus()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null && key.GetValue("CrosshairMY") != null)
                    {
                        bool oldState = _isInitialized;
                        _isInitialized = false;
                        ChkStartup.IsChecked = true;
                        _isInitialized = oldState;
                    }
                }
            }
            catch { }
        }

        // 3. Update Link
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.google.com", // Bura öz saytını yaza bilərsən
                    UseShellExecute = true
                });
            }
            catch { }
        }

        // 4. System Info (Resolution)
        private void UpdateSystemInfo()
        {
            TxtResolution.Text = $"{SystemParameters.PrimaryScreenWidth} x {SystemParameters.PrimaryScreenHeight}";
        }

        // ================= EDITOR FUNKSİYALARI =================
        private void LoadSettings()
        {
            StyleCombo.Items.Clear();
            StyleCombo.Items.Add("Classic +");
            StyleCombo.Items.Add("Dot");
            StyleCombo.Items.Add("Circle");
            StyleCombo.Items.Add("CS Style");
            StyleCombo.Items.Add("Valorant");

            ColorCombo.SelectedIndex = 1;
            StyleCombo.SelectedIndex = 0;

            LengthSlider.Value = _main._settings.Length;
            ThickSlider.Value = _main._settings.Thickness;
            GapSlider.Value = _main._settings.Gap;
            OpacitySlider.Value = _main._settings.Opacity * 100;
            OutlineSlider.Value = _main._settings.OutlineThickness;
        }

        private void Settings_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized || _main == null || _main._settings == null) return;
            _main._settings.Length = LengthSlider.Value;
            _main._settings.Thickness = ThickSlider.Value;
            _main._settings.Gap = GapSlider.Value;
            _main._settings.Opacity = OpacitySlider.Value / 100.0;
            _main._settings.OutlineThickness = OutlineSlider.Value;
            _main.DrawCrosshair(_main._settings);
        }

        private void ColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _main == null) return;
            switch (ColorCombo.SelectedIndex)
            {
                case 0: _main._settings.Color = Brushes.Red; break;
                case 1: _main._settings.Color = Brushes.Lime; break;
                case 2: _main._settings.Color = Brushes.Cyan; break;
                case 3: _main._settings.Color = Brushes.Yellow; break;
            }
            _main.DrawCrosshair(_main._settings);
        }

        private void StyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _main == null) return;
            _main._settings.Style = (CrosshairStyle)StyleCombo.SelectedIndex;
            _main.DrawCrosshair(_main._settings);
        }

        // ================= PROFILES =================
        private void LoadProfilesFromFile()
        {
            if (File.Exists(_profilePath))
            {
                try
                {
                    string json = File.ReadAllText(_profilePath);
                    var loaded = JsonSerializer.Deserialize<List<ProfileData>>(json);
                    if (loaded != null) _profiles = loaded;
                }
                catch { }
            }
            UpdateProfileList();
        }

        private void UpdateProfileList()
        {
            ListProfiles.Items.Clear();
            foreach (var p in _profiles)
            {
                ListProfiles.Items.Add(p.Name);
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtProfileName.Text;
            if (string.IsNullOrWhiteSpace(name)) return;

            var profile = new ProfileData
            {
                Name = name,
                Length = LengthSlider.Value,
                Thickness = ThickSlider.Value,
                Gap = GapSlider.Value,
                Opacity = OpacitySlider.Value / 100.0,
                OutlineThickness = OutlineSlider.Value,
                ColorIndex = ColorCombo.SelectedIndex,
                StyleIndex = StyleCombo.SelectedIndex
            };

            _profiles.Add(profile);
            string json = JsonSerializer.Serialize(_profiles);
            File.WriteAllText(_profilePath, json);
            UpdateProfileList();
            MessageBox.Show("Saved!");
        }

        private void ListProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListProfiles.SelectedIndex < 0 || ListProfiles.SelectedIndex >= _profiles.Count) return;
            var p = _profiles[ListProfiles.SelectedIndex];
            _isInitialized = false;
            LengthSlider.Value = p.Length;
            ThickSlider.Value = p.Thickness;
            GapSlider.Value = p.Gap;
            OpacitySlider.Value = p.Opacity * 100;
            OutlineSlider.Value = p.OutlineThickness;
            ColorCombo.SelectedIndex = p.ColorIndex;
            StyleCombo.SelectedIndex = p.StyleIndex;
            _isInitialized = true;
            Settings_Changed(this, null!);
            ColorCombo_SelectionChanged(this, null!);
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ListProfiles.SelectedIndex < 0) return;
            _profiles.RemoveAt(ListProfiles.SelectedIndex);
            string json = JsonSerializer.Serialize(_profiles);
            File.WriteAllText(_profilePath, json);
            UpdateProfileList();
        }

        // ================= ACTIONS & WINDOW =================
        private void Action_Toggle_Click(object sender, RoutedEventArgs e)
        {
            if (_main._overlay != null)
                _main._overlay.Visibility = _main._overlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ColorCombo.SelectedIndex = 1;
            StyleCombo.SelectedIndex = 0;
            LengthSlider.Value = 6;
            ThickSlider.Value = 2;
            GapSlider.Value = 4;
            OpacitySlider.Value = 100;
            OutlineSlider.Value = 1;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => this.Hide();
    }
}