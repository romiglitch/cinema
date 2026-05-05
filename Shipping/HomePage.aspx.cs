using DALLlilbrary;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Shipping
{
    public partial class HomePage : System.Web.UI.Page
    {
        string apiKey = ConfigurationManager.AppSettings["TMDbApiKey"];

        protected async void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)//הרצה רק בפעם הראשונה שהמשתמש נכנס לדף
            {
                await ImportMoviesFromApiAsync();//השרת לא ממשיך בקוד אלא מתפנה לעבוד על דברים אחרים (משתמשים(
                await LoadMoviesAsync();
            }
        }
        private async Task LoadMoviesAsync()
        {
            string cacheKey = "NowPlayingMovies";//הגדרת הזיכרון
            var cachedData = Cache[cacheKey] as JToken;//JToken באובייקט בפורמט NowPlayingMoviesשמירת מה שיש מ

            if (cachedData == null)
            {
                string url = $"https://api.themoviedb.org/3/movie/now_playing?api_key={apiKey}&language=he-IL&region=IL";

                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    cachedData = data["results"];

                    // שמירה במטמון ל-10 דקות
                    Cache.Insert(cacheKey, cachedData, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);
                }
            }

            // Map ל-Repeater
            var movieList = new List<Movie>();


            foreach (var item in cachedData)
            {
                int tmdbId = item["id"].ToObject<int>();


                movieList.Add(new Movie
                {
                    Id = tmdbId,
                    Title = item["title"].ToString(),
                    Poster = "https://image.tmdb.org/t/p/w200" + item["poster_path"].ToString(),
                });
            }

            rptMovies.DataSource = movieList;
            rptMovies.DataBind();
        }
        private void InsertMovie(string title, string description, int duration, int age, string poster, int tmdbId, List<int> genreIds)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            //שמירת הסרטים
            string queryMovie = @"INSERT INTO Movie (Title, Description, Duration, Age, Poster, TmdbId)
                          VALUES (@Title, @Description, @Duration, @Age, @Poster, @TmdbId);
                          SELECT SCOPE_IDENTITY();"; //האחרון שנוצר IDה

            int movieId;
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(queryMovie, conn))
            {
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Description", description);
                cmd.Parameters.AddWithValue("@Duration", duration == 0 ? (object)DBNull.Value : duration);
                cmd.Parameters.AddWithValue("@Age", age);
                cmd.Parameters.AddWithValue("@Poster", poster);
                cmd.Parameters.AddWithValue("@TmdbId", tmdbId);

                conn.Open();
                movieId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // שמירת הז’אנרים
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                foreach (int genreId in genreIds)
                {
                    string queryGenre = @"INSERT INTO MovieGenres (IdMovie, IdGenre) VALUES (@IdMovie, @IdGenre)";
                    using (SqlCommand cmd = new SqlCommand(queryGenre, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdMovie", movieId);
                        cmd.Parameters.AddWithValue("@IdGenre", genreId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private async Task<int> GetMovieRuntimeAsync(int movieId)
        {
            string url = $"https://api.themoviedb.org/3/movie/{movieId}?api_key={apiKey}&language=en-US";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(json);

                return data["runtime"]?.ToObject<int>() ?? 0;
            }
        }
        private async Task<int> GetMovieAgeRatingAsync(int movieId)
        {
            string url = $"https://api.themoviedb.org/3/movie/{movieId}/release_dates?api_key={apiKey}";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(json);

                var results = data["results"];
                if (results == null) return 0;

                foreach (var country in results)
                {
                    // Israel = "IL", fallback to US
                    if (country["iso_3166_1"].ToString() == "IL" ||
                        country["iso_3166_1"].ToString() == "US")
                    {
                        var rel = country["release_dates"][0];//בחירת ההפצה הראשונה
                        string cert = rel["certification"]?.ToString();//מחפשים את הדירוג בהפצה הראשונה. אם אין נשמור נאל

                        if (int.TryParse(cert, out int age))//אם מצאנו הגבלת גיל נשים אותה, אם לא אז 0 -out
                            return age;
                    }
                }
            }

            return 0;
        }

        private bool MovieExistsByTmdbId(int tmdbId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = "SELECT COUNT(*) FROM Movie WHERE TmdbId = @TmdbId";

            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@TmdbId", tmdbId);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;//אם חזר 1 אז אמת אם חזר 0 אז שקר
            }
        }

        private async Task ImportMoviesFromApiAsync()
        {
            string url = $"https://api.themoviedb.org/3/movie/now_playing?api_key={apiKey}&language=he-IL&region=IL";

            using (HttpClient client = new HttpClient())//APIשמבצע את הגלישה לאתר ה HttpClient יצירת אובייקט
            {
                var response = await client.GetAsync(url);//הקוד עוצר עד שיקבל את המידע .APIהמידע שהובא מה
                string json = await response.Content.ReadAsStringAsync();//JSONשמירת התשובה כמחרוזת מ
                JObject data = JObject.Parse(json);//C#פארסינג למחרוזת הארוכה לאובייקט שקל לעבוד איתו ב
                var apiMovies = data["results"];//שמירת התוצאות בלבד מכל המידע שיש

                //LINQ שמירת רק המזהים של הסרטים בעזרת
                List<int> currentApiTmdbIds = apiMovies.Select(m => m["id"].ToObject<int>()).ToList();

                //APIמחיקת כל הסרטים שכבר לא מופיעים ב
                DeleteOldMovies(currentApiTmdbIds);

                //(הוספת הסרטים לטבלה (במידה ולא קיימים כבר בה
                foreach (var item in apiMovies)
                {
                    int tmdbId = item["id"].ToObject<int>();
                    if (!MovieExistsByTmdbId(tmdbId))
                    {
                        string title = item["title"]?.ToString();
                        string description = item["overview"]?.ToString();
                        string poster = "https://image.tmdb.org/t/p/w200" + item["poster_path"]?.ToString();
                        int duration = await GetMovieRuntimeAsync(tmdbId);//APIפרטים שלא מופיעים ברשימה המתקבלת מה
                        int age = await GetMovieAgeRatingAsync(tmdbId);//לכן צריך פונקציה נפרדת
                        var genreIds = item["genre_ids"].Select(g => g.ToObject<int>()).ToList();

                        InsertMovie(title, description, duration, age, poster, tmdbId, genreIds);
                    }
                }
            }
        }

        // פונקציית עזר למחיקה
        private void DeleteOldMovies(List<int> currentTmdbIds)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string idList = string.Join(",", currentTmdbIds);
            //לזאנר שלו APIמחיקת הקשר בין הסרט שלא מופיע ב 
            //APIמחיקת ההקרנות המתכוננות של הסרט שלא מופיע ב 
            //לזאנר שלומחיקת הסרט עצמו מהטבלה
            string query = $@"
        DELETE FROM MovieGenres WHERE IdMovie IN (SELECT Id FROM Movie WHERE TmdbId NOT IN ({idList}));
        DELETE FROM Screening WHERE MovieId IN (SELECT Id FROM Movie WHERE TmdbId NOT IN ({idList}));
        DELETE FROM Movie WHERE TmdbId NOT IN ({idList});";

            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
      
        

    }
}

