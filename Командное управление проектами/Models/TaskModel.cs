using System;
using System.Windows.Media;
using System.Collections.Generic;

namespace Командное_управление_проектами.Models
{
    public class TaskModel
    {
        public int ID_задачи { get; set; } // Идентификатор задачи
        public string Название_задачи { get; set; } // Название задачи
        public string Описание { get; set; } // Описание задачи
        public string Приоритет { get; set; } // Приоритет задачи (Высокий, Средний, Низкий)
        public string Статус { get; set; } // Статус задачи (Открыта, В процессе, Завершена)
        public DateTime? Дата_начала { get; set; } // Дата начала задачи
        public DateTime? Дата_завершения { get; set; } // Дата завершения задачи (крайний срок)
        public string Название_проекта { get; set; } // Название проекта, к которому относится задача
        public string Ответственный { get; set; } // ФИО ответственного за задачу
        public int? ID_ответственного { get; set; } // ID ответственного сотрудника
        public Brush StatusColor { get; set; } // Цвет для отображения статуса в UI
        public List<SubtaskModel> Subtasks { get; set; } // Список подзадач
    }
}
