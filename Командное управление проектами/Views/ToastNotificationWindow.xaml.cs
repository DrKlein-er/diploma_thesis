using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Командное_управление_проектами.Helpers;

namespace Командное_управление_проектами.Views
{
    // Всплывающее уведомление (Toast), отображаемое в правом нижнем углу экрана
    public partial class ToastNotificationWindow : Window
    {
        private DispatcherTimer _timer;
        private const int DisplayDurationSeconds = 5; // Длительность показа уведомления в секундах

        // Конструктор окна уведомления
        public ToastNotificationWindow(string title, string message, string icon, string priority)
        {
            InitializeComponent();

            // Применяем текущую тему
            ApplyTheme();

            // Устанавливаем содержимое уведомления
            TitleText.Text = title;
            MessageText.Text = message;
            IconText.Text = icon;

            // Устанавливаем цвет рамки в зависимости от приоритета
            SetBorderColorByPriority(priority);

            // Позиционируем окно в правом нижнем углу экрана
            PositionWindow();

            // Анимация плавного появления
            AnimateFadeIn();

            // Запускаем таймер для автоматического закрытия
            StartAutoCloseTimer();
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

        // Установка цвета рамки в зависимости от приоритета уведомления
        private void SetBorderColorByPriority(string priority)
        {
            Color borderColor;

            switch (priority)
            {
                case "Высокий":
                    borderColor = Color.FromRgb(244, 67, 54); // Красный
                    break;
                case "Средний":
                    borderColor = Color.FromRgb(255, 152, 0); // Оранжевый
                    break;
                case "Низкий":
                    borderColor = Color.FromRgb(76, 175, 80); // Зелёный
                    break;
                default:
                    borderColor = Color.FromRgb(33, 150, 243); // Синий (по умолчанию)
                    break;
            }

            MainBorder.BorderBrush = new SolidColorBrush(borderColor);
        }

        // Позиционирование окна в правом нижнем углу экрана
        // С учётом других открытых уведомлений
        private void PositionWindow()
        {
            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 20;
            this.Top = workingArea.Bottom - this.Height - 20;

            // Проверяем наличие других Toast-окон и сдвигаем вверх
            var offset = 0;
            foreach (Window window in Application.Current.Windows)
            {
                if (window != this && window is ToastNotificationWindow)
                {
                    offset += 130; // Высота окна + отступ
                }
            }
            this.Top -= offset;
        }

        // Анимация плавного появления окна
        private void AnimateFadeIn()
        {
            this.Opacity = 0;
            var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            this.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
        }

        // Запуск таймера для автоматического закрытия уведомления
        private void StartAutoCloseTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(DisplayDurationSeconds);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        // Обработчик события таймера - закрытие окна по истечению времени
        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            CloseWithAnimation();
        }

        // Обработчик нажатия кнопки закрытия
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            CloseWithAnimation();
        }

        // Закрытие окна с анимацией плавного исчезновения
        private void CloseWithAnimation()
        {
            var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            fadeOutAnimation.Completed += (s, e) => this.Close();
            this.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        }

        // Обработка наведения мыши на уведомление
        // Останавливаем таймер, чтобы пользователь успел прочитать
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _timer?.Stop();
        }

        // Обработка ухода мыши с уведомления
        // Возобновляем таймер автоматического закрытия
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _timer?.Start();
        }
    }
}
