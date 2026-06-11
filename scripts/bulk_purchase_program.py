#!/usr/bin/env python3
"""
TC-13 — Bulk purchase program for four debit-card owners.

Each owner prefers 5 genres and buys 20–25 tickets per screening (random),
for matching movies on available cinema days (max 5 movies/day).
Initial balance per card: ₪32,000.
Screenings on yesterday's cinema day are removed from the DB before planning.
"""

from __future__ import annotations

import argparse
import json
import math
import random
import sys
import urllib.request
from collections import defaultdict
from dataclasses import dataclass, field
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Set, Tuple

BRIDGE_DEFAULT = "http://100.94.185.70:8765"
TICKET_PRICE = 50.0
INITIAL_BALANCE = 32000.0
MIN_TICKETS_PER_SCREENING = 20
MAX_TICKETS_PER_SCREENING = 25
MAX_MOVIES_PER_DAY = 5
APP_MAX_TICKETS_PER_PURCHASE = 10  # Ticketing.aspx.cs — plan lines may exceed this
RANDOM_SEED = 42

# Four card owners — 5 preferred genres each
OWNERS = [
    {
        "holder": "ISRAEL ISRAELI",
        "card": "1234567890123456",
        "expiry": "12/27",
        "cvc": "123",
        "genres": ["Action", "Adventure", "Science Fiction", "Fantasy", "Thriller"],
    },
    {
        "holder": "RACHEL COHEN",
        "card": "9876543210987654",
        "expiry": "06/26",
        "cvc": "456",
        "genres": ["Romance", "Comedy", "Drama", "Family", "Music"],
    },
    {
        "holder": "DAVID LEVY",
        "card": "1111222233334444",
        "expiry": "03/28",
        "cvc": "789",
        "genres": ["Horror", "Mystery", "Crime", "War", "Western"],
    },
    {
        "holder": "MICHAL GOLAN",
        "card": "5555666677778888",
        "expiry": "09/27",
        "cvc": "321",
        "genres": ["Animation", "Documentary", "History", "Music", "Family"],
    },
]


@dataclass
class Screening:
    screening_id: int
    movie_id: int
    title: str
    hall: int
    start_time: str
    end_time: str
    cinema_day: str
    free_seats: int
    genres: Set[str]


@dataclass
class PurchaseLine:
    cinema_day: str
    start_time: str
    end_time: str
    screening_id: int
    hall: int
    title: str
    genres: str
    tickets: int
    cost: float
    free_seats: int
    seat_range: str = ""
    seat_rows: List[Tuple[int, int]] = field(default_factory=list)


@dataclass
class OwnerProgram:
    holder: str
    card: str
    preferred_genres: List[str]
    lines: List[PurchaseLine] = field(default_factory=list)

    @property
    def total_tickets(self) -> int:
        return sum(l.tickets for l in self.lines)

    @property
    def total_cost(self) -> float:
        return sum(l.cost for l in self.lines)

    @property
    def movies_by_day(self) -> Dict[str, int]:
        counts: Dict[str, int] = defaultdict(int)
        seen: Dict[str, Set[str]] = defaultdict(set)
        for line in self.lines:
            if line.title not in seen[line.cinema_day]:
                seen[line.cinema_day].add(line.title)
                counts[line.cinema_day] += 1
        return dict(counts)


OWNER_ORDER: Dict[str, int] = {o["holder"]: i for i, o in enumerate(OWNERS)}


def format_seat(row: int, seat: int) -> str:
    return f"R{row}-S{seat}"


def format_seat_range(seats: List[Tuple[int, int]]) -> str:
    if not seats:
        return "—"
    if len(seats) == 1:
        return format_seat(seats[0][0], seats[0][1])
    return f"{format_seat(seats[0][0], seats[0][1])}–{format_seat(seats[-1][0], seats[-1][1])}"


def load_hall_dimensions(base: str) -> Dict[int, Tuple[int, int]]:
    raw = bridge_query(base, "SELECT HallId, Rows, SeatsPerRow FROM Halls")
    return {
        int(row["HallId"]): (int(row["Rows"]), int(row["SeatsPerRow"]))
        for row in raw["rows"]
    }


def load_taken_seats(base: str) -> Dict[int, Set[Tuple[int, int]]]:
    raw = bridge_query(base, "SELECT Screening, [Row], [Seat] FROM Tickets")
    taken: Dict[int, Set[Tuple[int, int]]] = defaultdict(set)
    for row in raw["rows"]:
        taken[int(row["Screening"])].add((int(row["Row"]), int(row["Seat"])))
    return taken


def iter_hall_seats(rows: int, seats_per_row: int):
    for row in range(1, rows + 1):
        for seat in range(1, seats_per_row + 1):
            yield row, seat


def pick_seats(
    hall: int,
    count: int,
    hall_dims: Dict[int, Tuple[int, int]],
    blocked: Set[Tuple[int, int]],
) -> List[Tuple[int, int]]:
    rows, seats_per_row = hall_dims[hall]
    picked: List[Tuple[int, int]] = []
    for pos in iter_hall_seats(rows, seats_per_row):
        if pos in blocked:
            continue
        picked.append(pos)
        blocked.add(pos)
        if len(picked) == count:
            break
    return picked


