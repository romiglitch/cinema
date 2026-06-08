namespace Payment
{
    // מחלקה המייצגת את תוצאת התשלום - מוחזרת מהשירות לאחר ניסיון חיוב
    public class PaymentResult
    {
        public bool Success { get; set; }// האם התשלום הצליח
        public string Message { get; set; }// הודעה למשתמש - הצלחה או סיבת הכישלון

        // יצירת תוצאה מוצלחת - נקראת כשהחיוב בוצע והיתרה עודכנה בהצלחה
        public static PaymentResult Ok()
        {
            return new PaymentResult { Success = true, Message = "התשלום בוצע בהצלחה." };
        }

        // יצירת תוצאה כושלת עם הודעת שגיאה מותאמת (פרטים שגויים, יתרה לא מספיקה וכו')
        public static PaymentResult Fail(string message)
        {
            return new PaymentResult { Success = false, Message = message };
        }
    }
}
