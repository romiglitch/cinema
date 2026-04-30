using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.UI;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Shipping
{
    public partial class TestAPI : Page
    {
        protected async void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                await GetPopularMovies();
            }
        }
        private async Task LoadMovies()
        {
            string cacheKey = "popularMovies";
            var cachedData = Cache[cacheKey] as JToken;

            if (cachedData == null)
            {
                // לא במטמון – נביא מה-API
                string apiKey = ConfigurationManager.AppSettings["TMDbApiKey"];
                string url = $"https://api.themoviedb.org/3/movie/now_playing?api_key={apiKey}&language=he-IL&region=IL";

                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    cachedData = data["results"];

                    // נשמור את זה במטמון ל-10 דקות
                    Cache.Insert(cacheKey, cachedData, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);
                }
            }

            rptMovies.DataSource = cachedData;
            rptMovies.DataBind();
        }


        private async Task GetPopularMovies()
        {
            string apiKey = ConfigurationManager.AppSettings["TMDbApiKey"];
            string url = $"https://api.themoviedb.org/3/movie/now_playing?api_key={apiKey}&language=he-IL&region=IL";


            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    // דוגמה להדפסה על המסך
                    var results = data["results"];
                    foreach (var movie in results)
                    {
                        string title = movie["title"].ToString();
                        Response.Write($"<p>{title}</p>");
                    }
                }
                catch (Exception ex)
                {
                    Response.Write($"<p style='color:red;'>שגיאה: {ex.Message}</p>");
                }
            }
        }
    }
}
