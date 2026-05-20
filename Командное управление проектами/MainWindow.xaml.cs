using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;
using Командное_управление_проектами.Views;

using System.Windows.Controls.Primitives;
using Border = System.Windows.Controls.Border;
using Color = System.Windows.Media.Color;


namespace Командное_управление_проектами

{

            
    public partial class MainWindow : Window
    {
        private UserModel _currentUser; 
        private List<TaskModel> tasks = new List<TaskModel>(); 
        private List<ProjectModel> _projects = new List<ProjectModel>(); 
        private List<TaskModel> _allTasks = new List<TaskModel>();
        private NotificationService _notificationService;
        private Button _activeMenuButton = null;
        private DateTime _currentMonth;
        private HubConnection _hubConnection;
        private System.Windows.Threading.DispatcherTimer _typingTimer;
        private DateTime _lastTypingNotification = DateTime.MinValue;
        private List<HistoryModel> _allHistory = new List<HistoryModel>();

        private string _eventTypeFilter = "Все";
        private string _eventSearchText = "";

        private Popup _dayDetailPopup;
        private TextBlock _popupDateLabel;
        private ItemsControl _popupItemsControl;

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

            // Сначала скрываем все ограниченные кнопки
            ReportsButton.Visibility = Visibility.Collapsed;
            UsersButton.Visibility = Visibility.Collapsed;
            HistoryButton.Visibility = Visibility.Collapsed;

            // Настройка видимости статических кнопок меню на основе роли
            if (PermissionHelper.HasPermission(_currentUser, "REPORTS_VIEW"))
            {
                ReportsButton.Visibility = Visibility.Visible;
            }
            if (PermissionHelper.HasPermission(_currentUser, "USER_MANAGE"))
            {
                UsersButton.Visibility = Visibility.Visible;
            }
            // Показываем кнопку "История изменений" только для администраторов
            if (PermissionHelper.HasPermission(_currentUser, "ADMIN_FULL"))
            {
                HistoryButton.Visibility = Visibility.Visible;
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
            HistoryPanel.Visibility = Visibility.Collapsed;
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
            // Логика прав доступа:
            bool canManageProjects = PermissionHelper.HasPermission(_currentUser, "PROJECT_CREATE");
            // Добавление проекта - только по правам (например, Админ/Менеджер)
            AddProjectBtn.Visibility = canManageProjects ? Visibility.Visible : Visibility.Collapsed;
            EditProjectBtn.Visibility = Visibility.Visible;
            // Удаление - только по правам
            bool canDeleteProjects = PermissionHelper.HasPermission(_currentUser, "PROJECT_DELETE");
            DeleteProjectBtn.Visibility = canDeleteProjects ? Visibility.Visible : Visibility.Collapsed;
            SetActiveMenuButton(ProjectsButton);
        }


        private void AddProject_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.AddProjectWindow();
            window.Owner = this;
            window.ShowDialog();
            ApplyProjectFilters(); // Обновляем список проектов
        }

