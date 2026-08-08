# -*- coding: utf-8 -*-
"""Remove studio backgrounds from Xiaoling portraits and normalize framing."""
from __future__ import annotations

import shutil
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter, ImageChops

ROOT = Path(r"D:\Street_Cat_Interview\github\Street_Cat_Interview")
SRC_DIR = ROOT / "Assets" / "Art" / "Characters" / "小凌立绘"
ART_OUT = ROOT / "Assets" / "Art" / "Characters"
RES_OUT = ROOT / "Assets" / "Resources" / "VnArt" / "Characters"

STATE_MAP = {
    "常态": "ch_xiaoling_default",
    "惊讶": "ch_xiaoling_surprised",
    "思考": "ch_xiaoling_thinking",
    "认真": "ch_xiaoling_serious",
    "局促": "ch_xiaoling_worried",
    "吐槽": "ch_xiaoling_smile",
}

# Extra English aliases for clarity / future script tags
EXTRA_ALIASES = {
    "局促": ["ch_xiaoling_awkward"],
    "吐槽": ["ch_xiaoling_sassy"],
}

CANVAS = (1024, 1536)
TARGET_CONTENT_H = 1420
TOP_MARGIN = 40


def find_sources() -> list[tuple[Path, str]]:
    files = sorted(SRC_DIR.glob("小凌-*.png"))
    out = []
    for p in files:
        state = p.stem.split("-", 1)[1] if "-" in p.stem else "常态"
        out.append((p, state))
    return out


def bg_like(r: int, g: int, b: int, a: int) -> bool:
    if a < 12:
        return True
    mx, mn = max(r, g, b), min(r, g, b)
    chroma = mx - mn
    # white / light studio
    if chroma <= 22 and mn >= 205:
        return True
    # soft grey / blue-grey gradient
    if chroma <= 28 and 155 <= mn <= 250:
        # protect pale skin (warmer, higher R relative to B)
        if r >= g and r > b + 12 and r >= 175 and g >= 130:
            return False
        # protect warm jacket tones
        if r > g + 20 and r > b + 20 and r >= 120:
            return False
        return True
    return False


def flood_alpha_mask(im: Image.Image) -> Image.Image:
    """Edge flood-fill background → soft alpha mask (255 keep)."""
    im = im.convert("RGBA")
    w, h = im.size
    raw = im.tobytes()  # RGBA interleaved
    n = w * h
    is_bg = bytearray(n)  # 1 = bg candidate

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

    # keep = not flooded bg
    mask_bytes = bytearray(n)
    for i in range(n):
        mask_bytes[i] = 0 if visited[i] else 255

    mask = Image.frombytes("L", (w, h), bytes(mask_bytes))
    # Grow character 1px so hair fringes aren't eaten, then soft blur
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


def strip_watermark(im: Image.Image) -> Image.Image:
    """Clear faint Doubao watermark strip in bottom-right if present."""
    w, h = im.size
    x0, y0 = int(w * 0.70), int(h * 0.90)
    px = im.load()
    for y in range(y0, h):
        for x in range(x0, w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            chroma = max(r, g, b) - min(r, g, b)
            # grey/dark text-like pixels with low chroma on near-empty area
            if chroma <= 30 and a < 220 and max(r, g, b) < 200:
                px[x, y] = (r, g, b, 0)
            elif chroma <= 25 and max(r, g, b) < 160 and a < 255:
                px[x, y] = (r, g, b, 0)
    return im


def normalize_to_canvas(im: Image.Image) -> Image.Image:
    box = content_bbox(im)
    cropped = im.crop(box)
    cw, ch = cropped.size
    scale = TARGET_CONTENT_H / float(ch)
    scale = max(0.5, min(1.4, scale))
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


def already_mostly_transparent(im: Image.Image) -> bool:
    w, h = im.size
    px = im.load()
    corners = [px[0, 0], px[w - 1, 0], px[0, h - 1], px[w - 1, h - 1], px[w // 2, 0]]
    return sum(1 for c in corners if c[3] < 8) >= 3


def process_one(path: Path, state: str) -> list[str]:
    print(f"Processing {path.name} → {state}")
    im = Image.open(path).convert("RGBA")
    print(f"  in {im.size}")

    if already_mostly_transparent(im):
        print("  corners already transparent; refining residual BG only")
        # still flood residual light fringes
        mask = flood_alpha_mask(im)
        cut = apply_mask(im, mask)
    else:
        mask = flood_alpha_mask(im)
        cut = apply_mask(im, mask)

    cut = strip_watermark(cut)
    norm = normalize_to_canvas(cut)

    # verify corners transparent
    px = norm.load()
    print(f"  out {norm.size} TL={px[0, 0]} TR={px[norm.width-1, 0]}")

    key = STATE_MAP.get(state)
    if not key:
        print(f"  WARN unknown state {state}")
        return []

    ART_OUT.mkdir(parents=True, exist_ok=True)
    RES_OUT.mkdir(parents=True, exist_ok=True)
    SRC_DIR.mkdir(parents=True, exist_ok=True)

    written = []
    keys = [key] + EXTRA_ALIASES.get(state, [])
    for k in keys:
        art_path = ART_OUT / f"{k}.png"
        res_path = RES_OUT / f"{k}.png"
        norm.save(art_path, "PNG")
        shutil.copy2(art_path, res_path)
        print(f"  wrote {k}.png")
        written.append(k)

    # overwrite Chinese-named source with cleaned transparent version
    zh_out = SRC_DIR / path.name
    norm.save(zh_out, "PNG")
    written.append(path.name)
    return written


def main() -> None:
    sources = find_sources()
    print("Sources:", [(p.name, s) for p, s in sources])
    if not sources:
        raise SystemExit("No 小凌-*.png found")
    all_written: list[str] = []
    for path, state in sources:
        all_written.extend(process_one(path, state))
    print("DONE:", all_written)


if __name__ == "__main__":
    main()
