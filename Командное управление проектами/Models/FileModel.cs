using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Командное_управление_проектами.Models
{
    public class FileModel
    {
        public int ID_файла { get; set; }
        public string Название_файла { get; set; }
        public string Путь_к_файлу { get; set; }
        public int ID_задачи { get; set; }
    }
}
