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
            if (!IsPostBack)
            {
                await ImportMoviesFromApiAsync();
                await LoadMoviesAsync();
            }
        }
        private async Task LoadMoviesAsync()
        {
            // מטמון פשוט ל-10 דקות
            string cacheKey = "NowPlayingMovies";
            var cachedData = Cache[cacheKey] as JToken;

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
            var movieList = new System.Collections.Generic.List<Movie>();


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

            // 1️⃣ שמירת הסרט
            string queryMovie = @"INSERT INTO Movie (Title, Description, Duration, Age, Poster, TmdbId)
                          VALUES (@Title, @Description, @Duration, @Age, @Poster, @TmdbId);
                          SELECT SCOPE_IDENTITY();"; // מחזיר את ה-Id של הסרט

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
                movieId = Convert.ToInt32(cmd.ExecuteScalar()); // <-- Movie.Id
            }

            // 2️⃣ שמירת הז’אנרים
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
                        var rel = country["release_dates"][0];
                        string cert = rel["certification"]?.ToString();

                        if (int.TryParse(cert, out int age))
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
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private async Task ImportMoviesFromApiAsync()
        {
            string url = $"https://api.themoviedb.org/3/movie/now_playing?api_key={apiKey}&language=he-IL&region=IL";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(json);
                var apiMovies = data["results"];

                // רשימה שתשמור את ה-IDs של הסרטים שקיבלנו מה-API עכשיו
                List<int> currentApiTmdbIds = apiMovies.Select(m => m["id"].ToObject<int>()).ToList();

                // 1. מחיקת סרטים ישנים (כאלו שלא ב-API הנוכחי)
                // שים לב: בגלל קשרי גומלין (Foreign Keys), כדאי שהמחיקה ב-SQL תהיה ב-Cascade 
                // או למחוק קודם מההקרנות ומהז'אנרים.
                DeleteOldMovies(currentApiTmdbIds);

                // 2. הוספת סרטים חדשים (רק אם הם לא קיימים)
                foreach (var item in apiMovies)
                {
                    int tmdbId = item["id"].ToObject<int>();
                    if (!MovieExistsByTmdbId(tmdbId))
                    {
                        string title = item["title"]?.ToString();
                        string description = item["overview"]?.ToString();
                        string poster = "https://image.tmdb.org/t/p/w200" + item["poster_path"]?.ToString();
                        int duration = await GetMovieRuntimeAsync(tmdbId);
                        int age = await GetMovieAgeRatingAsync(tmdbId);
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
            // שאילתה שמוחקת סרטים שה-TmdbId שלהם לא מופיע ברשימה שקיבלנו
            string idList = string.Join(",", currentTmdbIds);
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
        private int GetMovieIdByTmdbId(int tmdbId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT Id FROM Movie WHERE TmdbId = @TmdbId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TmdbId", tmdbId);
                    conn.Open();
                    return (int)cmd.ExecuteScalar();
                }
            }
        }
        private void InsertMovieGenres(int tmdbId, List<int> genreIds)
        {
            int movieId = GetMovieIdByTmdbId(tmdbId);
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();

                foreach (int genreId in genreIds)
                {
                    string query = @"
                INSERT INTO MovieGenres (IdMovie, IdGenre)
                VALUES (@IdMovie, @IdGenre)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdMovie", movieId);
                        cmd.Parameters.AddWithValue("@IdGenre", genreId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        private bool ScreeningsExistForMovie(int movieId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string query = "SELECT COUNT(*) FROM Screening WHERE MovieId = @MovieId";

            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MovieId", movieId);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        private void CreateScreeningsForMovie(int movieId, int duration)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            // שעות הקרנה קבועות
            TimeSpan[] showTimes =
            {
        new TimeSpan(13, 00, 0),
        new TimeSpan(16, 30, 0),
        new TimeSpan(20, 00, 0)
    };

            Random rnd = new Random();

            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();

                for (int day = 0; day < 14; day++)
                {
                    DateTime date = DateTime.Today.AddDays(day);

                    foreach (var time in showTimes)
                    {
                        DateTime startTime = date.Add(time);
                        DateTime endTime = startTime.AddMinutes(duration > 0 ? duration : 100);

                        string query = @"
                INSERT INTO Screening
                (MovieId, Hall, SeatesBought, StartTime, EndTime)
                VALUES
                (@MovieId, @Hall, @Seats, @StartTime, @EndTime)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@MovieId", movieId);
                            cmd.Parameters.AddWithValue("@StartTime", startTime);
                            cmd.Parameters.AddWithValue("@EndTime", endTime);
                            cmd.Parameters.AddWithValue("@Hall", rnd.Next(1, 6)); // אולמות 1–5
                            cmd.Parameters.AddWithValue("@Seats", 0);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
      
        

    }
}