def assign_disjoint_seat_ranges(
    programs: List[OwnerProgram],
    hall_dims: Dict[int, Tuple[int, int]],
    taken_seats: Dict[int, Set[Tuple[int, int]]],
) -> None:
    """Assign row/seat ranges per purchase line; shared screenings get disjoint blocks."""
    by_screening: Dict[int, List[Tuple[str, PurchaseLine]]] = defaultdict(list)
    for program in programs:
        for line in program.lines:
            by_screening[line.screening_id].append((program.holder, line))

    for screening_id, entries in by_screening.items():
        blocked = set(taken_seats.get(screening_id, set()))
        hall = entries[0][1].hall
        entries.sort(key=lambda e: OWNER_ORDER.get(e[0], 999))
        for _holder, line in entries:
            seats = pick_seats(hall, line.tickets, hall_dims, blocked)
            line.seat_rows = seats
            line.seat_range = format_seat_range(seats)


def validate_seat_assignments(programs: List[OwnerProgram]) -> List[str]:
    errors: List[str] = []
    by_screening: Dict[int, List[Tuple[str, PurchaseLine]]] = defaultdict(list)
    for program in programs:
        for line in program.lines:
            by_screening[line.screening_id].append((program.holder, line))

    for screening_id, entries in by_screening.items():
        seen: Set[Tuple[int, int]] = set()
        for holder, line in entries:
            if len(line.seat_rows) != line.tickets:
                errors.append(
                    f"screening #{screening_id} {holder}: assigned {len(line.seat_rows)} "
                    f"seats but planned {line.tickets}"
                )
            for seat in line.seat_rows:
                if seat in seen:
                    errors.append(
                        f"screening #{screening_id}: duplicate seat {format_seat(*seat)} "
                        f"across owners"
                    )
                seen.add(seat)
    return errors


CINEMA_DAY_SQL = """
    CASE WHEN DATEPART(HOUR, s.StartTime) < 9
         THEN CONVERT(varchar(10), DATEADD(day, -1, CAST(s.StartTime AS date)), 120)
         ELSE CONVERT(varchar(10), CAST(s.StartTime AS date), 120)
    END
"""


def server_yesterday(base: str) -> str:
    row = bridge_query(
        base,
        "SELECT CONVERT(varchar(10), DATEADD(day, -1, CAST(GETDATE() AS date)), 120) AS Y",
    )
    return row["rows"][0]["Y"]


def remove_yesterday_screenings(base: str) -> tuple[str, int, int]:
    """Delete tickets and screenings belonging to yesterday's cinema day."""
    yesterday = server_yesterday(base)
    tickets_sql = f"""
        DELETE FROM Tickets
        WHERE Screening IN (
            SELECT s.ScreeningId FROM Screening s
            WHERE {CINEMA_DAY_SQL.strip()} = '{yesterday}'
        )
    """
    bridge_query(base, tickets_sql)
    count_row = bridge_query(
        base,
        f"SELECT COUNT(*) AS Cnt FROM Screening s WHERE {CINEMA_DAY_SQL.strip()} = '{yesterday}'",
    )
    count = int(count_row["rows"][0]["Cnt"])
    if count:
        bridge_query(
            base,
            f"DELETE FROM Screening WHERE ScreeningId IN "
            f"(SELECT s.ScreeningId FROM Screening s WHERE {CINEMA_DAY_SQL.strip()} = '{yesterday}')",
        )
    return yesterday, count, 0


def screening_hour(start_time: str) -> str:
    if " " in start_time:
        return start_time.split(" ", 1)[1][:5]
    return start_time[:5]


def bridge_query(base: str, sql: str, payment: bool = False) -> dict:
    path = "/query-payment" if payment else "/query"
    req = urllib.request.Request(
        f"{base.rstrip('/')}{path}",
        data=json.dumps({"sql": sql}).encode(),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=30) as resp:
        data = json.load(resp)
    if not data.get("success"):
        raise RuntimeError(data.get("error", "query failed"))
    return data


def load_data(base: str) -> tuple[List[Screening], Dict[int, Set[str]]]:
    screenings_raw = bridge_query(
        base,
        """
        SELECT s.ScreeningId, s.Hall, m.Id AS MovieId, m.Title,
               CONVERT(varchar(16), s.StartTime, 120) AS StartTime,
               CONVERT(varchar(16), s.EndTime, 120) AS EndTime,
               CASE WHEN DATEPART(HOUR, s.StartTime) < 9
                    THEN CONVERT(varchar(10), DATEADD(day, -1, CAST(s.StartTime AS date)), 120)
                    ELSE CONVERT(varchar(10), CAST(s.StartTime AS date), 120)
               END AS CinemaDay,
               (h.Rows * h.SeatsPerRow) - ISNULL(tk.Sold, 0) AS FreeSeats
        FROM Screening s
        JOIN Movie m ON s.MovieId = m.Id
        JOIN Halls h ON s.Hall = h.HallId
        LEFT JOIN (SELECT Screening, COUNT(*) AS Sold FROM Tickets GROUP BY Screening) tk
            ON tk.Screening = s.ScreeningId
        WHERE s.StartTime > GETDATE()
          AND (h.Rows * h.SeatsPerRow) - ISNULL(tk.Sold, 0) > 0
        ORDER BY CinemaDay, StartTime
        """,
    )
    genres_raw = bridge_query(
        base,
        "SELECT mg.IdMovie, g.Name AS Genre FROM MovieGenres mg JOIN Genres g ON mg.IdGenre = g.Id",
    )
    movie_genres: Dict[int, Set[str]] = defaultdict(set)
    for row in genres_raw["rows"]:
        movie_genres[row["IdMovie"]].add(row["Genre"])

    screenings: List[Screening] = []
    for row in screenings_raw["rows"]:
        mid = row["MovieId"]
        screenings.append(
            Screening(
                screening_id=row["ScreeningId"],
                movie_id=mid,
                title=row["Title"],
                hall=int(row["Hall"]),
                start_time=row["StartTime"],
                end_time=row["EndTime"],
                cinema_day=row["CinemaDay"],
                free_seats=row["FreeSeats"],
                genres=movie_genres.get(mid, set()),
            )
        )
    return screenings, movie_genres


