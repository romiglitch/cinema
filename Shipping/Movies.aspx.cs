
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
    // עמוד הסרטים - מציג רשימת הקרנות לפי תאריך נבחר ומאפשר למשתמש לעבור לבחירת מושבים
    public partial class Movies : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // בטעינה ראשונה
            if (!IsPostBack)
            {
                LoadNext7Days();
            }
        }

        // עם 7 הימים הקרובים לבחירת תאריך הקרנה ddlמילוי ה
        private void LoadNext7Days()
        {
            ddlDates.Items.Clear();
            ddlDates.Items.Add(new ListItem("בחר תאריך", "")); // פריט ברירת מחדל
            for (int i = 0; i <= 7; i++)
            {
                DateTime date = DateTime.Today.AddDays(i);
                ddlDates.Items.Add(new ListItem(
                    date.ToString("dd/MM/yyyy"), date.ToString("yyyy-MM-dd")));
            }
        }

        // אירוע שינוי תאריך - נשלפים הסרטים המוקרנים בתאריך שנבחר
        protected void ddlDates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ddlDates.SelectedValue))
            {
                DateTime selectedDate = DateTime.Parse(ddlDates.SelectedValue);
                LoadMoviesByDate(selectedDate); // שליפת הסרטים
            }
            else
            {
                // אם לא נבחר תאריך, מנקים את הרשימה
                DLMoviesByDate.DataSource = null;
                DLMoviesByDate.DataBind();
            }
        }


        // שולפת מבסיס הנתונים את כל ההקרנות בתאריך הנבחר ומקבצת אותן לפי שם הסרט
        private void LoadMoviesByDate(DateTime date)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            List<Film> films = new List<Film>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
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
                    SqlDataReader reader = cmd.ExecuteReader();//שמירת התוצאות באובייקט קורא שמאפשר לעבור עליהן שורה אחר שורה

                    while (reader.Read())
                    {
                       string title = reader.GetString(0);
                       DateTime showTime = reader.GetDateTime(1);
                       int screeningId = reader.GetInt32(2);

                        // קיבוץ ההקרנות לפי שם הסרט - אם הסרט כבר קיים ברשימה, מוסיפים לו שעת הקרנה
                        var film = films.Find(f => f.film_name == title);//חיפוש ברשימה שיצרנו אם הסרט עליו כרגע עוברים קיים בה
                        if (film == null)
                        {
                            film = new Film { film_name = title, showtimes = new List<Showtime>() };//מוסיפים אותו לרשימה אם לא
                            films.Add(film);
                        }

                        film.showtimes.Add(new Showtime//אם כן מוסיפים לו את הקרנה
                        {
                            Id = screeningId,
                            start_time = showTime
                        });
                    }
                }
            }
            // קישור הנתונים לרשימת הסרטים בדף
            DLMoviesByDate.DataSource = films;
            DLMoviesByDate.DataBind();
        }


        // אירוע שנקרא לכל פריט ברשימה - מקשר את שעות ההקרנה לריפיטר הפנימי של כל סרט
        //DataBindמופעל אוטומטית עבור כל סרט לאחר ה
        protected void DLMoviesByDate_ItemDataBound(object sender, DataListItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)//בדיקה שהקוד ירוץ על השורות בדאטהליסט שמייצגות סטרים אמיתיים
            {
                var film = (Film)e.Item.DataItem; // שליפת אובייקט הסרט הנוכחי
                var rpt = (Repeater)e.Item.FindControl("RptShowtimes"); // מציאת הריפיטר הפנימי של שעות ההקרנה
                if (film.showtimes != null && film.showtimes.Count > 0)
                {
                    rpt.DataSource = film.showtimes;
                    rpt.DataBind();
                }
                else
                {
                    // אין שעות הקרנה לסרט זה - ריפיטר ריק
                    rpt.DataSource = null;
                    rpt.DataBind();
                }

            }
        }


        // מחלקות עזר לייצוג נתוני הסרטים וההקרנות

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

        // מייצג בית קולנוע עם שם, שם הסרט ורשימת שעות
        public class Theater
        {
            public string name { get; set; }
            public List<ShowtimeTheater> showtimes { get; set; }
            public string title { get; set; } // שם הסרט
        }

        // מייצג סרט עם שם ורשימת שעות הקרנה
        public class Film
        {
            public string film_name { get; set; }
            public List<Showtime> showtimes { get; set; }
        }

        // מייצג הקרנה בודדת - כולל מזהה ייחודי, שם הקולנוע ושעת ההתחלה
        public class Showtime
        {
            public int Id { get; set; } // מזהה ייחודי של ההקרנה לצורך בחירת מושבים
            public DateTime start_time { get; set; }
        }
    }
}


