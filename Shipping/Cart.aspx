<%@ Page Title="סיכום ההזמנה שלך"  Async="true" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Cart.aspx.cs" Inherits="Shipping.Cart" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script>
        function formatExpiry(input) {
            // מסיר כל תו שאינו ספרה
            var value = input.value.replace(/\D/g, '');

            // אם הוקלדו יותר מ-2 ספרות, מוסיף את האלכסון
            if (value.length > 2) {
                input.value = value.substring(0, 2) + '/' + value.substring(2, 4);
            } else {
                input.value = value;
            }
        }
    </script>
    <div class="cart-page">
        <div class="summary-container">
            <h1>סיכום ההזמנה שלך 🧾</h1>

            <div class="summary-box">
                <div class="summary-info">
                    <p>כמות כרטיסים: <strong><asp:Literal ID="litTotalTickets" runat="server" /></strong></p>
                    <p>סה"כ לתשלום: <strong>₪<asp:Literal ID="litTotalPrice" runat="server" /></strong></p>
                    
                    <div class="movie-details">
    <p>🎬 סרט: <strong><asp:Literal ID="litMovieName" runat="server" /></strong></p>
    <p>🕒 שעה: <strong><asp:Literal ID="litScreeningTime" runat="server" /></strong></p>
</div>
                </div>

                <hr />

                <h2>🎟️ פרטי הכרטיסים:</h2>

                <ul class="tickets-list">
                    <asp:Repeater ID="rptTickets" runat="server">
                        <ItemTemplate>
                            <li>
                                אולם: <strong><%# Eval("HallId") %></strong>, 
                                שורה: <strong><%# Eval("Row") %></strong>, 
                                מושב: <strong><%# Eval("Seat") %></strong>, 
                                סוג כרטיס: <strong><%# Eval("Type") %></strong>
                            </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>
               <div class="payment-fields" style="margin-top:20px; border-top: 1px solid #ddd; padding-top:10px;">
    <h3>💳 פרטי תשלום:</h3>

    <p>
        שם בעל הכרטיס:
        <asp:TextBox ID="txtHolderName" runat="server" MaxLength="100" placeholder="ISRAEL ISRAELI"
            CssClass="input-box-cart" style="text-transform: uppercase;" />
        <asp:RequiredFieldValidator ID="rfvHolderName" runat="server" ControlToValidate="txtHolderName"
            ErrorMessage="שדה חובה" Display="Dynamic" CssClass="error-text-simple" />
    </p>
    
    <p>
        מספר כרטיס: 
        <asp:TextBox ID="txtCardNum" runat="server" MaxLength="16" placeholder="1234567890123456" CssClass="input-box-cart" />
        <asp:RequiredFieldValidator ID="rfvCard" runat="server" ControlToValidate="txtCardNum" 
            ErrorMessage="שדה חובה" Display="Dynamic" CssClass="error-text-simple" />
        <asp:RegularExpressionValidator ID="revCard" runat="server" ControlToValidate="txtCardNum" 
            ValidationExpression="^\d{16}$" ErrorMessage="מספר כרטיס חייב להכיל 16 ספרות" 
            Display="Dynamic" CssClass="error-text-simple" />
    </p>

    <p>
    תוקף: 
    <asp:TextBox ID="txtExpiry" runat="server" 
        placeholder="MM/YY" 
        Width="80px" 
        CssClass="input-box-cart" 
        oninput="formatExpiry(this)" 
        MaxLength="5" />
    
    <asp:RequiredFieldValidator ID="rfvExpiry" runat="server" 
        ControlToValidate="txtExpiry" 
        ErrorMessage="שדה חובה" 
        Display="Dynamic" 
        CssClass="error-text-simple" />
    
    <asp:RegularExpressionValidator ID="revExpiry" runat="server" 
        ControlToValidate="txtExpiry" 
        ValidationExpression="^(0[1-9]|1[0-2])\/\d{2}$" 
        ErrorMessage="פורמט לא תקין" 
        Display="Dynamic" 
        CssClass="error-text-simple" />
</p>

    <p>
        CVV: 
        <asp:TextBox ID="txtCVV" runat="server" MaxLength="3" Width="60px" CssClass="input-box-cart" />
        <asp:RequiredFieldValidator ID="rfvCVV" runat="server" ControlToValidate="txtCVV" 
            ErrorMessage="שדה חובה" Display="Dynamic" CssClass="error-text-simple" />
        <asp:RegularExpressionValidator ID="revCVV" runat="server" ControlToValidate="txtCVV" 
            ValidationExpression="^\d{3}$" ErrorMessage="3 ספרות בלבד" 
            Display="Dynamic" CssClass="error-text-simple" />
    </p>

    <asp:Label ID="lblMsg" runat="server" CssClass="msg-label" />
</div>

<asp:Button ID="BtnPay" runat="server" Text="בצע תשלום עכשיו" CssClass="reserve-button" OnClick="BtnPay_Click" />
            </div>
        </div>
    </div>
</asp:Content>
