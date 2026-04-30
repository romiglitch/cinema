<%@ Page Title="הזמנה הושלמה" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Success.aspx.cs" Inherits="Shipping.Success" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;700&family=Rubik:wght@400;500&display=swap" rel="stylesheet">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="success-page-container">
        <div class="premium-success-card">
            <div class="icon-section">
                <div class="sparkle-v">
                    <span class="main-v">✓</span>
                    <div class="glow-effect"></div>
                </div>
            </div>

            <div class="text-section">
                <h1 class="main-title">ההזמנה הושלמה</h1>
                <div class="divider-gradient"></div>
                <p class="description-text">
                    .אישור הזמנה וקבלה נשלחו ברגע זה למייל שלך<br />
                   .הכרטיסים יחכו לך בקופות הקולנוע<br />
                    !צפייה נעימה
                </p>
            </div>

            <div class="action-section">
                <asp:Button ID="btnBackHome" runat="server" Text="חזרה לסרטים" PostBackUrl="~/HomePage.aspx" CssClass="premium-button" />
            </div>
        </div>
    </div>
</asp:Content>