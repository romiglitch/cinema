<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ResetPassword.aspx.cs" Inherits="Shipping.ResetPassword" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
        <div>
    <h2>בחירת סיסמה חדשה</h2>
    <asp:Label ID="lblMessage" runat="server"></asp:Label>
    <br />
    
    <label>סיסמה חדשה:</label>
    <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password"></asp:TextBox>
    <br />
    
    <label>אימות סיסמה:</label>
    <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password"></asp:TextBox>
    <br />
    
    <asp:Button ID="btnUpdate" runat="server" Text="עדכן סיסמה" OnClick="btnUpdate_Click" />
</div>
</asp:Content>
