using System;
using System.Windows;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditEventWindow : Window
    {
        private EventModel _event;

        public EditEventWindow(EventModel ev)
        {
            InitializeComponent();
            _event = ev;

            // Применяем текущую тему
            ApplyTheme();
            // Загружаем список проектов
            LoadProjects();
            // Заполняем поля данными события
            LoadEventData();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Загрузка данных события в поля формы
        private void LoadEventData()
        {
            TitleBox.Text = _event.Название_события;
            DescBox.Text = _event.Описание;
            EventDatePicker.SelectedDate = _event.Дата_события;
            ProjectComboBox.SelectedValue = _event.ID_проекта;
        }

        // Обработчик нажатия кнопки "Сохранить"
        private void Save_Click(object sender, RoutedEventArgs e)
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

            // Обновляем данные события
            _event.Название_события = title;
            _event.Описание = string.IsNullOrWhiteSpace(description) ? null : description;
            _event.Дата_события = eventDate;
            _event.ID_проекта = projectId;

            try
            {
                // Обновление события в базе данных
                DbHelper.UpdateEvent(_event);

                // Формируем сообщение об успехе
                string projectInfo = projectId.HasValue
                    ? $"\nПроект: {(ProjectComboBox.SelectedItem as ProjectModel)?.Название_проекта}"
                    : "";

                MessageBox.Show($"Событие успешно обновлено!\n\nНазвание: {title}\nДата: {eventDate:dd.MM.yyyy}{projectInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
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

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Enter - сохранить изменения (если фокус не на многострочном текстовом поле)
            if (e.Key == Key.Enter && !DescBox.IsFocused)
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