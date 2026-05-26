using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.UI;
using System.Diagnostics;

namespace Shipping
{
    public partial class ForgotPassword1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // אם הגיענו מהדף Login עם פרמטר email – נמלא אותו אוטומטית בתיבת הטקסט.
                string fromLogin = Request.QueryString["email"];
                if (!string.IsNullOrWhiteSpace(fromLogin))
                {
                    txtEmail.Text = fromLogin;
                }
            }
        }
        protected void btnSend_Click(object sender, EventArgs e)
        {
            // כדי לאפשר שליחת מייל בלי לתקוע את הדף async
            RegisterAsyncTask(new PageAsyncTask(async () =>
            {
                // נרמול אימייל כדי להתאים לאופן השמירה והחיפוש במסד (קיצוץ + אותיות קטנות).
                string userEmail = EmailHelper.Normalize(txtEmail.Text);

                if (string.IsNullOrEmpty(userEmail))
                {
                    lblStatus.Text = "אנא הזיני כתובת מייל.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    return;
                }

                try
                {
                    string token = Guid.NewGuid().ToString();//(טיפוס נתונים שמייצר מזהה ייחודי) Guid יצירת קוד בעזרת
                                                             //הקוד משמש כמפתח זמני וחד-פעמי שמאפשר למשתמש לשנות את הסיסמה שלו.
                                                             //כשהדף יבדוק שיש התאמה בין הקוד שבלינק לקוד שנשמר במסד נתונים ResetPassword הוא ישמש לבדיקה בעמוד 
                    // בניית הקישור לפי השרת המקומי שרץ כרגע
                    string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                    string resetLink = baseUrl + "/ResetPassword.aspx?token=" + token;


                    string connString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        //שמירת הקוד במוסד נתונים
                        string query = "UPDATE Users SET ResetToken = @token, TokenExpiry = @expiry WHERE Email = @email";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@token", token);
                        cmd.Parameters.AddWithValue("@expiry", DateTime.Now.AddMinutes(15)); // תוקף ל-15 דקות
                        cmd.Parameters.AddWithValue("@email", userEmail);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();//הרצת השאילתא ושמירה של מספר השורות שעודכנו

                        if (rowsAffected > 0)//בדיקה שאכן נמצא משתמש רשום עם האימייל
                        {
                            EmailService mailService = new EmailService();
                            await mailService.SendResetPasswordEmail(userEmail, resetLink);//בזמן שהמייל נשלח השרת מטפל במשתמשים אחרים
                            lblStatus.Text = "מייל שחזור נשלח בהצלחה!"; 
                            lblStatus.CssClass = "no-screenings-msg";

                        }
                        else
                        {
                            lblStatus.Text = "המייל לא נמצא במערכת.";
                            lblStatus.CssClass = "msg-label";
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "חלה שגיאה זמנית במערכת, אנא נסו שוב מאוחר יותר.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    Debug.WriteLine("Error: " + ex.ToString());
                }
            }));
        }
    }
}