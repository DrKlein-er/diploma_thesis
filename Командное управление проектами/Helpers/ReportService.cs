using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Командное_управление_проектами.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Командное_управление_проектами.Views;

namespace Командное_управление_проектами.Helpers
{
    public static class ReportService
    {
        // Генерация PDF отчёта по всем проектам
        public static void GenerateProjectsReportPDF(string filePath, List<ProjectModel> projects)
        {
            Document document = new Document(PageSize.A4, 50, 50, 25, 25);

            try
            {
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                document.Open();

                // Настройка шрифтов (поддержка кириллицы)
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font titleFont = new Font(baseFont, 18, Font.BOLD);
                Font headerFont = new Font(baseFont, 12, Font.BOLD);
                Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                Font smallFont = new Font(baseFont, 8, Font.NORMAL);

                // Заголовок
                Paragraph title = new Paragraph("Отчёт по проектам", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20;
                document.Add(title);

                // Дата генерации
                Paragraph date = new Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", smallFont);
                date.Alignment = Element.ALIGN_RIGHT;
                date.SpacingAfter = 20;
                document.Add(date);

                // Общая статистика
                document.Add(new Paragraph("Общая статистика", headerFont));
                document.Add(new Paragraph($"Всего проектов: {projects.Count}", normalFont));
                document.Add(new Paragraph($"Завершено: {projects.Count(p => p.Статус == "Завершен")}", normalFont));
                document.Add(new Paragraph($"В процессе: {projects.Count(p => p.Статус == "В процессе")}", normalFont));
                document.Add(new Paragraph($"Отложено: {projects.Count(p => p.Статус == "Отложен")}", normalFont));
                document.Add(new Paragraph($"Общий бюджет: {projects.Sum(p => p.Бюджет):N2} ₽", normalFont));
                document.Add(new Paragraph("\n", normalFont));

                // Таблица проектов
                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 5f, 20f, 15f, 15f, 15f, 15f });

                // Заголовки таблицы
                AddTableHeader(table, "ID", headerFont);
                AddTableHeader(table, "Название", headerFont);
                AddTableHeader(table, "Статус", headerFont);
                AddTableHeader(table, "Ответственный", headerFont);
                AddTableHeader(table, "Срок", headerFont);
                AddTableHeader(table, "Бюджет", headerFont);

                // Данные
                foreach (var project in projects)
                {
                    AddTableCell(table, project.ID_проекта.ToString(), normalFont);
                    AddTableCell(table, project.Название_проекта, normalFont);
                    AddTableCell(table, project.Статус, normalFont);
                    AddTableCell(table, project.Ответственный, normalFont);
                    AddTableCell(table, project.Дата_завершения?.ToString("dd.MM.yyyy") ?? "—", normalFont);
                    AddTableCell(table, $"{project.Бюджет:N2} ₽", normalFont);
                }

                document.Add(table);

                // Подробная информация по каждому проекту
                document.NewPage();
                document.Add(new Paragraph("Подробная информация по проектам", headerFont));
                document.Add(new Paragraph("\n", normalFont));

                foreach (var project in projects)
                {
                    document.Add(new Paragraph($"Проект: {project.Название_проекта}", headerFont));
                    document.Add(new Paragraph($"Описание: {project.Описание}", normalFont));
                    document.Add(new Paragraph($"Статус: {project.Статус}", normalFont));
                    document.Add(new Paragraph($"Ответственный: {project.Ответственный}", normalFont));
                    document.Add(new Paragraph($"Период: {project.Дата_начала?.ToString("dd.MM.yyyy")} - {project.Дата_завершения?.ToString("dd.MM.yyyy")}", normalFont));
                    document.Add(new Paragraph($"Бюджет: {project.Бюджет:N2} ₽", normalFont));
                    document.Add(new Paragraph("\n", normalFont));
                }

                document.Close();
            }
            catch (Exception ex)
            {
                document?.Close();
                throw new Exception($"Ошибка при создании PDF: {ex.Message}");
            }
        }

