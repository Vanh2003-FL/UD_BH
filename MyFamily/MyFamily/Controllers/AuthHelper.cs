using System;
using System.Web;
using MyFamily.Models;

namespace MyFamily.Controllers
{
    /// <summary>
    /// Helper class for authentication and authorization
    /// </summary>
    public static class AuthHelper
    {
        /// <summary>
        /// Get the current logged-in user ID from authentication cookie
        /// Returns null if user is not authenticated
        /// Cookie format: "username|UserId|Role"
        /// </summary>
        public static int? GetCurrentUserId()
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                return null;

            try
            {
                var ticket = HttpContext.Current.User.Identity.Name;
                if (string.IsNullOrEmpty(ticket))
                    return null;

                var parts = ticket.Split('|');
                // Format: username|UserId|Role
                if (parts.Length > 1 && int.TryParse(parts[1], out int userId))
                {
                    return userId;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Get the current logged-in user's role from authentication cookie
        /// Cookie format: "username|UserId|Role"
        /// </summary>
        public static string GetCurrentUserRole()
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                return null;

            try
            {
                var ticket = HttpContext.Current.User.Identity.Name;
                if (string.IsNullOrEmpty(ticket))
                    return null;

                var parts = ticket.Split('|');
                // Format: username|UserId|Role
                if (parts.Length > 2)
                {
                    return parts[2];
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Get the current logged-in user's username from authentication cookie
        /// Cookie format: "username|UserId|Role"
        /// </summary>
        public static string GetCurrentUsername()
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                return null;

            try
            {
                var ticket = HttpContext.Current.User.Identity.Name;
                if (string.IsNullOrEmpty(ticket))
                    return null;

                var parts = ticket.Split('|');
                // Format: username|UserId|Role
                if (parts.Length > 0)
                {
                    return parts[0];
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Check if current user is admin
        /// </summary>
        public static bool IsAdmin()
        {
            var role = GetCurrentUserRole();
            return !string.IsNullOrEmpty(role) && role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
