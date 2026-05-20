using DevExpress.Xpo;
using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, UserSession> ActiveSessions = new ConcurrentDictionary<string, UserSession>();
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

            if (ActiveSessions.TryGetValue(sessionId, out UserSession session))
            {
                session.LastActivity = DateTime.Now;
                return true;
            }
            return false;
        }

        public static UserModel GetUser(string sessionId)
        {
            if (ValidateSession(sessionId) && ActiveSessions.TryGetValue(sessionId, out UserSession session))
            {
                return session.User;
            }
            return null;
        }

        public static void Logout(string sessionId)
        {
            ActiveSessions.TryRemove(sessionId, out _);
        }

        public static void UpdateUserData(string sessionId, UserModel updatedUser)
        {
            if (ActiveSessions.TryGetValue(sessionId, out UserSession session))
            {
                session.User = updatedUser;
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
                ActiveSessions.TryRemove(sessionId, out _);
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