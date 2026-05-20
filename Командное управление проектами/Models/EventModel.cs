using System;

namespace Командное_управление_проектами.Models
{
    public class EventModel
    {
        public int ID_события { get; set; }
        public string Название_события { get; set; }
        public string Описание { get; set; }
        public DateTime? Дата_начала { get; set; }
        public DateTime? Дата_окончания { get; set; }
        public string Тип_события { get; set; }
        public string Место_проведения { get; set; }
        public int? ID_проекта { get; set; }
        public int? ID_задачи { get; set; }
        public string Название_проекта { get; set; }
        public string Название_задачи { get; set; }
        // Форматированное время начала-окончания для отображения в карточке.
        // "10:00 – 11:30" если есть и начало, и окончание; "10:00" если только начало; "" если ничего.
        public string ВремяОтображение
        {
            get
            {
                if (!Дата_начала.HasValue) return "";
                string start = Дата_начала.Value.ToString("HH:mm");
                if (Дата_окончания.HasValue)
                    return $"{start} – {Дата_окончания.Value:HH:mm}";
                return start;
            }
        }

        // Название проекта для футера карточки — с подменой пустого значения.
        public string ПроектОтображение
        {
            get
            {
                return string.IsNullOrWhiteSpace(Название_проекта)
                    ? "Без проекта"
                    : Название_проекта;
            }
        }
    }
}
