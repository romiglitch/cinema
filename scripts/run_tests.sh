#!/usr/bin/env bash
set -euo pipefail

BASE="http://100.94.185.70:50594"
BRIDGE="http://100.94.185.70:8765"
EMAIL="kladnitsky.romi@gmail.com"
PASS="1234"
CARD="1234567890123456"
SCREENING_ID=""
HALL_ID=""
BOOK_ROW=""
BOOK_SEAT=""
TMP="/tmp/cinema_test_$$"
mkdir -p "$TMP"

PASS_COUNT=0
FAIL_COUNT=0

result() {
  if [ "$1" = "PASS" ]; then
    echo "✓ $2: PASS"
    PASS_COUNT=$((PASS_COUNT + 1))
  else
    echo "✗ $2: FAIL — $3"
    FAIL_COUNT=$((FAIL_COUNT + 1))
  fi
}

get_fields() {
  local file=$1
  VS=$(grep -o 'id="__VIEWSTATE" value="[^"]*"' "$file" | sed 's/id="__VIEWSTATE" value="//;s/"//')
  EV=$(grep -o 'id="__EVENTVALIDATION" value="[^"]*"' "$file" | sed 's/id="__EVENTVALIDATION" value="//;s/"//')
  VSG=$(grep -o 'id="__VIEWSTATEGENERATOR" value="[^"]*"' "$file" | sed 's/id="__VIEWSTATEGENERATOR" value="//;s/"//')
}

login() {
  local cjar=$1
  rm -f "$cjar"
  curl -s --max-time 20 -c "$cjar" -o "$TMP/login.html" "$BASE/Login.aspx"
  get_fields "$TMP/login.html"
  curl -s --max-time 20 -b "$cjar" -c "$cjar" -o "$TMP/login_result.html" -L \
    --data-urlencode "__VIEWSTATE=$VS" \
    --data-urlencode "__VIEWSTATEGENERATOR=$VSG" \
    --data-urlencode "__EVENTVALIDATION=$EV" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$TxtEmail=$EMAIL" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$TxtPassword=$PASS" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$btnLogin=להתחבר" \
    "$BASE/Login.aspx"
  grep -q 'התנתקות' "$TMP/login_result.html"
}

# Find screening with free seats and one available seat via bridge
find_booking_target() {
  local resp
  resp=$(curl -s --max-time 15 -X POST -H "Content-Type: application/json" \
    -d '{"sql":"SELECT TOP 1 s.ScreeningId, s.Hall, h.Rows, h.SeatsPerRow FROM Screening s JOIN Halls h ON s.Hall = h.HallId WHERE s.StartTime > GETDATE() AND (h.Rows * h.SeatsPerRow) - ISNULL((SELECT COUNT(*) FROM Tickets t WHERE t.Screening = s.ScreeningId), 0) > 0 ORDER BY s.StartTime"}' \
    "$BRIDGE/query")
  SCREENING_ID=$(echo "$resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['rows'][0]['ScreeningId'] if d.get('rows') else '')" 2>/dev/null || true)
  HALL_ID=$(echo "$resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['rows'][0]['Hall'] if d.get('rows') else '')" 2>/dev/null || true)
  HALL_ROWS=$(echo "$resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['rows'][0]['Rows'] if d.get('rows') else '')" 2>/dev/null || true)
  SEATS_PER_ROW=$(echo "$resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['rows'][0]['SeatsPerRow'] if d.get('rows') else '')" 2>/dev/null || true)

  if [ -z "$SCREENING_ID" ]; then
    return 1
  fi

  # Find first free seat
  local taken_resp seat_found=""
  taken_resp=$(curl -s --max-time 15 -X POST -H "Content-Type: application/json" \
    -d "{\"sql\":\"SELECT [Row], [Seat] FROM Tickets WHERE Screening = $SCREENING_ID\"}" \
    "$BRIDGE/query")
  for row in $(seq 1 "$HALL_ROWS"); do
    for seat in $(seq 1 "$SEATS_PER_ROW"); do
      taken=$(echo "$taken_resp" | python3 -c "import sys,json; d=json.load(sys.stdin); rows=d.get('rows',[]); print('yes' if any(r.get('Row')==$row and r.get('Seat')==$seat for r in rows) else 'no')" 2>/dev/null || echo no)
      if [ "$taken" = "no" ]; then
        BOOK_ROW=$row
        BOOK_SEAT=$seat
        seat_found=1
        break 2
      fi
    done
  done
  [ -n "${seat_found:-}" ]
}

