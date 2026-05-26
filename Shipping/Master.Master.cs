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
            Session.Abandon(); // מוחק את כל נתוני הסשן (כולל displayName)
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
        public async Task<string> AskAiForRecommendation(string prompt, List<ChatMessage> history)
        {
            try
            {
                //שמירת המפתח והכתובת
                string apiKey = ConfigurationManager.AppSettings["AIKey"].Trim();
                string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

                //רשימת הסרטים המוקרנים באתר
                List<string> currentMovies = GetAllMovieNamesFromDB();
                string movieList = string.Join(", ", currentMovies);

                using (HttpClient client = new HttpClient())//שיצור את הקשר עם גמיני HttpClient יצירת אובייקט
                {
                    //עם המפתח שלי AI אימות עם השרת 
                    client.DefaultRequestHeaders.Add("X-goog-api-key", apiKey);

                    // בניית מבנה ההודעות עבור המודל
                    var messages = new List<object>();

                    // הניות
                    messages.Add(new
                    {
                        role = "user",
                        parts = new[] { new { text = $"אתה עוזר חכם באתר קולנוע. המלץ על סרט אחד בלבד מהרשימה: {movieList}. " +
                                              "התשובה חייבת להיות עד 3 משפטים בלבד. שם הסרט חייב להיות מודגש ב-**." } }//הדגמה למה הלקוח מבקש
                    });

                    // הדגמה של תשובה
                    messages.Add(new
                    {
                        role = "model",
                        parts = new[] { new { text = "הבנתי. אתן המלצה קצרה על סרט אחד בלבד." } }
                    });

                    // הוספת היסטוריות השיחות כדי לאפשר המשכיות
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

                    //כדי שיהיה ניתן לשלוח אותה JSONהמרת הבקשה ל
                    var requestBody = new { contents = messages };
                    string json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    //שליחת הבקשה לשרת של גוגל והמתנה (בלי לתקוע את האתר) עד שהשרת יחזיר תשובה.
                    var response = await client.PostAsync(apiUrl, content);
                    string resultJson = await response.Content.ReadAsStringAsync();//המרת התשובה לטקסט פשוט

                    if (response.IsSuccessStatusCode)
                    {
                        //המרת הטקסט לאובייקט שניתן לבדוק אוץו ושליפת התשובה
                        dynamic result = JsonConvert.DeserializeObject(resultJson);
                        return result.candidates[0].content.parts[0].text;
                    }

                    return "אופס, יש כרגע עומס בקופות... נסה שוב בעוד רגע! 🍿";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.ToString());
                return "משהו השתבש בתקשורת עם ה-AI. אנחנו מטפלים בזה!"; 
               
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