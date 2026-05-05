using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using Shipping.localhost1;

namespace Shipping
{
    public partial class MovieDetails : System.Web.UI.Page
    {
        protected async void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!int.TryParse(Request.QueryString["id"], out int movieId))//לא ניתן להגיע לעמוד בשום דרך שהיא לא לחיצה על הפוסטר
                {
                    Response.Redirect("HomePage.aspx");
                    return;
                }

                //טעינת פרטי הסרט
                LoadMovie(movieId);

                // קריאה לוובסרביס לקבלת טריילר
                string trailerUrl = await GetTrailerFromWebServiceAsync(movieId);

                if (!string.IsNullOrEmpty(trailerUrl))
                {
                    //Modalבשביל לעקוף את החסימה של יוטיוב ולהציג את הטריילר ישירות ב Embed
                    //הוספת פקדוה שמתחילה לנגן את הטריילר במיידי
                    modalTrailer.Attributes["data-src"] = trailerUrl.Replace("watch?v=", "embed/") + "?autoplay=1";
                }
                else
                {

#if DEBUG
                    if (!string.IsNullOrEmpty(trailerUrl))
                    {
                        Response.Write("<!-- Trailer Debug: " + trailerUrl + " -->");
                    }
#endif
                }
            }
             }

        // קריאה אסינכרונית לוובסרביס
        private Task<string> GetTrailerFromWebServiceAsync(int movieId)
        {
            return Task.Run(() =>
            {
                Trailers client = new Trailers(); // מחלקה מהוובסרוויס
                return client.GetTrailerKey(movieId);
            });
        }

        private void LoadMovie(int movieId)
        {
            //CinemaMovie טעינת פרטי הסרט באמצעות אובייקט
            CinemaMovie movie = GetMovieByTmdbId(movieId);

            if (movie == null)
            {
                Response.Redirect("HomePage.aspx");
                return;
            }

            imgPoster.ImageUrl = movie.Poster;
            lblTitle.InnerText = movie.Title;
            lblDescription.InnerText = movie.Desc;
            lblDuration.InnerText = $"⏱ {movie.GetFormattedDuration()}";
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
                            //CinemaMovie יצירת אובייקט מסוג
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
                //של הסרט הרצוי IDעמוד בחירת ההקרנה עם ה
                Response.Redirect($"SelectScreening.aspx?movieId={movieId}");
            }
        }
    }
}