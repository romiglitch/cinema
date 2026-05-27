<%@ Page Title="" Async="true" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ForgotPassword.aspx.cs" Inherits="Shipping.ForgotPassword1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server" >

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="forgot-password-container" dir="rtl">
        <div class="recovery-card">
            <h1>שחזור סיסמה</h1>
            <p>הכנס את כתובת האימייל איתה נרשמת:</p>
            
            <%-- תיבת קלט --%>
            <asp:TextBox ID="txtEmail" runat="server" CssClass="recovery-input" TextMode="Email" placeholder="email@example.com"></asp:TextBox>
            
            <%-- כפתור מעוצב וממורכז --%>
            <div style="margin-top: 10px;">
                <asp:Button Width="100%" ID="btnSend" runat="server" Text="שלח לי מייל לשחזור" OnClick="btnSend_Click" CssClass="signup-btn" />
            </div>
            
            <%-- סטטוס --%>
            <div style="margin-top: 15px;">
                <asp:Label ID="lblStatus" runat="server" Text="" CssClass="status-message"></asp:Label>
            </div>
        </div>
    </div>
</asp:Content>