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

            // Запускаем таймер проверки дедлайнов каждые 30 минут
            _checkTimer = new DispatcherTimer();
            _checkTimer.Interval = TimeSpan.FromMinutes(30);
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
            var reminders = DbHelper.GetRemindersByDate(date);
            foreach (var reminderText in reminders)
            {
                // Проверяем, не создано ли уже уведомление для этого напоминания
                bool alreadyNotified = _notifications.Any(n =>
                    n.Тип == "Напоминание" &&
                    n.Текст == reminderText &&
                    n.Дата_создания.Date == date);

                if (!alreadyNotified)
                {
                    AddNotification(
                        "🔔 Напоминание",
                        reminderText,
                        "Напоминание",
                        priority: "Средний"
                    );
                }
            }
        }
        private void CheckEvents(DateTime date)
        {
            var events = DbHelper.GetEventsByDate(date);
            foreach (var ev in events)
            {
                // Проверяем, не создано ли уже уведомление для этого события
                bool alreadyNotified = _notifications.Any(n =>
                    n.Тип == "Событие" &&
                    n.ID_связанного_объекта == ev.ID_события &&
                    n.Дата_создания.Date == date);

                if (!alreadyNotified)
                {
                    AddNotification(
                        "📅 Событие сегодня",
                        $"{ev.Название_события}" + (!string.IsNullOrEmpty(ev.Описание) ? $" - {ev.Описание}" : ""),
                        "Событие",
                        ev.ID_события,
                        "Средний"
                    );
                }
            }
        }
        /// Показать всплывающее окно (Toast notification)
        /// </summary>
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
        /// <summary>
        /// Обновить частоту проверки дедлайнов
        /// </summary>
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
