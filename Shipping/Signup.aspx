<%@ Page Title="Sign Up" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Signup.aspx.cs" Inherits="Shipping.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="signup-container">
        <h1>הרשמה</h1>

        <div class="signup-form">
            <table>
                <tr>
                    <td>
                        <asp:TextBox dir="rtl" ID="TxtName" runat="server" CssClass="input-box" placeholder="הכנס שם מלא"></asp:TextBox>
                        <br />
                        <asp:RequiredFieldValidator ID="RFVName" runat="server" ControlToValidate="TxtName"
                            ErrorMessage="שדה חובה" CssClass="error-text-simple" Display="Dynamic"></asp:RequiredFieldValidator>
                           <%-- שם מלא: לפחות שתי מילים, אותיות עברית/לטינית בלבד (ללא ספרות/סימנים) --%>
                        <asp:RegularExpressionValidator ID="REVName" runat="server" ControlToValidate="TxtName"
                            ErrorMessage="שם מלא: לפחות שתי מילים, אותיות בלבד (ללא מספרים או סימנים)" CssClass="error-text-simple" Display="Dynamic"
                            ValidationExpression="^[\u05D0-\u05EAa-zA-Z]+(?:\s+[\u05D0-\u05EAa-zA-Z]+)+$"></asp:RegularExpressionValidator>
                    </td>
                    <td>
                        <asp:Label ID="LblName" runat="server" Text="שם מלא"></asp:Label>
                    </td>
                </tr>

                <tr>
                    <td>
                        <asp:TextBox dir="rtl" ID="TxtEmail" runat="server" CssClass="input-box" TextMode="Email" placeholder="הכנס כתובת דואל"></asp:TextBox>
                        <br />
                        <%-- אימייל = שם משתמש; חובה + פורמט תקין --%>
                        <asp:RequiredFieldValidator ID="RFVEmail" runat="server" ControlToValidate="TxtEmail"
                            ErrorMessage="שדה חובה" CssClass="error-text-simple" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:RegularExpressionValidator ID="REVEmail" runat="server" ControlToValidate="TxtEmail"
                            ErrorMessage="כתובת אימייל לא תקינה" CssClass="error-text-simple" Display="Dynamic"
                            ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"></asp:RegularExpressionValidator>
                    </td>
                    <td>
                        <asp:Label ID="lblEmail" runat="server" Text="דואל"></asp:Label>
                    </td>
                </tr>

                <tr>
                    <td>
                        <asp:TextBox dir="rtl" ID="TxtPassword" runat="server" CssClass="input-box" TextMode="Password" placeholder="הכנס סיסמא" onkeyup="validatePasswordRealTime()"></asp:TextBox>
                       <br />
                        <asp:RequiredFieldValidator ID="RFVPassword" runat="server" ControlToValidate="TxtPassword"
                            ErrorMessage="שדה חובה" CssClass="error-text-simple" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:CustomValidator ID="CVPassword" runat="server"
                            ControlToValidate="TxtPassword"
                            ClientValidationFunction="validatePassword"
                            ErrorMessage=".הסיסמה חייבת להכיל 10 תווים, 2 אותיות גדולות, 2 קטנות, 3 מספרים ותו מיוחד"
                            CssClass="error-text-simple"
                            Display="Dynamic"
                            ValidateEmptyText="false"></asp:CustomValidator>
                       
                       

                        <%-- הלייבל הזה יציג את פירוט חוקי הסיסמה אם הם לא יתקיימו --%>
                        <asp:Label ID="lblPassError" runat="server" CssClass="error-text-simple" Display="Dynamic" Style="font-size: 0.8rem;"></asp:Label>
                    </td>
                    <td>
                        <asp:Label ID="LblPassword" runat="server" Text="סיסמא"></asp:Label>
                    </td>
                </tr>

                <tr>
                    <td>
                        <asp:TextBox dir="rtl" ID="TxtPhone" runat="server" CssClass="input-box" placeholder="הכנס מספר טלפון"></asp:TextBox>
                        <br />
                        <asp:RequiredFieldValidator ID="RFVPhone" runat="server" ControlToValidate="TxtPhone"
                            ErrorMessage="שדה חובה" CssClass="error-text-simple" Display="Dynamic"></asp:RequiredFieldValidator>
                        <%-- טלפון: מתחיל ב-0, ספרות בלבד, באורך 10 תווים --%>
                        <asp:RegularExpressionValidator ID="REVPhone" runat="server" ControlToValidate="TxtPhone"
                            ErrorMessage="מספר טלפון חייב להתחיל ב-0, לכלול 10 ספרות בלבד" CssClass="error-text-simple" Display="Dynamic"
                            ValidationExpression="^0\d{9}$"></asp:RegularExpressionValidator>
                    </td>
                    <td>
                        <asp:Label ID="LblPhone" runat="server" Text="טלפון"></asp:Label></td>
                </tr>

                <tr>
                    <td colspan="2">
                        <asp:Button ID="btnSign" runat="server" Text="צור חשבון" CssClass="signup-btn" OnClick="BtnSign_Click" />
                    </td>
                </tr>

                <tr class="no-border-row">
                    <td colspan="2">
                        <asp:Label ID="msg" runat="server" CssClass="msg-label" Text=""></asp:Label>
                    </td>
                </tr>
            </table>
        </div>
    </div>
    <script>
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

            const txtPass = document.getElementById('<%= TxtPassword.ClientID %>');
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
            var passValidator = document.getElementById('<%= CVPassword.ClientID %>');

     if (typeof (ValidatorValidate) == 'function') {
         // מפעיל בדיקה רק על פקד האימות הספציפי הזה
         ValidatorValidate(passValidator);
     }
 }
    </script>
</asp:Content>
