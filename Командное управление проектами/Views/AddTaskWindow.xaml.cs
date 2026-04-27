using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    /// <summary>
    /// Окно добавления новой задачи
    /// </summary>
    public partial class AddTaskWindow : Window
    {
        private UserModel _currentUser; // Поле для текущего пользователя

        /// <summary>
        /// Конструктор окна добавления задачи
        /// </summary>
        /// <param name="currentUser">Текущий авторизованный пользователь (опционально)</param>
        public AddTaskWindow(UserModel currentUser = null)
        {
            InitializeComponent();
            _currentUser = currentUser; // Сохраняем пользователя

            // Применяем текущую тему
            ApplyTheme();
            // Загружаем данные
            LoadProjects();
            LoadEmployees();
            // Устанавливаем фокус на первое поле
            TitleBox.Focus();
        }

        /// <summary>
        /// Применение текущей темы приложения к окну
        /// </summary>
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

        /// <summary>
        /// Загрузка списка проектов в ComboBox
        /// </summary>
        private void LoadProjects()
        {
            try
            {
                List<ProjectModel> projects = DbHelper.GetAllProjects();
                ProjectComboBox.ItemsSource = projects;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загрузка списка сотрудников в ComboBox
        /// </summary>
        private void LoadEmployees()
        {
            try
            {
                List<EmployeeModel> employees = DbHelper.GetAllEmployees();
                EmployeeComboBox.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка сотрудников:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Добавить задачу"
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string title = TitleBox.Text.Trim();
            string description = DescBox.Text.Trim();
            string priority = (PriorityBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;
            int? projectId = ProjectComboBox.SelectedValue as int?;
            int? employeeId = EmployeeComboBox.SelectedValue as int?;

            // Валидация: название обязательно
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Пожалуйста, введите название задачи.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            // Валидация: минимальная длина названия
            if (title.Length < 3)
            {
                MessageBox.Show("Название задачи должно содержать минимум 3 символа.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            // Валидация: проект обязателен
            if (!projectId.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите проект.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ProjectComboBox.Focus();
                return;
            }

            // Валидация: если указана дата завершения, должна быть указана и дата начала
            if (endDate.HasValue && !startDate.HasValue)
            {
                MessageBox.Show("Если указана дата завершения, необходимо указать и дату начала.",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            // Валидация: дата начала должна быть раньше даты завершения
            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                MessageBox.Show("Дата начала не может быть позже даты завершения.",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            // Создание новой задачи
            TaskModel task = new TaskModel
            {
                Название_задачи = title,
                Описание = description,
                Приоритет = priority,
                Статус = status,
                Дата_начала = startDate,
                Дата_завершения = endDate,
                ID_ответственного = employeeId
            };

            try
            {
                // Добавление задачи через DbHelper (возвращает ID)
                int newTaskId = DbHelper.AddTask(task, projectId.Value);

                // ✅ ДЕТАЛЬНОЕ ЛОГИРОВАНИЕ
                if (_currentUser != null)
                {
                    DbHelper.LogChange("Задача", newTaskId,
                        $"Создана задача: \"{title}\"", _currentUser.ID_сотрудника);
                }

                // Формируем сообщение об успехе с подробной информацией
                string projectName = (ProjectComboBox.SelectedItem as ProjectModel)?.Название_проекта;
                string employeeName = employeeId.HasValue
                    ? (EmployeeComboBox.SelectedItem as EmployeeModel)?.Имя_сотрудника
                    : "не назначен";
                string dueInfo = "";
                if (startDate.HasValue && endDate.HasValue)
                {
                    dueInfo = $"\nПериод: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                }
                else if (endDate.HasValue)
                {
                    dueInfo = $"\nДата завершения: {endDate:dd.MM.yyyy}";
                }

                MessageBox.Show($"Задача успешно добавлена!\n\n" +
                              $"Название: {title}\n" +
                              $"Статус: {status}\n" +
                              $"Проект: {projectName}\n" +
                              $"Ответственный: {employeeName}{dueInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении задачи:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработка горячих клавиш
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+Enter - добавить задачу
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Save_Click(this, new RoutedEventArgs());
            }
            // Escape - закрыть окно
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}