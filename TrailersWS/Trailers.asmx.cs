using System;
using System.Linq;
using System.Net.Http;
using System.Web.Services;
using Newtonsoft.Json.Linq;
using System.Configuration;

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
        // דוגמה: http://localhost:xxxx/Trailers.asmx/GetTrailerKey?tmdbId=637
        private static readonly HttpClient client = new HttpClient();

        [WebMethod]
        public string GetTrailerKey(int tmdbId)
        {
            if (tmdbId <= 0)
                return null;

            try
            {
                string apiKey = ConfigurationManager.AppSettings["TMDbApiKey"];
                string url = $"https://api.themoviedb.org/3/movie/{tmdbId}/videos?api_key={apiKey}&language=en-US";

                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                    return "STATUS: " + response.StatusCode;

                string json = response.Content.ReadAsStringAsync().Result;

                var data = JObject.Parse(json);
                var results = data["results"] as JArray;

                if (results != null && results.Count > 0)
                {
                    // מחפש ספציפית את ה-Trailer מיוטיוב
                    foreach (var video in results)
                    {
                        if (video["site"]?.ToString() == "YouTube" && video["type"]?.ToString() == "Trailer")
                        {
                            var key = video["key"]?.ToString();
                            if (!string.IsNullOrEmpty(key))
                                return "https://www.youtube.com/watch?v=" + key;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }

            return "NO TRAILER FOUND";
        }

    }
}
