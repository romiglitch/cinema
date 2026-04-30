<%@ Page Title="" Async="true" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ForgotPassword.aspx.cs" Inherits="Shipping.ForgotPassword1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server" >
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
     <div>
    <h2>שחזור סיסמה</h2>
    <p>הכנס את כתובת האימייל איתה נרשמת:</p>
         <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" placeholder="email@example.com"></asp:TextBox>
   
    <br />
    
    <asp:Button ID="btnSend" runat="server" Text="שלח לי מייל לשחזור" OnClick="btnSend_Click" />
    <br />
    
    <asp:Label ID="lblStatus" runat="server" Text=""></asp:Label>
</div>
</asp:Content>
