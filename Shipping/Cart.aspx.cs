using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using MailKit.Net.Smtp;
using MimeKit;
using System.Web;
using System.Threading.Tasks;

namespace Shipping
{
    public partial class Cart : Page
    {
        public class TicketDetail
        {
            public int HallId { get; set; }
            public int Row { get; set; }
            public int Seat { get; set; }
            public string Type { get; set; }
            public decimal Price { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // 1. שליפת נתונים מה-Session
                string selectedSeatsData = Session["SelectedSeats"] as string;
                object sIdObj = Session["ScreeningId"];
                int screeningId = (sIdObj != null) ? Convert.ToInt32(sIdObj) : 0;
                int hallId = Convert.ToInt32(Session["HallId"] ?? 0);

                if (string.IsNullOrEmpty(selectedSeatsData))
                {
                    Response.Write("<div style='color:red;text-align:center;padding:20px;'>לא נמצאו מושבים בהזמנה.</div>");
                    return;
                }

                // 2. שליפת פרטי סרט - עם טיפול בשגיאת שם הטבלה
                if (screeningId > 0)
                {
                    try
                    {
                        // --- חלק 1 המעודכן: שליפת פרטי סרט ושעה ---
                        string connStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                        using (SqlConnection con = new SqlConnection(connStr))
                        {
                            // שימוש בשמות הטבלאות והעמודות המדויקים שלך: Movie, Title, Screening
                            string sql = @"SELECT M.Title, S.StartTime 
               FROM [dbo].[Screening] S 
               JOIN [dbo].[Movie] M ON S.MovieId = M.Id 
               WHERE S.ScreeningId = @id";

                            using (SqlCommand cmd = new SqlCommand(sql, con))
                            {
                                // בדיקה חשובה: ודאי ש-screeningId הוא אכן ה-ID של ההקרנה ולא של הסרט
                                cmd.Parameters.AddWithValue("@id", screeningId);
                                con.Open();
                                using (SqlDataReader rdr = cmd.ExecuteReader())
                                {
                                    if (rdr.Read())
                                    {
                                        litMovieName.Text = rdr["Title"].ToString();
                                        litScreeningTime.Text = rdr["StartTime"].ToString();
                                    }
                                    else
                                    {
                                        litMovieName.Text = "לא נמצאו נתונים להקרנה מספר: " + screeningId;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // אם עדיין יש שגיאה, נציג אותה בצורה ברורה על המסך
                        litMovieName.Text = "שגיאת SQL: " + ex.Message; //יש להתחבר עם כתובת דואל למערכת לפני ביצוע רכישה
                    }
                }

                // 3. קריאה לפונקציית העיבוד שהצמדתי למטה
                ProcessTicketsDisplay(selectedSeatsData, hallId);
            }
        }

        // זו הפונקציה ששאלת עליה - היא מעבדת את רשימת הכרטיסים ומציגה אותם
        private void ProcessTicketsDisplay(string selectedSeatsData, int hallId)
        {
            string ticketTypesStr = Session["TicketTypes"] as string ?? "";
            string ticketPricesStr = Session["TicketPrices"] as string ?? "";
            var ticketTypes = ticketTypesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var ticketPrices = ticketPricesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var tickets = new List<TicketDetail>();
            string[] seats = selectedSeatsData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < seats.Length; i++)
            {
                var parts = seats[i].Split('|');
                if (parts.Length == 3)
                {
                    tickets.Add(new TicketDetail
                    {
                        HallId = hallId,
                        Row = Convert.ToInt32(parts[1]),
                        Seat = Convert.ToInt32(parts[2]),
                        Type = (i < ticketTypes.Length) ? ticketTypes[i] : "רגיל",
                        Price = (i < ticketPrices.Length && decimal.TryParse(ticketPrices[i], out decimal p)) ? p : 45.00m
                    });
                }
            }

            rptTickets.DataSource = tickets;
            rptTickets.DataBind();

            litTotalPrice.Text = tickets.Sum(t => t.Price).ToString("N2");
            litTotalTickets.Text = tickets.Count.ToString();
        }

        protected void BtnPay_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            if (Session["UserId"] == null)
            {
                lblMsg.Text = "עלייך להתחבר למערכת כדי לבצע רכישה.";
                return;
            }

            // --- שלב 1: שליפת הנתונים ---
            string userEmail = Session["UserEmail"]?.ToString() ?? "";
            string userName = Session["username"]?.ToString() ?? "";
            string movieName = litMovieName.Text;
            string rawSeats = Session["SelectedSeats"]?.ToString() ?? "";

            // --- שלב 2: עיבוד המושבים ---
            var seatEntries = rawSeats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> seatNumbersOnly = new List<string>();

            foreach (var entry in seatEntries)
            {
                var parts = entry.Split('|');
                if (parts.Length == 3)
                {
                    // פורמט תקין: ID|Row|Seat
                    seatNumbersOnly.Add($"שורה {parts[1]} מושב {parts[2]}");
                }
                else
                {
                    // אם אין |, פשוט נוסיף את מה שיש שם (למקרה שהפורמט שונה)
                    seatNumbersOnly.Add(entry);
                }
            }

            // אם הרשימה עדיין ריקה משום מה, ניקח את ה-rawSeats כמו שהוא
            string formattedSeatsForEmail = seatNumbersOnly.Count > 0
                ? string.Join(", ", seatNumbersOnly)
                : (string.IsNullOrEmpty(rawSeats) ? "לא נבחרו מושבים" : rawSeats);

            // --- המשך הקוד (בדיקת תשלום ושליחת מייל) ---

            // --- שלב 2: בדיקת תשלום ---
            BankService bank = new BankService();
            decimal amount = 0;
            decimal.TryParse(litTotalPrice.Text, out amount);

            if (bank.ProcessPayment(txtCardNum.Text, txtExpiry.Text, txtCVV.Text, amount))
            {
                try
                {
                    // --- שלב 3: שמירה לבסיס הנתונים (כאן ה-Session["SelectedSeats"] הופך ל-null) ---
                    SaveOrderToDatabase();

                    // --- שלב 4: שליחת המייל עם הנתונים ששמרנו מראש ---
                    RegisterAsyncTask(new PageAsyncTask(async () =>
                    {
                        try
                        {
                            EmailService mailService = new EmailService();
                            DateTime screeningDate;
                            if (!DateTime.TryParse(litScreeningTime.Text, out screeningDate))
                                screeningDate = DateTime.Now;

                            await mailService.SendOrderReceiptEmail(
                                userEmail,
                                movieName,
                                screeningDate,
                                formattedSeatsForEmail, // עכשיו זה בטוח לא יהיה ריק!
                                amount,
                                userName
                            );

                            Response.Redirect("Success.aspx");
                        }
                        catch (Exception ex)
                        {
                            lblMsg.Text = "הזמנה בוצעה אך המייל נכשל: " + ex.Message;
                        }
                    }));
                }
                catch (Exception ex)
                {
                    lblMsg.Text = "שגיאה בשמירת הנתונים: " + ex.Message;
                }
            }
            else
            {
                lblMsg.Text = "התשלום נדחה. נא לבדוק את פרטי האשראי.";
            }
        }

        private void SaveOrderToDatabase()
        {
            string connStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            int screeningId = Convert.ToInt32(Session["ScreeningId"] ?? 0);
            int userId = Convert.ToInt32(Session["UserId"] ?? 0);

            string selectedSeatsData = Session["SelectedSeats"] as string ?? "";
            string ticketTypesStr = Session["TicketTypes"] as string ?? "";
            string ticketPricesStr = Session["TicketPrices"] as string ?? "";

            string[] seatsArray = selectedSeatsData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] typesArray = ticketTypesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] pricesArray = ticketPricesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (seatsArray.Length == 0 || screeningId == 0) return;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // 1. שמירת כל כרטיס בנפרד בטבלת Tickets
                        string sqlTickets = @"INSERT INTO [dbo].[Tickets] ([User], [Screening], [Row], [Seat], [Price], [Type])  
                                     VALUES (@user, @screening, @row, @seat, @price, @type)";

