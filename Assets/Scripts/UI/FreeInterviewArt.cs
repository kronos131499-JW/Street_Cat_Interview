using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Crops the blank FreeInterview plates down to their painted pixels.
    /// The source PNGs carry a large transparent margin; showing the full texture
    /// makes the paper a sliver inside the control.
    /// Rects are [left, top, right, bottom) in source pixels, origin top-left,
    /// from Assets/Art/UI/FreeInterview/interview_ui_integration.md.
    /// </summary>
    public static class FreeInterviewArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static readonly Color Ink = new Color(0.294f, 0.176f, 0.106f, 1f);

        public static Sprite Photo => Plate("01_photo_background", 1154, 1363, 78, 73, 1122, 1333);
        public static Sprite DialogueCream => Plate("02_dialogue_cream_blank", 2146, 733, 40, 245, 2117, 519);
        public static Sprite DialoguePeach => Plate("03_dialogue_peach_blank", 2170, 725, 56, 265, 2119, 536);
        public static Sprite NameTab => Plate("04_speaker_name_tab", 2022, 778, 158, 107, 1885, 671);
        public static Sprite QuestionCard => Plate("05_question_card_blank", 1774, 887, 52, 110, 1730, 796);
        public static Sprite Input => Plate("06_question_input_blank", 2172, 724, 55, 230, 2112, 496);
        public static Sprite StatusPanel => Plate("07_status_panel_blank", 1156, 1361, 88, 15, 1139, 1353);
        public static Sprite TrustIcon => Plate("08_trust_heart_icon", 1339, 1175, 141, 151, 1207, 1080);
        public static Sprite PressureIcon => Plate("09_pressure_bolt_icon", 1274, 1235, 295, 86, 1047, 1179);
        public static Sprite FocusIcon => Plate("10_focus_binoculars_icon", 1536, 1024, 205, 150, 1329, 926);
        public static Sprite BarEmpty => Plate("11_status_bar_empty", 1942, 809, 52, 297, 1890, 517);
        public static Sprite TrustFill => Plate("12_trust_fill_full", 2172, 724, 57, 196, 2115, 528);
        public static Sprite PressureFill => Plate("13_pressure_fill_full", 1983, 793, 60, 292, 1923, 503);
        public static Sprite FocusFill => Plate("14_focus_fill_full", 2087, 753, 41, 253, 2048, 500);

        static Sprite Plate(string file, int srcW, int srcH, int left, int top, int right, int bottom) =>
            Get(file, srcW, srcH, left, top, right, bottom, Vector4.zero);

        static Sprite Slice(string file, int srcW, int srcH, int left, int top, int right, int bottom,
            float borderL, float borderB, float borderR, float borderT) =>
            Get(file, srcW, srcH, left, top, right, bottom, new Vector4(borderL, borderB, borderR, borderT));

        static Sprite Get(string file, int srcW, int srcH, int left, int top, int right, int bottom, Vector4 border01)
        {
            if (Cache.TryGetValue(file, out var cached))
                return cached;

            var src = VnArt.GetUi("FreeInterview/" + file);
            if (src == null || src.texture == null)
            {
                Cache[file] = null;
                return null;
            }

            var tex = src.texture;
            float sx = tex.width / (float)Mathf.Max(1, srcW);
            float sy = tex.height / (float)Mathf.Max(1, srcH);
            float w = Mathf.Max(1f, (right - left) * sx);
            float h = Mathf.Max(1f, (bottom - top) * sy);
            float x = Mathf.Clamp(left * sx, 0f, tex.width - 1f);
            float y = Mathf.Clamp(tex.height - bottom * sy, 0f, tex.height - 1f);
            w = Mathf.Min(w, tex.width - x);
            h = Mathf.Min(h, tex.height - y);

            var border = new Vector4(border01.x * w, border01.y * h, border01.z * w, border01.w * h);
            var sprite = Sprite.Create(tex, new Rect(x, y, w, h), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = file;
            sprite.hideFlags = HideFlags.DontSave;
            Cache[file] = sprite;
            return sprite;
        }
    }
}
