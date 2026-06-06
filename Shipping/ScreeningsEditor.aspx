<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ScreeningsEditor.aspx.cs" Inherits="Shipping.ScreeningsEditor" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
   <script type="text/javascript">
       // משתנים שתלויים בשרת — מועברים לקובץ ג׳אבסקריפט החיצוני דרך משתנים גלובליים
       var btnAddScreeningId = '<%= btnAddScreening.ClientID %>';
       var lblMessageId = '<%= lblMessage.ClientID %>';
       var ddlMoviesId = '<%= ddlMovies.ClientID %>';
       var ddlMoviesUniqueId = '<%= ddlMovies.UniqueID %>';
   </script>
   <script type="text/javascript" src="js/ScreeningsEditor.js"></script>

  <div class="signup-form" style=" margin-top: 80px;">
    <div style="text-align:center">  <asp:DropDownList CssClass= ID="ddlMovies" runat="server" AutoPostBack="false" OnSelectedIndexChanged="ddlMovies_SelectedIndexChanged"></asp:DropDownList>
</div>
<asp:DropDownList ID="ddlTimes" runat="server" Visible="false"></asp:DropDownList>

 
        <asp:Panel ID="pnlSchedule" runat="server" Visible="false"></asp:Panel>

<div class="screenings-editor-actions">
<asp:Button CssClass="login-btn schedule-update-btn"
    ID="btnAddScreening"
    runat="server"
    Text="עדכן הקרנות"
    EnableViewState="false"
    OnClick="btnAddScreening_Click" />
    <asp:Label CssClass="editorMsg"
        ID="lblMessage"
        runat="server"
        EnableViewState="false">
    </asp:Label>
</div>

    </div>
</asp:Content>
