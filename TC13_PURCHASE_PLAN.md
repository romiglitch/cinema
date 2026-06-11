# TC-13 — Bulk Purchase Plan

Generated from `scripts/bulk_purchase_program.py` (seed **42**, `--keep-yesterday`).
Source: `logs/tc13/plan_seed42.json` (executed TC-13 plan).

| Parameter | Value |
|-----------|-------|
| Ticket price | ₪50 |
| Initial balance per card | ₪32,000 |
| Tickets per screening | 20–25 (random) |
| Max movies per cinema day | 5 |
| Non-overlapping showtimes | yes (within each owner) |
| Seat assignment | disjoint row/seat blocks per owner (10×12 hall, row-major) |

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
| 09:00 | 7 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 11:15 | 2 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 13:50 | 10 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 16:30 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 19:50 | 8 | 22 | ₪1,100 | R1-S1–R2-S10 |

**2026-06-12**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 4 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 11:50 | 8 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 15:00 | 5 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 18:00 | 4 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 21:05 | 2 | 20 | ₪1,000 | R1-S1–R2-S8 |

**2026-06-13**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 3 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 11:45 | 3 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 14:40 | 2 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 18:40 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 21:05 | 2 | 24 | ₪1,200 | R1-S1–R2-S12 |

**2026-06-14**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 12:00 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 15:45 | 4 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 18:00 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 21:05 | 4 | 21 | ₪1,050 | R1-S1–R2-S9 |

**2026-06-15**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 12:00 | 3 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 15:45 | 4 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 18:00 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 21:05 | 3 | 24 | ₪1,200 | R1-S1–R2-S12 |

**2026-06-16**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 13:10 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 16:15 | 4 | 25 | ₪1,250 | R1-S1–R3-S1 |

### RACHEL COHEN
638 tickets · ₪31,900 · 29 screenings

**2026-06-11**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 23 | ₪1,150 | R1-S2–R2-S12 |
| 11:15 | 3 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 14:00 | 1 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 16:30 | 6 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 20:20 | 9 | 22 | ₪1,100 | R1-S1–R2-S10 |

**2026-06-12**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 11:30 | 7 | 21 | ₪1,050 | R1-S1–R2-S9 |
| 14:00 | 3 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 17:30 | 1 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 00:45 | 3 | 22 | ₪1,100 | R1-S1–R2-S10 |

**2026-06-13**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 11:45 | 3 | 21 | ₪1,050 | R3-S2–R4-S10 |
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
| 04:50 | 3 | 22 | ₪1,100 | R1-S1–R2-S10 |

**2026-06-15**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 4 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 14:00 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 16:30 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 01:55 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 04:50 | 3 | 23 | ₪1,150 | R1-S1–R2-S11 |

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
| 09:00 | 10 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 19:50 | 8 | 25 | ₪1,250 | R2-S11–R4-S11 |
| 23:30 | 10 | 24 | ₪1,200 | R1-S1–R2-S12 |
| 01:55 | 10 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 06:00 | 1 | 24 | ₪1,200 | R1-S1–R2-S12 |

**2026-06-12**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 1 | 21 | ₪1,050 | R2-S9–R4-S5 |
| 12:00 | 9 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 17:20 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 19:50 | 3 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 23:30 | 2 | 25 | ₪1,250 | R1-S1–R3-S1 |

**2026-06-13**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 21 | ₪1,050 | R2-S11–R4-S7 |
| 15:00 | 4 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 19:50 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 23:30 | 4 | 21 | ₪1,050 | R1-S1–R2-S9 |

**2026-06-14**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 13:10 | 1 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 18:00 | 3 | 23 | ₪1,150 | R2-S9–R4-S7 |
| 21:05 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 23:30 | 4 | 23 | ₪1,150 | R1-S1–R2-S11 |
| 01:55 | 1 | 25 | ₪1,250 | R2-S11–R4-S11 |

**2026-06-15**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 13:10 | 1 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 18:00 | 3 | 21 | ₪1,050 | R2-S9–R4-S5 |
| 21:05 | 4 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 23:30 | 4 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 01:55 | 1 | 21 | ₪1,050 | R3-S2–R4-S10 |

