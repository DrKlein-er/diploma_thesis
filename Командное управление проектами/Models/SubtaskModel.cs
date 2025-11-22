using System;
using System.Windows.Media;

namespace Командное_управление_проектами.Models
{
    public class SubtaskModel
    {
        public int ID_подзадачи { get; set; } // Идентификатор подзадачи
        public string Название_подзадачи { get; set; } // Название подзадачи
        public string Описание { get; set; } // Описание подзадачи
        public string Статус { get; set; } // Статус подзадачи (Открыта, В процессе, Завершена)
        public DateTime? Дата_начала { get; set; } // Дата начала подзадачи
        public DateTime? Дата_завершения { get; set; } // Дата завершения подзадачи (крайний срок)
        public int ID_задачи { get; set; } // ID родительской задачи
        public string Название_задачи { get; set; } // Название родительской задачи
        public Brush StatusColor { get; set; } // Цвет для отображения статуса в UI
    }
}
