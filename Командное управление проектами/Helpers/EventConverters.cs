using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Командное_управление_проектами.Helpers
{
    public class EventTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value as string;
            object resourceKey;
            switch (type)
            {
                case "Встреча": resourceKey = "EventMeetingBrush"; break;
                case "Дедлайн": resourceKey = "EventDeadlineBrush"; break;
                case "Презентация": resourceKey = "EventPresentBrush"; break;
                case "Личное": resourceKey = "EventPersonalBrush"; break;
                default: resourceKey = "EventOtherBrush"; break;
            }

            var brush = Application.Current?.Resources[resourceKey] as SolidColorBrush;
            return brush ?? Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Преобразует название типа события в бледную (пастельную) кисть
    // для фона бейджа и иконки статистической карточки.
    public class EventTypeToBackgroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value as string;
            object resourceKey;
            switch (type)
            {
                case "Встреча": resourceKey = "EventMeetingBgBrush"; break;
                case "Дедлайн": resourceKey = "EventDeadlineBgBrush"; break;
                case "Презентация": resourceKey = "EventPresentBgBrush"; break;
                case "Личное": resourceKey = "EventPersonalBgBrush"; break;
                default: resourceKey = "EventOtherBgBrush"; break;
            }

            var brush = Application.Current?.Resources[resourceKey] as SolidColorBrush;
            return brush ?? Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Скрывает элемент UI (Collapsed), если строка пустая или null.
    // Используется в карточке события для рядов «Место» и «Задача» —
    // если у события нет места проведения, строка просто исчезает.
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}