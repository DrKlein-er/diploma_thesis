using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Командное_управление_проектами.Models;

namespace Командное_управление_проектами.Helpers
{
    public static class SessionManager
    {
        private static readonly Dictionary<string, UserSession> ActiveSessions = new Dictionary<string, UserSession>();
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromHours(2);

        public static string CreateSession(UserModel user)
        {
            CleanExpiredSessions();

            string sessionId = Guid.NewGuid().ToString();
            ActiveSessions[sessionId] = new UserSession
            {
                UserId = user.ID,
                User = user,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                SessionId = sessionId
            };

            return sessionId;
        }

        public static bool ValidateSession(string sessionId)
        {
            CleanExpiredSessions();

            if (ActiveSessions.ContainsKey(sessionId))
            {
                ActiveSessions[sessionId].LastActivity = DateTime.Now;
                return true;
            }
            return false;
        }

        public static UserModel GetUser(string sessionId)
        {
            if (ValidateSession(sessionId))
            {
                return ActiveSessions[sessionId].User;
            }
            return null;
        }

        public static void Logout(string sessionId)
        {
            if (ActiveSessions.ContainsKey(sessionId))
            {
                ActiveSessions.Remove(sessionId);
            }
        }

        public static void UpdateUserData(string sessionId, UserModel updatedUser)
        {
            if (ActiveSessions.ContainsKey(sessionId))
            {
                ActiveSessions[sessionId].User = updatedUser;
            }
        }

        private static void CleanExpiredSessions()
        {
            var expiredSessions = ActiveSessions
                .Where(s => DateTime.Now - s.Value.LastActivity > SessionTimeout)
                .Select(s => s.Key)
                .ToList();

            foreach (var sessionId in expiredSessions)
            {
                ActiveSessions.Remove(sessionId);
            }
        }

        public static int GetActiveSessionsCount()
        {
            CleanExpiredSessions();
            return ActiveSessions.Count;
        }
    }

    public class UserSession
    {
        public string SessionId { get; set; }
        public int UserId { get; set; }
        public UserModel User { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }
    }
}