def movie_matches(genres: Set[str], preferred: Set[str]) -> bool:
    return bool(genres & preferred)


def all_preferred_genres() -> Set[str]:
    genres: Set[str] = set()
    for owner in OWNERS:
        genres.update(owner["genres"])
    return genres


def slot_interval_minutes(duration: int) -> int:
    return int(math.ceil(duration / 5.0) * 5) + 35


def parse_screening_time(value: str) -> datetime:
    if value.startswith("/Date("):
        ms = int(value[6 : value.index(")")])
        return datetime.fromtimestamp(ms / 1000.0)
    return datetime.strptime(value[:16], "%Y-%m-%d %H:%M")


def intervals_overlap(
    a_start: datetime,
    a_end: datetime,
    b_start: datetime,
    b_end: datetime,
) -> bool:
    return a_start < b_end and a_end > b_start


def pick_non_overlapping_screenings(
    candidates: List[Screening],
    *,
    max_picks: int,
) -> List[Screening]:
    """Pick up to max_picks screenings — one per title, no overlapping times."""
    by_title: Dict[str, List[Screening]] = defaultdict(list)
    for s in candidates:
        if s.free_seats >= MIN_TICKETS_PER_SCREENING:
            by_title[s.title].append(s)
    for title in by_title:
        by_title[title].sort(key=lambda s: parse_screening_time(s.start_time))

    remaining = set(by_title.keys())
    picked: List[Screening] = []
    busy: List[Tuple[datetime, datetime]] = []

    while len(picked) < max_picks and remaining:
        best: Optional[Screening] = None
        best_start: Optional[datetime] = None
        for title in remaining:
            for s in by_title[title]:
                start = parse_screening_time(s.start_time)
                end = parse_screening_time(s.end_time)
                if any(
                    intervals_overlap(start, end, busy_start, busy_end)
                    for busy_start, busy_end in busy
                ):
                    continue
                if best_start is None or start < best_start:
                    best = s
                    best_start = start
        if best is None:
            break
        picked.append(best)
        remaining.remove(best.title)
        busy.append(
            (parse_screening_time(best.start_time), parse_screening_time(best.end_time))
        )

    return picked


def cinema_day_start(cinema_day: str) -> datetime:
    return datetime.strptime(cinema_day, "%Y-%m-%d").replace(hour=9, minute=0, second=0)


def relevant_movie_ids(movie_genres: Dict[int, Set[str]]) -> Set[int]:
    preferred = all_preferred_genres()
    return {mid for mid, genres in movie_genres.items() if genres & preferred}


def ensure_screenings(
    base: str,
    movie_genres: Dict[int, Set[str]],
    *,
    dry_run: bool = False,
) -> int:
    """Insert screenings for genre-relevant movies on cinema days where they are missing."""
    cinema_days_raw = bridge_query(
        base,
        """
        SELECT DISTINCT
            CASE WHEN DATEPART(HOUR, s.StartTime) < 9
                 THEN CONVERT(varchar(10), DATEADD(day, -1, CAST(s.StartTime AS date)), 120)
                 ELSE CONVERT(varchar(10), CAST(s.StartTime AS date), 120)
            END AS CinemaDay
        FROM Screening s
        WHERE s.StartTime > GETDATE()
        ORDER BY CinemaDay
        """,
    )
    cinema_days = [r["CinemaDay"] for r in cinema_days_raw["rows"]]
    if not cinema_days:
        return 0

    movies_raw = bridge_query(
        base,
        "SELECT Id, Title, Duration FROM Movie ORDER BY Id",
    )
    movies = {r["Id"]: r for r in movies_raw["rows"]}
    rel_ids = relevant_movie_ids(movie_genres) & set(movies.keys())

    existing_raw = bridge_query(
        base,
        """
        SELECT s.MovieId, s.Hall,
               CONVERT(varchar(16), s.StartTime, 120) AS StartTime,
               CONVERT(varchar(16), s.EndTime, 120) AS EndTime,
               CASE WHEN DATEPART(HOUR, s.StartTime) < 9
                    THEN CONVERT(varchar(10), DATEADD(day, -1, CAST(s.StartTime AS date)), 120)
                    ELSE CONVERT(varchar(10), CAST(s.StartTime AS date), 120)
               END AS CinemaDay
        FROM Screening s
        WHERE s.StartTime > GETDATE()
        """,
    )

    movie_days: Dict[int, Set[str]] = defaultdict(set)
    hall_busy: Dict[int, List[Tuple[datetime, datetime]]] = defaultdict(list)
    for row in existing_raw["rows"]:
        mid = row["MovieId"]
        day = row["CinemaDay"]
        movie_days[mid].add(day)
        start = parse_screening_time(row["StartTime"])
        end = parse_screening_time(row["EndTime"])
        hall_busy[row["Hall"]].append((start, end))

    for intervals in hall_busy.values():
        intervals.sort(key=lambda x: x[0])

    def hall_free(hall: int, start: datetime, end: datetime) -> bool:
        for busy_start, busy_end in hall_busy[hall]:
            if start < busy_end and end > busy_start:
                return False
        return True

    inserted = 0
    for cinema_day in cinema_days:
        day_start = cinema_day_start(cinema_day)
        slot_usage: Dict[int, int] = defaultdict(int)
        for mid in sorted(rel_ids):
            if cinema_day in movie_days[mid]:
                continue
            movie = movies[mid]
            duration = int(movie["Duration"] or 100)
            slot_mins = slot_interval_minutes(duration)
            placed = False
            slot_order = sorted(range(8), key=lambda idx: (slot_usage[idx], idx))
            for slot_idx in slot_order:
                start = day_start + timedelta(minutes=slot_idx * slot_mins)
                end = start + timedelta(minutes=slot_mins)
                for hall in range(1, 11):
                    if hall_free(hall, start, end):
                        start_sql = start.strftime("%Y-%m-%d %H:%M:%S")
                        end_sql = end.strftime("%Y-%m-%d %H:%M:%S")
                        sql = (
                            "INSERT INTO Screening (MovieId, Hall, StartTime, EndTime, SeatesBought) "
                            f"VALUES ({mid}, {hall}, '{start_sql}', '{end_sql}', 0)"
                        )
                        if not dry_run:
                            bridge_query(base, sql)
                        hall_busy[hall].append((start, end))
                        movie_days[mid].add(cinema_day)
                        slot_usage[slot_idx] += 1
                        inserted += 1
                        placed = True
                        break
                if placed:
                    break
    return inserted


