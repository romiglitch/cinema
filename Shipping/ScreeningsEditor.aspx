<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ScreeningsEditor.aspx.cs" Inherits="Shipping.ScreeningsEditor" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
   <script type="text/javascript">
       // בודק אם יש צ'קבוקסים שסטטוסם שונה מהמצב השמור בשרת
       function hasUnsavedScheduleChanges() {
           var checkboxes = document.querySelectorAll('.weekSchedule .checkbox__trigger[data-initial-checked]');
           for (var i = 0; i < checkboxes.length; i++) {
               var cb = checkboxes[i];
               var initialChecked = cb.getAttribute('data-initial-checked') === 'true';
               if (cb.checked !== initialChecked) {
                   return true;
               }
           }
           return false;
       }

       // מציג/מסתיר את כפתור "עדכן הקרנות" לפי קיום שינויים שלא נשמרו
       function setUpdateButtonVisible(visible) {
           var btn = document.getElementById('<%= btnAddScreening.ClientID %>');
           if (!btn) return;

           if (visible) {
               btn.classList.remove('hiddenBtn');
               btn.classList.add('showBtn', 'login-btn');
               btn.style.opacity = '1';
               btn.style.pointerEvents = 'auto';
               btn.style.transform = 'translateY(0)';
           } else {
               btn.classList.remove('showBtn');
               btn.classList.add('hiddenBtn', 'login-btn');
               btn.style.opacity = '0';
               btn.style.pointerEvents = 'none';
           }
       }

       function updateUnsavedState() {
           var lbl = document.getElementById('<%= lblMessage.ClientID %>');
           var hasChanges = hasUnsavedScheduleChanges();

           setUpdateButtonVisible(hasChanges);

           if (lbl && hasChanges) {
               lbl.innerHTML = "";
           }
       }

       // מסמן/מסיר רקע בהיר לתא עם שינוי שלא נשמר
       function updateCellPendingState(checkbox) {
           var cell = checkbox.closest('td.schedule-day-cell');
           if (!cell) return;

           var initialChecked = checkbox.getAttribute('data-initial-checked') === 'true';
           var isChanged = checkbox.checked !== initialChecked;

           if (isChanged) {
               cell.classList.add('schedule-cell-pending');
           } else {
               cell.classList.remove('schedule-cell-pending');
           }
       }

       // מסנכרן את מצב הרקע והכפתור לכל התאים אחרי טעינת הדף
       function initScheduleEditorUi() {
           var ddl = document.getElementById('<%= ddlMovies.ClientID %>');
           if (ddl) {
               ddl.setAttribute('data-selected-value', ddl.value);
           }

           var checkboxes = document.querySelectorAll('.weekSchedule .checkbox__trigger[data-initial-checked]');
           for (var i = 0; i < checkboxes.length; i++) {
               updateCellPendingState(checkboxes[i]);
           }

           updateUnsavedState();
       }

       // לפני מעבר לסרט אחר — מאשר ביטול שינויים שלא נשמרו
       function onMovieSelectionChanging(select) {
           if (hasUnsavedScheduleChanges()) {
               var confirmed = confirm('יש שינויים שלא נשמרו בהקרנות. האם לבטל אותם ולעבור לסרט אחר?');
               if (!confirmed) {
                   select.value = select.getAttribute('data-selected-value');
                   return false;
               }
           }

           select.setAttribute('data-selected-value', select.value);
           __doPostBack('<%= ddlMovies.UniqueID %>', '');
           return false;
       }

       // נקרא בלחיצה על צ'קבוקס בלוח — מסנכרן את המראה הירוק, הרקע והכפתור
       function onScheduleCheckboxClick(checkbox) {
           // עוטף ה-CSS של הצ'קבוקס המותאם (checkbox-wrapper-33)
           var wrapper = checkbox.closest('.checkbox-wrapper-33');
           if (wrapper) {
               // האלמנט שמציג את סימן ה-V הירוק
               var symbol = wrapper.querySelector('.checkbox__symbol');
               if (symbol) {
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
           }

           updateCellPendingState(checkbox);
           updateUnsavedState();
       }

       if (window.addEventListener) {
           window.addEventListener('load', initScheduleEditorUi);
       } else {
           window.attachEvent('onload', initScheduleEditorUi);
       }
   </script>

  <div class="signup-form" style=" margin-top: 80px;">
    <div style="text-align:center">  <asp:DropDownList CssClass= ID="ddlMovies" runat="server" AutoPostBack="false" OnSelectedIndexChanged="ddlMovies_SelectedIndexChanged"></asp:DropDownList>
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
