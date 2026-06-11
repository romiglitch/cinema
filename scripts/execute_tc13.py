#!/usr/bin/env python3
"""
Execute TC-13 bulk purchases — one session per card owner, bulkTesting=1 (no receipt emails).

Usage:
  python3 scripts/execute_tc13.py --parallel          # 4 owners in parallel
  python3 scripts/execute_tc13.py --owner "ISRAEL ISRAELI"
"""

from __future__ import annotations

import argparse
import json
import multiprocessing as mp
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from datetime import datetime
from http.cookiejar import CookieJar
from pathlib import Path
from typing import Dict, List, Optional, Tuple

BASE_DEFAULT = "http://100.94.185.70:50594"
BRIDGE_DEFAULT = "http://100.94.185.70:8765"
EMAIL = "kladnitsky.romi@gmail.com"
PASSWORD = "1234"
MAX_TICKETS = 10
LOG_DIR = Path(__file__).resolve().parent.parent / "logs" / "tc13"

VS_RE = re.compile(r'id="__VIEWSTATE" value="([^"]*)"')
VSG_RE = re.compile(r'id="__VIEWSTATEGENERATOR" value="([^"]*)"')
EV_RE = re.compile(r'id="__EVENTVALIDATION" value="([^"]*)"')
SEAT_RE = re.compile(
    r'data-value="([^"]+)"[^>]*data-row="(\d+)"[^>]*data-seat="(\d+)"'
    r'|data-row="(\d+)"[^>]*data-seat="(\d+)"[^>]*data-value="([^"]+)"'
)


@dataclass
class PurchaseBatch:
    screening_id: int
    title: str
    cinema_day: str
    hour: str
    hall: int
    seats: List[Tuple[int, int]]


class Logger:
    def __init__(self, path: Path):
        path.parent.mkdir(parents=True, exist_ok=True)
        self.path = path
        self._fp = path.open("a", encoding="utf-8")

    def log(self, msg: str) -> None:
        line = f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] {msg}"
        print(line, flush=True)
        self._fp.write(line + "\n")
        self._fp.flush()

    def close(self) -> None:
        self._fp.close()


