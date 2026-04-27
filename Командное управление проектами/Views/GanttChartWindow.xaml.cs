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
    /// <summary>
    /// Окно отображения диаграммы Ганта для визуализации задач и подзадач проекта
    /// </summary>
    public partial class GanttChartWindow : Window
    {
        // Константа: ширина одного дня в пикселях на диаграмме
        private const double DayWidth = 40;

        // Сохраняем ссылки для использования в экспорте и построении диаграммы
        private TaskModel _mainTask;
        private List<GanttItem> _ganttItems;
        private DateTime _overallStartDate;
        private DateTime _overallEndDate;

        /// <summary>
        /// Конструктор окна диаграммы Ганта
        /// </summary>
        /// <param name="mainTask">Основная задача для построения диаграммы</param>
        public GanttChartWindow(TaskModel mainTask)
        {
            InitializeComponent();
            _mainTask = mainTask;

            // Устанавливаем заголовок с названием задачи и проекта
            ChartTitle.Text = $"Диаграмма Ганта: {mainTask.Название_задачи}";

            // Строим диаграмму Ганта
            BuildGanttChart(mainTask);
        }

        /// <summary>
        /// Построение диаграммы Ганта на основе основной задачи и её подзадач
        /// </summary>
        /// <param name="mainTask">Основная задача</param>
        private void BuildGanttChart(TaskModel mainTask)
        {
            _ganttItems = new List<GanttItem>();

            System.Diagnostics.Debug.WriteLine("=== НАЧАЛО ПОСТРОЕНИЯ ДИАГРАММЫ ===");
            System.Diagnostics.Debug.WriteLine($"Основная задача: {mainTask.Название_задачи}");
            System.Diagnostics.Debug.WriteLine($"Даты: {mainTask.Дата_начала} - {mainTask.Дата_завершения}");
            System.Diagnostics.Debug.WriteLine($"Подзадач в задаче: {mainTask.Subtasks?.Count ?? 0}");

            // 1. Добавляем основную задачу на первую строку
            if (mainTask.Дата_начала.HasValue && mainTask.Дата_завершения.HasValue)
            {
                var mainItem = new GanttItem
                {
                    Name = mainTask.Название_задачи,
                    StartDate = mainTask.Дата_начала.Value,
                    EndDate = mainTask.Дата_завершения.Value,
                    BarColor = GetColorByStatus(mainTask.Статус, true),
                    Status = mainTask.Статус
                };
                _ganttItems.Add(mainItem);
                System.Diagnostics.Debug.WriteLine($"✓ Добавлена основная задача");
            }

            // 2. Добавляем подзадачи на отдельные строки
            if (mainTask.Subtasks != null && mainTask.Subtasks.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Обработка {mainTask.Subtasks.Count} подзадач:");
                foreach (var subtask in mainTask.Subtasks)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {subtask.Название_подзадачи}");
                    System.Diagnostics.Debug.WriteLine($"    Даты: {subtask.Дата_начала} - {subtask.Дата_завершения}");

                    // Добавляем только подзадачи с корректными датами
                    if (subtask.Дата_начала.HasValue && subtask.Дата_завершения.HasValue)
                    {
                        var subtaskItem = new GanttItem
                        {
                            Name = "  → " + subtask.Название_подзадачи,
                            StartDate = subtask.Дата_начала.Value,
                            EndDate = subtask.Дата_завершения.Value,
                            BarColor = GetColorByStatus(subtask.Статус, false),
                            Status = subtask.Статус
                        };
                        _ganttItems.Add(subtaskItem);
                        System.Diagnostics.Debug.WriteLine($"    ✓ Добавлена");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    ✗ Пропущена (нет дат)");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Всего элементов в _ganttItems: {_ganttItems.Count}");

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

            System.Diagnostics.Debug.WriteLine($"Временной диапазон: {_overallStartDate:dd.MM.yyyy} - {_overallEndDate:dd.MM.yyyy}");

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

            // 3. Рассчитываем геометрию полос - КРИТИЧЕСКИ ВАЖНО
            System.Diagnostics.Debug.WriteLine("=== РАСЧЕТ ПОЗИЦИЙ ПОЛОС ===");
            for (int i = 0; i < _ganttItems.Count; i++)
            {
                var item = _ganttItems[i];

                // Длительность задачи в днях
                var duration = (item.EndDate.Date - item.StartDate.Date).TotalDays + 1;

                // Смещение от начала временной шкалы в днях
                double offsetDays = (item.StartDate.Date - _overallStartDate).TotalDays;

                // Рассчитываем ширину полосы и её позицию
                item.BarWidth = duration * DayWidth;
                item.BarLeft = offsetDays * DayWidth;

                // КРИТИЧНО: Каждая задача должна быть на своей строке
                item.BarTop = (i * 35) + 6;

                System.Diagnostics.Debug.WriteLine($"[{i}] {item.Name}");
                System.Diagnostics.Debug.WriteLine($"    BarTop={item.BarTop}, BarLeft={item.BarLeft}, BarWidth={item.BarWidth}");

                // Форматируем длительность для отображения на полосе
                item.Duration = duration == 1 ? "1 день" : $"{duration} дн.";

                // Формируем текст подсказки
                item.ToolTipText = $"{item.Name.Trim()}\n" +
                                  $"Статус: {item.Status}\n" +
                                  $"Начало: {item.StartDate:dd.MM.yyyy}\n" +
                                  $"Окончание: {item.EndDate:dd.MM.yyyy}\n" +
                                  $"Длительность: {duration} дн.";
            }

            System.Diagnostics.Debug.WriteLine($"=== ПРИВЯЗКА К UI ===");
            // Отображаем данные в соответствующих контролах
            TaskNamesItemsControl.ItemsSource = _ganttItems;
            System.Diagnostics.Debug.WriteLine($"TaskNamesItemsControl.ItemsSource установлен ({_ganttItems.Count} элементов)");

            TaskGridLinesItemsControl.ItemsSource = _ganttItems;
            System.Diagnostics.Debug.WriteLine($"TaskGridLinesItemsControl.ItemsSource установлен ({_ganttItems.Count} элементов)");

            GanttBarsItemsControl.ItemsSource = _ganttItems;
            System.Diagnostics.Debug.WriteLine($"GanttBarsItemsControl.ItemsSource установлен ({_ganttItems.Count} элементов)");

            // Добавляем вертикальную линию "Сегодня"
            AddTodayLine();

            // Обновляем статистику
            UpdateStatistics();

            System.Diagnostics.Debug.WriteLine("=== ПОСТРОЕНИЕ ЗАВЕРШЕНО ===");
        }

        /// <summary>
        /// Получение цвета полосы диаграммы в зависимости от статуса задачи
        /// </summary>
        private SolidColorBrush GetColorByStatus(string status, bool isMainTask)
        {
            Color baseColor;

            switch (status)
            {
                case "Завершена":
                    baseColor = Color.FromRgb(76, 175, 80);
                    break;
                case "В процессе":
                    baseColor = Color.FromRgb(255, 152, 0);
                    break;
                case "Открыта":
                    baseColor = Color.FromRgb(158, 158, 158);
                    break;
                default:
                    baseColor = isMainTask
                        ? Color.FromRgb(0, 122, 204)
                        : Color.FromRgb(32, 178, 170);
                    break;
            }

            return new SolidColorBrush(baseColor);
        }

        /// <summary>
        /// Добавление вертикальной линии "Сегодня" на диаграмму
        /// </summary>
        private void AddTodayLine()
        {
            var today = DateTime.Today;

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

                GanttCanvas.Children.Add(todayLine);

                var todayLabel = new TextBlock
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

        /// <summary>
        /// Обновление панели статистики проекта
        /// </summary>
        private void UpdateStatistics()
        {
            int totalDays = (_overallEndDate - _overallStartDate).Days + 1;
            int totalTasks = _ganttItems.Count;
            int completedTasks = _ganttItems.Count(i => i.Status == "Завершена");
            int inProgressTasks = _ganttItems.Count(i => i.Status == "В процессе");
            int openTasks = _ganttItems.Count(i => i.Status == "Открыта");

            double completionPercentage = totalTasks > 0 ? (completedTasks * 100.0 / totalTasks) : 0;

            StatsText.Text = $" Всего задач: {totalTasks} | " +
                           $" Завершено: {completedTasks} ({completionPercentage:F0}%) | " +
                           $" В процессе: {inProgressTasks} | " +
                           $" Открыто: {openTasks} | " +
                           $" Общая длительность: {totalDays} дн.";
        }

        /// <summary>
        /// Обработчик экспорта диаграммы в изображение
        /// </summary>
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
                    var mainGrid = (Grid)this.Content;
                    var border = (Border)mainGrid.Children[0];

                    var width = (int)border.ActualWidth;
                    var height = (int)border.ActualHeight;

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

                    renderBitmap.Render(border);

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

        /// <summary>
        /// Обработчик события прокрутки для синхронизации левой панели
        /// </summary>
        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                var margin = new Thickness(0, -e.VerticalOffset, 0, 0);
                TaskNamesItemsControl.Margin = margin;
            }
        }

        /// <summary>
        /// Обработка горячих клавиш окна
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Export_Click(this, new RoutedEventArgs());
            }
        }
    }
}