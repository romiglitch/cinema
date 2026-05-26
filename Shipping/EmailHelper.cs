using System;

namespace Shipping
{
    public static class EmailHelper
    {
        public static string Normalize(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;

            return email.Trim().ToLowerInvariant();
        }
    }
}
