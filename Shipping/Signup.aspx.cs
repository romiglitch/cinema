using DALLlilbrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Diagnostics;
using System.Web.UI.WebControls;

namespace Shipping
{
    // עמוד הרשמה - מאפשר למשתמש חדש ליצור חשבון ולהתחבר מיד לאחר מכן
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
           
        }

        // לחיצה על כפתור ההרשמה - מאמת את הטופס, יוצר משתמש חדש ומעביר לדף הבית
        protected void BtnSign_Click(object sender, EventArgs e)
        {
            if (Page.IsValid) // בדיקת תקינות שדות הטופס לפני ביצוע פעולה
            {
                try
                {
                    // שליפת הנתונים שהמשתמש הזין בטופס
                    string pass = TxtPassword.Text;
                    string fullname = TxtName.Text;
                    string email = TxtEmail.Text;
                    string phone = TxtPhone.Text;
                    bool isAdmin = false; // ברירת מחדל למשתמש חדש - לא מנהל

                    //Users להוספת המשתמש לטבלת INSERT בניית שאילתת   
                    string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                    string com = $@"INSERT INTO Users (FullName, Password, Phone, Email, IsAdmin) 
                    VALUES ('{fullname}', '{pass}', '{phone}', '{email}', '{isAdmin}')";

                    //יצירת אובייקט משתמש ושמירתו בבסיס הנתונים דרך מחלקת יוזר  
                    User newUser = new User(fullname, pass, phone, email, isAdmin);
                    newUser.CreateUser(connectionString, com);

                    //לאחר הרשמה מוצלחת - המשתמש מחובר מיד Session הגדרת 
                    //בודק  Master Page המפתחות חייבים להיות זהים לאלה שה
                    Session["username"] = fullname;
                    Session["category"] = "user"; // ערך "user" מבדיל בין משתמש רגיל למנהל ("admin")

                    // העברה לדף הבית לאחר הרשמה מוצלחת
                    Response.Redirect("HomePage.aspx");
                }
                catch (Exception ex)
                {
                    // הצגת הודעת שגיאה אם ההרשמה נכשלה 
                    msg.Text = "שגיאה ברישום";
                    Debug.WriteLine(ex.ToString());
                }
            }
        }
    }
}