
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
        // יום קולנוע נמשך מ-09:00 עד ~03:00 למחרת, כך שהקרנות אחרי חצות שייכות ליום הנבחר
        private void LoadMoviesByDate(DateTime date)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            List<Film> films = new List<Film>();

            // יום קולנוע מתחיל ב-09:00 ונגמר ב-09:00 למחרת
            // כך הקרנות אחרי חצות (למשל 24:00) נכללות ביום שבו התחילו ולא ביום שלמחרת
            DateTime scheduleStart = date.AddHours(9);
            DateTime scheduleEnd = date.AddDays(1).AddHours(9);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
    SELECT m.Title, s.StartTime, s.ScreeningID
    FROM Screening s
    JOIN Movie m ON s.MovieId = m.Id
    WHERE s.StartTime >= @ScheduleStart AND s.StartTime < @ScheduleEnd
    ORDER BY m.Title, s.StartTime";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ScheduleStart", scheduleStart);
                    cmd.Parameters.AddWithValue("@ScheduleEnd", scheduleEnd);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                       string title = reader.GetString(0);
                       DateTime showTime = reader.GetDateTime(1);
                       int screeningId = reader.GetInt32(2);

                        var film = films.Find(f => f.film_name == title);
                        if (film == null)
                        {
                            film = new Film { film_name = title, showtimes = new List<Showtime>() };
                            films.Add(film);
                        }

                        // הקרנה שהתאריך שלה הוא למחרת (חצתה את חצות) מוצגת בפורמט 24:00
                        // לדוגמה: הקרנה ב-00:00 של 06/06 שייכת ליום 05/06 ומוצגת כ-24:00
                        string displayTime;
                        if (showTime.Date > date.Date) // Date מחזיר רק את חלק התאריך ללא השעה
                            displayTime = $"{showTime.Hour + 24}:{showTime.Minute:D2}"; // 0+24=24 → "24:00"
                        else
                            displayTime = showTime.ToString("HH:mm"); // פורמט רגיל לשעות לפני חצות

                        film.showtimes.Add(new Showtime
                        {
                            Id = screeningId,
                            start_time = showTime,
                            display_time = displayTime
                        });
                    }
                }
            }

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
            public DateTime start_time { get; set; } // השעה האמיתית (DateTime) לצורך מיון ושאילתות
            public string display_time { get; set; } // השעה לתצוגה - "24:00" במקום "00:00" להקרנות אחרי חצות
        }
    }
}


