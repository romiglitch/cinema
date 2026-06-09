// Cart.js — עיצוב אוטומטי של שדה תוקף כרטיס אשראי (MM/YY)
function formatExpiry(input) {
    var value = input.value.replace(/\D/g, '');//מסיר את כל התווים שאינם מספרים

    if (value.length > 2) {
        input.value = value.substring(0, 2) + '/' + value.substring(2, 4);
    } else {
        input.value = value;
    }
}
