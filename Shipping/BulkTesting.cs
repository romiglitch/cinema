using System;
using System.Configuration;
using System.Web;

namespace Shipping
{
    /// <summary>
    /// Bulk purchase / load testing mode. When enabled, order receipt emails are skipped.
    /// Enable via Web.config BulkTesting=true, ?bulkTesting=1 on any request (session-sticky),
    /// or .env BulkTesting=true.
    /// </summary>
    public static class BulkTesting
    {
        public const string SessionKey = "BulkTesting";
        public const string QueryParam = "bulkTesting";

        public static bool IsEnabled(HttpContext context)
        {
            if (ConfigEnabled)
                return true;

            if (context == null)
                return false;

            if (QueryEnabled(context.Request.QueryString[QueryParam]))
            {
                if (context.Session != null)
                    context.Session[SessionKey] = true;
                return true;
            }

            return context.Session?[SessionKey] is bool enabled && enabled;
        }

        public static void EnableIfRequested(HttpContext context)
        {
            if (context?.Session == null)
                return;
            if (QueryEnabled(context.Request.QueryString[QueryParam]))
                context.Session[SessionKey] = true;
        }

        private static bool ConfigEnabled =>
            string.Equals(
                ConfigurationManager.AppSettings["BulkTesting"],
                "true",
                StringComparison.OrdinalIgnoreCase);

        private static bool QueryEnabled(string value) =>
            value == "1" ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
