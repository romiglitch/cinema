<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ResetPassword.aspx.cs" Inherits="Shipping.ResetPassword" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div dir="rtl" style="max-width: 420px; margin: 40px auto; text-align: right;">
        <h2 style="margin-bottom: 12px;">בחירת סיסמה חדשה</h2>

        <asp:Label ID="lblMessage" runat="server" CssClass="msg-label"></asp:Label>

        <table style="width: 100%; margin-top: 14px;">
            <tr>
                <td style="width: 55%;">
                    <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" CssClass="input-box" />
                    <br />
                    <asp:RequiredFieldValidator ID="RFVNewPassword" runat="server" ControlToValidate="txtNewPassword"
                        ErrorMessage="שדה חובה" CssClass="error-text-simple" Display="Dynamic"></asp:RequiredFieldValidator>
                </td>
                <td style="width: 45%; padding-right: 10px;">
                    <asp:Label ID="lblNewPassword" runat="server" Text="סיסמה חדשה"></asp:Label>
                </td>
            </tr>

            <tr>
                <td>
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
                <td style="padding-right: 10px;">
                    <asp:Label ID="lblConfirmPassword" runat="server" Text="אימות סיסמה"></asp:Label>
                </td>
            </tr>

            <tr>
                <td colspan="2" style="padding-top: 12px;">
                    <asp:Button ID="btnUpdate" runat="server" Text="עדכן סיסמה" OnClick="btnUpdate_Click" CssClass="signup-btn" />
                </td>
            </tr>
        </table>
    </div>
</asp:Content>
