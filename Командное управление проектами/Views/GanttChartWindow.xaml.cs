using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Командное_управление_проектами.Helpers;
using Командное_управление_проектами.Models;
using Командное_управление_проектами.ViewModels;

namespace Командное_управление_проектами.Views
{
    public partial class GanttChartWindow : Window
    {
        // Константа: ширина одного дня в пикселях
        private const double DayWidth = 40;

        // Сохраняем ссылки для использования в экспорте
        private TaskModel _mainTask;
        private List<GanttItem> _ganttItems;
        private DateTime _overallStartDate;
        private DateTime _overallEndDate;

        // Конструктор окна, принимает основную задачу для построения диаграммы
        public GanttChartWindow(TaskModel mainTask)
        {
            InitializeComponent();
            _mainTask = mainTask;

            // Применяем текущую тему приложения
            ApplyTheme();

            // Устанавливаем заголовок с названием задачи
            ChartTitle.Text = $"Диаграмма Ганта: {mainTask.Название_задачи}";

            // Строим диаграмму Ганта
            BuildGanttChart(mainTask);
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

        // Построение диаграммы Ганта на основе основной задачи и её подзадач
        private void BuildGanttChart(TaskModel mainTask)
        {
            _ganttItems = new List<GanttItem>();

            // Собираем основную задачу, если у неё есть даты начала и окончания
            if (mainTask.Дата_начала.HasValue && mainTask.Дата_завершения.HasValue)
            {
                _ganttItems.Add(new GanttItem
                {
                    Name = mainTask.Название_задачи,
                    StartDate = mainTask.Дата_начала.Value,
                    EndDate = mainTask.Дата_завершения.Value,
                    BarColor = GetColorByStatus(mainTask.Статус, true),
                    Status = mainTask.Статус
                });
            }

            // Собираем все подзадачи, у которых есть даты начала и окончания
            if (mainTask.Subtasks != null)
            {
                foreach (var subtask in mainTask.Subtasks)
                {
                    if (subtask.Дата_начала.HasValue && subtask.Дата_завершения.HasValue)
                    {
                        _ganttItems.Add(new GanttItem
                        {
                            Name = "  → " + subtask.Название_подзадачи,
                            StartDate = subtask.Дата_начала.Value,
                            EndDate = subtask.Дата_завершения.Value,
                            BarColor = GetColorByStatus(subtask.Статус, false),
                            Status = subtask.Статус
                        });
                    }
                }
            }

            // Валидация: если нет данных для построения диаграммы
            if (!_ganttItems.Any())
            {
                MessageBox.Show("Невозможно построить диаграмму:\n\nОтсутствуют даты начала и завершения для задачи и её подзадач.\n\nДля построения диаграммы Ганта необходимо указать даты начала и завершения.",
                    "Недостаточно данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                this.Close();
                return;
            }

            // Находим общие начальную и конечную даты для всей диаграммы
            _overallStartDate = _ganttItems.Min(i => i.StartDate).Date;
            _overallEndDate = _ganttItems.Max(i => i.EndDate).Date;

            // Генерируем шкалу дат (верхняя панель с датами)
            var dateMarkers = new List<GanttDateMarker>();
            for (DateTime date = _overallStartDate; date <= _overallEndDate; date = date.AddDays(1))
            {
                dateMarkers.Add(new GanttDateMarker
                {
                    Day = date.Day.ToString(),
                    Month = date.ToString("MMM", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"))
                });
            }
            DateScaleItemsControl.ItemsSource = dateMarkers;

            // Рассчитываем геометрию полос для каждой задачи/подзадачи
            int itemIndex = 0;
            foreach (var item in _ganttItems)
            {
                // Длительность задачи в днях
                var duration = (item.EndDate.Date - item.StartDate.Date).TotalDays + 1;

                // Смещение от начала временной шкалы в днях
                double offsetDays = (item.StartDate.Date - _overallStartDate).TotalDays;

                // Рассчитываем ширину полосы и её позицию
                item.BarWidth = duration * DayWidth;
                item.BarLeft = offsetDays * DayWidth;
                item.BarTop = (itemIndex * 35) + 6; // 35 - высота строки, 6 - отступ сверху для центрирования

                // Форматируем длительность для отображения на полосе
                item.Duration = duration == 1 ? "1 день" : $"{duration} дн.";

                // Формируем текст подсказки
                item.ToolTipText = $"{item.Name.Trim()}\n" +
                                  $"Статус: {item.Status}\n" +
                                  $"Начало: {item.StartDate:dd.MM.yyyy}\n" +
                                  $"Окончание: {item.EndDate:dd.MM.yyyy}\n" +
                                  $"Длительность: {duration} дн.";

                itemIndex++;
            }

            // Отображаем данные в соответствующих контролах
            TaskNamesItemsControl.ItemsSource = _ganttItems;
            TaskGridLinesItemsControl.ItemsSource = _ganttItems; // Для отрисовки горизонтальных линий
            GanttBarsItemsControl.ItemsSource = _ganttItems;

            // Добавляем вертикальную линию "Сегодня"
            AddTodayLine();

            // Обновляем статистику
            UpdateStatistics();
        }

        // Получение цвета полосы в зависимости от статуса
        private SolidColorBrush GetColorByStatus(string status, bool isMainTask)
        {
            Color baseColor;

            switch (status)
            {
                case "Завершена":
                    baseColor = Color.FromRgb(76, 175, 80);      // Зеленый
                    break;
                case "В процессе":
                    baseColor = Color.FromRgb(255, 152, 0);      // Оранжевый
                    break;
                case "Открыта":
                    baseColor = Color.FromRgb(158, 158, 158);    // Серый
                    break;
                default:
                    baseColor = isMainTask
                        ? Color.FromRgb(0, 122, 204)             // Синий для основной задачи
                        : Color.FromRgb(32, 178, 170);           // Бирюзовый для подзадач
                    break;
            }

            return new SolidColorBrush(baseColor);
        }

        // Добавление вертикальной линии "Сегодня" на диаграмму
        private void AddTodayLine()
        {
            var today = DateTime.Today;

            // Проверяем, попадает ли сегодняшний день в диапазон диаграммы
            if (today >= _overallStartDate && today <= _overallEndDate)
            {
                double todayOffset = (today - _overallStartDate).TotalDays * DayWidth;

                var todayLine = new Line
                {
                    X1 = todayOffset,
                    Y1 = 0,
                    X2 = todayOffset,
                    Y2 = _ganttItems.Count * 35,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 3 },
                    ToolTip = $"Сегодня: {today:dd.MM.yyyy}"
                };

                // Добавляем линию на Canvas
                GanttCanvas.Children.Add(todayLine);

                // Добавляем метку "Сегодня" над линией
                var todayLabel = new System.Windows.Controls.TextBlock
                {
                    Text = "Сегодня",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Red,
                    Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    Padding = new Thickness(3, 1, 3, 1)
                };

                Canvas.SetLeft(todayLabel, todayOffset - 20);
                Canvas.SetTop(todayLabel, -20);
                GanttCanvas.Children.Add(todayLabel);
            }
        }

        // Обновление статистики проекта
        private void UpdateStatistics()
        {
            int totalDays = (_overallEndDate - _overallStartDate).Days + 1;
            int totalTasks = _ganttItems.Count;
            int completedTasks = _ganttItems.Count(i => i.Status == "Завершена");
            int inProgressTasks = _ganttItems.Count(i => i.Status == "В процессе");
            int openTasks = _ganttItems.Count(i => i.Status == "Открыта");

            double completionPercentage = totalTasks > 0 ? (completedTasks * 100.0 / totalTasks) : 0;

            StatsText.Text = $"📊 Всего задач: {totalTasks} | " +
                           $"✅ Завершено: {completedTasks} ({completionPercentage:F0}%) | " +
                           $"🔄 В процессе: {inProgressTasks} | " +
                           $"⏸️ Открыто: {openTasks} | " +
                           $"📅 Общая длительность: {totalDays} дн.";
        }

        // Обработчик экспорта диаграммы в изображение
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG изображение (*.png)|*.png|JPEG изображение (*.jpg)|*.jpg",
                    Title = "Сохранить диаграмму Ганта",
                    FileName = $"Gantt_{_mainTask.Название_задачи}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Получаем область диаграммы для рендеринга
                    var chartArea = ChartArea;

                    // Создаем RenderTargetBitmap
                    var width = (int)chartArea.ActualWidth;
                    var height = (int)chartArea.ActualHeight;

                    if (width <= 0 || height <= 0)
                    {
                        MessageBox.Show("Невозможно экспортировать диаграмму.\nОбласть диаграммы не инициализирована.",
                            "Ошибка экспорта",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        width,
                        height,
                        96d,
                        96d,
                        PixelFormats.Pbgra32);

                    renderBitmap.Render(chartArea);

                    // Определяем кодировщик на основе выбранного расширения
                    BitmapEncoder encoder;
                    if (saveFileDialog.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        encoder = new JpegBitmapEncoder { QualityLevel = 95 };
                    }
                    else
                    {
                        encoder = new PngBitmapEncoder();
                    }

                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    // Сохраняем файл
                    using (var fileStream = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }

                    MessageBox.Show($"Диаграмма успешно экспортирована!\n\nФайл сохранен:\n{saveFileDialog.FileName}",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте диаграммы:\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработка горячих клавиш
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Escape - закрыть окно
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            // Ctrl+E - экспорт
            else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Export_Click(this, new RoutedEventArgs());
            }
        }
    }
}