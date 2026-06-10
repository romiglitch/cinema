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
using System.Diagnostics;
using System.Web.UI.WebControls;


namespace Shipping
{
    public partial class Master : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // הצגת היסטוריית הצאט
                BindChat();
            }

        }
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            Session.Abandon(); // מוחק את כל נתוני הסשן (כולל displayName ו-category)
            Response.Redirect("HomePage.aspx"); // שולח אותו להתחבר מחדש
        }
        private List<string> GetAllMovieNamesFromDB()
        {
            // יצירת רשימה ריקה שתכיל את שמות הסרטים שנשלוף מהמסד
            List<string> movies = new List<string>();
            string cs = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            //מבטיח שהחיבור למסד הנתונים ייסגר וינוקה מהזיכרון בסיום הפעולה usingשימוש ב 
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // SELECT DISTINCT - מחזיר שמות ייחודיים (מונע כפילויות אם לסרט יש כמה הקרנות)
                // JOIN - מחבר בין טבלת הסרטים לטבלת ההקרנות כדי להביא רק סרטים שבאמת מוקרנים
                string query = @"SELECT DISTINCT m.Title 
                         FROM Movie m 
                         JOIN Screening s ON m.Id = s.MovieId";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    // כל עוד יש שורות (סרטים) שחזרו מהמסד
                    while (rdr.Read())
                    {
                        
                        movies.Add(rdr["Title"].ToString());
                    }
                }
            } 
            return movies;
        }
        // רשימת ספקי AI לפי סדר עדיפות - כולם תואמים ל-OpenAI API format
        // הפונקציה תנסה כל ספק בתור עד שאחד יענה בהצלחה
        private static readonly (string EnvKey, string Url, string Model)[] AiProviders = new[] //רידאונלי בשביל שישאר מוגן ולא ישתנה בזמן ההרצה בטעות
        {
            ("GEMINI_API_KEY",    "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", "gemini-2.0-flash"),
            ("GROQ_API_KEY",      "https://api.groq.com/openai/v1/chat/completions",                         "llama-3.3-70b-versatile"),
            ("XAI_API_KEY",       "https://api.x.ai/v1/chat/completions",                                    "grok-3-mini"),
            ("CEREBRAS_API_KEY",  "https://api.cerebras.ai/v1/chat/completions",                             "llama-3.3-70b"),
            ("MISTRAL_API_KEY",   "https://api.mistral.ai/v1/chat/completions",                              "mistral-small-latest"),
            ("OPENROUTER_API_KEY","https://openrouter.ai/api/v1/chat/completions",                           "meta-llama/llama-3.3-70b-instruct:free"),
            ("SAMBANOVA_API_KEY", "https://api.sambanova.ai/v1/chat/completions",                            "Meta-Llama-3.3-70B-Instruct"),
        };

        public async Task<string> AskAiForRecommendation(string prompt, List<ChatMessage> history)
        {
            // שליפת רשימת הסרטים - משותף לכל הספקים
            List<string> currentMovies = GetAllMovieNamesFromDB();
            string movieList = string.Join(", ", currentMovies);

            // בניית רשימת ההודעות בפורמט OpenAI (תואם לכל הספקים)
            var messages = new List<object>();
            messages.Add(new { role = "system", content = $"אתה עוזר חכם באתר קולנוע. המלץ על סרט אחד בלבד מהרשימה: {movieList}. התשובה חייבת להיות עד 3 משפטים בלבד. שם הסרט חייב להיות מודגש ב-**." });

            // הוספת היסטוריית השיחה - user/assistant במקום user/model
            if (history != null)
            {
                foreach (var msg in history)
                    messages.Add(new { role = msg.Sender == "User" ? "user" : "assistant", content = msg.Message });
            }

            // לולאה על הספקים - מנסים בתור עד שאחד עונה
            foreach (var (envKey, url, model) in AiProviders)
            {
                string apiKey = ConfigurationManager.AppSettings[envKey]?.Trim();
                if (string.IsNullOrEmpty(apiKey)) continue; // מפתח לא מוגדר - עוברים לבא

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}"); // כל ספקי ה-OpenAI משתמשים ב-Bearer token לאימות
                        client.Timeout = TimeSpan.FromSeconds(15); // אם הספק לא עונה תוך 15 שניות עוברים לבא

                        string json = JsonConvert.SerializeObject(new { model, messages }); // המרת הבקשה ל-JSON לפי פורמט OpenAI
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync(url, content); // שליחת הבקשה לספק
                        string resultJson = await response.Content.ReadAsStringAsync(); // קריאת התשובה כטקסט

                        if (response.IsSuccessStatusCode) // קוד 200 - הספק ענה בהצלחה
                        {
                            dynamic result = JsonConvert.DeserializeObject(resultJson);
                            return result.choices[0].message.content; // שליפת טקסט התשובה מפורמט OpenAI
                        }

                        // הספק נכשל (429 = חריגה ממכסה, 503 = עומס) - רושמים ועוברים לספק הבא
                        Debug.WriteLine($"AI provider {envKey} failed with {(int)response.StatusCode}: {resultJson}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AI provider {envKey} exception: {ex.Message}"); // שגיאת רשת/timeout - עוברים לספק הבא
                }
            }

            // כל הספקים נכשלו - מציגים הודעה ידידותית למשתמש
            return "אופס, כל שירותי ה-AI עמוסים כרגע. נסה שוב בעוד רגע! 🍿";
        }
        // מחלקה לעיצוב ההודעות
        public class ChatMessage
        {
            public string Sender { get; set; } // "User" או "AI"
            public string Message { get; set; }
        }
        private List<ChatMessage> ChatHistory
        {
            get//הבאת השיחה הקיימת מהסשן במידה והיא קיימת
            {
                if (Session["ChatHistory"] == null)
                    Session["ChatHistory"] = new List<ChatMessage>();
                return (List<ChatMessage>)Session["ChatHistory"];
            }
            set { Session["ChatHistory"] = value; }// מחזיר את כל רשימת ההודעות שנשמרו עבור המשתמש הזה
        }
        protected async void btnChatSend_Click(object sender, EventArgs e)
        {
            string userText = txtChatPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            // הוספת הודעת המשתמש להיסטוריה
            ChatHistory.Add(new ChatMessage { Sender = "User", Message = userText });
            txtChatPrompt.Text = ""; // ניקוי התיבה
            BindChat(); 

            //והמתנה עד קבלת תשובה לפני ההמשך בקוד AIשליחת השאלה ל
            string aiResponse = await AskAiForRecommendation(userText, ChatHistory);

            // הוספת התשובה להיסטורייית צאט
            ChatHistory.Add(new ChatMessage { Sender = "AI", Message = aiResponse });
            BindChat();
        }

        private void BindChat()
        {
            rptChat.DataSource = ChatHistory;
            rptChat.DataBind();
            //גלילה אוטומטית למטה
            ScriptManager.RegisterStartupScript(this, GetType(), "scroll", "scrollToBottom();", true);
        }
        protected void MoodClick(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender; // זיהוי הכפתור הספציפי שנלחץ
            string mood = btn.CommandArgument;    // שליפת מצב הרוח שהוגדר במאפייני הכפתור
            string userMessage = "אני מחפש סרט שמתאים למצב רוח: " + mood; // בניית נוסח הפנייה

            // שמירת הבקשה בסשן ועדכון היסטוריית הצאט
            AddMessageToHistory("User", userMessage);
            BindChat(); 

            // זימון הפעולה האסינכרונית עם הפרמטרים החדשים
            Page.RegisterAsyncTask(new PageAsyncTask(async () =>
            {
                //AIשליחת ההודעה הנוכחית עם ההיסטוריה ל
                string aiAnswer = await AskAiForRecommendation(userMessage, ChatHistory);
                // טיפול בתשובה שחזרה
                AddMessageToHistory("AI", aiAnswer); //שמירה של התשובה
                BindChat(); //עדכון ההיסטוריה
                upChat.Update(); // עדכון הפאנל
            }));
        }
        private void SaveMessage(string sender, string message)
        {
            //שליפת הרשימה הקיימת מהסשן או יצירת חדשה אם היא ריקה
            List<ChatMessage> history = Session["ChatHistory"] as List<ChatMessage>;
            if (history == null)
            {
                history = new List<ChatMessage>();
            }

            //  הוספת ההודעה החדשה
            history.Add(new ChatMessage { Sender = sender, Message = message });
            //שמירה חזרה לסשן
            Session["ChatHistory"] = history;

            //חיבור לריפיטר ועדכון ההיסטוריה
            rptChat.DataSource = history;
            rptChat.DataBind();
        }
        private void AddMessageToHistory(string sender, string message)
        {
            ChatHistory.Add(new ChatMessage { Sender = sender, Message = message });
        }
    }
}