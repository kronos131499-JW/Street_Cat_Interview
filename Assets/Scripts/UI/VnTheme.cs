using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Immersive VN palette: evening street / magazine ink.
    /// Avoids purple gradients and cream-terracotta broadsheet clichés.
    /// </summary>
    public static class VnTheme
    {
        public static readonly Color BgTop = new Color(0.05f, 0.09f, 0.14f, 1f);
        public static readonly Color BgMid = new Color(0.08f, 0.10f, 0.13f, 1f);
        public static readonly Color BgBottom = new Color(0.11f, 0.08f, 0.07f, 1f);
        public static readonly Color Accent = new Color(0.93f, 0.64f, 0.30f, 1f);
        public static readonly Color AccentDim = new Color(0.93f, 0.64f, 0.30f, 0.22f);
        public static readonly Color AccentSoft = new Color(0.93f, 0.64f, 0.30f, 0.4f);
        public static readonly Color DialoguePanel = new Color(0.045f, 0.05f, 0.065f, 0.92f);
        public static readonly Color DialogueEdge = new Color(0.93f, 0.64f, 0.30f, 0.65f);
        public static readonly Color NamePlate = new Color(0.12f, 0.10f, 0.08f, 0.98f);
        public static readonly Color TextPrimary = new Color(0.96f, 0.94f, 0.90f, 1f);
        public static readonly Color TextMuted = new Color(0.68f, 0.66f, 0.62f, 1f);
        public static readonly Color TextInner = new Color(0.76f, 0.84f, 0.90f, 1f);
        public static readonly Color TextSystem = new Color(0.88f, 0.76f, 0.42f, 1f);
        public static readonly Color Button = new Color(0.13f, 0.14f, 0.17f, 0.96f);
        public static readonly Color ButtonHover = new Color(0.20f, 0.17f, 0.13f, 1f);
        public static readonly Color ButtonPrimary = new Color(0.18f, 0.14f, 0.10f, 0.98f);
        public static readonly Color StageWash = new Color(0.04f, 0.06f, 0.09f, 0.35f);
        public static readonly Color TopBar = new Color(0.03f, 0.04f, 0.055f, 0.72f);
        public static readonly Color InputBg = new Color(0.09f, 0.095f, 0.11f, 0.98f);
        public static readonly Color ChoicePanel = new Color(0.07f, 0.075f, 0.09f, 0.88f);
        public static readonly Color Letterbox = new Color(0.02f, 0.02f, 0.03f, 0.92f);
        public static readonly Color Paper = new Color(0.10f, 0.09f, 0.08f, 0.94f);
        public static readonly Color OverlayDim = new Color(0.02f, 0.025f, 0.04f, 0.72f);

        // Layout fractions (screen space, bottom=0 top=1)
        public const float LetterboxH = 0.045f;
        public const float DialogueTop = 0.28f;
        public const float ChoiceBottom = 0.295f;
        public const float ChoiceTop = 0.64f;
        public const float StageCenterY = 0.68f;
        public const float TopHudBottom = 0.935f;

        public static Texture2D VerticalGradient(Color top, Color bottom, int h = 96)
        {
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                // ease for richer dusk
                float e = t * t * (3f - 2f * t);
                var c = Color.Lerp(bottom, top, e);
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply(false, false);
            return tex;
        }

        public static Texture2D TripleGradient(Color top, Color mid, Color bottom, int h = 128)
        {
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color c = t < 0.55f
                    ? Color.Lerp(bottom, mid, t / 0.55f)
                    : Color.Lerp(mid, top, (t - 0.55f) / 0.45f);
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply(false, false);
            return tex;
        }

        public static Texture2D SoftVignette(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float cx = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / cx;
                float dy = (y - cx) / cx;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((d - 0.35f) / 0.85f);
                a = a * a * 0.75f;
                tex.SetPixel(x, y, new Color(0, 0, 0, a));
            }
            tex.Apply(false, false);
            return tex;
        }

        public static Sprite SpriteFromTexture(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Color AtmosphereForLocation(string label)
        {
            if (string.IsNullOrEmpty(label)) return StageWash;
            if (label.Contains("杂志") || label.Contains("工位") || label.Contains("办公室"))
                return new Color(0.08f, 0.09f, 0.12f, 0.4f);
            if (label.Contains("傍晚") || label.Contains("社区"))
                return new Color(0.12f, 0.08f, 0.06f, 0.38f);
            if (label.Contains("午后"))
                return new Color(0.10f, 0.11f, 0.10f, 0.32f);
            if (label.Contains("翻译") || label.Contains("采访"))
                return new Color(0.07f, 0.10f, 0.12f, 0.42f);
            return StageWash;
        }
    }
}
