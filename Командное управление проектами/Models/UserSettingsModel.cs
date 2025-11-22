using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Командное_управление_проектами.Models
{
    public class UserSettingsModel
    {
        public int ID_пользователя { get; set; }
        public string Тема { get; set; } = "Светлая"; // "Светлая", "Тёмная"
        public bool Уведомления_включены { get; set; } = true;
        public int Частота_проверки_дедлайнов { get; set; } = 30; // в минутах
        public bool Звук_уведомлений { get; set; } = true;
        public bool Показывать_завершённые_задачи { get; set; } = true;
        public string Язык { get; set; } = "Русский";
    }
}
