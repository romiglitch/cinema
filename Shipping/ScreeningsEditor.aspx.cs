using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using DALLlilbrary;

namespace Shipping
{
    // דף עריכת הקרנות - מאפשר למנהל לסמן/לבטל הקרנות בטבלה שבועית לכל סרט
    public partial class ScreeningsEditor : System.Web.UI.Page
    {
        // נקרא בכל טעינת דף - בודק הרשאת מנהל וטוען את רשימות הסרטים והאולמות בטעינה ראשונה
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["category"] != "admin") // רק מנהל מורשה לגשת לדף
                Response.Redirect("Login.aspx");

            if (!IsPostBack) // טעינה ראשונה בלבד (לא אחרי PostBack)
            {
                string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                LoadMovies(con); // מילוי תפריט הסרטים
            }
        }

        // PostBackבונה מחדש את טבלת הזמנים כדי שהצ׳קבוקסים יהיו זמינים ב - PageLoadנקרא לפני 
        protected void Page_Init(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Request.Form[ddlMovies.UniqueID])) //יש מזהה ייחודי ddlאם נבחר סרט בטופס, ל
            {
                if (int.TryParse(Request.Form[ddlMovies.UniqueID], out int movieId))
                    RebuildScheduleTable(movieId); // בנייה מחדש של הטבלה עם הסלוטים של הסרט
            }
        }

        //ddlנקרא ברגע שבוחרים סרט ב        
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);//מסיים את העיבוד המובנה של הדף

            bool scheduleReady = pnlSchedule.Visible && ddlMovies.SelectedIndex > 0; // האם הטבלה מוצגת וסרט נבחר
            btnAddScreening.CssClass = "login-btn schedule-update-btn";//עיצוב כפתור העדכון
            btnAddScreening.Enabled = scheduleReady; // הפעלת כפתור מצד שרת

            if (scheduleReady)
                btnAddScreening.Attributes["disabled"] = "disabled"; // כפתור מושבת בהתחלה אם אין שינויים
            else
                btnAddScreening.Attributes.Remove("disabled");
            ddlMovies.Attributes["onchange"] = "return onMovieSelectionChanging(this);"; //javaScriptשב onMovieSelectionChanging תקרא הפונקציה  ddlכאשר משנים סרט ב
        }

        // בניית טבלת הסלוטים השבועית לסרט הנבחר
        // כל שורה = סלוט זמן (למשל 09:00-11:30), כל עמודה = יום בשבוע
        // כל תא מכיל צ'קבוקס שמציין אם יש הקרנה בסלוט הזה ביום הזה
        private void RebuildScheduleTable(int movieId)
        {
            int slotMinutes = GetRoundedDuration(movieId); // חישוב אורך הסלוט בדקות (כולל פרסומות וניקיון)
            var dailySlots = GenerateSequentialSchedule(slotMinutes); // יצירת רשימת כל הסלוטים ביום

            string[] dayNames = { "ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת" }; // שמות הימים בעברית לפי אינדקס (0=ראשון)
            var culture = new CultureInfo("he-IL");

            DateTime startOfVisibleWeek = DateTime.Today; // הטבלה מתחילה מהיום (לא מתחילת השבוע הקלנדרי)

            var existingBySlot = LoadMovieScreeningsForWeek(movieId, startOfVisibleWeek); // שליפת כל ההקרנות הקיימות של הסרט עד לעוד שבוע מהיום

            // בניית הטבלה - כותרת ימים + שורות סלוטים
            Table tbl = new Table();
            tbl.CssClass = "weekSchedule";
            tbl.Attributes.Add("dir", "rtl"); // כיוון ימין לשמאל לעברית

            TableHeaderRow hr = new TableHeaderRow(); // שורת הכותרת עם שמות הימים
            hr.Cells.Add(new TableHeaderCell { Text = "שעה" }); // עמודה ראשונה שתציג את טווח השעות

            for (int i = 0; i < 7; i++)//עבור כל יום בשבוע הקרוב
            {
                DateTime dayDate = startOfVisibleWeek.AddDays(i);
                int dayIndex = ((int)dayDate.DayOfWeek - (int)DayOfWeek.Sunday + 7) % 7; // חישוב אינדקס שם היום מתוך המערך (0=ראשון, 6=שבת)
                var dayHeader = new TableHeaderCell();//(בניית עמודה חדשה בשורת הכותרת (תציג את שם היום ותאריך היום
                dayHeader.Controls.Add(new LiteralControl(//מוסיף לעמודה תוכן טקסטואלי שמציג את שם היום ותאריך היום
                    $"<span class=\"schedule-day-name\">{dayNames[dayIndex]}</span><br />" +
                    $"<span class=\"schedule-day-date\">{dayDate.ToString("dd/MM", culture)}</span>"));
                hr.Cells.Add(dayHeader);//מוסיף את העמודה לשורת הכותרת
            }

            tbl.Rows.Add(hr);//מוסיף את שורת הכותרת לטבלה


            foreach (var slot in dailySlots)//עבור כל סלוט ביום
            {
                TableRow row = new TableRow();
                // הצגת שעות הסלוט - שעת התחלה אחרי חצות מוצגת כ-24:00, שעת סיום מוצגת בפורמט רגיל (02:30)
                row.Cells.Add(new TableCell { Text = $"{FormatScheduleTime(slot.StartMin)} - {FormatScheduleTime(slot.EndMin)}" });

                for (int i = 0; i < 7; i++)//עבור כל סלוט עובר על ימי השבוע
                {
                    DateTime dayBase = startOfVisibleWeek.AddDays(i).AddHours(9);//מתחילים מהיום הראשון ב9
                    DateTime currentDayStart = dayBase.AddMinutes(slot.StartMin);//מחשבים את השעה ההתחלתית של הסלוט הנוכחי
                    DateTime currentDayEnd = dayBase.AddMinutes(slot.EndMin);//מחשבים את השעה הסופית של הסלוט הנוכחי
                    string cellKey = BuildCellKey(movieId, currentDayStart); // מפתח ייחודי לזיהוי הצ'קבוקס (המפתח מכיל מזהה+תאריך+שעה)
                    string slotKey = currentDayStart.ToString("yyyyMMddHHmm"); //(תאריך ושעה בפורמט yyyyMMddHHmm) יצירת מפתח ייחודי לזיהוי הסלוט  

                    TableCell cell = new TableCell();//בניית תא חדש בטבלה עבור הצ'קבוקס
                    cell.CssClass = "schedule-day-cell";
                    bool isPast = currentDayStart <= DateTime.Now; //בדיקה אם הסלוט עבר
                    existingBySlot.TryGetValue(slotKey, out ExistingScreening existing); //(הקרנה קיימת) יש ערך (slotKey) בדיקה בעזרת המילון אם למפתח
                    bool initiallyChecked = existing != null; //לא ריק - קיימת הקרנה existing אם
                    cell.Attributes["data-initial-checked"] = initiallyChecked ? "true" : "false"; // שמירת המצב ההתחלתי לזיהוי שינויים

                    if (existing != null) // קיימת הקרנה - הצ'קבוקס יופיע מסומן
                    {
                        cell.Controls.Add(CreateScheduleCheckBox(
                            movieId,
                            currentDayStart,
                            currentDayEnd,
                            cellKey,
                            existing.ScreeningId,
                            existing.Hall,
                            isChecked: true,
                            isEnabled: !isPast)); // ניתן לביטול רק אם ההקרנה עדיין לא עברה
                    }
                    else // לא קיימת הקרנה - הצ'קבוקס יופיע בלי סימון
                    {
                        bool canSchedule = !isPast && AnyHallAvailable(currentDayStart, currentDayEnd); 
                        cell.Controls.Add(CreateScheduleCheckBox(
                            movieId,
                            currentDayStart,
                            currentDayEnd,
                            cellKey,
                            screeningId: 0, // אין הקרנה קיימת
                            hallId: 0, // אולם ייבחר אוטומטית בעת ההוספה
                            isChecked: false,
                            isEnabled: canSchedule));// ניתן להוספה רק אם יש אולם פנוי והסלוט עתידי
                    }

                    row.Cells.Add(cell);//מוסיף את התא לשורה ומתקדם ליום הבא
                }

                tbl.Rows.Add(row);//מוסיף את השורה לטבלה ומתקדם לסלוט הבא
            }

            pnlSchedule.Controls.Clear(); // ניקוי טבלה קודמת אם קיימת
            pnlSchedule.Controls.Add(tbl); // הוספת הטבלה החדשה לפאנל
            pnlSchedule.Visible = true; // הצגת הפאנל

            btnAddScreening.Text = "עדכן הקרנות";
        }

        private static Control CreateScheduleCheckBox(
            int movieId,
            DateTime start,
            DateTime end,
            string cellKey,
            int screeningId,
            int hallId,
            bool isChecked,
            bool isEnabled)
        {
            var cb = new CheckBox//בניית צקבוקס עם כל המידע עליו
            {
                ID = "cb_" + cellKey,
                Checked = isChecked,
                Enabled = isEnabled,
                EnableViewState = true,
                CssClass = "checkbox__trigger visuallyhidden"
            };
            // בעת לחיצה על "עדכן הקרנות", הנתונים האלה נקראים ב-ProcessCheckboxChange
            cb.InputAttributes["data-info"] = $"{screeningId}|{movieId}|{start:yyyy-MM-dd HH:mm}|{end:yyyy-MM-dd HH:mm}|{hallId}";//של הצ׳קבוקס data-infoשמירת נתוני ההקרנה כמחרוזת ב
            cb.InputAttributes["data-initial-checked"] = isChecked ? "true" : "false"; // המצב ההתחלתי - להשוואה אם היה שינוי
            cb.InputAttributes["data-cell-key"] = cellKey; // מפתח ייחודי לזיהוי התא

            var wrapperClass = "checkbox-wrapper-33";
            if (isChecked)
                wrapperClass += " checkbox-wrapper-33--checked";//מוסיף עיצוב של תא שיש בו הקרנה
            if (!isEnabled)
                wrapperClass += " checkbox-wrapper-33--disabled";//מוסיף עיצוב של תא שלא ניתן לבחור אותו

            var wrapper = new Panel { CssClass = wrapperClass };
            var label = new System.Web.UI.HtmlControls.HtmlGenericControl("label");
            label.Attributes["class"] = "checkbox";//על כל צ׳קבוקס יש לייבל עם קלאס מתאים

            var symbol = new System.Web.UI.HtmlControls.HtmlGenericControl("span");
            var symbolClass = "checkbox__symbol";
            if (isChecked)
                symbolClass += " checkbox__symbol--checked";
            if (!isEnabled)
                symbolClass += " checkbox__symbol--disabled";
            symbol.Attributes["class"] = symbolClass;
            symbol.InnerHtml = @"<span class=""checkbox__check"" aria-hidden=""true"">&#10003;</span>";//שם וי במידה ויש הקרנה

            label.Controls.Add(cb);
            label.Controls.Add(symbol);
            wrapper.Controls.Add(label);

            return wrapper;
        }

        // יצירת מפתח ייחודי לתא בטבלה: מזהה הסרט + תאריך ושעה בפורמט yyyyMMddHHmm
        private static string BuildCellKey(int movieId, DateTime start) =>
            $"{movieId}_{start:yyyyMMddHHmm}";

        // שליפת כל ההקרנות הקיימות של סרט מסוים בשבוע הנוכחי
        // מחזיר עצם (מילון) שמציג טבלה עם שתי עמודות (מפתח וערך) - המילון משמש לבדיקה מהירה אם קיימת הקרנה בסלוט מסוים
        //  המציג את פרטי ההקרה ExistingScreening ערך הוא עצם מטיפוס  ,yyyyMMddHHmm מפתח הוא מחרוזת שמציגה תאריך ושעה בפורמט  
        private Dictionary<string, ExistingScreening> LoadMovieScreeningsForWeek(int movieId, DateTime weekStart)
        {
            var result = new Dictionary<string, ExistingScreening>();
            string query = @"
                SELECT ScreeningId, Hall, StartTime, EndTime
                FROM Screening
                WHERE MovieId = @MovieId
                  AND StartTime >= @WeekStart
                  AND StartTime < @WeekEnd";

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, query, "Screening");
            d1.Params.Add(new SqlParameter("@MovieId", movieId));
            d1.Params.Add(new SqlParameter("@WeekStart", weekStart));
            d1.Params.Add(new SqlParameter("@WeekEnd", weekStart.AddDays(8))); // 8 ימים כדי לכלול גם הקרנות שחוצות חצות ביום האחרון

            foreach (DataRow row in d1.GetTableWithParams().Rows)//מעבר על כל שורה בטבלה שנוצרה מהשאילתה
            {
                var start = (DateTime)row["StartTime"];
                result[start.ToString("yyyyMMddHHmm")] = new ExistingScreening//עבור כל מפתח יחודי במילון (תאריך ההקרנה), נוצר ערך - עצם חדש של פרטי ההקרנה
                {
                    ScreeningId = Convert.ToInt32(row["ScreeningId"]),
                    Hall = Convert.ToInt32(row["Hall"]),
                    StartTime = start,
                    EndTime = (DateTime)row["EndTime"]
                };
            }

            return result;//מחזיר את כל ההקרנות הקיימות של הסרט בשבוע הנוכחי בתור מילון
        }

        // אירוע שינוי סרט בתפריט - בונה את טבלת הסלוטים עבור הסרט שנבחר
        protected void ddlMovies_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblMessage.Text = ""; // ניקוי הודעה קודמת

            if (ddlMovies.SelectedIndex == 0) // חזרה ל"אנא בחר סרט"
            {
                pnlSchedule.Visible = false; // הסתרת הטבלה
                return;
            }

            RebuildScheduleTable(int.Parse(ddlMovies.SelectedValue)); // בנייה מחדש עם הסרט החדש
        }

        // אירוע לחיצה על "עדכן הקרנות" - עובר על כל הצ'קבוקסים בטבלה ומזהה שינויים
        // צ'קבוקס שסומן (ולא היה מסומן) → הוספת הקרנה חדשה
        // צ'קבוקס שבוטל (והיה מסומן) → מחיקת הקרנה קיימת
        protected void btnAddScreening_Click(object sender, EventArgs e)
        {
            if (ddlMovies.SelectedIndex == 0) // לא נבחר סרט
            {
                lblMessage.Text = "אנא בחר סרט.";
                lblMessage.CssClass = "editorMsg";
                return;
            }

            int movieId = int.Parse(ddlMovies.SelectedValue);
            string movieTitle = ddlMovies.SelectedItem.Text;
            var culture = new CultureInfo("he-IL");
            var added = new List<string>(); // רשימת הקרנות שנוספו (להודעת סיכום)
            var removed = new List<string>(); // רשימת הקרנות שבוטלו
            var errors = new List<string>(); // רשימת שגיאות (אולם תפוס, כרטיסים נמכרו)

            // מעבר על כל הצ'קבוקסים בטבלה - חיפוש שינויים מהמצב ההתחלתי
            foreach (Control row in pnlSchedule.Controls)
            {
                if (row is Table tbl)
                {
                    foreach (TableRow tr in tbl.Rows)
                    {
                        if (tr is TableHeaderRow) // דילוג על שורת הכותרת
                            continue;

                        for (int i = 1; i < tr.Cells.Count; i++) // מתחילים מ-1 כי תא 0 = שעות הסלוט
                        {
                            TableCell cell = tr.Cells[i];
                            if (!cell.HasControls())//בדיקה אם יש צ׳קבוקס בתא
                                continue;

                            // חיפוש צ'קבוקסים בתוך התא (הם עטופים ב-wrapper)
                            foreach (CheckBox cb in FindScheduleCheckBoxes(cell))
                                ProcessCheckboxChange(cb, movieTitle, culture, added, removed, errors);
                        }
                    }
                }
            }

            RebuildScheduleTable(movieId); // בנייה מחדש של הטבלה כדי לשקף את השינויים

            // הצגת הודעת סיכום למנהל
            if (added.Count > 0 || removed.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("<div class=\"editorMsgSuccess\">");
                sb.Append("<strong>ההקרנות עודכנו בהצלחה</strong>");

                foreach (string line in added) // פירוט כל הקרנה שנוספה
                    sb.Append($"<span class=\"editorMsg-item\">{line}</span>");

                foreach (string line in removed) // פירוט כל הקרנה שבוטלה
                    sb.Append($"<span class=\"editorMsg-item\">{line}</span>");

                sb.Append("</div>");
                lblMessage.Text = sb.ToString();
                lblMessage.CssClass = "";
            }
            else if (errors.Count > 0) // היו שגיאות בלבד
            {
                lblMessage.Text = string.Join("<br>", errors);
                lblMessage.CssClass = "editorMsg";
            }
            else // לא היו שינויים כלל
            {
                lblMessage.Text = "לא בוצעו שינויים.";
                lblMessage.CssClass = "editorMsg";
            }
        }

        // חיפוש רקורסיבי של צ'קבוקסים בתוך עץ הפקדים של תא בטבלה
        // yield return מחזיר כל צ'קבוקס שנמצא מבלי ליצור רשימה בזיכרון
        private static IEnumerable<CheckBox> FindScheduleCheckBoxes(Control root)
        {
            if (root is CheckBox cb && root.ID != null && root.ID.StartsWith("cb_"))
                yield return cb; // נמצא צ'קבוקס של סלוט (מתחיל ב-"cb_")

            foreach (Control child in root.Controls) // חיפוש בפקדי הילדים (הצ'קבוקס עטוף ב-wrapper)
            {
                foreach (CheckBox nested in FindScheduleCheckBoxes(child))
                    yield return nested;
            }
        }

        // טיפול בצ'קבוקס בודד - בדיקה אם השתנה מהמצב ההתחלתי וביצוע הפעולה המתאימה
        private void ProcessCheckboxChange(
            CheckBox cb,
            string movieTitle,
            CultureInfo culture,
            List<string> added,
            List<string> removed,
            List<string> errors)
        {
            // השוואת המצב הנוכחי למצב ההתחלתי (שנשמר ב-data-initial-checked)
            bool initiallyChecked = string.Equals(
                cb.InputAttributes["data-initial-checked"], "true", StringComparison.OrdinalIgnoreCase);
            bool currentlyChecked = cb.Checked;

            if (initiallyChecked == currentlyChecked) // לא השתנה - אין מה לעשות
                return;

            // פענוח נתוני ההקרנה מה-data-info: "screeningId|movieId|start|end|hallId"
            string data = cb.InputAttributes["data-info"];
            if (string.IsNullOrEmpty(data))
                return;

            string[] parts = data.Split('|'); // פיצול המחרוזת לחלקים לפי התו |
            int screeningId = int.Parse(parts[0]);
            int movieId = int.Parse(parts[1]);
            DateTime start = DateTime.Parse(parts[2]);
            DateTime end = DateTime.Parse(parts[3]);
            int hallId = int.Parse(parts[4]);

            if (!initiallyChecked && currentlyChecked) // הצ'קבוקס סומן → הוספת הקרנה חדשה
            {
                CleanupOverlappingUnsoldScreenings(movieId, start, end); // מחיקת הקרנות חופפות ללא כרטיסים

                if (IsMovieAlreadyScheduled(movieId, start, end)) // בדיקה אם נשארה הקרנה חופפת עם כרטיסים
                {
                    errors.Add($"כבר קיימת הקרנה לסרט {movieTitle} ב-{FormatSlotWhen(start, end, culture)} שכבר נמכרו לה כרטיסים.");
                    return;
                }
//בדיקה הכרחית למקרה ולאתר יש יותר ממנהל אחד
                hallId = FindAvailableHall(start, end); // חיפוש אולם פנוי בטווח הזמן
                if (hallId == -1) // כל האולמות תפוסים
                {
                    errors.Add($"אין אולם פנוי ב-{FormatSlotWhen(start, end, culture)}.");
                    return;
                }

                InsertScreening(movieId, hallId, start, end); // הוספת ההקרנה למסד הנתונים
                added.Add($"נוספה: {movieTitle}, אולם {hallId}, {FormatSlotWhen(start, end, culture)}");
            }
            else if (initiallyChecked && !currentlyChecked) // הצ'קבוקס בוטל → מחיקת הקרנה קיימת
            {
                if (screeningId <= 0)
                    return;

                if (!TryDeleteScreening(screeningId, out string deleteError)) // ניסיון מחיקה (נכשל אם נמכרו כרטיסים)
                {
                    errors.Add(deleteError);
                    return;
                }

                removed.Add($"בוטלה: {movieTitle}, אולם {hallId}, {FormatSlotWhen(start, end, culture)}");
            }
        }

        // עיצוב תאריך ושעה להודעת סיכום, למשל: "שישי 06/06/2026, 09:00–11:30"
        private static string FormatSlotWhen(DateTime start, DateTime end, CultureInfo culture)
        {
            string day = start.ToString("dddd dd/MM/yyyy", culture); // שם היום + תאריך בפורמט עברי
            return $"{day}, {start:HH:mm}–{end:HH:mm}";
        }

        // מחיקת הקרנות חופפות של אותו סרט שאין להן כרטיסים מכורים
        // נקרא לפני הוספת הקרנה חדשה כדי למנוע כפילויות
        // תנאי החפיפה: ההקרנה החדשה מתחילה לפני שהקיימת נגמרת, ונגמרת אחרי שהקיימת מתחילה
        private void CleanupOverlappingUnsoldScreenings(int movieId, DateTime start, DateTime end)
        {
            string query = @"
                DELETE FROM Screening 
                WHERE MovieId = @MovieId 
                  AND SeatesBought = 0
                  AND (@Start < EndTime AND @End > StartTime)";

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d = new DAL(con, query, "Screening");
            d.Params.Add(new SqlParameter("@MovieId", movieId));
            d.Params.Add(new SqlParameter("@Start", start));
            d.Params.Add(new SqlParameter("@End", end));
            d.ExecuteNonQueryDalPar();
        }

        // הוספת הקרנה חדשה למסד הנתונים עם 0 כרטיסים מכורים
        private void InsertScreening(int movieId, int hallId, DateTime start, DateTime end)
        {
            string query = @"
                INSERT INTO Screening (MovieId, Hall, StartTime, EndTime, SeatesBought)
                VALUES (@MovieId, @Hall, @StartTime, @EndTime, 0)";

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d = new DAL(con, query, "Screening");
            d.Params.Add(new SqlParameter("@MovieId", movieId));
            d.Params.Add(new SqlParameter("@Hall", hallId));
            d.Params.Add(new SqlParameter("@StartTime", start));
            d.Params.Add(new SqlParameter("@EndTime", end));
            d.ExecuteNonQueryDalPar();
        }

        // ניסיון מחיקת הקרנה - מצליח רק אם לא נמכרו כרטיסים
        // מחזיר true אם המחיקה הצליחה, false + הודעת שגיאה אם לא
        private bool TryDeleteScreening(int screeningId, out string error)
        {
            error = null; // out parameter - חייב לקבל ערך לפני שהפונקציה מחזירה
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (var conn = new SqlConnection(con))
            {
                conn.Open();
                // שלב 1: בדיקה כמה כרטיסים נמכרו להקרנה
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(SeatesBought, 0) FROM Screening WHERE ScreeningId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", screeningId);
                    object seats = cmd.ExecuteScalar(); // מחזיר ערך בודד (מספר הכרטיסים)
                    if (seats == null) // ההקרנה לא קיימת במסד
                    {
                        error = "ההקרנה לא נמצאה.";
                        return false;
                    }

                    if (Convert.ToInt32(seats) > 0) // יש כרטיסים מכורים - אסור למחוק
                    {
                        error = "לא ניתן לבטל הקרנה שכבר נמכרו לה כרטיסים.";
                        return false;
                    }
                }

                // שלב 2: מחיקת ההקרנה (רק אם עברנו את הבדיקות)
                using (var cmd = new SqlCommand("DELETE FROM Screening WHERE ScreeningId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", screeningId);
                    cmd.ExecuteNonQuery(); // ביצוע שאילתה שלא מחזירה תוצאות
                }
            }

            return true; // המחיקה הצליחה
        }

        // מילוי תפריט הסרטים מטבלת Movie - הטקסט = שם הסרט, הערך = מזהה הסרט
        private void LoadMovies(string con)
        {
            DAL dAL = new DAL(con, "SELECT Id, Title FROM Movie", "Movie");
            ddlMovies.DataSource = dAL.GetData(); // קישור נתוני הטבלה לתפריט
            ddlMovies.DataTextField = "Title"; // העמודה שתוצג למשתמש
            ddlMovies.DataValueField = "Id"; // העמודה שתישלח כערך בטופס
            ddlMovies.DataBind(); // ביצוע הקישור
            ddlMovies.Items.Insert(0, "אנא בחר סרט"); // הוספת פריט ברירת מחדל במקום הראשון
        }


        // חיפוש אולם פנוי בטווח זמן מסוים - עובר על כל האולמות ומחזיר את הראשון שפנוי
        // מחזיר -1 אם כל האולמות תפוסים
        private int FindAvailableHall(DateTime newStart, DateTime newEnd)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, "SELECT HallId FROM Halls", "Halls");

            foreach (DataRow row in d1.GetData().Rows)
            {
                int hallId = Convert.ToInt32(row["HallId"]);
                if (IsHallAvailable(hallId, newStart, newEnd)) // בדיקה אם האולם פנוי בזמן הזה
                    return hallId; // נמצא אולם פנוי - מחזירים אותו
            }

            return -1; // לא נמצא אולם פנוי
        }

        // שליפת אורך הסרט בדקות מטבלת Movie
        private int GetMovieDuration(int movieId)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL dAL = new DAL(con, "SELECT Duration FROM Movie WHERE Id=" + movieId, "Movie");
            return Convert.ToInt32(dAL.GetData().Rows[0]["Duration"]); // שליפת הערך מהשורה הראשונה (והיחידה)
        }

        // חישוב אורך הסלוט: עיגול אורך הסרט כלפי מעלה ל-5 דקות + 35 דקות (20 ניקיון + 15 פרסומות)
        // לדוגמה: סרט של 119 דק' → עיגול ל-120 + 35 = 155 דק' → סלוטים ב-9:00, 11:35, 14:10...
        private int GetRoundedDuration(int movieId)
        {
            int duration = GetMovieDuration(movieId);
            int remainder = duration % 5; // בדיקה כמה דקות חסרות לכפולה הקרובה של 5
            if (remainder != 0)
                duration += (5 - remainder); // עיגול כלפי מעלה לכפולה של 5

            return duration + 20 + 15; // אורך הסרט המעוגל + 20 דקות ניקיון + 15 דקות פרסומות = אורך הסלוט
        }

        // יצירת רשימת סלוטים רציפים ליום אחד, כהפרשי דקות מ-09:00
        // לדוגמה: סרט עם סלוט של 150 דקות → (0,150), (150,300), (300,450)... = 09:00, 11:30, 14:00...
        // הסלוט האחרון יכול לחצות את חצות (למשל 24:00-02:30)
        private List<(int StartMin, int EndMin)> GenerateSequentialSchedule(int totalMinutes)
        {
            var schedule = new List<(int, int)>();
            int offset = 0;
            int maxStartOffset = 16 * 60; // סלוטים מתחילים עד 01:00 (16 שעות = 960 דקות אחרי 09:00)

            while (offset < maxStartOffset)
            {
                schedule.Add((offset, offset + totalMinutes));
                offset += totalMinutes;
            }

            return schedule;
        }

        // שעת התחלה בחצות → 24:xx; שעות 01:xx–23:xx אחרי חצות → HH:mm רגיל
        private static string FormatScheduleTime(int minutesFrom9)
        {
            int hour = 9 + minutesFrom9 / 60;
            int min = minutesFrom9 % 60;
            if (hour > 24)
                hour -= 24;
            return $"{hour:D2}:{min:D2}";
        }

        // בדיקה אם יש לפחות אולם אחד פנוי בטווח זמן מסוים
        // משמש לקביעה אם הצ'קבוקס יהיה ניתן ללחיצה (enabled)
        private bool AnyHallAvailable(DateTime newStart, DateTime newEnd)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, "SELECT HallId FROM Halls", "Halls");

            foreach (DataRow row in d1.GetData().Rows)
            {
                if (IsHallAvailable(Convert.ToInt32(row["HallId"]), newStart, newEnd))
                    return true; // מספיק שאולם אחד פנוי
            }

            return false; // כל האולמות תפוסים בזמן הזה
        }

        // בדיקה אם לסרט כבר יש הקרנה חופפת (בכל אולם) - בודק חפיפת זמנים
        private bool IsMovieAlreadyScheduled(int movieId, DateTime newStart, DateTime newEnd)
        {
            string query = @"
                SELECT COUNT(*)
                FROM Screening
                WHERE MovieId = @MovieId
                  AND (@NewStart < EndTime AND @NewEnd > StartTime)"; // תנאי חפיפה: ההקרנות חופפות אם אחת מתחילה לפני שהשנייה נגמרת

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, query, "Screening");
            d1.Params.Add(new SqlParameter("@MovieId", movieId));
            d1.Params.Add(new SqlParameter("@NewStart", newStart));
            d1.Params.Add(new SqlParameter("@NewEnd", newEnd));

            return Convert.ToInt32(d1.ExecuteScalarDalPar()) > 0; // true אם נמצאה לפחות הקרנה חופפת אחת
        }

        // בדיקה אם אולם ספציפי פנוי בטווח זמן מסוים (אין הקרנה חופפת באולם)
        private bool IsHallAvailable(int hallId, DateTime newStart, DateTime newEnd)
        {
            string query = @"
                SELECT COUNT(*)
                FROM Screening
                WHERE Hall = @Hall
                  AND (@NewStart < EndTime AND @NewEnd > StartTime)"; // אותו תנאי חפיפה

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, query, "Screening");
            d1.Params.Add(new SqlParameter("@Hall", hallId));
            d1.Params.Add(new SqlParameter("@NewStart", newStart));
            d1.Params.Add(new SqlParameter("@NewEnd", newEnd));

            return Convert.ToInt32(d1.ExecuteScalarDalPar()) == 0; // true אם האולם פנוי (0 הקרנות חופפות)
        }

        // מחלקה פנימית לייצוג הקרנה קיימת שנשלפה מהמסד - משמשת בטבלת הסלוטים
        private sealed class ExistingScreening
        {
            public int ScreeningId { get; set; } // מזהה ייחודי של ההקרנה
            public int Hall { get; set; } // מספר האולם
            public DateTime StartTime { get; set; } // שעת התחלה
            public DateTime EndTime { get; set; } // שעת סיום
        }
    }
}
