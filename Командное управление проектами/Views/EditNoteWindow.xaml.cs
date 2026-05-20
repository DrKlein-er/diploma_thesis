using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditNoteWindow : Window
    {
        private readonly NoteModel _note;
        private bool _isLoading = true;

        public EditNoteWindow(NoteModel note)
        {
            InitializeComponent();
            ApplyTheme();

            _note = note ?? throw new ArgumentNullException(nameof(note));

            LoadProjects();
            LoadTasks();
            LoadParentTasks();
            LoadNoteData();

            _isLoading = false;
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
            try { ProjectComboBox.ItemsSource = DbHelper.GetAllProjects(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTasks()
        {
            try { TaskComboBox.ItemsSource = DbHelper.GetAllTasks(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка задач:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadParentTasks()
        {
            try { ParentTaskComboBox.ItemsSource = DbHelper.GetAllTasks(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка задач:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Заполнение полей текущими значениями заметки
        private void LoadNoteData()
        {
            TitleBox.Text = _note.Заголовок ?? string.Empty;
            NoteTextBox.Text = _note.Текст_заметки ?? string.Empty;
            PinnedCheckBox.IsChecked = _note.Закреплена;

            // Метка с датой создания
            if (_note.Дата_создания.HasValue)
                CreatedAtLabel.Text = $"Создано: {_note.Дата_создания.Value:dd.MM.yyyy HH:mm}";

            // Цвет
            switch (_note.Цвет)
            {
                case "Розовый": ColorPink.IsChecked = true; break;
                case "Голубой": ColorBlue.IsChecked = true; break;
                case "Зелёный": ColorGreen.IsChecked = true; break;
                case "Серый": ColorGray.IsChecked = true; break;
                case "Жёлтый":
                default: ColorYellow.IsChecked = true; break;
            }

            // Привязка — определяем по тому, какой ID заполнен
            if (_note.ID_подзадачи.HasValue)
            {
                LinkSubtask.IsChecked = true;
                SubtaskPanel.Visibility = Visibility.Visible;

                // Найти родительскую задачу для этой подзадачи
                // (ID_задачи в NoteModel у нас может быть NULL, поэтому идём через подзадачу)
                // Для надёжности можно загрузить подзадачу и взять её ID_задачи через GetSubtaskParent
                // Но в текущей схеме SubtaskModel содержит ID_задачи — вытащим его одним запросом
                int? parentTaskId = ResolveParentTaskFor(_note.ID_подзадачи.Value);
                if (parentTaskId.HasValue)
                {
                    ParentTaskComboBox.SelectedValue = parentTaskId.Value;
                    // SubtaskComboBox.ItemsSource будет установлен в SelectionChanged,
                    // а нужная подзадача — выбрана внутри ParentTaskComboBox_SelectionChanged
                }
            }
            else if (_note.ID_задачи.HasValue)
            {
                LinkTask.IsChecked = true;
                TaskComboBox.Visibility = Visibility.Visible;
                TaskComboBox.SelectedValue = _note.ID_задачи.Value;
            }
            else if (_note.ID_проекта.HasValue)
            {
                LinkProject.IsChecked = true;
                ProjectComboBox.Visibility = Visibility.Visible;
                ProjectComboBox.SelectedValue = _note.ID_проекта.Value;
            }
            else
            {
                LinkNone.IsChecked = true;
            }
        }

        // Вспомогательный метод: по ID подзадачи находит ID родительской задачи
        private int? ResolveParentTaskFor(int subtaskId)
        {
            try
            {
                // Перебираем все задачи и ищем ту, в которой есть данная подзадача
                var allTasks = DbHelper.GetAllTasks();
                foreach (var task in allTasks)
                {
                    var subs = DbHelper.GetSubtasksByTaskId(task.ID_задачи);
                    foreach (var sub in subs)
                    {
                        if (sub.ID_подзадачи == subtaskId)
                            return task.ID_задачи;
                    }
                }
            }
            catch { /* игнорируем — вернём null */ }
            return null;
        }

        private void LinkType_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            if (ProjectComboBox == null) return;

            ProjectComboBox.Visibility = Visibility.Collapsed;
            TaskComboBox.Visibility = Visibility.Collapsed;
            SubtaskPanel.Visibility = Visibility.Collapsed;

            ProjectComboBox.SelectedIndex = -1;
            TaskComboBox.SelectedIndex = -1;
            ParentTaskComboBox.SelectedIndex = -1;
            SubtaskComboBox.ItemsSource = null;
            SubtaskComboBox.IsEnabled = false;

            if (LinkProject?.IsChecked == true)
                ProjectComboBox.Visibility = Visibility.Visible;
            else if (LinkTask?.IsChecked == true)
                TaskComboBox.Visibility = Visibility.Visible;
            else if (LinkSubtask?.IsChecked == true)
                SubtaskPanel.Visibility = Visibility.Visible;
        }

        private void ParentTaskComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int? parentTaskId = ParentTaskComboBox.SelectedValue as int?;
                if (parentTaskId.HasValue)
                {
                    var subtasks = DbHelper.GetSubtasksByTaskId(parentTaskId.Value);
                    SubtaskComboBox.ItemsSource = subtasks;
                    SubtaskComboBox.IsEnabled = subtasks.Count > 0;

                    // При первой загрузке восстанавливаем выбранную подзадачу
                    if (_isLoading && _note.ID_подзадачи.HasValue)
                    {
                        SubtaskComboBox.SelectedValue = _note.ID_подзадачи.Value;
                    }
                    else
                    {
                        SubtaskComboBox.SelectedIndex = -1;
                    }
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

        private string GetSelectedColor()
        {
            if (ColorYellow.IsChecked == true) return "Жёлтый";
            if (ColorPink.IsChecked == true) return "Розовый";
            if (ColorBlue.IsChecked == true) return "Голубой";
            if (ColorGreen.IsChecked == true) return "Зелёный";
            if (ColorGray.IsChecked == true) return "Серый";
            return "Жёлтый";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
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

            // Привязка
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

            // Обновление модели
            _note.Заголовок = title;
            _note.Текст_заметки = string.IsNullOrWhiteSpace(text) ? null : text;
            _note.Цвет = color;
            _note.Закреплена = isPinned;
            _note.ID_проекта = projectId;
            _note.ID_задачи = taskId;
            _note.ID_подзадачи = subtaskId;

            try
            {
                DbHelper.UpdateNote(_note);

                MessageBox.Show("Заметка успешно обновлена!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении заметки:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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