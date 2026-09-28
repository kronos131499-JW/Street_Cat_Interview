# -*- coding: utf-8 -*-
"""Import revised backgrounds + social UI from artist delivery folder.

Reads Tools/import_revised_art_map.json and converts JPG/PNG → PNG into
Assets/Art and Assets/Resources/VnArt.
"""
from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
sys.stderr.reconfigure(encoding="utf-8", errors="replace")

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SRC = Path(r"D:\街角专访素材\街角专访 改和补充")
MAP_PATH = ROOT / "Tools" / "import_revised_art_map.json"
ART_BG = ROOT / "Assets" / "Art" / "Backgrounds" / "正式背景图"
RES_BG = ROOT / "Assets" / "Resources" / "VnArt" / "Backgrounds"
ART_SOCIAL = ROOT / "Assets" / "Art" / "UI" / "Social"
RES_SOCIAL = ROOT / "Assets" / "Resources" / "VnArt" / "UI" / "Social"


def main() -> None:
    src_root = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    if not src_root.is_dir():
        raise SystemExit(f"Source folder not found: {src_root}")

    mapping = json.loads(MAP_PATH.read_text(encoding="utf-8"))
    # One PowerShell process for all conversions (System.Drawing, no Pillow).
    pairs: list[tuple[str, str]] = []
    for m in mapping:
        src = src_root / m["src"]
        if not src.exists():
            print("MISSING", m["src"])
            continue
        if m.get("art"):
            pairs.append((str(src), str(ART_BG / m["art"])))
            for name in m.get("res") or []:
                pairs.append((str(src), str(RES_BG / name)))
        if m.get("artSocial"):
            pairs.append((str(src), str(ART_SOCIAL / m["artSocial"])))
            for name in m.get("resSocial") or []:
                pairs.append((str(src), str(RES_SOCIAL / name)))
        print("QUEUE", m["src"])

    if not pairs:
        raise SystemExit("Nothing to import.")

    # Encode as JSON for PowerShell to avoid console encoding issues.
    jobs_path = ROOT / "Tools" / "_import_revised_art_jobs.json"
    jobs_path.write_text(json.dumps(pairs, ensure_ascii=False), encoding="utf-8")
    ps = rf"""
Add-Type -AssemblyName System.Drawing
$jobs = Get-Content -LiteralPath '{jobs_path}' -Encoding UTF8 | ConvertFrom-Json
foreach ($j in $jobs) {{
  $from = [string]$j[0]; $to = [string]$j[1]
  $img = [System.Drawing.Image]::FromFile($from)
  try {{ $img.Save($to, [System.Drawing.Imaging.ImageFormat]::Png) }}
  finally {{ $img.Dispose() }}
  Write-Host ('OK ' + (Split-Path $to -Leaf))
}}
"""
    r = subprocess.run(
        ["powershell", "-NoProfile", "-Command", ps],
        cwd=str(ROOT),
    )
    jobs_path.unlink(missing_ok=True)
    if r.returncode != 0:
        raise SystemExit(r.returncode)
    print(f"Done: {len(pairs)} outputs from {src_root}")


if __name__ == "__main__":
    main()
