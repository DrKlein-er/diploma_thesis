using System;
using System.Windows;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditNoteWindow : Window
    {
        private NoteModel _note;

        public EditNoteWindow(NoteModel note)
        {
            InitializeComponent();
            _note = note;

            // Применяем текущую тему
            ApplyTheme();
            // Загружаем список задач
            LoadTasks();
            // Заполняем поля данными заметки
            LoadNoteData();
            // Устанавливаем фокус на первое поле
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
        // Загрузка данных заметки в поля формы
        private void LoadNoteData()
        {
            NoteTextBox.Text = _note.Текст_заметки;
            CreationDatePicker.SelectedDate = _note.Дата_создания;
            TaskComboBox.SelectedValue = _note.ID_задачи;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string noteText = NoteTextBox.Text.Trim();
            DateTime? creationDate = CreationDatePicker.SelectedDate;
            int? taskId = TaskComboBox.SelectedValue as int?;

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

            // Обновляем данные заметки
            _note.Текст_заметки = noteText;
            _note.Дата_создания = creationDate;
            _note.ID_задачи = taskId;

            try
            {
                // Обновление заметки в базе данных
                DbHelper.UpdateNote(_note);

                // Формируем сообщение об успехе
                string taskInfo = $"\nСвязана с задачей: {(TaskComboBox.SelectedItem as TaskModel)?.Название_задачи}";

                MessageBox.Show($"Заметка успешно обновлена!\n\nДата: {creationDate:dd.MM.yyyy}{taskInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении заметки:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+Enter - сохранить изменения (даже из текстового поля)
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
