// ScreeningsEditor.js — לוגיקת הצד-לקוח של עורך ההקרנות
// משתנים גלובליים (btnAddScreeningId, lblMessageId, ddlMoviesId, ddlMoviesUniqueId)
// מוגדרים בבלוק inline בדף ה-ASPX כי הם תלויים בשרת

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
    var btn = document.getElementById(btnAddScreeningId);
    if (!btn) return;

    btn.disabled = !enabled;
}

// מעדכן מצב הכפתור והודעת המשוב אחרי שינויים
function updateEditorState() {
    var lbl = document.getElementById(lblMessageId);
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
                wrapper.classList.add('checkbox-wrapper-33--checked');
                symbol.classList.add('checkbox__symbol--checked');
            } else {
                wrapper.classList.remove('checkbox-wrapper-33--checked');
                symbol.classList.remove('checkbox__symbol--checked');
            }
        }
    }

    updateCellPendingState(checkbox);
    updateEditorState();
}

// מאזין ללחיצות על הצ'קבוקסים (event delegation על כל הלוח)
function bindScheduleEditorEvents() {
    var schedule = document.querySelector('.weekSchedule');
    if (!schedule || schedule.getAttribute('data-editor-bound') === '1') {
        return;
    }

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
    schedule.setAttribute('data-editor-bound', '1');
}

// אתחול אחרי טעינת הדף — מאזינים, רקע תאים, ומצב התחלתי של הכפתור
function initScheduleEditorUi() {
    var ddl = document.getElementById(ddlMoviesId);
    if (ddl) {
        ddl.setAttribute('data-selected-value', ddl.value);
    }

    bindScheduleEditorEvents();
    updateEditorState();
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
    __doPostBack(ddlMoviesUniqueId, '');
}

// אתחול UI אחרי טעינת הדף (כולל postback)
if (window.addEventListener) {
    window.addEventListener('load', initScheduleEditorUi);
} else {
    window.attachEvent('onload', initScheduleEditorUi);
}
