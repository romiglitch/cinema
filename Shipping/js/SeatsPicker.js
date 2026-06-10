// SeatsPicker.js — לוגיקת בחירת מושבים
// maxSelect חייב להיות מוגדר בצד השרת (Session["TotalTickets"])
// maxSelect, seatsPerRow מוגדרים בבלוק inline בדף ה-ASPX
// maxSelect — כמות הכרטיסים הכוללת שהמשתמש בחר
// seatsPerRow — מספר המושבים שיש בכל שורה באולם הקולנוע (ViewState)

// מבנה פנימי של בחירות: [{val: "seatId|row|seat", row: "2", seat: 5, seatNum:5}]
var selected = [];

//לוקחת מחרוזת טקסט גולמית (כמו "101|5|12") והופכת אותה לאובייקט מסודר עם שמות.
function parseVal(v) {
    var p = v.split('|');
    return { seatId: p[0], row: parseInt(p[1], 10), seatNum: parseInt(p[2], 10), raw: v };
}

//מוודאת שהמושבים שנבחרו הם צמודים
function isContiguous(arr) {
    if (arr.length <= 1) return true;
    var nums = arr.map(x => x.seatNum).sort((a, b) => a - b); //ממיר את המושבים למספרי המושבים וממיינם מהקטן לגדול
    return (nums[nums.length - 1] - nums[0] + 1) === nums.length;
}

// מטרה: לסימולציה של השורה – מושב שנמכר או נבחר נחשב "תפוס" לבדיקת מושב בודד
function isSeatBlocked(rowNum, seatNum, selectedNums) {
    if (selectedNums.indexOf(seatNum) !== -1) return true;// אם המושב
    // מציאת אלמנט המושב ב-DOM לפי מספר שורה ומספר מושב (data-attributes מהשרת)
    var $seat = $('.seat[data-row="' + rowNum + '"][data-seat="' + seatNum + '"]');
    // תפוס = קיים בדף וגם (נמכר כבר / או נבחר כרגע על ידי המשתמש)
    return $seat.length > 0 && ($seat.hasClass('taken') || $seat.hasClass('selected'));
}

// סופר כמה מושבים פנויים נשארו באולם להקרנה (לא נמכרו ולא נבחרו בבחירה הנוכחית)
function countFreeSeatsInHall(allSelected) {
    var count = 0;
    $('.seat').each(function () {
        if ($(this).hasClass('taken')) return;
        var rowNum = parseInt($(this).data('row'), 10);
        var seatNum = parseInt($(this).data('seat'), 10);
        var picked = allSelected.some(function (s) { return s.row === rowNum && s.seatNum === seatNum; });
        if (picked) return;
        count++;
    });
    return count;
}

// מטרה: איסור על השארת בדיוק מושב ריק אחד בכל מקום בשורה
function hasOrphanSeatInRow(rowNum, selectedNums, allSelected) {
    // מונה כמה מושבים ריקים רצופים נפגשו לפני המושב התפוס הבא
    var emptyRun = 0;
    // סריקה משמאל לימין על כל מספרי המושבים בשורה (1..seatsPerRow)
    for (var s = 1; s <= seatsPerRow; s++) {
        if (isSeatBlocked(rowNum, s, selectedNums)) {
            // מושב תפוס אחרי בדיוק מושב ריק אחד = "יתום" באמצע השורה
            if (emptyRun === 1) {
                // אם המושב ה"יתום" הוא המושב הפנוי היחיד שנשאר בכל האולם — מאפשרים את הבחירה
                if (countFreeSeatsInHall(allSelected) === 1) return false;
                return true;
            }
            // אחרי תפוס – מאפסים את רצף הריקים
            emptyRun = 0;
        } else {
            // מושב ריק – מרחיבים את הרצף הנוכחי
            emptyRun++;
        }
    }
    //בדיקה אם המושב האחרון בשורה הוא ריק
    if (emptyRun === 1) {
        if (countFreeSeatsInHall(allSelected) === 1) return false;
        return true;
    }
    return false;
}

//(מעדכנת את מה שהמשתמש רואה ואת מה שהשרת יקבל (מה מושבים נותרו לבחירה
function updateHiddenAndDisplay() {
    // עדכון השדה החבוי
    var vals = selected.map(s => s.raw);
    $('#SelectedSeats').val(vals.join(','));
    // עדכון תצוגת נותרו
    $('#remaining').text(Math.max(0, maxSelect - selected.length));

    // חסימה/הפעלה של מושבים בהתאם למצב
    if (selected.length >= maxSelect) {
        $('.seat').not('.selected').addClass('disabled').prop('disabled', true);
    } else {
        $('.seat.disabled').removeClass('disabled').prop('disabled', false);
    }
}

