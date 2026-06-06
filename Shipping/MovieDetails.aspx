<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="MovieDetails.aspx.cs" Inherits="Shipping.MovieDetails" Async="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script type="text/javascript">
        var modalTrailerId = '<%= modalTrailer.ClientID %>';
    </script>
    <script type="text/javascript" src="js/MovieDetails.js"></script>

    <div class="movie-details-container">

    <div class="movie-header">
        <asp:Image ID="imgPoster" runat="server" CssClass="movie-poster" />

        <div class="movie-info-wrapper">
            <h1 style="direction: rtl;" runat="server" id="lblTitle" class="movie-title1"></h1>
            <p style="direction: rtl;" runat="server" id="lblDescription" class="movie-description"></p>

            <div class="movie-meta">
                <span runat="server" id="lblDuration" ></span>
                <span runat="server" id="lblGenre" ></span>
            </div>

           <div class="actions-container">
    <asp:Button 
    ID="btnBuyTickets" 
    runat="server" 
    Text="🎟 רכישת כרטיסים" 
    CssClass="btn-action" 
    OnClick="btnBuyTickets_Click" />

    <button type="button" class="btn-action" onclick="openTrailer()">
        <span class="play-icon">▶</span> צפה בטריילר
    </button>
</div>
        </div>
    </div>

    <!-- טריילר -->
    <div id="trailerModal" class="modal">
        <div class="modal-content">
            <span class="close" onclick="closeTrailer()">&times;</span>
            <iframe id="modalTrailer"
                runat="server"
                frameborder="0"
                allow="autoplay; encrypted-media"
                allowfullscreen>
            </iframe>
        </div>
    </div>

</div>

</asp:Content>
