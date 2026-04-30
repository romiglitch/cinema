using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using Shipping.localhost1; // שם ה-Web Reference שלך

namespace Shipping
{
    public partial class MovieDetails : System.Web.UI.Page
    {
        protected async void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!int.TryParse(Request.QueryString["id"], out int movieId))
                {
                    Response.Redirect("HomePage.aspx");
                    return;
                }

                // טוען פרטי הסרט מה־DB
                LoadMovie(movieId);

                // קריאה לוובסרביס לקבלת טריילר
                string trailerUrl = await GetTrailerFromWebServiceAsync(movieId);

                if (!string.IsNullOrEmpty(trailerUrl))
                {
                    // התאמה ל-iframe embed
                    modalTrailer.Attributes["data-src"] = trailerUrl.Replace("watch?v=", "embed/") + "?autoplay=1";
                }
            }
        }

        // קריאה אסינכרונית לוובסרביס
        private Task<string> GetTrailerFromWebServiceAsync(int movieId)
        {
            return Task.Run(() =>
            {
                Trailers client = new Trailers(); // מחלקת ה-client שנוצרה מ-Web Reference
                return client.GetTrailerKey(movieId);
            });
        }

        private void LoadMovie(int movieId)
        {
            // יירושה
            CinemaMovie movie = GetMovieByTmdbId(movieId);

            if (movie == null)
            {
                Response.Redirect("HomePage.aspx");
                return;
            }

            // יירושה
            imgPoster.ImageUrl = movie.Poster;
            lblTitle.InnerText = movie.Title;
            lblDescription.InnerText = movie.Desc;

            // שימוש במתודה ייחודית של הבן (CinemaMovie) להצגת זמן מעוצב
            lblDuration.InnerText = $"⏱ {movie.GetFormattedDuration()}";

            // שימוש בשדה המחיר החדש (אופציונלי להצגה בעמוד)
            // lblPrice.InnerText = movie.GetDisplayPrice();

            lblGenre.InnerText = $"🎭 {GetGenresByTmdbId(movieId)}";
        }

        private CinemaMovie GetMovieByTmdbId(int tmdbId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT * FROM Movie WHERE TmdbId = @TmdbId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TmdbId", tmdbId);
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                    
                        if (dr.Read())
                        {
                            // יצירת אובייקט מסוג הבן (CinemaMovie) ומילוי השדות שירש מהאב
                            return new CinemaMovie
                            {
                                Id = dr["Id"] != DBNull.Value ? (int)dr["Id"] : 0,
                                Title = dr["Title"]?.ToString(),
                                Desc = dr["Description"]?.ToString(),
                                Dur = dr["Duration"] != DBNull.Value ? (int)dr["Duration"] : 0,
                                Age = dr["Age"] != DBNull.Value ? (int)dr["Age"] : 0,
                                Poster = dr["Poster"]?.ToString(),
                                TicketPrice = 50.0 
                            };
                        }
                    }
                }
            }
            return null;
        }

        private string GetGenresByTmdbId(int tmdbId)
        {
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            string genres = "";

            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"
                    SELECT g.Name
                    FROM Movie m
                    JOIN MovieGenres mg ON m.Id = mg.IdMovie
                    JOIN Genres g ON mg.IdGenre = g.Id
                    WHERE m.TmdbId = @TmdbId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TmdbId", tmdbId);
                    conn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            genres += dr["Name"].ToString() + ", ";
                        }
                    }
                }
            }

            return genres.TrimEnd(',', ' ');
        }

        protected void btnBuyTickets_Click(object sender, EventArgs e)
        {
            if (int.TryParse(Request.QueryString["id"], out int movieId))
            {
                // שליחת ה-TmdbId לעמוד בחירת ההקרנה
                Response.Redirect($"SelectScreening.aspx?movieId={movieId}");
            }
        }
    }
}