using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditReminderWindow : Window
    {
        private readonly ReminderModel _reminder;

        public EditReminderWindow(ReminderModel reminder)
        {
            InitializeComponent();
            ApplyTheme();

            _reminder = reminder ?? throw new ArgumentNullException(nameof(reminder));

            LoadTasks();
            LoadReminderData();

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка задач:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Заполнение полей текущими значениями напоминания
        private void LoadReminderData()
        {
            TextBoxReminder.Text = _reminder.Текст_напоминания ?? string.Empty;

            // Дата и время напоминания
            if (_reminder.Дата_напоминания.HasValue)
            {
                ReminderDatePicker.SelectedDate = _reminder.Дата_напоминания.Value.Date;
                ReminderTimeBox.Text = _reminder.Дата_напоминания.Value.ToString("HH:mm");
            }
            else
            {
                ReminderDatePicker.SelectedDate = DateTime.Today;
                ReminderTimeBox.Text = "09:00";
            }

            // Статус
            string status = string.IsNullOrWhiteSpace(_reminder.Статус) ? "Активно" : _reminder.Статус;
            SelectByContent(StatusComboBox, status, fallbackIndex: 0);

            // Приоритет
            string priority = string.IsNullOrWhiteSpace(_reminder.Приоритет) ? "Средний" : _reminder.Приоритет;
            SelectByContent(PriorityComboBox, priority, fallbackIndex: 1);

            // Задача
            if (_reminder.ID_задачи.HasValue)
                TaskComboBox.SelectedValue = _reminder.ID_задачи.Value;
            else
                TaskComboBox.SelectedIndex = -1;
        }

        private static void SelectByContent(ComboBox combo, string content, int fallbackIndex)
        {
            foreach (var item in combo.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Content?.ToString() == content)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            combo.SelectedIndex = fallbackIndex;
        }

        private static bool TryParseTime(string text, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(text?.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out time)
                || TimeSpan.TryParseExact(text?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string text = TextBoxReminder.Text.Trim();
            DateTime? date = ReminderDatePicker.SelectedDate;
            string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Активно";
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
                MessageBox.Show("Время напоминания должно быть в формате ЧЧ:ММ.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ReminderTimeBox.Focus();
                return;
            }

            DateTime reminderDateTime = date.Value.Date.Add(time);

            // Валидация: задача обязательна
            if (!taskId.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите задачу, к которой относится напоминание.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TaskComboBox.Focus();
                return;
            }

            // Применяем изменения
            _reminder.Текст_напоминания = text;
            _reminder.Дата_напоминания = reminderDateTime;
            _reminder.Статус = status;
            _reminder.Приоритет = priority;
            _reminder.ID_задачи = taskId;

            try
            {
                DbHelper.UpdateReminder(_reminder);

                MessageBox.Show("Напоминание успешно обновлено!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении напоминания:\n{ex.Message}",
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
                Save_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}