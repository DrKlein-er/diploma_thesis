using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Командное_управление_проектами.Models
{
    public class EmployeeModel
    {
        public int ID_сотрудника { get; set; }
        public string Фамилия { get; set; }
        public string Имя { get; set; }
        public string Отчество { get; set; }
        public string Отдел { get; set; }
        public string Роль { get; set; }
        public int ID_роли { get; set; }

        // Свойство для отображения ФИО в комбобоксах
        public string Имя_сотрудника
        {
            get { return $"{Фамилия} {Имя} {Отчество}"; }
        }
    }
}