        // Генерация PDF отчёта по задачам
        public static void GenerateTasksReportPDF(string filePath, List<TaskModel> tasks)
        {
            Document document = new Document(PageSize.A4.Rotate(), 50, 50, 25, 25); // Альбомная ориентация

            try
            {
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                document.Open();

                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font titleFont = new Font(baseFont, 18, Font.BOLD);
                Font headerFont = new Font(baseFont, 12, Font.BOLD);
                Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                Font smallFont = new Font(baseFont, 8, Font.NORMAL);

                // Заголовок
                Paragraph title = new Paragraph("Отчёт по задачам", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20;
                document.Add(title);

                Paragraph date = new Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", smallFont);
                date.Alignment = Element.ALIGN_RIGHT;
                date.SpacingAfter = 20;
                document.Add(date);

                // Статистика
                document.Add(new Paragraph("Общая статистика", headerFont));
                document.Add(new Paragraph($"Всего задач: {tasks.Count}", normalFont));
                document.Add(new Paragraph($"Открыто: {tasks.Count(t => t.Статус == "Открыта")}", normalFont));
                document.Add(new Paragraph($"В процессе: {tasks.Count(t => t.Статус == "В процессе")}", normalFont));
                document.Add(new Paragraph($"Завершено: {tasks.Count(t => t.Статус == "Завершена")}", normalFont));

                var overdueTasks = tasks.Where(t => t.Дата_завершения.HasValue &&
                                                    t.Дата_завершения.Value < DateTime.Now &&
                                                    t.Статус != "Завершена").Count();
                document.Add(new Paragraph($"Просрочено: {overdueTasks}", normalFont));
                document.Add(new Paragraph("\n", normalFont));

                // Таблица задач
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 5f, 20f, 15f, 10f, 10f, 15f, 12f });

                AddTableHeader(table, "ID", headerFont);
                AddTableHeader(table, "Название", headerFont);
                AddTableHeader(table, "Проект", headerFont);
                AddTableHeader(table, "Приоритет", headerFont);
                AddTableHeader(table, "Статус", headerFont);
                AddTableHeader(table, "Ответственный", headerFont);
                AddTableHeader(table, "Срок", headerFont);

                foreach (var task in tasks.OrderBy(t => t.Дата_завершения))
                {
                    AddTableCell(table, task.ID_задачи.ToString(), normalFont);
                    AddTableCell(table, task.Название_задачи, normalFont);
                    AddTableCell(table, task.Название_проекта, normalFont);
                    AddTableCell(table, task.Приоритет ?? "—", normalFont);
                    AddTableCell(table, task.Статус, normalFont);
                    AddTableCell(table, task.Ответственный ?? "Не назначен", normalFont);
                    AddTableCell(table, task.Дата_завершения?.ToString("dd.MM.yyyy") ?? "—", normalFont);
                }

                document.Add(table);

                // Анализ по приоритетам
                document.NewPage();
                document.Add(new Paragraph("Анализ по приоритетам", headerFont));
                document.Add(new Paragraph("\n", normalFont));

                var tasksByPriority = tasks.GroupBy(t => t.Приоритет ?? "Не указан")
                                          .Select(g => new { Priority = g.Key, Count = g.Count() })
                                          .OrderByDescending(x => x.Count);

                foreach (var group in tasksByPriority)
                {
                    document.Add(new Paragraph($"{group.Priority}: {group.Count} задач", normalFont));
                }

                document.Close();
            }
            catch (Exception ex)
            {
                document?.Close();
                throw new Exception($"Ошибка при создании PDF: {ex.Message}");
            }
        }

        // Расширенный экспорт проектов в Excel
        public static void ExportProjectsToExcel(string filePath, List<ProjectModel> projects)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Лист 1: Список проектов
                var sheet1 = package.Workbook.Worksheets.Add("Проекты");

                // Заголовки
                string[] headers = { "ID", "Название", "Описание", "Статус", "Ответственный", "Дата начала", "Дата завершения", "Бюджет" };
                for (int i = 0; i < headers.Length; i++)
                {
                    sheet1.Cells[1, i + 1].Value = headers[i];
                }

                // Данные
                int row = 2;
                foreach (var project in projects)
                {
                    sheet1.Cells[row, 1].Value = project.ID_проекта;
                    sheet1.Cells[row, 2].Value = project.Название_проекта;
                    sheet1.Cells[row, 3].Value = project.Описание;
                    sheet1.Cells[row, 4].Value = project.Статус;
                    sheet1.Cells[row, 5].Value = project.Ответственный;
                    sheet1.Cells[row, 6].Value = project.Дата_начала?.ToString("dd.MM.yyyy");
                    sheet1.Cells[row, 7].Value = project.Дата_завершения?.ToString("dd.MM.yyyy");
                    sheet1.Cells[row, 8].Value = project.Бюджет;
                    row++;
                }

                // Стилизация заголовков
                using (var range = sheet1.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#4472C4"));
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                sheet1.Cells[sheet1.Dimension.Address].AutoFitColumns();
                sheet1.Column(8).Style.Numberformat.Format = "#,##0.00 ₽";

                // Лист 2: Статистика
                var sheet2 = package.Workbook.Worksheets.Add("Статистика");
                sheet2.Cells["A1"].Value = "Показатель";
                sheet2.Cells["B1"].Value = "Значение";

                sheet2.Cells["A2"].Value = "Всего проектов";
                sheet2.Cells["B2"].Value = projects.Count;

                sheet2.Cells["A3"].Value = "Завершено";
                sheet2.Cells["B3"].Value = projects.Count(p => p.Статус == "Завершен");

                sheet2.Cells["A4"].Value = "В процессе";
                sheet2.Cells["B4"].Value = projects.Count(p => p.Статус == "В процессе");

                sheet2.Cells["A5"].Value = "Отложено";
                sheet2.Cells["B5"].Value = projects.Count(p => p.Статус == "Отложен");

                sheet2.Cells["A6"].Value = "Общий бюджет";
                sheet2.Cells["B6"].Value = projects.Sum(p => p.Бюджет);
                sheet2.Cells["B6"].Style.Numberformat.Format = "#,##0.00 ₽";

                using (var range = sheet2.Cells["A1:B1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#4472C4"));
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                sheet2.Cells[sheet2.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(filePath));
            }
        }

        // Расширенный экспорт задач в Excel
        public static void ExportTasksToExcel(string filePath, List<TaskModel> tasks)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Лист 1: Список задач
                var sheet1 = package.Workbook.Worksheets.Add("Задачи");

                string[] headers = { "ID", "Название", "Проект", "Ответственный", "Приоритет", "Статус", "Срок выполнения" };
                for (int i = 0; i < headers.Length; i++)
                {
                    sheet1.Cells[1, i + 1].Value = headers[i];
                }

                int row = 2;
                foreach (var task in tasks)
                {
                    sheet1.Cells[row, 1].Value = task.ID_задачи;
                    sheet1.Cells[row, 2].Value = task.Название_задачи;
                    sheet1.Cells[row, 3].Value = task.Название_проекта;
                    sheet1.Cells[row, 4].Value = task.Ответственный;
                    sheet1.Cells[row, 5].Value = task.Приоритет;
                    sheet1.Cells[row, 6].Value = task.Статус;
                    sheet1.Cells[row, 7].Value = task.Дата_завершения?.ToString("dd.MM.yyyy");
                    row++;
                }

                using (var range = sheet1.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#4472C4"));
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                sheet1.Cells[sheet1.Dimension.Address].AutoFitColumns();

                // Лист 2: Статистика по статусам
                var sheet2 = package.Workbook.Worksheets.Add("Статистика");
                sheet2.Cells["A1"].Value = "Статус";
                sheet2.Cells["B1"].Value = "Количество";

                var statusGroups = tasks.GroupBy(t => t.Статус)
                                       .Select(g => new { Status = g.Key, Count = g.Count() })
                                       .OrderByDescending(x => x.Count)
                                       .ToList();

                row = 2;
                foreach (var group in statusGroups)
                {
                    sheet2.Cells[row, 1].Value = group.Status;
                    sheet2.Cells[row, 2].Value = group.Count;
                    row++;
                }

                using (var range = sheet2.Cells["A1:B1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#4472C4"));
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                sheet2.Cells[sheet2.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(filePath));
            }
        }

        // Вспомогательные методы
        private static void AddTableHeader(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = new BaseColor(68, 114, 196);
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            table.AddCell(cell);
        }

        private static void AddTableCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            table.AddCell(cell);
        }
    }
}