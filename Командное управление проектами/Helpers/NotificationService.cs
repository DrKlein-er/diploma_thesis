using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Helpers
{
    public class NotificationService
    {
        private static NotificationService _instance;
        private static readonly object _lock = new object();

        private ObservableCollection<NotificationModel> _notifications;
        private DispatcherTimer _checkTimer;
        private UserModel _currentUser;
        private int _nextId = 1;

        // Событие для обновления UI
        public event Action NotificationsChanged;

        private NotificationService()
        {
            _notifications = new ObservableCollection<NotificationModel>();
        }

        public static NotificationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new NotificationService();
                        }
                    }
                }
                return _instance;
            }
        }
        public void Initialize(UserModel user)
        {
            _currentUser = user;
            _notifications.Clear();

            _checkTimer = new DispatcherTimer();
            _checkTimer.Interval = TimeSpan.FromMinutes(1);
            _checkTimer.Tick += CheckDeadlines;
            _checkTimer.Start();

            // Первая проверка сразу при входе
            CheckDeadlines(null, null);

            // Приветственное уведомление
            AddNotification(
                "Добро пожаловать!",
                $"Здравствуйте, {user.Имя}! Вы успешно вошли в систему.",
                "Система",
                priority: "Низкий"
            );
        }
        public void Shutdown()
        {
            _checkTimer?.Stop();
            _notifications.Clear();
        }
        public void AddNotification(string title, string text, string type, int? relatedObjectId = null, string priority = "Средний")
        {
            var notification = new NotificationModel
            {
                ID = _nextId++,
                Заголовок = title,
                Текст = text,
                Тип = type,
                ID_связанного_объекта = relatedObjectId,
                Дата_создания = DateTime.Now,
                Прочитано = false,
                Приоритет = priority
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                _notifications.Insert(0, notification); // Добавляем в начало списка

                // Ограничиваем количество уведомлений (максимум 100)
                while (_notifications.Count > 100)
                {
                    _notifications.RemoveAt(_notifications.Count - 1);
                }
            });

            NotificationsChanged?.Invoke();

            // Показываем всплывающее окно для важных уведомлений
            if (priority == "Высокий" || priority == "Средний")
            {
                ShowToast(notification);
            }
        }
        public ObservableCollection<NotificationModel> GetAllNotifications()
        {
            return _notifications;
        }
        public List<NotificationModel> GetUnreadNotifications()
        {
            return _notifications.Where(n => !n.Прочитано).ToList();
        }
        public int GetUnreadCount()
        {
            return _notifications.Count(n => !n.Прочитано);
        }
        public void MarkAsRead(int notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.ID == notificationId);
            if (notification != null)
            {
                notification.Прочитано = true;
                NotificationsChanged?.Invoke();
            }
        }
        public void MarkAllAsRead()
        {
            foreach (var notification in _notifications)
            {
                notification.Прочитано = true;
            }
            NotificationsChanged?.Invoke();
        }
        public void DeleteNotification(int notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.ID == notificationId);
            if (notification != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _notifications.Remove(notification);
                });
                NotificationsChanged?.Invoke();
            }
        }
        public void ClearReadNotifications()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var readNotifications = _notifications.Where(n => n.Прочитано).ToList();
                foreach (var notification in readNotifications)
                {
                    _notifications.Remove(notification);
                }
            });
            NotificationsChanged?.Invoke();
        }
        private void CheckDeadlines(object sender, EventArgs e)
        {
            if (_currentUser == null) return;

            try
            {
                var tasks = DbHelper.GetAllTasks();
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                foreach (var task in tasks)
                {
                    // Проверяем только задачи текущего пользователя
                    if (task.ID_ответственного != _currentUser.ID_сотрудника)
                        continue;

                    // Пропускаем завершённые задачи
                    if (task.Статус == "Завершена")
                        continue;

                    if (!task.Дата_завершения.HasValue)
                        continue;

                    var deadline = task.Дата_завершения.Value.Date;

                    // Проверяем, не создано ли уже уведомление для этой задачи сегодня
                    bool alreadyNotified = _notifications.Any(n =>
                        n.Тип == "Задача" &&
                        n.ID_связанного_объекта == task.ID_задачи &&
                        n.Дата_создания.Date == today);

                    if (alreadyNotified)
                        continue;

                    // Задача просрочена
                    if (deadline < today)
                    {
                        AddNotification(
                            "⚠️ Просроченная задача!",
                            $"Задача \"{task.Название_задачи}\" просрочена с {deadline:dd.MM.yyyy}",
                            "Задача",
                            task.ID_задачи,
                            "Высокий"
                        );
                    }
                    // Задача истекает завтра
                    else if (deadline == tomorrow)
                    {
                        AddNotification(
                            "⏰ Дедлайн завтра!",
                            $"Задача \"{task.Название_задачи}\" должна быть выполнена завтра",
                            "Задача",
                            task.ID_задачи,
                            "Высокий"
                        );
                    }
                    // Задача истекает сегодня
                    else if (deadline == today)
                    {
                        AddNotification(
                            "🔥 Дедлайн сегодня!",
                            $"Задача \"{task.Название_задачи}\" должна быть выполнена сегодня",
                            "Задача",
                            task.ID_задачи,
                            "Высокий"
                        );
                    }
                    // Задача истекает через 3 дня
                    else if (deadline == today.AddDays(3))
                    {
                        AddNotification(
                            "📅 Напоминание",
                            $"Задача \"{task.Название_задачи}\" истекает через 3 дня ({deadline:dd.MM.yyyy})",
                            "Задача",
                            task.ID_задачи,
                            "Средний"
                        );
                    }
                }

                // Проверяем напоминания на сегодня
                CheckReminders(today);

                // Проверяем события на сегодня
                CheckEvents(today);
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем пользователю
                System.Diagnostics.Debug.WriteLine($"Ошибка проверки дедлайнов: {ex.Message}");
            }
        }
        private void CheckReminders(DateTime date)
        {
            // Получаем напоминания, у которых уже наступило время
            var dueReminders = DbHelper.GetDueReminders();

            // Фильтр: только напоминания, привязанные к задачам текущего пользователя
            // (или общие, без привязки)
            foreach (var reminder in dueReminders)
            {
                // Проверка ответственного по задаче
                if (reminder.ID_задачи.HasValue)
                {
                    var task = DbHelper.GetTaskById(reminder.ID_задачи.Value);
                    if (task != null && task.ID_ответственного != _currentUser.ID_сотрудника)
                        continue;
                }

                // Проверяем, не показывали ли это напоминание в текущей сессии
                bool alreadyShown = _notifications.Any(n =>
                    n.Тип == "Напоминание" &&
                    n.ID_связанного_объекта == reminder.ID_напоминания);

                if (alreadyShown)
                    continue;

                // Иконка по приоритету
                string icon = reminder.Приоритет == "Высокий" ? "🔴"
                            : reminder.Приоритет == "Низкий" ? "🔵"
                                                              : "🔔";

                string title = $"{icon} Напоминание" +
                               (reminder.Приоритет == "Высокий" ? " (важно)" : "");

                AddNotification(
                    title,
                    reminder.Текст_напоминания,
                    "Напоминание",
                    reminder.ID_напоминания,
                    reminder.Приоритет
                );

                // Отмечаем напоминание в БД как сработавшее
                DbHelper.SetReminderStatus(reminder.ID_напоминания, "Сработало");
            }
        }
        private void CheckEvents(DateTime date)
        {
            // События, у которых Дата_начала приходится на сегодня
            var events = DbHelper.GetEventsByDate(date);

            foreach (var ev in events)
            {
                bool alreadyNotified = _notifications.Any(n =>
                    n.Тип == "Событие" &&
                    n.ID_связанного_объекта == ev.ID_события &&
                    n.Дата_создания.Date == date);

                if (alreadyNotified)
                    continue;

                // Иконка по типу события
                // Иконка по типу события
                string icon;
                switch (ev.Тип_события)
                {
                    case "Встреча": icon = "👥"; break;
                    case "Дедлайн": icon = "⏰"; break;
                    case "Презентация": icon = "📊"; break;
                    case "Личное": icon = "👤"; break;
                    default: icon = "📅"; break;
                }

                // Текст: время + место + описание
                var parts = new List<string>();
                if (ev.Дата_начала.HasValue)
                {
                    string timeText = ev.Дата_окончания.HasValue
                        ? $"{ev.Дата_начала.Value:HH:mm}–{ev.Дата_окончания.Value:HH:mm}"
                        : $"{ev.Дата_начала.Value:HH:mm}";
                    parts.Add($"🕒 {timeText}");
                }
                if (!string.IsNullOrWhiteSpace(ev.Место_проведения))
                    parts.Add($"📍 {ev.Место_проведения}");
                if (!string.IsNullOrWhiteSpace(ev.Описание))
                    parts.Add(ev.Описание);

                string body = $"{ev.Название_события}\n{string.Join("  •  ", parts)}";

                AddNotification(
                    $"{icon} Событие сегодня",
                    body,
                    "Событие",
                    ev.ID_события,
                    "Средний"
                );
            }
        }
        
        /// Показать всплывающее окно 

        private void ShowToast(NotificationModel notification)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toastWindow = new Views.ToastNotificationWindow(
                    notification.Заголовок,
                    notification.Текст,
                    notification.ТипИконка,
                    notification.Приоритет
                );
                toastWindow.Show();
            });
        }

        /// Обновить частоту проверки дедлайнов

        public void UpdateCheckFrequency(int minutes)
        {
            if (_checkTimer != null)
            {
                _checkTimer.Stop();
                _checkTimer.Interval = TimeSpan.FromMinutes(minutes);
                _checkTimer.Start();
            }
        }
        public void RefreshNotifications()
        {
            CheckDeadlines(null, null);
        }
       

    }
}
