#!/usr/bin/env python3
"""Generate seamless-ish ambient VN BGM loops (procedural, no external deps)."""
from __future__ import annotations

import math
import os
import struct
import wave

SR = 44100
DUR = 36.0  # seconds
N = int(SR * DUR)


def clamp(x: float, lo: float = -1.0, hi: float = 1.0) -> float:
    return lo if x < lo else hi if x > hi else x


def env_loop(i: int, n: int, fade: int = 2048) -> float:
    """Tiny edge fade so ends meet cleanly."""
    if i < fade:
        return i / fade
    if i > n - fade:
        return (n - i) / fade
    return 1.0


def soft_noise(i: int, seed: int = 1) -> float:
    # deterministic cheap hash noise
    x = (i * 1103515245 + seed * 12345) & 0x7FFFFFFF
    return (x / 0x7FFFFFFF) * 2.0 - 1.0


def tone(i: int, freq: float, amp: float = 1.0) -> float:
    return math.sin(2.0 * math.pi * freq * i / SR) * amp


def write_wav(path: str, samples: list[float]) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "w") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        frames = bytearray()
        for i, s in enumerate(samples):
            # slight stereo width
            l = clamp(s * 0.92 + soft_noise(i, 3) * 0.004)
            r = clamp(s * 0.92 + soft_noise(i, 7) * 0.004)
            frames += struct.pack("<hh", int(l * 30000), int(r * 30000))
        w.writeframes(frames)
    print("wrote", path, f"({len(samples)/SR:.1f}s)")


def pad(freqs: list[tuple[float, float]], slow: float, air: float) -> list[float]:
    out = [0.0] * N
    for i in range(N):
        t = i / SR
        e = env_loop(i, N)
        breath = 0.55 + 0.45 * math.sin(2.0 * math.pi * slow * t)
        s = 0.0
        for f, a in freqs:
            # gentle detune shimmer
            s += tone(i, f, a)
            s += tone(i, f * 1.003, a * 0.35)
            s += tone(i, f * 0.5, a * 0.25)  # sub
        # soft filtered noise air
        s += soft_noise(i, 11) * air * (0.5 + 0.5 * math.sin(2.0 * math.pi * 0.07 * t))
        out[i] = clamp(s * breath * e * 0.22)
    return out


def sprinkle_plucks(buf: list[float], notes: list[float], interval: float, amp: float = 0.18) -> None:
    hits = int(DUR / interval)
    for h in range(hits):
        start = int((h * interval + 0.15) * SR) % N
        freq = notes[h % len(notes)]
        length = int(2.8 * SR)
        for j in range(length):
            idx = (start + j) % N
            decay = math.exp(-j / (SR * 1.1))
            # soft piano-ish: fundamental + partials
            v = (
                tone(j, freq, 1.0)
                + tone(j, freq * 2.01, 0.35)
                + tone(j, freq * 3.02, 0.12)
            ) * decay * amp
            # hammer noise
            v += soft_noise(j, 21) * decay * amp * 0.08
            buf[idx] = clamp(buf[idx] + v * env_loop(idx, N))


def normalize(buf: list[float], peak: float = 0.85) -> list[float]:
    m = max(abs(x) for x in buf) or 1.0
    scale = peak / m
    return [clamp(x * scale) for x in buf]


def midi(n: int) -> float:
    return 440.0 * (2.0 ** ((n - 69) / 12.0))


def build_magazine() -> list[float]:
    # warm minor pad A2 D3 E3 A3
    freqs = [
        (midi(45), 0.9),
        (midi(50), 0.7),
        (midi(52), 0.55),
        (midi(57), 0.45),
    ]
    buf = pad(freqs, slow=0.05, air=0.03)
    sprinkle_plucks(buf, [midi(69), midi(72), midi(71), midi(64), midi(67), midi(69)], interval=6.0, amp=0.14)
    return normalize(buf)


def build_community() -> list[float]:
    # brighter outdoor G major-ish
    freqs = [
        (midi(43), 0.8),
        (midi(50), 0.65),
        (midi(54), 0.5),
        (midi(59), 0.4),
    ]
    buf = pad(freqs, slow=0.07, air=0.05)
    sprinkle_plucks(buf, [midi(67), midi(71), midi(74), midi(71), midi(69), midi(62)], interval=5.0, amp=0.12)
    return normalize(buf)


def build_interview() -> list[float]:
    # sparse, quiet, slightly tense
    freqs = [
        (midi(40), 0.85),
        (midi(47), 0.55),
        (midi(52), 0.4),
        (midi(56), 0.28),
    ]
    buf = pad(freqs, slow=0.035, air=0.02)
    sprinkle_plucks(buf, [midi(64), midi(63), midi(59), midi(56)], interval=9.0, amp=0.09)
    return normalize(buf, peak=0.75)


def build_writing() -> list[float]:
    # night desk drone + soft high sparkles
    freqs = [
        (midi(38), 1.0),
        (midi(45), 0.7),
        (midi(50), 0.45),
        (midi(57), 0.3),
    ]
    buf = pad(freqs, slow=0.04, air=0.025)
    sprinkle_plucks(buf, [midi(74), midi(76), midi(71), midi(69), midi(76)], interval=7.2, amp=0.1)
    return normalize(buf)


def main() -> None:
    root = os.path.join(os.path.dirname(__file__), "..", "Assets")
    targets = [
        ("Audio/Bgm/bgm_magazine.wav", build_magazine),
        ("Audio/Bgm/bgm_community.wav", build_community),
        ("Audio/Bgm/bgm_interview.wav", build_interview),
        ("Audio/Bgm/bgm_writing.wav", build_writing),
    ]
    for rel, fn in targets:
        samples = fn()
        path = os.path.normpath(os.path.join(root, rel))
        write_wav(path, samples)
        # Resources mirror for runtime load
        res = os.path.normpath(os.path.join(root, "Resources", rel))
        write_wav(res, samples)


if __name__ == "__main__":
    main()
