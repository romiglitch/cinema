using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DALLlilbrary;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Shipping
{
    public partial class AdminPage : System.Web.UI.Page
    {
       
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["category"] != "admin")
                    Response.Redirect("Login.aspx");
            }

        }
        protected void Btn_Click(object sender, EventArgs e)
        {
            Session.Abandon();
            Response.Redirect("HomePage.aspx");
        }

        protected void Btn1_Click(object sender, EventArgs e)
        {
            Response.Redirect("MovieEditor.aspx");
        }
        protected void Btn2_Click(object sender, EventArgs e)
        {
            Response.Redirect("ScreeningsEditor.aspx");
        }

        //מילוי לוח הקרנות לפי סדר : בחירת יום,בחירת שעה, מילוי כל האולמות, מילוי כל השעות,מילוי כל הימים
        protected void BtnGenerateSchedule_Click(object sender, EventArgs e)
        {
            try //בשביל להגן על האתר מקריסה catchו try
            {
                ClearScreenings();//ניקוי הטבלה כולה ללא סרטים שכבר נקנו אליהם כרטיסים

                List<int> movieIds = GetAllMovieIds();
                Random rnd = new Random();
                int numberOfHalls = 10;

                for (int day = 0; day < 7; day++)
                {
                    DateTime currentDate = DateTime.Today.AddDays(day).Date;// (חישוב היום עליו עובדים כרגע (אליו מוסיפים הקרנות
                    DateTime globalTime = currentDate.AddHours(10); // מתחילים ב-10:00 בבוקר

                    while (globalTime < currentDate.AddDays(1).AddHours(1)) // רץ עד 1 בלילה
                    {
                        // רשימה זמנית לשעה הספציפית הזו כדי למנוע הקרנה של אותו סרט בכמה אולמות בו זמנית
                        List<int> moviesUsedInThisSlot = new List<int>();

                        for (int hall = 1; hall <= numberOfHalls; hall++)
                        {
                            // האם האולם פנוי? (בודק אם הסרט הקודם באולם הזה נגמר)
                            if (IsHallEmptyNow(hall, globalTime))
                            {
                                var shuffledMovies = movieIds.OrderBy(x => rnd.Next()).ToList();//ערבוב רשימת הסרטים 

                                foreach (int movieId in shuffledMovies)
                                {
                                    // בדיקה האם הסרט כבר שובץ בשעה הזו באולם אחר
                                    if (!moviesUsedInThisSlot.Contains(movieId))
                                    {
                                        //(בדיקת שאין חוסר התאמה בין סוג הסרט לשעה (ילדים/אימה
                                        if (!IsTimeMismatch(GetGenresForMovie(movieId), globalTime.TimeOfDay))//globalTimeהשעה בלבד מ :TimeOfDay
                                        {
                                            int duration = GetMovieDuration(movieId);
                                            DateTime endTime = globalTime.AddMinutes(duration + 40); // זמן הסרט + ניקיון

                                            // בדיקה שאין לסרט הקרנה חופפת בכל אולם אחר
                                            if (IsMovieOverlapping(movieId, globalTime, endTime))
                                                continue;

                                            InsertScreeningToDB(movieId, globalTime, endTime, hall);

                                            // מסמנים שהסרט תפוס לשעה הזו בשאר האולמות
                                            moviesUsedInThisSlot.Add(movieId);
                                            break;//יציאה מהלולאה כי מילאנו הקרנה לשעה עליה עבדנו, עוברים לאולם הבא
                                        }
                                    }
                                }
                            }
                        }
                        // מקדמים את הזמן ב-15 דקות ובודקים שוב את כל האולמות
                        globalTime = globalTime.AddMinutes(15);
                    }
                }
                lblAdminStatus.Text = "הלוח נוצר בהצלחה ובצורה נקייה!";
            }
            // catchברגע שיש שגיאה כלשהי עוברים אוטומטית ל
            catch (Exception ex)
            {
                // הצגת הודעת שגיאה באתר
                lblAdminStatus.Text = ".חלה שגיאה בביצוע הפעולה. אנא נסה שוב מאוחר";
                lblAdminStatus.ForeColor = System.Drawing.Color.Red;

                //שמכיל את כל המידע על התקלה Exception הצגת השגיאה המלאה בצד שרת בלבד עם אובייקט מסוג
                Debug.WriteLine("Admin Error Details: " + ex.Message);
                //שורת השגיאה
                Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }
        private bool IsMovieOverlapping(int movieId, DateTime start, DateTime end)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"SELECT COUNT(*) FROM Screening 
                    WHERE MovieId = @mid AND @start < EndTime AND @end > StartTime";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@mid", movieId);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private bool IsHallEmptyNow(int hall, DateTime time)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // בודק אם יש הקרנה שחופפת לזמן הנוכחי באולם הספציפי
                string query = "SELECT COUNT(*) FROM Screening WHERE Hall = @hall AND @time >= StartTime AND @time < EndTime";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@hall", hall);
                cmd.Parameters.AddWithValue("@time", time);
                conn.Open();
                return (int)cmd.ExecuteScalar() == 0;
            }
        }

        private void InsertScreeningToDB(int movieId, DateTime start, DateTime end, int hall)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = @"INSERT INTO Screening (MovieId, Hall, SeatesBought, StartTime, EndTime) 
                 VALUES (@mid, @hall, 0, @start, @end)";
            List<SqlParameter> Params = new List<SqlParameter>();

            Params.Add(new SqlParameter("@mid", movieId));
            Params.Add(new SqlParameter("@start", start));
            Params.Add(new SqlParameter("@end", end));
            Params.Add(new SqlParameter("@hall", hall));
            DAL d1 = new DAL(cs, query, "Screening", Params);
            d1.ExecuteNonQueryDalPar();//ביצוע שאילתא שלא מחזירה טבלה
        }
        private bool IsTimeMismatch(List<int> genreIds, TimeSpan slot)//בדיקה האם יש חוסר התאמה בין הסרט לשעת הקרנה
        {
            bool isKids = genreIds.Contains(16) || genreIds.Contains(10751);
            bool isHorror = genreIds.Contains(27);

            if (isKids && slot.Hours >= 19) return true; // ילדים לא בלילה
            if (isHorror && slot.Hours < 19) return true; // אימה לא בבוקר/צהריים
            return false;//יש התאמה
        }

        private void ClearScreenings()
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = "DELETE FROM Screening WHERE StartTime > GETDATE() AND SeatesBought = 0";
            DAL d1 = new DAL(cs,query,"Screening");
           d1.ExecuteNonQueryDal();
        }
        private List<int> GetAllMovieIds()
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            List<int> ids = new List<int>();
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT Id FROM Movie";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())//שמירת התוצאות באובייקט קורא שמאפשר לעבור עליהן שורה אחר שורה
                {
                    while (rdr.Read()) ids.Add((int)rdr["Id"]);//אם שורה נמצאה תואמת (כל עוד יש סרט בטבלה) לרשימת כל הסרטים
                }
            }
            return ids;
        }

        private int GetMovieDuration(int movieId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT Duration FROM Movie WHERE Id = @id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", movieId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return (result != DBNull.Value) ? (int)result : 120;//הגדרת אורך סרט 120 דק במידה ולא הוגדר לו אורך
            }
        }

        private List<int> GetGenresForMovie(int movieId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            List<int> genres = new List<int>();
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT IdGenre FROM MovieGenres WHERE IdMovie = @movieId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@movieId", movieId);
                conn.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) genres.Add((int)rdr["IdGenre"]);
                }
            }
            return genres;
        }

      
    } 
}