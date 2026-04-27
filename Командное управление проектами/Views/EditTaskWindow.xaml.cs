using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;
using Командное_управление_проектами.Views;

namespace Командное_управление_проектами
{

    /// Окно редактирования задачи с поддержкой управления подзадачами и файлами

    public partial class EditTaskWindow : Window
    {
        private TaskModel _task;
        private UserModel _currentUser;

        /// Конструктор окна редактирования задачи

        public EditTaskWindow(TaskModel task, UserModel currentUser)
        {
            InitializeComponent();
            _task = task;
            _currentUser = currentUser;

            // Применяем текущую тему
            ApplyTheme();
            // Загружаем данные в ComboBox
            LoadProjects();
            LoadEmployees();
            // Заполняем поля данными задачи
            LoadTaskData();
            // Загружаем подзадачи
            LoadSubtasks();
            // Загружаем файлы
            LoadFiles();
            // Загружаем историю изменений
            LoadHistory();
            // Устанавливаем фокус на первое поле
            TitleBox.Focus();

            // Ограничение прав для роли "Пользователь"

            if (_currentUser.Роль == "Пользователь")
            {
                // 1. Скрываем кнопку сохранения основных изменений
                SaveTaskBtn.Visibility = Visibility.Collapsed;

                // 2. Блокируем поля ввода, чтобы нельзя было менять данные
                TitleBox.IsReadOnly = true;
                DescBox.IsReadOnly = true;

                PriorityBox.IsEnabled = false;
                StatusBox.IsEnabled = false;
                StartDatePicker.IsEnabled = false;
                EndDatePicker.IsEnabled = false;
                ProjectComboBox.IsEnabled = false;
                EmployeeComboBox.IsEnabled = false;

                // Меняем заголовок окна
                Title = "Просмотр задачи";
            }
        }


        /// Применение текущей темы приложения к окну

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


        /// Загрузка списка проектов в ComboBox

