using System;
using System.Windows;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditReminderWindow : Window
    {
        private ReminderModel _reminder;

        public EditReminderWindow(ReminderModel reminder)
        {
            InitializeComponent();
            _reminder = reminder;

            // Применяем текущую тему
            ApplyTheme();
            // Загружаем список задач
            LoadTasks();
            // Заполняем поля данными напоминания
            LoadReminderData();
            // Устанавливаем фокус на первое поле
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

        // Загрузка данных напоминания в поля формы
        private void LoadReminderData()
        {
            TextBoxReminder.Text = _reminder.Текст_напоминания;
            ReminderDatePicker.SelectedDate = _reminder.Дата_напоминания;
            TaskComboBox.SelectedValue = _reminder.ID_задачи;
        }

        // Обработчик нажатия кнопки "Сохранить"
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string text = TextBoxReminder.Text.Trim();
            DateTime? date = ReminderDatePicker.SelectedDate;
            int? taskId = TaskComboBox.SelectedValue as int?;

            // Валидация: текст напоминания обязателен
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Пожалуйста, введите текст напоминания.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TextBoxReminder.Focus();
                return;
            }

            // Валидация: минимальная длина текста
            if (text.Length < 3)
            {
                MessageBox.Show("Текст напоминания должен содержать минимум 3 символа.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TextBoxReminder.Focus();
                return;
            }

            // Обновление данных напоминания
            _reminder.Текст_напоминания = text;
            _reminder.Дата_напоминания = date;
            _reminder.ID_задачи = taskId;

            try
            {
                // Обновление напоминания в базе данных
                DbHelper.UpdateReminder(_reminder);

                // Формируем сообщение об успехе
                string dateInfo = date.HasValue ? $"\nДата: {date:dd.MM.yyyy}" : "\nДата: не указана";
                string taskInfo = taskId.HasValue
                    ? $"\nЗадача: {(TaskComboBox.SelectedItem as TaskModel)?.Название_задачи}"
                    : "";

                MessageBox.Show($"Напоминание успешно обновлено!\n\nТекст: {text}{dateInfo}{taskInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
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

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Enter - сохранить изменения (если фокус не на многострочном текстовом поле)
            if (e.Key == Key.Enter && !TextBoxReminder.IsFocused)
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
