using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;

namespace Командное_управление_проектами.Views
{
    public partial class AddUserWindow : Window
    {
        private readonly string connectionString = "Data Source=DESKTOP-JRVC3AP;Initial Catalog=Coursework;Integrated Security=True";

        public AddUserWindow()
        {
            InitializeComponent();
            // Применяем текущую тему
            ApplyTheme();
            // Загружаем список отделов
            LoadDepartments();
            // Устанавливаем фокус на первое поле
            LastNameBox.Focus();
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

        // Загрузка списка отделов в ComboBox
        private void LoadDepartments()
        {
            DepartmentComboBox.Items.Add("IT");
            DepartmentComboBox.Items.Add("Разработка");
            DepartmentComboBox.Items.Add("Дизайн");
            DepartmentComboBox.Items.Add("Тестирование");
            DepartmentComboBox.Items.Add("Маркетинг");
            DepartmentComboBox.Items.Add("Продажи");
            DepartmentComboBox.SelectedIndex = 0;
        }

        // Обработчик нажатия кнопки "Добавить"
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из полей
            string lastName = LastNameBox.Text.Trim();
            string firstName = FirstNameBox.Text.Trim();
            string middleName = MiddleNameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string department = DepartmentComboBox.SelectedItem?.ToString();
            string role = (RoleBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Валидация: проверка обязательных полей
            if (string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Пожалуйста, введите фамилию.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                LastNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                MessageBox.Show("Пожалуйста, введите имя.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                FirstNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Пожалуйста, введите email.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EmailBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, введите пароль.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            // Валидация: корректность Email
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Введите корректный Email адрес.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EmailBox.Focus();
                return;
            }

            // Валидация: требования безопасности пароля
            if (!IsValidPassword(password))
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов:\n" +
                               "• Заглавную букву\n" +
                               "• Строчную букву\n" +
                               "• Цифру\n" +
                               "• Специальный символ",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            // Проверка уникальности email
            var existingUserByEmail = DbHelper.GetUserByEmail(email);

            if (existingUserByEmail != null)
            {
                MessageBox.Show("Пользователь с таким Email уже существует.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                EmailBox.Focus();
                return;
            }

            // Хэширование пароля
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int newEmployeeId;

                        // 1. Создаем сотрудника
                        string queryEmployee = @"
                            INSERT INTO Сотрудники (Фамилия, Имя, Отчество, Отдел, ID_роли)
                            VALUES (@lastName, @firstName, @middleName, @department, 
                                   (SELECT ID_роли FROM Роли WHERE Название_роли = @role));
                            SELECT SCOPE_IDENTITY();";

                        using (SqlCommand cmd = new SqlCommand(queryEmployee, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@lastName", lastName);
                            cmd.Parameters.AddWithValue("@firstName", firstName);
                            cmd.Parameters.AddWithValue("@middleName",
                                string.IsNullOrWhiteSpace(middleName) ? (object)DBNull.Value : middleName);
                            cmd.Parameters.AddWithValue("@department",
                                string.IsNullOrWhiteSpace(department) ? "Не указан" : department);
                            cmd.Parameters.AddWithValue("@role", role);
                            newEmployeeId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Создаем пользователя (логин = email)
                        string queryUser = @"
                            INSERT INTO Пользователи (Логин, Пароль, Email, ID_сотрудника)
                            VALUES (@email, @password, @email, @employeeId)";

                        using (SqlCommand cmd = new SqlCommand(queryUser, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@email", email);
                            cmd.Parameters.AddWithValue("@password", hashedPassword);
                            cmd.Parameters.AddWithValue("@employeeId", newEmployeeId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show($"Пользователь успешно добавлен!\n\n" +
                                      $"ФИО: {lastName} {firstName} {middleName}\n" +
                                      $"Email: {email}\n" +
                                      $"Роль: {role}",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Закрытие окна с успешным результатом
                        this.DialogResult = true;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка при добавлении пользователя:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        // Проверка корректности Email
        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        // Проверка требований безопасности пароля
        private bool IsValidPassword(string password)
        {
            // Минимум 8 символов
            if (password.Length < 8)
                return false;
            // Хотя бы одна заглавная буква
            if (!Regex.IsMatch(password, @"[A-Z]"))
                return false;
            // Хотя бы одна строчная буква
            if (!Regex.IsMatch(password, @"[a-z]"))
                return false;
            // Хотя бы одна цифра
            if (!Regex.IsMatch(password, @"\d"))
                return false;
            // Хотя бы один специальный символ
            if (!Regex.IsMatch(password, @"[\W_]"))
                return false;
            return true;
        }

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Enter - добавить пользователя
            if (e.Key == Key.Enter)
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