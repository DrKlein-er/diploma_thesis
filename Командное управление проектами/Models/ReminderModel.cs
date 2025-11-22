using System;


namespace Командное_управление_проектами.Models
{
    public class ReminderModel
    {
        public int ID_напоминания { get; set; }
        public string Текст_напоминания { get; set; }
        public DateTime? Дата_напоминания { get; set; }
        public int? ID_задачи { get; set; }
        public string Название_задачи { get; set; }
    }
}
