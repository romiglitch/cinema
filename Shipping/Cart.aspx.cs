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
using System.Diagnostics;

namespace Shipping
{
    public partial class Cart : Page
    {
        public class TicketDetail//מחלקה שמשמשת לשמירת פרטי הכרטיסים והצגתם
        {
            public int HallId { get; set; }
            public int Row { get; set; }
            public int Seat { get; set; }
            public string Type { get; set; }
            public decimal Price { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserId"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            if (!IsPostBack)//ביצוע הקוד רק אם זאת טעינה ראשונה של הדף
            {
               //שמירת נתונים מהסשן
                string selectedSeatsData = Session["SelectedSeats"] as string;
                object sIdObj = Session["ScreeningId"];
                int screeningId = (sIdObj != null) ? Convert.ToInt32(sIdObj) : 0;
                int hallId = Convert.ToInt32(Session["HallId"] ?? 0);

                if (string.IsNullOrEmpty(selectedSeatsData))
                {
                    Response.Write("<div style='color:red;text-align:center;padding:20px;'>לא נמצאו מושבים בהזמנה.</div>");
                    return;
                }

                //הצגת הנתונים (פרטי הסרט) בסל
                if (screeningId > 0)
                {
                    try
                    {
                       
                        string connStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                        using (SqlConnection con = new SqlConnection(connStr))
                        {
                            
                            string sql = @"SELECT M.Title, S.StartTime 
               FROM [dbo].[Screening] S 
               JOIN [dbo].[Movie] M ON S.MovieId = M.Id 
               WHERE S.ScreeningId = @id";

                            using (SqlCommand cmd = new SqlCommand(sql, con))
                            {
                               
                                cmd.Parameters.AddWithValue("@id", screeningId);
                                con.Open();
                                using (SqlDataReader rdr = cmd.ExecuteReader())//שמירת התוצאות באובייקט קורא
                                                                               //שמאפשר לעבור עליהן שורה אחר שורה - פרטי ההקרנה
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
                        // הצגת שגיאה מלאה בצד שרת
                        Debug.WriteLine("SQL Error: " + ex.Message);

                        // הצגת הודעת שגיאה ללקוח
                        litMovieName.Text = "חלה שגיאה בטעינת פרטי ההקרנה. אנא נסה שוב מאוחר יותר.";
                        litScreeningTime.Text = "";
                    }
                }

                //הצגת פרטי המושבים
                ProcessTicketsDisplay(selectedSeatsData, hallId);
            }
        }
        private void ProcessTicketsDisplay(string selectedSeatsData, int hallId)
        {
            string ticketTypesStr = Session["TicketTypes"] as string ?? "";//שמירת סוג הכרטסים מהסשן
            string ticketPricesStr = Session["TicketPrices"] as string ?? "";//שמירת מחיר הכרטיסים מהסשן
            var ticketTypes = ticketTypesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//שמירת המחרוזת מהסשן כרשימה שניתן לעבוד עליה
            var ticketPrices = ticketPricesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//שמירת המחרוזת מהסשן כרשימה שניתן לעבוד עליה

            var tickets = new List<TicketDetail>();
            string[] seats = selectedSeatsData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//שמירת המחרוזת מהסשן כרשימה שניתן לעבוד עליה

            for (int i = 0; i < seats.Length; i++)
            {
                var parts = seats[i].Split('|');
                if (parts.Length == 3)//בדיקה שהכרטיס מכיל את כל הפרטים שלו (אולם,מושב,שורה)
                {
                    tickets.Add(new TicketDetail//בניית אובייקט עם פרטי הכרטיס
                    {
                        HallId = hallId,
                        Row = Convert.ToInt32(parts[1]),
                        Seat = Convert.ToInt32(parts[2]),
                        Type = (i < ticketTypes.Length) ? ticketTypes[i] : "רגיל",
                        Price = (i < ticketPrices.Length && decimal.TryParse(ticketPrices[i], out decimal p)) ? p : 50.00m
                    });
                }
            }

            rptTickets.DataSource = tickets;//שיוך הנתונים לרפיטר שמציג את הכרטיסים
            rptTickets.DataBind();//מציגה את המידע

            litTotalPrice.Text = tickets.Sum(t => t.Price).ToString("N2");//והצגת המספר עם 2 ספרות אחרי הנקודה LINQ חישוב המחיר הכולל באמצעות פקודת
            litTotalTickets.Text = tickets.Count.ToString();
        }

        protected void BtnPay_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;//בדיקה שכל הולידטורים תקינים

            if (Session["UserId"] == null)
            {
                lblMsg.Text = "עלייך להתחבר למערכת כדי לבצע רכישה.";
                return;
            }

            // שמירת נתונים מהסשן
            string userEmail = Session["UserEmail"]?.ToString() ?? "";
            // שם מלא לברכה במייל; כתובת המייל נלקחת בנפרד מ-UserEmail
            string fullName = Session["displayName"]?.ToString() ?? "";
            string movieName = litMovieName.Text;
            string rawSeats = Session["SelectedSeats"]?.ToString() ?? "";
            string ticketTypesStr = Session["TicketTypes"] as string ?? "";

            // עיבוד המושבים עוד פעם
            var seatEntries = rawSeats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//שמירת המחרוזת מהסשן כרשימה שניתן לעבוד עליה
            List<string> seatNumbersOnly = new List<string>();

            foreach (var entry in seatEntries)
            {
                var parts = entry.Split('|');
                if (parts.Length == 3)//בדיקה שהכרטיס מכיל את כל הפרטים שלו (אולם,מושב,שורה)
                {
                    //הצגה בפורמט תקין (כשיש את כל המידע)
                    seatNumbersOnly.Add($"שורה {parts[1]} מושב {parts[2]}");
                }
                else
                {
                    //במידה ואין את כל המידע או המידע מוצג בפורמט שונה הצגה של מה שיש
                    seatNumbersOnly.Add(entry);
                }
            }

            // בדיקה סופית לפני השליחה למייל: אם יש מושבים מעובדים, הם יוצגו כמחרוזת
            string formattedSeatsForEmail = seatNumbersOnly.Count > 0
                ? string.Join(", ", seatNumbersOnly)
                : (string.IsNullOrEmpty(rawSeats) ? "לא נבחרו מושבים" : rawSeats);//אם אין מושבים, במידה והמידע בסשן ריק תוצג הודעה
        
            decimal amount = 0;// משתנה דצימלי המאפשר לשמור על דיוק בחישובים כספיים
            decimal.TryParse(litTotalPrice.Text, out amount);// המרת הטקסט של המחיר הכולל למספר דצימלי - אם ההמרה נכשלת הסכום יישאר 0

            // פרויקט  עם מסד נתונים נפרד לכרטיסי חיוב - Payment חיבור לשירות התשלום
            // בניית נתיב מלא לקובץ מסד הנתונים של התשלומים - ממוקם בתיקיית Payment ברמת הפתרון
            string webRoot = Server.MapPath("~/").TrimEnd('\\', '/');
            string solutionRoot = System.IO.Path.GetDirectoryName(webRoot);
            string paymentDbFullPath = System.IO.Path.Combine(solutionRoot, "Payment", "PaymentDb.mdf");
            string paymentConnStr = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={paymentDbFullPath};Integrated Security=True";
            var paymentService = new Payment.PaymentService(paymentConnStr);//ייצר משתנה של השירות התשלום
            // ביצוע תשלום: בדיקת פרטי הכרטיס, בדיקת יתרה וניכוי הסכום מהכרטיס
            Payment.PaymentResult paymentResult;
            try
            {
                paymentResult = paymentService.ProcessPayment(
                    txtCardNum.Text, txtCVV.Text, txtExpiry.Text, txtHolderName.Text, amount);
            }
            catch (Exception ex)
            {
                // שגיאת DB בלתי צפויה (נפילת חיבור, timeout וכו') - מציגים הודעה ידידותית במקום קריסה
                lblMsg.Text = "אירעה שגיאה טכנית בעת עיבוד התשלום. אנא נסה שוב מאוחר יותר.";
                Debug.WriteLine("Payment DB Error: " + ex.ToString());
                return;
            }

            if (paymentResult.Success)// אם התשלום הצליח למשתנה paymentResult יהיה Success=true
            {
                try
                {
                    SaveOrderToDatabase();//שמירת ההזמנה במסד נתונים

                    RegisterAsyncTask(new PageAsyncTask(async () => //יצירת פעולה אסינכרונית ששולחת מייל לאישור ההזמנה
                    {
                        try
                        {
                            EmailService mailService = new EmailService();
                            DateTime screeningDate;
                            if (!DateTime.TryParse(litScreeningTime.Text, out screeningDate))//אם ההמרה של התאריך נכשלה
                                screeningDate = DateTime.Now;// נשתמש בתאריך הנוכחי כברירת מחדל כדי שהמייל עדיין יישלח עם תאריך אפשרי

                            await mailService.SendOrderReceiptEmail( //מאפשר לטפל בפעולה זו בזמן שהשרת ממשיך לטפל בדברים אחרים בזמן שהמייל נשלח
                                userEmail,
                                movieName,
                                screeningDate,
                                formattedSeatsForEmail,
                                amount,
                                fullName,
                                ticketTypesStr
                            );

                            //נותן לסיים את ריצת השרת ואז להעביר את המשתמש כדי שהסשן יישמר ולא יווצרו שגיאות endResponse=false
                            Response.Redirect("Success.aspx", false);
                        }
                        catch (Exception ex)
                        {
                            lblMsg.Text = "ההזמנה בוצעה בהצלחה, אך חלה שגיאה בשליחת המייל לאישור.";
                            Debug.WriteLine("Email Error: " + ex.ToString());
                        }
                    }));
                }
                catch (Exception ex)
                {
                    lblMsg.Text = "מצטערים, חלה שגיאה בתהליך שמירת ההזמנה. אנא נסו שוב מאוחר יותר.";
                    Debug.WriteLine("Database/General Error: " + ex.ToString());
                }
            }
            else
            {
                // PaymentResult.csאם התשלום נכשל - הצגת הודעת השגיאה כפי שהוגדרה ב 
                lblMsg.Text = paymentResult.Message;
            }
        }

        private void SaveOrderToDatabase()//שמירת ההזמנה במסד נתונים
        {
            string connStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            int screeningId = Convert.ToInt32(Session["ScreeningId"] ?? 0);//שמירת האיידי של ההקרנה
            int userId = Convert.ToInt32(Session["UserId"] ?? 0);//שמירת האיידי של המשתמש

            string selectedSeatsData = Session["SelectedSeats"] as string ?? "";//שמירת המקומות כמחרוזת
            string ticketTypesStr = Session["TicketTypes"] as string ?? "";//שמירת סוג הכרטיסים כמחרוזת
            string ticketPricesStr = Session["TicketPrices"] as string ?? "";//שמירת מחיר הכרטיסים כמחרוזת

            string[] seatsArray = selectedSeatsData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//שמירת המחרוזת האורכה
                                                                                                                //(מושב|שורה|אולם) כמערך בו כל איבר מציג כיסא
            string[] typesArray = ticketTypesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] pricesArray = ticketPricesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (seatsArray.Length == 0 || screeningId == 0) return;//בדיקת תקינות

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())//פתיחת טרנזקציה - כל הפעולות לא ישמרו באופן סופי עד שיש אישור
                                                                           //(בשביל להבטיח שלא יהיה מצב בו יש הזמנה ללא כרטיסים (חוסר עקביות בנתונים
                {
                    try
                    {
                       
                        string sqlTickets = @"INSERT INTO [dbo].[Tickets] ([User], [Screening], [Row], [Seat], [Price], [Type])  
                                     VALUES (@user, @screening, @row, @seat, @price, @type)";

                        // שמירת כל כרטיס בטבלת כרטיסים
                        for (int i = 0; i < seatsArray.Length; i++)
                        {
                            var parts = seatsArray[i].Split('|');
                            if (parts.Length == 3)
                            {
                                using (SqlCommand cmd = new SqlCommand(sqlTickets, con, transaction))//יצירת קומנד
                                {
                                    string currentType = (i < typesArray.Length) ? typesArray[i].Trim() : "רגיל";
                                    decimal currentPrice = (i < pricesArray.Length && decimal.TryParse(pricesArray[i].Trim(), out decimal p)) ? p : 50.00m;

                                    cmd.Parameters.AddWithValue("@user", userId);
                                    cmd.Parameters.AddWithValue("@screening", screeningId);
                                    cmd.Parameters.AddWithValue("@row", Convert.ToInt32(parts[1]));
                                    cmd.Parameters.AddWithValue("@seat", Convert.ToInt32(parts[2]));
                                    cmd.Parameters.AddWithValue("@price", currentPrice);
                                    cmd.Parameters.AddWithValue("@type", currentType);

                                    cmd.ExecuteNonQuery();//(ביצוע השאילתא (נון קוורי מפני שלא צריך לקבל נתונים בחזרה אלא רק לעדכן
                                }
                            }
                        }

                        //עדכון הכרטיסים הבקרנה
                        string sqlUpdateScreening = @"UPDATE [dbo].[Screening] 
                                            SET SeatesBought = SeatesBought + @count 
                                            WHERE ScreeningId = @sId";

                        using (SqlCommand updateCmd = new SqlCommand(sqlUpdateScreening, con, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@count", seatsArray.Length); // כמות המושבים שנקנו עכשיו
                            updateCmd.Parameters.AddWithValue("@sId", screeningId);
                            updateCmd.ExecuteNonQuery();
                        }
                       

                        transaction.Commit();//אישור טרנזקציה - השינויים נשמרים במסד נתונים
                        Session["SelectedSeats"] = null;
                        Session["TotalTickets"] = null;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();//(ביטול כל מה שנעשה מתחילת הטרנזקציה (הגנה על נתונים

                        System.Diagnostics.Trace.WriteLine("Saving Tickets Error: " + ex.ToString());

                        throw new Exception("חלה שגיאה בעיבוד ההזמנה. אנא נסו שוב מאוחר יותר.");//שלה catchוהעברה ל BtnPay_Click עצירה של
                    }
                }
            }
        }
    }
}
