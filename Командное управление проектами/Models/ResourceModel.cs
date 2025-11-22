using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Командное_управление_проектами.Models
{
    public class ResourceModel
    {
        public int ID_ресурса { get; set; }
        public string Название { get; set; }
        public string Тип { get; set; }
        public int Количество { get; set; }
    }
}
