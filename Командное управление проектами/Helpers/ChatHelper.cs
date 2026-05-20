using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Helpers
{

    /// Класс для работы с базой данных чата
    public static class ChatHelper
    {
        private static readonly string connectionString =
            "Data Source=DESKTOP-JRVC3AP;Initial Catalog=Coursework;Integrated Security=True";

        /// Сохранение сообщения в базу данных
        public static int SaveMessage(int? projectId, int userId, string userName, string message)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO Сообщения_Чата 
                                   (ID_проекта, ID_отправителя, Имя_отправителя, Текст_сообщения, Дата_отправки) 
                                   VALUES (@ProjectId, @UserId, @UserName, @Message, @Timestamp);
                                   SELECT CAST(SCOPE_IDENTITY() as int);";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProjectId", projectId.HasValue ? (object)projectId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@UserName", userName);
                        cmd.Parameters.AddWithValue("@Message", message);
                        cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);

                        int messageId = (int)cmd.ExecuteScalar();
                        return messageId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения сообщения: {ex.Message}");
                return -1;
            }
        }


        /// Получение истории сообщений для проекта

        public static List<ChatMessageModel> GetChatHistory(int? projectId, int limit = 100)
        {
            List<ChatMessageModel> messages = new List<ChatMessageModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query;

                    if (projectId.HasValue)
                    {
                        // История чата конкретного проекта
                        query = @"SELECT TOP (@Limit) 
                                    ID_сообщения, ID_проекта, ID_отправителя, 
                                    Имя_отправителя, Текст_сообщения, Дата_отправки, Прочитано
                                FROM Сообщения_Чата 
                                WHERE ID_проекта = @ProjectId
                                ORDER BY Дата_отправки ASC";
                    }
                    else
                    {
                        // История общего чата
                        query = @"SELECT TOP (@Limit) 
                                    ID_сообщения, ID_проекта, ID_отправителя, 
                                    Имя_отправителя, Текст_сообщения, Дата_отправки, Прочитано
                                FROM Сообщения_Чата 
                                WHERE ID_проекта IS NULL
                                ORDER BY Дата_отправки ASC";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Limit", limit);
                        if (projectId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@ProjectId", projectId.Value);
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                messages.Add(new ChatMessageModel
                                {
                                    ID_сообщения = reader.GetInt32(0),
                                    ID_проекта = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                    ID_отправителя = reader.GetInt32(2),
                                    Имя_отправителя = reader.GetString(3),
                                    Текст_сообщения = reader.GetString(4),
                                    Дата_отправки = reader.GetDateTime(5),
                                    Прочитано = reader.GetBoolean(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки истории чата: {ex.Message}");
            }

            return messages;
        }


        /// Пометить сообщение как прочитанное

        public static void MarkAsRead(int messageId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE Сообщения_Чата SET Прочитано = 1 WHERE ID_сообщения = @MessageId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MessageId", messageId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статуса сообщения: {ex.Message}");
            }
        }


        /// Пометить все сообщения проекта как прочитанные

        public static void MarkAllAsRead(int? projectId, int userId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query;

                    if (projectId.HasValue)
                    {
                        query = @"UPDATE Сообщения_Чата 
                                SET Прочитано = 1 
                                WHERE ID_проекта = @ProjectId AND ID_отправителя != @UserId";
                    }
                    else
                    {
                        query = @"UPDATE Сообщения_Чата 
                                SET Прочитано = 1 
                                WHERE ID_проекта IS NULL AND ID_отправителя != @UserId";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (projectId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@ProjectId", projectId.Value);
                        }
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка пометки сообщений: {ex.Message}");
            }
        }


        /// Получить количество непрочитанных сообщений

        public static int GetUnreadCount(int? projectId, int userId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query;

                    if (projectId.HasValue)
                    {
                        query = @"SELECT COUNT(*) FROM Сообщения_Чата 
                                WHERE ID_проекта = @ProjectId 
                                AND ID_отправителя != @UserId 
                                AND Прочитано = 0";
                    }
                    else
                    {
                        query = @"SELECT COUNT(*) FROM Сообщения_Чата 
                                WHERE ID_проекта IS NULL 
                                AND ID_отправителя != @UserId 
                                AND Прочитано = 0";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (projectId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@ProjectId", projectId.Value);
                        }
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подсчета непрочитанных: {ex.Message}");
                return 0;
            }
        }


        /// Удалить все сообщения проекта (при удалении проекта)

        public static void DeleteProjectMessages(int projectId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM Сообщения_Чата WHERE ID_проекта = @ProjectId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProjectId", projectId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления сообщений проекта: {ex.Message}");
            }
        }


        /// Поиск сообщений по тексту

        public static List<ChatMessageModel> SearchMessages(string searchText, int? projectId = null)
        {
            List<ChatMessageModel> messages = new List<ChatMessageModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT TOP 50
                                        ID_сообщения, ID_проекта, ID_отправителя, 
                                        Имя_отправителя, Текст_сообщения, Дата_отправки, Прочитано
                                    FROM Сообщения_Чата 
                                    WHERE Текст_сообщения LIKE @SearchText";

                    if (projectId.HasValue)
                    {
                        query += " AND ID_проекта = @ProjectId";
                    }

                    query += " ORDER BY Дата_отправки DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
                        if (projectId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@ProjectId", projectId.Value);
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                messages.Add(new ChatMessageModel
                                {
                                    ID_сообщения = reader.GetInt32(0),
                                    ID_проекта = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                    ID_отправителя = reader.GetInt32(2),
                                    Имя_отправителя = reader.GetString(3),
                                    Текст_сообщения = reader.GetString(4),
                                    Дата_отправки = reader.GetDateTime(5),
                                    Прочитано = reader.GetBoolean(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка поиска сообщений: {ex.Message}");
            }

            return messages;
        }
        // Удаление сообщения из базы данных
        public static void DeleteMessage(long messageId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                DELETE FROM Сообщения_Чата 
                WHERE CAST(DATEDIFF(SECOND, '1970-01-01', Дата_отправки) AS BIGINT) * 10000000 = @MessageId
                OR ID_сообщения = @MessageId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MessageId", messageId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при удалении сообщения: {ex.Message}");
            }
        }


    }
}
