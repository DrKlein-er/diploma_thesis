using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditSubtaskWindow : Window
    {
        private SubtaskModel _subtask;

        public EditSubtaskWindow(SubtaskModel subtask)
        {
            InitializeComponent();
            _subtask = subtask;

            // Применяем текущую тему
            ApplyTheme();
            // Загружаем данные подзадачи
            LoadData();
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

        // Загрузка данных подзадачи в поля формы
        private void LoadData()
        {
            TitleTextBox.Text = _subtask.Название_подзадачи;
            DescTextBox.Text = _subtask.Описание;
            StartDatePicker.SelectedDate = _subtask.Дата_начала;
            EndDatePicker.SelectedDate = _subtask.Дата_завершения;

            // Устанавливаем выбранный статус
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content.ToString() == _subtask.Статус)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        // Обработчик нажатия кнопки "Сохранить"
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string title = TitleTextBox.Text.Trim();
            string description = DescTextBox.Text.Trim();
            string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
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

            // Обновление данных подзадачи
            _subtask.Название_подзадачи = title;
            _subtask.Описание = string.IsNullOrWhiteSpace(description) ? null : description;
            _subtask.Статус = status;
            _subtask.Дата_начала = startDate;
            _subtask.Дата_завершения = endDate;

            try
            {
                // Обновление подзадачи в базе данных
                DbHelper.UpdateSubtask(_subtask);

                // Формируем сообщение об успехе
                string dateInfo = "";
                if (startDate.HasValue && endDate.HasValue)
                {
                    dateInfo = $"\nПериод: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                }
                else if (endDate.HasValue)
                {
                    dateInfo = $"\nДата завершения: {endDate:dd.MM.yyyy}";
                }

                MessageBox.Show($"Подзадача успешно обновлена!\n\nНазвание: {title}\nСтатус: {status}{dateInfo}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении подзадачи:\n{ex.Message}",
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
            if (e.Key == Key.Enter && !DescTextBox.IsFocused)
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