setup_booking() {
  local cjar=$1
  login "$cjar" || return 1
  find_booking_target || return 1
  # Prefer fully empty screenings to avoid seat conflicts between test runs
  local empty_resp empty_id
  empty_resp=$(curl -s --max-time 15 -X POST -H "Content-Type: application/json" \
    -d '{"sql":"SELECT TOP 1 s.ScreeningId, s.Hall, h.Rows, h.SeatsPerRow FROM Screening s JOIN Halls h ON s.Hall = h.HallId WHERE s.StartTime > GETDATE() AND NOT EXISTS (SELECT 1 FROM Tickets t WHERE t.Screening = s.ScreeningId) ORDER BY s.StartTime"}' \
    "$BRIDGE/query")
  empty_id=$(echo "$empty_resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['rows'][0]['ScreeningId'] if d.get('rows') else '')" 2>/dev/null || true)
  if [ -n "$empty_id" ]; then
    SCREENING_ID=$empty_id
    HALL_ID=$(echo "$empty_resp" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['rows'][0]['Hall'])")
    BOOK_ROW=1
    BOOK_SEAT=1
  fi

  # Ticketing page
  curl -s --max-time 20 -b "$cjar" -c "$cjar" -o "$TMP/tick.html" "$BASE/Ticketing.aspx?screeningId=$SCREENING_ID"
  get_fields "$TMP/tick.html"
  local post_args=(
    --data-urlencode "__VIEWSTATE=$VS"
    --data-urlencode "__VIEWSTATEGENERATOR=$VSG"
    --data-urlencode "__EVENTVALIDATION=$EV"
  )
  local n=0
  while [ "$n" -le 3 ]; do
    local ctl
    ctl=$(printf '%02d' "$n")
    local q p t qv pv tv
    q=$(grep -o "name=\"ctl00\\\$ContentPlaceHolder1\\\$RepeaterTickets\\\$ctl${ctl}\\\$hiddenQty\"" "$TMP/tick.html" | sed 's/name="//;s/"//')
    p=$(grep -o "name=\"ctl00\\\$ContentPlaceHolder1\\\$RepeaterTickets\\\$ctl${ctl}\\\$hiddenPrice\"" "$TMP/tick.html" | sed 's/name="//;s/"//')
    t=$(grep -o "name=\"ctl00\\\$ContentPlaceHolder1\\\$RepeaterTickets\\\$ctl${ctl}\\\$hiddenType\"" "$TMP/tick.html" | sed 's/name="//;s/"//')
    pv=$(grep -o "ctl${ctl}\\\$hiddenPrice\" id=\"[^\"]*\" value=\"[^\"]*\"" "$TMP/tick.html" | sed 's/.*value="//;s/"//')
    tv=$(grep -o "ctl${ctl}\\\$hiddenType\" id=\"[^\"]*\" value=\"[^\"]*\"" "$TMP/tick.html" | sed 's/.*value="//;s/"//')
    if [ "$n" -eq 0 ]; then qv=1; else qv=0; fi
    [ -n "$q" ] && post_args+=(--data-urlencode "$q=$qv")
    [ -n "$p" ] && post_args+=(--data-urlencode "$p=$pv")
    [ -n "$t" ] && post_args+=(--data-urlencode "$t=$tv")
    n=$((n + 1))
  done
  post_args+=(--data-urlencode "ctl00\$ContentPlaceHolder1\$btnContinue=המשך")

  curl -s --max-time 20 -b "$cjar" -c "$cjar" -o "$TMP/tick_result.html" -L \
    "${post_args[@]}" \
    "$BASE/Ticketing.aspx?screeningId=$SCREENING_ID"
  grep -q 'SeatsPicker' "$TMP/tick_result.html" || return 1

  # SeatsPicker - parse first available seat from page or use DB row/seat
  curl -s --max-time 20 -b "$cjar" -c "$cjar" -o "$TMP/seats.html" "$BASE/SeatsPicker.aspx?screeningId=$SCREENING_ID"
  get_fields "$TMP/seats.html"
  local seat_val
  seat_val=$(python3 - <<'PY' "$TMP/seats.html"
import re, sys
html = open(sys.argv[1], encoding='utf-8', errors='ignore').read()
for m in re.finditer(r'<div[^>]*class="([^"]*seat[^"]*)"[^>]*data-value="([^"]+)"', html):
    classes, val = m.group(1), m.group(2)
    if 'taken' not in classes.split():
        print(val)
        break
PY
)
  [ -n "$seat_val" ] || return 1

  curl -s --max-time 20 -b "$cjar" -c "$cjar" -o "$TMP/cart.html" -L \
    --data-urlencode "__VIEWSTATE=$VS" \
    --data-urlencode "__VIEWSTATEGENERATOR=$VSG" \
    --data-urlencode "__EVENTVALIDATION=$EV" \
    --data-urlencode "SelectedSeats=$seat_val" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$btnConfirm=אישור מושבים" \
    "$BASE/SeatsPicker.aspx?screeningId=$SCREENING_ID"
  grep -q 'litTotalPrice\|סה"כ לתשלום\|txtCardNum' "$TMP/cart.html" || return 1
  return 0
}