def ensure_staggered_slots(
    base: str,
    movie_genres: Dict[int, Set[str]],
    *,
    dry_run: bool = False,
) -> int:
    """Add later-day screenings for movies that only have a morning slot on a cinema day."""
    cinema_days_raw = bridge_query(
        base,
        """
        SELECT DISTINCT
            CASE WHEN DATEPART(HOUR, s.StartTime) < 9
                 THEN CONVERT(varchar(10), DATEADD(day, -1, CAST(s.StartTime AS date)), 120)
                 ELSE CONVERT(varchar(10), CAST(s.StartTime AS date), 120)
            END AS CinemaDay
        FROM Screening s
        WHERE s.StartTime > GETDATE()
        ORDER BY CinemaDay
        """,
    )
    cinema_days = [r["CinemaDay"] for r in cinema_days_raw["rows"]]
    if not cinema_days:
        return 0

    movies_raw = bridge_query(
        base,
        "SELECT Id, Title, Duration FROM Movie ORDER BY Id",
    )
    movies = {r["Id"]: r for r in movies_raw["rows"]}
    rel_ids = relevant_movie_ids(movie_genres) & set(movies.keys())

    existing_raw = bridge_query(
        base,
        """
        SELECT s.MovieId, s.Hall,
               CONVERT(varchar(16), s.StartTime, 120) AS StartTime,
               CONVERT(varchar(16), s.EndTime, 120) AS EndTime,
               CASE WHEN DATEPART(HOUR, s.StartTime) < 9
                    THEN CONVERT(varchar(10), DATEADD(day, -1, CAST(s.StartTime AS date)), 120)
                    ELSE CONVERT(varchar(10), CAST(s.StartTime AS date), 120)
               END AS CinemaDay
        FROM Screening s
        WHERE s.StartTime > GETDATE()
        """,
    )

    hall_busy: Dict[int, List[Tuple[datetime, datetime]]] = defaultdict(list)
    movie_day_starts: Dict[Tuple[str, int], List[datetime]] = defaultdict(list)
    for row in existing_raw["rows"]:
        start = parse_screening_time(row["StartTime"])
        end = parse_screening_time(row["EndTime"])
        hall_busy[row["Hall"]].append((start, end))
        movie_day_starts[(row["CinemaDay"], row["MovieId"])].append(start)

    for intervals in hall_busy.values():
        intervals.sort(key=lambda x: x[0])

    def hall_free(hall: int, start: datetime, end: datetime) -> bool:
        for busy_start, busy_end in hall_busy[hall]:
            if start < busy_end and end > busy_start:
                return False
        return True

    inserted = 0
    for cinema_day in cinema_days:
        day_start = cinema_day_start(cinema_day)
        morning_only: List[int] = []
        for mid in sorted(rel_ids):
            starts = movie_day_starts.get((cinema_day, mid), [])
            if len(starts) != 1:
                continue
            if starts[0].hour == 9 and starts[0].minute == 0:
                morning_only.append(mid)

        for offset, mid in enumerate(morning_only[1 : MAX_MOVIES_PER_DAY + 2]):
            movie = movies[mid]
            duration = int(movie["Duration"] or 100)
            slot_mins = slot_interval_minutes(duration)
            slot_idx = offset + 1
            start = day_start + timedelta(minutes=slot_idx * slot_mins)
            end = start + timedelta(minutes=slot_mins)
            placed = False
            for hall in range(1, 11):
                if hall_free(hall, start, end):
                    start_sql = start.strftime("%Y-%m-%d %H:%M:%S")
                    end_sql = end.strftime("%Y-%m-%d %H:%M:%S")
                    sql = (
                        "INSERT INTO Screening (MovieId, Hall, StartTime, EndTime, SeatesBought) "
                        f"VALUES ({mid}, {hall}, '{start_sql}', '{end_sql}', 0)"
                    )
                    if not dry_run:
                        bridge_query(base, sql)
                    hall_busy[hall].append((start, end))
                    movie_day_starts[(cinema_day, mid)].append(start)
                    inserted += 1
                    placed = True
                    break

    return inserted


