using System;

namespace Shipping
{
    public static class EmailHelper
    {
        public static string Normalize(string email)
        {
            // Normalize user input so comparisons/uniqueness behave consistently (trim + lowercase).
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;

            return email.Trim().ToLowerInvariant();
        }
    }
}
