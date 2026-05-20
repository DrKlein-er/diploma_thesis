using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditEventWindow : Window
    {
        private readonly EventModel _event;
        private bool _isLoading = true;

        public EditEventWindow(EventModel ev)
        {
            InitializeComponent();
            ApplyTheme();

            _event = ev ?? throw new ArgumentNullException(nameof(ev));

            LoadProjects();
            LoadEventData();

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

        // Загрузка списка проектов в ComboBox
        private void LoadProjects()
        {
            try
            {
                var projects = DbHelper.GetAllProjects();
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

        // Заполнение полей текущими значениями события
        private void LoadEventData()
        {
            TitleBox.Text = _event.Название_события;
            DescBox.Text = _event.Описание ?? string.Empty;
            PlaceBox.Text = _event.Место_проведения ?? string.Empty;

            // Тип события
            string typeToFind = string.IsNullOrWhiteSpace(_event.Тип_события) ? "Другое" : _event.Тип_события;
            foreach (var item in TypeComboBox.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Content?.ToString() == typeToFind)
                {
                    TypeComboBox.SelectedItem = item;
                    break;
                }
            }
            if (TypeComboBox.SelectedItem == null) TypeComboBox.SelectedIndex = 4; // Другое

            // Дата и время начала
            if (_event.Дата_начала.HasValue)
            {
                StartDatePicker.SelectedDate = _event.Дата_начала.Value.Date;
                StartTimeBox.Text = _event.Дата_начала.Value.ToString("HH:mm");
            }
            else
            {
                StartDatePicker.SelectedDate = DateTime.Today;
                StartTimeBox.Text = "09:00";
            }

            // Дата и время окончания
            if (_event.Дата_окончания.HasValue)
            {
                EndDatePicker.SelectedDate = _event.Дата_окончания.Value.Date;
                EndTimeBox.Text = _event.Дата_окончания.Value.ToString("HH:mm");
            }
            else
            {
                EndDatePicker.SelectedDate = null;
                EndTimeBox.Text = string.Empty;
            }

            // Проект (если задан — задачи подгрузятся в SelectionChanged)
            if (_event.ID_проекта.HasValue)
                ProjectComboBox.SelectedValue = _event.ID_проекта.Value;
            else
                ProjectComboBox.SelectedIndex = -1;
        }

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int? projectId = ProjectComboBox.SelectedValue as int?;
                if (projectId.HasValue)
                {
                    var tasks = DbHelper.GetTasksByProject(projectId.Value);
                    TaskComboBox.ItemsSource = tasks;
                    TaskComboBox.IsEnabled = true;

                    // При первой загрузке восстанавливаем выбранную задачу
                    if (_isLoading && _event.ID_задачи.HasValue)
                    {
                        TaskComboBox.SelectedValue = _event.ID_задачи.Value;
                    }
                    else
                    {
                        TaskComboBox.SelectedIndex = -1;
                    }
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

        private static bool TryParseTime(string text, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(text?.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out time)
                || TimeSpan.TryParseExact(text?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string description = DescBox.Text.Trim();
            string place = PlaceBox.Text.Trim();
            string type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Другое";

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

            if (!TryParseTime(StartTimeBox.Text, out TimeSpan startTime))
            {
                MessageBox.Show("Время начала должно быть в формате ЧЧ:ММ.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StartTimeBox.Focus();
                return;
            }

            DateTime startDateTime = startDate.Value.Date.Add(startTime);

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
                MessageBox.Show("Заполните и дату, и время окончания — либо оставьте оба пустыми.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Обновление модели
            _event.Название_события = title;
            _event.Описание = string.IsNullOrWhiteSpace(description) ? null : description;
            _event.Место_проведения = string.IsNullOrWhiteSpace(place) ? null : place;
            _event.Тип_события = type;
            _event.Дата_начала = startDateTime;
            _event.Дата_окончания = endDateTime;
            _event.ID_проекта = projectId;
            _event.ID_задачи = taskId;

            try
            {
                DbHelper.UpdateEvent(_event);

                MessageBox.Show($"Событие успешно обновлено!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении события:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter && !DescBox.IsFocused)
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