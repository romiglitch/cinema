<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="ScreeningsEditor.aspx.cs" Inherits="Shipping.ScreeningsEditor" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .signup-form input.screenings-update-btn {
            background: linear-gradient(45deg, #8e2de2, #ffb347) !important;
            border: none !important;
            color: #fff !important;
            padding: 10px 25px !important;
            border-radius: 6px !important;
            cursor: pointer !important;
            font-weight: bold !important;
            transition: all 0.3s ease !important;
            box-shadow: 0 0 10px rgba(142, 45, 226, 0.4) !important;
            -webkit-appearance: none !important;
            appearance: none !important;
        }

        .signup-form input.screenings-update-btn.showBtn:hover {
            background: linear-gradient(45deg, #ffb347, #8e2de2) !important;
            transform: scale(1.05) !important;
            box-shadow: 0 0 18px rgba(255, 179, 71, 0.7) !important;
        }

        .signup-form input.screenings-update-btn.hiddenBtn {
            opacity: 0 !important;
            pointer-events: none;
            transform: translateY(10px);
            margin: 0 auto !important;
        }

        .signup-form input.screenings-update-btn.showBtn {
            opacity: 1 !important;
            pointer-events: auto;
            margin: 25px auto 0 auto !important;
            transform: translateY(0);
        }
    </style>
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

<div style="text-align:center;">
<asp:Button CssClass="signup-btn screenings-update-btn hiddenBtn"
    ID="btnAddScreening"
    runat="server"
    Text="עדכן הקרנות"
    OnClick="btnAddScreening_Click" />
</div>

<div style="margin-top:10px; text-align:center;">
    <asp:Label CssClass="editorMsg"
        ID="lblMessage"
        runat="server"
        EnableViewState="false">
    </asp:Label>
</div>

    </div>
</asp:Content>
