using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Командное_управление_проектами.Helpers
{
    public class NoteColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string colorName = value as string;
            switch (colorName)
            {
                case "Розовый": return new SolidColorBrush(Color.FromRgb(0xF8, 0xBB, 0xD0));
                case "Голубой": return new SolidColorBrush(Color.FromRgb(0xB3, 0xE5, 0xFC));
                case "Зелёный": return new SolidColorBrush(Color.FromRgb(0xC8, 0xE6, 0xC9));
                case "Серый": return new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                case "Жёлтый":
                default: return new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0x9D));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}