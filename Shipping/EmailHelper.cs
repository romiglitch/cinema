using System;

namespace Shipping
{
    // עזר לנרמול כתובות אימייל – משמש בהתחברות, הרשמה ושחזור סיסמה.
    public static class EmailHelper
    {
        public static string Normalize(string email)
        {
            // נרמול קלט: קיצוץ רווחים והמרה לאותיות קטנות לצורך השוואה וייחודיות עקביים.
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;

            return email.Trim().ToLowerInvariant();
        }
    }
}
