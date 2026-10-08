using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>Shared ink chips for the text-style and dialogue-color tools.</summary>
    public static class InkSwatches
    {
        public struct Swatch
        {
            public string Zh;
            public string En;
            public string ShortZh;
            public string ShortEn;
            public Color Color;
        }

        public static readonly Swatch[] All =
        {
            new Swatch
            {
                Zh = "纯黑", En = "Black", ShortZh = "纯黑", ShortEn = "Black",
                Color = Color.black
            },
            new Swatch
            {
                Zh = "深褐（推荐正文）", En = "Deep brown (body)", ShortZh = "深褐", ShortEn = "Brown",
                Color = new Color(42f / 255f, 18f / 255f, 2f / 255f, 1f) // #2A1202
            },
            new Swatch
            {
                Zh = "近黑暖墨", En = "Warm near-black", ShortZh = "近黑", ShortEn = "Near",
                Color = new Color(0.05f, 0.04f, 0.03f, 1f) // #0D0A08
            },
            new Swatch
            {
                Zh = "软褐", En = "Soft umber", ShortZh = "软褐", ShortEn = "Umber",
                Color = new Color(42f / 255f, 34f / 255f, 28f / 255f, 1f) // #2A221C
            },
            new Swatch
            {
                Zh = "冷石板（内心）", En = "Cool slate (inner)", ShortZh = "石板", ShortEn = "Slate",
                Color = new Color(20f / 255f, 22f / 255f, 26f / 255f, 1f) // #14161A
            },
            new Swatch
            {
                Zh = "焦糖棕（系统）", En = "Walnut (system)", ShortZh = "焦糖", ShortEn = "Walnut",
                Color = new Color(0.28f, 0.12f, 0.04f, 1f) // #471F0A
            },
        };

        /// <summary>Labeled chips. Clicking writes the swatch RGB and keeps the current alpha.</summary>
        public static bool DrawRow(ref Color color)
        {
            EditorGUILayout.LabelField(ToolLang.T("墨色预设", "Ink presets"));
            EditorGUILayout.BeginHorizontal();
            var hit = DrawChips(ref color, labeled: true);
            EditorGUILayout.EndHorizontal();
            var matched = Match(color);
            if (matched.HasValue)
            {
                var s = All[matched.Value];
                EditorGUILayout.LabelField(
                    ToolLang.T(s.Zh, s.En) + "  #" + ColorUtility.ToHtmlStringRGB(s.Color),
                    EditorStyles.miniLabel);
            }
            return hit;
        }

        /// <summary>Compact squares for a color-field row.</summary>
        public static bool DrawMini(ref Color color)
        {
            return DrawChips(ref color, labeled: false);
        }

        static bool DrawChips(ref Color color, bool labeled)
        {
            var hit = false;
            for (var i = 0; i < All.Length; i++)
            {
                var s = All[i];
                var selected = ColorsClose(color, s.Color);
                if (labeled)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(58f));
                    var square = DrawSquare(s, selected, 58f, 16f);
                    var pressed = GUILayout.Button(ToolLang.T(s.ShortZh, s.ShortEn), EditorStyles.miniButton);
                    if (square || pressed)
                    {
                        Apply(ref color, s);
                        hit = true;
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    if (DrawSquare(s, selected, 18f, 18f))
                    {
                        Apply(ref color, s);
                        hit = true;
                    }
                    GUILayout.Space(2f);
                }
            }
            return hit;
        }

        static void Apply(ref Color color, Swatch s)
        {
            var alpha = color.a;
            color = s.Color;
            color.a = alpha > 0.01f ? alpha : 1f;
            GUI.changed = true;
        }

        static bool DrawSquare(Swatch s, bool selected, float width, float height)
        {
            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            EditorGUI.DrawRect(rect, selected
                ? new Color(0.93f, 0.64f, 0.30f, 1f)
                : new Color(0.32f, 0.32f, 0.32f, 1f));
            EditorGUI.DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), s.Color);
            GUI.Label(rect, new GUIContent("", ToolLang.T(s.Zh, s.En) + "  #" + ColorUtility.ToHtmlStringRGB(s.Color)));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0)
                return false;
            if (!rect.Contains(Event.current.mousePosition))
                return false;
            Event.current.Use();
            return true;
        }

        static int? Match(Color color)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (ColorsClose(color, All[i].Color))
                    return i;
            }
            return null;
        }

        static bool ColorsClose(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.012f
                   && Mathf.Abs(a.g - b.g) < 0.012f
                   && Mathf.Abs(a.b - b.b) < 0.012f;
        }

    }
}