class CinemaClient:
    def __init__(self, base: str, bulk_testing: bool = True):
        self.base = base.rstrip("/")
        self.jar = CookieJar()
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(self.jar)
        )
        self.bulk_testing = bulk_testing

    def _url(self, path: str, query: Optional[Dict[str, str]] = None) -> str:
        url = f"{self.base}{path}"
        if query:
            url += "?" + urllib.parse.urlencode(query)
        return url

    def get(self, path: str, query: Optional[Dict[str, str]] = None) -> Tuple[str, str]:
        url = self._url(path, query)
        req = urllib.request.Request(url, method="GET")
        with self.opener.open(req, timeout=90) as resp:
            body = resp.read().decode("utf-8", errors="replace")
            return resp.geturl(), body

    def post(
        self,
        path: str,
        data: Dict[str, str],
        query: Optional[Dict[str, str]] = None,
    ) -> Tuple[str, str]:
        url = self._url(path, query)
        encoded = urllib.parse.urlencode(data).encode()
        req = urllib.request.Request(
            url, data=encoded, method="POST", headers={"Content-Type": "application/x-www-form-urlencoded"}
        )
        with self.opener.open(req, timeout=120) as resp:
            body = resp.read().decode("utf-8", errors="replace")
            return resp.geturl(), body

    @staticmethod
    def parse_fields(html: str) -> Dict[str, str]:
        vs = VS_RE.search(html)
        vsg = VSG_RE.search(html)
        ev = EV_RE.search(html)
        if not vs or not vsg or not ev:
            raise RuntimeError("Missing WebForms hidden fields")
        return {
            "__VIEWSTATE": vs.group(1),
            "__VIEWSTATEGENERATOR": vsg.group(1),
            "__EVENTVALIDATION": ev.group(1),
        }

    def login(self, log: Logger) -> None:
        q = {"bulkTesting": "1"} if self.bulk_testing else None
        _, html = self.get("/Login.aspx", q)
        fields = self.parse_fields(html)
        fields.update(
            {
                "ctl00$ContentPlaceHolder1$TxtEmail": EMAIL,
                "ctl00$ContentPlaceHolder1$TxtPassword": PASSWORD,
                "ctl00$ContentPlaceHolder1$btnLogin": "להתחבר",
            }
        )
        url, html = self.post("/Login.aspx", fields, q)
        if "התנתקות" not in html:
            raise RuntimeError("Login failed")
        log.log("Logged in (bulkTesting=1)")

    def purchase_batch(
        self,
        batch: PurchaseBatch,
        holder: str,
        card: str,
        expiry: str,
        cvc: str,
        log: Logger,
    ) -> bool:
        sid = batch.screening_id
        n = len(batch.seats)
        log.log(
            f"Purchase screening #{sid} {batch.title} ({batch.cinema_day} {batch.hour}) "
            f"hall {batch.hall} — {n} ticket(s)"
        )

        _, html = self.get("/Ticketing.aspx", {"screeningId": str(sid)})
        if "אזלו הכרטיסים" in html:
            log.log(f"  FAIL sold out screening #{sid}")
            return False
        fields = self.parse_fields(html)

        ctl_idx = 0
        while True:
            ctl = f"{ctl_idx:02d}"
            qty_name = f"ctl00$ContentPlaceHolder1$RepeaterTickets$ctl{ctl}$hiddenQty"
            price_name = f"ctl00$ContentPlaceHolder1$RepeaterTickets$ctl{ctl}$hiddenPrice"
            type_name = f"ctl00$ContentPlaceHolder1$RepeaterTickets$ctl{ctl}$hiddenType"
            if qty_name not in html:
                break
            price_m = re.search(
                rf'ctl{ctl}\$hiddenPrice" id="[^"]*" value="([^"]*)"',
                html,
            )
            type_m = re.search(
                rf'ctl{ctl}\$hiddenType" id="[^"]*" value="([^"]*)"',
                html,
            )
            if not price_m or not type_m:
                break
            fields[qty_name] = str(n if ctl_idx == 0 else 0)
            fields[price_name] = price_m.group(1)
            fields[type_name] = type_m.group(1)
            ctl_idx += 1

        if ctl_idx == 0:
            log.log("  FAIL no ticket types on Ticketing page")
            return False

        fields["ctl00$ContentPlaceHolder1$btnContinue"] = "המשך"
        url, html = self.post("/Ticketing.aspx", fields, {"screeningId": str(sid)})
        if "SeatsPicker" not in html and "SeatsPicker" not in url:
            log.log(f"  FAIL did not reach SeatsPicker (url={url})")
            return False

        _, html = self.get("/SeatsPicker.aspx", {"screeningId": str(sid)})
        seat_map: Dict[Tuple[int, int], str] = {}
        for m in SEAT_RE.finditer(html):
            if m.group(1):
                val, row, seat = m.group(1), int(m.group(2)), int(m.group(3))
            else:
                row, seat, val = int(m.group(4)), int(m.group(5)), m.group(6)
            seat_map[(row, seat)] = val

        selected: List[str] = []
        for row, seat in batch.seats:
            key = (row, seat)
            if key not in seat_map:
                log.log(f"  FAIL seat R{row}-S{seat} not found or taken")
                return False
            selected.append(seat_map[key])

        fields = self.parse_fields(html)
        fields["SelectedSeats"] = ",".join(selected)
        fields["ctl00$ContentPlaceHolder1$btnConfirm"] = "אישור מושבים"
        url, html = self.post("/SeatsPicker.aspx", fields, {"screeningId": str(sid)})
        if "Cart.aspx" not in url and 'txtCardNum' not in html:
            log.log(f"  FAIL did not reach Cart (url={url})")
            return False

        _, html = self.get("/Cart.aspx")
        fields = self.parse_fields(html)
        fields.update(
            {
                "ctl00$ContentPlaceHolder1$txtHolderName": holder,
                "ctl00$ContentPlaceHolder1$txtCardNum": card,
                "ctl00$ContentPlaceHolder1$txtExpiry": expiry,
                "ctl00$ContentPlaceHolder1$txtCVV": cvc,
                "ctl00$ContentPlaceHolder1$BtnPay": "בצע תשלום עכשיו",
            }
        )
        url, html = self.post("/Cart.aspx", fields)
        ok = "Success.aspx" in url or "ההזמנה הושלמה" in html
        if ok:
            log.log(f"  OK paid {n} ticket(s)")
        else:
            snippet = html[:200].replace("\n", " ")
            log.log(f"  FAIL payment url={url} body={snippet!r}")
        return ok


def chunk_seats(seats: List[Tuple[int, int]], size: int) -> List[List[Tuple[int, int]]]:
    return [seats[i : i + size] for i in range(0, len(seats), size)]


def bridge_query(base: str, sql: str, payment: bool = False) -> dict:
    endpoint = f"{base.rstrip('/')}/query-payment" if payment else f"{base.rstrip('/')}/query"
    req = urllib.request.Request(
        endpoint,
        data=json.dumps({"sql": sql}).encode(),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=60) as resp:
        return json.load(resp)


def reset_balances(bridge: str, cards: List[str], balance: float, log: Optional[Logger] = None) -> None:
    for card in cards:
        sql = f"UPDATE DebitCards SET Balance = {balance:.2f} WHERE CardNumber = '{card}'"
        bridge_query(bridge, sql, payment=True)
    msg = f"Reset {len(cards)} card balance(s) to ₪{balance:,.0f}"
    if log:
        log.log(msg)
    else:
        print(msg)


def load_plan(repo_root: Path, bridge: str, seed: int, keep_yesterday: bool) -> dict:
    script = repo_root / "scripts" / "bulk_purchase_program.py"
    cmd = [sys.executable, str(script), "--json", "--seed", str(seed), "--bridge", bridge]
    if keep_yesterday:
        cmd.append("--keep-yesterday")
    proc = subprocess.run(cmd, capture_output=True, text=True, check=False)
    if proc.returncode != 0:
        print(proc.stderr, file=sys.stderr)
        raise RuntimeError("bulk_purchase_program.py failed")
    return json.loads(proc.stdout)


