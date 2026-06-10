// ResetPassword.js — אימות סיסמה חדשה
// cvNewPasswordId מוגדר בבלוק inline בדף ה-ASPX

// אותה מדיניות סיסמה כמו ב-Signup.aspx (10 תווים, 2 גדולות, 2 קטנות, 3 ספרות, תו מיוחד).
// נקרא מ-CustomValidator בעת שליחת הטופס
function validatePassword(sender, args) {
    var pass = args.Value;

    // שדה ריק — RequiredFieldValidator מטפל בזה
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

// נקרא ב-onkeyup — בודק את הסיסמה בזמן הקלדה
function validatePasswordRealTime() {
    var passValidator = document.getElementById(cvNewPasswordId);

    if (typeof (ValidatorValidate) == 'function') {
        // מפעיל רק את וולידטור הסיסמה, לא את כל הדף
        ValidatorValidate(passValidator);
    }
}
