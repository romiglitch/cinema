
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;

namespace Shipping
{
    public class User
    {
        public string Fullname { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool isAdmin { get; set; }

        public User( string fullname, string password, string phone, string email, bool isAdmin)
        {
            Fullname = fullname;
            Password = password;
            Phone = phone;
            // אימייל מנורמל (אותיות קטנות) בכל יצירת משתמש
            Email = EmailHelper.Normalize(email);
            this.isAdmin = isAdmin;
        }
        public void CreateUser(string con, string com)
        {
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                SqlCommand sqlCommand = new SqlCommand(com, connection);
                sqlCommand.ExecuteNonQuery();//מבצעת את השאילתא ומחזירה את מספר השורות שהושפעו
            }

        }
    }
}