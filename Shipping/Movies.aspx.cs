
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
        // נקרא בכל טעינה של הדף
        protected void Page_Load(object sender, EventArgs e)
        {
            // IsPostBack = false רק בטעינה הראשונה (לא אחרי לחיצה על כפתור או שינוי בתפריט)
            if (!IsPostBack)
            {
                LoadNext7Days(); // מילוי תפריט התאריכים
            }
        }

        // מילוי תפריט הבחירה (DropDownList) עם 7 הימים הקרובים
        // הטקסט המוצג למשתמש: dd/MM/yyyy (למשל 06/06/2026)
        // הערך הנשלח לשרת: yyyy-MM-dd (למשל 2026-06-06) - פורמט שמתאים לעבודה עם DateTime
        private void LoadNext7Days()
        {
            ddlDates.Items.Clear();
            ddlDates.Items.Add(new ListItem("בחר תאריך", "")); // פריט ברירת מחדל עם ערך ריק
            for (int i = 0; i <= 7; i++) // לולאה מהיום (0) עד עוד 7 ימים
            {
                DateTime date = DateTime.Today.AddDays(i); // חישוב התאריך של היום ה-i
                ddlDates.Items.Add(new ListItem(
                    date.ToString("dd/MM/yyyy"), date.ToString("yyyy-MM-dd")));
            }
        }

        // אירוע שמופעל כשהמשתמש בוחר תאריך מהתפריט (AutoPostBack=true בדף aspx)
        protected void ddlDates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ddlDates.SelectedValue)) // נבחר תאריך אמיתי (לא "בחר תאריך")
            {
                DateTime selectedDate = DateTime.Parse(ddlDates.SelectedValue); // המרת הטקסט לאובייקט DateTime
                LoadMoviesByDate(selectedDate); // שליפת ההקרנות מבסיס הנתונים והצגתן בדף
            }
            else
            {
                // המשתמש חזר ל"בחר תאריך" - מנקים את רשימת הסרטים מהדף
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
                // שאילתה ששולפת את כל ההקרנות בטווח הזמן של יום הקולנוע
                // JOIN עם טבלת Movie כדי לקבל את שם הסרט
                // המיון לפי שם הסרט ואז לפי שעת ההתחלה - כך הקרנות של אותו סרט מקובצות יחד
                string query = @"
    SELECT m.Title, s.StartTime, s.ScreeningID
    FROM Screening s
    JOIN Movie m ON s.MovieId = m.Id
    WHERE s.StartTime >= @ScheduleStart AND s.StartTime < @ScheduleEnd
    ORDER BY m.Title, s.StartTime";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // שימוש בפרמטרים (@) במקום הדבקת ערכים ישירות בשאילתה - מגן מפני SQL Injection
                    cmd.Parameters.AddWithValue("@ScheduleStart", scheduleStart);
                    cmd.Parameters.AddWithValue("@ScheduleEnd", scheduleEnd);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader(); // ביצוע השאילתה וקבלת תוצאות שורה-שורה

                    while (reader.Read()) // מעבר על כל שורת תוצאה (כל הקרנה)
                    {
                       string title = reader.GetString(0); // עמודה 0 = שם הסרט
                       DateTime showTime = reader.GetDateTime(1); // עמודה 1 = שעת ההתחלה
                       int screeningId = reader.GetInt32(2); // עמודה 2 = מזהה ההקרנה

                        // קיבוץ ההקרנות לפי שם הסרט: חיפוש אם הסרט כבר קיים ברשימה
                        var film = films.Find(f => f.film_name == title);
                        if (film == null) // סרט חדש שעוד לא ברשימה - יצירת אובייקט Film חדש
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

                        // הוספת ההקרנה לרשימת ההקרנות של הסרט
                        film.showtimes.Add(new Showtime
                        {
                            Id = screeningId, // מזהה ההקרנה - משמש בקישור לדף בחירת המושבים
                            start_time = showTime, // השעה האמיתית - לצורך מיון
                            display_time = displayTime // השעה לתצוגה - "24:00" להקרנות אחרי חצות
                        });
                    }
                }
            }

            // קישור רשימת הסרטים לפקד DataList שמציג אותם בדף
            // DataBind גורם לפקד לקרוא לאירוע ItemDataBound עבור כל סרט
            DLMoviesByDate.DataSource = films;
            DLMoviesByDate.DataBind();
        }


        // אירוע שמופעל אוטומטית עבור כל סרט ברשימה אחרי DataBind
        // התפקיד: לקשר את רשימת שעות ההקרנה (Showtime) לריפיטר הפנימי שמציג אותן כלינקים
        protected void DLMoviesByDate_ItemDataBound(object sender, DataListItemEventArgs e)
        {
            // רק שורות שמייצגות סרטים אמיתיים (לא Header/Footer)
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                var film = (Film)e.Item.DataItem; // שליפת אובייקט הסרט מהנתונים שקושרו
                var rpt = (Repeater)e.Item.FindControl("RptShowtimes"); // מציאת פקד הריפיטר שמוגדר בתוך ה-ItemTemplate
                if (film.showtimes != null && film.showtimes.Count > 0)
                {
                    // קישור רשימת ההקרנות לריפיטר - כל Showtime יוצר לינק עם השעה
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

        // מייצג סרט עם שם ורשימת שעות הקרנה - משמש כמקור נתונים ל-DataList
        public class Film
        {
            public string film_name { get; set; } // שם הסרט - מוצג בדף דרך Eval("film_name")
            public List<Showtime> showtimes { get; set; } // רשימת ההקרנות - מקושרת לריפיטר הפנימי
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