//רצה בלחיצה על כל מושב : בדיקות
function toggleSeat(elem) {
    var $el = $(elem);
    if ($el.hasClass('taken') || $el.hasClass('disabled')) return; //אם המושב נבחר או חסום, לא נעשה שינוי

    var raw = $el.data('value'); // seatId|row|seat
    var info = parseVal(raw);//הצגה של המושב בפורמט "101|5|12" כך שניתן לעבוד עליו

    // מחפש אם המושב כבר נבחר ומוצא את המיקום שלו במערך selected
    var idx = selected.findIndex(x => x.raw === raw);
    if (idx !== -1) { // אם המושב נבחר ונמצא במערך selected
        selected.splice(idx, 1); // מוציא מושב אחד מהמערך עם האינדקס idx
        $el.removeClass('selected');//מסיר את המחלקה selected מהמושב
        updateHiddenAndDisplay();//מעדכן את המושבים שנבחרו ואת המושבים שנותרו לבחירה
        return;
    }

    // הוספה זמנית לבדיקה
    var temp = selected.slice(); // עותק זמני של המערך selected
    temp.push(info); // מוסיף את המושב שנבחר להתחלה של העותק הזמני

    // בדוק גבולות - לא יותר ממקסימום בשורה
    if (temp.length > maxSelect) {
        alert('לא ניתן לבחור יותר מ־' + maxSelect + ' מושבים.');
        return;
    }

    // בדוק רציפות ומושב יתום — רק כשבוחרים את המושב האחרון
    if (temp.length === maxSelect) {
        if (!isContiguous(temp)) {
            alert('המושבים חייבים להיות סמוכים.');
            return;
        }

        var seatNums = temp.map(x => x.seatNum);//מערך רק של מספרי המושבים
        if (hasOrphanSeatInRow(temp[0].row, seatNums, temp)) {
            alert('לא ניתן להשאיר מושב בודד ריק בשורה.');
            return;
        }
    }

    // אם עברנו את כל הבדיקות - הוסף
    selected.push(info); //מוסיף את המושב שנבחר למערך selected
    $el.addClass('selected');
    // בתוך פונקציית toggleSeat אחרי הוספת ה-Class
    $el.hide().fadeIn(200);

    updateHiddenAndDisplay();//מעדכן את נראות המושבים שנבחרו ואת המושבים שנותרו לבחירה
}

// פונקציה לאיסוף לפני שליחה (OnClientClick)
function collectSeats() {
    updateHiddenAndDisplay();
    var vals = $('#SelectedSeats').val().trim();
    if (!vals) {
        alert('אנא בחרי מושבים לפני שממשיכים.');
        return false;
    }
    var items = vals.split(',');
    if (items.length !== maxSelect) {
        alert('בחרת ' + items.length + ' מושבים; נדרשים בדיוק ' + maxSelect + '.');
        return false;
    }
    // אימות חוזר לפני מעבר לתשלום (אותן בדיקות כמו בלחיצה על מושב)
    var parsed = items.map(parseVal);
    if (!isContiguous(parsed)) { alert('המושבים חייבים להיות צמודים.'); return false; }
    var seatNums = parsed.map(x => x.seatNum);
    if (hasOrphanSeatInRow(parsed[0].row, seatNums, parsed)) {
        alert('לא ניתן להשאיר מושב בודד ריק בשורה.');
        return false;
    }
    return true;
}

//אתחול של הדף בעת טעינתו
$(document).ready(function () {
    // לחיצה על מושב => toggleSeat
    $(document).on('click', '.seat', function () {
        toggleSeat(this);
    });

    // אם הדף נבנה בצד השרת ויש כבר בחירות (למקרה של postback), ניתן לאתחל מתוך #SelectedSeats
    var existing = $('#SelectedSeats').val();
    if (existing && existing.trim().length > 0) {
        existing.split(',').forEach(function (v) {
            var info = parseVal(v);
            selected.push(info);//בונה את המערך selected עם המושבים שנבחרו
            // סמן ב־DOM את האלמנט המתאים אם קיים
            $('.seat').filter(function () { return $(this).data('value') === v; }).addClass('selected');//עבור כל מושב במערך selected מתווספת המחלקהselected
        });
        updateHiddenAndDisplay();
    }
});
