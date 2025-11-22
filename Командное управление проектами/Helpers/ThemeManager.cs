using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Командное_управление_проектами.Helpers
{
    public static class ThemeManager
    {
        private const string LightThemeUri = "Themes/LightTheme.xaml";
        private const string DarkThemeUri = "Themes/DarkTheme.xaml";

        // Событие для уведомления об изменении темы
        public static event Action ThemeChanged;

        /// <summary>
        /// Применить тему к приложению
        /// </summary>
        public static void ApplyTheme(string themeName)
        {
            try
            {
                string themeUri = themeName == "Тёмная" ? DarkThemeUri : LightThemeUri;

                var newTheme = new ResourceDictionary
                {
                    Source = new Uri(themeUri, UriKind.Relative)
                };

                // Удаляем старую тему
                if (Application.Current.Resources.MergedDictionaries.Count > 0)
                {
                    Application.Current.Resources.MergedDictionaries.Clear();
                }

                // Добавляем новую тему
                Application.Current.Resources.MergedDictionaries.Add(newTheme);

                System.Diagnostics.Debug.WriteLine($"Тема '{themeName}' успешно применена");

                // Уведомляем подписчиков об изменении темы
                ThemeChanged?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка применения темы: {ex.Message}");
            }
        }
        /// <summary>
        /// Получить текущую тему
        /// </summary>
        public static string GetCurrentTheme()
        {
            if (Application.Current.Resources.MergedDictionaries.Count > 0)
            {
                var currentDict = Application.Current.Resources.MergedDictionaries[0];
                if (currentDict.Source != null && currentDict.Source.ToString().Contains("DarkTheme"))
                {
                    return "Тёмная";
                }
            }
            return "Светлая";
        }
    }
}
