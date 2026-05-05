# 🎬 Cinema Booking System

A web application for browsing movies, selecting screenings, and purchasing tickets online.

Built with **ASP.NET Web Forms** and **SQL Server**.

---

## Features

- **Browse movies** by date — view all screenings for the next 7 days
- **Seat picker** — interactive seat selection per screening and hall
- **Ticket purchase** — payment processing via a bank service (BankService)
- **Order confirmation** — automatic email receipt sent after every successful purchase
- **User accounts** — registration, login, forgot password, and password reset via email token
- **Admin panel** — manage movies, screenings, and auto-generate a weekly schedule across 10 halls

---

## Project Structure

| Project | Description |
|---|---|
| `Shipping` | Main web application (ASP.NET Web Forms) |
| `DALLlilbrary` | Data Access Layer library |
| `TrailersWS` | Web service for movie trailers |

---

## Tech Stack

- ASP.NET Web Forms (.NET Framework)
- SQL Server
- MailKit / MimeKit (email sending via Gmail SMTP)
- Newtonsoft.Json
- Google APIs (Gmail)