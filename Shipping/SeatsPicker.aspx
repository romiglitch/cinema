<%@ Page Title="בחירת מושבים" Language="C#" MasterPageFile="~/Master.Master"
    AutoEventWireup="true" CodeBehind="SeatsPicker.aspx.cs" Inherits="Shipping.SeatsPicker" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Noto+Sans+JP:wght@300;400;500;700&display=swap" rel="stylesheet">
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script type="text/javascript">

    // maxSelect חייב להיות מוגדר בצד השרת (Session["TotalTickets"])
    var maxSelect = <%= (Session["TotalTickets"] ?? 0) %>;//כמות הכרטיסים הכוללת שהמשתמש בחר
    // seatsPerRow אם צריך - אופציונלי, רק בשביל בדיקה (ממולא ב-ViewState או ב-data-attr)
    var seatsPerRow = <%= (ViewState["SeatsPerRow"] ?? 0) %>;//מספר המושבים שיש בכל שורה באולם הקולנוע

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
        var nums = arr.map(x => x.seatNum).sort((a, b) => a - b);
        return (nums[nums.length - 1] - nums[0] + 1) === nums.length;
    }

    //בודקת שכל המושבים שנבחרו נמצאים באותה שורה
    function sameRow(arr) {
        if (arr.length === 0) return true;
        var r = arr[0].row;
        return arr.every(x => x.row === r);
    }

    // מושב חסום = כבר נמכר (taken) או נבחר כעת על ידי המשתמש
    function isSeatBlocked(rowNum, seatNum, selectedNums) {
        if (selectedNums.indexOf(seatNum) !== -1) return true;
        var $seat = $('.seat[data-row="' + rowNum + '"][data-seat="' + seatNum + '"]');
        return $seat.length > 0 && ($seat.hasClass('taken') || $seat.hasClass('selected'));
    }

    // כלל קולנוע: אסור להשאיר מושב בודד ריק בשורה (בקצה או באמצע בין מושבים חסומים)
    // דוגמה: מושב 3 תפוס + בחירת מושב 1 → מושב 2 נשאר יתום → נחסם
    function hasOrphanSeatInRow(rowNum, selectedNums) {
        var emptyRun = 0; // אורך רצף מושבים ריקים רצופים
        for (var s = 1; s <= seatsPerRow; s++) {
            if (isSeatBlocked(rowNum, s, selectedNums)) {
                if (emptyRun === 1) return true; // יתום לפני בלוק חסום
                emptyRun = 0;
            } else {
                emptyRun++;
            }
        }
        return emptyRun === 1; // יתום בסוף השורה
    }

    //(מעדכנת את מה שהמשתמש רואה ואת מה שהשרת יקבל (מה מושבים נותרו לבחירה
    function updateHiddenAndDisplay() {
        // עדכון השדה החבוי
        var vals = selected.map(s => s.raw);
        $('#SelectedSeats').val(vals.join(','));
        // עדכון תצוגת נותרו
        $('#remaining').text(Math.max(0, maxSelect - selected.length));
        // עדכון כיתוב נבחרים (אופציונלי)
        if (selected.length > 0) {
            var list = selected.map(s => 'ש' + s.row + '-' + s.seatNum).join(', ');
            $('#selectedDisplay').text('בחרת: ' + list);
        } else {
            $('#selectedDisplay').text('');
        }
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
        if ($el.hasClass('taken') || $el.hasClass('disabled')) return;

        var raw = $el.data('value'); // seatId|row|seat
        var info = parseVal(raw);

        // אם כבר בס elected -> הוצא
        var idx = selected.findIndex(x => x.raw === raw);
        if (idx !== -1) {
            selected.splice(idx, 1);
            $el.removeClass('selected');
            updateHiddenAndDisplay();
            return;
        }

        // צרף: חייב להיות באותה שורה
        if (selected.length > 0 && !sameRow([selected[0], info])) {
            alert('יש לבחור את כל המושבים באותה שורה בלבד.');
            return;
        }

        // הוספה זמנית לבדיקה
        var temp = selected.slice();
        temp.push(info);

        // בדוק גבולות - לא יותר ממקסימום בשורה
        if (temp.length > maxSelect) {
            alert('לא ניתן לבחור יותר מ־' + maxSelect + ' מושבים.');
            return;
        }

        // בדוק רציפות
        if (!isContiguous(temp)) {
            alert('המושבים חייבים להיות סמוכים.');
            return;
        }

        // בדיקת מושב יתום (לפני הוספה לבחירה)
        var seatNums = temp.map(x => x.seatNum);
        if (hasOrphanSeatInRow(temp[0].row, seatNums)) {
            alert('לא ניתן להשאיר מושב בודד ריק בשורה.');
            return;
        }

        // אם עברנו את כל הבדיקות - הוסף
        selected.push(info);
        $el.addClass('selected');
        // בתוך פונקציית toggleSeat אחרי הוספת ה-Class
        $el.hide().fadeIn(200);

        updateHiddenAndDisplay();
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
        // לחיצה על מושב => toggleSeat
        $(document).on('click', '.seat', function () {
            toggleSeat(this);
        });

        // אם הדף נבנה בצד השרת ויש כבר בחירות (למקרה של postback), ניתן לאתחל מתוך #SelectedSeats
        var existing = $('#SelectedSeats').val();
        if (existing && existing.trim().length > 0) {
            existing.split(',').forEach(function (v) {
                var info = parseVal(v);
                selected.push(info);
                // סמן ב־DOM את האלמנט המתאים אם קיים
                $('.seat').filter(function () { return $(this).data('value') === v; }).addClass('selected');
            });
            updateHiddenAndDisplay();
        }
    });
</script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <h1>בחירת מושבים</h1>

    <div class="screen">מסך</div>

    <div class="seats-container">
        <asp:Repeater ID="RepeaterRows" runat="server" OnItemDataBound="RepeaterRows_ItemDataBound">
            <ItemTemplate>
                <div class="seat-row">
                    <div class="row-label">שורה <%# Eval("RowNumber") %></div>
                    <div class="row-seats" id="rowSeatsContainer" runat="server"></div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>

   <div class="legend">
    <div class="legend-item">פנוי <span class="legend-box available"></span></div>
    <div class="legend-item">נבחר<span class="legend-box selected"></span></div>
    <div class="legend-item">תפוס<span class="legend-box taken"></span></div>
    <div class="legend-item">נגיש<span class="legend-box accessible"></span></div>
</div>

    <input type="hidden" id="SelectedSeats" name="SelectedSeats"/>

    <div style="margin-top: 25px;">
        <asp:Button ID="btnConfirm" runat="server" Text="אישור מושבים"
            CssClass="btn-continue"
            OnClick="btnConfirm_Click"
            OnClientClick="return collectSeats();" />
        <span style="margin-right:15px;">נותרו לבחור:
            <span id="remaining"><%= (Session["TotalTickets"] ?? 0).ToString() %></span>
        </span>
    </div>

</asp:Content>
