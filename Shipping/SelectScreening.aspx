<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="SelectScreening.aspx.cs" Inherits="Shipping.SelectScreening" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
  <div class="screenings-container">
    <h1 style="margin-top: -10px; margin-bottom: 30px;" id="lblMovieTitle" runat="server"></h1>

    <asp:Panel ID="pnlScreeningsByDate" runat="server" Visible="false" CssClass="screenings-by-date">
        <asp:Repeater ID="rptDays" runat="server" OnItemDataBound="rptDays_ItemDataBound">
            <ItemTemplate>
                <div class="screening-day">
                    <div class="screening-day-header">
                        <span><%# Eval("DateLabel") %></span>
                        <span class="screening-day-count"><%# Eval("Count") %> הקרנות</span>
                    </div>
                    <div class="screening-day-body">
                        <div class="screenings-grid-by-day">
                            <asp:Repeater ID="rptDayTimes" runat="server">
                                <ItemTemplate>
                                    <asp:LinkButton ID="btnSelect" runat="server" CssClass="screening-item"
                                                    OnClick="btnSelectTime_Click"
                                                    CommandArgument='<%# Eval("ScreeningId") %>'>
                                        <span class="screening-time"><%# Eval("StartTime", "{0:HH:mm}") %></span>
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </asp:Panel>

    <asp:Label ID="lblNoScreenings" runat="server" Text=".אין הקרנות קרובות לסרט זה"
               Visible="false" CssClass="no-screenings-msg" />
  </div>
</asp:Content>
