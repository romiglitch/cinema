// Master.js — צ'אט AI ופונקציות גלובליות של ה-Master Page
// txtChatPromptId מוגדר בבלוק inline ב-Master.Master

// גלילה אוטומטית לתחתית אזור הצ'אט אחרי הודעה
function scrollToBottom() {
    var container = document.getElementById('chatContainer');//מציאת האובייקט עם id של chatContainer
    if (container) {//במידה ונמצא
        container.scrollTop = container.scrollHeight;//גלילה לגובה הצ'אט
    }
}

// פתיחה/סגירה של חלון הצ'אט - אם סגור פותח ומגלגל למטה, אם פתוח סוגר
function toggleChat() {
    var chat = document.getElementById('ai-chat-window');//מציאת האובייקט עם id של ai-chat-window
    if (chat.style.display === 'none' || chat.style.display === '') {//במידה והצ'אט סגור
        chat.style.display = 'flex';//פתיחת הצ'אט
        setTimeout(scrollToBottom, 50); // המתנה קצרה כדי שמסמך מודל האובייקט יתעדכן לפני הגלילה
    } else {//במידה והצ'אט פתוח
        chat.style.display = 'none';//סגירת הצ'אט
    }
}


// האזנה לאירועי UpdatePanel - כאשר בקשה לשרת מסתיימת גוללים למטה
Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
    scrollToBottom();
});

var prm = Sys.WebForms.PageRequestManager.getInstance();

// תחילת בקשה לשרת - הצגת אנימציית טעינה וגלילה למטה
prm.add_beginRequest(function () {
    var loading = document.getElementById('loading');
    if (loading) loading.style.display = 'flex';
    scrollToBottom();
});

// סיום בקשה לשרת - ניקוי שדה הקלט, הסתרת אנימציית טעינה וגלילה למטה
prm.add_endRequest(function () {
    var input = document.getElementById(txtChatPromptId);
    var loading = document.getElementById('loading');

    if (input) input.value = '';         // ניקוי תיבת הקלט אחרי שליחה
    if (loading) loading.style.display = 'none'; // הסתרת אנימציית טעינה

    var container = document.getElementById('chatContainer');
    if (container) container.scrollTop = container.scrollHeight; // גלילה לתשובה החדשה
});
