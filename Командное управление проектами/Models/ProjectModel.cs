using System;
using System.Windows.Media;


namespace Командное_управление_проектами.Models
{
    public class ProjectModel
    {
        public int ID_проекта { get; set; }
        public string Название_проекта { get; set; }
        public string Описание { get; set; }
        public DateTime? Дата_начала { get; set; }
        public DateTime? Дата_завершения { get; set; }
        public string Статус { get; set; }
        public string Ответственный { get; set; }
        public int? ID_ответственного { get; internal set; }
        public Brush StatusColor { get; set; }
        public decimal Бюджет { get; set; }

    }
}