def owner_slug(holder: str) -> str:
    return holder.lower().replace(" ", "_")


def execute_owner(
    owner: dict,
    base: str,
    log_path: Path,
) -> Tuple[str, int, int]:
    log = Logger(log_path)
    log.log(f"=== TC-13 owner session: {owner['holder']} ===")
    client = CinemaClient(base, bulk_testing=True)
    ok_count = 0
    fail_count = 0

    try:
        client.login(log)
        batches: List[PurchaseBatch] = []
        for purchase in owner["purchases"]:
            seats = [(s["row"], s["seat"]) for s in purchase["seats"]]
            for chunk in chunk_seats(seats, MAX_TICKETS):
                batches.append(
                    PurchaseBatch(
                        screening_id=int(purchase["screening_id"]),
                        title=purchase.get("title", ""),
                        cinema_day=purchase["cinema_day"],
                        hour=purchase["hour"],
                        hall=int(purchase["hall"]),
                        seats=chunk,
                    )
                )

        log.log(f"Planned {len(owner['purchases'])} screening(s), {len(batches)} checkout(s)")
        for i, batch in enumerate(batches, 1):
            log.log(f"--- checkout {i}/{len(batches)} ---")
            success = False
            for attempt in range(1, 4):
                try:
                    if client.purchase_batch(
                        batch,
                        owner["holder"],
                        owner["card"],
                        owner["expiry"],
                        owner["cvc"],
                        log,
                    ):
                        success = True
                        ok_count += 1
                        break
                    if attempt < 3:
                        log.log(f"  retry {attempt + 1}/3 after failure")
                        time.sleep(1)
                except Exception as exc:
                    log.log(f"  ERROR {exc}")
                    if attempt < 3:
                        log.log(f"  retry {attempt + 1}/3 after error")
                        time.sleep(1)
            if not success:
                fail_count += 1
            time.sleep(0.3)

        log.log(f"=== DONE {owner['holder']}: {ok_count} ok, {fail_count} failed ===")
    except Exception as exc:
        log.log(f"FATAL {exc}")
        fail_count += 1
    finally:
        log.close()

    return owner["holder"], ok_count, fail_count


def _worker(args: Tuple[dict, str, str]) -> Tuple[str, int, int]:
    owner, base, log_path_str = args
    return execute_owner(owner, base, Path(log_path_str))


def main() -> int:
    parser = argparse.ArgumentParser(description="Execute TC-13 bulk purchases")
    parser.add_argument("--base", default=BASE_DEFAULT)
    parser.add_argument("--bridge", default=BRIDGE_DEFAULT)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--keep-yesterday", action="store_true")
    parser.add_argument("--owner", help="Run single owner by holder name")
    parser.add_argument("--parallel", action="store_true", help="Run all 4 owners in parallel")
    parser.add_argument("--plan-file", help="Use existing plan JSON instead of regenerating")
    parser.add_argument("--skip-reset", action="store_true")
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parent.parent
    LOG_DIR.mkdir(parents=True, exist_ok=True)

    if args.plan_file:
        plan = json.loads(Path(args.plan_file).read_text(encoding="utf-8"))
    else:
        print("Generating purchase plan…")
        plan = load_plan(repo_root, args.bridge, args.seed, args.keep_yesterday)
        plan_path = LOG_DIR / f"plan_seed{args.seed}.json"
        plan_path.write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"Plan saved to {plan_path}")

    owners = plan["owners"]
    if args.owner:
        owners = [o for o in owners if o["holder"] == args.owner]
        if not owners:
            print(f"Unknown owner: {args.owner}", file=sys.stderr)
            return 1

    if not args.skip_reset:
        reset_balances(
            args.bridge,
            [o["card"] for o in plan["owners"]],
            float(plan.get("initial_balance", 32000)),
        )

    started = datetime.now()
    summary_path = LOG_DIR / "summary.log"
    with summary_path.open("a", encoding="utf-8") as summary:
        summary.write(f"\n[{started.strftime('%Y-%m-%d %H:%M:%S')}] TC-13 run started\n")

    if len(owners) == 1:
        results = [
            execute_owner(
                owners[0],
                args.base,
                LOG_DIR / f"{owner_slug(owners[0]['holder'])}.log",
            )
        ]
    else:
        worker_args = [
            (
                owner,
                args.base,
                str(LOG_DIR / f"{owner_slug(owner['holder'])}.log"),
            )
            for owner in owners
        ]
        with mp.Pool(processes=min(4, len(owners))) as pool:
            results = pool.map(_worker, worker_args)

    total_ok = sum(r[1] for r in results)
    total_fail = sum(r[2] for r in results)
    elapsed = datetime.now() - started

    lines = [
        f"TC-13 finished in {elapsed}",
        f"Checkouts: {total_ok} ok, {total_fail} failed",
    ]
    for holder, ok, fail in results:
        lines.append(f"  {holder}: {ok} ok, {fail} failed")
    report = "\n".join(lines)
    print("\n" + report)
    with summary_path.open("a", encoding="utf-8") as summary:
        summary.write(report + "\n")

    return 0 if total_fail == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
