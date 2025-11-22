using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Командное_управление_проектами.Helpers;

namespace Командное_управление_проектами.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly string connectionString = "Data Source=DESKTOP-JRVC3AP;Initial Catalog=Coursework;Integrated Security=True";

        public RegisterWindow()
        {
            InitializeComponent();
            PasswordBox_PasswordChanged(null, null);
        }

        // Проверка уникальности Email
        private bool IsEmailUnique(string email)
        {
            var existingUser = DbHelper.GetUserByEmail(email);
            return existingUser == null;
        }

        // Обработчик изменения пароля с визуальной индикацией требований
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = PasswordBox.Password;

            // Проверка длины (минимум 8 символов)
            bool lengthValid = password.Length >= 8;
            LengthRequirement.Foreground = lengthValid ? Brushes.Green : Brushes.Gray;
            LengthRequirement.Text = lengthValid ? "✔ Минимум 8 символов" : "• Минимум 8 символов";

            // Проверка заглавной буквы
            bool upperValid = Regex.IsMatch(password, @"[A-Z]");
            UpperRequirement.Foreground = upperValid ? Brushes.Green : Brushes.Gray;
            UpperRequirement.Text = upperValid ? "✔ Одна заглавная буква (A-Z)" : "• Одна заглавная буква (A-Z)";

            // Проверка строчной буквы
            bool lowerValid = Regex.IsMatch(password, @"[a-z]");
            LowerRequirement.Foreground = lowerValid ? Brushes.Green : Brushes.Gray;
            LowerRequirement.Text = lowerValid ? "✔ Одна строчная буква (a-z)" : "• Одна строчная буква (a-z)";

            // Проверка цифры
            bool digitValid = Regex.IsMatch(password, @"\d");
            DigitRequirement.Foreground = digitValid ? Brushes.Green : Brushes.Gray;
            DigitRequirement.Text = digitValid ? "✔ Одна цифра (0-9)" : "• Одна цифра (0-9)";

            // Проверка спецсимвола
            bool symbolValid = Regex.IsMatch(password, @"[\W_]");
            SymbolRequirement.Foreground = symbolValid ? Brushes.Green : Brushes.Gray;
            SymbolRequirement.Text = symbolValid ? "✔ Один специальный символ (!, @, #...)" : "• Один специальный символ (!, @, #...)";

            // Активируем кнопку, только если все требования выполнены
            RegisterButton.IsEnabled = lengthValid && upperValid && lowerValid && digitValid && symbolValid;
        }

        // Обработчик нажатия кнопки "Зарегистрироваться"
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string lastName = LastNameBox.Text.Trim();
            string firstName = FirstNameBox.Text.Trim();
            string middleName = MiddleNameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Валидация: проверка заполненности обязательных полей
            if (string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Заполните все обязательные поля.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Валидация: проверка корректности Email
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Введите корректный Email.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EmailBox.Focus();
                return;
            }

            // Валидация: проверка совпадения паролей
            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return;
            }

            // Валидация: проверка требований к паролю
            if (!IsValidPassword(password))
            {
                MessageBox.Show("Пароль не соответствует требованиям безопасности.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            // Валидация: проверка уникальности Email
            if (!IsEmailUnique(email))
            {
                MessageBox.Show("Пользователь с таким Email уже существует.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EmailBox.Focus();
                return;
            }

            // Хэширование пароля с использованием BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int newEmployeeId;

                        // 1. Сначала создаем запись сотрудника
                        string queryEmployee = @"
                        INSERT INTO Сотрудники (Фамилия, Имя, Отчество, Отдел, ID_роли)
                        VALUES (@lastName, @firstName, @middleName, @department, 
                               (SELECT ID_роли FROM Роли WHERE Название_роли = 'Пользователь'));
                        SELECT SCOPE_IDENTITY();";

                        using (SqlCommand cmd = new SqlCommand(queryEmployee, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@lastName", lastName);
                            cmd.Parameters.AddWithValue("@firstName", firstName);
                            cmd.Parameters.AddWithValue("@middleName", string.IsNullOrEmpty(middleName) ? (object)DBNull.Value : middleName);
                            cmd.Parameters.AddWithValue("@department", "Не указан");
                            newEmployeeId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Затем создаем пользователя (БЕЗ ПОЛЯ ЛОГИН)
                        string queryUser = @"
                        INSERT INTO Пользователи (Пароль, Email, ID_сотрудника)
                        VALUES (@password, @email, @employeeId)";

                        using (SqlCommand cmd = new SqlCommand(queryUser, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@password", hashedPassword);
                            cmd.Parameters.AddWithValue("@email", email);
                            cmd.Parameters.AddWithValue("@employeeId", newEmployeeId);
                            cmd.ExecuteNonQuery();
                        }

                        // Фиксируем транзакцию
                        transaction.Commit();

                        MessageBox.Show($"Пользователь успешно зарегистрирован!\n\nEmail: {email}",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        // Откатываем транзакцию при ошибке
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка при регистрации:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        // Проверка валидности Email с помощью регулярного выражения
        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        // Проверка валидности пароля по всем требованиям безопасности
        private bool IsValidPassword(string password)
        {
            if (password.Length < 8)
                return false;
            if (!Regex.IsMatch(password, @"[A-Z]"))
                return false;
            if (!Regex.IsMatch(password, @"[a-z]"))
                return false;
            if (!Regex.IsMatch(password, @"\d"))
                return false;
            if (!Regex.IsMatch(password, @"[\W_]"))
                return false;
            return true;
        }
    }
}