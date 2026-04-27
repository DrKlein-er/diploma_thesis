using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Командное_управление_проектами.Helpers;

namespace Командное_управление_проектами.Views
{
    /// <summary>
    /// Всплывающее уведомление (Toast), отображаемое в правом нижнем углу экрана
    /// Автоматически закрывается через 5 секунд
    /// </summary>
    public partial class ToastNotificationWindow : Window
    {
        private DispatcherTimer _timer;
        private const int DisplayDurationSeconds = 5; // Длительность показа уведомления в секундах

        /// <summary>
        /// Конструктор окна уведомления
        /// </summary>
        /// <param name="title">Заголовок уведомления</param>
        /// <param name="message">Текст сообщения</param>
        /// <param name="icon">Иконка уведомления (emoji)</param>
        /// <param name="priority">Приоритет уведомления (Высокий, Средний, Низкий)</param>
        public ToastNotificationWindow(string title, string message, string icon, string priority)
        {
            InitializeComponent();

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

        /// <summary>
        /// Установка цвета рамки в зависимости от приоритета уведомления
        /// </summary>
        /// <param name="priority">Приоритет: Высокий, Средний, Низкий</param>
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

        /// <summary>
        /// Позиционирование окна в правом нижнем углу экрана
        /// С учётом других открытых уведомлений для правильного расположения
        /// </summary>
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

        /// <summary>
        /// Анимация плавного появления окна (fade in)
        /// </summary>
        private void AnimateFadeIn()
        {
            this.Opacity = 0;
            var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            this.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
        }

        /// <summary>
        /// Запуск таймера для автоматического закрытия уведомления
        /// </summary>
        private void StartAutoCloseTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(DisplayDurationSeconds);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        /// <summary>
        /// Обработчик события таймера - закрытие окна по истечению времени
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            CloseWithAnimation();
        }

        /// <summary>
        /// Обработчик нажатия кнопки закрытия
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            CloseWithAnimation();
        }

        /// <summary>
        /// Закрытие окна с анимацией плавного исчезновения (fade out)
        /// </summary>
        private void CloseWithAnimation()
        {
            var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            fadeOutAnimation.Completed += (s, e) => this.Close();
            this.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        }

        /// <summary>
        /// Обработка наведения мыши на уведомление
        /// Останавливаем таймер, чтобы пользователь успел прочитать сообщение
        /// </summary>
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _timer?.Stop();
        }

        /// <summary>
        /// Обработка ухода мыши с уведомления
        /// Возобновляем таймер автоматического закрытия
        /// </summary>
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _timer?.Start();
        }
    }
}