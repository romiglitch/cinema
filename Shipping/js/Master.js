// Master.js — צ'אט AI ופונקציות גלובליות של ה-Master Page
// txtChatPromptId מוגדר בבלוק inline ב-Master.Master

function scrollToBottom() {
    var container = document.getElementById('chatContainer');
    if (container) {
        container.scrollTop = container.scrollHeight;
    }
}

function toggleChat() {
    var chat = document.getElementById('ai-chat-window');
    if (chat.style.display === 'none' || chat.style.display === '') {
        chat.style.display = 'flex';
        setTimeout(scrollToBottom, 50);
    } else {
        chat.style.display = 'none';
    }
}

function handleChatClick(btn, event) {
    var input = document.getElementById(txtChatPromptId);
    var container = document.getElementById('chatContainer');
    var loading = document.getElementById('loading');

    if (input && input.value.trim() !== "") {
        var row = document.createElement('div');
        row.className = 'message-row user';
        row.innerHTML = '<div class="bubble">' + input.value + '</div>';

        if (loading) {
            container.insertBefore(row, loading);
            loading.style.display = 'flex';
        } else {
            container.appendChild(row);
        }

        container.scrollTop = container.scrollHeight;
        __doPostBack(btn.name, '');
    }
}

// אתחול אירועי UpdatePanel לצ'אט
Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
    scrollToBottom();
});

var prm = Sys.WebForms.PageRequestManager.getInstance();
prm.add_beginRequest(function () {
    var loading = document.getElementById('loading');
    if (loading) loading.style.display = 'flex';
    scrollToBottom();
});

prm.add_endRequest(function () {
    var input = document.getElementById(txtChatPromptId);
    var loading = document.getElementById('loading');

    if (input) input.value = '';
    if (loading) loading.style.display = 'none';

    var container = document.getElementById('chatContainer');
    if (container) container.scrollTop = container.scrollHeight;
});
