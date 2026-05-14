using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
namespace Shipping
{
    // עמוד בחירת הקרנה - מציג את כל שעות ההקרנה הזמינות לסרט שנבחר
    public partial class SelectScreening : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                //וטעינת ההקרנות המתאימות URL-מה (TMDb ID) שליפת מזהה הסרט 
                if (int.TryParse(Request.QueryString["movieId"], out int tmdbId))
                {
                    LoadScreenings(tmdbId);
                }
                else
                {
                    //תקין - מציגים הודעה ומסתירים את הרשימה ID אם לא הועבר  
                    lblNoScreenings.Text = "לא נבחר סרט תקין.";
                    lblNoScreenings.Visible = true;
                }
            }
        }

        //ומציגה אותן TMDb IDשולפת מבסיס הנתונים את כל ההקרנות העתידיות לסרט לפי 
        private void LoadScreenings(int tmdbId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // שאילתה המחברת Screening ו-Movie לפי TMDb ID ומסננת רק הקרנות עתידיות
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
                        // הצגת שם הסרט בכותרת הדף ורשימת השעות בריפיטר
                        lblMovieTitle.InnerText = dt.Rows[0]["Title"].ToString();
                        rptTimes.DataSource = dt;
                        rptTimes.DataBind();
                        lblNoScreenings.Visible = false;
                    }
                    else
                    {
                        // אין הקרנות זמינות לסרט זה
                        lblNoScreenings.Visible = true;
                    }
                }
                catch (Exception ex)
                {
                    lblNoScreenings.Text = "שגיאה בטעינת נתונים";
                    lblNoScreenings.Visible = true;
                    Debug.WriteLine("Error: " + ex.ToString());
                }
            }
        }

        //URLלחיצה על שעת הקרנה - מעבירה לעמוד בחירת כרטיסים עם מזהה ההקרנה ב
        protected void btnSelectTime_Click(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender;
            string sId = btn.CommandArgument; //של הכפתור CommandArgumentמזהה ההקרנה שהוגדר ב 
            Response.Redirect("Ticketing.aspx?screeningId=" + sId);
        }
    }
}