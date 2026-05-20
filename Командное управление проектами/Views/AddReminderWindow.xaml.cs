using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class AddReminderWindow : Window
    {
        public AddReminderWindow()
        {
            InitializeComponent();
            ApplyTheme();

            // Значения по умолчанию
            ReminderDatePicker.SelectedDate = DateTime.Today;
            PriorityComboBox.SelectedIndex = 1; // Средний

            LoadTasks();

            TextBoxReminder.Focus();
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

        // Загрузка списка задач в ComboBox
        private void LoadTasks()
        {
            try
            {
                var tasks = DbHelper.GetAllTasks();
                TaskComboBox.ItemsSource = tasks;
                TaskComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка задач:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static bool TryParseTime(string text, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(text?.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out time)
                || TimeSpan.TryParseExact(text?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time);
        }

        // Обработчик нажатия кнопки "Добавить"
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string text = TextBoxReminder.Text.Trim();
            DateTime? date = ReminderDatePicker.SelectedDate;
            string priority = (PriorityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Средний";
            int? taskId = TaskComboBox.SelectedValue as int?;

            // Валидация: текст
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Пожалуйста, введите текст напоминания.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TextBoxReminder.Focus();
                return;
            }

            if (text.Length < 3)
            {
                MessageBox.Show("Текст напоминания должен содержать минимум 3 символа.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TextBoxReminder.Focus();
                return;
            }

            // Валидация: дата
            if (!date.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату напоминания.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ReminderDatePicker.Focus();
                return;
            }

            // Валидация: время
            if (!TryParseTime(ReminderTimeBox.Text, out TimeSpan time))
            {
                MessageBox.Show("Время напоминания должно быть в формате ЧЧ:ММ (например, 14:30).",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ReminderTimeBox.Focus();
                return;
            }

            DateTime reminderDateTime = date.Value.Date.Add(time);

            // Валидация: момент напоминания не должен быть в прошлом
            if (reminderDateTime < DateTime.Now)
            {
                MessageBox.Show("Время напоминания не может быть в прошлом.\nВыберите момент в будущем.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ReminderTimeBox.Focus();
                return;
            }

            // Валидация: задача обязательна (в БД поле ID_задачи NOT NULL)
            if (!taskId.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите задачу, к которой относится напоминание.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TaskComboBox.Focus();
                return;
            }

            // Создание модели
            ReminderModel reminder = new ReminderModel
            {
                Текст_напоминания = text,
                Дата_напоминания = reminderDateTime,
                Статус = "Активно",
                Приоритет = priority,
                ID_задачи = taskId
            };

            try
            {
                DbHelper.AddReminder(reminder);

                string taskName = (TaskComboBox.SelectedItem as TaskModel)?.Название_задачи ?? "";
                MessageBox.Show($"Напоминание успешно добавлено!\n\nКогда: {reminderDateTime:dd.MM.yyyy HH:mm}\nПриоритет: {priority}\nЗадача: {taskName}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении напоминания:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Add_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}