pay_from_cart() {
  local cjar=$1 name=$2 card=$3 expiry=$4 cvv=$5 outfile=$6
  curl -s --max-time 20 -b "$cjar" -c "$cjar" -o "$TMP/cart_fresh.html" "$BASE/Cart.aspx"
  get_fields "$TMP/cart_fresh.html"
  echo $(curl -s --max-time 60 -b "$cjar" -c "$cjar" -o "$outfile" -w "%{url_effective}" -L \
    --data-urlencode "__VIEWSTATE=$VS" \
    --data-urlencode "__VIEWSTATEGENERATOR=$VSG" \
    --data-urlencode "__EVENTVALIDATION=$EV" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$txtHolderName=$name" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$txtCardNum=$card" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$txtExpiry=$expiry" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$txtCVV=$cvv" \
    --data-urlencode "ctl00\$ContentPlaceHolder1\$BtnPay=בצע תשלום עכשיו" \
    "$BASE/Cart.aspx")
}

restore_card_balance() {
  curl -s --max-time 10 -X POST -H "Content-Type: application/json" \
    -d "{\"sql\":\"UPDATE DebitCards SET Balance = 1000.00 WHERE CardNumber = '$CARD'\"}" \
    "$BRIDGE/query-payment" >/dev/null
}

chat_event_post() {
  local cjar=$1 infile=$2 target=$3 outfile=$4
  get_fields "$infile"
  curl -s --max-time 90 -b "$cjar" -c "$cjar" -o "$outfile" \
    --data-urlencode "__VIEWSTATE=$VS" \
    --data-urlencode "__VIEWSTATEGENERATOR=$VSG" \
    --data-urlencode "__EVENTVALIDATION=$EV" \
    --data-urlencode "__EVENTTARGET=$target" \
    --data-urlencode "__EVENTARGUMENT=" \
    "$BASE/"
}

