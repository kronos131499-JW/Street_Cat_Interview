using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Blank plates for the material-card library.
    /// Source art: Assets/Art/UI/material_cards/ui_backgrounds
    /// Runtime copies: Resources/VnArt/UI/MaterialCards
    /// </summary>
    public static class MaterialCardArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static readonly Color Ink = new Color(0.29f, 0.15f, 0.08f, 1f);
        public static readonly Color Cream = new Color(0.99f, 0.96f, 0.91f, 1f);
        public static readonly Color Hint = new Color(0.27f, 0.16f, 0.09f, 1f);

        public static Sprite ChapterTitle => Load("01_chapter_title_blank");
        public static Sprite Wood => Load("02_wood_background");
        public static Sprite Board => Load("03_main_board");
        public static Sprite Structure => Load("04_structure_panel");
        public static Sprite CardsBacking => Load("05_cards_area_backing");
        public static Sprite Detail => Load("06_detail_panel");
        public static Sprite SectionSelected => Load("13_section_selected");
        public static Sprite SectionNormal => Load("14_section_normal");
        public static Sprite NumberSelected => Load("15_number_tab_selected");
        public static Sprite NumberNormal => Load("16_number_tab_normal");
        public static Sprite IdTag => Load("17_material_id_tag");
        public static Sprite DetailButton => Load("18_detail_button");
        public static Sprite NavBack => Load("19_nav_back");
        public static Sprite NavNotebook => Load("20_nav_notebook");
        public static Sprite NavReturn => Load("21_nav_return");
        public static Sprite PreviewButton => Load("22_button_preview");
        public static Sprite WriteButton => Load("23_button_write");
        public static Sprite SelectionDot => Load("24_selection_dot");
        public static Sprite Underline => Load("25_green_underline");
        public static Sprite Separator => Load("26_detail_separator");

        static readonly string[] CardFiles =
        {
            "07_card_sage",
            "08_card_yellow",
            "09_card_terracotta",
            "10_card_pink",
            "11_card_teal",
            "12_card_periwinkle"
        };

        public static Sprite Card(int visualIndex)
        {
            int n = Mathf.Abs(visualIndex) % CardFiles.Length;
            return Load(CardFiles[n]);
        }

        static Sprite Load(string file)
        {
            if (Cache.TryGetValue(file, out var cached))
                return cached;

            var sprite = VnArt.GetUi("MaterialCards/" + file);
            if (sprite == null)
                sprite = LoadLooseFile(file);
            Cache[file] = sprite;
            return sprite;
        }

        /// <summary>
        /// Editor / unpacked checkout: the plates already live under Assets/Art.
        /// A player build uses the Resources copies instead.
        /// </summary>
        static Sprite LoadLooseFile(string file)
        {
            var path = Path.Combine(Application.dataPath, "Art", "UI", "material_cards", "ui_backgrounds", file + ".png");
            if (!File.Exists(path))
                return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path)))
                return null;

            tex.name = file;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = file;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
