using System;
using System.Linq;
using System.Net.Http;
using System.Web.Services;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Diagnostics;
namespace TrailersWS
{
    /// <summary>
    /// WebService להחזרת טריילרים של סרטים
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class Trailers : System.Web.Services.WebService
    {
        //(HttpClient) הגדרת שליח
        private static readonly HttpClient client = new HttpClient();

        [WebMethod]
        public string GetTrailerKey(int tmdbId)
        {
            //בדיקת תקינות
            if (tmdbId <= 0)
                return null;

            try
            {
                string apiKey = ConfigurationManager.AppSettings["TMDbApiKey"];

                //כדי לקבל רשימת סרטונים עבור הסרט הספציפי TMDb בניית הכתובת לשירות של  
                string url = $"https://api.themoviedb.org/3/movie/{tmdbId}/videos?api_key={apiKey}&language=en-US";

                //והמתנה לתשובה APIביצוע הפנייה ל
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                    return "STATUS: " + response.StatusCode;

                //JSON קריאת התשובה כמחרוזת ארוכה מפורמט
                string json = response.Content.ReadAsStringAsync().Result;

                // כאובייקט שאפשר לעבוד איץו JSONפיענוח ה
                var data = JObject.Parse(json);
                var results = data["results"] as JArray;

                // מעבר על רשימת הסרטונים שחזרו כדי למצוא את הטריילר המתאים
                if (results != null && results.Count > 0)
                {
                    foreach (var video in results)
                    {
                        //חיפוש סרטון שמוגדר כטריילר רשמית וגם נמצא ביוטיוב
                        if (video["site"]?.ToString() == "YouTube" && video["type"]?.ToString() == "Trailer")//במקרה ומשהו חסר יוחזר נאל במקום קריסה - ?
                        {
                            var key = video["key"]?.ToString();
                            if (!string.IsNullOrEmpty(key))
                            {
                                //החזרת הכתובת המלאה של הסרטון
                                return "https://www.youtube.com/watch?v=" + key;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.ToString());
                return "ERROR: חלה שגיאה במשיכת הטריילר.";
            }

            return "NO TRAILER FOUND";

        }
    }
}