chat_send_message() {
  local cjar=$1 infile=$2 message=$3 outfile=$4
  get_fields "$infile"
  curl -s --max-time 90 -b "$cjar" -c "$cjar" -o "$outfile" \
    --data-urlencode "__VIEWSTATE=$VS" \
    --data-urlencode "__VIEWSTATEGENERATOR=$VSG" \
    --data-urlencode "__EVENTVALIDATION=$EV" \
    --data-urlencode "ctl00\$txtChatPrompt=$message" \
    --data-urlencode "ctl00\$btnChatSend=➤" \
    "$BASE/"
}

validate_ai_chat() {
  local file=$1
  python3 - <<'PY' "$file"
import re, sys
html = open(sys.argv[1], encoding='utf-8', errors='ignore').read()
msgs = [(m.group(1), re.sub(r'\s+', ' ', m.group(2)).strip())
        for m in re.finditer(r"class='message-row (user|ai)'>\s*<div class=\"bubble\">(.*?)</div>", html, re.S)]
users = [t for s, t in msgs if s == 'user']
ais = [t for s, t in msgs if s == 'ai']
checks = {
    'ui_launcher': 'ai-chat-launcher' in html and 'toggleChat' in html,
    'ui_window': 'ai-chat-window' in html,
    'ui_moods': all(x in html for x in ['btnHappy', 'btnScary', 'btnDate', 'מצחיק', 'מפחיד', 'דייט']),
    'mood_funny': any('מצחיק' in u for u in users),
    'mood_scary': any('מפחיד' in u for u in users),
    'mood_date': any('דייט' in u for u in users),
    'genre_request': any('דרמה' in u and 'מדע' in u for u in users),
    'ai_replies': len(ais) >= 4,
    'ai_not_busy': not any('עמוסים' in a for a in ais),
    'ai_recommends': ais and '**' in ais[-1],
}
failed = [k for k, ok in checks.items() if not ok]
print('OK' if not failed else 'FAIL:' + ','.join(failed))
PY
}

echo "=== Cinema Test Run ==="
echo "Base: $BASE"
echo ""

# TC-01
CODE=$(curl -s --max-time 20 -o "$TMP/home.html" -w "%{http_code}" "$BASE/")
if [ "$CODE" = "200" ] && grep -q 'nav\|דף הבית\|סרטים' "$TMP/home.html"; then
  result PASS "TC-01 Homepage"
else
  result FAIL "TC-01 Homepage" "HTTP $CODE"
fi

# TC-02
if login "$TMP/c2.txt"; then
  result PASS "TC-02 Valid login"
else
  result FAIL "TC-02 Valid login" "No logout button after login"
fi

# TC-03
rm -f "$TMP/c3.txt"
curl -s --max-time 20 -c "$TMP/c3.txt" -o "$TMP/l3.html" "$BASE/Login.aspx"
get_fields "$TMP/l3.html"
curl -s --max-time 20 -b "$TMP/c3.txt" -c "$TMP/c3.txt" -o "$TMP/l3r.html" -L \
  --data-urlencode "__VIEWSTATE=$VS" \
  --data-urlencode "__VIEWSTATEGENERATOR=$VSG" \
  --data-urlencode "__EVENTVALIDATION=$EV" \
  --data-urlencode "ctl00\$ContentPlaceHolder1\$TxtEmail=$EMAIL" \
  --data-urlencode "ctl00\$ContentPlaceHolder1\$TxtPassword=WRONGPASS" \
  --data-urlencode "ctl00\$ContentPlaceHolder1\$btnLogin=להתחבר" \
  "$BASE/Login.aspx" >/dev/null
if grep -q 'אימייל או סיסמא לא נכונים' "$TMP/l3r.html" && ! grep -q 'התנתקות' "$TMP/l3r.html"; then
  result PASS "TC-03 Invalid login"
else
  result FAIL "TC-03 Invalid login" "Missing error or user logged in"
fi

# TC-04
rm -f "$TMP/c4.txt"
login "$TMP/c4.txt" || true
curl -s --max-time 20 -b "$TMP/c4.txt" -c "$TMP/c4.txt" -o "$TMP/m4.html" "$BASE/Movies.aspx"
get_fields "$TMP/m4.html"
MOVIE_DATE=$(grep -o '<option value="[0-9-]*">' "$TMP/m4.html" | grep -v 'value=""' | head -1 | sed 's/<option value="//;s/">//')
if [ -z "$MOVIE_DATE" ]; then
  MOVIE_DATE="2026-06-11"
