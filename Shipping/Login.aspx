<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="Shipping.Login1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
   
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
       <div class="login-container">
        <h1>התחברות</h1>

        <div class="login-form">
            <table>
                <tr>
                    <td>
                        <asp:TextBox dir="rtl" ID="TxtEmail" runat="server" CssClass="input-box" TextMode="Email" placeholder="הכנס את כתובת האימייל שלך"></asp:TextBox>
                        <br />
                        <asp:RequiredFieldValidator ID="RFVEmail" runat="server" ControlToValidate="TxtEmail"
                            ErrorMessage="שדה חובה" CssClass="error-text-simple" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:RegularExpressionValidator ID="REVEmail" runat="server" ControlToValidate="TxtEmail"
                            ErrorMessage="כתובת אימייל לא תקינה" CssClass="error-text-simple" Display="Dynamic"
                            ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"></asp:RegularExpressionValidator>
                    </td>
                    <td>
                        <asp:Label ID="LblEmail" runat="server" Text="אימייל"></asp:Label>
                    </td>
                </tr>

                <tr>
                    <td><%-- כפתור השכחתי סיסמא יופיע אחרי שנכתב תו אחד onkeyup--%>
                        <asp:TextBox dir="rtl" ID="TxtPassword" runat="server" CssClass="input-box" TextMode="Password" placeholder="הכנס את סיסמתך" onkeyup="showForgotButton()"></asp:TextBox>
                    </td>
                    <td>
                        <asp:Label ID="LblPassword" runat="server" Text="סיסמא"></asp:Label>
                    </td>
                </tr>

                <tr>
                    <td colspan="2">
                        <asp:Button ID="btnLogin" runat="server" Text="להתחבר" CssClass="login-btn" OnClick="BtnLogin_Click" />
                    </td>
                </tr>

                <tr class="no-border-row">
                    <td colspan="2" style="text-align: center; padding-top: 10px;">
                       <asp:LinkButton 
    ID="btnForgotPassword" 
    runat="server" 
    CssClass="forgot-link" 
    OnClick="BtnForgotPassword_Click" 
    Text="?שכחת סיסמא">
</asp:LinkButton>
                    </td>
                </tr>
<tr class="no-border-row">
    <td colspan="2" style="padding: 0;">
        <div>
             <asp:Label ID="LblMsg" runat="server" CssClass="msg-label" Text=""></asp:Label>
        </div>
    </td>
</tr>
            </table>
        </div>
    </div>
</asp:Content>

