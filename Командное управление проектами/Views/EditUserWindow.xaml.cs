using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Views
{
    public partial class EditUserWindow : Window
    {
        private readonly string connectionString = "Data Source=DESKTOP-JRVC3AP;Initial Catalog=Coursework;Integrated Security=True";
        private UserModel _user;

        public EditUserWindow(UserModel user)
        {
            InitializeComponent();
            _user = user;

            // Применяем текущую тему
            ApplyTheme();
            // Загружаем список отделов
            LoadDepartments();
            // Заполняем поля данными пользователя
            FillUserData();
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
        }

        // Заполнение полей данными пользователя
        private void FillUserData()
        {
            LastNameBox.Text = _user.Фамилия;
            FirstNameBox.Text = _user.Имя;
            MiddleNameBox.Text = _user.Отчество;
            EmailBox.Text = _user.Email;

            // Получаем отдел сотрудника из базы данных
            string department = GetEmployeeDepartment(_user.ID_сотрудника);
            if (!string.IsNullOrEmpty(department))
            {
                DepartmentComboBox.SelectedItem = department;
            }

            // Устанавливаем роль
            foreach (ComboBoxItem item in RoleBox.Items)
            {
                if (item.Content.ToString() == _user.Роль)
                {
                    RoleBox.SelectedItem = item;
                    break;
                }
            }
        }

        // Получение отдела сотрудника из базы данных
        private string GetEmployeeDepartment(int employeeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Отдел FROM Сотрудники WHERE ID_сотрудника = @employeeId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@employeeId", employeeId);
                        var result = cmd.ExecuteScalar();
                        return result?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении отдела:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return string.Empty;
            }
        }

        // Обработчик нажатия кнопки "Сохранить"
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

            // Валидация: формат email
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Пожалуйста, введите корректный email адрес.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                EmailBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(department))
            {
                MessageBox.Show("Пожалуйста, выберите отдел.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                DepartmentComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show("Пожалуйста, выберите роль.",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                RoleBox.Focus();
                return;
            }

            // Валидация: если введён новый пароль, проверяем его сложность
            if (!string.IsNullOrWhiteSpace(password) && !IsValidPassword(password))
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов:\n" +
                               "• Хотя бы одну заглавную букву (A-Z)\n" +
                               "• Хотя бы одну строчную букву (a-z)\n" +
                               "• Хотя бы одну цифру (0-9)\n" +
                               "• Хотя бы один специальный символ (!@#$%^&* и т.д.)",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Обновляем данные сотрудника (БЕЗ Email - его нет в таблице Сотрудники)
                            string updateEmployeeQuery = @"
                                UPDATE Сотрудники 
                                SET Фамилия = @lastName, 
                                    Имя = @firstName, 
                                    Отчество = @middleName, 
                                    Отдел = @department
                                WHERE ID_сотрудника = @employeeId";

                            using (SqlCommand cmd = new SqlCommand(updateEmployeeQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@lastName", lastName);
                                cmd.Parameters.AddWithValue("@firstName", firstName);
                                cmd.Parameters.AddWithValue("@middleName", string.IsNullOrWhiteSpace(middleName) ? (object)DBNull.Value : middleName);
                                cmd.Parameters.AddWithValue("@department", department);
                                cmd.Parameters.AddWithValue("@employeeId", _user.ID_сотрудника);
                                cmd.ExecuteNonQuery();
                            }

                            // 2. Получаем ID_роли по названию роли
                            int roleId = GetRoleIdByName(role, conn, transaction);

                            // 3. Обновляем ID_роли в таблице Сотрудники
                            string updateEmployeeRoleQuery = @"
                                UPDATE Сотрудники 
                                SET ID_роли = @roleId
                                WHERE ID_сотрудника = @employeeId";

                            using (SqlCommand cmd = new SqlCommand(updateEmployeeRoleQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@roleId", roleId);
                                cmd.Parameters.AddWithValue("@employeeId", _user.ID_сотрудника);
                                cmd.ExecuteNonQuery();
                            }

                            // 4. Обновляем данные пользователя (Email и пароль - в таблице Пользователи)
                            string updateUserQuery = @"
                                UPDATE Пользователи 
                                SET Email = @email, 
                                    Логин = @email" +
                                (!string.IsNullOrWhiteSpace(password) ? ", Пароль = @password" : "") +
                                " WHERE ID_сотрудника = @employeeId";

                            using (SqlCommand cmd = new SqlCommand(updateUserQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@employeeId", _user.ID_сотрудника);

                                // Если введён новый пароль, хешируем его
                                if (!string.IsNullOrWhiteSpace(password))
                                {
                                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                                    cmd.Parameters.AddWithValue("@password", hashedPassword);
                                }

                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            // Формируем сообщение об успехе
                            string passwordInfo = !string.IsNullOrWhiteSpace(password)
                                ? "\n\n✓ Пароль успешно обновлён"
                                : "";

                            MessageBox.Show($"Данные пользователя успешно обновлены!\n\n" +
                                          $"Имя: {firstName} {lastName}\n" +
                                          $"Email: {email}\n" +
                                          $"Отдел: {department}\n" +
                                          $"Роль: {role}{passwordInfo}",
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
                            MessageBox.Show($"Ошибка при обновлении данных:\n{ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Получение ID роли по её названию
        private int GetRoleIdByName(string roleName, SqlConnection conn, SqlTransaction transaction)
        {
            string query = "SELECT ID_роли FROM Роли WHERE Название_роли = @roleName";
            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@roleName", roleName);
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                throw new Exception($"Роль '{roleName}' не найдена в базе данных.");
            }
        }

        // Валидация формата email
        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        // Валидация сложности пароля
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

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Enter - сохранить изменения (если фокус не на PasswordBox)
            if (e.Key == Key.Enter && !PasswordBox.IsFocused)
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