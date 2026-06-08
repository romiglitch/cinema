# Cinema App — Manual Test Plan

Base URL: http://100.94.185.70:50594/
Test card: ISRAEL ISRAELI | 1234567890123456 | 12/27 | 123
Test user: kladnitsky.romi@gmail.com / 123ASdf!123

---

## TC-01 — Homepage loads
Navigate to the root URL. Verify the homepage loads (not a 403/404), shows movie listings or a welcome screen, and the navbar is present.

## TC-02 — Login with valid credentials
Navigate to /Login.aspx. Enter valid email + password and submit. Verify the user is redirected away from Login and the navbar shows a logout button (user is logged in).

## TC-03 — Login with invalid credentials
Navigate to /Login.aspx. Enter a wrong password and submit. Verify the user stays on the login page and an error message is displayed. Verify the user is NOT logged in.

## TC-04 — Browse movies by date
After login, navigate to /Movies.aspx. Select tomorrow's date from the dropdown. Verify that a list of movies with screening times appears.

## TC-05 — Full purchase flow (happy path)
Login → Movies → pick a tomorrow screening → Ticketing (add 1 regular ticket) → SeatsPicker (pick any free seat) → Cart (fill payment: ISRAEL ISRAELI / 1234567890123456 / 12/27 / 123) → Pay. Verify: (a) redirected to /Success.aspx, (b) success message is shown, (c) user is STILL logged in (logout button visible in navbar).

## TC-06 — Payment with wrong card number
Reach Cart with a valid order. Enter card number 9999999999999999 (not in DB), correct name/expiry/cvv. Submit. Verify an error message about wrong card details is shown. Verify user stays on Cart page.

## TC-07 — Payment with wrong holder name
Reach Cart. Enter card number 1234567890123456 but name WRONG NAME. Submit. Verify error message shown. User stays on Cart.

## TC-08 — Payment with insufficient balance
Query the DB to find a card with low balance (or manually set one). Attempt to buy tickets totalling more than the balance. Verify the "insufficient balance" error message is shown with the actual balance and required amount.

## TC-09 — Logout
While logged in, click the "התנתקות" (logout) button. Verify the user is redirected and the navbar no longer shows the logout button (showing login/register links instead).

## TC-10 — Access protected page without login
Without being logged in, navigate directly to /Cart.aspx. Verify the user is redirected to the login page or sees an access-denied message (not the cart itself).

## TC-11 — Movie details page loads
From Movies.aspx, click on a movie poster or title to open MovieDetails.aspx. Verify the page loads without a 500 error and shows movie info. Trailer may or may not load (graceful failure is acceptable).

---

## Results

| Test | Status | Notes |
|------|--------|-------|
| TC-01 | - | |
| TC-02 | - | |
| TC-03 | - | |
| TC-04 | - | |
| TC-05 | - | |
| TC-06 | - | |
| TC-07 | - | |
| TC-08 | - | |
| TC-09 | - | |
| TC-10 | - | |
| TC-11 | - | |
