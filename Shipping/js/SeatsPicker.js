// SeatsPicker.js — לוגיקת בחירת מושבים
// maxSelect, seatsPerRow מוגדרים בבלוק inline בדף ה-ASPX

// מבנה פנימי של בחירות: [{val: "seatId|row|seat", row: "2", seat: 5, seatNum:5}]
var selected = [];

function parseVal(v) {
    var p = v.split('|');
    return { seatId: p[0], row: parseInt(p[1], 10), seatNum: parseInt(p[2], 10), raw: v };
}

function isContiguous(arr) {
    if (arr.length <= 1) return true;
    var nums = arr.map(x => x.seatNum).sort((a, b) => a - b);
    return (nums[nums.length - 1] - nums[0] + 1) === nums.length;
}

function sameRow(arr) {
    if (arr.length === 0) return true;
    var r = arr[0].row;
    return arr.every(x => x.row === r);
}

function isSeatBlocked(rowNum, seatNum, selectedNums) {
    if (selectedNums.indexOf(seatNum) !== -1) return true;
    var $seat = $('.seat[data-row="' + rowNum + '"][data-seat="' + seatNum + '"]');
    return $seat.length > 0 && ($seat.hasClass('taken') || $seat.hasClass('selected'));
}

function hasOrphanSeatInRow(rowNum, selectedNums) {
    var emptyRun = 0;
    for (var s = 1; s <= seatsPerRow; s++) {
        if (isSeatBlocked(rowNum, s, selectedNums)) {
            if (emptyRun === 1) return true;
            emptyRun = 0;
        } else {
            emptyRun++;
        }
    }
    return emptyRun === 1;
}

function updateHiddenAndDisplay() {
    var vals = selected.map(s => s.raw);
    $('#SelectedSeats').val(vals.join(','));
    $('#remaining').text(Math.max(0, maxSelect - selected.length));

    if (selected.length >= maxSelect) {
        $('.seat').not('.selected').addClass('disabled').prop('disabled', true);
    } else {
        $('.seat.disabled').removeClass('disabled').prop('disabled', false);
    }
}

function toggleSeat(elem) {
    var $el = $(elem);
    if ($el.hasClass('taken') || $el.hasClass('disabled')) return;

    var raw = $el.data('value');
    var info = parseVal(raw);

    var idx = selected.findIndex(x => x.raw === raw);
    if (idx !== -1) {
        selected.splice(idx, 1);
        $el.removeClass('selected');
        updateHiddenAndDisplay();
        return;
    }

    if (selected.length > 0 && !sameRow([selected[0], info])) {
        alert('יש לבחור את כל המושבים באותה שורה בלבד.');
        return;
    }

    var temp = selected.slice();
    temp.push(info);

    if (temp.length > maxSelect) {
        alert('לא ניתן לבחור יותר מ־' + maxSelect + ' מושבים.');
        return;
    }

    if (!isContiguous(temp)) {
        alert('המושבים חייבים להיות סמוכים.');
        return;
    }

    var seatNums = temp.map(x => x.seatNum);
    if (hasOrphanSeatInRow(temp[0].row, seatNums)) {
        alert('לא ניתן להשאיר מושב בודד ריק בשורה.');
        return;
    }

    selected.push(info);
    $el.addClass('selected');
    $el.hide().fadeIn(200);

    updateHiddenAndDisplay();
}

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
    var parsed = items.map(parseVal);
    if (!sameRow(parsed)) { alert('המושבים חייבים להיות באותה שורה.'); return false; }
    if (!isContiguous(parsed)) { alert('המושבים חייבים להיות צמודים.'); return false; }
    var seatNums = parsed.map(x => x.seatNum);
    if (hasOrphanSeatInRow(parsed[0].row, seatNums)) {
        alert('לא ניתן להשאיר מושב בודד ריק בשורה.');
        return false;
    }
    return true;
}

$(document).ready(function () {
    $(document).on('click', '.seat', function () {
        toggleSeat(this);
    });

    var existing = $('#SelectedSeats').val();
    if (existing && existing.trim().length > 0) {
        existing.split(',').forEach(function (v) {
            var info = parseVal(v);
            selected.push(info);
            $('.seat').filter(function () { return $(this).data('value') === v; }).addClass('selected');
        });
        updateHiddenAndDisplay();
    }
});
