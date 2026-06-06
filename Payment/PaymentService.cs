using System;
using System.Data.SqlClient;

namespace Payment
{
    public class PaymentService
    {
        private readonly string _connectionString;

        public PaymentService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public PaymentResult ProcessPayment(string cardNumber, string cvv, string expiry, string holderName, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(cvv) ||
                string.IsNullOrWhiteSpace(expiry) || string.IsNullOrWhiteSpace(holderName))
                return PaymentResult.Fail("יש למלא את כל פרטי הכרטיס.");

            if (cardNumber.StartsWith("0000") || cvv.StartsWith("000"))
                return PaymentResult.Fail("פרטי כרטיס לא תקינים.");

            if (cardNumber.Length < 8 || cvv.Length < 3)
                return PaymentResult.Fail("פרטי כרטיס לא תקינים.");

            if (amount <= 0)
                return PaymentResult.Fail("סכום לתשלום אינו תקין.");

            try
            {
                string[] dateParts = expiry.Split('/');
                if (dateParts.Length != 2)
                    return PaymentResult.Fail("פורמט תוקף לא תקין (MM/YY).");

                int month = int.Parse(dateParts[0]);
                int year = int.Parse("20" + dateParts[1]);
                DateTime expiryDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

                if (expiryDate < DateTime.Now)
                    return PaymentResult.Fail("תוקף הכרטיס פג.");
            }
            catch
            {
                return PaymentResult.Fail("תאריך תוקף לא תקין.");
            }

            string expiryForDb = dateParts0_To_MmYyyy(expiry);
            if (expiryForDb == null)
                return PaymentResult.Fail("פורמט תוקף לא תקין.");

            return DeductFromCard(cardNumber.Trim(), cvv.Trim(), expiryForDb, holderName.Trim(), amount);
        }

        private static string dateParts0_To_MmYyyy(string expiry)
        {
            string[] parts = expiry.Split('/');
            if (parts.Length != 2) return null;
            return parts[0] + "/20" + parts[1];
        }

        private PaymentResult DeductFromCard(string cardNumber, string cvc, string expiryDate, string holderName, decimal amount)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        string selectQuery = @"
                            SELECT Id, Balance FROM DebitCards WITH (UPDLOCK, ROWLOCK)
                            WHERE CardNumber = @cardNumber
                              AND CVC = @cvc
                              AND ExpirationDate = @expiry
                              AND UPPER(LTRIM(RTRIM(HolderName))) = UPPER(LTRIM(RTRIM(@holderName)))";

                        int cardId = 0;
                        decimal balance = 0;
                        bool found = false;

                        using (SqlCommand cmd = new SqlCommand(selectQuery, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                            cmd.Parameters.AddWithValue("@cvc", cvc);
                            cmd.Parameters.AddWithValue("@expiry", expiryDate);
                            cmd.Parameters.AddWithValue("@holderName", holderName);

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    found = true;
                                    cardId = reader.GetInt32(0);
                                    balance = reader.GetDecimal(1);
                                }
                            }
                        }

                        if (!found)
                            return PaymentResult.Fail("פרטי הכרטיס שגויים או שהכרטיס לא נמצא במערכת.");

                        if (balance < amount)
                            return PaymentResult.Fail(
                                $"אין מספיק יתרה בכרטיס. היתרה הנוכחית: ₪{balance:N2}, סכום לתשלום: ₪{amount:N2}.");

                        string updateQuery = "UPDATE DebitCards SET Balance = Balance - @amount WHERE Id = @id";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, tx))
                        {
                            updateCmd.Parameters.AddWithValue("@amount", amount);
                            updateCmd.Parameters.AddWithValue("@id", cardId);
                            updateCmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return PaymentResult.Ok();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