def build_program(
    owner: dict,
    screenings: List[Screening],
    all_cinema_days: List[str],
    rng: random.Random,
) -> OwnerProgram:
    preferred = set(owner["genres"])
    program = OwnerProgram(
        holder=owner["holder"],
        card=owner["card"],
        preferred_genres=owner["genres"],
    )

    by_day: Dict[str, List[Screening]] = defaultdict(list)
    for s in screenings:
        if movie_matches(s.genres, preferred):
            by_day[s.cinema_day].append(s)

    for day in all_cinema_days:
        day_picks = pick_non_overlapping_screenings(
            by_day.get(day, []),
            max_picks=MAX_MOVIES_PER_DAY,
        )
        for s in day_picks:
            max_tickets = min(MAX_TICKETS_PER_SCREENING, s.free_seats)
            tickets = rng.randint(MIN_TICKETS_PER_SCREENING, max_tickets)
            cost = tickets * TICKET_PRICE
            if program.total_cost + cost > INITIAL_BALANCE:
                continue
            program.lines.append(
                PurchaseLine(
                    cinema_day=s.cinema_day,
                    start_time=s.start_time,
                    end_time=s.end_time,
                    screening_id=s.screening_id,
                    hall=s.hall,
                    title=s.title,
                    genres=", ".join(sorted(s.genres)),
                    tickets=tickets,
                    cost=cost,
                    free_seats=s.free_seats,
                )
            )

    program.lines.sort(key=lambda l: (l.cinema_day, l.start_time))
    return program


def validate_program(program: OwnerProgram) -> List[str]:
    errors: List[str] = []
    if not program.lines:
        errors.append("no screenings planned")
    if program.total_cost > INITIAL_BALANCE:
        errors.append(f"cost {program.total_cost} exceeds balance {INITIAL_BALANCE}")
    for day, count in program.movies_by_day.items():
        if count > MAX_MOVIES_PER_DAY:
            errors.append(f"day {day} has {count} movies (max {MAX_MOVIES_PER_DAY})")
    by_day_lines: Dict[str, List[PurchaseLine]] = defaultdict(list)
    for line in program.lines:
        by_day_lines[line.cinema_day].append(line)
    for day, lines in by_day_lines.items():
        for i, a in enumerate(lines):
            a_start = parse_screening_time(a.start_time)
            a_end = parse_screening_time(a.end_time)
            for b in lines[i + 1 :]:
                b_start = parse_screening_time(b.start_time)
                b_end = parse_screening_time(b.end_time)
                if intervals_overlap(a_start, a_end, b_start, b_end):
                    errors.append(
                        f"day {day} has overlapping screenings at "
                        f"{screening_hour(a.start_time)} and {screening_hour(b.start_time)}"
                    )
    for line in program.lines:
        if not (MIN_TICKETS_PER_SCREENING <= line.tickets <= MAX_TICKETS_PER_SCREENING):
            errors.append(
                f"screening {line.screening_id} has {line.tickets} tickets "
                f"(expected {MIN_TICKETS_PER_SCREENING}–{MAX_TICKETS_PER_SCREENING} per screening)"
            )
        if line.tickets > line.free_seats:
            errors.append(
                f"screening {line.screening_id} wants {line.tickets} but only {line.free_seats} free"
            )
    return errors


@dataclass
class CrossOwnerConflict:
    screening_id: int
    cinema_day: str
    hour: str
    hall: int
    title: str
    free_seats: int
    owners: List[Tuple[str, int]]
    total_tickets: int
    owner_seats: List[Tuple[str, str]] = field(default_factory=list)

    @property
    def over_capacity(self) -> bool:
        return self.total_tickets > self.free_seats

    @property
    def severity(self) -> str:
        if self.over_capacity:
            return "CAPACITY"
        return "PARALLEL"


def find_cross_owner_conflicts(programs: List[OwnerProgram]) -> List[CrossOwnerConflict]:
    """Screenings where more than one card owner buys tickets."""
    by_screening: Dict[int, List[Tuple[str, PurchaseLine]]] = defaultdict(list)
    for program in programs:
        for line in program.lines:
            by_screening[line.screening_id].append((program.holder, line))

    conflicts: List[CrossOwnerConflict] = []
    for screening_id, entries in by_screening.items():
        if len(entries) < 2:
            continue
        sample = entries[0][1]
        owner_tickets = [(holder, line.tickets) for holder, line in entries]
        owner_seats = [
            (holder, line.seat_range)
            for holder, line in sorted(
                entries, key=lambda e: OWNER_ORDER.get(e[0], 999)
            )
        ]
        conflicts.append(
            CrossOwnerConflict(
                screening_id=screening_id,
                cinema_day=sample.cinema_day,
                hour=screening_hour(sample.start_time),
                hall=sample.hall,
                title=sample.title,
                free_seats=sample.free_seats,
                owners=owner_tickets,
                total_tickets=sum(t for _, t in owner_tickets),
                owner_seats=owner_seats,
            )
        )

    conflicts.sort(key=lambda c: (c.cinema_day, c.hour, c.hall))
    return conflicts


def validate_cross_owner_conflicts(conflicts: List[CrossOwnerConflict]) -> List[str]:
    errors: List[str] = []
    for c in conflicts:
        owners_str = ", ".join(f"{h} ({t})" for h, t in c.owners)
        if c.over_capacity:
            errors.append(
                f"screening #{c.screening_id} {c.cinema_day} {c.hour} hall {c.hall} "
                f"({c.title}): {c.total_tickets} tickets planned but only "
                f"{c.free_seats} free — {owners_str}"
            )
    return errors


