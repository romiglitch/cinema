<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="SelectScreening.aspx.cs" Inherits="Shipping.SelectScreening" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* --- DEMO: screenings by date (accordion) --- */
        .screenings-mockup-banner {
            background: rgba(168, 85, 247, 0.15);
            border: 1px dashed rgba(168, 85, 247, 0.6);
            color: #e9d5ff;
            border-radius: 12px;
            padding: 10px 16px;
            margin-bottom: 24px;
            font-size: 0.9rem;
            text-align: center;
        }

        .screenings-accordion {
            text-align: right;
            margin-top: 8px;
        }

        .screening-day {
            margin-bottom: 14px;
            border-radius: 16px;
            border: 1px solid rgba(255, 255, 255, 0.08);
            background: rgba(0, 0, 0, 0.25);
            overflow: hidden;
        }

        .screening-day-header {
            list-style: none;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            padding: 16px 20px;
            background: linear-gradient(90deg, rgba(168, 85, 247, 0.12), rgba(249, 115, 22, 0.08));
            color: #fff;
            font-weight: 700;
            font-size: 1.05rem;
            user-select: none;
        }

        .screening-day-header::-webkit-details-marker {
            display: none;
        }

        .screening-day-header::after {
            content: '▼';
            font-size: 0.75rem;
            color: rgba(255, 255, 255, 0.55);
            transition: transform 0.25s ease;
        }

        .screening-day[open] .screening-day-header::after {
            transform: rotate(180deg);
        }

        .screening-day-count {
            font-size: 0.85rem;
            font-weight: 500;
            color: rgba(255, 255, 255, 0.55);
        }

        .screening-day-body {
            padding: 16px 18px 20px;
            border-top: 1px solid rgba(255, 255, 255, 0.06);
        }

        .screenings-accordion .screenings-grid {
            text-align: center;
        }

        .screening-item .screening-date {
            display: none;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
  <div class="screenings-container">
    <h1 style="margin-top: -10px; margin-bottom: 30px;" id="lblMovieTitle" runat="server">דמו: אווטאר: דרכו של המים</h1>

    <p class="screenings-mockup-banner">
        תצוגת דמו — הקרנות מקובצות לפי תאריך. לאחר אישור העיצוב נחבר לנתונים מהשרת.
    </p>

    <!-- DEMO: accordion by date -->
    <div class="screenings-accordion" aria-label="הקרנות לפי תאריך">

        <details class="screening-day" open>
            <summary class="screening-day-header">
                <span>יום רביעי, 28 במאי 2026</span>
                <span class="screening-day-count">3 הקרנות</span>
            </summary>
            <div class="screening-day-body">
                <div class="screenings-grid">
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">17:30</span>
                        <span class="screening-date">28/05</span>
                    </a>
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">20:00</span>
                        <span class="screening-date">28/05</span>
                    </a>
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">22:45</span>
                        <span class="screening-date">28/05</span>
                    </a>
                </div>
            </div>
        </details>

        <details class="screening-day">
            <summary class="screening-day-header">
                <span>יום חמישי, 29 במאי 2026</span>
                <span class="screening-day-count">2 הקרנות</span>
            </summary>
            <div class="screening-day-body">
                <div class="screenings-grid">
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">18:15</span>
                        <span class="screening-date">29/05</span>
                    </a>
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">21:00</span>
                        <span class="screening-date">29/05</span>
                    </a>
                </div>
            </div>
        </details>

        <details class="screening-day">
            <summary class="screening-day-header">
                <span>יום שישי, 30 במאי 2026</span>
                <span class="screening-day-count">4 הקרנות</span>
            </summary>
            <div class="screening-day-body">
                <div class="screenings-grid">
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">16:00</span>
                        <span class="screening-date">30/05</span>
                    </a>
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">18:30</span>
                        <span class="screening-date">30/05</span>
                    </a>
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">20:45</span>
                        <span class="screening-date">30/05</span>
                    </a>
                    <a href="#" class="screening-item" onclick="return false;">
                        <span class="screening-time">23:15</span>
                        <span class="screening-date">30/05</span>
                    </a>
                </div>
            </div>
        </details>

    </div>

    <%-- Production repeater (hidden until wired to grouped data) --%>
    <asp:Panel ID="pnlLegacyList" runat="server" Visible="false">
        <div class="screenings-grid">
            <asp:Repeater ID="rptTimes" runat="server">
                <ItemTemplate>
                    <asp:LinkButton ID="btnSelect" runat="server" CssClass="screening-item"
                                    OnClick="btnSelectTime_Click"
                                    CommandArgument='<%# Eval("ScreeningId") %>'>
                        <span class="screening-time"><%# Eval("StartTime", "{0:HH:mm}") %></span>
                        <span class="screening-date"><%# Eval("StartTime", "{0:dd/MM}") %></span>
                    </asp:LinkButton>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>

    <asp:Label ID="lblNoScreenings" runat="server" Text=".אין הקרנות קרובות לסרט זה"
               Visible="false" CssClass="no-screenings-msg" />
  </div>
</asp:Content>
