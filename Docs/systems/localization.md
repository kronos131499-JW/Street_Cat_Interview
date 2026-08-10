# Localization (Phase 1)

## Languages

- `zh` (default) / `en`
- Persisted in PlayerPrefs key `sci.lang`
- Switch in **Settings** (title menu or pause menu)

## Files

| Path | Role |
|------|------|
| `Assets/Scripts/Loc/GameSettings.cs` | Language, BGM/SFX, text speed, auto-play, fullscreen; font/size/spacing keyed per language (`sci.font.zh` / `sci.font.en`, …) |
| `Assets/Scripts/Loc/FontCatalog.cs` | Bundled font list + resolve |
| Settings → Font | Cycle faces; writes to the **current** language slot (ZH default SiYuan, EN Barlow) |

Bundled files live in `Assets/Resources/Fonts/`. Latin display fonts lack Chinese glyphs — use **System CJK** for Chinese playtests.

**Barlow Condensed / Lora** use larger size scale (~1.18–1.22) and extra letter spacing via `UILetterSpacing`.
| `Assets/Scripts/Loc/ScriptLoc.cs` | Script overlay by `sceneId:lineIndex` |
| `Assets/Resources/Loc/ui_zh.json` / `ui_en.json` | Shell UI |
| `Assets/Resources/Loc/scripts_en.json` | Chapter 1 fixed dialogue EN |
| `Tools/gen_scripts_en.py` | Regenerates `scripts_en.json` from `BuiltInScripts.cs` |

## Speaker names (EN)

小凌→Ling, 沈禾→Shen He, 保安叔叔→Uncle Guard, 大福→Dafu, 林女士→Ms. Lin, 系统→System

## Deferred (still Chinese in EN mode)

Investigation / talk beats, interview rule engines & LLM prompts, notebook content, writing materials, epilogue.

## Regenerate script EN stubs

```text
python Tools/gen_scripts_en.py
```

(Requires editing the `TR` map in that script when Chinese lines change.)
