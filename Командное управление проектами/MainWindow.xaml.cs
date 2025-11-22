using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data;
using Командное_управление_проектами.Models;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Views;
using System.Net.Sockets;
using System.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using Microsoft.Win32;

using Color = System.Windows.Media.Color;
using Border = System.Windows.Controls.Border;


namespace Командное_управление_проектами

{

            
    public partial class MainWindow : Window
    {
        private UserModel _currentUser; 
        private List<TaskModel> tasks = new List<TaskModel>(); 
        private List<ProjectModel> _projects = new List<ProjectModel>(); 
        private List<TaskModel> _allTasks = new List<TaskModel>();
        private TcpClient chatClient;
        private NetworkStream chatStream;
        private Thread chatReceiveThread;
        private string chatUserName;
        private NotificationService _notificationService;
        private Button _activeMenuButton = null;
        private DateTime _currentMonth;

        public string _sessionId;
        public MainWindow(UserModel user)
        {
            // Обновление статусов проектов и задач при запуске
            DbHelper.UpdateProjectStatuses();
            DbHelper.UpdateTaskStatuses();

            InitializeComponent();
            _currentUser = user;

            // Инициализация сервиса уведомлений
            _notificationService = NotificationService.Instance;
            _notificationService.Initialize(_currentUser);
            _notificationService.NotificationsChanged += UpdateNotificationBadge;

            // Загружаем и применяем сохранённые настройки
            var userSettings = SettingsManager.GetSettings(_currentUser.ID);
            SettingsManager.ApplySettings(userSettings, _currentUser);

            // Подписываемся на событие смены темы
            ThemeManager.ThemeChanged += RefreshMenuButtonStyles;

            // Применяем стили меню при запуске
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshMenuButtonStyles();
            }), System.Windows.Threading.DispatcherPriority.Loaded);





            // Тестовое уведомление (удалите после проверки)
            _notificationService.AddNotification(
                "⚠️ Тестовое уведомление",
                "Это пример высокоприоритетного уведомления",
                "Задача",
                priority: "Высокий"
            );







            // Настройка видимости статических кнопок меню на основе роли
            if (PermissionHelper.HasPermission(_currentUser, "REPORTS_VIEW"))
            {
                ReportsButton.Visibility = Visibility.Visible;
            }
            if (PermissionHelper.HasPermission(_currentUser, "USER_MANAGE"))
            {
                UsersButton.Visibility = Visibility.Visible;
            }

            // Инициализация календаря и подключения к чату
            InitializeCalendar();
            ConnectToChat();

            // По умолчанию показываем календарь
            ShowCalendar(null, null);

            // Создаем сессию
            _sessionId = SessionManager.CreateSession(user);
        }
        protected override void OnClosed(EventArgs e)
        {
            // Завершаем сессию при закрытии
            if (_sessionId != null)
            {
                SessionManager.Logout(_sessionId);
            }

            // Останавливаем сервис уведомлений
            _notificationService?.Shutdown();

            // Отписываемся от события смены темы
            ThemeManager.ThemeChanged -= RefreshMenuButtonStyles;

            // Отключаем чат
            DisconnectChat();

            base.OnClosed(e);
        }

        // Метод скрытия всех панелей модулей
        private void HideAllPanels()
        {
            ProjectsPanel.Visibility = Visibility.Collapsed;
            TasksPanel.Visibility = Visibility.Collapsed;
            CalendarPanel.Visibility = Visibility.Collapsed;
            EventsPanel.Visibility = Visibility.Collapsed;
            RemindersPanel.Visibility = Visibility.Collapsed;
            NotesPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Collapsed;
            ReportPanel.Visibility = Visibility.Collapsed;
            UsersPanel.Visibility = Visibility.Collapsed;
            NotificationsPanel.Visibility = Visibility.Collapsed;
        }

        // Добавьте этот метод
        private void SetActiveMenuButton(Button button)
        {
            // Сбрасываем стиль предыдущей активной кнопки
            if (_activeMenuButton != null)
            {
                _activeMenuButton.Style = (Style)FindResource("MenuButtonStyle");
            }

            // Устанавливаем стиль для новой активной кнопки
            _activeMenuButton = button;
            if (_activeMenuButton != null)
            {
                try
                {
                    _activeMenuButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
                }
                catch
                {
                    // Если стиль не найден, используем обычный
                    _activeMenuButton.Style = (Style)FindResource("MenuButtonStyle");
                }
            }
        }

        // Метод отображения панели "Проекты"
        private void ShowProjects(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            ProjectsPanel.Visibility = Visibility.Visible;
            ApplyProjectFilters();

            bool canManageProjects = PermissionHelper.HasPermission(_currentUser, "PROJECT_CREATE");
            AddProjectBtn.Visibility = canManageProjects ? Visibility.Visible : Visibility.Collapsed;
            EditProjectBtn.Visibility = canManageProjects ? Visibility.Visible : Visibility.Collapsed;

            bool canDeleteProjects = PermissionHelper.HasPermission(_currentUser, "PROJECT_DELETE");
            DeleteProjectBtn.Visibility = canDeleteProjects ? Visibility.Visible : Visibility.Collapsed;

            SetActiveMenuButton(ProjectsButton);
        }


        private void AddProject_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.AddProjectWindow();
            window.ShowDialog();
            ApplyProjectFilters(); // Обновляем список проектов
        }

        private void EditProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsGrid.SelectedItem is ProjectModel selected)
            {
                // Открываем окно, явно указывая, что это редактирование (isNew: false)
                var window = new EditProjectWindow(selected, _currentUser, false);
                window.ShowDialog();
                ApplyProjectFilters(); // Обновляем список проектов
            }
            else
            {
                MessageBox.Show("Выберите проект для редактирования.");
            }
        }
        private void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            // ИЗМЕНЕНИЕ: Добавлен второй уровень защиты (проверка прав в самом методе)
            if (!PermissionHelper.HasPermission(_currentUser, "PROJECT_DELETE"))
            {
                MessageBox.Show("У вас недостаточно прав для выполнения этого действия.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1. Проверяем, выбран ли проект в таблице
            if (ProjectsGrid.SelectedItem is ProjectModel selectedProject)
            {
                // 2. Спрашиваем подтверждение у пользователя
                MessageBoxResult result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить проект '{selectedProject.Название_проекта}'?\n\nВНИМАНИЕ: Будут также удалены все связанные с ним задачи, подзадачи, файлы, события и записи в бюджете.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 3. Вызываем метод из DbHelper для удаления
                        DbHelper.DeleteProject(selectedProject.ID_проекта);
                        MessageBox.Show("Проект успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // 4. Обновляем список проектов на экране
                        ApplyProjectFilters();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении проекта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите проект для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyProjectFilters()
        {
            var projects = DbHelper.GetAllProjects();
            string nameFilter = ProjectNameSearchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(nameFilter))
            {
                projects = projects.FindAll(p => p.Название_проекта.ToLower().Contains(nameFilter));
            }

            string responsibleFilter = ProjectResponsibleSearchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(responsibleFilter))
            {
                projects = projects.FindAll(p => p.Ответственный.ToLower().Contains(responsibleFilter));
            }

            if (ProjectStartDatePicker.SelectedDate.HasValue && ProjectEndDatePicker.SelectedDate.HasValue)
            {
                DateTime startDate = ProjectStartDatePicker.SelectedDate.Value;
                DateTime endDate = ProjectEndDatePicker.SelectedDate.Value;
                projects = projects.FindAll(p => p.Дата_начала >= startDate && p.Дата_завершения <= endDate);
            }

            ProjectsGrid.ItemsSource = projects;
        }

        private void ProjectFilters_Changed(object sender, EventArgs e)
        {
            ApplyProjectFilters();
        }

        private void ResetProjectFiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            ProjectNameSearchBox.Text = string.Empty;
            ProjectResponsibleSearchBox.Text = string.Empty;
            ProjectStartDatePicker.SelectedDate = null;
            ProjectEndDatePicker.SelectedDate = null;
            ApplyProjectFilters();
        }

        // Метод отображения панели "Задачи"
        private void ShowTasks(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            TasksPanel.Visibility = Visibility.Visible;

            _projects = DbHelper.GetAllProjects();
            ProjectFilterBox.ItemsSource = _projects;
            ProjectFilterBox.SelectedIndex = -1;

            _allTasks = DbHelper.GetAllTasks();
            TasksGrid.ItemsSource = _allTasks;

            bool canManageTasks = PermissionHelper.HasPermission(_currentUser, "TASK_CREATE");
            AddTaskBtn.Visibility = canManageTasks ? Visibility.Visible : Visibility.Collapsed;
            EditTaskBtn.Visibility = canManageTasks ? Visibility.Visible : Visibility.Collapsed;

            SetActiveMenuButton(TasksButton);
        }

        private void ResetFiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            ProjectFilterBox.SelectedIndex = -1;
            TaskSearchBox.Text = string.Empty;
            ApplyTaskFilters();
        }

        private void EditTask_Click(object sender, RoutedEventArgs e)
        {
            if (TasksGrid.SelectedItem is TaskModel selected)
            {
                var editWindow = new EditTaskWindow(selected, _currentUser); // <-- Теперь два аргумента
                editWindow.ShowDialog();
                ShowTasks(null, null);
            }
            else
            {
                MessageBox.Show("Выберите задачу для редактирования.");
            }
        }

        private void ProjectFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyTaskFilters();
        }

        private void ApplyTaskFilters()
        {
            var tasks = DbHelper.GetAllTasks();
            if (ProjectFilterBox.SelectedValue is int projectId)
            {
                var projects = DbHelper.GetAllProjects();
                string projectName = projects.FirstOrDefault(p => p.ID_проекта == projectId)?.Название_проекта;
                if (!string.IsNullOrEmpty(projectName))
                {
                    tasks = tasks.FindAll(t => t.Название_проекта == projectName);
                }
            }

            string searchText = TaskSearchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                tasks = tasks.FindAll(t => t.Название_задачи.ToLower().Contains(searchText));
            }
            TasksGrid.ItemsSource = tasks;
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var win = new Views.AddTaskWindow();
            win.ShowDialog();
            ShowTasks(sender, e);
        }

        // Инициализация и обновление календаря
        private void InitializeCalendar()
        {
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            UpdateCalendar();
        }

        private void UpdateCalendar()
        {
            MonthYearLabel.Text = _currentMonth.ToString("MMMM yyyy");
            CalendarItemsControl.Items.Clear();

            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            int firstDayOfWeek = (int)_currentMonth.DayOfWeek;
            if (firstDayOfWeek == 0) firstDayOfWeek = 7;
            int offset = firstDayOfWeek - 1;

            // Получаем цвета из текущей темы
            var foregroundBrush = (SolidColorBrush)Application.Current.Resources["ForegroundBrush"];
            var borderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"];
            var backgroundBrush = (SolidColorBrush)Application.Current.Resources["BackgroundBrush"];
            var secondaryBackgroundBrush = (SolidColorBrush)Application.Current.Resources["SecondaryBackgroundBrush"];

            // Яркие контрастные цвета для элементов календаря
            var eventBrush = new SolidColorBrush(Color.FromRgb(100, 181, 246)); // Светло-синий
            var reminderBrush = new SolidColorBrush(Color.FromRgb(129, 199, 132)); // Светло-зелёный
            var noteBrush = new SolidColorBrush(Color.FromRgb(206, 147, 216)); // Светло-фиолетовый
            var taskBrush = new SolidColorBrush(Color.FromRgb(255, 183, 77)); // Светло-оранжевый

            // Добавляем пустые ячейки для дней предыдущего месяца
            for (int i = 0; i < offset; i++)
            {
                CalendarItemsControl.Items.Add(new Border
                {
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(2),
                    Background = secondaryBackgroundBrush,
                    CornerRadius = new CornerRadius(4)
                });
            }

            // Заполняем ячейки для текущего месяца
            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                var events = DbHelper.GetEventsByDate(date);
                var reminders = DbHelper.GetRemindersByDate(date);
                var notes = DbHelper.GetNotesByDate(date);
                var tasks = DbHelper.GetTasksByDate(date);

                // Определяем, есть ли записи на этот день
                bool hasItems = events.Any() || reminders.Any() || notes.Any() || tasks.Any();

                Border cellBorder = new Border
                {
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(2),
                    Padding = new Thickness(5),
                    Background = hasItems ? secondaryBackgroundBrush : backgroundBrush,
                    CornerRadius = new CornerRadius(4)
                };

                StackPanel cellPanel = new StackPanel();

                // Заголовок с номером дня - увеличенный и более заметный
                TextBlock dayHeader = new TextBlock
                {
                    Text = day.ToString(),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = foregroundBrush,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                cellPanel.Children.Add(dayHeader);

                // События с иконками и фоном
                foreach (var ev in events)
                {
                    var eventBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, 100, 181, 246)), // Полупрозрачный фон
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4, 2, 4, 2),
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    eventBorder.Child = new TextBlock
                    {
                        Text = "✨ " + (ev.Название_события.Length > 20
                            ? ev.Название_события.Substring(0, 20) + "..."
                            : ev.Название_события),
                        FontSize = 11,
                        Foreground = eventBrush,
                        TextWrapping = TextWrapping.Wrap,
                        FontWeight = FontWeights.SemiBold
                    };
                    cellPanel.Children.Add(eventBorder);
                }

                // Напоминания с иконками и фоном
                foreach (var rem in reminders)
                {
                    var reminderBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, 129, 199, 132)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4, 2, 4, 2),
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    reminderBorder.Child = new TextBlock
                    {
                        Text = "⏰ " + (rem.Length > 20 ? rem.Substring(0, 20) + "..." : rem),
                        FontSize = 11,
                        Foreground = reminderBrush,
                        TextWrapping = TextWrapping.Wrap,
                        FontWeight = FontWeights.SemiBold
                    };
                    cellPanel.Children.Add(reminderBorder);
                }

                // Заметки с иконками и фоном
                foreach (var note in notes)
                {
                    var noteBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, 206, 147, 216)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4, 2, 4, 2),
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    noteBorder.Child = new TextBlock
                    {
                        Text = "📝 " + (note.Текст_заметки.Length > 20
                            ? note.Текст_заметки.Substring(0, 20) + "..."
                            : note.Текст_заметки),
                        FontSize = 11,
                        Foreground = noteBrush,
                        TextWrapping = TextWrapping.Wrap,
                        FontWeight = FontWeights.SemiBold
                    };
                    cellPanel.Children.Add(noteBorder);
                }

                // Задачи с иконками и фоном
                foreach (var task in tasks)
                {
                    var taskBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, 255, 183, 77)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4, 2, 4, 2),
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    taskBorder.Child = new TextBlock
                    {
                        Text = "📋 " + (task.Название_задачи.Length > 20
                            ? task.Название_задачи.Substring(0, 20) + "..."
                            : task.Название_задачи),
                        FontSize = 11,
                        Foreground = taskBrush,
                        TextWrapping = TextWrapping.Wrap,
                        FontWeight = FontWeights.SemiBold
                    };
                    cellPanel.Children.Add(taskBorder);
                }

                // Если элементов слишком много, показываем счётчик
                int totalItems = events.Count + reminders.Count + notes.Count + tasks.Count;
                if (totalItems > 4)
                {
                    cellPanel.Children.Add(new TextBlock
                    {
                        Text = $"+ ещё {totalItems - 4}",
                        FontSize = 10,
                        Foreground = foregroundBrush,
                        FontStyle = FontStyles.Italic,
                        Margin = new Thickness(0, 5, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }

                cellBorder.Child = cellPanel;
                CalendarItemsControl.Items.Add(cellBorder);
            }
        }

        private void PrevMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            UpdateCalendar();
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            UpdateCalendar();
        }

        // Отображение панели "Календарь"
        private void ShowCalendar(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            CalendarPanel.Visibility = Visibility.Visible;
            UpdateCalendar();
            SetActiveMenuButton(CalendarButton);
        }

        // Отображение панели "События"
        private void ShowEvents(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            EventsPanel.Visibility = Visibility.Visible;

            bool canManageEvents = PermissionHelper.HasPermission(_currentUser, "EVENT_MANAGE");
            AddEventBtn.Visibility = canManageEvents ? Visibility.Visible : Visibility.Collapsed;
            EditEventBtn.Visibility = canManageEvents ? Visibility.Visible : Visibility.Collapsed;
            DeleteEventBtn.Visibility = canManageEvents ? Visibility.Visible : Visibility.Collapsed;

            LoadEventsData();
            SetActiveMenuButton(EventsButton);
        }

        private void LoadEventsData()
        {
            var allEvents = DbHelper.GetAllEvents();
            EventsGrid.ItemsSource = allEvents;
        }

        private void AddEventBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.AddEventWindow();
            window.ShowDialog();
            LoadEventsData();
        }

        private void EditEventBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EventsGrid.SelectedItem is EventModel selectedEvent)
            {
                var window = new Views.EditEventWindow(selectedEvent);
                window.ShowDialog();
                LoadEventsData();
            }
            else
            {
                MessageBox.Show("Выберите событие для редактирования.");
            }
        }

        private void DeleteEventBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EventsGrid.SelectedItem is EventModel selectedEvent)
            {
                if (MessageBox.Show($"Удалить событие \"{selectedEvent.Название_события}\"?",
                                    "Подтверждение",
                                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DbHelper.DeleteEvent(selectedEvent.ID_события);
                    LoadEventsData();
                }
            }
            else
            {
                MessageBox.Show("Выберите событие для удаления.");
            }
        }

        // Отображение панели "Напоминания"
        private void ShowReminders(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            RemindersPanel.Visibility = Visibility.Visible;
            LoadRemindersData();
            SetActiveMenuButton(RemindersButton);
        }

        private void LoadRemindersData()
        {
            var reminders = DbHelper.GetAllReminders();
            RemindersGrid.ItemsSource = reminders;
        }

        private void AddReminderBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.AddReminderWindow();
            window.ShowDialog();
            LoadRemindersData();
        }

        private void EditReminderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RemindersGrid.SelectedItem is ReminderModel selectedReminder)
            {
                var window = new Views.EditReminderWindow(selectedReminder);
                window.ShowDialog();
                LoadRemindersData();
            }
            else
            {
                MessageBox.Show("Выберите напоминание для редактирования.");
            }
        }

        private void DeleteReminderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RemindersGrid.SelectedItem is ReminderModel selectedReminder)
            {
                if (MessageBox.Show($"Удалить напоминание \"{selectedReminder.Текст_напоминания}\"?",
                                    "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DbHelper.DeleteReminder(selectedReminder.ID_напоминания);
                    LoadRemindersData();
                }
            }
            else
            {
                MessageBox.Show("Выберите напоминание для удаления.");
            }
        }

        // Отображение панели "Заметки"
        private void ShowNotes(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            NotesPanel.Visibility = Visibility.Visible;
            LoadNotesData();
            SetActiveMenuButton(NotesButton);
        }

        private void LoadNotesData()
        {
            var notes = DbHelper.GetAllNotes();
            NotesGrid.ItemsSource = notes;
        }

        private void AddNoteBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.AddNoteWindow();
            window.ShowDialog();
            LoadNotesData();
        }

        private void EditNoteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NotesGrid.SelectedItem is NoteModel selectedNote)
            {
                var window = new Views.EditNoteWindow(selectedNote);
                window.ShowDialog();
                LoadNotesData();
            }
            else
            {
                MessageBox.Show("Выберите заметку для редактирования.");
            }
        }

        private void DeleteNoteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NotesGrid.SelectedItem is NoteModel selectedNote)
            {
                if (MessageBox.Show($"Удалить заметку \"{selectedNote.Текст_заметки}\"?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DbHelper.DeleteNote(selectedNote.ID_заметки);
                    LoadNotesData();
                }
            }
            else
            {
                MessageBox.Show("Выберите заметку для удаления.");
            }
        }

        // Отображение панели "Пользователи"
        private void ShowUsers(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            UsersPanel.Visibility = Visibility.Visible;
            LoadUsersData();
            SetActiveMenuButton(UsersButton);
        }

        private void LoadUsersData()
        {
            var users = DbHelper.GetAllUsers();
            UsersGrid.ItemsSource = users;
        }

        private void AddUserBtn_Click(object sender, RoutedEventArgs e)
        {
            Views.AddUserWindow win = new Views.AddUserWindow();
            win.ShowDialog();
            ShowUsers(sender, e);
        }

        // Отображение панели "Отчёты"
        private void ShowReports(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            ReportPanel.Visibility = Visibility.Visible;
            LoadReportsData();
            SetActiveMenuButton(ReportsButton);
        }

        private void LoadReportsData()
        {
            var projects = DbHelper.GetAllProjects();
            var tasks = DbHelper.GetAllTasks();
            var resources = DbHelper.GetAllResources();

            // Определяем, используется ли тёмная тема
            var isDarkTheme = ThemeManager.GetCurrentTheme() == "Тёмная";
            var textColor = isDarkTheme ? Brushes.White : Brushes.Black;
            var gridColor = isDarkTheme
                ? new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46))
                : new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));

            // Обновляем карточки статистики
            TotalProjectsCard.Text = projects.Count.ToString();
            TotalTasksCard.Text = tasks.Count.ToString();
            CompletedTasksCard.Text = tasks.Count(t => t.Статус == "Завершена").ToString();
            TotalBudgetCard.Text = $"{projects.Sum(p => p.Бюджет):N0} ₽";

            // 1. Круговая диаграмма: Проекты по статусам
            var projectsByStatus = projects
                .GroupBy(p => p.Статус)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            ProjectsStatusPieChart.Series = new SeriesCollection();
            ProjectsStatusPieChart.LegendLocation = LegendLocation.Bottom;
            ProjectsStatusPieChart.Foreground = textColor;

            foreach (var group in projectsByStatus)
            {
                ProjectsStatusPieChart.Series.Add(new PieSeries
                {
                    Title = group.Status,
                    Values = new ChartValues<int> { group.Count },
                    DataLabels = true,
                    LabelPoint = point => $"{point.Y} ({point.Participation:P0})",
                    Foreground = textColor
                });
            }

            // 2. Столбчатая диаграмма: Задачи по статусам
            var tasksByStatus = tasks
                .GroupBy(t => t.Статус)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            TasksStatusBarChart.Series = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Задачи",
            Values = new ChartValues<int>(tasksByStatus.Select(x => x.Count)),
            DataLabels = true,
            Foreground = textColor
        }
    };
            TasksStatusBarChart.AxisX[0].Labels = tasksByStatus.Select(x => x.Status).ToList();
            TasksStatusBarChart.AxisX[0].Foreground = textColor;
            TasksStatusBarChart.AxisX[0].Separator.Stroke = gridColor;
            TasksStatusBarChart.AxisY[0].Foreground = textColor;
            TasksStatusBarChart.AxisY[0].Separator.Stroke = gridColor;
            TasksStatusBarChart.Foreground = textColor;

            // 3. Круговая диаграмма: Задачи по приоритетам
            var tasksByPriority = tasks
                .GroupBy(t => t.Приоритет ?? "Не указан")
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToList();

            TasksPriorityPieChart.Series = new SeriesCollection();
            TasksPriorityPieChart.LegendLocation = LegendLocation.Bottom;
            TasksPriorityPieChart.Foreground = textColor;

            foreach (var group in tasksByPriority)
            {
                TasksPriorityPieChart.Series.Add(new PieSeries
                {
                    Title = group.Priority,
                    Values = new ChartValues<int> { group.Count },
                    DataLabels = true,
                    LabelPoint = point => $"{point.Y} ({point.Participation:P0})",
                    Foreground = textColor
                });
            }

            // 4. Столбчатая диаграмма: ТОП-5 проектов по бюджету
            var topProjects = projects
                .OrderByDescending(p => p.Бюджет)
                .Take(5)
                .ToList();

            ProjectsBudgetBarChart.Series = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Бюджет",
            Values = new ChartValues<decimal>(topProjects.Select(p => p.Бюджет)),
            DataLabels = true,
            LabelPoint = point => $"{point.Y:N0} ₽",
            Foreground = textColor
        }
    };
            ProjectsBudgetBarChart.AxisX[0].Labels = topProjects.Select(p => p.Название_проекта).ToList();
            ProjectsBudgetBarChart.AxisX[0].Foreground = textColor;
            ProjectsBudgetBarChart.AxisX[0].Separator.Stroke = gridColor;
            ProjectsBudgetBarChart.AxisY[0].Foreground = textColor;
            ProjectsBudgetBarChart.AxisY[0].Separator.Stroke = gridColor;
            ProjectsBudgetBarChart.Foreground = textColor;

            // 5. Анализ использования ресурсов
            var resourcesWithUsage = resources.Select(r => new
            {
                r.ID_ресурса,
                r.Название,
                r.Тип,
                r.Количество,
                ПроектовИспользуется = GetResourceUsageCount(r.ID_ресурса)
            }).ToList();

            ResourcesUsageGrid.ItemsSource = resourcesWithUsage;

            // 6. Просроченные задачи
            var overdueTasks = tasks
                .Where(t => t.Дата_завершения.HasValue &&
                            t.Дата_завершения.Value < DateTime.Now &&
                            t.Статус != "Завершена")
                .Select(t => new
                {
                    t.ID_задачи,
                    t.Название_задачи,
                    t.Название_проекта,
                    t.Ответственный,
                    ДнейПросрочено = (DateTime.Now - t.Дата_завершения.Value).Days
                })
                .OrderByDescending(t => t.ДнейПросрочено)
                .ToList();

            OverdueTasksGrid.ItemsSource = overdueTasks;
        }

        private int GetResourceUsageCount(int resourceId)
        {
            // Здесь нужно добавить запрос к БД для подсчёта проектов, использующих ресурс
            // Пока возвращаем 0, позже добавим метод в DbHelper
            return 0;
        }

        // Экспорт в PDF
        private void ExportToPDF_Click(object sender, RoutedEventArgs e)
        {
            var selectedType = (ExportTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF файл (*.pdf)|*.pdf",
                Title = $"Сохранить отчёт '{selectedType}' в PDF"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (selectedType == "Проекты")
                    {
                        var projects = DbHelper.GetAllProjects();
                        ReportService.GenerateProjectsReportPDF(saveFileDialog.FileName, projects);
                    }
                    else if (selectedType == "Задачи")
                    {
                        var tasks = DbHelper.GetAllTasks();
                        ReportService.GenerateTasksReportPDF(saveFileDialog.FileName, tasks);
                    }

                    MessageBox.Show($"PDF отчёт успешно сохранён:\n{saveFileDialog.FileName}",
                        "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Предлагаем открыть файл
                    if (MessageBox.Show("Открыть созданный файл?", "Открыть",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(saveFileDialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании PDF: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Новый экспорт в Excel
        private void ExportToExcelNew_Click(object sender, RoutedEventArgs e)
        {
            var selectedType = (ExportTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                Title = $"Сохранить отчёт '{selectedType}' в Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (selectedType == "Проекты")
                    {
                        var projects = DbHelper.GetAllProjects();
                        ReportService.ExportProjectsToExcel(saveFileDialog.FileName, projects);
                    }
                    else if (selectedType == "Задачи")
                    {
                        var tasks = DbHelper.GetAllTasks();
                        ReportService.ExportTasksToExcel(saveFileDialog.FileName, tasks);
                    }

                    MessageBox.Show($"Excel отчёт успешно сохранён:\n{saveFileDialog.FileName}",
                        "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (MessageBox.Show("Открыть созданный файл?", "Открыть",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(saveFileDialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании Excel: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Отображение панели "Чат"
        private void ShowChat(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            ChatPanel.Visibility = Visibility.Visible;
            ConnectToChat();
            SetActiveMenuButton(ChatButton);
        }

        // Подключение к серверу чата
        private void ConnectToChat()
        {
            if (chatClient != null && chatClient.Connected)
                return;

            chatUserName = _currentUser.Имя;
            try
            {
                chatClient = new TcpClient("127.0.0.1", 8888);
                chatStream = chatClient.GetStream();

                chatReceiveThread = new Thread(ReceiveChatMessages);
                chatReceiveThread.IsBackground = true;
                chatReceiveThread.Start();

                ChatTextBox.Text += $"{GetTimestamp()} Подключен к серверу\n";
                SendButton.IsEnabled = true;
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к серверу чата.");
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                string formattedMessage = $"{GetTimestamp()} {chatUserName}: {message}";
                SendChatMessage(formattedMessage);
                MessageTextBox.Clear();
            }
        }

        private void ReceiveChatMessages()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    int byteCount = chatStream.Read(buffer, 0, buffer.Length);
                    if (byteCount == 0) break;
                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    Dispatcher.Invoke(() => { ChatTextBox.Text += message + "\n"; });
                }
            }
            catch
            {
            }
        }

        private void SendChatMessage(string message)
        {
            if (chatStream != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                chatStream.Write(data, 0, data.Length);
            }
        }

        private void DisconnectChat()
        {
            try
            {
                if (chatStream != null)
                {
                    SendChatMessage($"{GetTimestamp()} {chatUserName} покинул чат");
                    chatStream.Close();
                    chatClient.Close();
                    if (chatReceiveThread != null && chatReceiveThread.IsAlive)
                        chatReceiveThread.Join();
                }
            }
            catch { }
        }

        private string GetTimestamp()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
        private void TaskSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyTaskFilters();
        }
        private void EditUserBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is UserModel selectedUser)
            {
                Views.EditUserWindow editWin = new Views.EditUserWindow(selectedUser);
                editWin.ShowDialog();
                LoadUsersData();
            }
            else
            {
                MessageBox.Show("Выберите пользователя для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteUserBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is UserModel selectedUser)
            {
                if (MessageBox.Show($"Удалить пользователя \"{selectedUser.Имя}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    DbHelper.DeleteUser(selectedUser.ID);
                    LoadUsersData();
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TasksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверяем, выбрана ли задача
            if (TasksGrid.SelectedItem is TaskModel selectedTask)
            {
                // Показываем правую панель
                TaskDetailsPanel.Visibility = Visibility.Visible;

                // Заполняем информацию о выбранной задаче
                SelectedTaskName.Text = selectedTask.Название_задачи;
                SelectedTaskDescription.Text = selectedTask.Описание;

                // Загружаем подзадачи в таблицу на правой панели
                SubtasksGrid.ItemsSource = selectedTask.Subtasks;
            }
            else
            {
                // Если ничего не выбрано, скрываем панель
                TaskDetailsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ExportProjectsToWorksheet(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("Проекты");
            var projects = DbHelper.GetAllProjects();

            string[] headers = { "ID", "Название проекта", "Описание", "Дата начала", "Дата завершения", "Статус", "Ответственный", "Бюджет" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            worksheet.Cells["A2"].LoadFromCollection(projects.Select(p => new {
                p.ID_проекта,
                p.Название_проекта,
                p.Описание,
                Дата_начала = p.Дата_начала?.ToString("dd.MM.yyyy"),
                Дата_завершения = p.Дата_завершения?.ToString("dd.MM.yyyy"),
                p.Статус,
                p.Ответственный,
                p.Бюджет
            }), false);

            // Стилизация
            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                // ИСПРАВЛЕНО ЗДЕСЬ:
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#007ACC"));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(8).Style.Numberformat.Format = "#,##0.00 ₽";
        }

        private void ExportTasksToWorksheet(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("Задачи");
            var tasks = DbHelper.GetAllTasks();

            string[] headers = { "ID", "Название задачи", "Описание", "Проект", "Ответственный", "Приоритет", "Статус", "Срок выполнения" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            worksheet.Cells["A2"].LoadFromCollection(tasks.Select(t => new {
                t.ID_задачи,
                t.Название_задачи,
                t.Описание,
                t.Название_проекта,
                t.Ответственный,
                t.Приоритет,
                t.Статус,
                Срок_выполнения = t.Дата_завершения?.ToString("dd.MM.yyyy")
            }), false);

            // Стилизация
            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                // И ИСПРАВЛЕНО ЗДЕСЬ:
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#007ACC"));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }
        private void ShowGanttChart_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, какая задача выбрана в основной таблице
            if (TasksGrid.SelectedItem is TaskModel selectedTask)
            {
                // Создаем и показываем окно с диаграммой, передавая в него выбранную задачу
                var ganttWindow = new GanttChartWindow(selectedTask);
                ganttWindow.Show();
            }
            else
            {
                MessageBox.Show("Сначала выберите задачу в основной таблице.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        /// Обновление счётчика непрочитанных уведомлений
        private void UpdateNotificationBadge()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int unreadCount = _notificationService.GetUnreadCount();

                // Обновляем текст на кнопке уведомлений (если она есть)
                // Пока просто выводим в заголовок окна количество непрочитанных
                if (unreadCount > 0)
                {
                    this.Title = $"Управление проектами ({unreadCount} новых уведомлений)";
                }
                else
                {
                    this.Title = "Управление проектами";
                }
            });
        }
        // ==================== УВЕДОМЛЕНИЯ ====================
        private void ShowNotifications(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            NotificationsPanel.Visibility = Visibility.Visible;
            LoadNotifications();
            SetActiveMenuButton(NotificationsButton);
        }

        private void LoadNotifications()
        {
            try
            {
                var allNotifications = _notificationService.GetAllNotifications();

                // Применяем фильтры
                var filtered = allNotifications.AsEnumerable();

                // Фильтр по прочитанности (проверяем на null)
                if (UnreadNotificationsRadio != null && UnreadNotificationsRadio.IsChecked == true)
                {
                    filtered = filtered.Where(n => !n.Прочитано);
                }

                // Фильтр по типу (проверяем на null)
                if (NotificationTypeFilter != null && NotificationTypeFilter.SelectedItem != null)
                {
                    var selectedType = (NotificationTypeFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
                    if (selectedType != null && selectedType != "Все типы")
                    {
                        filtered = filtered.Where(n => n.Тип == selectedType);
                    }
                }

                var filteredList = filtered.ToList();

                // Добавляем свойства для UI
                foreach (var notification in filteredList)
                {
                    // Получаем текущую тему
                    var isDarkTheme = ThemeManager.GetCurrentTheme() == "Тёмная";

                    // Фон для уведомлений с учётом темы
                    if (notification.Прочитано)
                    {
                        // Прочитанные уведомления - более тёмный/светлый фон
                        notification.BackgroundColor = isDarkTheme
                            ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)) // Очень тёмный
                            : new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)); // Светло-серый
                    }
                    else
                    {
                        // Непрочитанные уведомления - цвет зависит от приоритета
                        if (isDarkTheme)
                        {
                            // Тёмная тема
                            switch (notification.Приоритет)
                            {
                                case "Высокий":
                                    // Тёмно-красный оттенок
                                    notification.BackgroundColor = new SolidColorBrush(Color.FromRgb(0x3D, 0x20, 0x20));
                                    break;
                                case "Средний":
                                    // Тёмно-оранжевый оттенок
                                    notification.BackgroundColor = new SolidColorBrush(Color.FromRgb(0x38, 0x2F, 0x20));
                                    break;
                                case "Низкий":
                                    // Тёмно-синий оттенок
                                    notification.BackgroundColor = new SolidColorBrush(Color.FromRgb(0x20, 0x2D, 0x35));
                                    break;
                                default:
                                    // Стандартный серый
                                    notification.BackgroundColor = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
                                    break;
                            }
                        }
                        else
                        {
                            // Светлая тема
                            switch (notification.Приоритет)
                            {
                                case "Высокий":
                                    // Светло-красный
                                    notification.BackgroundColor = new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0xEE));
                                    break;
                                case "Средний":
                                    // Светло-оранжевый
                                    notification.BackgroundColor = new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xE0));
                                    break;
                                case "Низкий":
                                    // Светло-зелёный
                                    notification.BackgroundColor = new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9));
                                    break;
                                default:
                                    // Жёлтый для непрочитанных по умолчанию
                                    notification.BackgroundColor = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xE0));
                                    break;
                            }
                        }
                    }

                    // Видимость кнопки "Прочитано"
                    notification.ReadButtonVisibility = notification.Прочитано
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                }

                if (NotificationsListBox != null)
                {
                    NotificationsListBox.ItemsSource = filteredList;
                }

                // Показываем сообщение, если уведомлений нет
                if (NoNotificationsText != null)
                {
                    NoNotificationsText.Visibility = filteredList.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                UpdateNotificationBadge();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkAsRead_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int notificationId)
            {
                _notificationService.MarkAsRead(notificationId);
                LoadNotifications();
            }
        }

        private void DeleteNotification_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int notificationId)
            {
                if (MessageBox.Show("Удалить это уведомление?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _notificationService.DeleteNotification(notificationId);
                    LoadNotifications();
                }
            }
        }

        private void MarkAllAsRead_Click(object sender, RoutedEventArgs e)
        {
            _notificationService.MarkAllAsRead();
            LoadNotifications();
        }

        private void ClearReadNotifications_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Удалить все прочитанные уведомления?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _notificationService.ClearReadNotifications();
                LoadNotifications();
            }
        }

        private void RefreshNotifications_Click(object sender, RoutedEventArgs e)
        {
            _notificationService.RefreshNotifications();
            LoadNotifications();
        }

        private void NotificationFilter_Changed(object sender, RoutedEventArgs e)
        {
            // Проверяем, что панель уведомлений видима
            if (NotificationsPanel != null && NotificationsPanel.Visibility == Visibility.Visible)
            {
                LoadNotifications();
            }
        }
        private void ShowProfile(object sender, RoutedEventArgs e)
        {
            var profileWindow = new UserProfileWindow(_currentUser);
            profileWindow.ShowDialog();

            // Перезагружаем настройки после закрытия профиля
            var userSettings = SettingsManager.GetSettings(_currentUser.ID);
            SettingsManager.ApplySettings(userSettings, _currentUser);
        }
        // Обновляет стили всех кнопок меню после смены темы
        private void RefreshMenuButtonStyles()
        {
            try
            {
                var menuButtonStyle = (Style)FindResource("MenuButtonStyle");
                var activeMenuButtonStyle = (Style)FindResource("ActiveMenuButtonStyle");

                // Обновляем стили всех кнопок меню
                CalendarButton.Style = menuButtonStyle;
                ProjectsButton.Style = menuButtonStyle;
                TasksButton.Style = menuButtonStyle;
                EventsButton.Style = menuButtonStyle;
                RemindersButton.Style = menuButtonStyle;
                NotesButton.Style = menuButtonStyle;
                NotificationsButton.Style = menuButtonStyle;
                ChatButton.Style = menuButtonStyle;
                ReportsButton.Style = menuButtonStyle;
                UsersButton.Style = menuButtonStyle;
                ProfileButton.Style = menuButtonStyle;

                // Применяем активный стиль к текущей активной кнопке
                if (_activeMenuButton != null)
                {
                    _activeMenuButton.Style = activeMenuButtonStyle;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления стилей меню: {ex.Message}");
            }
        }
    }
}
