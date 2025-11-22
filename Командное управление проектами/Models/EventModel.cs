using System;

namespace Командное_управление_проектами.Models
{
    public class EventModel
    {
        public int ID_события { get; set; }
        public string Название_события { get; set; }
        public string Описание { get; set; }
        public DateTime? Дата_события { get; set; }
        public int? ID_проекта { get; set; }
        public string Название_проекта { get; set; }
    }
}
