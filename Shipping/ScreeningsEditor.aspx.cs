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
            // קוראים את הסרט מה-Request (PostBack-safe)
            if (!string.IsNullOrEmpty(Request.Form[ddlMovies.UniqueID]))//קורא איזה סרט נבחר גם ב־PostBack
            {
                int movieId;
                if (int.TryParse(Request.Form[ddlMovies.UniqueID], out movieId))
                {
                    RebuildScheduleTable(movieId);
                }
            }
        }


        private void RebuildScheduleTable(int movieId)
        {
            int slotMinutes = GetRoundedDuration(movieId);//רשימת טווחי שעות רציפים
            var dailySlots = GenerateSequentialSchedule(slotMinutes);

            // ימים מימין לשמאל
            string[] days = { "ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת" };

            Table tbl = new Table();
            tbl.CssClass = "weekSchedule";
            tbl.Attributes.Add("dir", "rtl"); // קריטי!

            // כותרת
            TableHeaderRow hr = new TableHeaderRow();

            hr.Cells.Add(new TableHeaderCell { Text = "שעה" }); //עמודת השעות

            foreach (string day in days) // מימין לשמאל
                hr.Cells.Add(new TableHeaderCell { Text = day });

            tbl.Rows.Add(hr);

            // שורות שעות
            foreach (var slot in dailySlots)//עבור כל טווח שעות שורה חדשה בכל יום
            {
                TableRow row = new TableRow();

                // עמודת שעה
                row.Cells.Add(new TableCell
                {
                    Text = $"{slot.Start:HH:mm} - {slot.End:HH:mm}"
                });

                // תאים לימי השבוע
                for (int i = 0; i < 7; i++)
                {
                    DateTime start = slot.Start.AddDays(i);
                    DateTime end = slot.End.AddDays(i);

                    TableCell cell = new TableCell();

                    bool available = AnyHallAvailable(start, end);

                    if (available)
                    {
                        CheckBox cb = new CheckBox();
                        cb.ID = $"cb_{movieId}_{start:yyyyMMddHHmm}";//יחודי מבוסס על הסרט והתאריך Id CheckBoxקובע ל
                        cb.CssClass = "circleCheck";
                        cb.EnableViewState = true;//PostBackמאפשר שמירת מצב סימון ב
                        cb.Attributes["data-info"] = $"{movieId}|{start}|{end}";//שמירת מידע נסתר, משמש בהוספת הקרנה
                        cb.Attributes.Add("onclick", "showAddButton(this);");

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

            pnlSchedule.Controls.Clear();
            pnlSchedule.Controls.Add(tbl); //מציג את הטבלה בפאנל
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

        // ------------------------------
        // טוען את כל האולמות
        // ------------------------------
        private void LoadHalls(string con)
        {
            DAL dAL = new DAL(con, "SELECT HallId FROM Halls", "Halls");
            ddlHalls.DataSource = dAL.GetData();
            ddlHalls.DataTextField = "HallId";
            ddlHalls.DataValueField = "HallId";
            ddlHalls.DataBind();
            ddlHalls.Items.Insert(0, "אולם");

        }

        // ------------------------------------------------------
        // כשהמשתמש בוחר סרט → מחשבים את זמן הסרט ושעות פנויות
        // ------------------------------------------------------
        protected void ddlMovies_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblMessage.Text = "";

            // מנקים רק מצב נראות
            btnAddScreening.CssClass = "btnAddS hiddenBtn";

            if (ddlMovies.SelectedIndex == 0)
                return;

            int movieId = int.Parse(ddlMovies.SelectedValue);
            RebuildScheduleTable(movieId);
        }

        protected void RadioSlot_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            string data = rb.Attributes["data-info"];

            string[] parts = data.Split('|');

            int movieId = int.Parse(parts[0]);
            DateTime start = DateTime.Parse(parts[1]);
            DateTime end = DateTime.Parse(parts[2]);

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

            ViewState["SelectedMovie"] = parts[0];
            ViewState["SelectedStart"] = parts[1];
            ViewState["SelectedEnd"] = parts[2];

            // הצגת הכפתור הקיים
            btnAddScreening.Visible = true;

            lblMessage.Text = "בחרת הקרנה. לחץ על 'הוסף הקרנה' כדי לאשר.";
            lblMessage.ForeColor = System.Drawing.Color.Black;
        }
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


        // ----------------------------------------------------
        // פונקציה שמחזירה משך סרט בדקות מתוך טבלת Movies
        // ----------------------------------------------------
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

        // ---------------------------------------------------------
        // מוצא את כל השעות הפנויות מ-09:00 עד 00:30 בקפיצות של 30 דק'
        // ---------------------------------------------------------
        private List<DateTime> FindAvailableTimes(int duration)
        {
            List<DateTime> free = new List<DateTime>();

            DateTime start = DateTime.Today.AddHours(9); // 09:00
            DateTime end = DateTime.Today.AddDays(1).AddMinutes(30); // 00:30

            while (start.AddMinutes(duration) <= end)
            {
                // בודק אם יש אולם כלשהו פנוי בזמן הזה
                if (AnyHallAvailable(start, start.AddMinutes(duration)))
                {
                    free.Add(start);
                }

                start = start.AddMinutes(30); // קפיצה של חצי שעה
            }

            return free;
        }

        // ---------------------------------------------------------
        // בודק האם *יש לפחות אולם אחד פנוי* בזמן הזה
        // ---------------------------------------------------------
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
                    return true; // מצאנו אולם פנוי → מספיק
            }

            return false; // אין אף אולם פנוי
        }

        // ---------------------------------------------------------
        // בודק האם אולם ספציפי פנוי בזמן הזה
        // ---------------------------------------------------------
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

        // ---------------------------------------------------------
        // הוספת הקרנה למסד הנתונים
        // ---------------------------------------------------------
        protected void btnAddScreening_Click(object sender, EventArgs e)
        {
            int panelCount = pnlSchedule.Controls.Count;

            List<string> added = new List<string>();

            foreach (Control row in pnlSchedule.Controls)
            {
                if (row is Table tbl)
                {
                    foreach (TableRow tr in tbl.Rows)
                    {
                        // דילוג על שורת כותרת
                        if (tr is TableHeaderRow)
                            continue;

                        for (int i = 1; i < tr.Cells.Count; i++) // ⬅️ מדלגים על עמודת השעה
                        {
                            TableCell cell = tr.Cells[i];

                            if (!cell.HasControls())
                                continue;
                            foreach (Control c in cell.Controls)
                            {
                                if (c is CheckBox cb && cb.Checked)
                                {
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

                                        added.Add($"{start:HH:mm} - {end:HH:mm} ביום {start:dddd}");
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
                lblMessage.Text = "הקרנות נוספו בהצלחה:<br>" + string.Join("<br>", added);
            }
            else
            {
                lblMessage.ForeColor = System.Drawing.Color.Red;
                lblMessage.Text = "לא נבחרו הקרנות או שאין אולם פנוי.";
            }

            btnAddScreening.CssClass = "btnAddS hiddenBtn";
        }



    }
}