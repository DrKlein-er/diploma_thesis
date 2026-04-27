using System;
using System.Windows;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class AddBudgetWindow : Window
    {
        private int _projectId;
        private UserModel _currentUser; // Добавляем поле для пользователя

        // Конструктор окна добавления бюджета
        public AddBudgetWindow(int projectId, UserModel currentUser = null)
        {
            InitializeComponent();
            _projectId = projectId;
            _currentUser = currentUser; // Сохраняем пользователя

            // Применяем текущую тему
            ApplyTheme();

            // Фокус на первое поле
            PurposeTextBox.Focus();
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

            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(themeDict);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Получаем и очищаем данные
            string purpose = PurposeTextBox.Text.Trim();
            string amountText = AmountTextBox.Text.Trim();

            // Валидация: проверка на пустые поля
            if (string.IsNullOrWhiteSpace(purpose))
            {
                MessageBox.Show("Пожалуйста, укажите назначение расходов.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                PurposeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(amountText))
            {
                MessageBox.Show("Пожалуйста, укажите сумму.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            // Валидация: проверка корректности суммы
            if (!decimal.TryParse(amountText, out decimal amount))
            {
                MessageBox.Show("Сумма должна быть числом.\nНапример: 10000 или 10000.50",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                AmountTextBox.SelectAll();
                AmountTextBox.Focus();
                return;
            }

            // Валидация: проверка на отрицательное значение
            if (amount <= 0)
            {
                MessageBox.Show("Сумма должна быть больше нуля.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                AmountTextBox.SelectAll();
                AmountTextBox.Focus();
                return;
            }

            // Создание новой записи бюджета
            var newBudgetEntry = new BudgetModel
            {
                ID_проекта = _projectId,
                Назначение = purpose,
                Сумма = amount
            };

            try
            {
                // Добавление записи в базу данных
                DbHelper.AddBudgetEntry(newBudgetEntry);

                // ЛОГИРУЕМ ДОБАВЛЕНИЕ БЮДЖЕТА
                if (_currentUser != null)
                {
                    DbHelper.LogChange("Проект", _projectId,
                        $"Добавлена запись бюджета: \"{purpose}\" ({amount:N2} руб.)",
                        _currentUser.ID_сотрудника);
                }

                MessageBox.Show($"Запись в бюджет успешно добавлена!\n\nНазначение: {purpose}\nСумма: {amount:N2} ₽",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Закрытие окна с успешным результатом
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении записи в бюджет:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработка нажатия Enter для подтверждения
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter)
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