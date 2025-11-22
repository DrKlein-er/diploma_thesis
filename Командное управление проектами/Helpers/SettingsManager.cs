using System;
using System.IO;
using Newtonsoft.Json;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Helpers
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProjectManagement",
            "settings.json"
        );

        private static UserSettingsModel _currentSettings;

        /// <summary>
        /// Получить текущие настройки
        /// </summary>
        public static UserSettingsModel GetSettings(int userId)
        {
            if (_currentSettings == null || _currentSettings.ID_пользователя != userId)
            {
                _currentSettings = LoadSettings(userId);
            }
            return _currentSettings;
        }

        /// <summary>
        /// Загрузить настройки из файла
        /// </summary>
        private static UserSettingsModel LoadSettings(int userId)
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);

                    // Логирование для отладки
                    System.Diagnostics.Debug.WriteLine($"Загружаем настройки из: {SettingsFilePath}");
                    System.Diagnostics.Debug.WriteLine($"Содержимое файла: {json}");

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var allSettings = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<int, UserSettingsModel>>(json);

                        if (allSettings != null && allSettings.ContainsKey(userId))
                        {
                            System.Diagnostics.Debug.WriteLine($"Настройки найдены для пользователя {userId}");
                            return allSettings[userId];
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Настройки не найдены для пользователя {userId}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Файл настроек не существует: {SettingsFilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }

            // Возвращаем настройки по умолчанию
            System.Diagnostics.Debug.WriteLine($"Возвращаем настройки по умолчанию для пользователя {userId}");
            return new UserSettingsModel
            {
                ID_пользователя = userId,
                Тема = "Светлая",
                Уведомления_включены = true,
                Частота_проверки_дедлайнов = 30,
                Звук_уведомлений = true,
                Показывать_завершённые_задачи = true,
                Язык = "Русский"
            };
        }

        /// <summary>
        /// Сохранить настройки в файл
        /// </summary>
        public static void SaveSettings(UserSettingsModel settings)
        {
            try
            {
                // Создаём директорию если её нет
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    System.Diagnostics.Debug.WriteLine($"Создана директория: {directory}");
                }

                // Загружаем все существующие настройки
                var allSettings = new System.Collections.Generic.Dictionary<int, UserSettingsModel>();

                if (File.Exists(SettingsFilePath))
                {
                    string existingJson = File.ReadAllText(SettingsFilePath);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        allSettings = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<int, UserSettingsModel>>(existingJson)
                                    ?? new System.Collections.Generic.Dictionary<int, UserSettingsModel>();
                    }
                }

                // Обновляем настройки для текущего пользователя
                allSettings[settings.ID_пользователя] = settings;

                // Сохраняем обратно в файл
                string json = JsonConvert.SerializeObject(allSettings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);

                System.Diagnostics.Debug.WriteLine($"Настройки сохранены в: {SettingsFilePath}");
                System.Diagnostics.Debug.WriteLine($"Содержимое: {json}");

                _currentSettings = settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
                throw new Exception($"Ошибка при сохранении настроек: {ex.Message}");
            }
        }

        /// <summary>
        /// Применить настройки к приложению
        /// </summary>
        public static void ApplySettings(UserSettingsModel settings, UserModel currentUser)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Применяем настройки для пользователя {currentUser.ID}");
                System.Diagnostics.Debug.WriteLine($"Тема: {settings.Тема}");
                System.Diagnostics.Debug.WriteLine($"Уведомления включены: {settings.Уведомления_включены}");
                System.Diagnostics.Debug.WriteLine($"Частота проверки: {settings.Частота_проверки_дедлайнов} минут");

                // Применяем тему
                ThemeManager.ApplyTheme(settings.Тема);

                // Применяем настройки уведомлений
                var notificationService = NotificationService.Instance;

                if (settings.Уведомления_включены)
                {
                    // Проверяем, инициализирован ли сервис
                    if (notificationService != null)
                    {
                        notificationService.UpdateCheckFrequency(settings.Частота_проверки_дедлайнов);
                        System.Diagnostics.Debug.WriteLine("Частота проверки обновлена");
                    }
                }
                else
                {
                    // Не останавливаем сервис полностью, просто перестаём показывать уведомления
                    System.Diagnostics.Debug.WriteLine("Уведомления отключены");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка применения настроек: {ex.Message}");
            }
        }

        /// <summary>
        /// Сброс настроек к значениям по умолчанию
        /// </summary>
        public static UserSettingsModel ResetToDefaults(int userId)
        {
            var defaultSettings = new UserSettingsModel
            {
                ID_пользователя = userId,
                Тема = "Светлая",
                Уведомления_включены = true,
                Частота_проверки_дедлайнов = 30,
                Звук_уведомлений = true,
                Показывать_завершённые_задачи = true,
                Язык = "Русский"
            };

            SaveSettings(defaultSettings);
            return defaultSettings;
        }
    }
}