def format_conflicts_report(conflicts: List[CrossOwnerConflict]) -> str:
    if not conflicts:
        return "No cross-owner screening conflicts."

    lines = [
        f"{len(conflicts)} screening(s) shared by multiple card owners:\n",
        "| Day | Hour | Hall | Screening | Planned | Seat ranges (disjoint) |",
        "|-----|------|------|-----------|---------|-------------------------|",
    ]
    for c in conflicts:
        title = c.title if len(c.title) <= 18 else c.title[:15] + "..."
        seats_str = "; ".join(f"{h}: {sr}" for h, sr in c.owner_seats)
        lines.append(
            f"| {c.cinema_day} | {c.hour} | {c.hall} | #{c.screening_id} {title} | "
            f"{c.total_tickets} | {seats_str} |"
        )
    capacity = sum(1 for c in conflicts if c.over_capacity)
    parallel_only = len(conflicts) - capacity
    lines.append("")
    lines.append(
        f"Summary: {capacity} over-capacity, {parallel_only} shared screening(s) "
        f"with disjoint seat blocks assigned (owner order: "
        f"{', '.join(o['holder'] for o in OWNERS)})."
    )
    return "\n".join(lines)


def print_conflicts(conflicts: List[CrossOwnerConflict]) -> None:
    print("\n## Cross-owner conflicts")
    print(format_conflicts_report(conflicts))


def print_program(program: OwnerProgram) -> None:
    print(f"\n{'=' * 72}")
    print(f"  {program.holder}  —  card …{program.card[-4:]}")
    print(f"  Preferred genres: {', '.join(program.preferred_genres)}")
    print(
        f"  Tickets per screening: {MIN_TICKETS_PER_SCREENING}–{MAX_TICKETS_PER_SCREENING}"
        f"  |  Total: {program.total_tickets} across {len(program.lines)} screening(s)"
    )
    print(f"  Total cost: ₪{program.total_cost:,.2f}  |  Balance after: ₪{INITIAL_BALANCE - program.total_cost:,.2f}")
    print(f"{'=' * 72}")
    current_day = None
    for line in program.lines:
        if line.cinema_day != current_day:
            current_day = line.cinema_day
            movies_today = program.movies_by_day.get(current_day, 0)
            print(f"\n  📅 Cinema day {current_day}  ({movies_today} movie(s))")
        print(
            f"    {line.start_time}  #{line.screening_id}  {line.title}\n"
            f"      genres: {line.genres}\n"
            f"      → {line.tickets} ticket(s) × ₪{TICKET_PRICE:.0f} = ₪{line.cost:.0f}"
            f"  (free seats: {line.free_seats})"
        )


def print_compact_program(program: OwnerProgram) -> None:
    print(f"\n### {program.holder}")
    print(
        f"{program.total_tickets} tickets · ₪{program.total_cost:,.0f} · "
        f"{len(program.lines)} screenings"
    )
    current_day = None
    for line in program.lines:
        if line.cinema_day != current_day:
            current_day = line.cinema_day
            print(f"\n**{current_day}**")
            print("| Hour | Hall | Tickets | Sum | Seats |")
            print("|------|------|---------|-----|-------|")
        hour = screening_hour(line.start_time)
        print(
            f"| {hour} | {line.hall} | {line.tickets} | ₪{line.cost:,.0f} | "
            f"{line.seat_range} |"
        )


def print_genre_division() -> None:
    print("\n" + "=" * 72)
    print("  GENRE DIVISION — 4 card owners × 5 genres")
    print("=" * 72)
    for owner in OWNERS:
        print(f"\n  {owner['holder']}")
        print(f"    Card: {owner['card']}")
        for i, g in enumerate(owner["genres"], 1):
            print(f"    {i}. {g}")


def seat_range_for_owner(owner_data: dict, screening_id: int) -> str:
    for purchase in owner_data.get("purchases", []):
        if purchase.get("screening_id") == screening_id:
            return purchase.get("seat_range", "—")
    return "—"


