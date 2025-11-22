using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;
using Командное_управление_проектами.Views;

namespace Командное_управление_проектами
{
    public partial class EditTaskWindow : Window
    {
        private readonly string connectionString = "Data Source=DESKTOP-JRVC3AP;Initial Catalog=Coursework;Integrated Security=True";
        private TaskModel _task;
        private UserModel _currentUser;

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

        // Загрузка списка проектов в ComboBox
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

        // Загрузка списка сотрудников в ComboBox
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

        // Загрузка данных задачи в поля формы
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

            // Получаем ID проекта из базы данных
            int? projectId = GetProjectIdByTaskId(_task.ID_задачи);
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

        // Получение ID проекта по ID задачи из базы данных
        private int? GetProjectIdByTaskId(int taskId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ID_проекта FROM Задачи WHERE ID_задачи = @taskId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@taskId", taskId);
                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : (int?)null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении проекта:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }

        // Обработчик нажатия кнопки "Сохранить"
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
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            // Валидация: дата начала должна быть раньше даты завершения
            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                MessageBox.Show("Дата начала не может быть позже даты завершения.",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE Задачи 
                        SET Название_задачи = @title, 
                            Описание = @desc, 
                            Приоритет = @priority, 
                            Статус = @status, 
                            Дата_начала = @startDate,
                            Дата_завершения = @endDate,
                            ID_проекта = @projectId, 
                            ID_ответственного = @empId
                        WHERE ID_задачи = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(desc) ? (object)DBNull.Value : desc);
                        cmd.Parameters.AddWithValue("@priority", string.IsNullOrEmpty(priority) ? (object)DBNull.Value : priority);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@startDate", startDate.HasValue ? (object)startDate.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@endDate", endDate.HasValue ? (object)endDate.Value : DBNull.Value); cmd.Parameters.AddWithValue("@projectId", projectId.Value);
                        cmd.Parameters.AddWithValue("@empId", employeeId.HasValue ? (object)employeeId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", _task.ID_задачи);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Логируем изменение
                DbHelper.LogChange("Задача", _task.ID_задачи,
                    $"Изменена задача '{title}'", _currentUser.ID_сотрудника);

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

        // Загрузка списка подзадач
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

        // Обработчик добавления подзадачи
        private void AddSubtask_Click(object sender, RoutedEventArgs e)
        {
            var addSubtaskWindow = new AddSubtaskWindow(_task.ID_задачи);
            if (addSubtaskWindow.ShowDialog() == true)
            {
                DbHelper.LogChange("Подзадача", _task.ID_задачи,
                    $"Для задачи '{_task.Название_задачи}' добавлена новая подзадача",
                    _currentUser.ID_сотрудника);
                LoadSubtasks();
            }
        }

        // Обработчик редактирования подзадачи
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

        // Обработчик удаления подзадачи
        private void DeleteSubtask_Click(object sender, RoutedEventArgs e)
        {
            if (SubtasksGrid.SelectedItem is SubtaskModel selected)
            {
                if (MessageBox.Show($"Удалить подзадачу '{selected.Название_подзадачи}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        DbHelper.DeleteSubtask(selected.ID_подзадачи);
                        DbHelper.LogChange("Подзадача", _task.ID_задачи,
                            $"Удалена подзадача '{selected.Название_подзадачи}'",
                            _currentUser.ID_сотрудника);
                        LoadSubtasks();

                        MessageBox.Show("Подзадача успешно удалена.",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
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
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        // Загрузка списка файлов
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

        // Обработчик добавления файла
        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Title = "Выберите файл для загрузки";
                dlg.Filter = "Все файлы (*.*)|*.*";

                if (dlg.ShowDialog() == true)
                {
                    // Создаём папку для вложений, если её нет
                    string attachmentsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");
                    Directory.CreateDirectory(attachmentsDir);

                    string sourceFile = dlg.FileName;
                    string fileName = Path.GetFileName(sourceFile);
                    string destFile = Path.Combine(attachmentsDir, fileName);

                    // Копируем файл в папку Attachments
                    File.Copy(sourceFile, destFile, true);

                    // Создаём запись в базе данных
                    var newFile = new FileModel
                    {
                        Название_файла = fileName,
                        Путь_к_файлу = fileName, // Сохраняем только имя файла
                        ID_задачи = _task.ID_задачи
                    };

                    DbHelper.AddFile(newFile);
                    DbHelper.LogChange("Файл", _task.ID_задачи,
                        $"Добавлен файл '{fileName}' к задаче '{_task.Название_задачи}'",
                        _currentUser.ID_сотрудника);

                    LoadFiles();

                    MessageBox.Show($"Файл '{fileName}' успешно добавлен.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении файла:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработчик открытия файла
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

        // Обработчик удаления файла
        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListBox.SelectedItem is FileModel selected)
            {
                if (MessageBox.Show($"Удалить файл '{selected.Название_файла}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Удаляем запись из базы данных
                        DbHelper.DeleteFile(selected.ID_файла);

                        // Удаляем физический файл
                        string attachmentsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");
                        string fullPath = Path.Combine(attachmentsDir, selected.Путь_к_файлу);

                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                        }

                        DbHelper.LogChange("Файл", _task.ID_задачи,
                            $"Удалён файл '{selected.Название_файла}' из задачи '{_task.Название_задачи}'",
                            _currentUser.ID_сотрудника);
                        LoadFiles();

                        MessageBox.Show("Файл успешно удалён.",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
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
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Escape - закрыть окно
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
