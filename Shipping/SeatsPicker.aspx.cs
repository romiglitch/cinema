using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace Shipping
{
    public partial class SeatsPicker : System.Web.UI.Page
    {
        protected int screeningId;
        protected int hallId;
        protected int totalTickets;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                totalTickets = Convert.ToInt32(Session["TotalTickets"] ?? 0);
                ViewState["TicketsCount"] = totalTickets;

                if (!string.IsNullOrEmpty(Request.QueryString["screeningId"]) &&
                    int.TryParse(Request.QueryString["screeningId"], out int qScId))
                {
                    screeningId = qScId;
                    ViewState["screeningId"] = screeningId;
                }

                // נשלוף את ה-HallId רק פעם אחת
                string connStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 Hall FROM Screening WHERE ScreeningId = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", screeningId);
                        hallId = Convert.ToInt32(cmd.ExecuteScalar());
                        ViewState["hallId"] = hallId;
                    }
                }

                // טען אולם ומושבים פעם אחת בלבד
                LoadHallAndSeats();
            }
            else
            {
                // בפוסטבק נקרא את הנתונים מה־ViewState בלבד
                screeningId = Convert.ToInt32(ViewState["screeningId"]);
                hallId = Convert.ToInt32(ViewState["hallId"]);
                totalTickets = Convert.ToInt32(ViewState["TicketsCount"]);
            }
            if (Session["UserId"] == null)
            {
                string currentScId = Request.QueryString["screeningId"];
                string targetUrl = $"Login.aspx?returnUrl=SeatsPicker.aspx?screeningId={currentScId}";

                string script = $@"
    Swal.fire({{
        html: '<p class=""glass-content"">כדי לקנות כרטיסים עליך להתחבר למערכת</p>' +
              '<p class=""glass-content"">אין לך חשבון? <a href=""#"" class=""glass-link"">צור חשבון חדש</a></p>',
        icon: 'warning',
        background: 'transparent',
        showConfirmButton: true,
        confirmButtonText: 'מעבר להתחברות',
        customClass: {{
            popup: 'my-glass-popup',
            confirmButton: 'my-glass-button'
        }}
    }}).then((result) => {{
        if (result.isConfirmed) {{
            window.location.href = '{targetUrl}';
        }}
else {{
window.location.href = 'HomePage.aspx';
}}
    }});";


                ClientScript.RegisterStartupScript(this.GetType(), "LoginAlert", script, true);
                return;
            }

        }


        private void LoadHallAndSeats()
        {

            string connStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            int rows = 0, seatsPerRow = 0;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // קבלת HallId לפי ההקרנה (שים לב לשם הטבלה - בדוק אם הטבלה שלך נקראת "Screening" או "Screenings")
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 Hall FROM Screening WHERE ScreeningId = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", screeningId);
                    object hallObj = cmd.ExecuteScalar();
                    if (hallObj == null)
                    {
                        Response.Write("<div style='color:red;text-align:center;'>שגיאה: לא נמצא אולם להקרנה זו.</div>");
                        return;
                    }
                    hallId = Convert.ToInt32(hallObj);
                    ViewState["hallId"] = hallId;
                }

                // קבלת נתוני מבנה האולם
                using (SqlCommand cmd = new SqlCommand("SELECT Rows, SeatsPerRow FROM Halls WHERE HallId = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", hallId);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            rows = Convert.ToInt32(rdr["Rows"]);
                            seatsPerRow = Convert.ToInt32(rdr["SeatsPerRow"]);
                        }
                    }
                }

                // בניית מבנה שורות ומושבים (אחסון בלבד)
                List<HallRow> hallRows = new List<HallRow>();
                for (int r = 1; r <= rows; r++)
                {
                    List<SeatData> seats = new List<SeatData>();
                    for (int s = 1; s <= seatsPerRow; s++)
                    {
                        seats.Add(new SeatData
                        {
                            SeatId = 0, // אם יש ב־Seats טבלה מזהה מושב אמיתי אפשר למלא אותו
                            RowNumber = r,
                            SeatNumber = s,
                            CssClass = "seat available"
                        });
                    }
                    hallRows.Add(new HallRow { RowNumber = r, Seats = seats });
                }

                // שליפת מושבים מיוחדים מטבלת Seats (תפוסים / נגישים)
                using (SqlCommand cmd = new SqlCommand("SELECT SeatId, RowNumber, SeatNumber, IsAccessible FROM Seats WHERE HallId = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", hallId);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            int seatId = rdr["SeatId"] != DBNull.Value ? Convert.ToInt32(rdr["SeatId"]) : 0;
                            int row = Convert.ToInt32(rdr["RowNumber"]);
                            int seat = Convert.ToInt32(rdr["SeatNumber"]);
                            bool accessible = Convert.ToBoolean(rdr["IsAccessible"]);

                            HallRow targetRow = hallRows.Find(x => x.RowNumber == row);
                            if (targetRow != null)
                            {
                                SeatData sObj = targetRow.Seats.Find(x => x.SeatNumber == seat);
                                if (sObj != null)
                                {
                                    sObj.SeatId = seatId;
                                    if (accessible)
                                        sObj.CssClass = "seat accessible";
                                }
                            }
                        }
                    }
                }

                // סימון מושבים שתפוסים עבור הסקרינינג (מניח שיש טבלת Tickets שמכילה Screening/Row/Seat)
                using (SqlCommand cmd = new SqlCommand("SELECT [Row], [Seat] FROM Tickets WHERE Screening = @screening", con))
                {
                    cmd.Parameters.AddWithValue("@screening", screeningId);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            int row = Convert.ToInt32(rdr["Row"]);
                            int seat = Convert.ToInt32(rdr["Seat"]);

                            HallRow targetRow = hallRows.Find(x => x.RowNumber == row);
                            if (targetRow != null)
                            {
                                SeatData sObj = targetRow.Seats.Find(x => x.SeatNumber == seat);
                                if (sObj != null)
                                {
                                    sObj.CssClass = "seat taken";
                                }
                            }
                        }
                    }
                }

                // הצגת השורות בריפיטר
                RepeaterRows.DataSource = hallRows;
                RepeaterRows.DataBind();

                // שמירת seatsPerRow לשימוש בצד לקוח או לוגיקה עתידית
                ViewState["SeatsPerRow"] = seatsPerRow;
            }
        }

        protected void RepeaterRows_ItemDataBound(object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != System.Web.UI.WebControls.ListItemType.Item &&
                e.Item.ItemType != System.Web.UI.WebControls.ListItemType.AlternatingItem)
                return;

            var rowData = (HallRow)e.Item.DataItem;

            // מצא את ה-div שיש לו runat="server" (ולא Literal)
            HtmlGenericControl container = (HtmlGenericControl)e.Item.FindControl("rowSeatsContainer");

            if (container == null)
                return; // אם משום מה לא נמצא, לא נזרוק שגיאה

            foreach (var seat in rowData.Seats)
            {
                HtmlGenericControl seatDiv = new HtmlGenericControl("div");
                seatDiv.Attributes["class"] = seat.CssClass;
                string val = $"{seat.SeatId}|{seat.RowNumber}|{seat.SeatNumber}";
                seatDiv.Attributes["data-value"] = val;
                seatDiv.Attributes["data-row"] = seat.RowNumber.ToString();
                seatDiv.Attributes["data-seat"] = seat.SeatNumber.ToString();
                seatDiv.InnerText = seat.SeatNumber.ToString();

                container.Controls.Add(seatDiv);
            }
        }



        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            string selectedData = Request.Form["SelectedSeats"];

            if (string.IsNullOrEmpty(selectedData))
            {
                ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('אנא בחרי מושבים לפני אישור.');", true);
                return;
            }

            var selectedSeatsArray = selectedData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            int ticketsCount = Convert.ToInt32(ViewState["TicketsCount"] ?? 0);

            if (selectedSeatsArray.Length != ticketsCount)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "alert", $"alert('בחרת {selectedSeatsArray.Length} מושבים, אך עלייך לבחור בדיוק {ticketsCount}.');", true);
                return;
            }

            // --- התיקון כאן ---
            // אם ה-ViewState ריק, נסי לשלוף מה-URL או להשתמש בנתון שקיבלת כשהדף נטען
            Session["SelectedSeats"] = selectedData;

            // שליפה בטוחה: אם ה-ViewState ריק, ננסה לקחת מה-QueryString (למשל ?id=5)
            string sId = ViewState["screeningId"]?.ToString() ?? Request.QueryString["id"];
            string hId = ViewState["hallId"]?.ToString() ?? Request.QueryString["hall"];

            Session["ScreeningId"] = sId;
            Session["HallId"] = hId;

            Response.Redirect("Cart.aspx");
        }


    }

    // מחלקות עזר
    public class HallRow
    {
        public int RowNumber { get; set; }
        public List<SeatData> Seats { get; set; }
    }

    public class SeatData
    {
        public int SeatId { get; set; }
        public int RowNumber { get; set; }
        public int SeatNumber { get; set; }
        public string CssClass { get; set; }
    }
}
