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
using Newtonsoft.Json.Linq; // ודאי שיש לך את הספרייה הזו מותקנת (Json.NET)

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

        protected void btnGenerateSchedule_Click(object sender, EventArgs e)
        {
            try
            {
                // שלב 1: מנקים את הטבלה לחלוטין
                ClearScreenings();

                List<int> movieIds = GetAllMovieIds();
                Random rnd = new Random();
                int numberOfHalls = 10;

                // שלב 2: רצים על 7 הימים הקרובים
                for (int day = 0; day < 7; day++)
                {
                    DateTime currentDate = DateTime.Today.AddDays(day).Date;
                    DateTime globalTime = currentDate.AddHours(10); // מתחילים ב-10:00 בבוקר

                    while (globalTime < currentDate.AddDays(1).AddHours(1)) // רץ עד 1 בלילה
                    {
                        // רשימה זמנית לשעה הספציפית הזו כדי למנוע כפילויות בין אולמות
                        List<int> moviesUsedInThisSlot = new List<int>();

                        for (int hall = 1; hall <= numberOfHalls; hall++)
                        {
                            // האם האולם פנוי? (בודק אם הסרט הקודם באולם הזה נגמר)
                            if (IsHallEmptyNow(hall, globalTime))
                            {
                                var shuffledMovies = movieIds.OrderBy(x => rnd.Next()).ToList();

                                foreach (int movieId in shuffledMovies)
                                {
                                    // בדיקה: האם הסרט כבר שובץ בשעה הזו באולם אחר?
                                    if (!moviesUsedInThisSlot.Contains(movieId))
                                    {
                                        // בדיקת ז'אנר (ילדים/אימה)
                                        if (!IsTimeMismatch(GetGenresForMovie(movieId), globalTime.TimeOfDay))
                                        {
                                            int duration = GetMovieDuration(movieId);
                                            DateTime endTime = globalTime.AddMinutes(duration + 40); // זמן סיום + ניקיון

                                            InsertScreeningToDB(movieId, globalTime, endTime, hall);

                                            // מסמנים שהסרט תפוס לשעה הזו בשאר האולמות
                                            moviesUsedInThisSlot.Add(movieId);
                                            break;
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
            catch (Exception ex)
            {
                lblAdminStatus.Text = "שגיאה: " + ex.Message;
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
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"INSERT INTO Screening (MovieId, Hall, SeatesBought, StartTime, EndTime) 
                 VALUES (@mid, @hall, 0, @start, @end)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@mid", movieId);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                cmd.Parameters.AddWithValue("@hall", hall);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        private bool IsHallAvailable(int hall, DateTime start, DateTime end)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"SELECT COUNT(*) FROM Screening 
                         WHERE Hall = @hall 
                         AND (
                            (@start >= StartTime AND @start < EndTime) OR 
                            (@end > StartTime AND @end <= EndTime) OR
                            (StartTime >= @start AND StartTime < @end)
                         )";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@hall", hall);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);

                conn.Open();
                return (int)cmd.ExecuteScalar() == 0;
            }
        }

        // פונקציית עזר לבדיקת ז'אנר מול שעה
        private bool IsTimeMismatch(List<int> genreIds, TimeSpan slot)
        {
            bool isKids = genreIds.Contains(16) || genreIds.Contains(10751);
            bool isHorror = genreIds.Contains(27);

            if (isKids && slot.Hours >= 19) return true; // ילדים לא בלילה
            if (isHorror && slot.Hours < 19) return true; // אימה לא בבוקר/צהריים
            return false;
        }

        private void ClearScreenings()
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // מוחקים רק את מה שעדיין לא קרה ואין לו מכירות
                // אם משהו כבר נקנה (SeatesBought > 0), הוא יישאר בטבלה.
                string query = "DELETE FROM Screening WHERE StartTime > GETDATE() AND SeatesBought = 0";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
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
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) ids.Add((int)rdr["Id"]);
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
                return (result != DBNull.Value) ? (int)result : 120;
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

        //private void BindMoviesToDatalist()
        //{
        //    Paging();
        //}
        //    protected void DLMovies_EditCommand(object source, DataListCommandEventArgs e)
        //    {
        //        DLMovies.EditItemIndex = e.Item.ItemIndex;
        //        BindMoviesToDatalist();
        //    }
        //    protected void DLMovies_UpdateCommand(object source, DataListCommandEventArgs e)
        //    {
        //        DataListItem changedItem = e.Item;
        //        string id = ((Label)changedItem.FindControl("LblId")).Text;
        //        string title = ((TextBox)changedItem.FindControl("TxtTitle")).Text;
        //        string description = ((TextBox)changedItem.FindControl("TxtDesc")).Text;
        //        string duration = ((TextBox)changedItem.FindControl("TxtDur")).Text;
        //        string age = ((TextBox)changedItem.FindControl("TxtAge")).Text;
        //        //command string and a connection string
        //        string cmdString = "UPDATE Movie SET title=@Title, description=@Description, duration=@Duration,age=@Age WHERE Id=@Id ";
        //        string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
        //        //create the connection
        //        using (SqlConnection connection = new SqlConnection(connectionString))
        //        {
        //            connection.Open();
        //            SqlCommand command = new SqlCommand(cmdString, connection);
        //            command.Parameters.AddWithValue("Title", title);
        //            command.Parameters.AddWithValue("Description", description);
        //            command.Parameters.AddWithValue("Duration", duration);
        //            command.Parameters.AddWithValue("Age", age);
        //            command.Parameters.AddWithValue("Id", id);
        //            command.ExecuteNonQuery();
        //        }
        //        DLMovies.EditItemIndex = -1;
        //        BindMoviesToDatalist();
        //    }
        //    protected void DLMovies_CancelCommand(object source, DataListCommandEventArgs e)
        //    {
        //        DLMovies.EditItemIndex = -1;
        //        BindMoviesToDatalist();

        //    }

        //    public int CurrentPage
        //    {
        //        get
        //        {
        //            object obj = ViewState["CurrentPage"];
        //            return (obj == null) ? 0 : (int)obj;
        //        }
        //        set
        //        {
        //            ViewState["CurrentPage"] = value;
        //        }
        //    }


        //    private DataTable GetMovies()
        //    {
        //        string query = "SELECT Id, Title, Description, Duration, Age, Poster FROM Movie";
        //        string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
        //        DAL dt = new DAL(connectionString, query, "Movie");
        //        return dt.GetTable();

        //    }

        //    //private void Paging()
        //    //{
        //    //    DataTable dt = GetMovies();

        //    //    PagedDataSource paged = new PagedDataSource();
        //    //    paged.DataSource = dt.DefaultView;
        //    //    paged.AllowPaging = true;
        //    //    paged.PageSize = 4;
        //    //    paged.CurrentPageIndex = CurrentPage;

        //    //    btnPrev.Enabled = !paged.IsFirstPage;
        //    //    btnNext.Enabled = !paged.IsLastPage;

        //    //    lblPageNumber.Text = $"Page {CurrentPage + 1} of {paged.PageCount}";

        //    //    DLMovies.DataSource = paged;
        //    //    DLMovies.DataBind();
        //    //}

        //    //protected void btnNext_Click(object sender, EventArgs e)
        //    //{
        //    //    CurrentPage++;
        //    //    Paging();
        //    //}

        //    //protected void btnPrev_Click(object sender, EventArgs e)
        //    //{
        //    //    CurrentPage--;
        //    //    Paging();
        //    //}

        //}
    } 
}