<%@ Page Title="" Language="C#" Async="true" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Movies.aspx.cs" Inherits="Shipping.Movies" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <!-- jQuery UI עבור datepicker -->
    <link href="//code.jquery.com/ui/1.12.1/themes/base/jquery-ui.css" rel="stylesheet" />
    <script src="//code.jquery.com/jquery-1.12.4.js"></script>
    <script src="//code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
  
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="movies-page">

        <h1 class="page-title">🎬 סרטים והקרנות</h1>

        <asp:DropDownList
            ID="ddlDates"
            runat="server"
            CssClass="date-dropdown"
            AutoPostBack="true"
            OnSelectedIndexChanged="ddlDates_SelectedIndexChanged">
        </asp:DropDownList>

        <asp:DataList
            ID="DLMoviesByDate"
            runat="server"
            CssClass="movies-vertical-list"
            OnItemDataBound="DLMoviesByDate_ItemDataBound" 
            RepeatLayout="Flow">

            <ItemTemplate>

                <div class="movie-row">

                    <div class="movie-name">
                        <%# Eval("film_name") %>
                    </div>

                    <div class="movie-showtimes">
                        <asp:Repeater ID="RptShowtimes" runat="server">
                            <ItemTemplate>
                                <%# RenderShowtimeLink(Container.DataItem) %>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                </div>

            </ItemTemplate>
        </asp:DataList>

    </div>

</asp:Content>

