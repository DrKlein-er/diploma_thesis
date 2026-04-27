using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    // Окно профиля пользователя
    public partial class UserProfileWindow : Window
    {
        private UserModel _currentUser;
        private UserSettingsModel _currentSettings;

        // Конструктор окна профиля
        public UserProfileWindow(UserModel user)
        {
            InitializeComponent();
            _currentUser = user;

            // Применяем текущую тему
            ApplyTheme();

            // Загружаем данные пользователя
            LoadUserData();

            // Загружаем настройки приложения
            LoadUserSettings();

            // Загружаем статистику
            LoadUserStatistics();
        }

        // Применение текущей темы приложения к окну
        private void ApplyTheme()
        {
            var theme = ThemeManager.GetCurrentTheme();
            var themeUri = theme == "Тёмная"
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            var themeDict = new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Relative)
            };

            this.Resources.MergedDictionaries.Add(themeDict);
        }

        // Загрузка данных пользователя в форму
        private void LoadUserData()
        {
            // Формируем инициалы для аватара
            string initials = "";
            if (!string.IsNullOrEmpty(_currentUser.Фамилия))
                initials += _currentUser.Фамилия[0];
            if (!string.IsNullOrEmpty(_currentUser.Имя))
                initials += _currentUser.Имя[0];

            // Устанавливаем данные в шапке профиля
            UserInitials.Text = initials;
            UserFullName.Text = $"{_currentUser.Фамилия} {_currentUser.Имя} {_currentUser.Отчество}".Trim();
            UserRole.Text = _currentUser.Роль;

            // Заполняем поля формы
            LastNameBox.Text = _currentUser.Фамилия;
            FirstNameBox.Text = _currentUser.Имя;
            MiddleNameBox.Text = _currentUser.Отчество;
            EmailBox.Text = _currentUser.Email;
            RoleBox.Text = _currentUser.Роль;

            // Загружаем отдел из БД
            LoadDepartment();
        }

        // Загрузка отдела сотрудника из базы данных
        private void LoadDepartment()
        {
            string department = DbHelper.GetEmployeeDepartment(_currentUser.ID_сотрудника);

            // Устанавливаем выбранный отдел в ComboBox
            foreach (ComboBoxItem item in DepartmentComboBox.Items)
            {
                if (item.Content.ToString() == department)
                {
                    DepartmentComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        // Загрузка настроек пользователя из файла
        private void LoadUserSettings()
        {
            // Получаем настройки из SettingsManager
            _currentSettings = SettingsManager.GetSettings(_currentUser.ID);

            // Используем Dispatcher для обновления UI после полной загрузки окна
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Применяем настройки к UI элементам
                NotificationsCheckBox.IsChecked = _currentSettings.Уведомления_включены;
                SoundCheckBox.IsChecked = _currentSettings.Звук_уведомлений;
                ShowCompletedCheckBox.IsChecked = _currentSettings.Показывать_завершённые_задачи;

                // Устанавливаем частоту проверки дедлайнов
                foreach (ComboBoxItem item in CheckFrequencyComboBox.Items)
                {
                    if (item.Content.ToString() == _currentSettings.Частота_проверки_дедлайнов.ToString())
                    {
                        CheckFrequencyComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Устанавливаем тему
                foreach (ComboBoxItem item in ThemeComboBox.Items)
                {
                    if (item.Content.ToString() == _currentSettings.Тема)
                    {
                        ThemeComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Обновляем доступность связанных настроек
                NotificationsCheckBox_Changed(null, null);

            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // Загрузка статистики пользователя
        private void LoadUserStatistics()
        {
            var projects = DbHelper.GetAllProjects();
            var tasks = DbHelper.GetAllTasks();

            // Подсчёт проектов, где пользователь ответственный
            int myProjects = projects.Count(p => p.ID_ответственного == _currentUser.ID_сотрудника);
            MyProjectsCount.Text = myProjects.ToString();

            // Подсчёт задач, назначенных на пользователя
            var myTasks = tasks.Where(t => t.ID_ответственного == _currentUser.ID_сотрудника).ToList();
            MyTasksCount.Text = myTasks.Count.ToString();

            // Подсчёт завершённых задач
            int completedTasks = myTasks.Count(t => t.Статус == "Завершена");
            MyCompletedTasksCount.Text = completedTasks.ToString();

            // Подсчёт просроченных задач (дата окончания прошла, но задача не завершена)
            int overdueTasks = myTasks.Count(t =>
                t.Дата_завершения.HasValue &&
                t.Дата_завершения.Value < DateTime.Now &&
                t.Статус != "Завершена");
            MyOverdueTasksCount.Text = overdueTasks.ToString();

            // Загружаем последнюю активность из истории изменений
            LoadRecentActivity();
        }

        // Загрузка последней активности пользователя из истории изменений
        private void LoadRecentActivity()
        {
            var activities = DbHelper.GetRecentUserActivity(_currentUser.ID_сотрудника, 10);
            RecentActivityGrid.ItemsSource = activities;
        }

        // ========== ПЕРЕКЛЮЧЕНИЕ ВКЛАДОК ==========

        // Показать вкладку "Основная информация"
        private void ShowProfileTab(object sender, RoutedEventArgs e)
        {
            HideAllTabs();
            ProfileTabContent.Visibility = Visibility.Visible;
            ResetMenuButtons();
            ProfileTab.Style = (Style)FindResource("ActiveProfileMenuButton");
        }

        // Показать вкладку "Настройки"
        private void ShowSettingsTab(object sender, RoutedEventArgs e)
        {
            HideAllTabs();
            SettingsTabContent.Visibility = Visibility.Visible;
            ResetMenuButtons();
            SettingsTab.Style = (Style)FindResource("ActiveProfileMenuButton");
        }

        // Показать вкладку "Статистика"
        private void ShowStatsTab(object sender, RoutedEventArgs e)
        {
            HideAllTabs();
            StatsTabContent.Visibility = Visibility.Visible;
            LoadUserStatistics(); // Обновляем статистику при открытии вкладки
            ResetMenuButtons();
            StatsTab.Style = (Style)FindResource("ActiveProfileMenuButton");
        }

        // Скрыть все вкладки контента
        private void HideAllTabs()
        {
            ProfileTabContent.Visibility = Visibility.Collapsed;
            SettingsTabContent.Visibility = Visibility.Collapsed;
            StatsTabContent.Visibility = Visibility.Collapsed;
        }

        // Сбросить стили всех кнопок меню к обычному состоянию
        private void ResetMenuButtons()
        {
            ProfileTab.Style = (Style)FindResource("ProfileMenuButton");
            SettingsTab.Style = (Style)FindResource("ProfileMenuButton");
            StatsTab.Style = (Style)FindResource("ProfileMenuButton");
        }

        // ========== СОХРАНЕНИЕ ДАННЫХ ==========

        // Обработчик сохранения основной информации профиля
        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            string lastName = LastNameBox.Text.Trim();
            string firstName = FirstNameBox.Text.Trim();
            string middleName = MiddleNameBox.Text.Trim();
            string department = (DepartmentComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Валидация: фамилия и имя обязательны
            if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
            {
                MessageBox.Show("Заполните обязательные поля: Фамилия и Имя.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Обновляем данные сотрудника в БД через DbHelper
                DbHelper.UpdateEmployeeProfile(
                    _currentUser.ID_сотрудника,
                    lastName,
                    firstName,
                    middleName,
                    department ?? "Не указан"
                );

                // Обновляем данные в текущем объекте пользователя
                _currentUser.Фамилия = lastName;
                _currentUser.Имя = firstName;
                _currentUser.Отчество = middleName;

                // Обновляем отображение в шапке профиля
                LoadUserData();

                MessageBox.Show("Данные профиля успешно сохранены!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработчик сохранения настроек приложения
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Собираем настройки из UI элементов
                _currentSettings.ID_пользователя = _currentUser.ID;
                _currentSettings.Уведомления_включены = NotificationsCheckBox.IsChecked == true;
                _currentSettings.Звук_уведомлений = SoundCheckBox.IsChecked == true;
                _currentSettings.Показывать_завершённые_задачи = ShowCompletedCheckBox.IsChecked == true;
                _currentSettings.Тема = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Светлая";

                // Получаем частоту проверки дедлайнов
                string frequencyStr = (CheckFrequencyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (int.TryParse(frequencyStr, out int frequency))
                {
                    _currentSettings.Частота_проверки_дедлайнов = frequency;
                }

                // Сохраняем настройки в файл
                SettingsManager.SaveSettings(_currentSettings);

                // Применяем настройки к приложению (тема, уведомления и т.д.)
                SettingsManager.ApplySettings(_currentSettings, _currentUser);

                MessageBox.Show("Настройки успешно сохранены и применены! Для применения настроек требуется перезагрузить приложение",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработчик изменения состояния чекбокса уведомлений
        // Активирует/деактивирует связанные настройки
        private void NotificationsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (SoundCheckBox != null && CheckFrequencyComboBox != null)
            {
                bool isEnabled = NotificationsCheckBox.IsChecked == true;
                SoundCheckBox.IsEnabled = isEnabled;
                CheckFrequencyComboBox.IsEnabled = isEnabled;
            }
        }
    }
}