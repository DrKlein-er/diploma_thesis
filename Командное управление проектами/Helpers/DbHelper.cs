using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Media;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Helpers
{
    public static class DbHelper
    {
        private static readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Coursework;Integrated Security=True";

        // ========================== ПОЛЬЗОВАТЕЛИ ==========================

        // Получение пользователя по Email
        public static UserModel GetUserByEmail(string email)
        {
            UserModel user = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT 
                    п.ID_пользователя, 
                    п.Email, 
                    п.Пароль,
                    с.ID_сотрудника,
                    с.Фамилия,
                    с.Имя,
                    с.Отчество,
                    р.Название_роли
                 FROM Пользователи п
                 JOIN Сотрудники с ON п.ID_сотрудника = с.ID_сотрудника
                 JOIN Роли р ON с.ID_роли = р.ID_роли
                 WHERE п.Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new UserModel
                            {
                                ID = reader.GetInt32(0),
                                Email = reader.GetString(1),
                                Пароль = reader.GetString(2),
                                ID_сотрудника = reader.GetInt32(3),
                                Фамилия = reader.GetString(4),
                                Имя = reader.GetString(5),
                                Отчество = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Роль = reader.GetString(7)
                            };
                        }
                    }
                }
            }

            return user;
        }

        // Получение всех пользователей
        public static List<UserModel> GetAllUsers()
        {
            List<UserModel> users = new List<UserModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT 
                    п.ID_пользователя, 
                    п.Email, 
                    п.Пароль,
                    с.ID_сотрудника,
                    с.Фамилия,
                    с.Имя,
                    с.Отчество,
                    р.Название_роли
                 FROM Пользователи п
                 JOIN Сотрудники с ON п.ID_сотрудника = с.ID_сотрудника
                 JOIN Роли р ON с.ID_роли = р.ID_роли";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserModel
                        {
                            ID = reader.GetInt32(0),
                            Email = reader.GetString(1),
                            Пароль = reader.GetString(2),
                            ID_сотрудника = reader.GetInt32(3),
                            Фамилия = reader.GetString(4),
                            Имя = reader.GetString(5),
                            Отчество = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            Роль = reader.GetString(7)
                        });
                    }
                }
            }
            return users;
        }

        // Удаление пользователя
        public static void DeleteUser(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Пользователи WHERE ID_пользователя = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== СОТРУДНИКИ ==========================

        // Получение всех сотрудников
        public static List<EmployeeModel> GetAllEmployees()
        {
            List<EmployeeModel> employees = new List<EmployeeModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT 
                    с.ID_сотрудника, 
                    с.Фамилия,
                    с.Имя,
                    с.Отчество,
                    с.Отдел,
                    р.ID_роли,
                    р.Название_роли
                FROM Сотрудники с
                JOIN Роли р ON с.ID_роли = р.ID_роли";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new EmployeeModel
                        {
                            ID_сотрудника = reader.GetInt32(0),
                            Фамилия = reader.GetString(1),
                            Имя = reader.GetString(2),
                            Отчество = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Отдел = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            ID_роли = reader.GetInt32(5),
                            Роль = reader.GetString(6)
                        });
                    }
                }
            }
            return employees;
        }

        // ========================== ПРОЕКТЫ ==========================

        // Получение всех проектов с бюджетом
        public static List<ProjectModel> GetAllProjects()
        {
            List<ProjectModel> projects = new List<ProjectModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    p.ID_проекта,
                    p.Название_проекта,
                    ISNULL(p.Описание, '') AS Описание,
                    p.Дата_начала,
                    p.Дата_завершения,
                    p.Статус,
                    ISNULL(с.Фамилия + ' ' + с.Имя + ' ' + ISNULL(с.Отчество, ''), 'Не назначен') AS Ответственный,
                    p.ID_ответственного,
                    ISNULL(SUM(b.Сумма), 0) AS Бюджет,
                    (SELECT COUNT(*) FROM Задачи WHERE ID_проекта = p.ID_проекта) AS ВсегоЗадач,
                    (SELECT COUNT(*) FROM Задачи WHERE ID_проекта = p.ID_проекта AND Статус = 'Завершена') AS ЗавершенныхЗадач
                FROM Проекты p
                LEFT JOIN Сотрудники с ON p.ID_ответственного = с.ID_сотрудника
                LEFT JOIN Бюджет b ON p.ID_проекта = b.ID_проекта
                GROUP BY p.ID_проекта, p.Название_проекта, p.Описание, p.Дата_начала, p.Дата_завершения, p.Статус, с.Фамилия, с.Имя, с.Отчество, p.ID_ответственного
                ORDER BY p.ID_проекта";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var project = new ProjectModel
                        {
                            ID_проекта = reader.GetInt32(0),
                            Название_проекта = reader.GetString(1),
                            Описание = reader.GetString(2),
                            Дата_начала = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                            Дата_завершения = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                            Статус = reader.GetString(5),
                            Ответственный = reader.GetString(6),
                            ID_ответственного = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                            Бюджет = reader.GetDecimal(8),
                            ВсегоЗадач = reader.GetInt32(9),
                            ЗавершенныхЗадач = reader.GetInt32(10)
                        };

                        // Цветовая индикация статуса проекта
                        switch (project.Статус)
                        {
                            case "В процессе":
                                project.StatusColor = Brushes.Orange;
                                break;
                            case "Завершен":
                                project.StatusColor = Brushes.Green;
                                break;
                            case "Отложен":
                                project.StatusColor = Brushes.Gray;
                                break;
                            default:
                                project.StatusColor = Brushes.Black;
                                break;
                        }
                        projects.Add(project);
                    }
                }
            }
            return projects;
        }

        // Добавление нового проекта
        public static int AddProject(ProjectModel project)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                        INSERT INTO Проекты 
                        (Название_проекта, Описание, Дата_начала, Дата_завершения, Статус, ID_ответственного)
                        VALUES (@name, @desc, @start, @end, @status, @empId);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", project.Название_проекта);
                    cmd.Parameters.AddWithValue("@desc", (object)project.Описание ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@start", (object)project.Дата_начала ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@end", (object)project.Дата_завершения ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", project.Статус);
                    cmd.Parameters.AddWithValue("@empId", project.ID_ответственного.HasValue ? (object)project.ID_ответственного.Value : DBNull.Value);

                    // Возвращаем ID созданного проекта
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        // Обновление данных проекта
        public static void UpdateProject(ProjectModel project)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                UPDATE Проекты 
                SET Название_проекта = @title, 
                    Описание = @desc, 
                    Дата_начала = @start, 
                    Дата_завершения = @end, 
                    Статус = @status, 
                    ID_ответственного = @empId
                WHERE ID_проекта = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", project.Название_проекта);
                    cmd.Parameters.AddWithValue("@desc", (object)project.Описание ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@start", (object)project.Дата_начала ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@end", (object)project.Дата_завершения ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", project.Статус);
                    cmd.Parameters.AddWithValue("@empId", project.ID_ответственного.HasValue ? (object)project.ID_ответственного.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", project.ID_проекта);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Автоматическое обновление статусов проектов
        public static void UpdateProjectStatuses()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                UPDATE Проекты
                SET Статус = 'Завершен'
                WHERE Дата_завершения <= CAST(GETDATE() AS DATE)
                  AND Статус <> 'Завершен'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Удаление проекта (с каскадным удалением связанных записей)
        public static void DeleteProject(int projectId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Проекты WHERE ID_проекта = @ProjectId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProjectId", projectId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== ЗАДАЧИ ==========================

        // Получение всех задач с подзадачами
        public static List<TaskModel> GetAllTasks()
        {
            List<TaskModel> tasks = new List<TaskModel>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    t.ID_задачи,
                    t.Название_задачи,
                    ISNULL(t.Описание, '') AS Описание,
                    ISNULL(t.Приоритет, '') AS Приоритет,
                    t.Статус,
                    t.Дата_начала,
                    t.Дата_завершения,
                    p.Название_проекта,
                    ISNULL(с.Фамилия + ' ' + с.Имя + ' ' + ISNULL(с.Отчество, ''), '') AS Ответственный,
                    t.ID_ответственного
                FROM Задачи t
                JOIN Проекты p ON t.ID_проекта = p.ID_проекта
                LEFT JOIN Сотрудники с ON t.ID_ответственного = с.ID_сотрудника";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var task = new TaskModel
                        {
                            ID_задачи = reader.GetInt32(0),
                            Название_задачи = reader.GetString(1),
                            Описание = reader.GetString(2),
                            Приоритет = reader.GetString(3),
                            Статус = reader.GetString(4),
                            Дата_начала = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                            Дата_завершения = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                            Название_проекта = reader.GetString(7),
                            Ответственный = reader.GetString(8),
                            ID_ответственного = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9)
                        };

                        // Загружаем подзадачи для этой задачи
                        task.Subtasks = GetSubtasksByTaskId(task.ID_задачи);

                        // Цветовая индикация статуса задачи
                        switch (task.Статус)
                        {
                            case "Открыта":
                                task.StatusColor = Brushes.DodgerBlue;
                                break;
                            case "В процессе":
                                task.StatusColor = Brushes.Orange;
                                break;
                            case "Завершена":
                                task.StatusColor = Brushes.Green;
                                break;
                            default:
                                task.StatusColor = Brushes.Gray;
                                break;
                        }

                        tasks.Add(task);
                    }
                }
            }

            return tasks;
        }

        // Получение задач по дате завершения
        public static List<TaskModel> GetTasksByDate(DateTime date)
        {
            List<TaskModel> tasks = new List<TaskModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT ID_задачи, Название_задачи, Описание, Приоритет, Статус, Дата_начала, Дата_завершения
                FROM Задачи
                WHERE Дата_завершения = @date";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@date", System.Data.SqlDbType.Date).Value = date;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new TaskModel
                            {
                                ID_задачи = reader.GetInt32(0),
                                Название_задачи = reader.GetString(1),
                                Описание = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Приоритет = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Статус = reader.GetString(4),
                                Дата_начала = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                Дата_завершения = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }
            return tasks;
        }

        // Получение задач по дате завершения в диапазоне (для календаря)
        public static List<TaskModel> GetTasksInRange(DateTime start, DateTime end)
        {
            List<TaskModel> tasks = new List<TaskModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    t.ID_задачи,
                    t.Название_задачи,
                    ISNULL(t.Описание, '') AS Описание,
                    ISNULL(t.Приоритет, '') AS Приоритет,
                    t.Статус,
                    t.Дата_начала,
                    t.Дата_завершения,
                    p.Название_проекта,
                    ISNULL(с.Фамилия + ' ' + с.Имя + ' ' + ISNULL(с.Отчество, ''), '') AS Ответственный,
                    t.ID_ответственного
                FROM Задачи t
                JOIN Проекты p ON t.ID_проекта = p.ID_проекта
                LEFT JOIN Сотрудники с ON t.ID_ответственного = с.ID_сотрудника
                WHERE t.Дата_завершения BETWEEN @start AND @end
                ORDER BY t.Дата_завершения";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@start", System.Data.SqlDbType.Date).Value = start.Date;
                    cmd.Parameters.Add("@end", System.Data.SqlDbType.Date).Value = end.Date;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new TaskModel
                            {
                                ID_задачи = reader.GetInt32(0),
                                Название_задачи = reader.GetString(1),
                                Описание = reader.GetString(2),
                                Приоритет = reader.GetString(3),
                                Статус = reader.GetString(4),
                                Дата_начала = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                Дата_завершения = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                Название_проекта = reader.GetString(7),
                                Ответственный = reader.GetString(8),
                                ID_ответственного = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9)
                            });
                        }
                    }
                }
            }
            return tasks;
        }

        // Получение задач по ID проекта
        public static List<TaskModel> GetTasksByProject(int projectId)
        {
            List<TaskModel> tasks = new List<TaskModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    ID_задачи, 
                    Название_задачи, 
                    ISNULL(Описание, '') AS Описание, 
                    ISNULL(Приоритет, '') AS Приоритет, 
                    Статус, 
                    Дата_начала, 
                    Дата_завершения,
                    ID_ответственного
                FROM Задачи
                WHERE ID_проекта = @projectId
                ORDER BY Название_задачи";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new TaskModel
                            {
                                ID_задачи = reader.GetInt32(0),
                                Название_задачи = reader.GetString(1),
                                Описание = reader.GetString(2),
                                Приоритет = reader.GetString(3),
                                Статус = reader.GetString(4),
                                Дата_начала = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                Дата_завершения = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                ID_ответственного = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7)
                            });
                        }
                    }
                }
            }
            return tasks;
        }

        // Получение задачи по ID
        public static TaskModel GetTaskById(int taskId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    ID_задачи, 
                    Название_задачи, 
                    ISNULL(Описание, '') AS Описание, 
                    ISNULL(Приоритет, '') AS Приоритет, 
                    Статус, 
                    Дата_начала, 
                    Дата_завершения,
                    ID_проекта,
                    ID_ответственного
                FROM Задачи
                WHERE ID_задачи = @taskId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TaskModel
                            {
                                ID_задачи = reader.GetInt32(0),
                                Название_задачи = reader.GetString(1),
                                Описание = reader.GetString(2),
                                Приоритет = reader.GetString(3),
                                Статус = reader.GetString(4),
                                Дата_начала = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                Дата_завершения = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                ID_ответственного = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
                            };
                        }
                    }
                }
            }
            return null;
        }

        // Автоматическое обновление статусов задач
        public static void UpdateTaskStatuses()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                UPDATE Задачи
                SET Статус = 'Завершена'
                WHERE Дата_завершения <= CAST(GETDATE() AS DATE)
                  AND Статус <> 'Завершена'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Получение ID проекта по ID задачи
        public static int? GetProjectIdByTaskId(int taskId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ID_проекта FROM Задачи WHERE ID_задачи = @taskId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@taskId", taskId);
                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : (int?)null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении ID проекта: {ex.Message}");
            }
        }

        // Обновление задачи
        public static void UpdateTask(TaskModel task, int projectId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            UPDATE Задачи 
            SET Название_задачи = @title, 
                Описание = @desc, 
                Приоритет = @priority, 
                Статус = @status, 
                Дата_начала = @startDate,
                Дата_завершения = @endDate,
                ID_проекта = @projectId, 
                ID_ответственного = @empId
            WHERE ID_задачи = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", task.Название_задачи);
                    cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(task.Описание) ? (object)DBNull.Value : task.Описание);
                    cmd.Parameters.AddWithValue("@priority", string.IsNullOrEmpty(task.Приоритет) ? (object)DBNull.Value : task.Приоритет);
                    cmd.Parameters.AddWithValue("@status", task.Статус);
                    cmd.Parameters.AddWithValue("@startDate", task.Дата_начала.HasValue ? (object)task.Дата_начала.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@endDate", task.Дата_завершения.HasValue ? (object)task.Дата_завершения.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    cmd.Parameters.AddWithValue("@empId", task.ID_ответственного.HasValue ? (object)task.ID_ответственного.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", task.ID_задачи);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Добавление новой задачи в базу данных
        public static int AddTask(TaskModel task, int projectId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            INSERT INTO Задачи 
            (Название_задачи, Описание, Приоритет, Статус, Дата_начала, Дата_завершения, ID_проекта, ID_ответственного)
            VALUES (@title, @desc, @priority, @status, @startDate, @endDate, @projectId, @empId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", task.Название_задачи);
                    cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(task.Описание) ? (object)DBNull.Value : task.Описание);
                    cmd.Parameters.AddWithValue("@priority", string.IsNullOrEmpty(task.Приоритет) ? (object)DBNull.Value : task.Приоритет);
                    cmd.Parameters.AddWithValue("@status", task.Статус);
                    cmd.Parameters.AddWithValue("@startDate", task.Дата_начала.HasValue ? (object)task.Дата_начала.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@endDate", task.Дата_завершения.HasValue ? (object)task.Дата_завершения.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    cmd.Parameters.AddWithValue("@empId", task.ID_ответственного.HasValue ? (object)task.ID_ответственного.Value : DBNull.Value);

                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        // ========================== ПОДЗАДАЧИ ==========================

        // Получение подзадач по ID задачи
        public static List<SubtaskModel> GetSubtasksByTaskId(int taskId)
        {
            List<SubtaskModel> subtasks = new List<SubtaskModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    п.ID_подзадачи,
                    п.Название_подзадачи,
                    п.Описание,
                    п.Статус,
                    п.Дата_начала,
                    п.Дата_завершения,
                    п.ID_задачи,
                    з.Название_задачи
                FROM Подзадачи п
                JOIN Задачи з ON п.ID_задачи = з.ID_задачи
                WHERE п.ID_задачи = @taskId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var subtask = new SubtaskModel
                            {
                                ID_подзадачи = reader.GetInt32(0),
                                Название_подзадачи = reader.GetString(1),
                                Описание = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Статус = reader.GetString(3),
                                Дата_начала = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                Дата_завершения = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                ID_задачи = reader.GetInt32(6),
                                Название_задачи = reader.GetString(7)
                            };

                            // Цветовая индикация статуса подзадачи
                            switch (subtask.Статус)
                            {
                                case "Открыта":
                                    subtask.StatusColor = Brushes.DodgerBlue;
                                    break;
                                case "В процессе":
                                    subtask.StatusColor = Brushes.Orange;
                                    break;
                                case "Завершена":
                                    subtask.StatusColor = Brushes.Green;
                                    break;
                                default:
                                    subtask.StatusColor = Brushes.Gray;
                                    break;
                            }

                            subtasks.Add(subtask);
                        }
                    }
                }
            }
            return subtasks;
        }

        // Добавление подзадачи
        public static void AddSubtask(SubtaskModel subtask)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"INSERT INTO Подзадачи (Название_подзадачи, Описание, Статус, Дата_начала, Дата_завершения, ID_задачи) 
                              VALUES (@name, @desc, @status, @startDate, @endDate, @taskId)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", subtask.Название_подзадачи);
                    cmd.Parameters.AddWithValue("@desc", (object)subtask.Описание ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", subtask.Статус);
                    cmd.Parameters.AddWithValue("@startDate", (object)subtask.Дата_начала ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@endDate", (object)subtask.Дата_завершения ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@taskId", subtask.ID_задачи);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Обновление подзадачи
        public static void UpdateSubtask(SubtaskModel subtask)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"UPDATE Подзадачи 
                              SET Название_подзадачи = @name, Описание = @desc, Статус = @status, 
                                  Дата_начала = @startDate, Дата_завершения = @endDate 
                              WHERE ID_подзадачи = @subtaskId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", subtask.Название_подзадачи);
                    cmd.Parameters.AddWithValue("@desc", (object)subtask.Описание ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", subtask.Статус);
                    cmd.Parameters.AddWithValue("@startDate", (object)subtask.Дата_начала ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@endDate", (object)subtask.Дата_завершения ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@subtaskId", subtask.ID_подзадачи);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Удаление подзадачи
        public static void DeleteSubtask(int subtaskId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "DELETE FROM Подзадачи WHERE ID_подзадачи = @subtaskId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@subtaskId", subtaskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== СОБЫТИЯ ==========================

        // Получение всех событий
        public static List<EventModel> GetAllEvents()
        {
            List<EventModel> events = new List<EventModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    e.ID_события,
                    e.Название_события,
                    e.Описание,
                    e.Дата_начала,
                    e.Дата_окончания,
                    e.Тип_события,
                    e.Место_проведения,
                    e.ID_проекта,
                    e.ID_задачи,
                    p.Название_проекта,
                    t.Название_задачи
                FROM События e
                LEFT JOIN Проекты p ON e.ID_проекта = p.ID_проекта
                LEFT JOIN Задачи  t ON e.ID_задачи  = t.ID_задачи
                ORDER BY e.Дата_начала";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        events.Add(MapEvent(reader));
                    }
                }
            }
            return events;
        }

        // Получение событий по дате (по дате начала, игнорируя время)
        public static List<EventModel> GetEventsByDate(DateTime date)
        {
            var list = new List<EventModel>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"
                SELECT 
                    e.ID_события,
                    e.Название_события, 
                    e.Описание, 
                    e.Дата_начала,
                    e.Дата_окончания,
                    e.Тип_события,
                    e.Место_проведения,
                    e.ID_проекта,
                    e.ID_задачи,
                    p.Название_проекта,
                    t.Название_задачи
                FROM События e
                LEFT JOIN Проекты p ON e.ID_проекта = p.ID_проекта
                LEFT JOIN Задачи  t ON e.ID_задачи  = t.ID_задачи
                WHERE CAST(e.Дата_начала AS DATE) = @date
                ORDER BY e.Дата_начала";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@date", date.Date);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapEvent(reader));
                        }
                    }
                }
            }
            return list;
        }


        // Получение событий в диапазоне дат
        public static List<EventModel> GetEventsInRange(DateTime start, DateTime end)
        {
            var list = new List<EventModel>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    e.ID_события,
                    e.Название_события, 
                    e.Описание, 
                    e.Дата_начала,
                    e.Дата_окончания,
                    e.Тип_события,
                    e.Место_проведения,
                    e.ID_проекта,
                    e.ID_задачи,
                    p.Название_проекта,
                    t.Название_задачи
                FROM События e
                LEFT JOIN Проекты p ON e.ID_проекта = p.ID_проекта
                LEFT JOIN Задачи  t ON e.ID_задачи  = t.ID_задачи
                WHERE e.Дата_начала BETWEEN @start AND @end
                ORDER BY e.Дата_начала";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapEvent(reader));
                        }
                    }
                }
            }
            return list;
        }

        // Маппинг ридера в EventModel (внутренний хелпер)
        private static EventModel MapEvent(SqlDataReader r)
        {
            return new EventModel
            {
                ID_события = r.GetInt32(0),
                Название_события = r.GetString(1),
                Описание = r.IsDBNull(2) ? "" : r.GetString(2),
                Дата_начала = r.IsDBNull(3) ? (DateTime?)null : r.GetDateTime(3),
                Дата_окончания = r.IsDBNull(4) ? (DateTime?)null : r.GetDateTime(4),
                Тип_события = r.IsDBNull(5) ? "Другое" : r.GetString(5),
                Место_проведения = r.IsDBNull(6) ? "" : r.GetString(6),
                ID_проекта = r.IsDBNull(7) ? (int?)null : r.GetInt32(7),
                ID_задачи = r.IsDBNull(8) ? (int?)null : r.GetInt32(8),
                Название_проекта = r.IsDBNull(9) ? "" : r.GetString(9),
                Название_задачи = r.IsDBNull(10) ? "" : r.GetString(10)
            };
        }

        // Добавление события
        public static void AddEvent(EventModel ev)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                INSERT INTO События (Название_события, Описание, Дата_начала, Дата_окончания, Тип_события, Место_проведения, ID_проекта, ID_задачи)
                VALUES (@name, @desc, @start, @end, @type, @place, @projId, @taskId)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", ev.Название_события);
                    cmd.Parameters.AddWithValue("@desc", (object)ev.Описание ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@start", ev.Дата_начала ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@end", ev.Дата_окончания.HasValue ? (object)ev.Дата_окончания.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@type", string.IsNullOrWhiteSpace(ev.Тип_события) ? "Другое" : ev.Тип_события);
                    cmd.Parameters.AddWithValue("@place", (object)ev.Место_проведения ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@projId", ev.ID_проекта.HasValue ? (object)ev.ID_проекта.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@taskId", ev.ID_задачи.HasValue ? (object)ev.ID_задачи.Value : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Обновление события
        public static void UpdateEvent(EventModel ev)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                UPDATE События
                SET Название_события = @name,
                    Описание = @desc,
                    Дата_начала = @start,
                    Дата_окончания = @end,
                    Тип_события = @type,
                    Место_проведения = @place,
                    ID_проекта = @projId,
                    ID_задачи = @taskId
                WHERE ID_события = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", ev.Название_события);
                    cmd.Parameters.AddWithValue("@desc", (object)ev.Описание ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@start", ev.Дата_начала ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@end", ev.Дата_окончания.HasValue ? (object)ev.Дата_окончания.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@type", string.IsNullOrWhiteSpace(ev.Тип_события) ? "Другое" : ev.Тип_события);
                    cmd.Parameters.AddWithValue("@place", (object)ev.Место_проведения ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@projId", ev.ID_проекта.HasValue ? (object)ev.ID_проекта.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@taskId", ev.ID_задачи.HasValue ? (object)ev.ID_задачи.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", ev.ID_события);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Удаление события
        public static void DeleteEvent(int eventId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM События WHERE ID_события = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", eventId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== НАПОМИНАНИЯ ==========================

        // Получение всех напоминаний
        public static List<ReminderModel> GetAllReminders()
        {
            List<ReminderModel> reminders = new List<ReminderModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    n.ID_напоминания,
                    n.Текст_напоминания,
                    n.Дата_напоминания,
                    n.Статус,
                    n.Приоритет,
                    n.ID_задачи,
                    t.Название_задачи
                FROM Напоминания n
                LEFT JOIN Задачи t ON n.ID_задачи = t.ID_задачи
                ORDER BY 
                    CASE n.Статус WHEN N'Активно' THEN 0 WHEN N'Отложено' THEN 1 WHEN N'Сработало' THEN 2 ELSE 3 END,
                    n.Дата_напоминания";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reminders.Add(MapReminder(reader));
                    }
                }
            }
            return reminders;
        }

        // Получение напоминаний по дате (только активные/отложенные)
        public static List<string> GetRemindersByDate(DateTime date)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"
                SELECT Текст_напоминания 
                FROM Напоминания 
                WHERE CAST(Дата_напоминания AS DATE) = @date
                  AND Статус IN (N'Активно', N'Отложено')";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@date", date.Date);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return list;
        }

        // Получение активных напоминаний, для которых пора показать Toast
        public static List<ReminderModel> GetDueReminders()
        {
            var list = new List<ReminderModel>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"
                SELECT 
                    n.ID_напоминания,
                    n.Текст_напоминания,
                    n.Дата_напоминания,
                    n.Статус,
                    n.Приоритет,
                    n.ID_задачи,
                    t.Название_задачи
                FROM Напоминания n
                LEFT JOIN Задачи t ON n.ID_задачи = t.ID_задачи
                WHERE n.Статус IN (N'Активно', N'Отложено')
                  AND n.Дата_напоминания <= @now
                ORDER BY n.Дата_напоминания";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@now", DateTime.Now);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapReminder(reader));
                        }
                    }
                }
            }
            return list;
        }

        // Получение напоминаний в диапазоне дат (для календаря)
        public static List<ReminderModel> GetRemindersInRange(DateTime start, DateTime end)
        {
            var list = new List<ReminderModel>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"
                SELECT 
                    n.ID_напоминания,
                    n.Текст_напоминания,
                    n.Дата_напоминания,
                    n.Статус,
                    n.Приоритет,
                    n.ID_задачи,
                    t.Название_задачи
                FROM Напоминания n
                LEFT JOIN Задачи t ON n.ID_задачи = t.ID_задачи
                WHERE n.Дата_напоминания BETWEEN @start AND @end
                ORDER BY n.Дата_напоминания";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ReminderModel
                            {
                                ID_напоминания = reader.GetInt32(0),
                                Текст_напоминания = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Дата_напоминания = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                Статус = reader.IsDBNull(3) ? "Активно" : reader.GetString(3),
                                Приоритет = reader.IsDBNull(4) ? "Средний" : reader.GetString(4),
                                ID_задачи = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                Название_задачи = reader.IsDBNull(6) ? "" : reader.GetString(6)
                            });
                        }
                    }
                }
            }
            return list;
        }

        // Маппинг ридера в ReminderModel (внутренний хелпер)
        private static ReminderModel MapReminder(SqlDataReader r)
        {
            return new ReminderModel
            {
                ID_напоминания = r.GetInt32(0),
                Текст_напоминания = r.IsDBNull(1) ? "" : r.GetString(1),
                Дата_напоминания = r.IsDBNull(2) ? (DateTime?)null : r.GetDateTime(2),
                Статус = r.IsDBNull(3) ? "Активно" : r.GetString(3),
                Приоритет = r.IsDBNull(4) ? "Средний" : r.GetString(4),
                ID_задачи = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                Название_задачи = r.IsDBNull(6) ? "" : r.GetString(6)
            };
        }

        // Добавление напоминания
        public static void AddReminder(ReminderModel reminder)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                INSERT INTO Напоминания (Текст_напоминания, Дата_напоминания, Статус, Приоритет, ID_задачи)
                VALUES (@text, @date, @status, @priority, @taskId)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@text", reminder.Текст_напоминания);
                    cmd.Parameters.AddWithValue("@date", reminder.Дата_напоминания ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(reminder.Статус) ? "Активно" : reminder.Статус);
                    cmd.Parameters.AddWithValue("@priority", string.IsNullOrWhiteSpace(reminder.Приоритет) ? "Средний" : reminder.Приоритет);
                    cmd.Parameters.AddWithValue("@taskId", reminder.ID_задачи.HasValue ? (object)reminder.ID_задачи.Value : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Обновление напоминания
        public static void UpdateReminder(ReminderModel reminder)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                UPDATE Напоминания
                SET Текст_напоминания = @text,
                    Дата_напоминания = @date,
                    Статус = @status,
                    Приоритет = @priority,
                    ID_задачи = @taskId
                WHERE ID_напоминания = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@text", reminder.Текст_напоминания);
                    cmd.Parameters.AddWithValue("@date", reminder.Дата_напоминания ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(reminder.Статус) ? "Активно" : reminder.Статус);
                    cmd.Parameters.AddWithValue("@priority", string.IsNullOrWhiteSpace(reminder.Приоритет) ? "Средний" : reminder.Приоритет);
                    cmd.Parameters.AddWithValue("@taskId", reminder.ID_задачи.HasValue ? (object)reminder.ID_задачи.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", reminder.ID_напоминания);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Удаление напоминания
        public static void DeleteReminder(int reminderId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Напоминания WHERE ID_напоминания = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", reminderId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Изменение только статуса напоминания
        public static void SetReminderStatus(int reminderId, string newStatus)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Напоминания SET Статус = @status WHERE ID_напоминания = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id", reminderId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Отложить напоминание (Snooze) на указанное число минут
        public static void SnoozeReminder(int reminderId, int minutes)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                UPDATE Напоминания 
                SET Дата_напоминания = DATEADD(MINUTE, @minutes, 
                                              CASE WHEN Дата_напоминания > GETDATE() 
                                                   THEN Дата_напоминания 
                                                   ELSE GETDATE() END),
                    Статус = N'Отложено'
                WHERE ID_напоминания = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@minutes", minutes);
                    cmd.Parameters.AddWithValue("@id", reminderId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== ЗАМЕТКИ ==========================

        public static List<NoteModel> GetAllNotes()
        {
            List<NoteModel> notes = new List<NoteModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    z.ID_заметки,
                    z.Заголовок,
                    z.Текст_заметки,
                    z.Цвет,
                    z.Закреплена,
                    z.Дата_создания,
                    z.ID_проекта,
                    z.ID_задачи,
                    z.ID_подзадачи,
                    p.Название_проекта,
                    t.Название_задачи,
                    s.Название_подзадачи
                FROM Заметки z
                LEFT JOIN Проекты   p ON z.ID_проекта   = p.ID_проекта
                LEFT JOIN Задачи    t ON z.ID_задачи    = t.ID_задачи
                LEFT JOIN Подзадачи s ON z.ID_подзадачи = s.ID_подзадачи
                ORDER BY z.Закреплена DESC, z.Дата_создания DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        notes.Add(MapNote(reader));
                    }
                }
            }
            return notes;
        }

        // Получение заметок по дате создания
        public static List<NoteModel> GetNotesByDate(DateTime date)
        {
            List<NoteModel> notes = new List<NoteModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    z.ID_заметки,
                    z.Заголовок,
                    z.Текст_заметки, 
                    z.Цвет,
                    z.Закреплена,
                    z.Дата_создания,
                    z.ID_проекта,
                    z.ID_задачи,
                    z.ID_подзадачи,
                    p.Название_проекта,
                    t.Название_задачи,
                    s.Название_подзадачи
                FROM Заметки z
                LEFT JOIN Проекты   p ON z.ID_проекта   = p.ID_проекта
                LEFT JOIN Задачи    t ON z.ID_задачи    = t.ID_задачи
                LEFT JOIN Подзадачи s ON z.ID_подзадачи = s.ID_подзадачи
                WHERE CAST(z.Дата_создания AS DATE) = @date
                ORDER BY z.Закреплена DESC, z.Дата_создания DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@date", date.Date);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notes.Add(MapNote(reader));
                        }
                    }
                }
            }
            return notes;
        }

        // Маппинг ридера в NoteModel (внутренний хелпер)
        private static NoteModel MapNote(SqlDataReader r)
        {
            return new NoteModel
            {
                ID_заметки = r.GetInt32(0),
                Заголовок = r.GetString(1),
                Текст_заметки = r.IsDBNull(2) ? "" : r.GetString(2),
                Цвет = r.IsDBNull(3) ? "Жёлтый" : r.GetString(3),
                Закреплена = !r.IsDBNull(4) && r.GetBoolean(4),
                Дата_создания = r.IsDBNull(5) ? (DateTime?)null : r.GetDateTime(5),
                ID_проекта = r.IsDBNull(6) ? (int?)null : r.GetInt32(6),
                ID_задачи = r.IsDBNull(7) ? (int?)null : r.GetInt32(7),
                ID_подзадачи = r.IsDBNull(8) ? (int?)null : r.GetInt32(8),
                Название_проекта = r.IsDBNull(9) ? "" : r.GetString(9),
                Название_задачи = r.IsDBNull(10) ? "" : r.GetString(10),
                Название_подзадачи = r.IsDBNull(11) ? "" : r.GetString(11)
            };
        }

        // Добавление заметки
        public static void AddNote(NoteModel note)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                INSERT INTO Заметки (Заголовок, Текст_заметки, Цвет, Закреплена, Дата_создания, ID_проекта, ID_задачи, ID_подзадачи)
                VALUES (@title, @text, @color, @pinned, @date, @projId, @taskId, @subId)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", note.Заголовок ?? "Без названия");
                    cmd.Parameters.AddWithValue("@text", (object)note.Текст_заметки ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@color", string.IsNullOrWhiteSpace(note.Цвет) ? "Жёлтый" : note.Цвет);
                    cmd.Parameters.AddWithValue("@pinned", note.Закреплена);
                    cmd.Parameters.AddWithValue("@date", note.Дата_создания ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@projId", note.ID_проекта.HasValue ? (object)note.ID_проекта.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@taskId", note.ID_задачи.HasValue ? (object)note.ID_задачи.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@subId", note.ID_подзадачи.HasValue ? (object)note.ID_подзадачи.Value : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Обновление заметки
        public static void UpdateNote(NoteModel note)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                UPDATE Заметки
                SET Заголовок = @title,
                    Текст_заметки = @text,
                    Цвет = @color,
                    Закреплена = @pinned,
                    ID_проекта = @projId,
                    ID_задачи = @taskId,
                    ID_подзадачи = @subId
                WHERE ID_заметки = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", note.Заголовок ?? "Без названия");
                    cmd.Parameters.AddWithValue("@text", (object)note.Текст_заметки ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@color", string.IsNullOrWhiteSpace(note.Цвет) ? "Жёлтый" : note.Цвет);
                    cmd.Parameters.AddWithValue("@pinned", note.Закреплена);
                    cmd.Parameters.AddWithValue("@projId", note.ID_проекта.HasValue ? (object)note.ID_проекта.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@taskId", note.ID_задачи.HasValue ? (object)note.ID_задачи.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@subId", note.ID_подзадачи.HasValue ? (object)note.ID_подзадачи.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", note.ID_заметки);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Удаление заметки
        public static void DeleteNote(int noteId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Заметки WHERE ID_заметки = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", noteId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Переключение закрепления заметки
        public static void ToggleNotePin(int noteId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Заметки SET Закреплена = ~Закреплена WHERE ID_заметки = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", noteId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== РЕСУРСЫ ==========================

        // Получение всех ресурсов
        public static List<ResourceModel> GetAllResources()
        {
            List<ResourceModel> resources = new List<ResourceModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ID_ресурса, Название, Тип, Количество FROM Ресурсы";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resources.Add(new ResourceModel
                        {
                            ID_ресурса = reader.GetInt32(0),
                            Название = reader.GetString(1),
                            Тип = reader.GetString(2),
                            Количество = reader.GetInt32(3)
                        });
                    }
                }
            }
            return resources;
        }

        // Получение ресурсов, назначенных на проект
        public static List<ResourceModel> GetResourcesForProject(int projectId)
        {
            var resources = new List<ResourceModel>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"SELECT r.ID_ресурса, r.Название, r.Тип, r.Количество 
                              FROM Ресурсы r
                              JOIN Проект_Ресурсы pr ON r.ID_ресурса = pr.ID_ресурса
                              WHERE pr.ID_проекта = @projectId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resources.Add(new ResourceModel
                            {
                                ID_ресурса = reader.GetInt32(0),
                                Название = reader.GetString(1),
                                Тип = reader.GetString(2),
                                Количество = reader.GetInt32(3)
                            });
                        }
                    }
                }
            }
            return resources;
        }

        // Получение доступных ресурсов для проекта (не назначенных)
        public static List<ResourceModel> GetAvailableResourcesForProject(int projectId)
        {
            var resources = new List<ResourceModel>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"SELECT ID_ресурса, Название, Тип, Количество 
                              FROM Ресурсы 
                              WHERE ID_ресурса NOT IN (SELECT ID_ресурса FROM Проект_Ресурсы WHERE ID_проекта = @projectId)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resources.Add(new ResourceModel
                            {
                                ID_ресурса = reader.GetInt32(0),
                                Название = reader.GetString(1),
                                Тип = reader.GetString(2),
                                Количество = reader.GetInt32(3)
                            });
                        }
                    }
                }
            }
            return resources;
        }

        // Назначение ресурса на проект
        public static void AssignResourceToProject(int projectId, int resourceId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "INSERT INTO Проект_Ресурсы (ID_проекта, ID_ресурса) VALUES (@projectId, @resourceId)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    cmd.Parameters.AddWithValue("@resourceId", resourceId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Удаление ресурса из проекта
        public static void RemoveResourceFromProject(int projectId, int resourceId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "DELETE FROM Проект_Ресурсы WHERE ID_проекта = @projectId AND ID_ресурса = @resourceId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    cmd.Parameters.AddWithValue("@resourceId", resourceId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== БЮДЖЕТ ==========================

        // Получение бюджета проекта
        public static List<BudgetModel> GetBudgetByProjectId(int projectId)
        {
            List<BudgetModel> budgets = new List<BudgetModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    б.ID_бюджета,
                    б.ID_проекта,
                    б.Сумма,
                    б.Назначение,
                    б.Дата_создания,
                    п.Название_проекта
                FROM Бюджет б
                JOIN Проекты п ON б.ID_проекта = п.ID_проекта
                WHERE б.ID_проекта = @projectId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            budgets.Add(new BudgetModel
                            {
                                ID_бюджета = reader.GetInt32(0),
                                ID_проекта = reader.GetInt32(1),
                                Сумма = reader.GetDecimal(2),
                                Назначение = reader.GetString(3),
                                Дата_создания = reader.GetDateTime(4),
                                Название_проекта = reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return budgets;
        }

        // Добавление записи бюджета
        public static void AddBudgetEntry(BudgetModel budget)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "INSERT INTO Бюджет (ID_проекта, Сумма, Назначение) VALUES (@projectId, @amount, @purpose)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", budget.ID_проекта);
                    cmd.Parameters.AddWithValue("@amount", budget.Сумма);
                    cmd.Parameters.AddWithValue("@purpose", budget.Назначение);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Удаление записи бюджета
        public static void DeleteBudgetEntry(int budgetId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Бюджет WHERE ID_бюджета = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", budgetId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== ФАЙЛЫ ==========================

        // Получение файлов для задачи
        public static List<FileModel> GetFilesForTask(int taskId)
        {
            var files = new List<FileModel>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "SELECT ID_файла, Название_файла, Путь_к_файлу, ID_задачи FROM Файлы WHERE ID_задачи = @taskId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            files.Add(new FileModel
                            {
                                ID_файла = reader.GetInt32(0),
                                Название_файла = reader.GetString(1),
                                Путь_к_файлу = reader.GetString(2),
                                ID_задачи = reader.GetInt32(3)
                            });
                        }
                    }
                }
            }
            return files;
        }

        // Добавление файла
        public static void AddFile(FileModel file)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "INSERT INTO Файлы (Название_файла, Путь_к_файлу, ID_задачи) VALUES (@name, @path, @taskId)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", file.Название_файла);
                    cmd.Parameters.AddWithValue("@path", file.Путь_к_файлу);
                    cmd.Parameters.AddWithValue("@taskId", file.ID_задачи);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Удаление файла
        public static void DeleteFile(int fileId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "DELETE FROM Файлы WHERE ID_файла = @fileId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@fileId", fileId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========================== ИСТОРИЯ ИЗМЕНЕНИЙ ==========================

        // Логирование изменения
        public static void LogChange(string entity, int objectId, string action, int employeeId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"INSERT INTO История_Изменений (Сущность, ID_объекта, Действие, ID_сотрудника)
                      VALUES (@entity, @objectId, @action, @employeeId)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@entity", entity);
                    cmd.Parameters.AddWithValue("@objectId", objectId);
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@employeeId", employeeId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Получает историю изменений для указанного объекта
        // Список записей истории изменений
        public static List<HistoryModel> GetHistory(string entityType, int entityId)
        {
            List<HistoryModel> historyList = new List<HistoryModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    h.Дата_изменения,
                    h.Действие,
                    ISNULL(с.Фамилия + ' ' + с.Имя + ' ' + ISNULL(с.Отчество, ''), 'Неизвестно') AS Имя_сотрудника
                FROM История_Изменений h
                LEFT JOIN Сотрудники с ON h.ID_сотрудника = с.ID_сотрудника
                WHERE h.Сущность = @EntityType 
                  AND h.ID_объекта = @EntityId
                ORDER BY h.Дата_изменения DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EntityType", entityType);
                        cmd.Parameters.AddWithValue("@EntityId", entityId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                historyList.Add(new HistoryModel
                                {
                                    Дата_изменения = reader.GetDateTime(0),
                                    Действие = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    ФИО_сотрудника = reader.GetString(2)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке истории изменений:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return historyList;
        }

        // Получает всю историю изменений с возможностью фильтрации
        public static List<HistoryModel> GetAllHistory(string entityType = null, string employeeName = null,
    DateTime? startDate = null, DateTime? endDate = null)
        {
            List<HistoryModel> historyList = new List<HistoryModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Базовый запрос
                    string query = @"
                SELECT 
                    h.ID_изменения,
                    h.Сущность,
                    h.ID_объекта,
                    h.Действие,
                    h.Дата_изменения,
                    h.ID_сотрудника,
                    ISNULL(с.Фамилия + ' ' + с.Имя + ' ' + ISNULL(с.Отчество, ''), 'Неизвестно') AS ФИО_сотрудника
                FROM История_Изменений h
                LEFT JOIN Сотрудники с ON h.ID_сотрудника = с.ID_сотрудника
                WHERE 1=1";

                    // Добавляем фильтры
                    if (!string.IsNullOrEmpty(entityType))
                    {
                        query += " AND h.Сущность = @EntityType";
                    }

                    if (!string.IsNullOrEmpty(employeeName))
                    {
                        query += " AND (с.Фамилия LIKE @EmployeeName OR с.Имя LIKE @EmployeeName OR с.Отчество LIKE @EmployeeName)";
                    }

                    if (startDate.HasValue)
                    {
                        query += " AND h.Дата_изменения >= @StartDate";
                    }

                    if (endDate.HasValue)
                    {
                        query += " AND h.Дата_изменения <= @EndDate";
                    }

                    query += " ORDER BY h.Дата_изменения DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Добавляем параметры
                        if (!string.IsNullOrEmpty(entityType))
                        {
                            cmd.Parameters.AddWithValue("@EntityType", entityType);
                        }

                        if (!string.IsNullOrEmpty(employeeName))
                        {
                            cmd.Parameters.AddWithValue("@EmployeeName", "%" + employeeName + "%");
                        }

                        if (startDate.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
                        }

                        if (endDate.HasValue)
                        {
                            // Устанавливаем конец дня для endDate
                            cmd.Parameters.AddWithValue("@EndDate", endDate.Value.Date.AddDays(1).AddSeconds(-1));
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                historyList.Add(new HistoryModel
                                {
                                    ID_изменения = reader.GetInt32(0),
                                    Сущность = reader.GetString(1),
                                    ID_объекта = reader.GetInt32(2),
                                    Действие = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Дата_изменения = reader.GetDateTime(4),
                                    ID_сотрудника = reader.GetInt32(5),
                                    ФИО_сотрудника = reader.GetString(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке истории изменений:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return historyList;
        }

        // ========================== ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ПРОФИЛЯ ==========================

        // Получение отдела сотрудника по ID
        public static string GetEmployeeDepartment(int employeeId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Отдел FROM Сотрудники WHERE ID_сотрудника = @employeeId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@employeeId", employeeId);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        // Обновление профиля сотрудника
        public static void UpdateEmployeeProfile(int employeeId, string lastName, string firstName, string middleName, string department)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            UPDATE Сотрудники 
            SET Фамилия = @lastName, 
                Имя = @firstName, 
                Отчество = @middleName, 
                Отдел = @department
            WHERE ID_сотрудника = @employeeId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@lastName", lastName);
                    cmd.Parameters.AddWithValue("@firstName", firstName);
                    cmd.Parameters.AddWithValue("@middleName", string.IsNullOrEmpty(middleName) ? (object)DBNull.Value : middleName);
                    cmd.Parameters.AddWithValue("@department", department);
                    cmd.Parameters.AddWithValue("@employeeId", employeeId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Получение последней активности пользователя из истории изменений
        public static List<object> GetRecentUserActivity(int employeeId, int count = 10)
        {
            var activities = new List<object>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT TOP (@count)
                Дата_изменения,
                Сущность + ': ' + Действие AS Действие
                FROM История_Изменений
                WHERE ID_сотрудника = @employeeId
                ORDER BY Дата_изменения DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@count", count);
                    cmd.Parameters.AddWithValue("@employeeId", employeeId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new
                            {
                                Дата = reader.GetDateTime(0).ToString("dd.MM.yyyy HH:mm"),
                                Действие = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return activities;
        }


    }
}