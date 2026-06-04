#!/usr/bin/env bash
# Query the Cinema SQL Bridge from Mac.
# Usage:
#   ./query.sh "SELECT TOP 5 * FROM Movie"
#   ./query.sh tables
#   ./query.sh schema Movie
#   ./query.sh ping
#
# Set CINEMA_DB_HOST to the Windows machine's IP if not already set.
# Default port: 8765

HOST="${CINEMA_DB_HOST:-}"
PORT="${CINEMA_DB_PORT:-8765}"
BASE="http://${HOST}:${PORT}"

if [ -z "$HOST" ]; then
    echo "Error: Set CINEMA_DB_HOST to the Windows machine's IP address."
    echo "  export CINEMA_DB_HOST=192.168.x.x"
    exit 1
fi

case "${1,,}" in
    ping)
        curl -s "$BASE/ping" | python3 -m json.tool
        ;;
    tables)
        curl -s "$BASE/tables" | python3 -m json.tool
        ;;
    schema)
        if [ -z "$2" ]; then
            echo "Usage: $0 schema <TableName>"
            exit 1
        fi
        curl -s "$BASE/schema?table=$2" | python3 -m json.tool
        ;;
    *)
        curl -s -X POST "$BASE/query" \
            -H "Content-Type: application/json" \
            -d "{\"sql\": $(echo "$1" | python3 -c 'import sys,json; print(json.dumps(sys.stdin.read().strip()))')}" \
            | python3 -m json.tool
        ;;
esac
