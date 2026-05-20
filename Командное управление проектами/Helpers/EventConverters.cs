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

    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            switch (status)
            {
                case "В работе":
                case "В процессе":
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243));
                case "Планируется":
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158));
                case "Завершён":
                case "Завершен":
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                case "Пауза":
                case "Отложен":
                case "Приостановлен":
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0));
                case "Отменён":
                case "Отменен":
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotNullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}