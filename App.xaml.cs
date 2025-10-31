using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
namespace ScriptRunner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
        public partial class App : Application
        {
            private static readonly Dictionary<string, Color> _lightColors = new Dictionary<string, Color>
{
    { "WindowForegroundColor", (Color)ColorConverter.ConvertFromString("#FF1E1E1E") },
    { "WindowBackgroundColor", (Color)ColorConverter.ConvertFromString("#FFDCDCDC") },
    { "AccentColor", (Color)ColorConverter.ConvertFromString("#FF007ACC") },
    { "TabSelectedColor", (Color)ColorConverter.ConvertFromString("#FFCCE5FF") },
    { "TabHoverColor", (Color)ColorConverter.ConvertFromString("#FFDCEFFF") },
    { "TabBorderBrushColor", (Color)ColorConverter.ConvertFromString("#FF00FFFF") },
    { "ControlBackgroundColor", (Color)ColorConverter.ConvertFromString("#FFC8C8C8") },
    { "ButtonBackgroundColor", (Color)ColorConverter.ConvertFromString("#FFC8C8C8") },
  { "CardBackgroundColor", (Color)ColorConverter.ConvertFromString("#FFE5E5E5") },
};
        private static readonly Dictionary<string, Color> _darkColors = new Dictionary<string, Color>
        {
            ["WindowForegroundColor"] = (Color)ColorConverter.ConvertFromString("#FFFDFDFD"),
            ["WindowBackgroundColor"] = (Color)ColorConverter.ConvertFromString("#FF1E1E1E"),
            ["AccentColor"] = (Color)ColorConverter.ConvertFromString("#FF007ACC"),
            ["TabSelectedColor"] = (Color)ColorConverter.ConvertFromString("#FFCCE5FF"),
            ["TabHoverColor"] = (Color)ColorConverter.ConvertFromString("#FFDCEFFF"),
            ["TabBorderBrushColor"] = (Color)ColorConverter.ConvertFromString("#FF00FFFF"),
            ["ButtonBackgroundColor"] = (Color)ColorConverter.ConvertFromString("#FF1E1E1E"),
            ["ControlBackgroundColor"] = (Color)ColorConverter.ConvertFromString("#FF2D2D2D"),
            ["CardBackgroundColor"] = (Color)ColorConverter.ConvertFromString("#FF444444"),
        };

            public void SetTheme(bool darkMode)
            {
                var theme = darkMode ? _darkColors : _lightColors;
                var res = Resources;

                // Update both Color resources AND SolidColorBrush resources
                foreach (var kv in theme)
                {
                    // Update the Color resource
                    if (res.Contains(kv.Key))
                        res[kv.Key] = kv.Value;
                    else
                        res.Add(kv.Key, kv.Value);

                    // Update corresponding SolidColorBrush resources
                    string brushKey = GetBrushKeyFromColorKey(kv.Key);
                    if (!string.IsNullOrEmpty(brushKey))
                    {
                        var brush = new SolidColorBrush(kv.Value);
                        if (res.Contains(brushKey))
                            res[brushKey] = brush;
                        else
                            res.Add(brushKey, brush);
                    }
                }
            }
        public class ColorSubstitutionConverter : IValueConverter
        {
            public Color From { get; set; }
            public Color To { get; set; }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is SolidColorBrush brush && brush.Color == From)
                    return new SolidColorBrush(To);
                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }


        private static string GetBrushKeyFromColorKey(string colorKey)
        {
            // Map color keys to their corresponding brush keys
            switch (colorKey)
            {
                case "WindowForegroundColor":
                    return "ForegroundBrush";
                case "WindowBackgroundColor":
                    return "WindowBackgroundBrush";
                case "AccentColor":
                    return "AccentBrush";
                case "TabSelectedColor":
                    return "TabSelectedBrush";
                case "TabHoverColor":
                    return "TabHoverBrush";
                case "TabBorderBrushColor":
                    return "TabBorderBrush";
                case "ControlBackgroundColor":
                    return "ControlBackgroundBrush";
                case "CardBackgroundColor":
                    return "CardBackgroundBrush";
                case "ButtonBackgroundColor":
                    return "ButtonBackgroundBrush";
                default:
                    return null;
            }
        }
    }
}