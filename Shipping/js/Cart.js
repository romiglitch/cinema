// Cart.js — עיצוב אוטומטי של שדה תוקף כרטיס אשראי (MM/YY)
function formatExpiry(input) {
    // מסיר כל תו שאינו ספרה
    var value = input.value.replace(/\D/g, '');

    // אם הוקלדו יותר מ-2 ספרות, מוסיף את האלכסון
    if (value.length > 2) {
        input.value = value.substring(0, 2) + '/' + value.substring(2, 4);
    } else {
        input.value = value;
    }
}
