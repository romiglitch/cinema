<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="AdminPage.aspx.cs" Inherits="Shipping.AdminPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
   
    <div>
        <h1>עמוד מנהל</h1>

        <div>
            <asp:Button CssClass="admin-buttons" ID="Btn1" runat="server" Text="מעבר לעריכת סרטים" OnClick="Btn1_Click"/>
            <asp:Button CssClass="admin-buttons" ID="Btn3" runat="server" Text="מעבר לעריכת הקרנות" OnClick="Btn2_Click"/>
            <asp:Button ID="btnGenerateSchedule" runat="server" Text="יצירת לוח הקרנות אוטומטי" 
                OnClick="btnGenerateSchedule_Click" CssClass="my-button" />
    <asp:Label ID="lblAdminStatus" runat="server"  Text=""></asp:Label>
    </div>
        </div>
</asp:Content>
