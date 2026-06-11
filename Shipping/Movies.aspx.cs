
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
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

        // מילוי תפריט הבחירה עם 7 הימים הקרובים (היום + 6 ימים הבאים)
        private void LoadNext7Days()
        {
            ddlDates.Items.Clear();
            ddlDates.Items.Add(new ListItem("בחר תאריך", ""));
            DateTime today = DateTime.Today;
            for (int i = 0; i < 7; i++)
            {
                DateTime date = today.AddDays(i);
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
                LoadMoviesByDate(selectedDate); // שליחת התאריך הנבחר לפונקציה ששולפת מהמסד את ההקרנות ומציגה אותן בדף
            }
            else
            {
                // המשתמש חזר ל"בחר תאריך" - מנקים את רשימת הסרטים מהדף
                DLMoviesByDate.DataSource = null;
                DLMoviesByDate.DataBind();
            }
        }


        // שולפת מבסיס הנתונים את כל ההקרנות בתאריך הנבחר ומקבצת אותן לפי שם הסרט
        private void LoadMoviesByDate(DateTime date)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            List<Film> films = new List<Film>();//יצירת רשימה ריקה של סרטים

            // יום קולנוע מתחיל ב-09:00 ונגמר ב-09:00 למחרת
            // כך הקרנות אחרי חצות (למשל 24:00) נכללות ביום שבו התחילו ולא ביום שלמחרת
            DateTime scheduleStart = date.AddHours(9);//שעת התחלה של  ההקרנות בתאריך שבחרנו (9:00)
            DateTime scheduleEnd = date.AddDays(1).AddHours(9);//שעת סיום של ההקרנות בתאריך שבחרנו (9:00 למחרת)

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                // שאילתה ששולפת את כל ההקרנות בטווח הזמן של היום שבחרנו
                // JOIN עם טבלת Movie כדי לקבל את שם הסרט
                // המיון לפי שם הסרט ואז לפי שעת ההתחלה - כך הקרנות של אותו סרט מקובצות יחד
                string query = @"
    SELECT m.Title, s.StartTime, s.ScreeningID,
           (h.Rows * h.SeatsPerRow) - ISNULL((SELECT COUNT(*) FROM Tickets t WHERE t.Screening = s.ScreeningID), 0) AS AvailableSeats
    FROM Screening s
    JOIN Movie m ON s.MovieId = m.Id
    JOIN Halls h ON s.Hall = h.HallId
    WHERE s.StartTime >= @ScheduleStart AND s.StartTime < @ScheduleEnd
      AND s.StartTime > GETDATE()
    ORDER BY m.Title, s.StartTime";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // שימוש בפרמטרים (@) במקום הדבקת ערכים ישירות בשאילתה - מגן מפני SQL Injection
                    cmd.Parameters.AddWithValue("@ScheduleStart", scheduleStart);
                    cmd.Parameters.AddWithValue("@ScheduleEnd", scheduleEnd);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader(); // שמירת התוצאות באובייקט קורא שמאפשר לעבור עליהן שורה אחר שורה

                    while (reader.Read()) // מעבר על כל שורת תוצאה (כל הקרנה)
                    {
                       string title = reader.GetString(0); // עמודה 0 = שם הסרט
                       DateTime showTime = reader.GetDateTime(1); // עמודה 1 = שעת ההתחלה
                       int screeningId = reader.GetInt32(2); // עמודה 2 = מזהה ההקרנה
                       int availableSeats = reader.GetInt32(3); // עמודה 3 = מקומות פנויים

                        //קיבוץ ההקרנות לפי שם הסרט
                        var film = films.Find(f => f.film_name == title);//בודק עבור כל סרט ברשימה אם השם שלו שווה לשם הסרט שאנחנו בודקים (אם הוא כבר קיים ברשימה)
                        if (film == null) //אם הסרט לא קיים ברשימה מוסיפים אותו 
                        {
                            film = new Film { film_name = title, showtimes = new List<Showtime>() };//יצירת אובייקט סרט חדש ברשימה עם השם שלו ורשימת ההקרנות שלו
                            films.Add(film);
                        }

                        // הקרנה אחרי חצות שייכת ליום הקולנוע שנבחר; רק 00:xx מוצג כ-24:xx
                        string displayTime;
                        if (showTime.Date > date.Date && showTime.Hour == 0)//אם התאריך של ההקרנה גדול מהתאריך שבחרנו והוא מוצג בחצות 
                            displayTime = $"24:{showTime.Minute:D2}";
                        else
                            displayTime = showTime.ToString("HH:mm"); // פורמט רגיל לשעות לפני חצות
                        
                        // הוספת ההקרנה לרשימת ההקרנות של הסרט
                        film.showtimes.Add(new Showtime
                        {
                            Id = screeningId, // מזהה ההקרנה - משמש בקישור לדף בחירת המושבים
                            start_time = showTime, // שעת ההתחלה - השעה האמיתית מהמסד נתונים - לצורך מיון
                            display_time = displayTime, //  השעה לתצוגה - "24:00" להקרנות אחרי חצות
                            available_seats = availableSeats
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
            public string display_time { get; set; } // 24:xx רק ל-00:xx; שאר שעות אחרי חצות ב-HH:mm
            public int available_seats { get; set; } // מקומות פנויים - 0 = אזלו הכרטיסים
        }

        // מחזיר קישור פעיל או תווית מושבתת לפי זמינות מקומות
        protected string RenderShowtimeLink(object item)
        {
            var st = (Showtime)item;
            string time = HttpUtility.HtmlEncode(st.display_time);
            if (st.available_seats > 0)
                return $"<a href=\"Ticketing.aspx?screeningId={st.Id}\" class=\"time-slot\">{time}</a>";
            return $"<span class=\"time-slot sold-out\" title=\"אזלו הכרטיסים\" aria-disabled=\"true\">{time}</span>";
        }
    }
}


