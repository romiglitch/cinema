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
    var cells = document.querySelectorAll('.weekSchedule td.schedule-day-cell[data-initial-checked]');//כל התאים שהם צ׳קבוקס
    for (var i = 0; i < cells.length; i++) {
        var cell = cells[i];
        var cb = getScheduleCheckbox(cell);//מציאת הצ׳קבוקס בתא
        // תאים חסומים (עבר / אין אולם פנוי) לא נספרים כשינוי
        if (!cb || cb.disabled) continue;

        var initialChecked = cell.getAttribute('data-initial-checked') === 'true';//בדיקה אם הצ׳קבוקס נבחר (קיימת הקרנה)
        if (cb.checked !== initialChecked) {
            return true;//יש שינויים
        }
    }
    return false;//אין שינויים
}

// מפעיל/מנטרל את כפתור "עדכן הקרנות" — הכפתור תמיד גלוי, רק לא לחיץ כשאין שינויים
function setUpdateButtonEnabled(enabled) {
    var btn = document.getElementById(btnAddScreeningId);
    if (!btn) return;

    btn.disabled = !enabled;//במידה ויש שינויים יופעל הכפתור
}

// מעדכן מצב הכפתור והודעת המשוב אחרי שינויים 
function updateEditorState() {
    var lbl = document.getElementById(lblMessageId);
    var schedule = document.querySelector('.weekSchedule');//מציאת הלוח
    var hasChanges = schedule && hasUnsavedScheduleChanges();

    setUpdateButtonEnabled(hasChanges);//כפתור יופעל אם יש שינויים

    // ניקוי הודעה קודמת (למשל "לא בוצעו שינויים") ברגע שמתחילים לערוך שוב
    if (lbl && hasChanges) {
        lbl.innerHTML = "";
    }
}

// מסמן/מסיר רקע בהיר לתא עם שינוי שלא נשמר
function updateCellPendingState(checkbox) {
    var cell = checkbox.closest('td.schedule-day-cell');//מציאת התא שבו נמצא הצ׳קבוקס
    if (!cell) return;

    var initialChecked = cell.getAttribute('data-initial-checked') === 'true';
    var isChanged = checkbox.checked !== initialChecked;

    if (isChanged) {
        cell.classList.add('schedule-cell-pending');//מוסיף רקע בהיר לתא
    } else {
        cell.classList.remove('schedule-cell-pending');//מסיר רקע בהיר לתא
    }
}

// מסנכרן מראה הצ'קבוקס, רקע התא ומצב הכפתור אחרי לחיצה
function syncScheduleCheckboxUi(checkbox) {
    var wrapper = checkbox.closest('.checkbox-wrapper-33');//מציאת הדיב שבו נמצא הצ׳קבוקס
    if (wrapper) {
        var symbol = wrapper.querySelector('.checkbox__symbol');//מציאת הסמל שבו נמצא הצ׳קבוקס
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

    updateCellPendingState(checkbox);//מעדכן את רקע התא
    updateEditorState();//מחיקת ההודעה הקודמת ועדכון הכפתור
}

// מאזין ללחיצות על הצ'קבוקסים (event delegation על כל הלוח)
function bindScheduleEditorEvents() {
    var schedule = document.querySelector('.weekSchedule');
    //כבר נקשר ללוח (OnClick) בדיקה אם המאזין 
    if (!schedule || schedule.getAttribute('data-editor-bound') === '1') {
        return;//אם אין לוח או שהוא כבר נקשר לאירועים יוצא מהפונקציה
    }

    schedule.addEventListener('click', function (e) {
        var wrapper = e.target.closest('.checkbox-wrapper-33');//מציאת הדיב שבו נמצא הצ׳קבוקס
        if (!wrapper || wrapper.classList.contains('checkbox-wrapper-33--disabled')) {//במידה ואין דיב או שהוא מושבת
            return;
        }

        var checkbox = wrapper.querySelector('input[type="checkbox"]');//מציאת הצ׳קבוקס בדיב
        if (!checkbox || checkbox.disabled) {
            return;
        }

        // מבטיח שהפעולה תרוץ אחרי שכל האירועים כבר רצו setTimeout
        window.setTimeout(function () {//לחכות עד שהצ׳קבוקס יופעל (0-לא לחכות)
            syncScheduleCheckboxUi(checkbox);//סנכרון הצ׳קבוקס
        }, 0);
    });
    schedule.setAttribute('data-editor-bound', '1');//מסמן שהלוח נקשר לאירועים
}

// אתחול אחרי טעינת הדף — מאזינים, רקע תאים, ומצב התחלתי של הכפתור
function initScheduleEditorUi() {
    var ddl = document.getElementById(ddlMoviesId);
    if (ddl) {
        // מגדיר את הסרט שנבחר לפני השינוי
        ddl.setAttribute('data-selected-value', ddl.value);
    }

    bindScheduleEditorEvents();//מקשר איבנטים לטבלה
    updateEditorState();//עדכון המצב השמור בשרת
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
    __doPostBack(ddlMoviesUniqueId, '');//עושה פוסטבק של הסרט החדש - בונה את הטבלה מחדש בהתאם לסרט
}

// אתחול UI אחרי טעינת הדף (כולל postback)
if (window.addEventListener) {
    window.addEventListener('load', initScheduleEditorUi);//אירוע שמופעל אחרי טעינת הדף
} else {
    window.attachEvent('onload', initScheduleEditorUi);
}
