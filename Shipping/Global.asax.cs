using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace Shipping
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            LoadEnvFile();
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            BulkTesting.EnableIfRequested(HttpContext.Current);
        }

        // קריאת קובץ .env משורש הפרויקט והזרקת הערכים ל-AppSettings בזמן ריצה
        // כך המפתחות לא נשמרים ב-Web.config ולא עולים ל-GitHub
        private static void LoadEnvFile()
        {
            string webRoot = HttpRuntime.AppDomainAppPath.TrimEnd('\\', '/');
            string solutionRoot = Path.GetDirectoryName(webRoot);
            string envPath = Path.Combine(solutionRoot, ".env");

            if (!File.Exists(envPath))
                return;

            foreach (string line in File.ReadAllLines(envPath))
            {
                // דילוג על שורות ריקות והערות
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                int eq = line.IndexOf('=');
                if (eq < 0) continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                // הזרקה ל-AppSettings - מדרסת את הערך הריק שב-Web.config
                ConfigurationManager.AppSettings[key] = value;
            }
        }
    }
}
