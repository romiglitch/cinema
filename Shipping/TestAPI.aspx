<%@ Page Title="" Language="C#" Async="true" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="TestAPI.aspx.cs" Inherits="Shipping.TestAPI" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:Repeater ID="rptMovies" runat="server">
    <ItemTemplate>
        <div class="movie">
            <img src='https://image.tmdb.org/t/p/w200<%# Eval("poster_path") %>' alt='<%# Eval("title") %>' />
            <h3><%# Eval("title") %></h3>
            <p>תאריך יציאה: <%# Eval("release_date") %></p>
        </div>
    </ItemTemplate>
</asp:Repeater>

</asp:Content>
