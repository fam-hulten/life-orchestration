#!/usr/bin/env python3
"""Decision Logger CLI - Log, list, and search decisions."""

import sqlite3
import sys
import os
import argparse
from datetime import datetime
from pathlib import Path

DB_PATH = Path(__file__).parent / "decisions.db"

def init_db():
    """Initialize the database."""
    conn = sqlite3.connect(DB_PATH)
    conn.execute("""
        CREATE TABLE IF NOT EXISTS decisions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            decision TEXT NOT NULL,
            context TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)
    conn.commit()
    return conn

def add(conn, decision: str, context: str = None):
    """Add a new decision."""
    cursor = conn.execute(
        "INSERT INTO decisions (decision, context) VALUES (?, ?)",
        (decision, context)
    )
    conn.commit()
    print(f"✅ Decision #{cursor.lastrowid} logged: {decision[:60]}{'...' if len(decision) > 60 else ''}")

def list_decisions(conn, limit: int = 50):
    """List all decisions."""
    cursor = conn.execute(
        "SELECT id, decision, context, created_at FROM decisions ORDER BY created_at DESC LIMIT ?",
        (limit,)
    )
    rows = cursor.fetchall()
    if not rows:
        print("No decisions logged yet.")
        return
    for row in rows:
        print(f"\n#{row[0]} | {row[3]}")
        print(f"  {row[1]}")
        if row[2]:
            print(f"  [Context: {row[2]}]")

def search(conn, term: str):
    """Search decisions."""
    cursor = conn.execute(
        "SELECT id, decision, context, created_at FROM decisions WHERE decision LIKE ? OR context LIKE ? ORDER BY created_at DESC",
        (f"%{term}%", f"%{term}%")
    )
    rows = cursor.fetchall()
    if not rows:
        print(f"No decisions found matching: {term}")
        return
    print(f"Found {len(rows)} decision(s):\n")
    for row in rows:
        print(f"#{row[0]} | {row[3]}")
        print(f"  {row[1]}")
        if row[2]:
            print(f"  [Context: {row[2]}]")
        print()

def main():
    parser = argparse.ArgumentParser(description="Decision Logger CLI")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_add = sub.add_parser("add", help="Add a new decision")
    p_add.add_argument("decision", help="The decision text")
    p_add.add_argument("--context", "-c", help="Context or background")

    p_list = sub.add_parser("list", help="List all decisions")
    p_list.add_argument("--limit", "-n", type=int, default=50, help="Max results")

    p_search = sub.add_parser("search", help="Search decisions")
    p_search.add_argument("term", help="Search term")

    args = parser.parse_args()

    conn = init_db()
    try:
        if args.cmd == "add":
            add(conn, args.decision, args.context)
        elif args.cmd == "list":
            list_decisions(conn, args.limit)
        elif args.cmd == "search":
            search(conn, args.term)
    finally:
        conn.close()

if __name__ == "__main__":
    main()
