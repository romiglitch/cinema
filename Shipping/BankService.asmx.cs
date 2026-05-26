using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Services;

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class BankService : System.Web.Services.WebService
{
    [WebMethod]
    public bool ProcessPayment(string cardNumber, string cvv, string Expiry, string holderName)
    {
        //בדיקה שהנתונים לא ריקים
        if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(cvv) ||
            string.IsNullOrWhiteSpace(Expiry) || string.IsNullOrWhiteSpace(holderName))
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

        string expiryForDb = ConvertExpiryToMmYyyy(Expiry);
        if (expiryForDb == null)
            return false;

        return CardExistsInDatabase(cardNumber.Trim(), cvv.Trim(), expiryForDb, holderName.Trim());
    }

    // MM/YY מהטופס → MM/YYYY כפי שנשמר בטבלה
    private static string ConvertExpiryToMmYyyy(string expiry)
    {
        string[] parts = expiry.Split('/');
        if (parts.Length != 2)
            return null;

        return parts[0] + "/20" + parts[1];
    }

    // בדיקה מול טבלת CreditCards: מספר כרטיס, CVC, תוקף ושם בעל הכרטיס
    private bool CardExistsInDatabase(string cardNumber, string cvc, string expiryDate, string holderName)
    {
        string connStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

        string query = @"SELECT COUNT(*) FROM CreditCards
                         WHERE CardNumber = @cardNumber
                           AND CVC = @cvc
                           AND ExpirationDate = @expiry
                           AND UPPER(LTRIM(RTRIM(HolderName))) = UPPER(LTRIM(RTRIM(@holderName)))";

        using (SqlConnection conn = new SqlConnection(connStr))
        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
            cmd.Parameters.AddWithValue("@cvc", cvc);
            cmd.Parameters.AddWithValue("@expiry", expiryDate);
            cmd.Parameters.AddWithValue("@holderName", holderName);

            conn.Open();
            int count = (int)cmd.ExecuteScalar();
            return count > 0;
        }
    }
}