**2026-06-16**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 13:10 | 1 | 25 | ₪1,250 | R3-S2–R5-S2 |
| 21:05 | 2 | 22 | ₪1,100 | R1-S1–R2-S10 |
| 23:30 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |
| 01:55 | 1 | 25 | ₪1,250 | R1-S1–R3-S1 |

### MICHAL GOLAN
477 tickets · ₪23,850 · 21 screenings

**2026-06-11**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 10 | 25 | ₪1,250 | R2-S11–R4-S11 |
| 11:30 | 6 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 14:00 | 1 | 24 | ₪1,200 | R2-S12–R4-S11 |
| 18:00 | 3 | 25 | ₪1,250 | R1-S1–R3-S1 |

**2026-06-12**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 1 | 21 | ₪1,050 | R4-S6–R6-S2 |
| 11:30 | 7 | 24 | ₪1,200 | R2-S10–R4-S9 |
| 14:00 | 3 | 25 | ₪1,250 | R3-S2–R5-S2 |

**2026-06-13**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 2 | 21 | ₪1,050 | R4-S8–R6-S4 |
| 15:45 | 3 | 21 | ₪1,050 | R2-S10–R4-S6 |
| 02:30 | 1 | 23 | ₪1,150 | R1-S1–R2-S11 |

**2026-06-14**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 4 | 23 | ₪1,150 | R2-S9–R4-S7 |
| 14:00 | 2 | 22 | ₪1,100 | R2-S12–R4-S9 |
| 16:30 | 1 | 25 | ₪1,250 | R2-S9–R4-S9 |
| 01:55 | 1 | 25 | ₪1,250 | R4-S12–R6-S12 |

**2026-06-15**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 09:00 | 4 | 24 | ₪1,200 | R3-S1–R4-S12 |
| 14:00 | 2 | 21 | ₪1,050 | R2-S11–R4-S7 |
| 16:30 | 1 | 25 | ₪1,250 | R2-S9–R4-S9 |
| 01:55 | 1 | 22 | ₪1,100 | R4-S11–R6-S8 |

**2026-06-16**
| Hour | Hall | Tickets | Sum | Seats |
|------|------|---------|-----|-------|
| 14:00 | 2 | 20 | ₪1,000 | R1-S1–R2-S8 |
| 16:30 | 1 | 21 | ₪1,050 | R2-S9–R4-S5 |
| 00:45 | 3 | 20 | ₪1,000 | R2-S9–R4-S4 |

## Totals

- ISRAEL ISRAELI: 626 tickets, ₪31,300 (28 screenings)
- RACHEL COHEN: 638 tickets, ₪31,900 (29 screenings)
- DAVID LEVY: 630 tickets, ₪31,500 (28 screenings)
- MICHAL GOLAN: 477 tickets, ₪23,850 (21 screenings)

## Cross-owner conflicts

22 screening(s) shared by multiple card owners:

