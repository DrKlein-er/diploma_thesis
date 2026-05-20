using System;


namespace Командное_управление_проектами.Models
{
    public class NoteModel
    {
        public int ID_заметки { get; set; }
        public string Заголовок { get; set; }
        public string Текст_заметки { get; set; }
        public string Цвет { get; set; }
        public bool Закреплена { get; set; }
        public DateTime? Дата_создания { get; set; }
        public int? ID_проекта { get; set; }
        public int? ID_задачи { get; set; }
        public int? ID_подзадачи { get; set; }
        public string Название_проекта { get; set; }
        public string Название_задачи { get; set; }
        public string Название_подзадачи { get; set; }

        // Вычисляемое свойство для отображения привязки в DataGrid одной строкой
        public string ПривязкаОтображение
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Название_подзадачи))
                    return $" Подзадача: {Название_подзадачи}";
                if (!string.IsNullOrWhiteSpace(Название_задачи))
                    return $" Задача: {Название_задачи}";
                if (!string.IsNullOrWhiteSpace(Название_проекта))
                    return $" Проект: {Название_проекта}";
                return "";
            }
        }
    }
}
