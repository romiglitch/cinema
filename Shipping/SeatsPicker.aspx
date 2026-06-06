<%@ Page Title="בחירת מושבים" Language="C#" MasterPageFile="~/Master.Master"
    AutoEventWireup="true" CodeBehind="SeatsPicker.aspx.cs" Inherits="Shipping.SeatsPicker" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Noto+Sans+JP:wght@300;400;500;700&display=swap" rel="stylesheet">
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script type="text/javascript">
    var maxSelect = <%= (Session["TotalTickets"] ?? 0) %>;
    var seatsPerRow = <%= (ViewState["SeatsPerRow"] ?? 0) %>;
</script>
<script type="text/javascript" src="js/SeatsPicker.js"></script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <h1>בחירת מושבים</h1>

    <div class="screen">מסך</div>

    <div class="seats-container">
        <asp:Repeater ID="RepeaterRows" runat="server" OnItemDataBound="RepeaterRows_ItemDataBound">
            <ItemTemplate>
                <div class="seat-row">
                    <div class="row-label">שורה <%# Eval("RowNumber") %></div>
                    <div class="row-seats" id="rowSeatsContainer" runat="server"></div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>

   <div class="legend">
    <div class="legend-item">פנוי <span class="legend-box available"></span></div>
    <div class="legend-item">נבחר<span class="legend-box selected"></span></div>
    <div class="legend-item">תפוס<span class="legend-box taken"></span></div>
    <div class="legend-item">נגיש<span class="legend-box accessible"></span></div>
</div>

    <input type="hidden" id="SelectedSeats" name="SelectedSeats"/>

    <div style="margin-top: 25px;">
        <asp:Button ID="btnConfirm" runat="server" Text="אישור מושבים"
            CssClass="btn-continue"
            OnClick="btnConfirm_Click"
            OnClientClick="return collectSeats();" />
        <span style="margin-right:15px;">נותרו לבחור:
            <span id="remaining"><%= (Session["TotalTickets"] ?? 0).ToString() %></span>
        </span>
    </div>

</asp:Content>
