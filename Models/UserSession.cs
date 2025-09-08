using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Coftea_Capstone.C_
{
    public class UserSession
    {
        private static UserSession _instance;
        public static UserSession Instance => _instance ??= new UserSession();

        // Store current user info
        public string Email { get; private set; }
        public bool IsAdmin { get; private set; }

        // Set the current user
        public void SetUser(string email, bool isAdmin)
        {
            Email = email;
            IsAdmin = isAdmin;
        }

        // Clear user info (logout)
        public void Clear()
        {
            Email = null;
            IsAdmin = false;
        }
    }
}