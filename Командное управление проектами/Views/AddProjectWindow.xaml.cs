using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class AddProjectWindow : Window
    {
        public AddProjectWindow()
        {
            InitializeComponent();
            // Применяем текущую тему
            ApplyTheme();
            // Устанавливаем дату начала по умолчанию на сегодня
            StartDatePicker.SelectedDate = DateTime.Today;
            // Загружаем список сотрудников
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

        // Загрузка списка сотрудников в ComboBox
        private void LoadEmployees()
        {
            try
            {
                var employees = DbHelper.GetAllEmployees();
                EmployeeComboBox.ItemsSource = employees;

                // Не выбираем ничего по умолчанию - ответственный необязателен
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

        // Обработчик нажатия кнопки "Добавить проект"
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string name = TitleBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();
            string status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;
            int? employeeId = EmployeeComboBox.SelectedValue as int?;

            // Валидация: название обязательно
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Пожалуйста, введите название проекта.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            // Валидация: минимальная длина названия
            if (name.Length < 3)
            {
                MessageBox.Show("Название проекта должно содержать минимум 3 символа.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            // Валидация: статус обязателен
            if (string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Пожалуйста, выберите статус проекта.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StatusBox.Focus();
                return;
            }

            // Валидация: дата начала обязательна
            if (!startDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату начала проекта.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            // Валидация: дата завершения обязательна
            if (!endDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату завершения проекта.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EndDatePicker.Focus();
                return;
            }

            // Валидация: дата начала должна быть раньше даты завершения
            if (startDate.Value > endDate.Value)
            {
                MessageBox.Show("Дата начала проекта не может быть позже даты завершения.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            // Предупреждение о слишком далекой дате начала
            if (startDate.Value > DateTime.Today.AddYears(2))
            {
                var result = MessageBox.Show(
                    "Дата начала проекта находится более чем через 2 года.\nПродолжить?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    StartDatePicker.Focus();
                    return;
                }
            }

            // Создание нового проекта
            ProjectModel project = new ProjectModel
            {
                Название_проекта = name,
                Описание = string.IsNullOrWhiteSpace(description) ? null : description,
                Дата_начала = startDate,
                Дата_завершения = endDate,
                Статус = status,
                ID_ответственного = employeeId
            };

            try
            {
                // Добавление проекта в базу данных через DbHelper
                DbHelper.AddProject(project);

                // Формируем сообщение об успехе с подробной информацией
                string dateInfo = $"\nПериод: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                string employeeInfo = employeeId.HasValue
                    ? $"\nОтветственный: {(EmployeeComboBox.SelectedItem as EmployeeModel)?.Имя_сотрудника}"
                    : "";

                MessageBox.Show($"Проект успешно добавлен!\n\nНазвание: {name}\nСтатус: {status}{dateInfo}{employeeInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении проекта:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+Enter - добавить проект (даже из многострочного поля)
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