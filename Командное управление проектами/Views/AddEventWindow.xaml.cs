using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class AddEventWindow : Window
    {
        public AddEventWindow()
        {
            InitializeComponent();
            ApplyTheme();

            // Значения по умолчанию
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;
            TypeComboBox.SelectedIndex = 0; // Встреча

            LoadProjects();
            TaskComboBox.IsEnabled = false;

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
                var projects = DbHelper.GetAllProjects();
                ProjectComboBox.ItemsSource = projects;
                ProjectComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // При выборе проекта подгружаем его задачи
        private void ProjectComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                int? projectId = ProjectComboBox.SelectedValue as int?;
                if (projectId.HasValue)
                {
                    var tasks = DbHelper.GetTasksByProject(projectId.Value);
                    TaskComboBox.ItemsSource = tasks;
                    TaskComboBox.SelectedIndex = -1;
                    TaskComboBox.IsEnabled = true;
                }
                else
                {
                    TaskComboBox.ItemsSource = null;
                    TaskComboBox.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки задач проекта:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Разбор времени в формате "HH:MM"
        private static bool TryParseTime(string text, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(text?.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out time)
                || TimeSpan.TryParseExact(text?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time);
        }

        // Обработчик кнопки "Добавить"
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Сбор данных
            string title = TitleBox.Text.Trim();
            string description = DescBox.Text.Trim();
            string place = PlaceBox.Text.Trim();
            string type = (TypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Другое";

            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            int? projectId = ProjectComboBox.SelectedValue as int?;
            int? taskId = TaskComboBox.SelectedValue as int?;

            // Валидация: название
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Пожалуйста, введите название события.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            // Валидация: дата начала
            if (!startDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату начала события.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            // Валидация: время начала
            if (!TryParseTime(StartTimeBox.Text, out TimeSpan startTime))
            {
                MessageBox.Show("Время начала должно быть в формате ЧЧ:ММ (например, 09:30).",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StartTimeBox.Focus();
                return;
            }

            DateTime startDateTime = startDate.Value.Date.Add(startTime);

            // Дата/время окончания — необязательно, но если задано — валидируем оба
            DateTime? endDateTime = null;
            bool hasEndDate = endDate.HasValue;
            bool hasEndTime = !string.IsNullOrWhiteSpace(EndTimeBox.Text);

            if (hasEndDate && hasEndTime)
            {
                if (!TryParseTime(EndTimeBox.Text, out TimeSpan endTime))
                {
                    MessageBox.Show("Время окончания должно быть в формате ЧЧ:ММ.",
                        "Ошибка валидации",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    EndTimeBox.Focus();
                    return;
                }
                endDateTime = endDate.Value.Date.Add(endTime);

                if (endDateTime.Value <= startDateTime)
                {
                    MessageBox.Show("Дата/время окончания должны быть позже даты/времени начала.",
                        "Ошибка валидации",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    EndDatePicker.Focus();
                    return;
                }
            }
            else if (hasEndDate ^ hasEndTime)
            {
                // Заполнено только одно из двух полей окончания
                MessageBox.Show("Заполните и дату, и время окончания — либо оставьте оба пустыми.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Создание модели
            EventModel newEvent = new EventModel
            {
                Название_события = title,
                Описание = string.IsNullOrWhiteSpace(description) ? null : description,
                Дата_начала = startDateTime,
                Дата_окончания = endDateTime,
                Тип_события = type,
                Место_проведения = string.IsNullOrWhiteSpace(place) ? null : place,
                ID_проекта = projectId,
                ID_задачи = taskId
            };

            try
            {
                DbHelper.AddEvent(newEvent);

                MessageBox.Show($"Событие успешно добавлено!\n\nНазвание: {title}\nНачало: {startDateTime:dd.MM.yyyy HH:mm}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении события:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Горячие клавиши
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter && !DescBox.IsFocused)
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