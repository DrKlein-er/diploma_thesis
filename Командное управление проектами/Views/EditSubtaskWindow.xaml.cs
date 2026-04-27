using System;
using System.Collections.Generic;
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
        private UserModel _currentUser;

        public EditSubtaskWindow(SubtaskModel subtask, UserModel currentUser = null)
        {
            InitializeComponent();
            _subtask = subtask;
            _currentUser = currentUser;

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

            try
            {
                // ✅ ОТСЛЕЖИВАЕМ ИЗМЕНЕНИЯ ДЛЯ ДЕТАЛЬНОГО ЛОГИРОВАНИЯ
                List<string> changes = new List<string>();

                // Проверяем изменение названия
                if (_subtask.Название_подзадачи != title)
                {
                    changes.Add($"Изменено название подзадачи: '{_subtask.Название_подзадачи}' → '{title}'");
                }

                // Проверяем изменение описания
                if (_subtask.Описание != description)
                {
                    if (string.IsNullOrEmpty(_subtask.Описание) && !string.IsNullOrEmpty(description))
                    {
                        changes.Add($"Добавлено описание к подзадаче \"{_subtask.Название_подзадачи}\"");
                    }
                    else if (!string.IsNullOrEmpty(_subtask.Описание) && string.IsNullOrEmpty(description))
                    {
                        changes.Add($"Удалено описание подзадачи \"{_subtask.Название_подзадачи}\"");
                    }
                    else
                    {
                        changes.Add($"Изменено описание подзадачи \"{_subtask.Название_подзадачи}\"");
                    }
                }

                // Проверяем изменение статуса
                if (_subtask.Статус != status)
                {
                    changes.Add($"Изменен статус подзадачи \"{_subtask.Название_подзадачи}\": '{_subtask.Статус}' → '{status}'");
                }

                // Проверяем изменение даты начала
                if (_subtask.Дата_начала != startDate)
                {
                    string oldDate = _subtask.Дата_начала?.ToString("dd.MM.yyyy") ?? "не указана";
                    string newDate = startDate?.ToString("dd.MM.yyyy") ?? "не указана";
                    changes.Add($"Изменена дата начала подзадачи \"{_subtask.Название_подзадачи}\": {oldDate} → {newDate}");
                }

                // Проверяем изменение даты завершения
                if (_subtask.Дата_завершения != endDate)
                {
                    string oldDate = _subtask.Дата_завершения?.ToString("dd.MM.yyyy") ?? "не указана";
                    string newDate = endDate?.ToString("dd.MM.yyyy") ?? "не указана";
                    changes.Add($"Изменена дата завершения подзадачи \"{_subtask.Название_подзадачи}\": {oldDate} → {newDate}");
                }

                // Обновление данных подзадачи
                _subtask.Название_подзадачи = title;
                _subtask.Описание = string.IsNullOrWhiteSpace(description) ? null : description;
                _subtask.Статус = status;
                _subtask.Дата_начала = startDate;
                _subtask.Дата_завершения = endDate;

                // Обновление подзадачи в базе данных
                DbHelper.UpdateSubtask(_subtask);

                // ✅ ЛОГИРУЕМ КАЖДОЕ ИЗМЕНЕНИЕ ОТДЕЛЬНО (только если передан пользователь)
                if (_currentUser != null && changes.Count > 0)
                {
                    foreach (var change in changes)
                    {
                        DbHelper.LogChange("Задача", _subtask.ID_задачи, change, _currentUser.ID_сотрудника);
                    }
                }

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
