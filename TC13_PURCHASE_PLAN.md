# TC-13 — Bulk Purchase Plan

Generated from `scripts/bulk_purchase_program.py` (seed **42**).

| Parameter | Value |
|-----------|-------|
| Ticket price | ₪50 |
| Initial balance per card | ₪32,000 |
| Tickets per screening | 20–25 (random) |
| Max movies per cinema day | 5 |
| Non-overlapping showtimes | yes (within each owner) |
| Seat assignment | disjoint row/seat blocks per owner (10×12 hall, row-major) |
| Bulk testing | add `?bulkTesting=1` to first URL per session — skips order receipt emails |

## Card owners and genres

| Owner | Card (last 4) | Preferred genres |
|-------|---------------|------------------|
| ISRAEL ISRAELI | …3456 | Action, Adventure, Science Fiction, Fantasy, Thriller |
| RACHEL COHEN | …7654 | Romance, Comedy, Drama, Family, Music |
| DAVID LEVY | …4444 | Horror, Mystery, Crime, War, Western |
| MICHAL GOLAN | …8888 | Animation, Documentary, History, Music, Family |

Seat blocks on shared screenings are assigned in owner order above (ISRAEL → RACHEL → DAVID → MICHAL).

## Purchase plan

### ISRAEL ISRAELI
626 tickets · ₪31,300 · 28 screenings

**2026-06-11**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 5 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 12:00 | 9 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 15:45 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 18:00 | 7 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 20:20 | 4 | 22 | ₪1,100 | R1-S1–R2-S10 |

**2026-06-12**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 11:50 | 8 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 15:00 | 5 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 18:00 | 4 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 21:05 | 2 | 20 | ₪1,000 | R1-S1–R2-S8 |

**2026-06-13**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 14:10 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 17:20 | 1 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 19:50 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 23:30 | 2 | 24 | ₪1,200 | R1-S1–R2-S12 |

**2026-06-14**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 1 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 11:50 | 4 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 15:45 | 4 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 18:00 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 21:05 | 4 | 21 | ₪1,050 | R1-S1–R2-S9 |

**2026-06-15**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 1 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 11:50 | 4 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 15:45 | 4 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 18:00 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 21:05 | 3 | 24 | ₪1,200 | R1-S1–R2-S12 |

**2026-06-16**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 1 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 11:50 | 5 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 16:15 | 4 | 25 | ₪1,250 | R1-S1–R3-S1 |

### RACHEL COHEN
638 tickets · ₪31,900 · 29 screenings

**2026-06-11**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 6 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 11:30 | 1 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 14:30 | 5 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 17:30 | 4 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 20:20 | 9 | 22 | ₪1,100 | R1-S1–R2-S10 |

**2026-06-12**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 11:30 | 3 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 14:40 | 4 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 17:30 | 1 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 00:45 | 3 | 22 | ₪1,100 | R1-S1–R2-S10 |

**2026-06-13**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 11:45 | 3 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 15:45 | 3 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 18:00 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 00:10 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |

**2026-06-14**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 4 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 14:00 | 2 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 16:30 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 01:55 | 1 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 04:50 | 1 | 22 | ₪1,100 | R1-S1–R2-S10 |

**2026-06-15**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 4 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 14:00 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 16:30 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 01:55 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 04:50 | 1 | 23 | ₪1,150 | R1-S1–R2-S11 |

**2026-06-16**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 11:50 | 4 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 16:30 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 22:00 | 3 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 00:45 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |

### DAVID LEVY
630 tickets · ₪31,500 · 28 screenings

**2026-06-11**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 8 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 18:40 | 10 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 21:05 | 10 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 01:55 | 10 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 06:00 | 1 | 24 | ₪1,200 | R1-S1–R2-S12 |

**2026-06-12**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 1 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 12:00 | 9 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 17:20 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 19:50 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 23:30 | 4 | 25 | ₪1,250 | R1-S1–R3-S1 |

