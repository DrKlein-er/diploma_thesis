using System;

namespace Командное_управление_проектами.Models
{
    /// <summary>
    /// Модель сообщения чата
    /// </summary>
    public class ChatMessageModel
    {
        /// <summary>
        /// ID сообщения
        /// </summary>
        public int ID_сообщения { get; set; }

        /// <summary>
        /// ID проекта (null для общего чата)
        /// </summary>
        public int? ID_проекта { get; set; }

        /// <summary>
        /// ID отправителя
        /// </summary>
        public int ID_отправителя { get; set; }

        /// <summary>
        /// Имя отправителя
        /// </summary>
        public string Имя_отправителя { get; set; }

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Текст_сообщения { get; set; }

        /// <summary>
        /// Дата и время отправки
        /// </summary>
        public DateTime Дата_отправки { get; set; }

        /// <summary>
        /// Прочитано ли сообщение
        /// </summary>
        public bool Прочитано { get; set; }

        /// <summary>
        /// Форматированная дата для отображения
        /// </summary>
        public string ФорматированнаяДата
        {
            get
            {
                // Если сегодня - показываем только время
                if (Дата_отправки.Date == DateTime.Today)
                {
                    return Дата_отправки.ToString("HH:mm");
                }
                // Если вчера
                else if (Дата_отправки.Date == DateTime.Today.AddDays(-1))
                {
                    return "Вчера " + Дата_отправки.ToString("HH:mm");
                }
                // Иначе полная дата
                else
                {
                    return Дата_отправки.ToString("dd.MM.yyyy HH:mm");
                }
            }
        }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public ChatMessageModel()
        {
            Дата_отправки = DateTime.Now;
            Прочитано = false;
        }
    }
}