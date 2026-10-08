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
        public static Sprite PickedMark => Load("选中素材卡icon", "");

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

        /// <param name="subfolder">Folder under Art/UI/material_cards ("" = the folder itself).</param>
        static Sprite Load(string file, string subfolder = "ui_backgrounds")
        {
            if (Cache.TryGetValue(file, out var cached))
                return cached;

            var sprite = VnArt.GetUi("MaterialCards/" + file);
            if (sprite == null)
                sprite = LoadLooseFile(file, subfolder);
            Cache[file] = sprite;
            return sprite;
        }

        /// <summary>
        /// Editor / unpacked checkout: the plates already live under Assets/Art.
        /// A player build uses the Resources copies instead.
        /// </summary>
        static Sprite LoadLooseFile(string file, string subfolder)
        {
            var folder = Path.Combine(Application.dataPath, "Art", "UI", "material_cards");
            if (!string.IsNullOrEmpty(subfolder))
                folder = Path.Combine(folder, subfolder);
            var path = Path.Combine(folder, file + ".png");
            if (!File.Exists(path))
                return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path)))
                return null;

            tex.name = file;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var rect = OpaqueRect(tex);
            var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
            sprite.name = file;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>
        /// Several plates are a thin stroke inside a tall transparent canvas.
        /// Stretching the whole file turns the stroke into a hairline.
        /// </summary>
        static Rect OpaqueRect(Texture2D tex)
        {
            int w = tex.width;
            int h = tex.height;
            var pixels = tex.GetPixels32();
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (pixels[row + x].a <= 16) continue;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < minX || maxY < minY)
                return new Rect(0, 0, w, h);
            const int pad = 2;
            minX = Mathf.Max(0, minX - pad);
            minY = Mathf.Max(0, minY - pad);
            maxX = Mathf.Min(w - 1, maxX + pad);
            maxY = Mathf.Min(h - 1, maxY + pad);
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