        private void EditProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsGrid.SelectedItem is ProjectModel selected)
            {
                var window = new EditProjectWindow(selected, _currentUser, false);
                window.Owner = this;
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

            // Логика прав доступа:
            bool canManageTasks = PermissionHelper.HasPermission(_currentUser, "TASK_CREATE");

            // ИЗМЕНЕНИЕ: Дополнительная проверка - если роль "Пользователь", скрываем кнопку добавления
            // Даже если в БД есть право TASK_CREATE, этот код принудительно скроет кнопку для обычных пользователей
            if (_currentUser.Роль == "Пользователь")
            {
                AddTaskBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                AddTaskBtn.Visibility = canManageTasks ? Visibility.Visible : Visibility.Collapsed;
            }

            // Кнопку редактирования оставляем как есть (или тоже можно открыть для всех, если нужно)
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
                var editWindow = new EditTaskWindow(selected, _currentUser);
                editWindow.Owner = this;
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
            var window = new Views.AddTaskWindow(_currentUser);
            window.Owner = this;
            window.ShowDialog();
            ApplyTaskFilters();
        }

        // Инициализация и обновление календаря
        private void InitializeCalendar()
        {
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            UpdateCalendar();
        }

        private void UpdateCalendar()
        {
            MonthYearLabel.Text = _currentMonth.ToString("MMMM yyyy",
                new System.Globalization.CultureInfo("ru-RU"));
            CalendarItemsControl.Items.Clear();

            int firstDayOfWeek = (int)_currentMonth.DayOfWeek;
            if (firstDayOfWeek == 0) firstDayOfWeek = 7;
            int offset       = firstDayOfWeek - 1;
            DateTime gridStart = _currentMonth.AddDays(-offset);
            DateTime gridEnd   = gridStart.AddDays(41);

            var allEvents    = DbHelper.GetEventsInRange(gridStart, gridEnd);
            var allTasks     = DbHelper.GetTasksInRange(gridStart, gridEnd);
            var allReminders = DbHelper.GetRemindersInRange(gridStart, gridEnd);

            bool showEvents    = ToggleEvents.IsChecked == true;
            bool showTasks     = ToggleTasks.IsChecked == true;
            bool showReminders = ToggleReminders.IsChecked == true;

            bool isDark = ThemeManager.GetCurrentTheme() == "Тёмная";

            var foregroundBrush = (SolidColorBrush)Application.Current.Resources["ForegroundBrush"];
            var borderBrush     = (SolidColorBrush)Application.Current.Resources["BorderBrush"];
            var backgroundBrush = (SolidColorBrush)Application.Current.Resources["BackgroundBrush"];

            var eventBrush    = (SolidColorBrush)Application.Current.Resources["EventMeetingBrush"];
            var taskBrush     = (SolidColorBrush)Application.Current.Resources["EventDeadlineBrush"];
            var reminderBrush = (SolidColorBrush)Application.Current.Resources["EventOtherBrush"];

            Color todayCellBg = isDark ? Color.FromRgb(62, 62, 66)  : Color.FromRgb(227, 242, 253);
            Color hoverBg     = isDark ? Color.FromRgb(45, 45, 48)  : Color.FromRgb(245, 245, 245);
            Color todayMarker = isDark ? Color.FromRgb(14, 99, 156)  : Color.FromRgb(33, 150, 243);

            for (int i = 0; i < 42; i++)
            {
                DateTime cellDate      = gridStart.AddDays(i);
                bool isCurrentMonth    = cellDate.Month == _currentMonth.Month;
                bool isToday           = cellDate.Date == DateTime.Today;

                var dayEvents = showEvents
                    ? allEvents.Where(e => e.Дата_начала.HasValue
                        && e.Дата_начала.Value.Date == cellDate.Date).ToList()
                    : new List<EventModel>();
                var dayTasks = showTasks
                    ? allTasks.Where(t => t.Дата_завершения.HasValue
                        && t.Дата_завершения.Value.Date == cellDate.Date).ToList()
                    : new List<TaskModel>();
                var dayReminders = showReminders
                    ? allReminders.Where(r => r.Дата_напоминания.HasValue
                        && r.Дата_напоминания.Value.Date == cellDate.Date).ToList()
                    : new List<ReminderModel>();

                int totalItems  = dayEvents.Count + dayTasks.Count + dayReminders.Count;
                var cellContent = new StackPanel { Margin = new Thickness(2) };

                if (isToday)
                {
                    var circle = new Border
                    {
                        Width               = 28,
                        Height              = 28,
                        CornerRadius        = new CornerRadius(14),
                        Background          = new SolidColorBrush(todayMarker),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin              = new Thickness(0, 0, 0, 3)
                    };
                    circle.Child = new TextBlock
                    {
                        Text                = cellDate.Day.ToString(),
                        FontWeight          = FontWeights.Bold,
                        FontSize            = 13,
                        Foreground          = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment   = VerticalAlignment.Center
                    };
                    cellContent.Children.Add(circle);
                }
                else
                {
                    cellContent.Children.Add(new TextBlock
                    {
                        Text       = cellDate.Day.ToString(),
                        FontWeight = FontWeights.SemiBold,
                        FontSize   = 13,
                        Foreground = foregroundBrush,
                        Opacity    = isCurrentMonth ? 1.0 : 0.4,
                        Margin     = new Thickness(0, 0, 0, 3)
                    });
                }

                int shown = 0;
                foreach (var ev in dayEvents)
                {
                    if (shown >= 3) break;
                    cellContent.Children.Add(BuildCalendarCard(ev.Название_события, eventBrush, ev));
                    shown++;
                }
                foreach (var task in dayTasks)
                {
                    if (shown >= 3) break;
                    cellContent.Children.Add(BuildCalendarCard(task.Название_задачи, taskBrush, task));
                    shown++;
                }
                foreach (var rem in dayReminders)
                {
                    if (shown >= 3) break;
                    cellContent.Children.Add(BuildCalendarCard(rem.Текст_напоминания, reminderBrush, rem));
                    shown++;
                }

                if (totalItems > 3)
                {
                    int extra                = totalItems - 3;
                    var capturedDate         = cellDate;
                    var capturedDayEvents    = dayEvents;
                    var capturedDayTasks     = dayTasks;
                    var capturedDayReminders = dayReminders;
                    var moreLink = new TextBlock
                    {
                        Text       = $"Ещё +{extra}",
                        FontSize   = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = eventBrush,
                        Cursor     = Cursors.Hand,
                        Margin     = new Thickness(4, 2, 0, 0)
                    };
                    moreLink.MouseLeftButtonUp += (s, args) =>
                        ShowDayDetailPopup(capturedDate, capturedDayEvents,
                            capturedDayTasks, capturedDayReminders, (UIElement)s);
                    cellContent.Children.Add(moreLink);
                }

                Color normalBg       = isToday ? todayCellBg : backgroundBrush.Color;
                var capturedNormal   = normalBg;
                var capturedHover    = hoverBg;

                var cellBorder = new Border
                {
                    BorderBrush     = borderBrush,
                    BorderThickness = new Thickness(1),
                    Margin          = new Thickness(2),
                    Padding         = new Thickness(5),
                    Background      = new SolidColorBrush(normalBg),
                    CornerRadius    = new CornerRadius(4),
                    MinHeight       = 90,
                    Opacity         = isCurrentMonth ? 1.0 : 0.65,
                    Child           = cellContent
                };

                cellBorder.MouseEnter += (s, args) =>
                    ((Border)s).Background = new SolidColorBrush(capturedHover);
                cellBorder.MouseLeave += (s, args) =>
                    ((Border)s).Background = new SolidColorBrush(capturedNormal);

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

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            UpdateCalendar();
        }

        private void CalendarFilter_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCalendar();
        }

        private Border BuildCalendarCard(string title, SolidColorBrush accentBrush, object item)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var strip = new Border
            {
                Background   = accentBrush,
                CornerRadius = new CornerRadius(2, 0, 0, 2)
            };
            Grid.SetColumn(strip, 0);
            grid.Children.Add(strip);

            var label = new TextBlock
            {
                Text              = title ?? "",
                FontSize          = 11,
                Foreground        = (SolidColorBrush)Application.Current.Resources["ForegroundBrush"],
                TextTrimming      = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(4, 0, 2, 0)
            };
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            var card = new Border
            {
                Height       = 22,
                CornerRadius = new CornerRadius(4),
                Margin       = new Thickness(0, 1, 0, 1),
                Background   = new SolidColorBrush(Color.FromArgb(
                    30, accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B)),
                Cursor = Cursors.Hand,
                Child  = grid,
                Tag    = item
            };
            card.MouseLeftButtonUp += CalendarCard_Click;
            return card;
        }

        private void CalendarCard_Click(object sender, MouseButtonEventArgs e)
        {
            var card = sender as Border;
            if (card?.Tag == null) return;

            if (_dayDetailPopup != null)
                _dayDetailPopup.IsOpen = false;

            if (card.Tag is EventModel ev)
            {
                var win = new EditEventWindow(ev) { Owner = this };
                win.ShowDialog();
            }
            else if (card.Tag is TaskModel task)
            {
                var win = new EditTaskWindow(task, _currentUser) { Owner = this };
                win.ShowDialog();
            }
            else if (card.Tag is ReminderModel rem)
            {
                var win = new EditReminderWindow(rem) { Owner = this };
                win.ShowDialog();
            }

            UpdateCalendar();
            e.Handled = true;
        }

        private void EnsureDayDetailPopup()
        {
            if (_dayDetailPopup != null) return;

            _popupDateLabel = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                FontSize   = 14,
                Foreground = (SolidColorBrush)Application.Current.Resources["ForegroundBrush"],
                Margin     = new Thickness(0, 0, 0, 8)
            };

            _popupItemsControl = new ItemsControl();

            var scrollViewer = new ScrollViewer
            {
                MaxHeight                   = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content                     = _popupItemsControl
            };

            var innerPanel = new StackPanel();
            innerPanel.Children.Add(_popupDateLabel);
            innerPanel.Children.Add(scrollViewer);

            var popupBorder = new Border
            {
                Background      = (SolidColorBrush)Application.Current.Resources["BackgroundBrush"],
                BorderBrush     = (SolidColorBrush)Application.Current.Resources["BorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(12),
                MinWidth        = 240,
                MaxWidth        = 340,
                Child           = innerPanel
            };

            _dayDetailPopup = new Popup
            {
                Child              = popupBorder,
                StaysOpen          = false,
                AllowsTransparency = true,
                PopupAnimation     = PopupAnimation.Fade
            };
        }

        private void ShowDayDetailPopup(DateTime date, List<EventModel> events,
            List<TaskModel> tasks, List<ReminderModel> reminders, UIElement anchor)
        {
            EnsureDayDetailPopup();

            _popupDateLabel.Text = date.ToString("d MMMM yyyy",
                new System.Globalization.CultureInfo("ru-RU"));
            _popupItemsControl.Items.Clear();

            var eventBrush    = (SolidColorBrush)Application.Current.Resources["EventMeetingBrush"];
            var taskBrush     = (SolidColorBrush)Application.Current.Resources["EventDeadlineBrush"];
            var reminderBrush = (SolidColorBrush)Application.Current.Resources["EventOtherBrush"];

            foreach (var ev in events)
            {
                var card = BuildCalendarCard(ev.Название_события, eventBrush, ev);
                card.Height = double.NaN;
                card.Margin = new Thickness(0, 2, 0, 2);
                _popupItemsControl.Items.Add(card);
            }
            foreach (var task in tasks)
            {
                var card = BuildCalendarCard(task.Название_задачи, taskBrush, task);
                card.Height = double.NaN;
                card.Margin = new Thickness(0, 2, 0, 2);
                _popupItemsControl.Items.Add(card);
            }
            foreach (var rem in reminders)
            {
                var card = BuildCalendarCard(rem.Текст_напоминания, reminderBrush, rem);
                card.Height = double.NaN;
                card.Margin = new Thickness(0, 2, 0, 2);
                _popupItemsControl.Items.Add(card);
            }

            _dayDetailPopup.PlacementTarget = anchor;
            _dayDetailPopup.Placement       = PlacementMode.MousePoint;
            _dayDetailPopup.IsOpen          = true;
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

            LoadEventsData();
            SetActiveMenuButton(EventsButton);
        }

        private void LoadEventsData()
        {
            try
            {
                var allEvents = DbHelper.GetAllEvents();

                // Фильтрация по типу
                IEnumerable<EventModel> filtered = allEvents;
                if (_eventTypeFilter != "Все")
                {
                    filtered = filtered.Where(ev => ev.Тип_события == _eventTypeFilter);
                }

                // Фильтрация по строке поиска
                if (!string.IsNullOrWhiteSpace(_eventSearchText))
                {
                    string q = _eventSearchText.Trim().ToLower();
                    filtered = filtered.Where(ev =>
                        (ev.Название_события?.ToLower().Contains(q) ?? false) ||
                        (ev.Описание?.ToLower().Contains(q) ?? false) ||
                        (ev.Место_проведения?.ToLower().Contains(q) ?? false) ||
                        (ev.Название_проекта?.ToLower().Contains(q) ?? false) ||
                        (ev.Название_задачи?.ToLower().Contains(q) ?? false));
                }

                var result = filtered.ToList();

                // Счётчик с корректным склонением
                EventsCountLabel.Text = $"{result.Count} {GetEventWord(result.Count)}";

                // Привязка карточек к ItemsControl + empty state
                EventsItemsControl.ItemsSource = result;
                EventsEmptyState.Visibility = result.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки событий:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Склонение слова «событие» по числу: 1 событие / 2-4 события / 5+ событий
        private static string GetEventWord(int n)
        {
            int mod100 = n % 100;
            int mod10 = n % 10;
            if (mod100 >= 11 && mod100 <= 14) return "событий";
            if (mod10 == 1) return "событие";
            if (mod10 >= 2 && mod10 <= 4) return "события";
            return "событий";
        }

        // Обработчик переключения чипа-фильтра по типу
        private void EventTypeFilter_Changed(object sender, RoutedEventArgs e)
        {
            // Если ChipAll ещё не существует (XAML не успел проинициализироваться) — пропускаем
            if (!IsLoaded) return;

            if (ChipMeeting.IsChecked == true) _eventTypeFilter = "Встреча";
            else if (ChipDeadline.IsChecked == true) _eventTypeFilter = "Дедлайн";
            else if (ChipPresent.IsChecked == true) _eventTypeFilter = "Презентация";
            else if (ChipPersonal.IsChecked == true) _eventTypeFilter = "Личное";
            else _eventTypeFilter = "Все";

            LoadEventsData();
        }

        // Обработчик ввода в поле поиска
        private void EventSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            _eventSearchText = EventSearchBox.Text ?? "";
            LoadEventsData();
        }

        private void AddEventBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.AddEventWindow();
            window.Owner = this;
            window.ShowDialog();
            LoadEventsData();
        }


        // Редактирование события из карточки (по ID из Tag кнопки)
        private void EditEventCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int eventId)
            {
                try
                {
                    var ev = DbHelper.GetAllEvents().FirstOrDefault(x => x.ID_события == eventId);
                    if (ev == null) return;

                    var window = new Views.EditEventWindow(ev);
                    window.Owner = this;
                    window.ShowDialog();
                    LoadEventsData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия события:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Удаление события из карточки (по ID из Tag кнопки)
        private void DeleteEventCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int eventId)
            {
                try
                {
                    var ev = DbHelper.GetAllEvents().FirstOrDefault(x => x.ID_события == eventId);
                    if (ev == null) return;

                    var confirm = MessageBox.Show(
                        $"Удалить событие «{ev.Название_события}»?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        DbHelper.DeleteEvent(eventId);
                        LoadEventsData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления события:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            window.Owner = this;
            window.ShowDialog();
            LoadRemindersData();
        }

        private void EditReminderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RemindersGrid.SelectedItem is ReminderModel selectedReminder)
            {
                var window = new Views.EditReminderWindow(selectedReminder);
                window.Owner = this;
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

        private void SnoozeReminder15_Click(object sender, RoutedEventArgs e)
        {
            SnoozeReminder(sender, 15);
        }

        private void SnoozeReminder60_Click(object sender, RoutedEventArgs e)
        {
            SnoozeReminder(sender, 60);
        }

        private void SnoozeReminder(object sender, int minutes)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int reminderId)
            {
                try
                {
                    DbHelper.SnoozeReminder(reminderId, minutes);
                    LoadRemindersData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отложения напоминания:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MarkReminderDone_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int reminderId)
            {
                try
                {
                    DbHelper.SetReminderStatus(reminderId, "Выполнено");
                    LoadRemindersData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения статуса:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            window.Owner = this;
            window.ShowDialog();
            LoadNotesData();
        }

        private void EditNoteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NotesGrid.SelectedItem is NoteModel selectedNote)
            {
                var window = new Views.EditNoteWindow(selectedNote);
                window.Owner = this;
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

        private void ToggleNotePin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is int noteId)
            {
                try
                {
                    DbHelper.ToggleNotePin(noteId);
                    LoadNotesData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка закрепления заметки:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            win.Owner = this;
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
            ExcelPackage.License.SetNonCommercialPersonal("My Name");

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
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
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

        // Подключение к серверу чата через SignalR с улучшенным UI
        private async void ConnectToChat()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                return;

            try
            {
                // Обновляем статус
                ChatStatusText.Text = "Подключение...";
                ConnectionIndicator.Fill = new SolidColorBrush(Colors.Orange);

                // Создаем подключение
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5000/chatHub")
                    .WithAutomaticReconnect()
                    .Build();

                // Обработчик получения сообщений
                _hubConnection.On<int, string, string, DateTime>("ReceiveMessage",
                    (userId, userName, message, timestamp) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // Проверяем, не системное ли это сообщение об удалении
                            if (userName == "Система" && message.Contains("удалил"))
                            {
                                AddSystemMessage(message);
                            }
                            else
                            {
                                AddMessageBubble(userId, userName, message, timestamp);
                                ChatHelper.SaveMessage(null, userId, userName, message);
                            }

                            ChatScrollViewer.ScrollToBottom();
                        });
                    });

                // Обработчик переподключения
                _hubConnection.Reconnected += async (connectionId) =>
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        AddSystemMessage("Переподключено к серверу");
                        ChatStatusText.Text = "Подключено";
                        ConnectionIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
                        SendButton.IsEnabled = true;
                    });
                };

                // Обработчик разрыва соединения
                _hubConnection.Closed += async (error) =>
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        AddSystemMessage("Соединение разорвано. Попытка переподключения...");
                        ChatStatusText.Text = "Отключено";
                        ConnectionIndicator.Fill = new SolidColorBrush(Colors.Red);
                        SendButton.IsEnabled = false;
                    });
                };

                // Запускаем подключение
                await _hubConnection.StartAsync();

                // Успешное подключение
                ChatStatusText.Text = "Подключено";
                ConnectionIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
                SendButton.IsEnabled = true;

                AddSystemMessage("Подключено к серверу чата");
                LoadChatHistory();
            }
            catch (Exception ex)
            {
                ChatStatusText.Text = "Ошибка подключения";
                ConnectionIndicator.Fill = new SolidColorBrush(Colors.Red);

                MessageBox.Show(
                    $"Не удалось подключиться к серверу чата.\n\nОшибка: {ex.Message}",
                    "Ошибка подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                SendButton.IsEnabled = false;
            }
        }

        // Добавление сообщения в чат в виде пузырька
        private void AddMessageBubble(int userId, string userName, string message, DateTime timestamp)
        {
            // Определяем, это наше сообщение или чужое
            bool isMyMessage = userId == _currentUser.ID_сотрудника;

            // Создаём контейнер для сообщения
            var messageContainer = new StackPanel
            {
                Margin = new Thickness(0, 5, 0, 5),
                Tag = new { UserId = userId, MessageId = timestamp.Ticks } // Сохраняем ID для удаления
            };

            // Создаём пузырёк сообщения
            var messageBubble = new Border
            {
                Style = isMyMessage
                    ? (Style)FindResource("MyMessageBubbleStyle")
                    : (Style)FindResource("OtherMessageBubbleStyle"),
                HorizontalAlignment = isMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            // Содержимое пузырька
            var bubbleContent = new StackPanel();

            // Имя отправителя (только для чужих сообщений)
            if (!isMyMessage)
            {
                var senderName = new TextBlock
                {
                    Text = userName,
                    Style = (Style)FindResource("MessageSenderNameStyle"),
                    Foreground = (SolidColorBrush)FindResource("OtherMessageForegroundBrush")
                };
                bubbleContent.Children.Add(senderName);
            }

            // Текст сообщения
            var messageText = new TextBlock
            {
                Text = message,
                Style = (Style)FindResource("MessageTextStyle"),
                Foreground = isMyMessage
                    ? (SolidColorBrush)FindResource("MyMessageForegroundBrush")
                    : (SolidColorBrush)FindResource("OtherMessageForegroundBrush")
            };
            bubbleContent.Children.Add(messageText);

            // Время отправки
            var timeText = new TextBlock
            {
                Text = timestamp.ToString("HH:mm"),
                Style = (Style)FindResource("MessageTimeStyle"),
                Foreground = isMyMessage
                    ? (SolidColorBrush)FindResource("MyMessageForegroundBrush")
                    : (SolidColorBrush)FindResource("OtherMessageForegroundBrush")
            };
            bubbleContent.Children.Add(timeText);

            messageBubble.Child = bubbleContent;

            // ========== ДОБАВЛЯЕМ КОНТЕКСТНОЕ МЕНЮ ДЛЯ СВОИХ СООБЩЕНИЙ ==========
            if (isMyMessage)
            {
                var contextMenu = new ContextMenu();

                // Пункт "Удалить"
                var deleteMenuItem = new MenuItem
                {
                    Header = "🗑️ Удалить сообщение",
                    Tag = timestamp.Ticks // Сохраняем ID сообщения
                };

                deleteMenuItem.Click += async (s, e) =>
                {
                    var result = MessageBox.Show(
                        "Вы уверены, что хотите удалить это сообщение?",
                        "Удаление сообщения",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // Удаляем из UI
                            ChatMessagesControl.Items.Remove(messageContainer);

                            // Удаляем из БД
                            long messageId = (long)deleteMenuItem.Tag;
                            ChatHelper.DeleteMessage(messageId);

                            // Отправляем уведомление другим пользователям через SignalR
                            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                            {
                                await _hubConnection.InvokeAsync("SendMessage",
                                    _currentUser.ID_сотрудника,
                                    "Система",
                                    $"[{_currentUser.Имя} удалил(а) сообщение]");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления сообщения: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                };

                // Пункт "Скопировать текст"
                var copyMenuItem = new MenuItem
                {
                    Header = "📋 Скопировать текст"
                };
                copyMenuItem.Click += (s, e) =>
                {
                    try
                    {
                        Clipboard.SetText(message);
                        // Можно добавить toast уведомление
                    }
                    catch { }
                };

                contextMenu.Items.Add(deleteMenuItem);
                contextMenu.Items.Add(new System.Windows.Controls.Separator());
                contextMenu.Items.Add(copyMenuItem);

                messageBubble.ContextMenu = contextMenu;
                messageBubble.Cursor = Cursors.Hand;
            }

            messageContainer.Children.Add(messageBubble);

            // Добавляем в чат
            ChatMessagesControl.Items.Add(messageContainer);
        }

        // Добавление системного сообщения
        private void AddSystemMessage(string message)
        {
            var systemBubble = new Border
            {
                Style = (Style)FindResource("SystemMessageStyle")
            };

            var systemText = new TextBlock
            {
                Text = message,
                Style = (Style)FindResource("SystemMessageTextStyle")
            };

            systemBubble.Child = systemText;
            ChatMessagesControl.Items.Add(systemBubble);
        }

        // Загрузка истории сообщений из БД

        private void LoadChatHistory()
        {
            try
            {
                var messages = ChatHelper.GetChatHistory(null, 50);

                if (messages.Count > 0)
                {
                    ChatMessagesControl.Items.Clear();
                    AddSystemMessage($"Загружена история: {messages.Count} сообщений");

                    foreach (var msg in messages)
                    {
                        AddMessageBubble(
                            msg.ID_отправителя,
                            msg.Имя_отправителя,
                            msg.Текст_сообщения,
                            msg.Дата_отправки);
                    }

                    ChatScrollViewer.ScrollToBottom();
                }
                else
                {
                    AddSystemMessage("История сообщений пуста. Начните общение!");
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"Ошибка загрузки истории: {ex.Message}");
            }
        }

        // Отправка сообщения

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text.Trim();

            if (string.IsNullOrEmpty(message))
                return;

            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                MessageBox.Show("Нет подключения к серверу чата.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Отправляем через SignalR
                await _hubConnection.InvokeAsync("SendMessage",
                    _currentUser.ID_сотрудника,
                    _currentUser.Имя,
                    message);

                // Очищаем поле
                MessageTextBox.Clear();
                MessageTextBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Отправка по нажатию Enter
        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                // Если нажат Shift+Enter - перенос строки
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    return; // Разрешаем перенос
                }

                // Иначе отправляем
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }

        // Отслеживание набора текста для индикатора

        private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Можно добавить логику отправки уведомления "печатает..."
            // Но для простоты пока пропустим
        }

        // Добавление эмодзи в сообщение
        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            // Простой набор эмодзи
            string[] emojis = { "😊", "😂", "❤️", "👍", "🎉", "🔥", "✅", "❌", "💯", "👀" };

            // Создаём контекстное меню с эмодзи
            var contextMenu = new ContextMenu();

            foreach (var emoji in emojis)
            {
                var menuItem = new MenuItem
                {
                    Header = emoji,
                    FontSize = 20
                };
                menuItem.Click += (s, args) =>
                {
                    MessageTextBox.Text += emoji;
                    MessageTextBox.Focus();
                    MessageTextBox.CaretIndex = MessageTextBox.Text.Length;
                };
                contextMenu.Items.Add(menuItem);
            }

            contextMenu.PlacementTarget = EmojiButton;
            contextMenu.IsOpen = true;
        }
        // Обработчик получения фокуса полем ввода
        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Находим Border, который оборачивает TextBox
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var border = FindParent<Border>(textBox);
                if (border != null && border.Name == "MessageInputBorder")
                {
                    // Яркая синяя рамка при фокусе
                    border.BorderBrush = (SolidColorBrush)FindResource("AccentBrush");
                    border.BorderThickness = new Thickness(2);

                    // Опционально: лёгкое изменение фона
                    var inputBrush = (SolidColorBrush)FindResource("InputBackgroundBrush");
                    var lighterBrush = new SolidColorBrush(
                        Color.FromArgb(
                            inputBrush.Color.A,
                            (byte)Math.Min(255, inputBrush.Color.R + 10),
                            (byte)Math.Min(255, inputBrush.Color.G + 10),
                            (byte)Math.Min(255, inputBrush.Color.B + 10)
                        )
                    );
                    border.Background = lighterBrush;
                }
            }
        }
        // Обработчик потери фокуса полем ввода
        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Находим Border
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var border = FindParent<Border>(textBox);
                if (border != null && border.Name == "MessageInputBorder")
                {
                    // Возвращаем обычную рамку
                    border.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");
                    border.BorderThickness = new Thickness(2);

                    // Возвращаем обычный фон
                    border.Background = (SolidColorBrush)FindResource("InputBackgroundBrush");
                }
            }
        }
        // Вспомогательный метод для поиска родительского элемента
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }

        // Отключение от чата
        private async void DisconnectChat()
        {
            try
            {
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отключения: {ex.Message}");
            }
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
                editWin.Owner = this;
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
                var ganttWindow = new GanttChartWindow(selectedTask);
                ganttWindow.Owner = this;
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
            profileWindow.Owner = this;
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
        // Отображение панели "История изменений"
        private void ShowHistory(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            HistoryPanel.Visibility = Visibility.Visible;
            LoadHistoryData();
            SetActiveMenuButton(HistoryButton);
        }
        // Загрузка данных истории изменений
        private void LoadHistoryData()
        {
            try
            {
                _allHistory = DbHelper.GetAllHistory();

                // Проверяем, что элементы инициализированы
                if (HistoryGrid != null && TotalHistoryCountText != null)
                {
                    ApplyHistoryFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке истории:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Применение фильтров к истории изменений
        private void ApplyHistoryFilters()
        {
            // Проверка на инициализацию элементов
            if (HistoryGrid == null || HistoryTypeFilter == null ||
                HistoryEmployeeSearchBox == null || TotalHistoryCountText == null)
            {
                return;
            }

            var filtered = _allHistory.AsEnumerable();

            // Фильтр по типу объекта
            string selectedType = (HistoryTypeFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedType != "Все" && !string.IsNullOrEmpty(selectedType))
            {
                filtered = filtered.Where(h => h.Сущность == selectedType);
            }

            // Фильтр по сотруднику
            string employeeSearch = HistoryEmployeeSearchBox.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(employeeSearch))
            {
                filtered = filtered.Where(h => !string.IsNullOrEmpty(h.ФИО_сотрудника) &&
                                               h.ФИО_сотрудника.ToLower().Contains(employeeSearch));
            }

            // Фильтр по дате начала
            if (HistoryStartDatePicker != null && HistoryStartDatePicker.SelectedDate.HasValue)
            {
                DateTime startDate = HistoryStartDatePicker.SelectedDate.Value.Date;
                filtered = filtered.Where(h => h.Дата_изменения.Date >= startDate);
            }

            // Фильтр по дате окончания
            if (HistoryEndDatePicker != null && HistoryEndDatePicker.SelectedDate.HasValue)
            {
                DateTime endDate = HistoryEndDatePicker.SelectedDate.Value.Date;
                filtered = filtered.Where(h => h.Дата_изменения.Date <= endDate);
            }

            var result = filtered.ToList();
            HistoryGrid.ItemsSource = result;
            TotalHistoryCountText.Text = result.Count.ToString();
        }

        // Обработчик изменения фильтров истории
        private void HistoryFilters_Changed(object sender, EventArgs e)
        {
            // Проверяем, что все элементы загружены
            if (HistoryGrid != null && _allHistory != null && _allHistory.Count > 0)
            {
                ApplyHistoryFilters();
            }
        }

        // Сброс фильтров истории
        private void ResetHistoryFiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            HistoryTypeFilter.SelectedIndex = 0;
            HistoryEmployeeSearchBox.Clear();
            HistoryStartDatePicker.SelectedDate = null;
            HistoryEndDatePicker.SelectedDate = null;
            ApplyHistoryFilters();
        }


    }
}
