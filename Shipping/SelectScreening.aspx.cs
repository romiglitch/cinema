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
    // עמוד בחירת הקרנה - מציג הקרנות עתידיות מקובצות לפי תאריך
    public partial class SelectScreening : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
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

        private void LoadScreenings(int tmdbId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"SELECT s.ScreeningId, s.StartTime, m.Title 
                         FROM Screening s 
                         JOIN Movie m ON s.MovieId = m.Id 
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

                        var dayGroups = dt.AsEnumerable()
                            .GroupBy(r => ((DateTime)r["StartTime"]).Date)
                            .OrderBy(g => g.Key)
                            .Select(g => new ScreeningDayGroup
                            {
                                DateLabel = FormatHebrewDate(g.Key),
                                Count = g.Count(),
                                Screenings = g.Select(r => new ScreeningSlot
                                {
                                    ScreeningId = Convert.ToInt32(r["ScreeningId"]),
                                    StartTime = (DateTime)r["StartTime"]
                                }).ToList()
                            })
                            .ToList();

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

        protected void rptDays_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
                return;

            var day = (ScreeningDayGroup)e.Item.DataItem;
            var rptDayTimes = (Repeater)e.Item.FindControl("rptDayTimes");
            if (rptDayTimes != null)
            {
                rptDayTimes.DataSource = day.Screenings;
                rptDayTimes.DataBind();
            }
        }

        private static string FormatHebrewDate(DateTime date)
        {
            var culture = new CultureInfo("he-IL");
            return date.ToString("dddd, d MMMM yyyy", culture);
        }

        protected void btnSelectTime_Click(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender;
            string sId = btn.CommandArgument;
            Response.Redirect("Ticketing.aspx?screeningId=" + sId);
        }

        private sealed class ScreeningDayGroup
        {
            public string DateLabel { get; set; }
            public int Count { get; set; }
            public List<ScreeningSlot> Screenings { get; set; }
        }

        private sealed class ScreeningSlot
        {
            public int ScreeningId { get; set; }
            public DateTime StartTime { get; set; }
        }
    }
}
