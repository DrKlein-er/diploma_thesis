using System;
using System.Windows;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class AddSubtaskWindow : Window
    {
        private readonly int _parentTaskId;

        // Конструктор окна, принимает ID родительской задачи
        public AddSubtaskWindow(int parentTaskId)
        {
            InitializeComponent();
            _parentTaskId = parentTaskId;

            // Применяем текущую тему приложения
            ApplyTheme();

            // Устанавливаем фокус на первое поле
            TitleTextBox.Focus();
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

        // Обработчик нажатия кнопки "Добавить подзадачу"
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string title = TitleTextBox.Text.Trim();
            string description = DescTextBox.Text.Trim();
            string status = (StatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            // Валидация: название обязательно
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Пожалуйста, введите название подзадачи.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            // Валидация: минимальная длина названия
            if (title.Length < 3)
            {
                MessageBox.Show("Название подзадачи должно содержать минимум 3 символа.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            // Валидация: статус обязателен
            if (string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Пожалуйста, выберите статус подзадачи.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StatusComboBox.Focus();
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

            // Создание новой подзадачи
            SubtaskModel newSubtask = new SubtaskModel
            {
                Название_подзадачи = title,
                Описание = description,
                Статус = status,
                Дата_начала = startDate,
                Дата_завершения = endDate,
                ID_задачи = _parentTaskId
            };

            try
            {
                // Добавление подзадачи в базу данных
                DbHelper.AddSubtask(newSubtask);

                // Формируем информацию о датах для сообщения
                string dateInfo = "";
                if (startDate.HasValue && endDate.HasValue)
                {
                    dateInfo = $"\nПериод: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                }
                else if (startDate.HasValue)
                {
                    dateInfo = $"\nДата начала: {startDate:dd.MM.yyyy}";
                }
                else if (endDate.HasValue)
                {
                    dateInfo = $"\nДата завершения: {endDate:dd.MM.yyyy}";
                }

                MessageBox.Show($"Подзадача успешно добавлена!\n\nНазвание: {title}\nСтатус: {status}{dateInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении подзадачи:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+Enter - добавить подзадачу (даже из многострочного поля)
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
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