<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ScreeningsEditor.aspx.cs" Inherits="Shipping.ScreeningsEditor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
   <script type="text/javascript">
       function showAddButton(radio) {
           var btn = document.getElementById('<%= btnAddScreening.ClientID %>');
           var lbl = document.getElementById('<%= lblMessage.ClientID %>');

           if (btn) {
               btn.classList.remove('hiddenBtn');
               btn.classList.add('showBtn');
               btn.setAttribute('data-info', radio.getAttribute('data-info'));
           }

           if (lbl) {
               lbl.innerHTML = ""; // מנקה הודעה קודמת
           }
       }

   </script>

  <div class="signup-form" style=" margin-top: 80px;">
    <div style="text-align:center">  <asp:DropDownList CssClass= ID="ddlMovies" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlMovies_SelectedIndexChanged"></asp:DropDownList>
</div>
<asp:DropDownList ID="ddlTimes" runat="server" Visible="false"></asp:DropDownList>

<asp:DropDownList ID="ddlHalls" runat="server" Visible="false"></asp:DropDownList>
 
        <asp:Panel ID="pnlSchedule" runat="server" Visible="false"></asp:Panel>

<asp:Button CssClass="btnAddS hiddenBtn"
    ID="btnAddScreening"
    runat="server"
    Text="הוסף הקרנה"
    OnClick="btnAddScreening_Click" />

<div style="margin-top:10px; text-align:center;">
    <asp:Label CssClass="editorMsg"
        ID="lblMessage"
        runat="server"
        EnableViewState="false">
    </asp:Label>
</div>

    </div>
</asp:Content>
