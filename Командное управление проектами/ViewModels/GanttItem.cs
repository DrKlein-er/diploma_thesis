using System;
using System.Windows;
using System.Windows.Media;

namespace Командное_управление_проектами.ViewModels
{
    public class GanttItem
    {
        // Основные данные
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }  
        // Свойства для отображения в XAML
        public Brush BarColor { get; set; }
        public double BarWidth { get; set; }
        public double BarLeft { get; set; } 
        public double BarTop { get; set; }  
        public Thickness BarMargin { get; set; }
        public string Duration { get; set; }
        public string ToolTipText { get; set; }

    }
}