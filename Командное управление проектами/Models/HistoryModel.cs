using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Командное_управление_проектами.Models
{
    public class HistoryModel
    {
        public int ID_изменения { get; set; }
        public string Сущность { get; set; }
        public int ID_объекта { get; set; }
        public string Действие { get; set; }
        public DateTime Дата_изменения { get; set; }
        public int ID_сотрудника { get; set; }
        public string ФИО_сотрудника { get; set; }
    }
}