**2026-06-13**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 21 | ₪1,050 | R2-S11–R4-S7 |
| 15:00 | 4 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 19:50 | 1 | 20 | ₪1,000 | R2-S9–R4-S4 |
| 23:30 | 3 | 21 | ₪1,050 | R1-S1–R2-S9 |

**2026-06-14**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 13:10 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 18:00 | 3 | 23 | ₪1,150 | R2-S9–R4-S7 |
| 21:05 | 3 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 23:30 | 3 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 01:55 | 1 | 25 | ₪1,250 | R2-S11–R4-S11 |

**2026-06-15**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 13:10 | 1 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 18:00 | 3 | 21 | ₪1,050 | R2-S9–R4-S5 |
| 21:05 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 23:30 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 01:55 | 1 | 21 | ₪1,050 | R3-S2–R4-S10 |

**2026-06-16**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 13:10 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 21:05 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 23:30 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 01:55 | 2 | 25 | ₪1,250 | R1-S1–R3-S1 |

### MICHAL GOLAN
457 tickets · ₪22,850 · 20 screenings

**2026-06-11**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 6 | 25 | ₪1,250 | R2-S12–R4-S12 |
| 11:30 | 1 | 20 | ₪1,000 | R2-S10–R4-S5 |
| 15:45 | 3 | 24 | ₪1,200 | R2-S9–R4-S8 |
| 18:40 | 10 | 25 | ₪1,250 | R3-S2–R5-S2 |

**2026-06-12**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 3 | 21 | ₪1,050 | R2-S9–R4-S5 |
| 11:30 | 3 | 24 | ₪1,200 | R2-S10–R4-S9 |

**2026-06-13**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 25 | ₪1,250 | R4-S8–R6-S8 |
| 15:45 | 3 | 21 | ₪1,050 | R2-S10–R4-S6 |
| 02:30 | 2 | 21 | ₪1,050 | R1-S1–R2-S9 |

**2026-06-14**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 4 | 23 | ₪1,150 | R2-S9–R4-S7 |
| 14:00 | 2 | 23 | ₪1,150 | R2-S12–R4-S10 |
| 16:30 | 1 | 22 | ₪1,100 | R2-S9–R4-S6 |
| 01:55 | 1 | 25 | ₪1,250 | R4-S12–R6-S12 |

**2026-06-15**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 4 | 25 | ₪1,250 | R3-S1–R5-S1 |
| 14:00 | 2 | 24 | ₪1,200 | R2-S11–R4-S10 |
| 16:30 | 1 | 21 | ₪1,050 | R2-S9–R4-S5 |
| 01:55 | 1 | 25 | ₪1,250 | R4-S11–R6-S11 |

**2026-06-16**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 14:00 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 16:30 | 1 | 20 | ₪1,000 | R2-S9–R4-S4 |
| 00:45 | 3 | 21 | ₪1,050 | R2-S9–R4-S5 |

---

## Totals

- ISRAEL ISRAELI: 626 tickets, ₪31,300 (28 screenings)
- RACHEL COHEN: 638 tickets, ₪31,900 (29 screenings)
- DAVID LEVY: 630 tickets, ₪31,500 (28 screenings)
- MICHAL GOLAN: 457 tickets, ₪22,850 (20 screenings)

## Cross-owner conflicts

21 screening(s) shared by multiple card owners:

