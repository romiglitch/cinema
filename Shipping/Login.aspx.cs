using DALLlilbrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Shipping
{
    public partial class Login1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = TxtName.Text;
            string password = TxtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                LblMsg.Text = "יש להזין שם משתמש וסיסמא";
                return;
            }

            // טיפול במנהל מערכת (Admin)
            if (username == "admin" && password == "1234")
            {
                Session["category"] = "admin";
                Session["username"] = "admin";
                Response.Redirect("AdminPage.aspx");
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string strQuery = "SELECT UserId, Email, FullName FROM Users WHERE FullName=@name AND Password=@pass";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(strQuery, con);
                cmd.Parameters.AddWithValue("@name", username);//על ידי שליחת שם המשתמש כפרמטר מאובטח ולא כטקסט חופשי SQL Injection הגנה מפני התקפות
                cmd.Parameters.AddWithValue("@pass", password);

                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();//יצירת אובייקט קורא שמכיל את התוצאות שחזרו מהטבלה לאחר הקומנד

                if (rdr.Read())//אם נמצאה שורה תואמת - כלומר אם נמצא משתמש שתואם את השם והסיסמא
                {
                    // שמירת נתוני המשתמש בסשן
                    Session["UserId"] = rdr["UserId"];//שמירת המזהה הייחודי של המשתמש לצורך ביצוע רכישות בהמשך
                    Session["UserEmail"] = rdr["Email"];//שמירת המייל כדי שנוכל לשלוח אישור הזמנה בסיום הרכישה
                    Session["username"] = rdr["FullName"];//שמירת השם המלא לצורך הצגה
                    Session["category"] = "user";// הבדלה בין מנהל למשתמש

                    // --- כאן הלוגיקה של ה-Return URL ---
                    string returnUrl = Request.QueryString["returnUrl"];

                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        //בדיקה אם המשתמש הגיע מעמוד אחר 
                        Response.Redirect(returnUrl);
                    }
                    else
                    {
                        
                        Response.Redirect("Movies.aspx");
                    }
                }
                else
                {
                    LblMsg.Text = "שם משתמש או סיסמא לא נכונים";
                }
            }
        }

        protected void BtnForgotPassword_Click(object sender, EventArgs e)
        {
            Response.Redirect("ForgotPassword.aspx");
        }
    }
}