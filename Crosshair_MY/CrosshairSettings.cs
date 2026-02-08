using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Crosshair_MY
{
    public enum CrosshairStyle
    {
        ClassicPlus = 0,
        Dot = 1,
        Circle = 2,
        CSStyle = 3,
        ValorantStyle = 4
    }

    public class CrosshairSettings
    {
        // ==============================
        // 🔹 GENERAL
        // ==============================
        public bool Enabled { get; set; } = true;
        public CrosshairStyle Style { get; set; } = CrosshairStyle.ClassicPlus;

        // ==============================
        // 🔹 VISIBILITY
        // ==============================
        public bool DotEnabled { get; set; } = true;
        public bool LinesEnabled { get; set; } = true;

        // ==============================
        // 🔹 SIZE & SHAPE
        // ==============================
        public double DotSize { get; set; } = 6;
        public double Thickness { get; set; } = 3;
        public double Length { get; set; } = 40;
        public double Gap { get; set; } = 4;

        // ==============================
        // 🔹 OPACITY & COLOR
        // ==============================
        public double Opacity { get; set; } = 1.0;

        [JsonIgnore]
        public Brush Color { get; set; } = Brushes.Red;

        public string ColorHex
        {
            get => Color is SolidColorBrush scb ? scb.Color.ToString() : "#FFFF0000";
            set
            {
                try
                {
                    var c = ColorConverter.ConvertFromString(value);
                    if (c != null) Color = new SolidColorBrush((Color)c);
                }
                catch { Color = Brushes.Red; }
            }
        }

        // ==============================
        // 🔹 CIRCLE MODE
        // ==============================
        public double CircleRadius { get; set; } = 10;
        public double CircleThickness { get; set; } = 2;

        // ==============================
        // 🔹 OUTLINE
        // ==============================
        public bool OutlineEnabled { get; set; } = false;
        public double OutlineThickness { get; set; } = 1;

        [JsonIgnore]
        public Brush OutlineColor { get; set; } = Brushes.Black;

        public string OutlineColorHex
        {
            get => OutlineColor is SolidColorBrush scb ? scb.Color.ToString() : "#FF000000";
            set
            {
                try
                {
                    var c = ColorConverter.ConvertFromString(value);
                    if (c != null) OutlineColor = new SolidColorBrush((Color)c);
                }
                catch { OutlineColor = Brushes.Black; }
            }
        }

        // ==============================
        // 🔹 ANIMATION
        // ==============================
        public bool FireAnimationEnabled { get; set; } = false;
        public double FireExpandAmount { get; set; } = 6;
        public double FireAnimationSpeed { get; set; } = 0.15;

        // ==============================
        // 🔹 PRESET SYSTEM
        // ==============================
        public string PresetName { get; set; } = "Default";

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        public static CrosshairSettings FromJson(string json)
        {
            try
            {
                // Deserialize zamanı null gəlmə ehtimalını tamamilə qapatdıq
                var settings = JsonSerializer.Deserialize<CrosshairSettings>(json);
                return settings ?? new CrosshairSettings();
            }
            catch
            {
                return new CrosshairSettings();
            }
        }

        public CrosshairSettings Clone()
        {
            return FromJson(ToJson());
        }
    }
}