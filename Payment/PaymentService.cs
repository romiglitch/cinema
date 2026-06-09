using System;
using System.Data.SqlClient;

namespace Payment
{
    // שירות תשלום - אחראי על אימות פרטי כרטיס חיוב וניכוי סכום מהיתרה
    public class PaymentService
    {
        private readonly string _connectionString;// מחרוזת התחברות למסד הנתונים של התשלומים

        // פעולה בונה - מקבלת את מחרוזת ההתחברות למסד הנתונים של כרטיסי החיוב
        public PaymentService(string connectionString)
        {
            _connectionString = connectionString;
        }

        //פעולה ראשית - מאמתת את פרטי הכרטיס 
        // מחזירה PaymentResult עם הצלחה או כישלון והודעה מתאימה
        public PaymentResult ProcessPayment(string cardNumber, string cvv, string expiry, string holderName, decimal amount)
        {
            // בדיקה שכל השדות מלאים
            if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(cvv) ||
                string.IsNullOrWhiteSpace(expiry) || string.IsNullOrWhiteSpace(holderName))
                return PaymentResult.Fail("יש למלא את כל פרטי הכרטיס.");

            // בדיקה שמספר הכרטיס וה-CVV לא מתחילים בערכים לא תקינים
            if (cardNumber.StartsWith("0000") || cvv.StartsWith("000"))
                return PaymentResult.Fail("פרטי כרטיס לא תקינים.");

            // בדיקת אורך בסיסית של מספר כרטיס ו-CVV
            if (cardNumber.Length < 8 || cvv.Length < 3)
                return PaymentResult.Fail("פרטי כרטיס לא תקינים.");

            // בדיקה שהסכום לתשלום חיובי
            if (amount <= 0)
                return PaymentResult.Fail("סכום לתשלום אינו תקין.");

            // בדיקת תוקף הכרטיס - המרת MM/YY לתאריך ובדיקה שלא פג
            try
            {
                string[] dateParts = expiry.Split('/');// פיצול התוקף לחודש ושנה
                if (dateParts.Length != 2)
                    return PaymentResult.Fail("פורמט תוקף לא תקין (MM/YY).");

                int month = int.Parse(dateParts[0]);
                int year = int.Parse("20" + dateParts[1]);// הוספת "20" כדי להפוך "26" ל-2026
                DateTime expiryDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);// יצירת תאריך היום האחרון בחודש התוקף

                if (expiryDate < DateTime.Now)// אם תאריך התפוגה עבר
                    return PaymentResult.Fail("תוקף הכרטיס פג.");
            }
            catch
            {
                return PaymentResult.Fail("תאריך תוקף לא תקין.");
            }

            // המרת פורמט התוקף מ-MM/YY ל-MM/YYYY כפי שנשמר בטבלה
            string expiryForDb = dateParts0_To_MmYyyy(expiry);
            if (expiryForDb == null)
                return PaymentResult.Fail("פורמט תוקף לא תקין.");

            // כל הבדיקות עברו - ניסיון לחייב את הכרטיס
            return DeductFromCard(cardNumber.Trim(), cvv.Trim(), expiryForDb, holderName.Trim(), amount);
        }

        // המרת פורמט תוקף: MM/YY מהטופס → MM/YYYY כפי שנשמר בטבלה
        private static string dateParts0_To_MmYyyy(string expiry)
        {
            string[] parts = expiry.Split('/');
            if (parts.Length != 2) return null;
            return parts[0] + "/20" + parts[1];//הוספת "20" כדי להפוך "26" ל-2026
        }

        // חיפוש הכרטיס בטבלת DebitCards, בדיקת יתרה וניכוי הסכום - הכל בתוך טרנזקציה
        private PaymentResult DeductFromCard(string cardNumber, string cvc, string expiryDate, string holderName, decimal amount)
        {
            // נרמול תוקף: MM/YY → MM/20YY כדי להתאים לפורמט שמור במסד הנתונים
            if (expiryDate != null && System.Text.RegularExpressions.Regex.IsMatch(expiryDate.Trim(), @"^\d{2}/\d{2}$"))
                expiryDate = expiryDate.Trim().Substring(0, 3) + "20" + expiryDate.Trim().Substring(3);

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();// פתיחת חיבור למסד הנתונים
                using (SqlTransaction tx = conn.BeginTransaction())// פתיחת טרנזקציה - הכל מצליח או הכל מתבטל
                //מניעת אי עקביות בנתונים 
                {
                    try
                    {
                        // שאילתה לחיפוש הכרטיס עם נעילת שורה למניעת חיוב כפול במקביל
                        string selectQuery = @"
                            SELECT Id, Balance FROM DebitCards WITH (UPDLOCK, ROWLOCK)
                            WHERE CardNumber = @cardNumber
                              AND CVC = @cvc
                              AND ExpirationDate = @expiry
                              AND UPPER(LTRIM(RTRIM(HolderName))) = UPPER(LTRIM(RTRIM(@holderName)))";

                        int cardId = 0;
                        decimal balance = 0;// משתנה דצימלי המאפשר לשמור על דיוק בחישובים כספיים
                        bool found = false;

                        // הרצת השאילתה עם פרמטרים למניעת SQL Injection
                        using (SqlCommand cmd = new SqlCommand(selectQuery, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                            cmd.Parameters.AddWithValue("@cvc", cvc);
                            cmd.Parameters.AddWithValue("@expiry", expiryDate);
                            cmd.Parameters.AddWithValue("@holderName", holderName);

                            using (SqlDataReader reader = cmd.ExecuteReader())// קריאת התוצאות
                            {
                                if (reader.Read())// אם נמצא כרטיס תואם
                                {
                                    found = true;
                                    cardId = reader.GetInt32(0);// שמירת מזהה הכרטיס
                                    balance = reader.GetDecimal(1);// שמירת היתרה הנוכחית
                                }
                            }
                        }

                        // אם הכרטיס לא נמצא - פרטים שגויים
                        if (!found)
                            return PaymentResult.Fail("פרטי הכרטיס שגויים או שהכרטיס לא נמצא במערכת.");

                        // בדיקה שיש מספיק יתרה בכרטיס לביצוע התשלום
                        if (balance < amount)
                            return PaymentResult.Fail(
                                $"אין מספיק יתרה בכרטיס. היתרה הנוכחית: ₪{balance:N2}, סכום לתשלום: ₪{amount:N2}.");

                        // ניכוי הסכום מיתרת הכרטיס
                        string updateQuery = "UPDATE DebitCards SET Balance = Balance - @amount WHERE Id = @id";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, tx))
                        {
                            updateCmd.Parameters.AddWithValue("@amount", amount);
                            updateCmd.Parameters.AddWithValue("@id", cardId);
                            updateCmd.ExecuteNonQuery();// ביצוע העדכון במסד הנתונים
                        }

                        tx.Commit();// אישור הטרנזקציה - השינויים נשמרים סופית
                        return PaymentResult.Ok();
                    }
                    catch
                    {
                        tx.Rollback();// ביטול כל מה שנעשה מתחילת הטרנזקציה - הגנה על הנתונים
                        // throw; זורק מחדש את אותה שגיאה שנתפסה מבלי לאפס את המחסנית
                        // בניגוד ל-throw ex שמוחק את מיקום השגיאה המקורי
                        // כך הקורא יודע שהתרחשה שגיאה, ואנחנו כבר דאגנו ל-Rollback
                        throw;
                    }
                }
            }
        }
    }
}
