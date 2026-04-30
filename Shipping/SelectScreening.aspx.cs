using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Shipping
{
    public partial class SelectScreening : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // בדיקה אם הגיע ID של סרט ב-URL
                if (int.TryParse(Request.QueryString["movieId"], out int tmdbId))
                {
                    LoadScreenings(tmdbId);
                }
                else
                {
                    lblNoScreenings.Text = "לא נבחר סרט תקין.";
                    lblNoScreenings.Visible = true;
                }
            }
        }

        private void LoadScreenings(int tmdbId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // שאילתה שמתאימה למבנה הטבלה שלך
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
                        // כאן ה-C# מזהה את lblMovieTitle כי הוספנו runat="server"
                        lblMovieTitle.InnerText = dt.Rows[0]["Title"].ToString();
                        rptTimes.DataSource = dt;
                        rptTimes.DataBind();
                        lblNoScreenings.Visible = false;
                    }
                    else
                    {
                        lblNoScreenings.Visible = true;
                    }
                }
                catch (Exception ex)
                {
                    lblNoScreenings.Text = "שגיאה בטעינת נתונים: " + ex.Message;
                    lblNoScreenings.Visible = true;
                }
            }
        }

        // פונקציית הלחיצה שתעביר לעמוד ה-Ticketing הקיים שלך
        protected void btnSelectTime_Click(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender;
            string sId = btn.CommandArgument;
            Response.Redirect("Ticketing.aspx?screeningId=" + sId);
        }
    }
}