| Day | Hour | Hall | Screening | Planned | Seat ranges (disjoint) |
|-----|------|------|-----------|---------|-------------------------|
| 2026-06-11 | 09:00 | 6 | #35613 בילי אייליש: Hi... | 48 | RACHEL COHEN: R1-S1–R2-S11; MICHAL GOLAN: R2-S12–R4-S12 |
| 2026-06-11 | 11:30 | 1 | #35623 Mother Mary | 41 | RACHEL COHEN: R1-S1–R2-S9; MICHAL GOLAN: R2-S10–R4-S5 |
| 2026-06-11 | 15:45 | 3 | #35640 חוות החיות | 44 | ISRAEL ISRAELI: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S8 |
| 2026-06-11 | 18:40 | 10 | #35652 בלשי צמרת | 50 | DAVID LEVY: R1-S1–R3-S1; MICHAL GOLAN: R3-S2–R5-S2 |
| 2026-06-12 | 09:00 | 3 | #35680 בילי אייליש: Hi... | 41 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S5 |
| 2026-06-12 | 11:30 | 3 | #35689 Mother Mary | 45 | RACHEL COHEN: R1-S1–R2-S9; MICHAL GOLAN: R2-S10–R4-S9 |
| 2026-06-13 | 09:00 | 2 | #35775 בלשי צמרת | 68 | RACHEL COHEN: R1-S1–R2-S10; DAVID LEVY: R2-S11–R4-S7; MICHAL GOLAN: R4-S8–R6-S8 |
| 2026-06-13 | 15:45 | 3 | #35784 חוות החיות | 42 | RACHEL COHEN: R1-S1–R2-S9; MICHAL GOLAN: R2-S10–R4-S6 |
| 2026-06-13 | 19:50 | 1 | #35716 הנוסע | 40 | ISRAEL ISRAELI: R1-S1–R2-S8; DAVID LEVY: R2-S9–R4-S4 |
| 2026-06-14 | 01:55 | 1 | #35790 בלשי צמרת | 72 | RACHEL COHEN: R1-S1–R2-S10; DAVID LEVY: R2-S11–R4-S11; MICHAL GOLAN: R4-S12–R6-S12 |
| 2026-06-14 | 09:00 | 4 | #35800 חוות החיות | 43 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S7 |
| 2026-06-14 | 14:00 | 2 | #35793 Mother Mary | 46 | RACHEL COHEN: R1-S1–R2-S11; MICHAL GOLAN: R2-S12–R4-S10 |
| 2026-06-14 | 16:30 | 1 | #35789 בילי אייליש: Hi... | 42 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S6 |
| 2026-06-14 | 18:00 | 3 | #35799 התגלית | 43 | ISRAEL ISRAELI: R1-S1–R2-S8; DAVID LEVY: R2-S9–R4-S7 |
| 2026-06-15 | 01:55 | 1 | #35806 בלשי צמרת | 71 | RACHEL COHEN: R1-S1–R3-S1; DAVID LEVY: R3-S2–R4-S10; MICHAL GOLAN: R4-S11–R6-S11 |
| 2026-06-15 | 09:00 | 4 | #35816 חוות החיות | 49 | RACHEL COHEN: R1-S1–R2-S12; MICHAL GOLAN: R3-S1–R5-S1 |
| 2026-06-15 | 14:00 | 2 | #35809 Mother Mary | 46 | RACHEL COHEN: R1-S1–R2-S10; MICHAL GOLAN: R2-S11–R4-S10 |
| 2026-06-15 | 16:30 | 1 | #35805 בילי אייליש: Hi... | 41 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S5 |
| 2026-06-15 | 18:00 | 3 | #35815 התגלית | 41 | ISRAEL ISRAELI: R1-S1–R2-S8; DAVID LEVY: R2-S9–R4-S5 |
| 2026-06-16 | 00:45 | 3 | #35834 חוות החיות | 41 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S5 |
| 2026-06-16 | 16:30 | 1 | #35821 בילי אייליש: Hi... | 40 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S4 |

Summary: 0 over-capacity, 21 shared screening(s) with disjoint seat blocks assigned (owner order: ISRAEL ISRAELI, RACHEL COHEN, DAVID LEVY, MICHAL GOLAN).

_Regenerate: `python3 scripts/bulk_purchase_program.py --seed 42 --keep-yesterday --compact`_

_Execute with bulk testing (no receipt emails): log in via `Login.aspx?bulkTesting=1` or set `BulkTesting=true` in Web.config / `.env`._
