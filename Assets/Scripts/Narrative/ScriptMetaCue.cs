using System;

namespace StreetCat.Narrative
{
    /// <summary>
    /// Detects production-only script lines (BGM/SFX/background cues) that must not appear in dialogue UI.
    /// </summary>
    public static class ScriptMetaCue
    {
        static readonly string[] Prefixes =
        {
            "[BGM]", "[SFX]", "[BG]",
            "【BGM】", "【BGM：", "【BGM:",
            "【bg】", "【sfx】", "【SE】", "【se】",
            "【背景】", "【背景：", "【背景:",
            "【BG】", "【bg】",
        };

        public static bool IsProductionMetaText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var trimmed = text.Trim();
            foreach (var prefix in Prefixes)
            {
                if (StartsWithIgnoreCase(trimmed, prefix))
                    return true;
            }
            return false;
        }

        public static bool IsStructuredCueSource(ScriptLine line)
        {
            return line != null
                && string.IsNullOrEmpty(line.text)
                && (!string.IsNullOrEmpty(line.bgm)
                    || !string.IsNullOrEmpty(line.sfx)
                    || !string.IsNullOrEmpty(line.background));
        }

        public static string SanitizeDisplayText(string text)
        {
            return IsProductionMetaText(text) ? "" : text;
        }

        public static bool TryParseProductionMeta(string text, out string kind, out string label)
        {
            kind = null;
            label = null;
            if (!IsProductionMetaText(text))
                return false;

            var trimmed = text.Trim();
            foreach (var prefix in Prefixes)
            {
                if (!StartsWithIgnoreCase(trimmed, prefix))
                    continue;

                kind = NormalizeKind(prefix);
                label = trimmed.Length > prefix.Length
                    ? trimmed.Substring(prefix.Length).Trim()
                    : "";
                return true;
            }
            return false;
        }

        static string NormalizeKind(string prefix)
        {
            if (prefix.StartsWith("[", StringComparison.Ordinal))
            {
                var inner = prefix.Substring(1, prefix.Length - 2);
                return inner.ToUpperInvariant();
            }

            var p = prefix;
            if (p.StartsWith("【") && p.Length >= 3)
                p = p.Substring(1);
            p = p.TrimEnd('】', '：', ':');
            if (p.Equals("背景", StringComparison.Ordinal) || p.Equals("BG", StringComparison.OrdinalIgnoreCase))
                return "BG";
            if (p.Equals("SE", StringComparison.OrdinalIgnoreCase) || p.Equals("SFX", StringComparison.OrdinalIgnoreCase))
                return "SFX";
            if (p.Equals("BGM", StringComparison.OrdinalIgnoreCase))
                return "BGM";
            return p.ToUpperInvariant();
        }

        static bool StartsWithIgnoreCase(string text, string prefix)
        {
            return text.Length >= prefix.Length
                && string.Compare(text, 0, prefix, 0, prefix.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