def render_markdown_plan(data: dict, source_note: str = "") -> str:
    """Render TC-13 purchase plan markdown from JSON plan payload."""
    ticket_price = data.get("ticket_price", TICKET_PRICE)
    initial_balance = data.get("initial_balance", INITIAL_BALANCE)
    lines = [
        "# TC-13 — Bulk Purchase Plan",
        "",
        "Generated from `scripts/bulk_purchase_program.py` (seed **42**, `--keep-yesterday`).",
    ]
    if source_note:
        lines.append(f"Source: {source_note}.")
    lines.extend(
        [
            "",
            "| Parameter | Value |",
            "|-----------|-------|",
            f"| Ticket price | ₪{ticket_price:,.0f} |",
            f"| Initial balance per card | ₪{initial_balance:,.0f} |",
            f"| Tickets per screening | {MIN_TICKETS_PER_SCREENING}–{MAX_TICKETS_PER_SCREENING} (random) |",
            f"| Max movies per cinema day | {MAX_MOVIES_PER_DAY} |",
            "| Non-overlapping showtimes | yes (within each owner) |",
            "| Seat assignment | disjoint row/seat blocks per owner (10×12 hall, row-major) |",
            "",
            "## Card owners and genres",
            "",
            "| Owner | Card (last 4) | Preferred genres |",
            "|-------|---------------|------------------|",
        ]
    )
    for owner in OWNERS:
        lines.append(
            f"| {owner['holder']} | …{owner['card'][-4:]} | "
            f"{', '.join(owner['genres'])} |"
        )
    lines.extend(
        [
            "",
            "Seat blocks on shared screenings are assigned in owner order above "
            "(ISRAEL → RACHEL → DAVID → MICHAL).",
            "",
            "## Purchase plan",
        ]
    )

    for owner_data in data["owners"]:
        total_tickets = owner_data["total_tickets"]
        total_cost = owner_data["total_cost"]
        count = len(owner_data["purchases"])
        lines.append(f"\n### {owner_data['holder']}")
        lines.append(
            f"{total_tickets} tickets · ₪{total_cost:,.0f} · {count} screenings"
        )
        current_day = None
        for purchase in owner_data["purchases"]:
            day = purchase["cinema_day"]
            if day != current_day:
                current_day = day
                lines.append(f"\n**{day}**")
                lines.append("| Hour | Hall | Tickets | Sum | Seats |")
                lines.append("|------|------|---------|-----|-------|")
            lines.append(
                f"| {purchase['hour']} | {purchase['hall']} | {purchase['tickets']} | "
                f"₪{purchase['cost']:,.0f} | {purchase.get('seat_range', '—')} |"
            )

    lines.extend(["", "## Totals", ""])
    for owner_data in data["owners"]:
        lines.append(
            f"- {owner_data['holder']}: {owner_data['total_tickets']} tickets, "
            f"₪{owner_data['total_cost']:,.0f} "
            f"({len(owner_data['purchases'])} screenings)"
        )

    conflicts = data.get("cross_owner_conflicts", [])
    lines.extend(["", "## Cross-owner conflicts", ""])
    if not conflicts:
        lines.append("No cross-owner screening conflicts.")
    else:
        lines.append(f"{len(conflicts)} screening(s) shared by multiple card owners:")
        lines.append("")
        lines.append(
            "| Day | Hour | Hall | Screening | Planned | Seat ranges (disjoint) |"
        )
        lines.append(
            "|-----|------|------|-----------|---------|-------------------------|"
        )
        owner_by_holder = {o["holder"]: o for o in data["owners"]}
        capacity = 0
        for conflict in conflicts:
            title = conflict["title"]
            if len(title) > 18:
                title = title[:15] + "..."
            seat_parts = []
            for entry in sorted(
                conflict["owners"],
                key=lambda e: OWNER_ORDER.get(e["holder"], 999),
            ):
                holder = entry["holder"]
                seat_range = seat_range_for_owner(
                    owner_by_holder.get(holder, {}), conflict["screening_id"]
                )
                seat_parts.append(f"{holder}: {seat_range}")
            if conflict.get("severity") == "CAPACITY" or conflict.get(
                "total_tickets", 0
            ) > conflict.get("free_seats", 999):
                capacity += 1
            lines.append(
                f"| {conflict['cinema_day']} | {conflict['hour']} | {conflict['hall']} | "
                f"#{conflict['screening_id']} {title} | {conflict['total_tickets']} | "
                f"{'; '.join(seat_parts)} |"
            )
        parallel = len(conflicts) - capacity
        lines.append("")
        lines.append(
            f"Summary: {capacity} over-capacity, {parallel} shared screening(s) "
            f"with disjoint seat blocks assigned (owner order: "
            f"{', '.join(o['holder'] for o in OWNERS)})."
        )

    lines.extend(
        [
            "",
            "_Regenerate plan JSON: "
            "`python3 scripts/bulk_purchase_program.py --seed 42 --keep-yesterday --json`",
            "",
            "_Render this file: "
            "`python3 scripts/bulk_purchase_program.py --export-markdown logs/tc13/plan_seed42.json`",
            "",
            "_Execute: `python3 scripts/execute_tc13.py --parallel`_",
            "",
        ]
    )
    return "\n".join(lines)


