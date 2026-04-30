using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace Shipping
{
    public partial class Master : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // טעינה ראשונית של ההיסטוריה לתוך ה-Repeater
                BindChat();
            }
            //if (!IsPostBack)
            //{
            //    // מחוץ ל־if (!IsPostBack)
            //    if (Session["category"] != null)
            //    {
            //        string category = Session["category"].ToString();
            //        if (category == "user" || category == "admin")
            //        {
            //            Btn.Visible = true;
            //        }
            //    }
            //}

        }
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            Session.Abandon(); // מוחק את כל הנתונים (כולל את ה-username)
            Response.Redirect("HomePage.aspx"); // שולח אותו להתחבר מחדש
        }
        private List<string> GetAllMovieNamesFromDB()
        {
            List<string> movies = new List<string>();
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(cs))
            {
                // שליפת שמות סרטים ייחודיים שיש להם הקרנות
                string query = @"SELECT DISTINCT m.Title 
                         FROM Movie m 
                         JOIN Screening s ON m.Id = s.MovieId";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        movies.Add(rdr["Title"].ToString());
                    }
                }
            }
            return movies;
        }
        public async Task<string> AskAiForRecommendation(string prompt, List<ChatMessage> history)
        {
            try
            {
                string apiKey = ConfigurationManager.AppSettings["AIKey"].Trim();
                string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

                // שליפת רשימת הסרטים מה-DB (כמו שעשית קודם)
                List<string> currentMovies = GetAllMovieNamesFromDB();
                string movieList = string.Join(", ", currentMovies);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-goog-api-key", apiKey);

                    // --- כאן בניית ההודעה המקוצרת ---
                    var messages = new List<object>();

                    // 1. הוראת מערכת (System Instruction) שמכריחה אותו לקצר
                    messages.Add(new
                    {
                        role = "user",
                        parts = new[] { new { text = $"אתה עוזר חכם באתר קולנוע. המלץ על סרט אחד בלבד מהרשימה: {movieList}. " +
                                             "התשובה חייבת להיות עד 3 משפטים בלבד. שם הסרט חייב להיות מודגש ב-**." } }
                    });

                    // 2. אישור של המודל שהוא הבין את המגבלה
                    messages.Add(new
                    {
                        role = "model",
                        parts = new[] { new { text = "הבנתי. אתן המלצה קצרה על סרט אחד בלבד." } }
                    });

                    // 3. הוספת היסטוריית השיחה מה-Session (כדי שיהיה לו זיכרון)
                    if (history != null)
                    {
                        foreach (var msg in history)
                        {
                            messages.Add(new
                            {
                                role = msg.Sender == "User" ? "user" : "model",
                                parts = new[] { new { text = msg.Message } }
                            });
                        }
                    }

                    // בניית גוף הבקשה
                    var requestBody = new { contents = messages };
                    string json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, content);
                    string resultJson = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic result = JsonConvert.DeserializeObject(resultJson);
                        return result.candidates[0].content.parts[0].text;
                    }

                    return "אופס, יש כרגע עומס בקופות... נסה שוב בעוד רגע! 🍿";
                }
            }
            catch (Exception ex)
            {
                return "תקלה טכנית: " + ex.Message;
            }
        }
        // מחלקה לעיצוב ההודעות
        public class ChatMessage
        {
            public string Sender { get; set; } // "User" או "AI"
            public string Message { get; set; }
        }
        private List<ChatMessage> ChatHistory
        {
            get
            {
                if (Session["ChatHistory"] == null)
                    Session["ChatHistory"] = new List<ChatMessage>();
                return (List<ChatMessage>)Session["ChatHistory"];
            }
            set { Session["ChatHistory"] = value; }
        }
        protected async void btnChatSend_Click(object sender, EventArgs e)
        {
            string userText = txtChatPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            // 1. הוספת הודעת המשתמש להיסטוריה
            ChatHistory.Add(new ChatMessage { Sender = "User", Message = userText });
            txtChatPrompt.Text = ""; // ניקוי התיבה
            BindChat(); // עדכון ה-UI

            // 2. פנייה ל-AI עם ההיסטוריה
            // שימי לב: אנחנו שולחים ל-AI את ה-ChatHistory כדי שיהיה לו "זיכרון"
            string aiResponse = await AskAiForRecommendation(userText, ChatHistory);

            // 3. הוספת תשובת ה-AI להיסטוריה
            ChatHistory.Add(new ChatMessage { Sender = "AI", Message = aiResponse });
            BindChat();
        }

        private void BindChat()
        {
            rptChat.DataSource = ChatHistory;
            rptChat.DataBind();
            // גלילה אוטומטית למטה (דרך סקריפט בסוף ה-UpdatePanel)
            ScriptManager.RegisterStartupScript(this, GetType(), "scroll", "scrollToBottom();", true);
        }
        protected void MoodClick(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender;
            string mood = btn.CommandArgument;
            string userMessage = "אני מחפש סרט שמתאים למצב רוח: " + mood;

            // 1. הוספת הודעת המשתמש להיסטוריה (ב-Session) ול-UI
            AddMessageToHistory("User", userMessage);
            BindChat(); // עדכון ה-Repeater כדי שהמשתמש יראה את מה שהוא לחץ

            // 2. זימון הפעולה האסינכרונית עם הפרמטרים החדשים
            Page.RegisterAsyncTask(new PageAsyncTask(async () =>
            {
                // שליחת ההודעה הנוכחית יחד עם כל ההיסטוריה שנשמרה ב-Session
                string aiAnswer = await AskAiForRecommendation(userMessage, ChatHistory);

                // 3. הוספת תשובת ה-AI להיסטוריה ועדכון התצוגה
                AddMessageToHistory("AI", aiAnswer);
                BindChat();
                upChat.Update(); // רענון ה-UpdatePanel
            }));
        }
        private void SaveMessage(string sender, string message)
        {
            // 1. שליפת הרשימה הקיימת מה-Session או יצירת חדשה אם היא ריקה
            List<ChatMessage> history = Session["ChatHistory"] as List<ChatMessage>;
            if (history == null)
            {
                history = new List<ChatMessage>();
            }

            // 2. הוספת ההודעה החדשה
            history.Add(new ChatMessage { Sender = sender, Message = message });

            // 3. שמירה חזרה ל-Session
            Session["ChatHistory"] = history;

            // 4. חיבור לרפיטר - שימי לב לשם rptChat!
            rptChat.DataSource = history;
            rptChat.DataBind();
        }
        private void AddMessageToHistory(string sender, string message)
        {
            ChatHistory.Add(new ChatMessage { Sender = sender, Message = message });
        }
    }
}