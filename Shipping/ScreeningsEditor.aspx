<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ScreeningsEditor.aspx.cs" Inherits="Shipping.ScreeningsEditor" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
   <script type="text/javascript">
       // מציג את כפתור "עדכן הקרנות" ומנקה הודעות משוב קודמות אחרי שינוי בלוח
       function showUpdateButton() {
           var btn = document.getElementById('<%= btnAddScreening.ClientID %>');
           var lbl = document.getElementById('<%= lblMessage.ClientID %>');

           if (btn) {
               // מסיר הסתרה ומחיל את עיצוב כפתור ההתחברות כדי שהכפתור יופיע מתחת ללוח
               btn.classList.remove('hiddenBtn');
               btn.classList.add('showBtn', 'login-btn');
           }

           if (lbl) {
               // מנקה הודעה מהשמירה הקודמת (למשל "לא בוצעו שינויים")
               lbl.innerHTML = "";
           }
       }

       // נקרא בלחיצה על צ'קבוקס בלוח — מסנכרן את המראה הירוק ומציג את כפתור השמירה
       function onScheduleCheckboxClick(checkbox) {
           showUpdateButton();

           // עוטף ה-CSS של הצ'קבוקס המותאם (checkbox-wrapper-33)
           var wrapper = checkbox.closest('.checkbox-wrapper-33');
           if (!wrapper) return;

           // האלמנט שמציג את סימן ה-V הירוק
           var symbol = wrapper.querySelector('.checkbox__symbol');
           if (!symbol) return;

           if (checkbox.checked) {
               // הקרנה נבחרה — מסמן את התא כפעיל (ירוק)
               wrapper.classList.add('checkbox-wrapper-33--checked');
               symbol.classList.add('checkbox__symbol--checked');
           } else {
               // ביטול בחירה — מחזיר למראה רגיל
               wrapper.classList.remove('checkbox-wrapper-33--checked');
               symbol.classList.remove('checkbox__symbol--checked');
           }
       }
   </script>

  <div class="signup-form" style=" margin-top: 80px;">
    <div style="text-align:center">  <asp:DropDownList CssClass= ID="ddlMovies" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlMovies_SelectedIndexChanged"></asp:DropDownList>
</div>
<asp:DropDownList ID="ddlTimes" runat="server" Visible="false"></asp:DropDownList>

<asp:DropDownList ID="ddlHalls" runat="server" Visible="false"></asp:DropDownList>
 
        <asp:Panel ID="pnlSchedule" runat="server" Visible="false"></asp:Panel>

<div class="screenings-editor-actions">
<asp:Button CssClass="login-btn hiddenBtn"
    ID="btnAddScreening"
    runat="server"
    Text="עדכן הקרנות"
    EnableViewState="false"
    OnClick="btnAddScreening_Click" />
    <asp:Label CssClass="editorMsg"
        ID="lblMessage"
        runat="server"
        EnableViewState="false">
    </asp:Label>
</div>

    </div>
</asp:Content>
