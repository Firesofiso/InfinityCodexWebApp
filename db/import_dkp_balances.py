from __future__ import annotations

import csv
import sqlite3
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parent
DB_PATH = ROOT.parent / "app.db"
DKP_FILE = ROOT / "old-dkp.txt"


def main() -> None:
    rows = read_dkp_file()
    connection = sqlite3.connect(DB_PATH)
    connection.row_factory = sqlite3.Row

    try:
        connection.execute("PRAGMA foreign_keys = ON;")
        connection.execute("BEGIN TRANSACTION;")

        inserted = 0
        skipped = 0

        for name, dkp_total in rows:
            existing_user = connection.execute(
                "SELECT Id FROM Users WHERE DisplayName = ? AND IsImported = 1",
                (name,),
            ).fetchone()

            if existing_user is not None:
                skipped += 1
                continue

            discord_id = str(uuid.uuid4())
            connection.execute(
                """
                INSERT INTO Users (
                    DiscordId, DisplayName, Role,
                    IsActive, IsRegistrationComplete, IsImported,
                    DkpBalance, CreatedAt
                )
                VALUES (?, ?, 'member', 1, 0, 1, ?, CURRENT_TIMESTAMP)
                """,
                (discord_id, name, dkp_total),
            )
            user_id = connection.execute("SELECT last_insert_rowid()").fetchone()[0]

            connection.execute(
                """
                INSERT INTO Characters (Name, OwnerUserId, IsActive, IsMain, DataSource)
                VALUES (?, ?, 1, 1, 'import')
                """,
                (name, user_id),
            )

            inserted += 1

        connection.commit()
        print(f"  Inserted: {inserted}")
        print(f"  Skipped (already present): {skipped}")

    except Exception:
        connection.rollback()
        raise
    finally:
        connection.close()


def read_dkp_file() -> list[tuple[str, int]]:
    results = []
    with DKP_FILE.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.reader(handle, delimiter="\t")
        next(reader)  # skip header
        for row in reader:
            if len(row) < 2:
                continue
            name = row[0].strip()
            dkp_total = int(row[1].strip())
            if name:
                results.append((name, dkp_total))
    return results


if __name__ == "__main__":
    main()
