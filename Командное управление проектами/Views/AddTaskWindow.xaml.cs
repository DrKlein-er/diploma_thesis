using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class AddTaskWindow : Window
    {
        private readonly string connectionString = "Data Source=DESKTOP-JRVC3AP;Initial Catalog=Coursework;Integrated Security=True";

        public AddTaskWindow()
        {
            InitializeComponent();
            // Применяем текущую тему
            ApplyTheme();
            // Загружаем данные
            LoadProjects();
            LoadEmployees();
            // Устанавливаем фокус на первое поле
            TitleBox.Focus();
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

        // Загрузка списка проектов в ComboBox
        private void LoadProjects()
        {
            try
            {
                List<ProjectModel> projects = DbHelper.GetAllProjects();
                ProjectComboBox.ItemsSource = projects;
                ProjectComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Загрузка списка сотрудников в ComboBox
        private void LoadEmployees()
        {
            try
            {
                List<EmployeeModel> employees = DbHelper.GetAllEmployees();
                EmployeeComboBox.ItemsSource = employees;
                EmployeeComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка сотрудников:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработчик нажатия кнопки "Добавить"
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

            try
            {
                // Добавление задачи в базу данных
                string query = @"
                    INSERT INTO Задачи 
                    (Название_задачи, Описание, Приоритет, Статус, Дата_начала, Дата_завершения, ID_проекта, ID_ответственного)
                    VALUES (@title, @desc, @priority, @status, @startDate, @endDate, @projectId, @empId)";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@desc",
                            string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description);
                        cmd.Parameters.AddWithValue("@priority",
                            string.IsNullOrWhiteSpace(priority) ? (object)DBNull.Value : priority);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@startDate",
                            startDate.HasValue ? (object)startDate.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@endDate",
                            endDate.HasValue ? (object)endDate.Value : DBNull.Value); cmd.Parameters.AddWithValue("@projectId", projectId.Value);
                        cmd.Parameters.AddWithValue("@empId",
                            employeeId.HasValue ? (object)employeeId.Value : DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Уведомление об успехе
                MessageBox.Show("Задача успешно добавлена!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрываем окно
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

        // Обработка горячих клавиш
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