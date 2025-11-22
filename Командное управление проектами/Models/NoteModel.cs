using System;


namespace Командное_управление_проектами.Models
{
    public class NoteModel
    {
        public int ID_заметки { get; set; }
        public string Текст_заметки { get; set; }
        public DateTime? Дата_создания { get; set; }
        public int? ID_задачи { get; set; } 
        public string Название_задачи { get; set; }
    }
}
