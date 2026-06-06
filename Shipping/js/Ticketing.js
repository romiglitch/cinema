// Ticketing.js — בחירת כמות כרטיסים ואימות זכאות

$(document).ready(function () {
    function updateTotals() {
        let total = 0;
        $(".ticket-row").each(function () {
            let row = $(this);
            let qty = parseInt(row.find(".qty-display").val()) || 0;
            let price = parseFloat(row.find("input[id*='hiddenPrice']").val()) || 0;
            let type = row.find(".verification-box").data("type");

            total += (qty * price);

            // עדכון ה-HiddenField כדי שה-C# יוכל לקרוא את הכמות
            row.find("input[type='hidden'][id*='hiddenQty']").val(qty);

            let vBox = row.find(".verification-box");
            if (qty > 0 && type !== "רגיל") {
                vBox.slideDown(200);
            } else {
                vBox.slideUp(200);
                vBox.find(".id-input").val("");
            }
        });
        $("#grand-total").text("₪" + total.toFixed(2));
    }

    $(document).on("click", ".btn-plus", function (e) {
        e.stopImmediatePropagation();
        let input = $(this).siblings(".qty-display");
        input.val(parseInt(input.val()) + 1);
        updateTotals();
    });

    $(document).on("click", ".btn-minus", function (e) {
        e.stopImmediatePropagation();
        let input = $(this).siblings(".qty-display");
        let val = parseInt(input.val());
        if (val > 0) {
            input.val(val - 1);
            updateTotals();
        }
    });

    updateTotals();
});

function validateVerification() {
    let isValid = true;
    let totalTicketsSelected = 0;

    $(".error-text-simple").hide().text("");
    $(".id-input").css("border-color", "#ccc");

    // בדיקה שנבחר לפחות כרטיס אחד
    $(".qty-display").each(function () {
        totalTicketsSelected += parseInt($(this).val()) || 0;
    });

    if (totalTicketsSelected === 0) {
        alert("אנא בחרי לפחות כרטיס אחד כדי להמשיך.");
        return false;
    }

    // בדיקת תעודת זהות בשדות הגלויים
    $(".verification-box:visible").each(function () {
        let container = $(this);
        let input = container.find(".id-input");
        let errorDiv = container.find(".error-text-simple");
        let idValue = input.val().trim();
        let idPattern = /^\d{8,9}$/;

        if (idValue === "") {
            isValid = false;
            input.css("border-color", "red");
            errorDiv.text("חובה להזין מספר מזהה").fadeIn();
        }
        else if (!idPattern.test(idValue)) {
            isValid = false;
            input.css("border-color", "red");
            errorDiv.text("מספר זהות לא תקין (8-9 ספרות)").fadeIn();
        }
    });

    return isValid;
}

// העלמת השגיאה כשהמשתמש מקליד
$(document).on("input", ".id-input", function () {
    $(this).css("border-color", "#ccc");
    $(this).siblings(".error-text-simple").fadeOut();
});
