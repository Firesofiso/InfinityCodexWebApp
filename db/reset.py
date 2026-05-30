"""
Fresh database reset. Run from anywhere inside the repo.

  python db/reset.py

Steps:
  1. Back up app.db → app.db.YYYYMMDD_HHMMSS.bak (skipped if no db exists)
  2. Apply all EF migrations via `dotnet ef database update`
  3. Seed placeholder DKP users via import_dkp_balances.py
"""

from __future__ import annotations

import subprocess
import sys
from datetime import datetime
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DB_PATH = REPO_ROOT / "app.db"


def backup_database() -> None:
    if not DB_PATH.exists():
        print("No existing database found — skipping backup.")
        return

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_path = DB_PATH.with_suffix(f".db.{timestamp}.bak")
    DB_PATH.rename(backup_path)
    print(f"Backed up: app.db -> {backup_path.name}")


def run_migrations() -> None:
    print("Running migrations...")
    result = subprocess.run(
        ["dotnet", "ef", "database", "update"],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        print(result.stderr)
        sys.exit(1)
    print("  Migrations applied.")


def run_import() -> None:
    print("Importing DKP balances...")
    import import_dkp_balances
    import_dkp_balances.main()


if __name__ == "__main__":
    backup_database()
    run_migrations()
    run_import()
    print("Done.")
