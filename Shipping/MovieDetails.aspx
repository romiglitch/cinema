<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="MovieDetails.aspx.cs" Inherits="Shipping.MovieDetails" Async="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script>
        function openTrailer() {
            var iframe = document.getElementById("<%= modalTrailer.ClientID %>");
            var videoSrc = iframe.getAttribute("data-src");

            iframe.src = videoSrc; // טוען רק בלחיצה
            document.getElementById("trailerModal").style.display = "block";
        }

        function closeTrailer() {
            var iframe = document.getElementById("<%= modalTrailer.ClientID %>");

            iframe.src = ""; // מאפס לגמרי = מפסיק וידאו
            document.getElementById("trailerModal").style.display = "none";
        }
    </script>

    <div class="movie-details-container">

    <!-- Header עם הפוסטר והפרטים -->
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

    <!-- 🔹 Modal הטריילר צריך להיות כאן -->
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
