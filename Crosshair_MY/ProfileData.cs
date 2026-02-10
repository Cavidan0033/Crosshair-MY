#nullable disable

using System.Collections.Generic;
using System.Text.Json.Serialization;
// Rənglərin qarışmaması üçün tam adlardan istifadə edirik
using System.Windows.Media;

namespace Crosshair_MY
{
    // === QAT TİPLƏRİ (Bütün növlər burdadır) ===
    public enum LayerType
    {
        Cross,      // 0
        Circle,     // 1
        Dot,        // 2
        Square,     // 3
        X_Shape,    // 4 (Xəta verməməsi üçün bu vacibdir)
        Triangle,   // 5
        Image       // 6
    }

    // === CROSSHAIR LAYER ===
    public class CrosshairLayer
    {
        public string Name { get; set; } = "Layer 1";
        public LayerType Type { get; set; } = LayerType.Cross;
        public bool Enabled { get; set; } = true;
        public double OutlineThickness { get; set; } = 1.0;
        public bool HasOutline { get; set; } = true;
        // Ölçülər
        public double Length { get; set; } = 10;
        public double Thickness { get; set; } = 2;
        public double Gap { get; set; } = 4;
        public double Rotation { get; set; } = 0;
        public double OffsetX { get; set; } = 0;
        public double OffsetY { get; set; } = 0;

        // Şəkil Yolu (Image type üçün vacibdir)
        public string ImagePath { get; set; } = "";

        // Rəng
        public string ColorHex { get; set; } = "#FF00FF00";
        public double Opacity { get; set; } = 1.0;

        [JsonIgnore]
        public System.Windows.Media.SolidColorBrush ColorBrush
        {
            get
            {
                try
                {
                    var converter = new System.Windows.Media.BrushConverter();
                    return (System.Windows.Media.SolidColorBrush)converter.ConvertFrom(ColorHex);
                }
                catch { return System.Windows.Media.Brushes.Lime; }
            }
        }
    }

    // === SETTINGS ===
    public class CrosshairSettings
    {
        public List<CrosshairLayer> Layers { get; set; } = new List<CrosshairLayer>();
        public CrosshairSettings()
        {
            Layers.Add(new CrosshairLayer());
        }
    }

    // === PROFILE ===
    public class ProfileData
    {
        public string Name { get; set; } = "New Profile";
        public List<CrosshairLayer> Layers { get; set; } = new List<CrosshairLayer>();
    }
}