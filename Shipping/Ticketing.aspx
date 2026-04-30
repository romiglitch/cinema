<%@ Page Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Ticketing.aspx.cs" Inherits="Shipping.Ticketing" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="container">
        <div class="screening-info" style="text-align: center; margin-bottom: 25px;">
            <asp:Literal ID="litScreeningInfo" runat="server" />
        </div>

        <asp:Repeater ID="RepeaterTickets" runat="server">
            <ItemTemplate>
                <div class="ticket-row">
                    <div style="display: flex; justify-content: space-between; align-items: center; width: 100%;">
                        <div class="ticket-type" style="flex: 1;"><%# Eval("PersonType") %></div>
                        <div class="ticket-price" style="margin: 0 20px;">₪<%# Eval("Price", "{0:N2}") %></div>

                        <div class="qty-controls" style="display: flex; align-items: center;">
                            <button type="button" class="btn-qty btn-minus">−</button>
                            <input type="text" class="qty-display" value="0" readonly style="width: 30px; text-align: center; border: none;" />
                            <button type="button" class="btn-qty btn-plus">+</button>
                        </div>
                    </div>

                    <div class="verification-box" data-type='<%# Eval("PersonType") %>'>
    <div class="verification-label">
        נדרש אימות זכאות עבור <%# Eval("PersonType") %>:
    </div>
    <input type="text" class="id-input" placeholder="הזן תעודת זהות / מספר מזהה" />
    <div class="error-text-simple" style="display: none;text-align:center;margin-top:5px;"></div>
</div>
                    <asp:HiddenField ID="hiddenQty" runat="server" Value="0" />
                    <asp:HiddenField ID="hiddenPrice" runat="server" Value='<%# Eval("Price") %>' />
                    <asp:HiddenField ID="hiddenType" runat="server" Value='<%# Eval("PersonType") %>' />
                </div>
            </ItemTemplate>
        </asp:Repeater>
        <div class="summary-line">
            <div class="total-label">
                סך הכל: <span id="grand-total">₪0.00</span>
            </div>
            <asp:Button ID="btnContinue" runat="server" CssClass="btn-continue" Text="המשך" OnClick="btnContinue_Click" OnClientClick="return validateVerification();" />
        </div>
    </div>

    <script>

        $(document).ready(function () {
            function updateTotals() {
                let total = 0;
                $(".ticket-row").each(function () {
                    let row = $(this);
                    let qty = parseInt(row.find(".qty-display").val()) || 0;
                    let price = parseFloat(row.find("input[id*='hiddenPrice']").val()) || 0;
                    let type = row.find(".verification-box").data("type");

                    total += (qty * price);

                    // עדכון ה-HiddenField כדי שה-C# יוכל לקרוא את הכמות
                    row.find("input[type='hidden'][id*='hiddenQty']").val(qty);

                    let vBox = row.find(".verification-box");
                    if (qty > 0 && type !== "רגיל") {
                        vBox.slideDown(200);
                    } else {
                        vBox.slideUp(200);
                        vBox.find(".id-input").val("");
                    }
                });
                $("#grand-total").text("₪" + total.toFixed(2));
            }

            $(document).on("click", ".btn-plus", function (e) {
                e.stopImmediatePropagation();
                let input = $(this).siblings(".qty-display");
                input.val(parseInt(input.val()) + 1);
                updateTotals();
            });

            $(document).on("click", ".btn-minus", function (e) {
                e.stopImmediatePropagation();
                let input = $(this).siblings(".qty-display");
                let val = parseInt(input.val());
                if (val > 0) {
                    input.val(val - 1);
                    updateTotals();
                }
            });

            updateTotals();
        });
        function validateVerification() {
            let isValid = true;
            let totalTicketsSelected = 0;

            // איפוס שגיאות קודמות - מחפשים לפי הקלאס החדש
            $(".error-text-simple").hide().text("");
            $(".id-input").css("border-color", "#ccc");

            // 1. בדיקה שנבחר לפחות כרטיס אחד
            $(".qty-display").each(function () {
                totalTicketsSelected += parseInt($(this).val()) || 0;
            });

            if (totalTicketsSelected === 0) {
                alert("אנא בחרי לפחות כרטיס אחד כדי להמשיך.");
                return false;
            }

            // 2. בדיקת תעודת זהות בשדות הגלויים
            $(".verification-box:visible").each(function () {
                let container = $(this);
                let input = container.find(".id-input");
                let errorDiv = container.find(".error-text-simple"); // עדכון לקלאס שלך
                let idValue = input.val().trim();
                let idPattern = /^\d{8,9}$/;

                if (idValue === "") {
                    isValid = false;
                    input.css("border-color", "red");
                    errorDiv.text("חובה להזין מספר מזהה").fadeIn();
                }
                else if (!idPattern.test(idValue)) {
                    isValid = false;
                    input.css("border-color", "red");
                    errorDiv.text("מספר זהות לא תקין (8-9 ספרות)").fadeIn();
                }
            });

            return isValid;
        }

        // בונוס: העלמת השגיאה כשהמשתמש מקליד
        $(document).on("input", ".id-input", function () {
            $(this).css("border-color", "#ccc");
            $(this).siblings(".error-text-simple").fadeOut();
        });
</script>
</asp:Content>
