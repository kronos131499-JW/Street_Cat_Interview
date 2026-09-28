#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Extract portrait zip with correct UTF-8 names and copy into Assets/Art/Characters."""
from __future__ import annotations

import shutil
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets" / "Art" / "Characters"
DEFAULT_ZIP = Path(r"c:\Users\bnndy\Downloads\《街角专访》人物立绘(2).zip")

FOLDER_MAP = {
    "小凌": "小凌立绘",
    "沈禾": "沈禾立绘",
    "保安大叔": "保安大叔立绘",
    "大福": "大福立绘",
    "林女士": "林女士立绘",
}


def fix_zip_name(name: str) -> str:
    for enc in ("cp437", "latin1", "cp1252"):
        try:
            return name.encode(enc).decode("utf-8")
        except UnicodeError:
            continue
    return name


def extract_and_copy(zip_path: Path) -> list[str]:
    if not zip_path.exists():
        raise SystemExit(f"Zip not found: {zip_path}")

    copied: list[str] = []
    with zipfile.ZipFile(zip_path, "r") as z:
        for info in z.infolist():
            if info.is_dir():
                continue
            name = fix_zip_name(info.filename)
            if not name.lower().endswith(".png"):
                continue
            if "__MACOSX" in name or name.endswith(".DS_Store"):
                continue

            parts = Path(name).parts
            # Expect: 《街角专访》人物立绘 / {char} / {file}.png
            if len(parts) < 3:
                continue
            char_folder = parts[-2]
            file_name = parts[-1]
            if file_name.endswith("_2.png") or file_name.endswith("_3.png"):
                print("SKIP variant", file_name)
                continue

            dst_folder = FOLDER_MAP.get(char_folder, char_folder + "立绘")
            dst_dir = ART / dst_folder
            dst_dir.mkdir(parents=True, exist_ok=True)
            dst = dst_dir / file_name

            with z.open(info, "r") as src, open(dst, "wb") as out:
                shutil.copyfileobj(src, out)
            rel = f"{dst_folder}/{file_name}"
            copied.append(rel)
            print("COPY", rel)

    return copied


def main() -> None:
    import sys

    zip_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_ZIP
    copied = extract_and_copy(zip_path)
    print(f"\nDone: {len(copied)} portraits -> Assets/Art/Characters")


if __name__ == "__main__":
    main()
