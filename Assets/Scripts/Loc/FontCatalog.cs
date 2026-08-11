using UnityEngine;

namespace StreetCat.Loc
{
    /// <summary>
    /// Bundled + system font options for UI / dialogue testing.
    /// </summary>
    public static class FontCatalog
    {
        public struct Option
        {
            public string Id;
            public string DisplayName;
            /// <summary>Resources path under Fonts/, or empty for OS CJK fallback.</summary>
            public string ResourcesName;
            public bool LatinOnly;
            /// <summary>Multiply base UI/dialogue font sizes.</summary>
            public float SizeScale;
            /// <summary>Extra pixels between glyphs (mapped to TMP characterSpacing).</summary>
            public float LetterSpacing;
        }

        // All faces share a slightly larger size + open tracking for readability while testing.
        const float S = 1.2f;
        const float Sp = 3f;

        public static readonly Option[] All =
        {
            new Option { Id = "siyuan", DisplayName = "Source Han Sans / 思源黑体", ResourcesName = "SiYuanHeiTi", LatinOnly = false, SizeScale = 1.15f, LetterSpacing = 2.5f },
            // TrueType CJK (optional): StreetCat/Fonts/Import Windows SimHei — more reliable for TMP FontEngine than CFF OTF.
            new Option { Id = "simhei", DisplayName = "SimHei / 黑体", ResourcesName = "SimHei", LatinOnly = false, SizeScale = 1.15f, LetterSpacing = 2.2f },
            new Option { Id = "system", DisplayName = "System CJK", ResourcesName = "", LatinOnly = false, SizeScale = 1.15f, LetterSpacing = 2.2f },
            new Option { Id = "butflow", DisplayName = "Butflow", ResourcesName = "Butflow", LatinOnly = true, SizeScale = S, LetterSpacing = Sp },
            new Option { Id = "papernotes", DisplayName = "Papernotes", ResourcesName = "Papernotes", LatinOnly = true, SizeScale = S, LetterSpacing = Sp },
            new Option { Id = "papernotes_bold", DisplayName = "Papernotes Bold", ResourcesName = "PapernotesBold", LatinOnly = true, SizeScale = S, LetterSpacing = Sp },
            new Option { Id = "papernotes_sketch", DisplayName = "Papernotes Sketch", ResourcesName = "PapernotesSketch", LatinOnly = true, SizeScale = S, LetterSpacing = Sp },
            new Option { Id = "elegant_bloom", DisplayName = "Elegant Bloom", ResourcesName = "ElegantBloom", LatinOnly = true, SizeScale = S, LetterSpacing = Sp },
            new Option { Id = "barlow", DisplayName = "Barlow Condensed", ResourcesName = "BarlowCondensed", LatinOnly = true, SizeScale = 1.22f, LetterSpacing = 3.5f },
            new Option { Id = "barlow_semibold", DisplayName = "Barlow Condensed SemiBold", ResourcesName = "BarlowCondensedSemiBold", LatinOnly = true, SizeScale = 1.22f, LetterSpacing = 3.5f },
            new Option { Id = "barlow_bold", DisplayName = "Barlow Condensed Bold", ResourcesName = "BarlowCondensedBold", LatinOnly = true, SizeScale = 1.24f, LetterSpacing = 3.3f },
            new Option { Id = "lora", DisplayName = "Lora", ResourcesName = "Lora", LatinOnly = true, SizeScale = 1.2f, LetterSpacing = 3f },
            new Option { Id = "lora_medium", DisplayName = "Lora Medium", ResourcesName = "LoraMedium", LatinOnly = true, SizeScale = 1.2f, LetterSpacing = 3f },
            new Option { Id = "lora_semibold", DisplayName = "Lora SemiBold", ResourcesName = "LoraSemiBold", LatinOnly = true, SizeScale = 1.22f, LetterSpacing = 2.8f },
            new Option { Id = "lora_bold", DisplayName = "Lora Bold", ResourcesName = "LoraBold", LatinOnly = true, SizeScale = 1.22f, LetterSpacing = 2.8f },
            new Option { Id = "helvetica", DisplayName = "Helvetica", ResourcesName = "Helvetica", LatinOnly = true, SizeScale = 1.18f, LetterSpacing = 3f },
            new Option { Id = "helvetica_bold", DisplayName = "Helvetica Bold", ResourcesName = "HelveticaBold", LatinOnly = true, SizeScale = 1.2f, LetterSpacing = 2.8f },
            new Option { Id = "verdana", DisplayName = "Verdana", ResourcesName = "Verdana", LatinOnly = true, SizeScale = 1.15f, LetterSpacing = 2.5f },
            new Option { Id = "verdana_bold", DisplayName = "Verdana Bold", ResourcesName = "VerdanaBold", LatinOnly = true, SizeScale = 1.18f, LetterSpacing = 2.5f },
        };

        public static int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return i;
            }
            return 0;
        }

        public static Option Get(string id) => All[IndexOf(id)];

        public static Font Resolve(string id)
        {
            var opt = Get(id);
            if (!string.IsNullOrEmpty(opt.ResourcesName))
            {
                var bundled = Resources.Load<Font>("Fonts/" + opt.ResourcesName);
                if (bundled != null) return bundled;
                Debug.LogWarning("[FontCatalog] Missing Resources/Fonts/" + opt.ResourcesName);
            }

            return ResolveSystemCjk();
        }

        public static Font ResolveSystemCjk()
        {
            var names = new[]
            {
                "Microsoft YaHei UI", "Microsoft YaHei", "微软雅黑",
                "SimHei", "黑体", "PingFang SC",
                "Noto Sans CJK SC", "Source Han Sans SC"
            };
            foreach (var name in names)
            {
                try
                {
                    var f = Font.CreateDynamicFontFromOSFont(name, 32);
                    if (f != null) return f;
                }
                catch
                {
                    // ignore
                }
            }
            var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtin != null ? builtin : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
