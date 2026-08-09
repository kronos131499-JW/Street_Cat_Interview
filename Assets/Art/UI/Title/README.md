# Title menu art (split from concept)

Concept reference: `title_concept_ref.png`

Generated AI splits (also copied to `Assets/Resources/VnArt/Title/` for runtime load):

| File | Role |
|---|---|
| `title_desk_bg.png` | Empty wooden desk background |
| `title_magazine_open.png` | Blank open magazine (may still have faint page marks — prefer `title_hero_composite` for quick plug-in) |
| `title_feature_art.png` | Left-page noir cat interview illustration |
| `title_hero_composite.png` | Desk + magazine + props composite; right page kept relatively clear for UI buttons |
| `btn_tape_primary_idle.png` | Orange primary menu strip (AI may bake English label — crop/mask or replace later) |
| `btn_tape_idle.png` | Beige secondary menu strip |
| `prop_translator.png` | Cat-language translator prop |
| `prop_field_notes.png` | Field notes notebook + pen |
| `prop_polaroids.png` | Polaroid stack |
| `title_menu_icons_sheet.png` | Icon sheet (play / cassette / map / doc / gear / exit) — slice in editor |

## Still better as type / handoff
- Chinese logo「街角专访」— use font in Unity, AI hanzi is unreliable
- Clean empty tape strips without English — regenerate or paint out text if needed
- Hover / pressed button states

## Suggested Unity wiring
1. Full-screen `title_hero_composite` (or desk + magazine layers)
2. Overlay 6 buttons using tape sprites + sliced icons + Text labels
3. Optional clickable props on translator / notes
