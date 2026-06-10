using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Shipping
{
    // עמוד בחירת הקרנה – מציג הקרנות עתידיות לסרט, מקובצות לפי תאריך
    public partial class SelectScreening : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // movieId ב-URL הוא מזהה TMDb (כמו בעמוד פרטי הסרט), לא מזהה פנימי של Movie.Id
                if (int.TryParse(Request.QueryString["movieId"], out int tmdbId))
                {
                    LoadScreenings(tmdbId);
                }
                else
                {
                    lblNoScreenings.Text = "לא נבחר סרט תקין.";
                    lblNoScreenings.Visible = true;
                    pnlScreeningsByDate.Visible = false;
                }
            }
        }

        // שולף מהמסד את כל ההקרנות העתידיות לסרט ומקבץ לפי יום לתצוגה ב-rptDays / rptDayTimes
        private void LoadScreenings(int tmdbId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"SELECT s.ScreeningId, s.StartTime, m.Title,
                        (h.Rows * h.SeatsPerRow) - ISNULL((SELECT COUNT(*) FROM Tickets t WHERE t.Screening = s.ScreeningId), 0) AS AvailableSeats
                         FROM Screening s 
                         JOIN Movie m ON s.MovieId = m.Id 
                         JOIN Halls h ON s.Hall = h.HallId
                         WHERE m.TmdbId = @tmdbId AND s.StartTime > GETDATE()
                         ORDER BY s.StartTime";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tmdbId", tmdbId);

                try
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        lblMovieTitle.InnerText = dt.Rows[0]["Title"].ToString();

                        
                        var dayGroups = dt.AsEnumerable()//LINQבשביל להשתמש ב datarow בתור datatableמתייחס ל
                            // מפתח הקיבוץ: רק תאריך (00:00) – כל ההקרנות באותו יום בקבוצה אחת
                            .GroupBy(r => ((DateTime)r["StartTime"]).Date)
                            // ימים מהקרוב לרחוק (השאילתה כבר ממוינת, OrderBy שומר על סדר עקבי)
                            .OrderBy(g => g.Key)
                            // בניית אובייקט לכל יום – מה ש-rptDays מציג ב-ItemTemplate
                            .Select(g => new ScreeningDayGroup
                            {
                                // כותרת היום בעברית (יום בשבוע + תאריך)
                                DateLabel = FormatHebrewDate(g.Key),
                                // מספר ההקרנות באותו יום – מוצג ליד הכותרת ("N הקרנות")
                                Count = g.Count(),
                                // רשימת השעות באותו יום – נקשרת ל-rptDayTimes ב-ItemDataBound
                                Screenings = g.Select(r => new ScreeningSlot
                                {
                                    ScreeningId = Convert.ToInt32(r["ScreeningId"]),
                                    StartTime = (DateTime)r["StartTime"],
                                    AvailableSeats = Convert.ToInt32(r["AvailableSeats"])
                                }).ToList()
                            })
                            .ToList();

                        // ריפיטר חיצוני: איטרציה על ימים (כל ItemTemplate = screening-day אחד)
                        rptDays.DataSource = dayGroups;
                        rptDays.DataBind();
                        pnlScreeningsByDate.Visible = true;
                        lblNoScreenings.Visible = false;
                    }
                    else
                    {
                        pnlScreeningsByDate.Visible = false;
                        lblNoScreenings.Visible = true;
                    }
                }
                catch (Exception ex)
                {
                    pnlScreeningsByDate.Visible = false;
                    lblNoScreenings.Text = "שגיאה בטעינת נתונים";
                    lblNoScreenings.Visible = true;
                    Debug.WriteLine("Error: " + ex.ToString());
                }
            }
        }

        // ריפיטר מקונן: לכל יום ב-rptDays ממלאים את rptDayTimes בשעות של אותו יום
        protected void rptDays_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
                return;

            var day = (ScreeningDayGroup)e.Item.DataItem;
            var rptDayTimes = (Repeater)e.Item.FindControl("rptDayTimes");
            if (rptDayTimes != null)
            {
                rptDayTimes.ItemDataBound += rptDayTimes_ItemDataBound;
                rptDayTimes.DataSource = day.Screenings;
                rptDayTimes.DataBind();
            }
        }

        protected void rptDayTimes_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
                return;

            var slot = (ScreeningSlot)e.Item.DataItem;
            var btn = (LinkButton)e.Item.FindControl("btnSelect");
            if (btn == null || slot.AvailableSeats > 0)
                return;

            btn.Visible = false;
            e.Item.Controls.Add(new LiteralControl(
                $"<span class=\"screening-item sold-out\" title=\"אזלו הכרטיסים\">" +
                $"<span class=\"screening-time\">{slot.StartTime:HH:mm}</span></span>"));
        }

        // תווית תאריך בעברית לכותרת כל קבוצה (למשל: יום רביעי, 27 במאי 2026)
        private static string FormatHebrewDate(DateTime date)
        {
            var culture = new CultureInfo("he-IL");
            return date.ToString("dddd, d MMMM yyyy", culture);
        }

        // לחיצה על שעה – מעבר לבחירת סוגי כרטיסים והמשך תהליך הרכישה
        protected void btnSelectTime_Click(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender;
            string sId = btn.CommandArgument;
            Response.Redirect("Ticketing.aspx?screeningId=" + sId);
        }

        // נתונים לשורת תאריך אחת בריפיטר החיצוני
        private sealed class ScreeningDayGroup
        {
            public string DateLabel { get; set; }
            public int Count { get; set; }
            public List<ScreeningSlot> Screenings { get; set; }
        }

        // הקרנה בודדת בתוך יום – מזהה לניווט ושעה לתצוגה
        private sealed class ScreeningSlot
        {
            public int ScreeningId { get; set; }
            public DateTime StartTime { get; set; }
            public int AvailableSeats { get; set; }
        }
    }
}