| Day | Hour | Hall | Screening | Planned | Seat ranges (disjoint) |
|-----|------|------|-----------|---------|-------------------------|
| 2026-06-11 | 09:00 | 10 | #35617 בלשי צמרת | 47 | DAVID LEVY: R1-S1–R2-S10; MICHAL GOLAN: R2-S11–R4-S11 |
| 2026-06-11 | 14:00 | 1 | #35633 Mother Mary | 47 | RACHEL COHEN: R1-S1–R2-S11; MICHAL GOLAN: R2-S12–R4-S11 |
| 2026-06-11 | 19:50 | 8 | #35655 הנוסע | 47 | ISRAEL ISRAELI: R1-S1–R2-S10; DAVID LEVY: R2-S11–R4-S11 |
| 2026-06-12 | 09:00 | 1 | #35678 בלשי צמרת | 62 | RACHEL COHEN: R1-S1–R2-S8; DAVID LEVY: R2-S9–R4-S5; MICHAL GOLAN: R4-S6–R6-S2 |
| 2026-06-12 | 11:30 | 7 | #35690 בילי אייליש: Hi... | 45 | RACHEL COHEN: R1-S1–R2-S9; MICHAL GOLAN: R2-S10–R4-S9 |
| 2026-06-12 | 14:00 | 3 | #35698 Mother Mary | 50 | RACHEL COHEN: R1-S1–R3-S1; MICHAL GOLAN: R3-S2–R5-S2 |
| 2026-06-13 | 09:00 | 2 | #35775 בלשי צמרת | 64 | RACHEL COHEN: R1-S1–R2-S10; DAVID LEVY: R2-S11–R4-S7; MICHAL GOLAN: R4-S8–R6-S4 |
| 2026-06-13 | 11:45 | 3 | #35781 עדן | 46 | ISRAEL ISRAELI: R1-S1–R3-S1; RACHEL COHEN: R3-S2–R4-S10 |
| 2026-06-13 | 15:45 | 3 | #35784 חוות החיות | 42 | RACHEL COHEN: R1-S1–R2-S9; MICHAL GOLAN: R2-S10–R4-S6 |
| 2026-06-14 | 01:55 | 1 | #35790 בלשי צמרת | 72 | RACHEL COHEN: R1-S1–R2-S10; DAVID LEVY: R2-S11–R4-S11; MICHAL GOLAN: R4-S12–R6-S12 |
| 2026-06-14 | 09:00 | 4 | #35800 חוות החיות | 43 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S7 |
| 2026-06-14 | 14:00 | 2 | #35793 Mother Mary | 45 | RACHEL COHEN: R1-S1–R2-S11; MICHAL GOLAN: R2-S12–R4-S9 |
| 2026-06-14 | 16:30 | 1 | #35789 בילי אייליש: Hi... | 45 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S9 |
| 2026-06-14 | 18:00 | 3 | #35799 התגלית | 43 | ISRAEL ISRAELI: R1-S1–R2-S8; DAVID LEVY: R2-S9–R4-S7 |
| 2026-06-15 | 01:55 | 1 | #35806 בלשי צמרת | 68 | RACHEL COHEN: R1-S1–R3-S1; DAVID LEVY: R3-S2–R4-S10; MICHAL GOLAN: R4-S11–R6-S8 |
| 2026-06-15 | 09:00 | 4 | #35816 חוות החיות | 48 | RACHEL COHEN: R1-S1–R2-S12; MICHAL GOLAN: R3-S1–R4-S12 |
| 2026-06-15 | 14:00 | 2 | #35809 Mother Mary | 43 | RACHEL COHEN: R1-S1–R2-S10; MICHAL GOLAN: R2-S11–R4-S7 |
| 2026-06-15 | 16:30 | 1 | #35805 בילי אייליש: Hi... | 45 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S9 |
| 2026-06-15 | 18:00 | 3 | #35815 התגלית | 41 | ISRAEL ISRAELI: R1-S1–R2-S8; DAVID LEVY: R2-S9–R4-S5 |
| 2026-06-16 | 00:45 | 3 | #35834 חוות החיות | 40 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S4 |
| 2026-06-16 | 13:10 | 1 | #35820 עסקים כרגיל | 50 | ISRAEL ISRAELI: R1-S1–R3-S1; DAVID LEVY: R3-S2–R5-S2 |
| 2026-06-16 | 16:30 | 1 | #35821 בילי אייליש: Hi... | 41 | RACHEL COHEN: R1-S1–R2-S8; MICHAL GOLAN: R2-S9–R4-S5 |

Summary: 0 over-capacity, 22 shared screening(s) with disjoint seat blocks assigned (owner order: ISRAEL ISRAELI, RACHEL COHEN, DAVID LEVY, MICHAL GOLAN).

_Regenerate plan JSON: `python3 scripts/bulk_purchase_program.py --seed 42 --keep-yesterday --json`

_Render this file: `python3 scripts/bulk_purchase_program.py --export-markdown logs/tc13/plan_seed42.json`

_Execute: `python3 scripts/execute_tc13.py --parallel`_