def export_markdown(json_path: str, output_path: str) -> None:
    with open(json_path, encoding="utf-8") as f:
        data = json.load(f)
    markdown = render_markdown_plan(
        data, source_note=f"`{json_path}` (executed TC-13 plan)"
    )
    with open(output_path, "w", encoding="utf-8") as f:
        f.write(markdown)


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate bulk ticket purchase programs")
    parser.add_argument("--bridge", default=BRIDGE_DEFAULT, help="sql-bridge base URL")
    parser.add_argument("--seed", type=int, default=RANDOM_SEED, help="RNG seed")
    parser.add_argument("--json", action="store_true", help="Output JSON only")
    parser.add_argument(
        "--no-ensure-screenings",
        action="store_true",
        help="Skip inserting missing screenings before planning",
    )
    parser.add_argument(
        "--dry-run-seed",
        action="store_true",
        help="Report seeding only; do not insert screenings",
    )
    parser.add_argument(
        "--keep-yesterday",
        action="store_true",
        help="Do not delete yesterday's cinema-day screenings from the DB",
    )
    parser.add_argument(
        "--compact",
        action="store_true",
        help="Compact plan: hour, hall, tickets, sum only",
    )
    parser.add_argument(
        "--export-markdown",
        metavar="PLAN.json",
        help="Write TC13_PURCHASE_PLAN.md from a plan JSON file",
    )
    parser.add_argument(
        "--markdown-output",
        default="TC13_PURCHASE_PLAN.md",
        help="Output path for --export-markdown (default: TC13_PURCHASE_PLAN.md)",
    )
    args = parser.parse_args()

    if args.export_markdown:
        export_markdown(args.export_markdown, args.markdown_output)
        print(f"Wrote {args.markdown_output}")
        return 0

    removed_day = None
    removed_count = 0
    if not args.keep_yesterday:
        removed_day, removed_count, _ = remove_yesterday_screenings(args.bridge)

    rng = random.Random(args.seed)

    movie_genres: Dict[int, Set[str]] = defaultdict(set)
    genres_raw = bridge_query(
        args.bridge,
        "SELECT mg.IdMovie, g.Name AS Genre FROM MovieGenres mg JOIN Genres g ON mg.IdGenre = g.Id",
    )
    for row in genres_raw["rows"]:
        movie_genres[row["IdMovie"]].add(row["Genre"])

    seeded = 0
    staggered = 0
    if not args.no_ensure_screenings:
        seeded = ensure_screenings(
            args.bridge,
            movie_genres,
            dry_run=args.dry_run_seed,
        )
        staggered = ensure_staggered_slots(
            args.bridge,
            movie_genres,
            dry_run=args.dry_run_seed,
        )

    screenings, movie_genres = load_data(args.bridge)

    if removed_day:
        screenings = [s for s in screenings if s.cinema_day != removed_day]

    if not screenings:
        print("No upcoming screenings with free seats.", file=sys.stderr)
        return 1

    cinema_days = sorted({s.cinema_day for s in screenings})
    matching_movies = {
        s.title
        for s in screenings
        if any(
            movie_matches(s.genres, set(o["genres"]))
            for o in OWNERS
        )
    }

    programs = [
        build_program(owner, screenings, cinema_days, rng) for owner in OWNERS
    ]
    hall_dims = load_hall_dimensions(args.bridge)
    taken_seats = load_taken_seats(args.bridge)
    assign_disjoint_seat_ranges(programs, hall_dims, taken_seats)
    conflicts = find_cross_owner_conflicts(programs)

    if args.json:
        payload = {
            "cinema_days": cinema_days,
            "ticket_price": TICKET_PRICE,
            "initial_balance": INITIAL_BALANCE,
            "removed_yesterday": removed_day,
            "removed_screenings": removed_count,
            "cross_owner_conflicts": [
                {
                    "screening_id": c.screening_id,
                    "cinema_day": c.cinema_day,
                    "hour": c.hour,
                    "hall": c.hall,
                    "title": c.title,
                    "free_seats": c.free_seats,
                    "total_tickets": c.total_tickets,
                    "owners": [{"holder": h, "tickets": t} for h, t in c.owners],
                    "severity": c.severity,
                }
                for c in conflicts
            ],
            "owners": [
                {
                    "holder": p.holder,
                    "card": p.card,
                    "expiry": next(
                        (o["expiry"] for o in OWNERS if o["holder"] == p.holder), ""
                    ),
                    "cvc": next(
                        (o["cvc"] for o in OWNERS if o["holder"] == p.holder), ""
                    ),
                    "preferred_genres": p.preferred_genres,
                    "tickets_per_screening": [
                        MIN_TICKETS_PER_SCREENING,
                        MAX_TICKETS_PER_SCREENING,
                    ],
                    "total_tickets": p.total_tickets,
                    "total_cost": p.total_cost,
                    "purchases": [
                        {
                            "screening_id": l.screening_id,
                            "cinema_day": l.cinema_day,
                            "hour": screening_hour(l.start_time),
                            "hall": l.hall,
                            "title": l.title,
                            "tickets": l.tickets,
                            "cost": l.cost,
                            "seat_range": l.seat_range,
                            "seats": [
                                {"row": r, "seat": s} for r, s in l.seat_rows
                            ],
                        }
                        for l in p.lines
                    ],
                }
                for p in programs
            ],
        }
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    elif args.compact:
        if removed_day and removed_count:
            print(f"Removed cinema day {removed_day}: {removed_count} screening(s)")
        for program in programs:
            print_compact_program(program)
        print("\n---")
        print("**Totals**")
        for p in programs:
            print(
                f"- {p.holder}: {p.total_tickets} tickets, ₪{p.total_cost:,.0f} "
                f"({len(p.lines)} screenings)"
            )
        print_conflicts(conflicts)
    else:
        print_genre_division()
        print("\n" + "=" * 72)
        print("  AVAILABLE SCHEDULE")
        print("=" * 72)
        if removed_day and removed_count:
            print(f"  Removed cinema day {removed_day}: {removed_count} screening(s)")
        if seeded or staggered:
            print(f"  Screenings added this run: {seeded} new, {staggered} staggered")
        print(f"  Cinema days: {', '.join(cinema_days)} ({len(cinema_days)} days)")
        print(f"  Screenings with free seats: {len(screenings)}")
        print(f"  Movies matching at least one owner genre: {len(matching_movies)}")

        for program in programs:
            print_program(program)

        print("\n" + "=" * 72)
        print("  SUMMARY")
        print("=" * 72)
        for p in programs:
            print(
                f"  {p.holder:20}  {p.total_tickets:2} tickets  "
                f"₪{p.total_cost:7,.0f}  ({len(p.lines)} screenings)"
            )
        print_conflicts(conflicts)

    all_errors: List[str] = []
    for program in programs:
        errs = validate_program(program)
        all_errors.extend(f"{program.holder}: {e}" for e in errs)
    all_errors.extend(validate_cross_owner_conflicts(conflicts))
    all_errors.extend(validate_seat_assignments(programs))
    if all_errors:
        print("\nVALIDATION ERRORS:", file=sys.stderr)
        for e in all_errors:
            print(f"  - {e}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
