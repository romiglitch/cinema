<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ScreeningsEditor.aspx.cs" Inherits="Shipping.ScreeningsEditor" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
   <script type="text/javascript">
       // מחזיר את הצ'קבוקס מתוך תא בלוח ההקרנות
       function getScheduleCheckbox(cell) {
           if (!cell) return null;
           return cell.querySelector('input[type="checkbox"]');
       }

       // בודק אם יש תאים שסטטוס הצ'קבוקס שלהם שונה מהמצב השמור בשרת
       function hasUnsavedScheduleChanges() {
           var cells = document.querySelectorAll('.weekSchedule td.schedule-day-cell[data-initial-checked]');
           for (var i = 0; i < cells.length; i++) {
               var cell = cells[i];
               var cb = getScheduleCheckbox(cell);
               // תאים חסומים (עבר / אין אולם פנוי) לא נספרים כשינוי
               if (!cb || cb.disabled) continue;

               var initialChecked = cell.getAttribute('data-initial-checked') === 'true';
               if (cb.checked !== initialChecked) {
                   return true;
               }
           }
           return false;
       }

       // מפעיל/מנטרל את כפתור "עדכן הקרנות" — הכפתור תמיד גלוי, רק לא לחיץ כשאין שינויים
       function setUpdateButtonEnabled(enabled) {
           var btn = document.getElementById('<%= btnAddScreening.ClientID %>');
           if (!btn) return;

           btn.disabled = !enabled;
       }

       // מעדכן מצב הכפתור והודעת המשוב לפי שינויים שלא נשמרו
       function updateUnsavedState() {
           var lbl = document.getElementById('<%= lblMessage.ClientID %>');
           var schedule = document.querySelector('.weekSchedule');
           var hasChanges = schedule && hasUnsavedScheduleChanges();

           setUpdateButtonEnabled(hasChanges);

           // ניקוי הודעה קודמת (למשל "לא בוצעו שינויים") ברגע שמתחילים לערוך שוב
           if (lbl && hasChanges) {
               lbl.innerHTML = "";
           }
       }

       // מסמן/מסיר רקע בהיר לתא עם שינוי שלא נשמר
       function updateCellPendingState(checkbox) {
           var cell = checkbox.closest('td.schedule-day-cell');
           if (!cell) return;

           var initialChecked = cell.getAttribute('data-initial-checked') === 'true';
           var isChanged = checkbox.checked !== initialChecked;

           if (isChanged) {
               cell.classList.add('schedule-cell-pending');
           } else {
               cell.classList.remove('schedule-cell-pending');
           }
       }

       // מסנכרן מראה הצ'קבוקס, רקע התא ומצב הכפתור אחרי לחיצה
       function syncScheduleCheckboxUi(checkbox) {
           var wrapper = checkbox.closest('.checkbox-wrapper-33');
           if (wrapper) {
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

       // מאזין ללחיצות על הצ'קבוקסים (event delegation על כל הלוח)
       function bindScheduleEditorEvents() {
           var schedule = document.querySelector('.weekSchedule');
           // מונע רישום כפול של אותו מאזין אחרי postback
           if (!schedule || schedule.getAttribute('data-editor-bound') === '1') {
               return;
           }

           schedule.setAttribute('data-editor-bound', '1');
           schedule.addEventListener('click', function (e) {
               var wrapper = e.target.closest('.checkbox-wrapper-33');
               if (!wrapper || wrapper.classList.contains('checkbox-wrapper-33--disabled')) {
                   return;
               }

               var checkbox = wrapper.querySelector('input[type="checkbox"]');
               if (!checkbox || checkbox.disabled) {
                   return;
               }

               // setTimeout מבטיח ש-checkbox.checked כבר התעדכן לפני הסנכרון
               window.setTimeout(function () {
                   syncScheduleCheckboxUi(checkbox);
               }, 0);
           });
       }

       // אתחול אחרי טעינת הדף — מאזינים, רקע תאים, ומצב התחלתי של הכפתור
       function initScheduleEditorUi() {
           var ddl = document.getElementById('<%= ddlMovies.ClientID %>');
           if (ddl) {
               // שומר את הסרט הנוכחי לצורך ביטול מעבר אם המשתמש מבטל confirm
               ddl.setAttribute('data-selected-value', ddl.value);
           }

           bindScheduleEditorEvents();

           var cells = document.querySelectorAll('.weekSchedule td.schedule-day-cell[data-initial-checked]');
           for (var i = 0; i < cells.length; i++) {
               var cb = getScheduleCheckbox(cells[i]);
               if (cb) {
                   updateCellPendingState(cb);
               }
           }

           updateUnsavedState();
       }

       // לפני מעבר לסרט אחר — מאשר ביטול שינויים שלא נשמרו
       function onMovieSelectionChanging(select) {
           if (hasUnsavedScheduleChanges()) {
               var confirmed = confirm('יש שינויים שלא נשמרו בהקרנות. האם לבטל אותם ולעבור לסרט אחר?');
               if (!confirmed) {
                   // מחזיר את הרשימה הנפתחת לסרט שנבחר קודם
                   select.value = select.getAttribute('data-selected-value');
                   return false;
               }
           }

           select.setAttribute('data-selected-value', select.value);
           __doPostBack('<%= ddlMovies.UniqueID %>', '');
           return false;
       }

       // אתחול UI אחרי טעינת הדף (כולל postback)
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

 
        <asp:Panel ID="pnlSchedule" runat="server" Visible="false"></asp:Panel>

<div class="screenings-editor-actions">
<asp:Button CssClass="login-btn schedule-update-btn"
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
