using System;
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
                // РЕЖИМ СОЗДАНИЯ
                Title = "Новый проект";
                // Блокируем вкладки (они станут доступны после создания проекта)
                BudgetTab.IsEnabled = false;
                ResourcesTab.IsEnabled = false;
                HistoryTab.IsEnabled = false;
            }
            else
            {
                // РЕЖИМ РЕДАКТИРОВАНИЯ
                Title = "Редактировать проект";
                // Заполняем данные проекта
                FillProjectData();
                // Загружаем данные для вкладок
                LoadBudget();
                LoadResources();
                LoadHistory();
            }

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

            // Обновляем данные проекта
            _project.Название_проекта = title;
            _project.Описание = desc;
            _project.Дата_начала = start;
            _project.Дата_завершения = end;
            _project.Статус = status;
            _project.ID_ответственного = employeeId;

            try
            {
                // Обновляем проект через DbHelper
                DbHelper.UpdateProject(_project);

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
            var window = new AddBudgetWindow(_project.ID_проекта);
            if (window.ShowDialog() == true)
            {
                LoadBudget();
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
                        LoadBudget();
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
                    DbHelper.LogChange("Ресурсы", _project.ID_проекта,
                        $"Назначен ресурс: {selectedResource.Название}", _currentUser.ID_сотрудника);
                    LoadResources();
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
                    DbHelper.LogChange("Ресурсы", _project.ID_проекта,
                        $"Снят ресурс: {selectedResource.Название}", _currentUser.ID_сотрудника);
                    LoadResources();
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

            // Ctrl+S - сохранить изменения
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
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