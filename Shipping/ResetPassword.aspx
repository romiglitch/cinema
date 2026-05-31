<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ResetPassword.aspx.cs" Inherits="Shipping.ResetPassword" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <%-- קונטיינר חיצוני שממרכז את הכרטיס בדיוק באמצע הדף --%>
    <div class="forgot-password-container" dir="rtl">
        
        <%-- כרטיס זכוכית כהה מעוצב --%>
        <div class="recovery-card" style="text-align: right;">
            <h1 style="margin-bottom: 12px; text-align: center;">בחירת סיסמה חדשה</h1>

            <table style="width: 100%; margin-top: 14px;">
                <tr>
                    <td style="width: 45%; padding-right: 10px;">
                        <asp:Label ID="lblNewPassword" runat="server" Text="סיסמה חדשה"></asp:Label>
                    </td>
                    <td style="width: 55%;">
                        <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" CssClass="input-box" onkeyup="validatePasswordRealTime()" />
                        <br />
                        <asp:RequiredFieldValidator ID="RFVNewPassword" runat="server" ControlToValidate="txtNewPassword"
                            ErrorMessage="שדה חובה" CssClass="error-text-simple" Display="Dynamic"></asp:RequiredFieldValidator>
                        
                        <asp:CustomValidator ID="CVNewPassword" runat="server"
                            ControlToValidate="txtNewPassword"
                            ClientValidationFunction="validatePassword"
                            ErrorMessage=".הסיסמה חייבת להכיל 10 תווים, 2 אותיות גדולות, 2 קטנות, 3 מספרים ותו מיוחד"
                            CssClass="error-text-simple"
                            Display="Dynamic"
                            ValidateEmptyText="false"></asp:CustomValidator>
                    </td>
                </tr>

                <tr>
                    <td style="padding-right: 10px;">
                        <asp:Label ID="lblConfirmPassword" runat="server" Text="אימות סיסמה"></asp:Label>
                    </td>
                    <td >
                        <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="input-box" />
                        <br />
                        <asp:RequiredFieldValidator ID="RFVConfirmPassword" runat="server" ControlToValidate="txtConfirmPassword"
                            ErrorMessage="שדה חובה" CssClass="error-text-simple" Display="Dynamic"></asp:RequiredFieldValidator>
                        
                        <asp:CompareValidator ID="CVConfirmPassword" runat="server"
                            ControlToValidate="txtConfirmPassword"
                            ControlToCompare="txtNewPassword"
                            Operator="Equal"
                            Type="String"
                            ErrorMessage="אימות הסיסמה לא תואם לסיסמה החדשה"
                            CssClass="error-text-simple"
                            Display="Dynamic"></asp:CompareValidator>
                    </td>
                </tr>

                <tr>
                    <td colspan="2" style="padding-top: 12px; border: none">
                        <asp:Button ID="btnUpdate" runat="server" Text="עדכן סיסמה" OnClick="btnUpdate_Click" CssClass="signup-btn" Style="width: 100%;" />
                    </td>
                </tr>
                <tr>
                    <td colspan="2" style="text-align: center; padding-top: 15px; border: none">
                        <asp:Label ID="lblMessage" runat="server"></asp:Label>
                    </td>
                </tr>
            </table>
        </div>
    </div>

    <script>
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
            var passValidator = document.getElementById('<%= CVNewPassword.ClientID %>');

            if (typeof (ValidatorValidate) == 'function') {
                // מפעיל רק את וולידטור הסיסמה, לא את כל הדף
                ValidatorValidate(passValidator);
            }
        }
    </script>
</asp:Content>