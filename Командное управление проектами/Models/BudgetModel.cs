using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Командное_управление_проектами.Models
{
    public class BudgetModel
    {
        public int ID_бюджета { get; set; }
        public int ID_проекта { get; set; }
        public decimal Сумма { get; set; }
        public string Назначение { get; set; }
        public DateTime Дата_создания { get; set; }
        public string Название_проекта { get; set; }
    }
}