        private void LoadProjects()
        {
            try
            {
                List<ProjectModel> projects = DbHelper.GetAllProjects();
                ProjectComboBox.ItemsSource = projects;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        /// Загрузка списка сотрудников в ComboBox

        private void LoadEmployees()
        {
            try
            {
                List<EmployeeModel> employees = DbHelper.GetAllEmployees();
                EmployeeComboBox.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка сотрудников:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        /// Загрузка данных задачи в поля формы

        private void LoadTaskData()
        {
            TitleBox.Text = _task.Название_задачи;
            DescBox.Text = _task.Описание;
            StartDatePicker.SelectedDate = _task.Дата_начала;
            EndDatePicker.SelectedDate = _task.Дата_завершения;

            // Устанавливаем приоритет
            foreach (ComboBoxItem item in PriorityBox.Items)
            {
                if (item.Content.ToString() == _task.Приоритет)
                {
                    PriorityBox.SelectedItem = item;
                    break;
                }
            }

            // Устанавливаем статус
            foreach (ComboBoxItem item in StatusBox.Items)
            {
                if (item.Content.ToString() == _task.Статус)
                {
                    StatusBox.SelectedItem = item;
                    break;
                }
            }

            // Получаем ID проекта из базы данных через DbHelper
            int? projectId = DbHelper.GetProjectIdByTaskId(_task.ID_задачи);
            if (projectId.HasValue)
            {
                ProjectComboBox.SelectedValue = projectId.Value;
            }

            // Устанавливаем ответственного
            if (_task.ID_ответственного.HasValue)
            {
                EmployeeComboBox.SelectedValue = _task.ID_ответственного.Value;
            }
        }

        /// Загрузка истории изменений задачи

        private void LoadHistory()
        {
            try
            {
                var history = DbHelper.GetHistory("Задача", _task.ID_задачи);
                HistoryGrid.ItemsSource = null;
                HistoryGrid.ItemsSource = history;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        /// Обработчик нажатия кнопки "Сохранить"

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string title = TitleBox.Text.Trim();
            string desc = DescBox.Text.Trim();
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

            // Валидация: статус обязателен
            if (string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Пожалуйста, выберите статус задачи.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StatusBox.Focus();
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
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            // Валидация: дата начала должна быть раньше даты завершения
            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                MessageBox.Show("Дата начала не может быть позже даты завершения.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            try
            {
                List<string> changes = new List<string>();

                // Проверяем изменение названия
                if (_task.Название_задачи != title)
                {
                    changes.Add($"Изменено название: '{_task.Название_задачи}' → '{title}'");
                }

                // Проверяем изменение описания
                if (_task.Описание != desc)
                {
                    if (string.IsNullOrEmpty(_task.Описание) && !string.IsNullOrEmpty(desc))
                    {
                        changes.Add("Добавлено описание");
                    }
                    else if (!string.IsNullOrEmpty(_task.Описание) && string.IsNullOrEmpty(desc))
                    {
                        changes.Add("Удалено описание");
                    }
                    else
                    {
                        changes.Add("Изменено описание");
                    }
                }

                // Проверяем изменение приоритета
                if (_task.Приоритет != priority)
                {
                    changes.Add($"Изменен приоритет: '{_task.Приоритет}' → '{priority}'");
                }

                // Проверяем изменение статуса
                if (_task.Статус != status)
                {
                    changes.Add($"Изменен статус: '{_task.Статус}' → '{status}'");
                }

                // Проверяем изменение даты начала
                if (_task.Дата_начала != startDate)
                {
                    string oldDate = _task.Дата_начала?.ToString("dd.MM.yyyy") ?? "не указана";
                    string newDate = startDate?.ToString("dd.MM.yyyy") ?? "не указана";
                    changes.Add($"Изменена дата начала: {oldDate} → {newDate}");
                }

                // Проверяем изменение даты завершения
                if (_task.Дата_завершения != endDate)
                {
                    string oldDate = _task.Дата_завершения?.ToString("dd.MM.yyyy") ?? "не указана";
                    string newDate = endDate?.ToString("dd.MM.yyyy") ?? "не указана";
                    changes.Add($"Изменена дата завершения: {oldDate} → {newDate}");
                }

                // Проверяем изменение ответственного
                if (_task.ID_ответственного != employeeId)
                {
                    string oldEmployee = _task.ID_ответственного.HasValue
                        ? (EmployeeComboBox.Items.Cast<EmployeeModel>()
                            .FirstOrDefault(emp => emp.ID_сотрудника == _task.ID_ответственного.Value)?.Имя_сотрудника ?? "Неизвестно")
                        : "не назначен";
                    string newEmployee = employeeId.HasValue
                        ? (EmployeeComboBox.SelectedItem as EmployeeModel)?.Имя_сотрудника ?? "Неизвестно"
                        : "не назначен";
                    changes.Add($"Изменен ответственный: {oldEmployee} → {newEmployee}");
                }

                // Обновляем задачу через DbHelper
                _task.Название_задачи = title;
                _task.Описание = desc;
                _task.Приоритет = priority;
                _task.Статус = status;
                _task.Дата_начала = startDate;
                _task.Дата_завершения = endDate;
                _task.ID_ответственного = employeeId;

                DbHelper.UpdateTask(_task, projectId.Value);

                if (changes.Count > 0)
                {
                    foreach (var change in changes)
                    {
                        DbHelper.LogChange("Задача", _task.ID_задачи, change, _currentUser.ID_сотрудника);
                    }
                }

                // Формируем сообщение об успехе
                string projectName = (ProjectComboBox.SelectedItem as ProjectModel)?.Название_проекта;
                string employeeName = employeeId.HasValue
                    ? (EmployeeComboBox.SelectedItem as EmployeeModel)?.Имя_сотрудника
                    : "не назначен";
                string dueInfo = "";
                if (startDate.HasValue && endDate.HasValue)
                {
                    dueInfo = $"\nПериод: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                }
                else if (endDate.HasValue)
                {
                    dueInfo = $"\nДата завершения: {endDate:dd.MM.yyyy}";
                }

                MessageBox.Show($"Задача успешно обновлена!\n\n" +
                              $"Название: {title}\n" +
                              $"Статус: {status}\n" +
                              $"Проект: {projectName}\n" +
                              $"Ответственный: {employeeName}{dueInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении задачи:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        /// Загрузка списка подзадач

        private void LoadSubtasks()
        {
            try
            {
                SubtasksGrid.ItemsSource = DbHelper.GetSubtasksByTaskId(_task.ID_задачи);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подзадач:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// Обработчик добавления подзадачи

        private void AddSubtask_Click(object sender, RoutedEventArgs e)
        {
            var addSubtaskWindow = new AddSubtaskWindow(_task.ID_задачи);
            if (addSubtaskWindow.ShowDialog() == true)
            {
                DbHelper.LogChange("Подзадача", _task.ID_задачи,
                    $"Для задачи '{_task.Название_задачи}' добавлена новая подзадача",
                    _currentUser.ID_сотрудника);
                LoadSubtasks();
                LoadHistory();
            }
        }


        /// Обработчик редактирования подзадачи

        private void EditSubtask_Click(object sender, RoutedEventArgs e)
        {
            if (SubtasksGrid.SelectedItem is SubtaskModel selected)
            {
                var editSubtaskWindow = new EditSubtaskWindow(selected);
                if (editSubtaskWindow.ShowDialog() == true)
                {
                    DbHelper.LogChange("Подзадача", _task.ID_задачи,
                        $"Изменена подзадача '{selected.Название_подзадачи}'",
                        _currentUser.ID_сотрудника);
                    LoadSubtasks();
                    LoadHistory();
                }
            }
            else
            {
                MessageBox.Show("Выберите подзадачу для редактирования.",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }


        /// Обработчик удаления подзадачи

        private void DeleteSubtask_Click(object sender, RoutedEventArgs e)
        {
            if (SubtasksGrid.SelectedItem is SubtaskModel selectedSubtask)
            {
                if (MessageBox.Show($"Удалить подзадачу '{selectedSubtask.Название_подзадачи}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Удаляем через DbHelper
                        DbHelper.DeleteSubtask(selectedSubtask.ID_подзадачи);

                        DbHelper.LogChange("Задача", _task.ID_задачи,
                            $"Удалена подзадача: \"{selectedSubtask.Название_подзадачи}\"",
                            _currentUser.ID_сотрудника);

                        LoadSubtasks();
                        LoadHistory();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении подзадачи:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите подзадачу для удаления.",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }


        /// Загрузка списка файлов

        private void LoadFiles()
        {
            try
            {
                FilesListBox.ItemsSource = DbHelper.GetFilesForTask(_task.ID_задачи);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файлов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        /// Обработчик добавления файла

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите файл для добавления",
                Filter = "Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(filePath);

                    var fileModel = new FileModel
                    {
                        Название_файла = fileName,
                        Путь_к_файлу = filePath,
                        ID_задачи = _task.ID_задачи
                    };

                    // Добавляем файл через DbHelper
                    DbHelper.AddFile(fileModel);

                    // ДЕТАЛЬНОЕ ЛОГИРОВАНИЕ
                    DbHelper.LogChange("Задача", _task.ID_задачи,
                        $"Прикреплён файл: \"{fileName}\"", _currentUser.ID_сотрудника);

                    LoadFiles();
                    LoadHistory();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении файла:\n{ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }


        /// Обработчик открытия файла

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListBox.SelectedItem is FileModel selected)
            {
                try
                {
                    string attachmentsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");
                    string fullPath = Path.Combine(attachmentsDir, selected.Путь_к_файлу);

                    if (File.Exists(fullPath))
                    {
                        Process.Start(fullPath);
                    }
                    else
                    {
                        MessageBox.Show($"Файл не найден.\nВозможно, он был удалён вручную.\n\nПуть: {fullPath}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла:\n{ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите файл для открытия.",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }


        /// Обработчик удаления файла

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListBox.SelectedItem is FileModel selectedFile)
            {
                if (MessageBox.Show($"Удалить файл '{selectedFile.Название_файла}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Удаляем через DbHelper
                        DbHelper.DeleteFile(selectedFile.ID_файла);

                        // ДЕТАЛЬНОЕ ЛОГИРОВАНИЕ
                        DbHelper.LogChange("Задача", _task.ID_задачи,
                            $"Удалён файл: \"{selectedFile.Название_файла}\"",
                            _currentUser.ID_сотрудника);

                        LoadFiles();
                        LoadHistory();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении файла:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите файл для удаления.",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+Enter - сохранить изменения
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