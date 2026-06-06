namespace Payment
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static PaymentResult Ok()
        {
            return new PaymentResult { Success = true, Message = "התשלום בוצע בהצלחה." };
        }

        public static PaymentResult Fail(string message)
        {
            return new PaymentResult { Success = false, Message = message };
        }
    }
}
