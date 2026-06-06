// ResetPassword.js — אימות סיסמה חדשה
// cvNewPasswordId מוגדר בבלוק inline בדף ה-ASPX

// אותה מדיניות סיסמה כמו ב-Signup (10 תווים, 2 גדולות, 2 קטנות, 3 ספרות, תו מיוחד)
function validatePassword(sender, args) {
    var pass = args.Value;

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
    var passValidator = document.getElementById(cvNewPasswordId);

    if (typeof (ValidatorValidate) == 'function') {
        ValidatorValidate(passValidator);
    }
}
