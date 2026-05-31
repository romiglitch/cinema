<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ScreeningsEditor.aspx.cs" Inherits="Shipping.ScreeningsEditor" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
   <script type="text/javascript">
       // מציג את כפתור "עדכן הקרנות" ומנקה הודעות קודמות בכל שינוי צ'קבוקס
       function showUpdateButton() {
           var btn = document.getElementById('<%= btnAddScreening.ClientID %>');
           var lbl = document.getElementById('<%= lblMessage.ClientID %>');

           if (btn) {
               btn.classList.remove('hiddenBtn');
               btn.classList.add('showBtn');
           }

           if (lbl) {
               lbl.innerHTML = "";
           }
       }

       function onScheduleCheckboxClick(checkbox) {
           showUpdateButton();
           var wrapper = checkbox.closest('.checkbox-wrapper-33');
           if (!wrapper) return;

           var symbol = wrapper.querySelector('.checkbox__symbol');
           if (!symbol) return;

           if (checkbox.checked) {
               wrapper.classList.add('checkbox-wrapper-33--checked');
               symbol.classList.add('checkbox__symbol--checked');
           } else {
               wrapper.classList.remove('checkbox-wrapper-33--checked');
               symbol.classList.remove('checkbox__symbol--checked');
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
    Text="עדכן הקרנות"
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
