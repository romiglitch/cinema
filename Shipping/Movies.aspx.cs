
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

namespace Shipping
{
    public partial class Movies : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadNext7Days();
            }
        }

        private void LoadNext7Days()
        {
            ddlDates.Items.Clear();
            ddlDates.Items.Add(new ListItem("בחר תאריך", ""));
            for (int i = 0; i <= 7; i++)
            {
                DateTime date = DateTime.Today.AddDays(i);
                ddlDates.Items.Add(new ListItem(
                    date.ToString("dd/MM/yyyy"), date.ToString("yyyy-MM-dd")));
            }
        }

        protected void ddlDates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ddlDates.SelectedValue))
            {
                DateTime selectedDate = DateTime.Parse(ddlDates.SelectedValue);
                LoadMoviesByDate(selectedDate); // קריאה סינכרונית
            }
            else
            {
                DLMoviesByDate.DataSource = null;
                DLMoviesByDate.DataBind();
            }
        }


        private void LoadMoviesByDate(DateTime date)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            List<Film> films = new List<Film>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                /// בתוך מתודת LoadMoviesByDate
                string query = @"
    SELECT m.Title, s.StartTime, s.ScreeningID
    FROM Screening s
    JOIN Movie m ON s.MovieId = m.Id
    WHERE CAST(s.StartTime AS DATE) = @SelectedDate
    ORDER BY m.Title, s.StartTime";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SelectedDate", date);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                       string title = reader.GetString(0);
    DateTime showTime = reader.GetDateTime(1);
    
    // ודאי שהאינדקס (2) מתאים למיקום של ה-ID בשאילתה שלך
    int screeningId = reader.GetInt32(2);

                        var film = films.Find(f => f.film_name == title);
                        if (film == null)
                        {
                            film = new Film { film_name = title, showtimes = new List<Showtime>() };
                            films.Add(film);
                        }

                        film.showtimes.Add(new Showtime
                        {
                            Id = screeningId, // שמירת ה-ID
                            cinema_name = "Cinema Name",
                            start_time = showTime
                        });
                    }
                }
            }
            DLMoviesByDate.DataSource = films;
            DLMoviesByDate.DataBind();
        }


        protected void DLMoviesByDate_ItemDataBound(object sender, DataListItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                var film = (Film)e.Item.DataItem;
                var rpt = (Repeater)e.Item.FindControl("RptShowtimes");
                if (film.showtimes != null && film.showtimes.Count > 0)
                {
                    rpt.DataSource = film.showtimes;
                    rpt.DataBind();
                }
                else
                {
                    rpt.DataSource = null;
                    rpt.DataBind();
                }

            }
        }


        public class LocalResults
        {
            public List<TheaterShowtime> showtimes { get; set; }
        }

        public class TheaterShowtime
        {
            public string theater { get; set; }
            public List<string> times { get; set; }
        }

        public class ShowtimeTheater
        {
            public string theater { get; set; }
            public List<string> times { get; set; }
        }

        public class Theater
        {
            public string name { get; set; }
            public List<ShowtimeTheater> showtimes { get; set; }
            public string title { get; set; } // film name
        }

        public class Film
        {
            public string film_name { get; set; }
            public List<Showtime> showtimes { get; set; }
        }

        public class Showtime
        {
            public int Id { get; set; } // הוספת ה-ID של ההקרנה
            public string cinema_name { get; set; }
            public DateTime start_time { get; set; }
        }
    }
}


