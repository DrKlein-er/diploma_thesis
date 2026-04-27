using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Командное_управление_проектами.Models
{
    /// <summary>
    /// Модель для записи истории изменений объектов
    /// </summary>
    public class HistoryModel
    {
        public int ID_изменения { get; set; }
        public string Сущность { get; set; }
        public int ID_объекта { get; set; }
        public string Действие { get; set; }
        public DateTime Дата_изменения { get; set; }
        public int ID_сотрудника { get; set; }
        public string ФИО_сотрудника { get; set; }

        // Алиасы для совместимости с DataGrid
        /// <summary>
        /// Алиас для отображения описания изменения в DataGrid
        /// </summary>
        public string Описание_изменения
        {
            get => Действие;
            set => Действие = value;
        }

        /// <summary>
        /// Алиас для отображения имени сотрудника в DataGrid
        /// </summary>
        public string Имя_сотрудника
        {
            get => ФИО_сотрудника;
            set => ФИО_сотрудника = value;
        }
    }
}