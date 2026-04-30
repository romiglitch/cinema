using DALLlilbrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Shipping
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
        protected void BtnSign_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                try
                {            string pass = TxtPassword.Text;

                    string fullname = TxtName.Text;
                    string email = TxtEmail.Text;
                    string phone = TxtPhone.Text;
                    bool isAdmin = false; // ברירת מחדל למשתמש חדש

                    // שאילתת ה-SQL המתוקנת עם שמות העמודות
                    string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString; string com = $@"INSERT INTO Users (FullName, Password, Phone, Email, IsAdmin) 
                    VALUES ('{fullname}', '{pass}', '{phone}', '{email}', '{isAdmin}')";

                    User newUser = new User(fullname, pass, phone, email, isAdmin);
                    newUser.CreateUser(connectionString, com);

                    // --- כאן החלק הקריטי לחיבור המשתמש ---
                    // שימי לב: המפתחות חייבים להיות בכתב קטן בדיוק כמו ב-Master Page
                    Session["username"] = fullname;
                    Session["category"] = "user"; // מחרוזת פשוטה כי ה-Master בודק .ToString() == "admin"

                    // העברה לדף הבית
                    Response.Redirect("HomePage.aspx");
                }
                catch (Exception ex)
                {
                    msg.Text = "שגיאה ברישום: " + ex.Message;
                }
            }
        }
    }
}