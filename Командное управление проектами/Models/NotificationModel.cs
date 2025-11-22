using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Командное_управление_проектами.Models
{
    public class NotificationModel
    {
        public int ID { get; set; }
        public string Заголовок { get; set; }
        public string Текст { get; set; }
        public string Тип { get; set; } // "Задача", "Проект", "Напоминание", "Событие", "Система"
        public int? ID_связанного_объекта { get; set; }
        public DateTime Дата_создания { get; set; }
        public bool Прочитано { get; set; }
        public string Приоритет { get; set; } // "Низкий", "Средний", "Высокий"

        // Свойства для UI
        public Brush BackgroundColor { get; set; } = Brushes.White;
        public Visibility ReadButtonVisibility { get; set; } = Visibility.Visible;

        // Свойство для отображения времени
        public string ВремяС_момента_создания
        {
            get
            {
                var span = DateTime.Now - Дата_создания;
                if (span.TotalMinutes < 1) return "только что";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} мин. назад";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours} ч. назад";
                return $"{(int)span.TotalDays} дн. назад";
            }
        }

        // Цвет в зависимости от приоритета
        public Brush ПриоритетЦвет
        {
            get
            {
                switch (Приоритет)
                {
                    case "Высокий":
                        return Brushes.Red;
                    case "Средний":
                        return Brushes.Orange;
                    case "Низкий":
                        return Brushes.Green;
                    default:
                        return Brushes.Gray;
                }
            }
        }

        // Иконка в зависимости от типа
        public string ТипИконка
        {
            get
            {
                switch (Тип)
                {
                    case "Задача":
                        return "📋";
                    case "Проект":
                        return "📁";
                    case "Напоминание":
                        return "⏰";
                    case "Событие":
                        return "📅";
                    case "Система":
                        return "ℹ️";
                    default:
                        return "🔔";
                }
            }
        }
    }
}