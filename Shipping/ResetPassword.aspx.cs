using System;
using System.Data.SqlClient; // או מה שאת משתמשת בו ל-DB
using System.Configuration;

namespace Shipping
{
    public partial class ResetPassword : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // בדיקה אם הגיע טוקן בכתובת
            if (string.IsNullOrEmpty(Request.QueryString["token"]))
            {
                lblMessage.Text = "קישור לא תקין.";
                btnUpdate.Enabled = false;
            }
        }

        protected void btnUpdate_Click(object sender, EventArgs e)
        {
            string token = Request.QueryString["token"];
            string newPass = txtNewPassword.Text;

            string connString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connString))
            {
                // בדיקה שהטוקן קיים ושלא עבר הזמן
                string query = "UPDATE Users SET Password = @pass, ResetToken = NULL, TokenExpiry = NULL " +
                               "WHERE ResetToken = @token AND TokenExpiry > @now";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@pass", newPass);
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@now", DateTime.Now);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();

                if (rows > 0)
                {
                    lblMessage.Text = "הסיסמה עודכנה! את יכולה להתחבר.";
                    lblMessage.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblMessage.Text = "הקישור לא בתוקף או שכבר נעשה בו שימוש.";
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                }
            }
        }

        private void UpdatePasswordInDb(string token, string newPassword)
        {
            // הערה: זהו קוד כללי, את צריכה להתאים אותו לטבלה שלך (Users)
            // את צריכה למצוא את המשתמש שהטוקן שלו שווה לטוקן שקיבלנו

            /* דוגמה קטנה ב-SQL:
            string connString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "UPDATE Users SET Password = @pass WHERE ResetToken = @token";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@pass", newPassword); // מומלץ להצפין סיסמה!
                cmd.Parameters.AddWithValue("@token", token);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0) {
                    lblMessage.Text = "הסיסמה עודכנה בהצלחה! אפשר להתחבר.";
                } else {
                    lblMessage.Text = "הטוקן פג תוקף או לא קיים.";
                }
            }
            */

            lblMessage.Text = "סיסמה עודכנה בהצלחה (כאן יבוא הקוד של ה-DB שלך)";
            lblMessage.ForeColor = System.Drawing.Color.Green;
        }
    }
}