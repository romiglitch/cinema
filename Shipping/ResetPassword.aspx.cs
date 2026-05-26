using System;
using System.Data.SqlClient; 
using System.Configuration;

namespace Shipping
{
    // עמוד איפוס סיסמה - מאפשר למשתמש לבחור סיסמה חדשה לאחר קבלת קישור שחזור במייל
    public partial class ResetPassword : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // ForgotPasswordבדיקה אם הגיע טוקן בכתובת - כלומר שלהקוח הגיע מ
            if (string.IsNullOrEmpty(Request.QueryString["token"]))
            {
                lblMessage.Text = "קישור לא תקין.";
                btnUpdate.Enabled = false; // משביתים את הכפתור כדי שלא ניתן יהיה לשלוח ללא טוקן
            }
        }

        // לחיצה על כפתור עדכון הסיסמה - מבצעת את עדכון הסיסמה בבסיס הנתונים
        protected void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            string token = Request.QueryString["token"]; // שליפת הטוקן מהקישור
            string newPass = txtNewPassword.Text; // הסיסמה החדשה שהמשתמש הזין
            string confirmPass = txtConfirmPassword.Text; // אימות סיסמה

            if (newPass != confirmPass)
            {
                // הגנה נוספת בצד השרת גם אם ה-CompareValidator לא רץ (למשל JS כבוי)
                lblMessage.Text = "אימות הסיסמה לא תואם לסיסמה החדשה.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connString))
            {
                // מעדכנים את הסיסמה רק אם הטוקן קיים ועדיין בתוקף (לא פג)
                // לאחר העדכון הטוקן והתפוגה מתעדכנים לנאל למניעת שימוש חוזר
                string query = "UPDATE Users SET Password = @pass, ResetToken = NULL, TokenExpiry = NULL " +
                               "WHERE ResetToken = @token AND TokenExpiry > @now";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@pass", newPass);
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@now", DateTime.Now); // השוואה לשעה הנוכחית לבדיקת תוקף

                conn.Open();
                int rows = cmd.ExecuteNonQuery(); // מספר השורות שעודכנו

                if (rows > 0)
                {
                    lblMessage.Text = "הסיסמה עודכנה! את יכולה להתחבר.";
                    lblMessage.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    // לא נמצאה שורה תואמת - הטוקן פג תוקף או שכבר נעשה בו שימוש
                    lblMessage.Text = "הקישור לא בתוקף או שכבר נעשה בו שימוש.";
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                }
            }
        }
    }
}