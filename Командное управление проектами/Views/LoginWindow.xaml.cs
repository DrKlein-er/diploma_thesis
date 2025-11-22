using System.Windows;
using System.Windows.Controls;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Views;
using System.Text.RegularExpressions;

// Пользователь:    nikitaovPolz@gmail.com  LipionNik1346.
// Админ:           nikitaov362@gmail.com  LipionNik1346.
// Менеджер:        nikitaovMeng@gmail.com  LipionNik1346.

namespace Командное_управление_проектами
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите Email и пароль.");
                return;
            }

            // Проверка формата Email
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Введите корректный Email.");
                return;
            }

            var user = DbHelper.GetUserByEmail(email);
            if (user == null)
            {
                MessageBox.Show("Пользователь не найден");
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.Пароль))
            {
                MessageBox.Show("Неверный пароль");
                return;
            }

            // Создаем сессию
            string sessionId = SessionManager.CreateSession(user);

            // Формируем приветствие с ФИО
            string greeting = $"Добро пожаловать, {user.Фамилия} {user.Имя}";
            if (!string.IsNullOrEmpty(user.Отчество))
            {
                greeting += $" {user.Отчество}";
            }
            greeting += $" ({user.Роль})";

            MessageBox.Show(greeting);

            // Создаем MainWindow
            MainWindow main = new MainWindow(user);
            main.Show();
            this.Close();
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }
        private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            VisiblePasswordBox.Text = PasswordBox.Password;
            VisiblePasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }
        private void ShowPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Password = VisiblePasswordBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            VisiblePasswordBox.Visibility = Visibility.Collapsed;
        }
    }
}
