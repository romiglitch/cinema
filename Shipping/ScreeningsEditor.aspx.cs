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
    public partial class ScreeningsEditor : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["category"] != "admin")
                Response.Redirect("Login.aspx");

            if (!IsPostBack)
            {
                string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                LoadMovies(con);
                LoadHalls(con);
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Request.Form[ddlMovies.UniqueID]))
            {
                if (int.TryParse(Request.Form[ddlMovies.UniqueID], out int movieId))
                    RebuildScheduleTable(movieId);
            }
        }

        private void RebuildScheduleTable(int movieId)
        {
            int slotMinutes = GetRoundedDuration(movieId);
            var dailySlots = GenerateSequentialSchedule(slotMinutes);

            string[] days = { "ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת" };
            var culture = new CultureInfo("he-IL");

            DateTime today = DateTime.Today;
            int daysSinceSunday = ((int)today.DayOfWeek - (int)DayOfWeek.Sunday + 7) % 7;
            DateTime startOfVisibleWeek = today.AddDays(-daysSinceSunday);

            var existingBySlot = LoadMovieScreeningsForWeek(movieId, startOfVisibleWeek);
            var highlightedCells = ViewState["UpdatedCellKeys"] as List<string> ?? new List<string>();

            Table tbl = new Table();
            tbl.CssClass = "weekSchedule";
            tbl.Attributes.Add("dir", "rtl");

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

            foreach (var slot in dailySlots)
            {
                TableRow row = new TableRow();
                row.Cells.Add(new TableCell { Text = $"{slot.Start:HH:mm} - {slot.End:HH:mm}" });

                for (int i = 0; i < 7; i++)
                {
                    DateTime currentDayStart = startOfVisibleWeek.AddDays(i).Add(slot.Start.TimeOfDay);
                    DateTime currentDayEnd = startOfVisibleWeek.AddDays(i).Add(slot.End.TimeOfDay);
                    string cellKey = BuildCellKey(movieId, currentDayStart);
                    string slotKey = currentDayStart.ToString("yyyyMMddHHmm");

                    TableCell cell = new TableCell();
                    bool isPast = currentDayStart <= DateTime.Now;
                    existingBySlot.TryGetValue(slotKey, out ExistingScreening existing);

                    if (existing != null)
                    {
                        cell.Controls.Add(CreateScheduleCheckBox(
                            movieId,
                            currentDayStart,
                            currentDayEnd,
                            cellKey,
                            existing.ScreeningId,
                            existing.Hall,
                            isChecked: true,
                            isEnabled: !isPast));
                    }
                    else
                    {
                        bool canSchedule = !isPast && AnyHallAvailable(currentDayStart, currentDayEnd);
                        cell.Controls.Add(CreateScheduleCheckBox(
                            movieId,
                            currentDayStart,
                            currentDayEnd,
                            cellKey,
                            screeningId: 0,
                            hallId: 0,
                            isChecked: false,
                            isEnabled: canSchedule));
                    }

                    if (highlightedCells.Contains(cellKey))
                        cell.CssClass = "schedule-cell-updated";

                    row.Cells.Add(cell);
                }

                tbl.Rows.Add(row);
            }

            pnlSchedule.Controls.Clear();
            pnlSchedule.Controls.Add(tbl);
            pnlSchedule.Visible = true;

            btnAddScreening.Text = "עדכן הקרנות";
            btnAddScreening.CssClass = "btnAddS showBtn";
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
            var cb = new CheckBox
            {
                ID = "cb_" + cellKey,
                Checked = isChecked,
                Enabled = isEnabled,
                EnableViewState = true,
                CssClass = "checkbox__trigger visuallyhidden"
            };

            cb.Attributes["data-info"] = $"{screeningId}|{movieId}|{start:yyyy-MM-dd HH:mm}|{end:yyyy-MM-dd HH:mm}|{hallId}";
            cb.Attributes["data-initial-checked"] = isChecked ? "true" : "false";
            cb.Attributes["data-cell-key"] = cellKey;

            if (isEnabled)
                cb.Attributes["onclick"] = "onScheduleCheckboxClick(this);";

            var wrapperClass = "checkbox-wrapper-33";
            if (isChecked)
                wrapperClass += " checkbox-wrapper-33--checked";
            if (!isEnabled)
                wrapperClass += " checkbox-wrapper-33--disabled";

            var wrapper = new Panel { CssClass = wrapperClass };
            var label = new System.Web.UI.HtmlControls.HtmlGenericControl("label");
            label.Attributes["class"] = "checkbox";

            var symbol = new System.Web.UI.HtmlControls.HtmlGenericControl("span");
            symbol.Attributes["class"] = "checkbox__symbol";
            symbol.InnerHtml =
                @"<svg aria-hidden=""true"" class=""icon-checkbox"" width=""28px"" height=""28px"" viewBox=""0 0 28 28"" version=""1"" xmlns=""http://www.w3.org/2000/svg"">" +
                @"<path d=""M4 14l8 7L24 7""></path></svg>";

            label.Controls.Add(cb);
            label.Controls.Add(symbol);
            wrapper.Controls.Add(label);

            return wrapper;
        }

        private static string BuildCellKey(int movieId, DateTime start) =>
            $"{movieId}_{start:yyyyMMddHHmm}";

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
            d1.Params.Add(new SqlParameter("@WeekEnd", weekStart.AddDays(7)));

            foreach (DataRow row in d1.GetTableWithParams().Rows)
            {
                var start = (DateTime)row["StartTime"];
                result[start.ToString("yyyyMMddHHmm")] = new ExistingScreening
                {
                    ScreeningId = Convert.ToInt32(row["ScreeningId"]),
                    Hall = Convert.ToInt32(row["Hall"]),
                    StartTime = start,
                    EndTime = (DateTime)row["EndTime"]
                };
            }

            return result;
        }

        protected void ddlMovies_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            ViewState["UpdatedCellKeys"] = null;
            btnAddScreening.CssClass = "btnAddS hiddenBtn";

            if (ddlMovies.SelectedIndex == 0)
            {
                pnlSchedule.Visible = false;
                return;
            }

            RebuildScheduleTable(int.Parse(ddlMovies.SelectedValue));
        }

        protected void btnAddScreening_Click(object sender, EventArgs e)
        {
            if (ddlMovies.SelectedIndex == 0)
            {
                lblMessage.Text = "אנא בחר סרט.";
                lblMessage.CssClass = "editorMsg";
                return;
            }

            int movieId = int.Parse(ddlMovies.SelectedValue);
            string movieTitle = ddlMovies.SelectedItem.Text;
            var culture = new CultureInfo("he-IL");
            var added = new List<string>();
            var removed = new List<string>();
            var changedCellKeys = new List<string>();
            var errors = new List<string>();

            foreach (Control row in pnlSchedule.Controls)
            {
                if (row is Table tbl)
                {
                    foreach (TableRow tr in tbl.Rows)
                    {
                        if (tr is TableHeaderRow)
                            continue;

                        for (int i = 1; i < tr.Cells.Count; i++)
                        {
                            TableCell cell = tr.Cells[i];
                            if (!cell.HasControls())
                                continue;

                            foreach (CheckBox cb in FindScheduleCheckBoxes(cell))
                                ProcessCheckboxChange(cb, movieTitle, culture, added, removed, changedCellKeys, errors);
                        }
                    }
                }
            }

            ViewState["UpdatedCellKeys"] = changedCellKeys;
            RebuildScheduleTable(movieId);

            if (added.Count > 0 || removed.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("<div class=\"editorMsgSuccess\">");
                sb.Append("<strong>ההקרנות עודכנו בהצלחה</strong>");

                foreach (string line in added)
                    sb.Append($"<span class=\"editorMsg-item\">{line}</span>");

                foreach (string line in removed)
                    sb.Append($"<span class=\"editorMsg-item\">{line}</span>");

                sb.Append("</div>");
                lblMessage.Text = sb.ToString();
                lblMessage.CssClass = "";
            }
            else if (errors.Count > 0)
            {
                lblMessage.Text = string.Join("<br>", errors);
                lblMessage.CssClass = "editorMsg";
            }
            else
            {
                lblMessage.Text = "לא בוצעו שינויים.";
                lblMessage.CssClass = "editorMsg";
            }
        }

        private static IEnumerable<CheckBox> FindScheduleCheckBoxes(Control root)
        {
            if (root is CheckBox cb && root.ID != null && root.ID.StartsWith("cb_"))
                yield return cb;

            foreach (Control child in root.Controls)
            {
                foreach (CheckBox nested in FindScheduleCheckBoxes(child))
                    yield return nested;
            }
        }

        private void ProcessCheckboxChange(
            CheckBox cb,
            string movieTitle,
            CultureInfo culture,
            List<string> added,
            List<string> removed,
            List<string> changedCellKeys,
            List<string> errors)
        {
            bool initiallyChecked = cb.Attributes["data-initial-checked"] == "true";
            bool currentlyChecked = cb.Checked;

            if (initiallyChecked == currentlyChecked)
                return;

            string data = cb.Attributes["data-info"];
            if (string.IsNullOrEmpty(data))
                return;

            string[] parts = data.Split('|');
            int screeningId = int.Parse(parts[0]);
            int movieId = int.Parse(parts[1]);
            DateTime start = DateTime.Parse(parts[2]);
            DateTime end = DateTime.Parse(parts[3]);
            int hallId = int.Parse(parts[4]);

            string cellKey = cb.Attributes["data-cell-key"];
            if (!string.IsNullOrEmpty(cellKey))
                changedCellKeys.Add(cellKey);

            if (!initiallyChecked && currentlyChecked)
            {
                if (IsMovieAlreadyScheduled(movieId, start, end))
                {
                    errors.Add($"כבר קיימת הקרנה לסרט {movieTitle} ב-{FormatSlotWhen(start, end, culture)}.");
                    return;
                }

                hallId = FindAvailableHall(start, end);
                if (hallId == -1)
                {
                    errors.Add($"אין אולם פנוי ב-{FormatSlotWhen(start, end, culture)}.");
                    return;
                }

                InsertScreening(movieId, hallId, start, end);
                added.Add($"נוספה: {movieTitle}, אולם {hallId}, {FormatSlotWhen(start, end, culture)}");
            }
            else if (initiallyChecked && !currentlyChecked)
            {
                if (screeningId <= 0)
                    return;

                if (!TryDeleteScreening(screeningId, out string deleteError))
                {
                    errors.Add(deleteError);
                    return;
                }

                removed.Add($"בוטלה: {movieTitle}, אולם {hallId}, {FormatSlotWhen(start, end, culture)}");
            }
        }

        private static string FormatSlotWhen(DateTime start, DateTime end, CultureInfo culture)
        {
            string day = start.ToString("dddd dd/MM/yyyy", culture);
            return $"{day}, {start:HH:mm}–{end:HH:mm}";
        }

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
            d.ExecuteScalarDalPar();
        }

        private bool TryDeleteScreening(int screeningId, out string error)
        {
            error = null;
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (var conn = new SqlConnection(con))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(SeatesBought, 0) FROM Screening WHERE ScreeningId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", screeningId);
                    object seats = cmd.ExecuteScalar();
                    if (seats == null)
                    {
                        error = "ההקרנה לא נמצאה.";
                        return false;
                    }

                    if (Convert.ToInt32(seats) > 0)
                    {
                        error = "לא ניתן לבטל הקרנה שכבר נמכרו לה כרטיסים.";
                        return false;
                    }
                }

                using (var cmd = new SqlCommand("DELETE FROM Screening WHERE ScreeningId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", screeningId);
                    cmd.ExecuteNonQuery();
                }
            }

            return true;
        }

        private void LoadMovies(string con)
        {
            DAL dAL = new DAL(con, "SELECT Id, Title FROM Movie", "Movie");
            ddlMovies.DataSource = dAL.GetData();
            ddlMovies.DataTextField = "Title";
            ddlMovies.DataValueField = "Id";
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

        private int FindAvailableHall(DateTime newStart, DateTime newEnd)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, "SELECT HallId FROM Halls", "Halls");

            foreach (DataRow row in d1.GetData().Rows)
            {
                int hallId = Convert.ToInt32(row["HallId"]);
                if (IsHallAvailable(hallId, newStart, newEnd))
                    return hallId;
            }

            return -1;
        }

        private int GetMovieDuration(int movieId)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL dAL = new DAL(con, "SELECT Duration FROM Movie WHERE Id=" + movieId, "Movie");
            return Convert.ToInt32(dAL.GetData().Rows[0]["Duration"]);
        }

        private int GetRoundedDuration(int movieId)
        {
            int duration = GetMovieDuration(movieId);
            int remainder = duration % 5;
            if (remainder != 0)
                duration += (5 - remainder);

            return duration + 20 + 15;
        }

        private List<(DateTime Start, DateTime End)> GenerateSequentialSchedule(int totalMinutes)
        {
            var schedule = new List<(DateTime, DateTime)>();
            DateTime start = DateTime.Today.AddHours(9);
            DateTime dayEnd = DateTime.Today.AddDays(1);

            while (start.AddMinutes(totalMinutes) <= dayEnd)
            {
                DateTime end = start.AddMinutes(totalMinutes);
                schedule.Add((start, end));
                start = end;
            }

            return schedule;
        }

        private bool AnyHallAvailable(DateTime newStart, DateTime newEnd)
        {
            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, "SELECT HallId FROM Halls", "Halls");

            foreach (DataRow row in d1.GetData().Rows)
            {
                if (IsHallAvailable(Convert.ToInt32(row["HallId"]), newStart, newEnd))
                    return true;
            }

            return false;
        }

        private bool IsMovieAlreadyScheduled(int movieId, DateTime newStart, DateTime newEnd)
        {
            string query = @"
                SELECT COUNT(*)
                FROM Screening
                WHERE MovieId = @MovieId
                  AND (@NewStart < EndTime AND @NewEnd > StartTime)";

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, query, "Screening");
            d1.Params.Add(new SqlParameter("@MovieId", movieId));
            d1.Params.Add(new SqlParameter("@NewStart", newStart));
            d1.Params.Add(new SqlParameter("@NewEnd", newEnd));

            return Convert.ToInt32(d1.ExecuteScalarDalPar()) > 0;
        }

        private bool IsHallAvailable(int hallId, DateTime newStart, DateTime newEnd)
        {
            string query = @"
                SELECT COUNT(*)
                FROM Screening
                WHERE Hall = @Hall
                  AND (@NewStart < EndTime AND @NewEnd > StartTime)";

            string con = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DAL d1 = new DAL(con, query, "Screening");
            d1.Params.Add(new SqlParameter("@Hall", hallId));
            d1.Params.Add(new SqlParameter("@NewStart", newStart));
            d1.Params.Add(new SqlParameter("@NewEnd", newEnd));

            return Convert.ToInt32(d1.ExecuteScalarDalPar()) == 0;
        }

        private sealed class ExistingScreening
        {
            public int ScreeningId { get; set; }
            public int Hall { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }
    }
}
