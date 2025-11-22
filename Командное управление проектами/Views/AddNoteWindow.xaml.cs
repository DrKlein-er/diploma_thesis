using System;
using System.Windows;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class AddNoteWindow : Window
    {
        public AddNoteWindow()
        {
            InitializeComponent();
            // Применяем текущую тему
            ApplyTheme();
            // Устанавливаем дату по умолчанию на сегодня
            CreationDatePicker.SelectedDate = DateTime.Today;
            // Загружаем список задач
            LoadTasks();
            // Устанавливаем фокус на текстовое поле
            NoteTextBox.Focus();
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

                // Не выбираем ничего по умолчанию - пользователь должен выбрать
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
            string noteText = NoteTextBox.Text.Trim();
            DateTime? creationDate = CreationDatePicker.SelectedDate;
            int? taskId = TaskComboBox.SelectedValue as int?;

            // Преобразуем 0 в null для "Не выбрано"
            if (taskId == 0)
                taskId = null;

            // Валидация: текст обязателен
            if (string.IsNullOrWhiteSpace(noteText))
            {
                MessageBox.Show("Пожалуйста, введите текст заметки.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                NoteTextBox.Focus();
                return;
            }

            // Валидация: минимальная длина текста
            if (noteText.Length < 3)
            {
                MessageBox.Show("Текст заметки должен содержать минимум 3 символа.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                NoteTextBox.Focus();
                return;
            }

            // Валидация: дата обязательна
            if (!creationDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату создания заметки.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                CreationDatePicker.Focus();
                return;
            }

            // Валидация: задача обязательна
            if (!taskId.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите задачу.\nЗаметка должна быть связана с задачей.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TaskComboBox.Focus();
                return;
            }

            // Создание новой заметки
            NoteModel note = new NoteModel
            {
                Текст_заметки = noteText,
                Дата_создания = creationDate,
                ID_задачи = taskId
            };

            try
            {
                // Добавление заметки в базу данных
                DbHelper.AddNote(note);

                // Формируем сообщение об успехе
                string taskInfo = $"\nСвязана с задачей: {(TaskComboBox.SelectedItem as TaskModel)?.Название_задачи}";

                MessageBox.Show($"Заметка успешно добавлена!\n\nДата: {creationDate:dd.MM.yyyy}{taskInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении заметки:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+Enter - добавить заметку (даже из текстового поля)
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
