<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ScreeningsEditor.aspx.cs" Inherits="Shipping.ScreeningsEditor" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
   <script type="text/javascript">
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
               if (!cb || cb.disabled) continue;

               var initialChecked = cell.getAttribute('data-initial-checked') === 'true';
               if (cb.checked !== initialChecked) {
                   return true;
               }
           }
           return false;
       }

       // מציג/מסתיר את כפתור "עדכן הקרנות"
       function setUpdateButtonVisible(visible) {
           var btn = document.getElementById('<%= btnAddScreening.ClientID %>');
           if (!btn) return;

           if (visible) {
               btn.classList.add('is-visible');
           } else {
               btn.classList.remove('is-visible');
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

           var initialChecked = cell.getAttribute('data-initial-checked') === 'true';
           var isChanged = checkbox.checked !== initialChecked;

           if (isChanged) {
               cell.classList.add('schedule-cell-pending');
           } else {
               cell.classList.remove('schedule-cell-pending');
           }
       }

       // מסנכרן מראה הצ'קבוקס, רקע התא ומצב הכפתור
       function syncScheduleCheckboxUi(checkbox) {
           var wrapper = checkbox.closest('.checkbox-wrapper-33');
           if (wrapper) {
               var symbol = wrapper.querySelector('.checkbox__symbol');
               if (symbol) {
                   if (checkbox.checked) {
                       wrapper.classList.add('checkbox-wrapper-33--checked');
                       symbol.classList.add('checkbox__symbol--checked');
                   } else {
                       wrapper.classList.remove('checkbox-wrapper-33--checked');
                       symbol.classList.remove('checkbox__symbol--checked');
                   }
               }
           }

           updateCellPendingState(checkbox);
           updateUnsavedState();
       }

       // מאזין ללחיצות על הצ'קבוקסים (delegation) — setTimeout מבטיח שהמצב כבר התעדכן
       function bindScheduleEditorEvents() {
           var schedule = document.querySelector('.weekSchedule');
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

               window.setTimeout(function () {
                   syncScheduleCheckboxUi(checkbox);
               }, 0);
           });
       }

       // מסנכרן את מצב הרקע והכפתור לכל התאים אחרי טעינת הדף
       function initScheduleEditorUi() {
           var ddl = document.getElementById('<%= ddlMovies.ClientID %>');
           if (ddl) {
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
                   select.value = select.getAttribute('data-selected-value');
                   return false;
               }
           }

           select.setAttribute('data-selected-value', select.value);
           __doPostBack('<%= ddlMovies.UniqueID %>', '');
           return false;
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
