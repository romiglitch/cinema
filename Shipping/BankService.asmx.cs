using System;
using System.Web.Services;

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class BankService : System.Web.Services.WebService
{
    [WebMethod]
    public bool ProcessPayment(string cardNumber, string expiry, string cvv, decimal amount)
    {
        // סימולציה: אם מספר הכרטיס מתחיל ב-"000", התשלום נכשל. כל שאר המקרים מצליחים.
        if (cardNumber.StartsWith("000"))
            return false;

        // כאן במציאות תהיה בדיקה מול בסיס נתונים של בנק
        return true;
    }
}
