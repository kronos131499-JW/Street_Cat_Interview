using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Central factory / styling helpers for in-game TextMeshPro UI.
    /// Prefer these over scattering <see cref="TextMeshProUGUI"/> setup.
    /// </summary>
    public static class VnText
    {
        static bool loggedMissingFont;

        public static TextAlignmentOptions ToAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.TopLeft;
            }
        }

        public static TextMeshProUGUI Create(
            Transform parent,
            string name,
            TMP_FontAsset font,
            int size,
            TextAnchor align,
            Color color,
            Vector2 pos,
            Vector2 sizeDelta,
            bool wrap = true,
            bool raycastTarget = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<TextMeshProUGUI>();
            ApplyDefaults(t, font, size, align, color, wrap, raycastTarget);
            return t;
        }

        public static TextMeshProUGUI Add(
            GameObject go,
            TMP_FontAsset font,
            int size,
            TextAnchor align,
            Color color,
            bool wrap = true,
            bool raycastTarget = false)
        {
            var t = go.GetComponent<TextMeshProUGUI>();
            if (t == null) t = go.AddComponent<TextMeshProUGUI>();
            ApplyDefaults(t, font, size, align, color, wrap, raycastTarget);
            return t;
        }

        public static void ApplyDefaults(
            TextMeshProUGUI t,
            TMP_FontAsset font,
            int size,
            TextAnchor align,
            Color color,
            bool wrap,
            bool raycastTarget = false)
        {
            if (t == null) return;
            if (font == null)
            {
                // ResolveActive is cached inside TmpFontCatalog — safe; do not recreate atlases here.
                font = Loc.TmpFontCatalog.ResolveActive();
                if (font == null && !loggedMissingFont)
                {
                    loggedMissingFont = true;
                    Debug.LogError("[VnText] No TMP_FontAsset available — text will render as □ / default LiberationSans.");
                }
            }
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = ToAlignment(align);
            t.enableWordWrapping = wrap;
            t.overflowMode = wrap ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
            t.richText = true;
            t.raycastTarget = raycastTarget;
            t.extraPadding = true;
        }

        public static void SetWrap(TMP_Text t, bool wrap, TextOverflowModes overflow = TextOverflowModes.Truncate)
        {
            if (t == null) return;
            t.enableWordWrapping = wrap;
            t.overflowMode = overflow;
        }

        public static void SetOverflow(TMP_Text t, bool wrap, bool truncate)
        {
            if (t == null) return;
            t.enableWordWrapping = wrap;
            t.overflowMode = truncate ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
        }

        public static void SetAutoSize(TMP_Text t, float min, float max)
        {
            if (t == null) return;
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
        }

        public static void ClearAutoSize(TMP_Text t)
        {
            if (t == null) return;
            t.enableAutoSizing = false;
        }

        public static void ApplyLetterSpacing(TMP_Text t, float pixelSpacing)
        {
            if (t == null) return;
            float size = t.fontSize > 0.5f ? t.fontSize : 24f;
            t.characterSpacing = Loc.TmpFontCatalog.PixelSpacingToCharacterSpacing(pixelSpacing, size);
        }

        public static void SetFontStyle(TMP_Text t, bool bold)
        {
            if (t == null) return;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        }

        /// <summary>Simple single-line TMP input (interview / talk).</summary>
        public static TMP_InputField CreateInput(
            Transform parent,
            string name,
            TMP_FontAsset font,
            int fontSize,
            Color textColor,
            Color placeholderColor,
            string placeholderText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = VnTheme.InputBg;

            var textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaGo.transform.SetParent(go.transform, false);
            var areaRt = textAreaGo.GetComponent<RectTransform>();
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(14f, 6f);
            areaRt.offsetMax = new Vector2(-14f, -6f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textAreaGo.transform, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            var text = textGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaults(text, font, fontSize, TextAnchor.MiddleLeft, textColor, wrap: false, raycastTarget: false);
            text.richText = false;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(textAreaGo.transform, false);
            StretchFull(phGo.GetComponent<RectTransform>());
            var ph = phGo.AddComponent<TextMeshProUGUI>();
            ApplyDefaults(ph, font, fontSize, TextAnchor.MiddleLeft, placeholderColor, wrap: false, raycastTarget: false);
            ph.text = placeholderText;
            ph.fontStyle = FontStyles.Italic;

            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = font;
            input.pointSize = fontSize;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            return input;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
