# Title menu art (magazine-on-desk)

Source pack: `title_menu_assets.zip` — English snake_case filenames (no Chinese rename needed).

Runtime load path: `Resources.Load` via `VnArt.GetTitle(nameWithoutExtension)`.
Authoring mirror: `Assets/Art/UI/Title/` (same files).

## Layers

| File | Role |
|---|---|
| `title_desk_bg.png` | Full-screen wooden desk (1920×1080) |
| `title_magazine_shadow.png` | Magazine drop shadow (offset slightly; ~40% alpha) |
| `title_magazine_open.png` | Blank open magazine |
| `title_feature_art.png` | Left-page noir cat interview art |
| `title_logo_cn.png` | Chinese title「街角专访」 |
| `title_logo_en.png` | English strip + mic |
| `title_quote_box_l.png` | Left-page quote frame |
| `title_blurb_deco.png` | Cat mark + decorative rules |
| `title_contents_header.png` | Right-page header rule |
| `title_txt_contents.png` | Pre-rendered “CONTENTS” (logo ink / SimHei) |
| `title_txt_subtitle.png` | Left-page magazine blurb |
| `title_txt_tagline.png` | Right-page chapter line |
| `title_txt_tagline_cleared.png` | “存档已清除” feedback |
| `title_btn_01_newgame.png` … `title_btn_05_exit.png` | Tape button labels (indexed) |
| `btn_tape_primary_idle.png` / `_hover.png` | Orange primary tape button |
| `btn_tape_idle.png` / `_hover.png` / `_pressed.png` | Beige secondary tape button |
| `deco_paperclip.png` | Clip on primary button |
| `icon_play.png` | 新游戏 |
| `icon_cassette.png` | 继续 |
| `icon_map.png` | 读档 (Archive → load; no chapter system) |
| `icon_doc.png` | 清除存档 |
| `icon_gear.png` | Settings (unused — no settings screen yet) |
| `icon_exit.png` | 退出 |
| `prop_translator.png` | Desk prop |
| `prop_field_notes.png` | Desk prop (click → 笔记) |
| `prop_polaroid_a.png` / `prop_polaroid_b.png` | Desk props |
| `prop_scraps.png` | Desk prop |
| `title_menu_assets_contact_sheet.png` | Reference only (kept under `Art/UI/Title`) |

## Unity import

- Texture Type: Sprite (2D and UI), Alpha Is Transparency
- Text sprites: uncompressed preferred; mipmaps off (see `.meta`)
- Buttons use independent Button rects + pre-rendered label sprites (`title_btn_*`); Text fallback if missing
