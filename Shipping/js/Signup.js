// Signup.js — יצירת סיסמה חזקה ואימות סיסמה בזמן אמת
// txtPasswordId, cvPasswordId מוגדרים בבלוק inline בדף ה-ASPX

function generateStrongPassword() {
    const uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const lowers = "abcdefghijklmnopqrstuvwxyz";
    const digits = "0123456789";
    const specials = "!@#$%^&*()_+";

    let pass = "";
    // הבטחת הדרישות המינימליות
    for (let i = 0; i < 2; i++) pass += uppers[Math.floor(Math.random() * uppers.length)];
    for (let i = 0; i < 2; i++) pass += lowers[Math.floor(Math.random() * lowers.length)];
    for (let i = 0; i < 3; i++) pass += digits[Math.floor(Math.random() * digits.length)];
    pass += specials[Math.floor(Math.random() * specials.length)];

    // השלמה ל-10 תווים (עוד 2 אקראיים)
    const all = uppers + lowers + digits + specials;
    for (let i = 0; i < 2; i++) pass += all[Math.floor(Math.random() * all.length)];

    // ערבוב התווים
    pass = pass.split('').sort(() => 0.5 - Math.random()).join('');

    const txtPass = document.getElementById(txtPasswordId);
    txtPass.value = pass;
    txtPass.type = "text"; // מציג את הסיסמה כדי שיראו מה הוצע
    alert("הוצעה סיסמה חזקה: " + pass);
}

// מדיניות סיסמה: 10 תווים, 2 גדולות, 2 קטנות, 3 ספרות, תו מיוחד אחד
function validatePassword(sender, args) {
    var pass = args.Value;

    // שדה ריק – לא מציגים שגיאת מורכבות (יש RequiredFieldValidator)
    if (pass === "") {
        args.IsValid = true;
        return;
    }

    var hasMinLength = pass.length >= 10;
    var hasUpper = (pass.match(/[A-Z]/g) || []).length >= 2;
    var hasLower = (pass.match(/[a-z]/g) || []).length >= 2;
    var hasDigits = (pass.match(/[0-9]/g) || []).length >= 3;
    var hasSpecial = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(pass);

    args.IsValid = hasMinLength && hasUpper && hasLower && hasDigits && hasSpecial;
}

function validatePasswordRealTime() {
    // במקום לבדוק את כל הדף, אנחנו מוצאים רק את הוולידטור של הסיסמה
    var passValidator = document.getElementById(cvPasswordId);

    if (typeof (ValidatorValidate) == 'function') {
        // מפעיל בדיקה רק על פקד האימות הספציפי הזה
        ValidatorValidate(passValidator);
    }
}
