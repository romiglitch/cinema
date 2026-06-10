using System.Diagnostics;
using System;
using System.Configuration;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Shipping
{
    // עמוד בחירת כרטיסים - המשתמש בוחר כמה כרטיסים מכל סוג (רגיל, סטודנט וכו') לפני בחירת מושבים
    public partial class Ticketing : System.Web.UI.Page
    {
        private const int MaxTicketsPerPurchase = 10;

        protected int screeningId; // -URLמזהה ההקרנה שהועבר בכתובת ה
        protected int freeSeatsAvailable; // מקומות פנויים שנותרו להקרנה
        protected int maxTicketsAllowed; // המינימום בין מכסת רכישה למקומות פנויים

        protected void Page_Load(object sender, EventArgs e)
        {
            // אם לא תקין, מציגים הודעת שגיאה .URLשליפת מזהה ההקרנה מה
            if (!int.TryParse(Request.QueryString["screeningId"], out screeningId))
            {
                litScreeningInfo.Text = "<div style='color:red;'>לא נבחרה הקרנה תקינה.</div>";
            }
            else if (!IsPostBack)
            {
                freeSeatsAvailable = GetAvailableSeatsForScreening(screeningId);
                maxTicketsAllowed = Math.Min(MaxTicketsPerPurchase, freeSeatsAvailable);
                LoadScreeningDetails();

                if (freeSeatsAvailable <= 0)
                {
                    litScreeningInfo.Text += "<div style='color:#c0392b;margin-top:10px;font-weight:bold;'>אזלו הכרטיסים להקרנה זו.</div>";
                    RepeaterTickets.Visible = false;
                    btnContinue.Visible = false;
                }
                else
                {
                    LoadTickets();
                }
            }
        }

        // מחזיר כמה מושבים פנויים נותרו להקרנה (קיבולת האולם פחות כרטיסים שנמכרו)
        private int GetAvailableSeatsForScreening(int sId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = @"
                SELECT (h.Rows * h.SeatsPerRow) - ISNULL((SELECT COUNT(*) FROM Tickets WHERE Screening = @sId), 0)
                FROM Screening s
                JOIN Halls h ON s.Hall = h.HallId
                WHERE s.ScreeningId = @sId";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@sId", sId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }

        // שולפת שם הסרט ושעת ההקרנה ומציגה אותם בראש העמוד
        private void LoadScreeningDetails()
        {
            var configSetting = ConfigurationManager.ConnectionStrings["ConnectionString"];
            if (configSetting == null)
            {
                litScreeningInfo.Text = "שגיאה: מחרוזת ההתחברות לא נמצאה.";
                return;
            }

            string connectionString = configSetting.ConnectionString;

            // לפי מזהה ההקרנה Screening ו-Movie שאילתה המאחדת את
            string query = @"SELECT m.Title, s.StartTime 
                     FROM Screening s 
                     JOIN Movie m ON s.MovieId = m.Id 
                     WHERE s.ScreeningId = @sId";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@sId", screeningId);

                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();//שמירת התוצאות באובייקט קורא שמאפשר לעבור עליהן שורה אחר שורה
                    if (reader.Read())
                    {
                        string movieTitle = reader["Title"].ToString();
                        DateTime screeningTime = Convert.ToDateTime(reader["StartTime"]);

                        // הצגת שם הסרט והזמן בראש העמוד בפורמט dd/MM/yyyy HH:mm
                        litScreeningInfo.Text = $"<h2 style='color:black; margin:0;'>{movieTitle}</h2>" +
                                               $"<p style='color:#666; margin:5px 0;'>{screeningTime.ToString("dd/MM/yyyy HH:mm")}</p>";
                    }
                    else
                    {
                        litScreeningInfo.Text = "<div style='color:red;'>לא נמצאו פרטים להקרנה זו.</div>";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: " +ex.ToString());
                    litScreeningInfo.Text = "שגיאה בשליפת נתונים";
                }
            }
        }

        //ומקשרת אותם לריפיטר TicketsPricing טוענת את סוגי הכרטיסים והמחירים מטבלת  
        private void LoadTickets()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = "SELECT Id, PersonType, Price FROM TicketsPricing ORDER BY Id";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                adapter.Fill(dt);
                RepeaterTickets.DataSource = dt;
                RepeaterTickets.DataBind();
            }
        }

        //ומעבירה לבחירת מושבים Sessionלחיצה על "המשך" - מחשבת סה"כ כרטיסים ומחיר, שומרת ב
        protected void btnContinue_Click(object sender, EventArgs e)
        {
            int totalTickets = 0;
            decimal totalPrice = 0;// משתנה דצימלי המאפשר לשמור על דיוק בחישובים כספיים

            List<string> ticketTypes = new List<string>();
            List<string> ticketPrices = new List<string>();

            // עוברים על כל שורה בריפיטר ואוספים את הכמויות שנבחרו
            foreach (RepeaterItem item in RepeaterTickets.Items)
            {
                HiddenField hfQty = (HiddenField)item.FindControl("hiddenQty");//שליפת נתונים חבויים
                HiddenField hfPrice = (HiddenField)item.FindControl("hiddenPrice");
                HiddenField hfType = (HiddenField)item.FindControl("hiddenType"); // סוג הכרטיס (רגיל / סטודנט / ילד)

                if (hfQty != null && hfPrice != null && hfType != null)
                {
                    int qty = int.TryParse(hfQty.Value, out int q) ? q : 0;
                    decimal price = decimal.TryParse(hfPrice.Value, out decimal p) ? p : 0;// משתנה דצימלי המאפשר לשמור על דיוק בחישובים כספיים

                    if (qty > 0)
                    {
                        totalTickets += qty;
                        totalPrice += price * qty;

                        // שומר את סוג ומחיר כל כרטיס בנפרד לפי הכמות שנבחרה
                        for (int i = 0; i < qty; i++)
                        {
                            ticketTypes.Add(hfType.Value);
                            ticketPrices.Add(price.ToString("F2"));
                        }
                    }
                }
            }

            // אין לאפשר המשך ללא בחירת כרטיס אחד לפחות
            if (totalTickets == 0)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('אנא בחרי לפחות כרטיס אחד');", true);
                return;
            }

            if (totalTickets > MaxTicketsPerPurchase)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "alert", $"alert('ניתן לרכוש עד {MaxTicketsPerPurchase} כרטיסים בהזמנה אחת.');", true);
                return;
            }

            int availableSeats = GetAvailableSeatsForScreening(screeningId);
            if (totalTickets > availableSeats)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "alert",
                    $"alert('נותרו רק {availableSeats} מקומות פנויים להקרנה זו.');", true);
                return;
            }

            //להמשך תהליך הרכישה בעמודים הבאים Sessionשמירת הנתונים ב
            Session["TotalTickets"] = totalTickets;
            Session["TotalPrice"] = totalPrice;
            Session["TicketTypes"] = string.Join(",", ticketTypes);   // רשימת סוגים מופרדת בפסיקים
            Session["TicketPrices"] = string.Join(",", ticketPrices); // רשימת מחירים מופרדת בפסיקים

            // URLהעברה לעמוד בחירת המושבים עם מזהה ההקרנה ב
            string sId = Request.QueryString["screeningId"] ?? Request.QueryString["ScreeningId"] ?? "";
            Response.Redirect("SeatsPicker.aspx?screeningId=" + sId);
        }

    }
}


