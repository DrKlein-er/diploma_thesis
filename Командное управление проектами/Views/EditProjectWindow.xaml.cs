using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditProjectWindow : Window
    {
        private readonly ProjectModel _project;
        private readonly UserModel _currentUser;
        private readonly bool _isNewProject;

        public EditProjectWindow(ProjectModel project, UserModel currentUser, bool isNew)
        {
            InitializeComponent();
            _project = project;
            _currentUser = currentUser;
            _isNewProject = isNew;

            // Применяем текущую тему
            ApplyTheme();
            // Загружаем список сотрудников
            LoadEmployees();

            if (_isNewProject)
            {
                Title = "Новый проект";
                BudgetTab.IsEnabled = false;
                ResourcesTab.IsEnabled = false;
                HistoryTab.IsEnabled = false;
            }
            else
            {
                Title = "Редактировать проект";
                FillProjectData();
                LoadBudget();
                LoadResources();
                LoadHistory();
            }
            if (_currentUser.Роль == "Пользователь")
            {
                // 1. Скрываем кнопку сохранения изменений
                SaveProjectBtn.Visibility = Visibility.Collapsed;

                // 2. Скрываем кнопки управления бюджетом
                AddBudgetButton.Visibility = Visibility.Collapsed;
                DeleteBudgetButton.Visibility = Visibility.Collapsed;

                // 3. Скрываем кнопки управления ресурсами
                AssignResourceButton.Visibility = Visibility.Collapsed;
                RemoveResourceButton.Visibility = Visibility.Collapsed;

                // Дополнительно: Блокируем поля для ввода, чтобы было понятно, что это режим просмотра
                TitleBox.IsReadOnly = true;
                DescBox.IsReadOnly = true;
                StartDate.IsEnabled = false;
                EndDate.IsEnabled = false;
                StatusBox.IsEnabled = false;
                EmployeeComboBox.IsEnabled = false;

                // Меняем заголовок окна для ясности
                Title = "Просмотр проекта (Только чтение)";
            }
            // ================================================================

            // Устанавливаем фокус на первое поле (если доступно)
            if (TitleBox.IsEnabled && !TitleBox.IsReadOnly)
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

        // Загрузка списка сотрудников в ComboBox
        private void LoadEmployees()
        {
            try
            {
                var employees = DbHelper.GetAllEmployees();
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

        // Заполнение полей данными проекта
        private void FillProjectData()
        {
            TitleBox.Text = _project.Название_проекта;
            DescBox.Text = _project.Описание;
            StartDate.SelectedDate = _project.Дата_начала;
            EndDate.SelectedDate = _project.Дата_завершения;

            // Устанавливаем статус
            foreach (ComboBoxItem item in StatusBox.Items)
            {
                if (item.Content.ToString() == _project.Статус)
                {
                    StatusBox.SelectedItem = item;
                    break;
                }
            }

            // Устанавливаем ответственного
            EmployeeComboBox.SelectedValue = _project.ID_ответственного;
        }

        // Загрузка данных бюджета проекта
        private void LoadBudget()
        {
            try
            {
                BudgetGrid.ItemsSource = DbHelper.GetBudgetForProject(_project.ID_проекта);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки бюджета:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Загрузка ресурсов проекта
        private void LoadResources()
        {
            try
            {
                // Загружаем назначенные и доступные ресурсы
                var assigned = DbHelper.GetResourcesForProject(_project.ID_проекта);
                var available = DbHelper.GetAvailableResourcesForProject(_project.ID_проекта);

                AssignedResourcesList.ItemsSource = assigned;
                AvailableResourcesList.ItemsSource = available;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ресурсов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Загрузка истории изменений проекта
        private void LoadHistory()
        {
            try
            {
                var history = DbHelper.GetHistory("Проект", _project.ID_проекта);
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

        // Обработчик кнопки "Сохранить"
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string title = TitleBox.Text.Trim();
            string desc = DescBox.Text.Trim();
            DateTime? start = StartDate.SelectedDate;
            DateTime? end = EndDate.SelectedDate;
            string status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            int? employeeId = EmployeeComboBox.SelectedValue as int?;

            // Валидация: название обязательно
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Пожалуйста, введите название проекта.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            // Валидация: статус обязателен
            if (string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Пожалуйста, выберите статус проекта.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StatusBox.Focus();
                return;
            }

            // Валидация: дата завершения не раньше даты начала
            if (start.HasValue && end.HasValue && end < start)
            {
                MessageBox.Show("Дата завершения не может быть раньше даты начала.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EndDate.Focus();
                return;
            }

            try
            {
                // ✅ ОТСЛЕЖИВАЕМ ИЗМЕНЕНИЯ ДЛЯ ДЕТАЛЬНОГО ЛОГИРОВАНИЯ
                List<string> changes = new List<string>();

                // Проверяем изменение названия
                if (_project.Название_проекта != title)
                {
                    changes.Add($"Изменено название: '{_project.Название_проекта}' → '{title}'");
                }

                // Проверяем изменение описания
                if (_project.Описание != desc)
                {
                    if (string.IsNullOrEmpty(_project.Описание) && !string.IsNullOrEmpty(desc))
                    {
                        changes.Add("Добавлено описание");
                    }
                    else if (!string.IsNullOrEmpty(_project.Описание) && string.IsNullOrEmpty(desc))
                    {
                        changes.Add("Удалено описание");
                    }
                    else
                    {
                        changes.Add("Изменено описание");
                    }
                }

                // Проверяем изменение статуса
                if (_project.Статус != status)
                {
                    changes.Add($"Изменен статус: '{_project.Статус}' → '{status}'");
                }

                // Проверяем изменение даты начала
                if (_project.Дата_начала != start)
                {
                    string oldDate = _project.Дата_начала?.ToString("dd.MM.yyyy") ?? "не указана";
                    string newDate = start?.ToString("dd.MM.yyyy") ?? "не указана";
                    changes.Add($"Изменена дата начала: {oldDate} → {newDate}");
                }

                // Проверяем изменение даты завершения
                if (_project.Дата_завершения != end)
                {
                    string oldDate = _project.Дата_завершения?.ToString("dd.MM.yyyy") ?? "не указана";
                    string newDate = end?.ToString("dd.MM.yyyy") ?? "не указана";
                    changes.Add($"Изменена дата завершения: {oldDate} → {newDate}");
                }

                // Проверяем изменение ответственного
                if (_project.ID_ответственного != employeeId)
                {
                    string oldEmployee = _project.ID_ответственного.HasValue
                        ? (EmployeeComboBox.Items.Cast<EmployeeModel>()
                            .FirstOrDefault(emp => emp.ID_сотрудника == _project.ID_ответственного.Value)?.Имя_сотрудника ?? "Неизвестно")
                        : "не назначен";
                    string newEmployee = employeeId.HasValue
                        ? (EmployeeComboBox.SelectedItem as EmployeeModel)?.Имя_сотрудника ?? "Неизвестно"
                        : "не назначен";
                    changes.Add($"Изменен ответственный: {oldEmployee} → {newEmployee}");
                }

                // Обновляем данные проекта
                _project.Название_проекта = title;
                _project.Описание = desc;
                _project.Дата_начала = start;
                _project.Дата_завершения = end;
                _project.Статус = status;
                _project.ID_ответственного = employeeId;

                // Обновляем проект через DbHelper
                DbHelper.UpdateProject(_project);

                // ✅ ЛОГИРУЕМ КАЖДОЕ ИЗМЕНЕНИЕ ОТДЕЛЬНО
                if (changes.Count > 0)
                {
                    foreach (var change in changes)
                    {
                        DbHelper.LogChange("Проект", _project.ID_проекта, change, _currentUser.ID_сотрудника);
                    }
                }

                MessageBox.Show($"Проект '{title}' успешно обновлен!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении проекта:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки "Добавить бюджет"
        private void AddBudget_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddBudgetWindow(_project.ID_проекта, _currentUser);
            if (window.ShowDialog() == true)
            {
                LoadBudget();
                LoadHistory();
            }
        }

        // Обработчик кнопки "Удалить бюджет"
        private void DeleteBudget_Click(object sender, RoutedEventArgs e)
        {
            if (BudgetGrid.SelectedItem is BudgetModel selectedEntry)
            {
                if (MessageBox.Show($"Удалить запись бюджета '{selectedEntry.Назначение}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Удаляем через DbHelper
                        DbHelper.DeleteBudgetEntry(selectedEntry.ID_бюджета);

                        // ✅ ДЕТАЛЬНОЕ ЛОГИРОВАНИЕ
                        DbHelper.LogChange("Проект", _project.ID_проекта,
                            $"Удалена запись бюджета: \"{selectedEntry.Назначение}\" ({selectedEntry.Сумма:N2} руб.)",
                            _currentUser.ID_сотрудника);

                        LoadBudget();
                        LoadHistory();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите запись в бюджете для удаления.",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // Обработчик кнопки "Назначить ресурс"
        private void AssignResource_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableResourcesList.SelectedItem is ResourceModel selectedResource)
            {
                try
                {
                    // Назначаем через DbHelper
                    DbHelper.AssignResourceToProject(_project.ID_проекта, selectedResource.ID_ресурса);

                    // ✅ ДЕТАЛЬНОЕ ЛОГИРОВАНИЕ
                    DbHelper.LogChange("Проект", _project.ID_проекта,
                        $"Назначен ресурс: \"{selectedResource.Название}\"", _currentUser.ID_сотрудника);

                    LoadResources();
                    LoadHistory();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при назначении ресурса:\n{ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите ресурс из списка 'Доступные ресурсы'.",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // Обработчик кнопки "Удалить ресурс"
        private void RemoveResource_Click(object sender, RoutedEventArgs e)
        {
            if (AssignedResourcesList.SelectedItem is ResourceModel selectedResource)
            {
                try
                {
                    // Удаляем через DbHelper
                    DbHelper.RemoveResourceFromProject(_project.ID_проекта, selectedResource.ID_ресурса);

                    // ✅ ДЕТАЛЬНОЕ ЛОГИРОВАНИЕ
                    DbHelper.LogChange("Проект", _project.ID_проекта,
                        $"Снят ресурс: \"{selectedResource.Название}\"", _currentUser.ID_сотрудника);

                    LoadResources();
                    LoadHistory();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении ресурса:\n{ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите ресурс из списка 'Ресурсы проекта'.",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }



        // Обработка горячих клавиш
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