                        for (int i = 0; i < seatsArray.Length; i++)
                        {
                            var parts = seatsArray[i].Split('|');
                            if (parts.Length == 3)
                            {
                                using (SqlCommand cmd = new SqlCommand(sqlTickets, con, transaction))
                                {
                                    string currentType = (i < typesArray.Length) ? typesArray[i].Trim() : "רגיל";
                                    decimal currentPrice = (i < pricesArray.Length && decimal.TryParse(pricesArray[i].Trim(), out decimal p)) ? p : 40.00m;

                                    cmd.Parameters.AddWithValue("@user", userId);
                                    cmd.Parameters.AddWithValue("@screening", screeningId);
                                    cmd.Parameters.AddWithValue("@row", Convert.ToInt32(parts[1]));
                                    cmd.Parameters.AddWithValue("@seat", Convert.ToInt32(parts[2]));
                                    cmd.Parameters.AddWithValue("@price", currentPrice);
                                    cmd.Parameters.AddWithValue("@type", currentType);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // --- כאן ההוספה החדשה שלך! ---
                        // 2. עדכון המונה בטבלת Screening כדי שפונקציית המחיקה לא תמחק את ההקרנה הזו
                        string sqlUpdateScreening = @"UPDATE [dbo].[Screening] 
                                            SET SeatesBought = SeatesBought + @count 
                                            WHERE ScreeningId = @sId";

                        using (SqlCommand updateCmd = new SqlCommand(sqlUpdateScreening, con, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@count", seatsArray.Length); // כמות המושבים שנקנו עכשיו
                            updateCmd.Parameters.AddWithValue("@sId", screeningId);
                            updateCmd.ExecuteNonQuery();
                        }
                        // ------------------------------

                        transaction.Commit();
                        Session["SelectedSeats"] = null;
                        Session["TotalTickets"] = null;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("שגיאה בשמירת הכרטיסים: " + ex.Message);
                    }
                }
            }
        }
    }
}
