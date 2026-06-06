// MovieDetails.js — פתיחה וסגירה של חלון הטריילר
// modalTrailerId מוגדר בבלוק inline בדף ה-ASPX

function openTrailer() {
    var iframe = document.getElementById(modalTrailerId);
    var videoSrc = iframe.getAttribute("data-src");

    iframe.src = videoSrc;
    document.getElementById("trailerModal").style.display = "block";
}

function closeTrailer() {
    var iframe = document.getElementById(modalTrailerId);

    iframe.src = "";
    document.getElementById("trailerModal").style.display = "none";
}
