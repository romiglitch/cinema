<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="SelectScreening.aspx.cs" Inherits="Shipping.SelectScreening" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
  <div class="screenings-container">
    <h1 style="margin-top: -10px; margin-bottom: 30px;" id="lblMovieTitle" runat="server" ></h1>
    
    <div class="screenings-grid">
        <asp:Repeater ID="rptTimes" runat="server">
            <ItemTemplate>
                <asp:LinkButton ID="btnSelect" runat="server" CssClass="screening-item" 
                                OnClick="btnSelectTime_Click" 
                                CommandArgument='<%# Eval("ScreeningId") %>'>
                    <span class="screening-time"><%# Eval("StartTime", "{0:HH:mm}") %></span>
                    <span class="screening-date"><%# Eval("StartTime", "{0:dd/MM}") %></span>
                </asp:LinkButton>
            </ItemTemplate>
        </asp:Repeater>
    </div>

    <asp:Label  ID="lblNoScreenings" runat="server" Text=".אין הקרנות קרובות לסרט זה" 
               Visible="false" CssClass="no-screenings-msg" />
</div>
</asp:Content>
