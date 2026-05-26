using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;
using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
namespace Shipping
{
    // שירות שליחת מיילים - אחראי על שליחת מייל שחזור סיסמה ואישור הזמנה למשתמשים
    public class EmailService
    {
        private readonly string senderEmail = "kladnitsky.romi@gmail.com";//רידאונלי בשביל שישאר מוגן ולא ישתנה בזמן ההרצה בטעות
        private readonly string appPassword ="slprfmpvlzczmnit";//שמאשרת לשלוח מיילים בשמי בלי לאשר ידנית כל פעם SMTP סיסמא יחודית בשביל

        //הגדרת הפעולה כמשימה אסינכרונית : הפעולה עומדת לקחת זמן אז בזמן הזה השרת יטפל בדברים אחרים
        //המטרה היא למנוע מהאתר לקופא בזמן שהוא מחכה שפעולה איטית תסתיים

        // שליחת מייל איפוס סיסמה - מכיל קישור ייחודי (טוקן) לאיפוס הסיסמה
        public async Task SendResetPasswordEmail(string recipientEmail, string resetLink)
        {
            var message = new MimeMessage();//(MailKit בעזרת ספריית) MimeMessage יצירת אובייקט מסוג
            message.From.Add(new MailboxAddress("אתר הקולנוע של רומי 🎬", senderEmail));//הגדרת שם השולח שיוצג והכתובת ממנה המייל ישלח
            message.To.Add(new MailboxAddress("", recipientEmail));//כתובת מייל הלקוח
            message.Subject = "שחזור סיסמה";

            var bodyBuilder = new BodyBuilder//בניית גוף ההודעה לפי העיצובי שנכתב
            {
                HtmlBody = $@"
        <div dir='rtl' style='font-family: ""Montserrat"", ""Rubik"", ""Segoe UI"", Tahoma, sans-serif; max-width: 500px; margin: 20px auto; border: 1px solid #eee; border-radius: 15px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); text-align: center;'>
            <div style='background: linear-gradient(to right, #8e2de2, #4a00e0); padding: 25px;'>
                <h2 style='color: white; margin: 0; font-size: 22px;'>שכחת את הסיסמא?</h2>
            </div>

            <div style='padding: 30px; background-color: #ffffff; color: #333;'>
                <p style='font-size: 16px;'>אל דאגה, זה קורה לטובים ביותר. קיבלנו בקשה לאיפוס הסיסמה בחשבון שלך.</p>
                <p style='font-size: 16px;'>לחצי על הכפתור למטה כדי לבחור סיסמה חדשה ולהמשיך להזמין סרטים:</p>
                
                <div style='margin: 30px 0;'>
                    <a href='{resetLink}' style='background: #8e2de2; color: white; padding: 15px 35px; text-decoration: none; border-radius: 50px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 15px rgba(142, 45, 226, 0.3); display: inline-block;'>איפוס סיסמה כעת</a>
                </div>

                <p style='font-size: 14px; color: #999;'>אם לא ביקשת לאפס את הסיסמה, אפשר פשוט להתעלם מהמייל הזה.</p>
                
                <div style='height: 2px; background: #f0f0f0; margin: 25px 0;'></div>
                
                <p style='font-size: 12px; color: #7f8c8d;'>הקישור יהיה בתוקף ל-15 דקות הקרובות בלבד.</p>
            </div>
        </div>"
            };
            message.Body = bodyBuilder.ToMessageBody();

            await ExecuteSendAsync(message);//השרת ממשיך לטפל בדברים אחרים בזמן שהמייל נשלח
        }
        
        public async Task SendOrderReceiptEmail(string toEmail, string movieName, DateTime screeningTime, string seats, decimal totalPrice, string fullName)
        {
            // תיקון תצוגת המושבים - מחליף את ה- | בפסיק ורווח לקריאות נוחה
            string formattedSeats = seats.Replace("|", ", ");

            // עיצוב התאריך והשעה
            string displayDate = screeningTime.ToString("dd/MM/yyyy");
            string displayTime = screeningTime.ToString("HH:mm");

            var message = new MimeMessage();//(MailKit בעזרת ספריית) MimeMessage יצירת אובייקט מסוג
            message.From.Add(new MailboxAddress("אתר הקולנוע של רומי 🎬", senderEmail));//הגדרת שם השולח שיוצג והכתובת ממנה המייל ישלח
            message.To.Add(new MailboxAddress("", toEmail));//כתובת מייל הלקוח
            message.Subject = "אישור הזמנה 🎟️";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
 <div dir='rtl' style='font-family: ""Montserrat"", ""Rubik"", ""Segoe UI"", Tahoma, sans-serif; max-width: 600px; margin: 20px auto; border: 1px solid #eee; border-radius: 15px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
     <div style='background-color: #8e2de2; background: linear-gradient(to right, #8e2de2, #4a00e0); padding: 20px; text-align: center;'>
         <h1 style='color: white; margin: 0; font-size: 24px;'>הזמנתך הושלמה</h1>
     </div>

     <div style='padding: 30px; background-color: #ffffff; color: #333;'>
         <p style='font-size: 18px;'>היי, {fullName}</p>
         <p style='font-size: 16px;'>תודה על הזמנתך מאתר הקולנוע של רומי! הכרטיסים שלך לסרט מחכים לך. הנה פרטי ההזמנה:</p>
         
         <div style='background-color: #f8f9fa; border-right: 5px solid #8e2de2; padding: 20px; margin: 20px 0; border-radius: 5px;'>
             <p style='margin: 5px 0;'><strong>🎬 סרט:</strong> <span style='color: #8e2de2; font-size: 18px;'>{movieName}</span></p>
             <p style='margin: 5px 0;'><strong>📅 תאריך:</strong> {displayDate}</p>
             <p style='margin: 5px 0;'><strong>🕒 שעה:</strong> {displayTime}</p>
             <p style='margin: 5px 0;'><strong>💺 מושבים:</strong> {formattedSeats}</p>
             <p style='margin: 5px 0;'><strong>💰 סה""כ לתשלום:</strong> ₪{totalPrice:N2}</p>
         </div>

         <p style='font-size: 15px; color: #666; text-align: center;'>הכרטיסים יחכו לך בקופות הקולנוע עם הצגת המייל הזה או מספר הטלפון שלך.</p>
         
         <div style='height: 4px; background: linear-gradient(to right, #8e2de2, #4a00e0); margin: 30px 0; border-radius: 2px;'></div>

         <p style='text-align: center; font-weight: bold; color: #8e2de2; font-size: 18px;'>נתראה בסרט! 🍿</p>
     </div>

     <div style='background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #999;'>
         © {DateTime.Now.Year} אתר הקולנוע של רומי | רחוב הקולנוע 1, סינמה סיטי
     </div>
 </div>"
            };
            message.Body = bodyBuilder.ToMessageBody();

            await ExecuteSendAsync(message);
        }

        // פונקציית השליחה המרכזית (SMTP)
        private async Task ExecuteSendAsync(MimeMessage message)
        {
            using (var client = new SmtpClient())
            {
                try
                {
                    //יצירת קשר עם השרת של גוגל
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(senderEmail, appPassword); // אימות מול Gmail
                    await client.SendAsync(message); // שליחת ההודעה
                    await client.DisconnectAsync(true); // ניתוק מסודר מהשרת
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("שגיאת שרת בשליחת מייל");
                    Debug.WriteLine(ex.ToString());

                    throw new Exception("חלה שגיאה טכנית בשליחת המייל. אנא נסה שוב מאוחר יותר.");
                }
            }
             }  

        }
}