fi
curl -s --max-time 20 -b "$TMP/c4.txt" -c "$TMP/c4.txt" -o "$TMP/m4d.html" \
  --data-urlencode "__EVENTTARGET=ctl00\$ContentPlaceHolder1\$ddlDates" \
  --data-urlencode "__EVENTARGUMENT=" \
  --data-urlencode "__VIEWSTATE=$VS" \
  --data-urlencode "__VIEWSTATEGENERATOR=$VSG" \
  --data-urlencode "__EVENTVALIDATION=$EV" \
  --data-urlencode "ctl00\$ContentPlaceHolder1\$ddlDates=$MOVIE_DATE" \
  "$BASE/Movies.aspx"
LINKS=$(grep -c 'class="time-slot"' "$TMP/m4d.html" || true)
SOLD=$(grep -c 'class="time-slot sold-out"' "$TMP/m4d.html" || true)
if [ "$LINKS" -gt 0 ] || [ "$SOLD" -gt 0 ]; then
  result PASS "TC-04 Browse by date" 
else
  result FAIL "TC-04 Browse by date" "No showtimes for $MOVIE_DATE"
fi

# TC-05 through TC-08 need booking setup
restore_card_balance
if setup_booking "$TMP/c5.txt"; then
  DEST=$(pay_from_cart "$TMP/c5.txt" "ISRAEL ISRAELI" "$CARD" "12/27" "123" "$TMP/pay5.html")
  if echo "$DEST" | grep -qi 'Success' && { grep -q 'התנתקות' "$TMP/pay5.html" || grep -q 'ההזמנה הושלמה' "$TMP/pay5.html"; }; then
    result PASS "TC-05 Happy path purchase"
  else
    result FAIL "TC-05 Happy path purchase" "dest=$DEST"
  fi
else
  result FAIL "TC-05 Happy path purchase" "Could not set up booking (screening ${SCREENING_ID:-unknown})"
fi

restore_card_balance
if setup_booking "$TMP/c6.txt"; then
  pay_from_cart "$TMP/c6.txt" "ISRAEL ISRAELI" "9999999999999999" "12/27" "123" "$TMP/pay6.html" >/dev/null
  if grep -q 'פרטי הכרטיס שגויים' "$TMP/pay6.html" && ! grep -q 'Success.aspx' "$TMP/pay6.html"; then
    result PASS "TC-06 Wrong card number"
  else
    result FAIL "TC-06 Wrong card number" "Expected card error"
  fi
else
  result FAIL "TC-06 Wrong card number" "Could not set up booking"
fi

restore_card_balance
if setup_booking "$TMP/c7.txt"; then
  pay_from_cart "$TMP/c7.txt" "WRONG NAME" "$CARD" "12/27" "123" "$TMP/pay7.html" >/dev/null
  if grep -q 'פרטי הכרטיס שגויים' "$TMP/pay7.html"; then
    result PASS "TC-07 Wrong holder name"
  else
    result FAIL "TC-07 Wrong holder name" "Expected card error"
  fi
else
  result FAIL "TC-07 Wrong holder name" "Could not set up booking"
fi

restore_card_balance
curl -s --max-time 10 -X POST -H "Content-Type: application/json" \
  -d "{\"sql\":\"UPDATE DebitCards SET Balance = 10.00 WHERE CardNumber = '$CARD'\"}" \
  "$BRIDGE/query-payment" >/dev/null
if setup_booking "$TMP/c8.txt"; then
  pay_from_cart "$TMP/c8.txt" "ISRAEL ISRAELI" "$CARD" "12/27" "123" "$TMP/pay8.html" >/dev/null
  if grep -q 'אין מספיק יתרה' "$TMP/pay8.html"; then
    result PASS "TC-08 Insufficient balance"
  else
    result FAIL "TC-08 Insufficient balance" "Expected balance error"
  fi
