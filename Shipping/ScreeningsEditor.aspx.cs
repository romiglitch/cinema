using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Configuration;
using System.Drawing;
using System.Globalization;
using DALLlilbrary;

namespace Shipping
{
    public partial class ScreeningsEditor : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["category"] != "admin")
                Response.Redirect("Login.aspx");

            if (!IsPostBack) //בטעינה ראשונית בלבד טוען את רשימת הסרטים והאולמות
            {
                string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                LoadMovies(con);
                LoadHalls(con);
            }
        }

        protected void Page_Init(object sender, EventArgs e)//כשהעמוד נוצר בזיכרון אבל עדיין לא היה פייג' לואד
        {
            // DDLבדיקה שנבחר סרט ב
            if (!string.IsNullOrEmpty(Request.Form[ddlMovies.UniqueID]))//Request.Form מכיוון שהעמוד עדיין לא נבנה ניגשים לנתונים עם
            {
                int movieId;
                if (int.TryParse(Request.Form[ddlMovies.UniqueID], out movieId))
                {
                    //בנייה מחדש של הטבלה מיד עם טעינת הדף כדי שהצ'קבוקסים בתוך הטבלה ייווצרו בזיכרון
                    RebuildScheduleTable(movieId);
                }
            }
        }


        private void RebuildScheduleTable(int movieId)
        {
            // חישוב אורך הסרט כולל הפסקות ועיגול זמנים
            int slotMinutes = GetRoundedDuration(movieId);
            // יצירת רשימת טווחי זמן לאורך יום אחד
            var dailySlots = GenerateSequentialSchedule(slotMinutes);

            // הגדרת מערך הימים לתצוגה בכותרת הטבלה
            string[] days = { "ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת" };
            var culture = new CultureInfo("he-IL");

            // יום ראשון של השבוע הנוכחי (השבוע שמכיל את היום)
            DateTime today = DateTime.Today;
            int daysSinceSunday = ((int)today.DayOfWeek - (int)DayOfWeek.Sunday + 7) % 7;
            DateTime startOfVisibleWeek = today.AddDays(-daysSinceSunday);

            //יצירת הטבלה שתציג את כל ההקרנות
            Table tbl = new Table();
            tbl.CssClass = "weekSchedule";
            tbl.Attributes.Add("dir", "rtl");

            // יצירת שורת הכותרת של הטבלה
            TableHeaderRow hr = new TableHeaderRow();
            hr.Cells.Add(new TableHeaderCell { Text = "שעה" });

            for (int i = 0; i < 7; i++)
            {
                DateTime dayDate = startOfVisibleWeek.AddDays(i);
                var dayHeader = new TableHeaderCell();
                dayHeader.Controls.Add(new LiteralControl(
                    $"<span class=\"schedule-day-name\">{days[i]}</span><br />" +
                    $"<span class=\"schedule-day-date\">{dayDate.ToString("dd/MM", culture)}</span>"));
                hr.Cells.Add(dayHeader);
            }

            tbl.Rows.Add(hr);

            // מעבר על כל טווח שעות (שורה בטבלה)
            foreach (var slot in dailySlots)
            {
                TableRow row = new TableRow();
                // הוספת עמודת השעה מצד ימין
                row.Cells.Add(new TableCell { Text = $"{slot.Start:HH:mm} - {slot.End:HH:mm}" });

                // מעבר על 7 ימי השבוע עבור כל טווח שעות
                for (int i = 0; i < 7; i++)
                {
                    // חישוב התאריך והזמן המדויק עבור התא הספציפי בטבלה
                    DateTime currentDayStart = startOfVisibleWeek.AddDays(i).Add(slot.Start.TimeOfDay);
                    DateTime currentDayEnd = startOfVisibleWeek.AddDays(i).Add(slot.End.TimeOfDay);

                    TableCell cell = new TableCell();
                    // ❌ = מועבר (עבר) או שאין אולם פנוי לטווח השעות
                    bool isPast = currentDayStart <= DateTime.Now;
                    bool hallFree = AnyHallAvailable(currentDayStart, currentDayEnd);

                    if (!isPast && hallFree)
                    {
                        // יצירת תיבת סימון אם הזמן פנוי
                        CheckBox cb = new CheckBox();
                        cb.ID = $"cb_{movieId}_{currentDayStart:yyyyMMddHHmm}";//יחודי לכל צ'ק בוקס ID בניית 
                        cb.CssClass = "circleCheck";//הגדרת עיצוב
                        cb.EnableViewState = true; //עבור הפקד ViewState מפעיל
                                                   //שומר על מצב הסימון בין טעינות דף

                        //בשביל שנוכל אחכ לקרוא את המידע ולשמור אותו במסד HTMLשמירת נתוני ההקרנה ב
                        cb.Attributes["data-info"] = $"{movieId}|{currentDayStart:yyyy-MM-dd HH:mm}|{currentDayEnd:yyyy-MM-dd HH:mm}";
                        // הצגת כפתור ההוספה ברגע שצ'ק בוקס נבחר
                        cb.Attributes.Add("onclick", "showAddButton(this);");
                        //הכנסת אובייקט הצ'ק בוקס לטבלה
                        cell.Controls.Add(cb);
                    }
                    else
                    {
                        cell.Text = "❌";
                    }
                    row.Cells.Add(cell);
                }
                tbl.Rows.Add(row);
            }

            // ניקוי הפאנל מהטבלה הישנה והוספת הטבלה המעודכנת
            pnlSchedule.Controls.Clear();
            pnlSchedule.Controls.Add(tbl);
            pnlSchedule.Visible = true;
        }

        protected void Btn_Click(object sender, EventArgs e)
        {
            Session.Abandon();
            Response.Redirect("HomePage.aspx");
        }
        private void LoadMovies(string con)
        {
            DAL dAL = new DAL(con, "SELECT Id, Title FROM Movie", "Movie");
            ddlMovies.DataSource = dAL.GetData();//ddl מגדיר את הנתונים כמקור המידע של
            ddlMovies.DataTextField = "Title";
            ddlMovies.DataValueField = "Id";//הערך שישלח לשרת
            ddlMovies.DataBind();

            ddlMovies.Items.Insert(0, "אנא בחר סרט");
        }

        private void LoadHalls(string con)
        {
            DAL dAL = new DAL(con, "SELECT HallId FROM Halls", "Halls");
            ddlHalls.DataSource = dAL.GetData();
            ddlHalls.DataTextField = "HallId";
            ddlHalls.DataValueField = "HallId";
            ddlHalls.DataBind();
            ddlHalls.Items.Insert(0, "אולם");

        }


        protected void ddlMovies_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblMessage.Text = "";

            // הסתרת כפתור ההוספה עד שיבחר צ'ק בוקס חדש
            btnAddScreening.CssClass = "btnAddS hiddenBtn";

            if (ddlMovies.SelectedIndex == 0)//בדיקת בחירה תקינה
                return;

            int movieId = int.Parse(ddlMovies.SelectedValue);
            RebuildScheduleTable(movieId);//חישוב אורך הסרט, בדיקת אולמות פנויים ויצירת טבלה עבור הסרט הספציפי
        }

        protected void RadioSlot_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender; // זיהוי הפקד הספציפי ששלח את האירוע
            string data = rb.Attributes["data-info"]; // שליפת מחרוזת המידע הנסתרת

            // פירוק המחרוזת לשלושה חלקים
            string[] parts = data.Split('|');

            int movieId = int.Parse(parts[0]); // מזהה הסרט
            DateTime start = DateTime.Parse(parts[1]); // זמן תחילת ההקרנה
            DateTime end = DateTime.Parse(parts[2]); // זמן סיום ההקרנה

            int hallId = FindAvailableHall(start, end);

            if (hallId == -1)
            {
                lblMessage.Text = "אין אולם פנוי בשעה זו.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            string query = @"
        INSERT INTO Screening (MovieId, Hall, StartTime, EndTime)
        VALUES (@MovieId, @Hall, @StartTime, @EndTime)
    ";

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d = new DAL(con, query, "Screening");

            d.Params.Add(new SqlParameter("@MovieId", movieId));
            d.Params.Add(new SqlParameter("@Hall", hallId));
            d.Params.Add(new SqlParameter("@StartTime", start));
            d.Params.Add(new SqlParameter("@EndTime", end));

            d.ExecuteScalarDalPar();

            // שמירת פרטי הבחירה בזיכרון של הדף כדי שיהיו זמינים בלחיצה על כפתור האישור הסופי
            ViewState["SelectedMovie"] = parts[0];
            ViewState["SelectedStart"] = parts[1];
            ViewState["SelectedEnd"] = parts[2];

            // שינוי מצב הכפתור לגלוי כדי שהמשתמש יוכל לאשר סופית
            btnAddScreening.Visible = true;

            lblMessage.Text = "בחרת הקרנה. לחץ על 'הוסף הקרנה' כדי לאשר.";
            lblMessage.ForeColor = System.Drawing.Color.Black;
        }

        //של האולם הספציפי שנמצא פנוי IDה
        private int FindAvailableHall(DateTime newStart, DateTime newEnd)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = "SELECT HallId FROM Halls";

            DAL d1 = new DAL(con, query, "Halls");
            DataTable halls = d1.GetData();

            foreach (DataRow row in halls.Rows)
            {
                int hallId = Convert.ToInt32(row["HallId"]);

                if (IsHallAvailable(hallId, newStart, newEnd))
                    return hallId; // מחזיר את האולם הפנוי הראשון
            }

            return -1; // אין אף אולם פנוי
        }


        private int GetMovieDuration(int movieId)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = "SELECT Duration FROM Movie WHERE Id=" + movieId;
            DAL dAL = new DAL(con, query, "Movie");
            DataTable dt = dAL.GetData();
            return Convert.ToInt32(dt.Rows[0]["Duration"]);
        }
        private int GetRoundedDuration(int movieId)
        {
            int duration = GetMovieDuration(movieId);
            int remainder = duration % 5;
            if (remainder != 0)
                duration += (5 - remainder); // עיגול למעלה

            return duration + 20 + 15; // ניקיון + הפסקה
        }
        //אופציות אפשריות להקרנת הסרט בהתחשב האורך שלו
        private List<(DateTime Start, DateTime End)> GenerateSequentialSchedule(int totalMinutes)
        {
            List<(DateTime, DateTime)> schedule = new List<(DateTime, DateTime)>();

            DateTime start = DateTime.Today.AddHours(9); // 09:00
            DateTime dayEnd = DateTime.Today.AddDays(1); // 00:00

            while (start.AddMinutes(totalMinutes) <= dayEnd)
            {
                DateTime end = start.AddMinutes(totalMinutes);
                schedule.Add((start, end));

                // ההתחלה הבאה אחרי המעבר המלא
                start = end;
            }

            return schedule;
        }

        //האם קיים אולם פנוי כלשהו
        private bool AnyHallAvailable(DateTime newStart, DateTime newEnd)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = "SELECT HallId FROM Halls";
            DAL d1 = new DAL(con, query, "Halls");
            // מביא את כל האולמות
            DataTable halls = d1.GetData();

            foreach (DataRow row in halls.Rows)
            {
                int hallId = Convert.ToInt32(row["HallId"]);

                if (IsHallAvailable(hallId, newStart, newEnd))
                    return true;
            }

            return false; // אין אף אולם פנוי
        }
        private bool IsHallAvailable(int hallId, DateTime newStart, DateTime newEnd)
        {
            string query = @"
        SELECT COUNT(*)
        FROM Screening
        WHERE Hall = @Hall
          AND (@NewStart < EndTime AND @NewEnd > StartTime)
    ";

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            DAL d1 = new DAL(con, query, "Screening");
            d1.Params.Add(new SqlParameter("@Hall", hallId));
            d1.Params.Add(new SqlParameter("@NewStart", newStart));
            d1.Params.Add(new SqlParameter("@NewEnd", newEnd));

            int count = Convert.ToInt32(d1.ExecuteScalarDalPar());
            return count == 0;
        }

        protected void btnAddScreening_Click(object sender, EventArgs e)
        {
            int panelCount = pnlSchedule.Controls.Count;

            List<string> added = new List<string>(); // רשימה שתשמור את פרטי ההקרנות שנוספו בהצלחה כדי להציג למשתמש

            foreach (Control row in pnlSchedule.Controls) // סריקת הפאנל שמכיל את הטבלה
            {
                if (row is Table tbl) // מוודא שאנחנו עובדים על הטבלה
                {
                    foreach (TableRow tr in tbl.Rows) // מעבר על כל שורה בטבלה
                    {
                        if (tr is TableHeaderRow) 
                            continue; // דילוג על שורת הכותרת (ימי השבוע)

                        for (int i = 1; i < tr.Cells.Count; i++) // רץ על כל העמודות (מדלג על עמודה 0 שהיא שעת ההתחלה)
                        {
                            TableCell cell = tr.Cells[i];
                            if (!cell.HasControls()) 
                                continue; // אם התא ריק (למשל אולם תפוס), מדלגים

                            foreach (Control c in cell.Controls) // בודק מה יש בתוך התא
                            {
                                if (c is CheckBox cb && cb.Checked) // אם מצאנו צ'קבוקס והוא מסומן (V)
                                {
                                    // HTMLשליפת המידע שהצמדנו ב
                                    string data = cb.Attributes["data-info"];
                                    string[] parts = data.Split('|');

                                    int movieId = int.Parse(parts[0]);
                                    DateTime start = DateTime.Parse(parts[1]);
                                    DateTime end = DateTime.Parse(parts[2]);

                                    int hallId = FindAvailableHall(start, end);
                                    if (hallId != -1)
                                    {
                                        string query = @"
                                        INSERT INTO Screening (MovieId, Hall, StartTime, EndTime)
                                        VALUES (@MovieId, @Hall, @StartTime, @EndTime)
                                    ";

                                        string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                                        DAL d = new DAL(con, query, "Screening");

                                        d.Params.Add(new SqlParameter("@MovieId", movieId));
                                        d.Params.Add(new SqlParameter("@Hall", hallId));
                                        d.Params.Add(new SqlParameter("@StartTime", start));
                                        d.Params.Add(new SqlParameter("@EndTime", end));

                                        d.ExecuteScalarDalPar();

                                        //של ישראל (Culture) הגדרת אובייקט תרבות  
                                        var culture = new System.Globalization.CultureInfo("he-IL");

                                        // המרה של תאריך ההתחלה לשם היום בעברית
                                        string dayName = start.ToString("dddd", culture);

                                        // הוספה לרשימה בפורמט עברי
                                        added.Add($"{start:HH:mm} - {end:HH:mm} ביום {dayName}");
                                    }
                                }
                            }
                        }

                    }

                }
            }

            if (added.Count > 0)
            {
                lblMessage.ForeColor = System.Drawing.Color.Green;
                // איחוד כל ההקרנות לרשימה אחת עם ירידת שורה
                lblMessage.Text = ":הקרנות נוספו בהצלחה<br>" + string.Join("<br>", added);
            }
            else
            {
                lblMessage.Text = "לא נבחרו הקרנות או שאין אולם פנוי.";
            }

            // הסתרת כפתור הוספה
            btnAddScreening.CssClass = "btnAddS hiddenBtn";


             }
        }
}