using System;


namespace Командное_управление_проектами.Models
{
    public class UserModel
    {
        public int ID { get; set; }
        public string Email { get; set; }
        public string Пароль { get; set; }
        public string Фамилия { get; set; }
        public string Имя { get; set; }
        public string Отчество { get; set; }
        public string Роль { get; set; }
        public int ID_сотрудника { get; set; }
    }
}
