using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DALLlilbrary;

namespace Shipping
{
    // עמוד עריכת סרטים - מאפשר למנהל לצפות, לערוך ולעדכן את רשימת הסרטים במערכת
    public partial class MovieEditor : System.Web.UI.Page
    {
        /// <summary>
        /// נטען בעת טעינת הדף.
        /// בודק הרשאות גישה, ומפעיל Paging בעת טעינה ראשונה (לא PostBack).
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["category"] != "admin")
                    Response.Redirect("Login.aspx"); // רק מנהל מורשה להיכנס לדף זה

                CurrentPage = 0; // אתחול העמוד הנוכחי ל-0
                Paging();        // טעינת הדאטה ל-DataList בפעם הראשונה
            }
        }

        //לתמונה  URL ומחזירה TMDb APIמחפשת פוסטר לסרט לפי שמו ב 
        public string GetPosterFromTMDbByTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "/posters/no-image.jpg";

            string apiKey = ConfigurationManager.AppSettings["TMDbApiKey"];
            string url = $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(title)}";

            using (var client = new HttpClient())
            {
                var response = client.GetStringAsync(url).Result; //TMDb APIקריאה אסינכרונית ל
                var json = JObject.Parse(response);
                var result = json["results"]?.FirstOrDefault(); // לוקחים את התוצאה הראשונה (הרלוונטית ביותר)
                string posterPath = result?["poster_path"]?.ToString();

                if (!string.IsNullOrEmpty(posterPath))
                    return "https://image.tmdb.org/t/p/original" + posterPath;
            }

            return "/posters/no-image.jpg"; // תמונת ברירת מחדל אם לא נמצא פוסטר
        }


        //(DataList) חיבור הנתונים לתצוגה
        private void BindMoviesToDatalist()
        {
            Paging();
        }

        /// <summary>
        /// נכנס למצב עריכה בשורת הסרט הנבחרת ב־DataList.
        /// </summary>
        protected void DLMovies_EditCommand(object source, DataListCommandEventArgs e)
        {
            DLMovies.EditItemIndex = e.Item.ItemIndex;//עדכון הזיכרון
            BindMoviesToDatalist();// עדכון הדאטהליסט והצגה של טקסטבוקס
        }

        /// <summary>
        /// מעדכן סרט במסד הנתונים לאחר שינוי ערכים במצב עריכה.
        /// </summary>
        protected void DLMovies_UpdateCommand(object source, DataListCommandEventArgs e)
        {
            DataListItem changedItem = e.Item;

            string id = ((Label)changedItem.FindControl("LblId")).Text;
            string title = ((TextBox)changedItem.FindControl("TxtTitle")).Text;
            string description = ((TextBox)changedItem.FindControl("TxtDesc")).Text;
            string duration = ((TextBox)changedItem.FindControl("TxtDur")).Text;
            string age = ((TextBox)changedItem.FindControl("TxtAge")).Text;

            string cmdString = "UPDATE Movie SET title=@Title, description=@Description, duration=@Duration, age=@Age WHERE Id=@Id";
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(cmdString, connection);
                command.Parameters.AddWithValue("Title", title);
                command.Parameters.AddWithValue("Description", description);
                command.Parameters.AddWithValue("Duration", duration);
                command.Parameters.AddWithValue("Age", age);
                command.Parameters.AddWithValue("Id", id);
                command.ExecuteNonQuery();
            }

            DLMovies.EditItemIndex = -1;
            BindMoviesToDatalist();
        }

        /// <summary>
        /// ביטול עריכה והחזרת ה־DataList למצב קריאה.
        /// </summary>
        protected void DLMovies_CancelCommand(object source, DataListCommandEventArgs e)
        {
            DLMovies.EditItemIndex = -1;
            BindMoviesToDatalist();
        }


        /// <summary>
        ///מנגנון ששומר מידע בצד הלקוח כדי שיהיה זמין גם לאחר פוסטבק ,ViewState שמירת מספר העמוד הנוכחי של הדפדוף דרך
        ///חיוני ViewStateכשהמשתמש עובר עמוד הדף נשלח לשרת ונטען מחדש לכן השימוש ב
        /// </summary>
        public int CurrentPage
        {
            get
            {
                object obj = ViewState["CurrentPage"];//ViewStateב CurrentPage בדיקה אם קיים 
                return (obj == null) ? 0 : (int)obj;
            }
            set
            {
                ViewState["CurrentPage"] = value;//CurrentPageאת הערך שהוקצה ל CurrentPageשומר ב
            }
        }

        /// <summary>
        /// DataTableכ Movieמחזיר את כל הסרטים מ
        /// </summary>
        private DataTable GetMovies()
        {
            string query = "SELECT Id, Title, Description, Duration, Age, Poster FROM Movie";
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            DAL dt = new DAL(connectionString, query, "Movie");
            return dt.GetTable();
        }

        /// <summary>
        /// מנהל את הצגת הדפים (Paging) — קובע את מספר העמודים,
        /// מפעיל/מכבה כפתורי מעבר ומעדכן את ה־DataList.
        /// </summary>
        private async Task Paging()
        {
            DataTable dt = GetMovies();// טעינת כל הסרטים מהמסד
            
            PagedDataSource paged = new PagedDataSource();// אובייקט שמאפשר לקחת אוסף נתונים גדול ולחתוך אותו לעמודים ,PagedDataSource יצירת אובייקט
            paged.DataSource = dt.DefaultView;//לדפדפף בין העמודים PagedDataSourceהדאטה טייבל הופך לדיפולט ויו ומאפשר ל
            paged.AllowPaging = true; // הפעלת אפשרות החלוקה לעמודים
            paged.PageSize = 4; // קביעה שיוצגו בדיוק 4 סרטים בכל עמוד
            paged.CurrentPageIndex = CurrentPage; // הגדרת העמוד שבו המשתמש נמצא כרגע 

            btnPrev.Enabled = !paged.IsFirstPage;//מונע ממעבר לעמוד קודם/הבא 
            btnNext.Enabled = !paged.IsLastPage;// בעמוד הראשון/האחרון

            lblPageNumber1.Text = $"Page {CurrentPage + 1} of {paged.PageCount}";
            lblPageNumber2.Text = $"Page {CurrentPage + 1} of {paged.PageCount}";

            DLMovies.DataSource = paged; // -DataListהזרקת הנתונים החתוכים ל
            DLMovies.DataBind(); // הצגת הנתונים

        }
        // אירוע שנקרא לכל פריט ב-DataList - מאפשר לבצע עיבוד נוסף על כל שורה (כגון טעינת תמונה)
        protected void DLMovies_ItemDataBound(object sender, DataListItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DataRowView row = (DataRowView)e.Item.DataItem;
                string title = row["Title"].ToString();

                Image img = (Image)e.Item.FindControl("Img"); // מציאת פקד התמונה בפריט
            }
        }

        /// <summary>
        /// העברה לעמוד הבא
        /// </summary>
        protected void btnNext_Click(object sender, EventArgs e)
        {
            DataTable dt = GetMovies();
            int maxPages = (int)Math.Ceiling(dt.Rows.Count / 4.0);//חישוב כמה עמודי דפדוף יהיו בסך הכל עבור רשימת הסרטים עם המרה למעלה 

            if (CurrentPage < maxPages - 1)
                CurrentPage++;

            Paging();
        }

        /// <summary>
        /// העברה לעמוד הקודם
        /// </summary>
        protected void btnPrev_Click(object sender, EventArgs e)
        {
            if (CurrentPage > 0)
                CurrentPage--;

            Paging();
        }
    }
}