else
  result FAIL "TC-08 Insufficient balance" "Could not set up booking"
fi
restore_card_balance

# TC-09
rm -f "$TMP/c9.txt"
login "$TMP/c9.txt" || true
curl -s --max-time 20 -b "$TMP/c9.txt" -c "$TMP/c9.txt" -o "$TMP/m9.html" "$BASE/Movies.aspx"
get_fields "$TMP/m9.html"
curl -s --max-time 20 -b "$TMP/c9.txt" -c "$TMP/c9.txt" -o "$TMP/l9r.html" -L \
  --data-urlencode "__VIEWSTATE=$VS" \
  --data-urlencode "__VIEWSTATEGENERATOR=$VSG" \
  --data-urlencode "__EVENTVALIDATION=$EV" \
  --data-urlencode "ctl00\$Btn=התנתקות" \
  "$BASE/Movies.aspx" >/dev/null
if ! grep -q 'התנתקות' "$TMP/l9r.html"; then
  result PASS "TC-09 Logout"
else
  result FAIL "TC-09 Logout" "Still logged in"
fi

# TC-10
DEST10=$(curl -s --max-time 20 -o "$TMP/cart10.html" -w "%{url_effective}" -L "$BASE/Cart.aspx")
if echo "$DEST10" | grep -qi 'Login'; then
  result PASS "TC-10 Unauth Cart redirects"
else
  result FAIL "TC-10 Unauth Cart redirects" "dest=$DEST10"
fi

# TC-11
CODE11=$(curl -s --max-time 20 -o "$TMP/md11.html" -w "%{http_code}" "$BASE/MovieDetails.aspx?id=1122573")
ERRORS=$(grep -ic 'Server Error\|Exception Details' "$TMP/md11.html" || true)
if [ "$CODE11" = "200" ] && [ "$ERRORS" -eq 0 ]; then
  result PASS "TC-11 MovieDetails"
else
  result FAIL "TC-11 MovieDetails" "HTTP=$CODE11 errors=$ERRORS"
fi

# TC-12 — AI chat
rm -f "$TMP/c12.txt"
curl -s --max-time 20 -c "$TMP/c12.txt" -o "$TMP/chat0.html" "$BASE/"
if chat_event_post "$TMP/c12.txt" "$TMP/chat0.html" 'ctl00$btnHappy' "$TMP/chat1.html" \
  && chat_event_post "$TMP/c12.txt" "$TMP/chat1.html" 'ctl00$btnScary' "$TMP/chat2.html" \
  && chat_event_post "$TMP/c12.txt" "$TMP/chat2.html" 'ctl00$btnDate' "$TMP/chat3.html" \
  && chat_send_message "$TMP/c12.txt" "$TMP/chat3.html" "אני מחפש סרט בז'אנר דרמה או מדע בדיוני" "$TMP/chat4.html"; then
  CHAT_RESULT=$(validate_ai_chat "$TMP/chat4.html")
  if [ "$CHAT_RESULT" = "OK" ]; then
    result PASS "TC-12 AI chat"
  else
    result FAIL "TC-12 AI chat" "$CHAT_RESULT"
  fi
else
  result FAIL "TC-12 AI chat" "Chat postback failed"
fi

# TC-13 — Bulk purchase program (plan generation)
if python3 "$(dirname "$0")/bulk_purchase_program.py" --seed 42 >/dev/null 2>&1; then
  result PASS "TC-13 Bulk purchase program"
else
  result FAIL "TC-13 Bulk purchase program" "Program validation failed (run scripts/bulk_purchase_program.py for details)"
fi

echo ""
echo "=============================="
echo "TOTAL: $PASS_COUNT passed, $FAIL_COUNT failed"
echo "=============================="

rm -rf "$TMP"
exit $FAIL_COUNT
