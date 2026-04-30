<%@ Page Title="" Language="C#" Async="true" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="HomePage.aspx.cs" Inherits="Shipping.HomePage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<h1>🎬 עכשיו בקולנוע</h1>
<div class="movies-grid">
   <asp:Repeater ID="rptMovies" runat="server">
    <ItemTemplate>
        <div class="movie-card">

            <!-- מעבר לעמוד פרטים -->
            <a href='MovieDetails.aspx?id=<%# Eval("Id") %>' class="movie-link">
                <img src="<%# Eval("Poster") %>" alt="<%# Eval("Title") %>" />
                <p class="movie-title"><%# Eval("Title") %></p>
            </a>

        </div>
    </ItemTemplate>
</asp:Repeater>
</div>

</asp:Content>

