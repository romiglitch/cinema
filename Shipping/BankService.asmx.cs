using System;
using System.Web.Services;

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class BankService : System.Web.Services.WebService
{
    [WebMethod]
    public bool ProcessPayment(string cardNumber, string cvv, string Expiry)
    {
        //בדיקה שהנתונים לא ריקים
        if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(cvv) || string.IsNullOrWhiteSpace(Expiry))
            return false;

        // בדיקה שהפרטים תקינים
        if (cardNumber.StartsWith("0000") || cvv.StartsWith("000"))
            return false;

        // בדיקת אורך בסיסית
        if (cardNumber.Length < 8 || cvv.Length < 3)
            return false;

        // בדיקת תוקף
        try
        {
            string[] dateParts = Expiry.Split('/');
            if (dateParts.Length != 2) return false; // פורמט חייב להיות MM/YY

            int month = int.Parse(dateParts[0]);
            // הוספת "20" לשנה כדי להפוך "26" ל-2026
            int year = int.Parse("20" + dateParts[1]);

            // יצירת תאריך של היום האחרון באותו חודש
            DateTime expiryDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            if (expiryDate < DateTime.Now)//אם תאריך התפוגה עבר
                return false;
        }
        catch
        {
            return false; //משהו לא תקין בתאריך
        }

        return true; // עבר את כל הבדיקות
    }
}
