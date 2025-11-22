using System;
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
            // Применяем текущую тему
            ApplyTheme();
            // Устанавливаем дату по умолчанию на сегодня
            EventDatePicker.SelectedDate = DateTime.Today;
            // Загружаем список проектов
            LoadProjects();
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
                var projects = DbHelper.GetAllProjects();
                ProjectComboBox.ItemsSource = projects;
                ProjectComboBox.SelectedIndex = -1; // Ничего не выбрано по умолчанию
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработчик нажатия кнопки "Добавить"
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string title = TitleBox.Text.Trim();
            string description = DescBox.Text.Trim();
            DateTime? eventDate = EventDatePicker.SelectedDate;
            int? projectId = ProjectComboBox.SelectedValue as int?;

            // Валидация: название обязательно
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Пожалуйста, введите название события.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleBox.Focus();
                return;
            }

            // Валидация: дата обязательна
            if (!eventDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату события.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EventDatePicker.Focus();
                return;
            }

            // Создание нового события
            EventModel newEvent = new EventModel
            {
                Название_события = title,
                Описание = string.IsNullOrWhiteSpace(description) ? null : description,
                Дата_события = eventDate,
                ID_проекта = projectId
            };

            try
            {
                // Добавление события в базу данных
                DbHelper.AddEvent(newEvent);

                // Формируем сообщение об успехе
                string projectInfo = projectId.HasValue
                    ? $"\nПроект: {(ProjectComboBox.SelectedItem as ProjectModel)?.Название_проекта}"
                    : "";

                MessageBox.Show($"Событие успешно добавлено!\n\nНазвание: {title}\nДата: {eventDate:dd.MM.yyyy}{projectInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
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

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Enter - добавить событие (если фокус не на многострочном текстовом поле)
            if (e.Key == Key.Enter && !DescBox.IsFocused)
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