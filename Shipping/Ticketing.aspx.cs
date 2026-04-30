
using System;
using System.Configuration;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Shipping
{
    public partial class Ticketing : System.Web.UI.Page
    {
        protected int screeningId;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!int.TryParse(Request.QueryString["screeningId"], out screeningId))
            {
                litScreeningInfo.Text = "<div style='color:red;'>לא נבחרה הקרנה תקינה.</div>";
            }

            if (!IsPostBack)
            {
                LoadScreeningDetails();
                LoadTickets();
            }
        }
        private void LoadScreeningDetails()
        {
            var configSetting = ConfigurationManager.ConnectionStrings["ConnectionString"];
            if (configSetting == null)
            {
                litScreeningInfo.Text = "שגיאה: מחרוזת ההתחברות לא נמצאה.";
                return;
            }

            string connectionString = configSetting.ConnectionString;

            // שאילתה מעודכנת לפי שמות העמודות בטבלאות שלך:
            // ב-Screening זה ScreeningId ו-MovieId
            // ב-Movie זה Id ו-Title
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
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        string movieTitle = reader["Title"].ToString();
                        DateTime screeningTime = Convert.ToDateTime(reader["StartTime"]);

                        // הצגת שם הסרט והזמן בראש העמוד
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
                    litScreeningInfo.Text = "שגיאה בשליפת נתונים: " + ex.Message;
                }
            }
        }
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


        protected void btnContinue_Click(object sender, EventArgs e)
        {
            int totalTickets = 0;
            decimal totalPrice = 0;

            List<string> ticketTypes = new List<string>();
            List<string> ticketPrices = new List<string>();

            foreach (RepeaterItem item in RepeaterTickets.Items)
            {
                HiddenField hfQty = (HiddenField)item.FindControl("hiddenQty");
                HiddenField hfPrice = (HiddenField)item.FindControl("hiddenPrice");
                HiddenField hfType = (HiddenField)item.FindControl("hiddenType"); // הוסיפי בשורה הרלוונטית בריפיטר

                if (hfQty != null && hfPrice != null && hfType != null)
                {
                    int qty = int.TryParse(hfQty.Value, out int q) ? q : 0;
                    decimal price = decimal.TryParse(hfPrice.Value, out decimal p) ? p : 0;

                    if (qty > 0)
                    {
                        totalTickets += qty;
                        totalPrice += price * qty;

                        // שומר את סוג ומחיר כל כרטיס לפי הכמות
                        for (int i = 0; i < qty; i++)
                        {
                            ticketTypes.Add(hfType.Value);
                            ticketPrices.Add(price.ToString("F2"));
                        }
                    }
                }
            }

            if (totalTickets == 0)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('אנא בחרי לפחות כרטיס אחד');", true);
                return;
            }

            // שמירה ל-Session
            Session["TotalTickets"] = totalTickets;
            Session["TotalPrice"] = totalPrice;
            Session["TicketTypes"] = string.Join(",", ticketTypes);
            Session["TicketPrices"] = string.Join(",", ticketPrices);

            // העברה לעמוד בחירת מושבים
            string sId = Request.QueryString["screeningId"] ?? Request.QueryString["ScreeningId"] ?? "";
            Response.Redirect("SeatsPicker.aspx?screeningId=" + sId);
        }

    }
}


