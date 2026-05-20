using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
            ApplyTheme();

            LoadProjects();
            LoadTasks();
            LoadParentTasks();

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

        private void LoadProjects()
        {
            try
            {
                ProjectComboBox.ItemsSource = DbHelper.GetAllProjects();
                ProjectComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTasks()
        {
            try
            {
                TaskComboBox.ItemsSource = DbHelper.GetAllTasks();
                TaskComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка задач:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadParentTasks()
        {
            try
            {
                ParentTaskComboBox.ItemsSource = DbHelper.GetAllTasks();
                ParentTaskComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка задач:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Переключение видимости полей привязки
        private void LinkType_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded && ProjectComboBox == null) return;

            // По умолчанию всё скрыто
            ProjectComboBox.Visibility = Visibility.Collapsed;
            TaskComboBox.Visibility = Visibility.Collapsed;
            SubtaskPanel.Visibility = Visibility.Collapsed;

            // Сбрасываем выбор
            ProjectComboBox.SelectedIndex = -1;
            TaskComboBox.SelectedIndex = -1;
            ParentTaskComboBox.SelectedIndex = -1;
            SubtaskComboBox.ItemsSource = null;
            SubtaskComboBox.IsEnabled = false;

            // Показываем нужный контрол
            if (LinkProject?.IsChecked == true)
                ProjectComboBox.Visibility = Visibility.Visible;
            else if (LinkTask?.IsChecked == true)
                TaskComboBox.Visibility = Visibility.Visible;
            else if (LinkSubtask?.IsChecked == true)
                SubtaskPanel.Visibility = Visibility.Visible;
        }

        // Для подсветки: при выборе задачи как привязки — ничего не делаем дополнительно
        private void TaskComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Заглушка, обработчик нужен только для XAML; при необходимости можно расширить
        }

        // При выборе родительской задачи в режиме «Подзадача» — подгружаем её подзадачи
        private void ParentTaskComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int? parentTaskId = ParentTaskComboBox.SelectedValue as int?;
                if (parentTaskId.HasValue)
                {
                    var subtasks = DbHelper.GetSubtasksByTaskId(parentTaskId.Value);
                    SubtaskComboBox.ItemsSource = subtasks;
                    SubtaskComboBox.SelectedIndex = -1;
                    SubtaskComboBox.IsEnabled = subtasks.Count > 0;
                }
                else
                {
                    SubtaskComboBox.ItemsSource = null;
                    SubtaskComboBox.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подзадач:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Получение выбранного цвета из RadioButton
        private string GetSelectedColor()
        {
            if (ColorYellow.IsChecked == true) return "Жёлтый";
            if (ColorPink.IsChecked == true) return "Розовый";
            if (ColorBlue.IsChecked == true) return "Голубой";
            if (ColorGreen.IsChecked == true) return "Зелёный";
            if (ColorGray.IsChecked == true) return "Серый";
            return "Жёлтый";
        }

        // Обработчик нажатия кнопки "Добавить"
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string text = NoteTextBox.Text.Trim();
            string color = GetSelectedColor();
            bool isPinned = PinnedCheckBox.IsChecked == true;

            // Валидация: заголовок
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Пожалуйста, введите заголовок заметки.",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            if (title.Length < 3)
            {
                MessageBox.Show("Заголовок должен содержать минимум 3 символа.",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            // Определяем привязку
            int? projectId = null;
            int? taskId = null;
            int? subtaskId = null;

            if (LinkProject.IsChecked == true)
            {
                projectId = ProjectComboBox.SelectedValue as int?;
                if (!projectId.HasValue)
                {
                    MessageBox.Show("Выберите проект для привязки заметки.",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProjectComboBox.Focus();
                    return;
                }
            }
            else if (LinkTask.IsChecked == true)
            {
                taskId = TaskComboBox.SelectedValue as int?;
                if (!taskId.HasValue)
                {
                    MessageBox.Show("Выберите задачу для привязки заметки.",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TaskComboBox.Focus();
                    return;
                }
            }
            else if (LinkSubtask.IsChecked == true)
            {
                subtaskId = SubtaskComboBox.SelectedValue as int?;
                if (!subtaskId.HasValue)
                {
                    MessageBox.Show("Выберите родительскую задачу и подзадачу для привязки заметки.",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            // Если LinkNone — все три ID остаются null, это валидно

            // Создание модели
            NoteModel note = new NoteModel
            {
                Заголовок = title,
                Текст_заметки = string.IsNullOrWhiteSpace(text) ? null : text,
                Цвет = color,
                Закреплена = isPinned,
                Дата_создания = DateTime.Now,
                ID_проекта = projectId,
                ID_задачи = taskId,
                ID_подзадачи = subtaskId
            };

            try
            {
                DbHelper.AddNote(note);

                MessageBox.Show("Заметка успешно добавлена!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении заметки:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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