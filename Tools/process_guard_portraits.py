# -*- coding: utf-8 -*-
"""Normalize 保安大叔 portraits to a shared 1024x1536 transparent canvas.

Some source expressions shipped as 1536x1024 landscape plates with black
studio fill; others are already 1024x1536. With GameUI preserveAspect that
makes dialogue portraits jump in on-screen size when the expression changes.
"""
from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter, ImageChops

ROOT = Path(__file__).resolve().parents[1]
SRC_DIR = ROOT / "Assets" / "Art" / "Characters" / "保安大叔立绘"
ART_OUT = ROOT / "Assets" / "Art" / "Characters"
RES_OUT = ROOT / "Assets" / "Resources" / "VnArt" / "Characters"

STATE_MAP = {
    "常态": "ch_guard_default",
    "疑惑": "ch_guard_puzzled",
    "苦笑": "ch_guard_wry",
    "回忆": "ch_guard_recall",
}

CANVAS = (1024, 1536)
TARGET_CONTENT_H = 1420
TOP_MARGIN = 40


def find_sources() -> list[tuple[Path, str]]:
    out: list[tuple[Path, str]] = []
    for p in sorted(SRC_DIR.glob("保安大叔_*.png")):
        state = p.stem.split("_", 1)[1] if "_" in p.stem else "常态"
        out.append((p, state))
    return out


def bg_like(r: int, g: int, b: int, a: int) -> bool:
    if a < 12:
        return True
    # Solid / near-black studio plates used on landscape exports
    if max(r, g, b) <= 18:
        return True
    return False


def flood_alpha_mask(im: Image.Image) -> Image.Image:
    im = im.convert("RGBA")
    w, h = im.size
    raw = im.tobytes()
    n = w * h
    is_bg = bytearray(n)

    for i in range(n):
        o = i * 4
        r, g, b, a = raw[o], raw[o + 1], raw[o + 2], raw[o + 3]
        if bg_like(r, g, b, a):
            is_bg[i] = 1

    visited = bytearray(n)
    q: deque[int] = deque()

    def try_seed(x: int, y: int) -> None:
        i = y * w + x
        if is_bg[i] and not visited[i]:
            visited[i] = 1
            q.append(i)

    for x in range(w):
        try_seed(x, 0)
        try_seed(x, h - 1)
    for y in range(h):
        try_seed(0, y)
        try_seed(w - 1, y)

    while q:
        i = q.popleft()
        x = i % w
        y = i // w
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if nx < 0 or ny < 0 or nx >= w or ny >= h:
                continue
            j = ny * w + nx
            if visited[j] or not is_bg[j]:
                continue
            visited[j] = 1
            q.append(j)

    mask_bytes = bytearray(n)
    for i in range(n):
        mask_bytes[i] = 0 if visited[i] else 255

    mask = Image.frombytes("L", (w, h), bytes(mask_bytes))
    mask = mask.filter(ImageFilter.MaxFilter(3))
    mask = mask.filter(ImageFilter.GaussianBlur(radius=1.25))
    return mask


def apply_mask(im: Image.Image, mask: Image.Image) -> Image.Image:
    im = im.convert("RGBA")
    r, g, b, a = im.split()
    new_a = ImageChops.multiply(a, mask)
    return Image.merge("RGBA", (r, g, b, new_a))


def content_bbox(im: Image.Image, alpha_thresh: int = 12) -> tuple[int, int, int, int]:
    a = im.split()[-1]
    binmask = a.point(lambda v: 255 if v > alpha_thresh else 0)
    box = binmask.getbbox()
    if box is None:
        return (0, 0, im.width, im.height)
    return box


def normalize_to_canvas(im: Image.Image) -> Image.Image:
    box = content_bbox(im)
    cropped = im.crop(box)
    cw, ch = cropped.size
    # Landscape plates need >1.4× upscale to match portrait exports.
    scale = TARGET_CONTENT_H / float(ch)
    scale = max(0.5, min(2.2, scale))
    nw = max(1, int(round(cw * scale)))
    nh = max(1, int(round(ch * scale)))

    # Guard expressions can be wider (question mark etc.); fit width if needed.
    if nw > CANVAS[0] - 8:
        scale *= (CANVAS[0] - 8) / float(nw)
        nw = max(1, int(round(cw * scale)))
        nh = max(1, int(round(ch * scale)))
    # Re-fit height after a width clamp so all expressions share visual stature.
    if nh > TARGET_CONTENT_H:
        scale *= TARGET_CONTENT_H / float(nh)
        nw = max(1, int(round(cw * scale)))
        nh = max(1, int(round(ch * scale)))
    elif nh < TARGET_CONTENT_H * 0.96 and nw < CANVAS[0] - 8:
        bump = min(TARGET_CONTENT_H / float(nh), (CANVAS[0] - 8) / float(nw))
        scale *= bump
        nw = max(1, int(round(cw * scale)))
        nh = max(1, int(round(ch * scale)))

    resized = cropped.resize((nw, nh), Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    x = (CANVAS[0] - nw) // 2
    y = TOP_MARGIN
    if y + nh > CANVAS[1]:
        y = max(0, CANVAS[1] - nh)
        if y == 0 and nh > CANVAS[1]:
            resized = resized.crop((0, nh - CANVAS[1], nw, nh))
            nh = resized.size[1]
    canvas.paste(resized, (x, y), resized)
    return canvas


def process_one(path: Path, state: str) -> list[str]:
    print(f"Processing {path.name} → {state}")
    im = Image.open(path).convert("RGBA")
    print(f"  in {im.size}")

    mask = flood_alpha_mask(im)
    cut = apply_mask(im, mask)
    norm = normalize_to_canvas(cut)
    px = norm.load()
    print(f"  out {norm.size} TL={px[0, 0]} bbox={content_bbox(norm)}")

    key = STATE_MAP.get(state)
    if not key:
        print(f"  WARN unknown state {state}")
        return []

    ART_OUT.mkdir(parents=True, exist_ok=True)
    RES_OUT.mkdir(parents=True, exist_ok=True)

    written: list[str] = []
    art_path = ART_OUT / f"{key}.png"
    res_path = RES_OUT / f"{key}.png"
    zh_out = SRC_DIR / path.name

    def atomic_save(path: Path) -> None:
        tmp = path.with_suffix(path.suffix + ".tmp")
        norm.save(tmp, "PNG")
        try:
            tmp.replace(path)
        except OSError:
            path.write_bytes(tmp.read_bytes())
            try:
                tmp.unlink()
            except OSError:
                pass

    atomic_save(zh_out)
    atomic_save(art_path)
    atomic_save(res_path)
    print(f"  wrote {key}.png + {path.name}")
    written.extend([key, path.name])
    return written


def main() -> None:
    sources = find_sources()
    print("Sources:", [(p.name, s) for p, s in sources])
    if not sources:
        raise SystemExit("No 保安大叔_*.png found")
    all_written: list[str] = []
    for path, state in sources:
        all_written.extend(process_one(path, state))
    print("DONE:", all_written)


if __name__ == "__main__":
    main()
