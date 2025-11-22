using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Helpers
{
    public static class PermissionHelper
    {
        // Статическое хранилище разрешений для каждой роли
        private static readonly Dictionary<string, HashSet<string>> RolePermissions = new Dictionary<string, HashSet<string>>
        {
            {
                "Администратор", new HashSet<string>
                {
                    "PROJECT_CREATE", "PROJECT_EDIT", "PROJECT_DELETE",
                    "TASK_CREATE", "TASK_EDIT", "TASK_DELETE",
                    "USER_MANAGE", "REPORTS_VIEW", "SETTINGS_MANAGE",
                    "EVENT_MANAGE", "REMINDER_MANAGE", "NOTE_MANAGE",
                    "RESOURCE_MANAGE", "BUDGET_MANAGE"
                }
            },
            {
                "Менеджер", new HashSet<string>
                {
                    "PROJECT_CREATE", "PROJECT_EDIT",
                    "TASK_CREATE", "TASK_EDIT", "TASK_DELETE",
                    "REPORTS_VIEW", "EVENT_MANAGE", "REMINDER_MANAGE",
                    "NOTE_MANAGE", "RESOURCE_MANAGE", "BUDGET_MANAGE"
                }
            },
            {
                "Пользователь", new HashSet<string>
                {
                    "TASK_CREATE", "TASK_EDIT", "TASK_DELETE",
                    "EVENT_MANAGE", "REMINDER_MANAGE", "NOTE_MANAGE"
                }
            }
        };

        public static bool HasPermission(UserModel user, string permissionCode)
        {
            if (user == null) return false;

            return RolePermissions.ContainsKey(user.Роль) &&
                   RolePermissions[user.Роль].Contains(permissionCode);
        }

        public static bool CanEditProject(UserModel user, ProjectModel project = null)
        {
            // Администраторы могут редактировать любые проекты
            if (user.Роль == "Администратор") return true;

            // Менеджеры могут редактировать любые проекты
            if (user.Роль == "Менеджер") return true;

            // Пользователи могут редактировать только проекты, где они ответственные
            if (user.Роль == "Пользователь" && project != null)
            {
                return project.ID_ответственного == user.ID_сотрудника;
            }

            return false;
        }

        public static bool CanEditTask(UserModel user, TaskModel task = null)
        {
            // Администраторы могут редактировать любые задачи
            if (user.Роль == "Администратор") return true;

            // Менеджеры могут редактировать любые задачи
            if (user.Роль == "Менеджер") return true;

            // Пользователи могут редактировать только свои задачи
            if (user.Роль == "Пользователь" && task != null)
            {
                return task.ID_ответственного == user.ID_сотрудника;
            }

            return false;
        }
    }
}
