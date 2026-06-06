<%@ Page Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Ticketing.aspx.cs" Inherits="Shipping.Ticketing" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="container">
        <div class="screening-info" style="text-align: center; margin-bottom: 25px;">
            <asp:Literal ID="litScreeningInfo" runat="server" />
        </div>

        <asp:Repeater ID="RepeaterTickets" runat="server">
            <ItemTemplate>
                <div class="ticket-row">
                    <div style="display: flex; justify-content: space-between; align-items: center; width: 100%;">
                        <div class="ticket-type" style="flex: 1;"><%# Eval("PersonType") %></div>
                        <div class="ticket-price" style="margin: 0 20px;">₪<%# Eval("Price", "{0:N2}") %></div>

                        <div class="qty-controls" style="display: flex; align-items: center;">
                            <button type="button" class="btn-qty btn-minus">−</button>
                            <input type="text" class="qty-display" value="0" readonly style="width: 30px; text-align: center; border: none;" />
                            <button type="button" class="btn-qty btn-plus">+</button>
                        </div>
                    </div>

                    <div class="verification-box" data-type='<%# Eval("PersonType") %>'>
    <div class="verification-label">
        נדרש אימות זכאות עבור <%# Eval("PersonType") %>:
    </div>
    <input type="text" class="id-input" placeholder="הזן תעודת זהות / מספר מזהה" />
    <div class="error-text-simple" style="display: none;text-align:center;margin-top:5px;"></div>
</div>
                    <asp:HiddenField ID="hiddenQty" runat="server" Value="0" />
                    <asp:HiddenField ID="hiddenPrice" runat="server" Value='<%# Eval("Price") %>' />
                    <asp:HiddenField ID="hiddenType" runat="server" Value='<%# Eval("PersonType") %>' />
                </div>
            </ItemTemplate>
        </asp:Repeater>
        <div class="summary-line">
            <div class="total-label">
                סך הכל: <span id="grand-total">₪0.00</span>
            </div>
            <asp:Button ID="btnContinue" runat="server" CssClass="btn-continue" Text="המשך" OnClick="btnContinue_Click" OnClientClick="return validateVerification();" />
        </div>
    </div>

    <script type="text/javascript" src="js/Ticketing.js"></script>
</asp:Content>
