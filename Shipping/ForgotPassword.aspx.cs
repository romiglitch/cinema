using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.UI;

namespace Shipping
{
    public partial class ForgotPassword1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // קוד שירוץ בטעינת העמוד (אם צריך)
        }

        // הוספנו async כדי לאפשר שליחת מייל בלי לתקוע את הדף
        protected void btnSend_Click(object sender, EventArgs e)
        {
            // אנחנו רושמים משימה אסינכרונית שהדף צריך לבצע
            RegisterAsyncTask(new PageAsyncTask(async () =>
            {
                string userEmail = txtEmail.Text.Trim();

                if (string.IsNullOrEmpty(userEmail))
                {
                    lblStatus.Text = "אנא הזיני כתובת מייל.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    return;
                }

                try
                {
                    string token = Guid.NewGuid().ToString();
                    // הקוד הזה בונה את הכתובת לפי השרת המקומי שרץ אצלך כרגע
                    string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                    string resetLink = baseUrl + "/ResetPassword.aspx?token=" + token;

                    // חיבור ל-DB ושמירת הטוקן (החליפי את מחרוזת החיבור לשלך)
                    string connString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        // אנחנו מעדכנים את המשתמש לפי המייל שלו
                        string query = "UPDATE Users SET ResetToken = @token, TokenExpiry = @expiry WHERE Email = @email";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@token", token);
                        cmd.Parameters.AddWithValue("@expiry", DateTime.Now.AddMinutes(15)); // תוקף ל-15 דקות
                        cmd.Parameters.AddWithValue("@email", userEmail);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            EmailService mailService = new EmailService();
                            await mailService.SendResetPasswordEmail(userEmail, resetLink);
                            lblStatus.Text = "מייל שחזור נשלח בהצלחה!";
                            lblStatus.ForeColor = System.Drawing.Color.Green;
                        }
                        else
                        {
                            lblStatus.Text = "המייל לא נמצא במערכת.";
                            lblStatus.ForeColor = System.Drawing.Color.Red;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "שגיאה: " + ex.Message;
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }));
        }
    }
}