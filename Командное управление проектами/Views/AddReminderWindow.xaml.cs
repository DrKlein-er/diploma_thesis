using System;
using System.Windows;
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
            // Применяем текущую тему
            ApplyTheme();
            // Устанавливаем дату по умолчанию на сегодня
            ReminderDatePicker.SelectedDate = DateTime.Today;
            // Загружаем список задач
            LoadTasks();
            // Устанавливаем фокус на текстовое поле
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

                // Не выбираем ничего по умолчанию - задача необязательна
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

        // Обработчик нажатия кнопки "Добавить"
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string text = TextBoxReminder.Text.Trim();
            DateTime? date = ReminderDatePicker.SelectedDate;
            int? taskId = TaskComboBox.SelectedValue as int?;

            // Преобразуем 0 в null для "Не выбрано"
            if (taskId == 0)
                taskId = null;

            // Валидация: текст обязателен
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

            // Валидация: дата обязательна
            if (!date.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату напоминания.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ReminderDatePicker.Focus();
                return;
            }

            // Валидация: дата не должна быть в прошлом
            if (date.Value < DateTime.Today)
            {
                MessageBox.Show("Дата напоминания не может быть в прошлом.\nВыберите сегодняшнюю или будущую дату.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ReminderDatePicker.Focus();
                return;
            }

            // Создание нового напоминания
            ReminderModel reminder = new ReminderModel
            {
                Текст_напоминания = text,
                Дата_напоминания = date,
                ID_задачи = taskId
            };

            try
            {
                // Добавление напоминания в базу данных
                DbHelper.AddReminder(reminder);

                // Формируем сообщение об успехе
                string taskInfo = taskId.HasValue
                    ? $"\nСвязано с задачей: {(TaskComboBox.SelectedItem as TaskModel)?.Название_задачи}"
                    : "\nОбщее напоминание (не связано с задачей)";

                MessageBox.Show($"Напоминание успешно добавлено!\n\nДата: {date:dd.MM.yyyy}{taskInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
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

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+Enter - добавить напоминание (даже из текстового поля)
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Add_Click(this, new RoutedEventArgs());
            }
            // Escape - закрыть окно
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}