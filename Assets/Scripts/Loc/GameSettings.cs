using System;
using UnityEngine;

namespace StreetCat.Loc
{
    /// <summary>
    /// Persisted player preferences (language, audio, reading, display).
    /// Font family / size / letter-spacing are stored per language.
    /// </summary>
    public static class GameSettings
    {
        const string PrefLang = "sci.lang";
        // Legacy (pre per-language) keys — migrated once into the active language slot.
        const string PrefFontLegacy = "sci.font";
        const string PrefFontSizeLegacy = "sci.fontSize";
        const string PrefLetterSpacingLegacy = "sci.letterSpacing";
        const string PrefBgm = "sci.bgm";
        const string PrefSfx = "sci.sfx";
        const string PrefTextSpeed = "sci.textSpeed";
        const string PrefAutoPlay = "sci.autoPlay";
        const string PrefAutoDelay = "sci.autoDelay";
        const string PrefFullscreen = "sci.fullscreen";

        const string DefaultFontZh = "simhei";
        const string DefaultFontEn = "barlow";

        public const float FontSizeMin = 0.75f;
        public const float FontSizeMax = 1.6f;
        public const float LetterSpacingMin = 0f;
        public const float LetterSpacingMax = 10f;

        public static event Action OnChanged;

        static bool loaded;
        static GameLanguage language = GameLanguage.Zh;
        static string uiFontId = DefaultFontZh;
        static float fontSizeScale = 1.15f;
        static float letterSpacing = 2.5f;
        static float bgmVolume = 0.7f;
        static float sfxVolume = 0.8f;
        static int textSpeed = 1; // 0 slow, 1 normal, 2 fast
        static bool autoPlay;
        static float autoDelay = 1.2f;
        static bool fullscreen = true;

        public static GameLanguage Language
        {
            get { EnsureLoaded(); return language; }
            set
            {
                EnsureLoaded();
                if (language == value) return;
                language = value;
                PlayerPrefs.SetString(PrefLang, value == GameLanguage.En ? "en" : "zh");
                LoadFontProfileFor(language);
                PlayerPrefs.Save();
                UiLoc.Reload();
                ScriptLoc.Reload();
                Notify();
            }
        }

        /// <summary>FontCatalog option id for the active language (system / siyuan / butflow / …).</summary>
        public static string UiFontId
        {
            get { EnsureLoaded(); return uiFontId; }
            set
            {
                EnsureLoaded();
                var id = string.IsNullOrEmpty(value) ? DefaultFontId(language) : value;
                if (!IsKnownFont(id)) id = DefaultFontId(language);
                if (uiFontId == id) return;
                uiFontId = id;
                PlayerPrefs.SetString(PrefFont(language), uiFontId);
                ApplyFontRecommendedMetrics(notify: false);
                PlayerPrefs.Save();
                Notify();
            }
        }

        /// <summary>Dialogue/UI size multiplier for the active language (0.75–1.6).</summary>
        public static float FontSizeScale
        {
            get { EnsureLoaded(); return fontSizeScale; }
            set
            {
                EnsureLoaded();
                float v = Mathf.Clamp(value, FontSizeMin, FontSizeMax);
                if (Mathf.Approximately(fontSizeScale, v)) return;
                fontSizeScale = v;
                PlayerPrefs.SetFloat(PrefFontSize(language), fontSizeScale);
                PlayerPrefs.Save();
                Notify();
            }
        }

        /// <summary>Extra pixels between glyphs for the active language (0–10).</summary>
        public static float LetterSpacing
        {
            get { EnsureLoaded(); return letterSpacing; }
            set
            {
                EnsureLoaded();
                float v = Mathf.Clamp(value, LetterSpacingMin, LetterSpacingMax);
                if (Mathf.Approximately(letterSpacing, v)) return;
                letterSpacing = v;
                PlayerPrefs.SetFloat(PrefLetterSpacing(language), letterSpacing);
                PlayerPrefs.Save();
                Notify();
            }
        }

        public static void CycleUiFont(int delta)
        {
            EnsureLoaded();
            int i = FontCatalog.IndexOf(uiFontId);
            int n = FontCatalog.All.Length;
            if (n <= 0) return;
            i = (i + delta) % n;
            if (i < 0) i += n;
            UiFontId = FontCatalog.All[i].Id;
        }

        static void ApplyFontRecommendedMetrics(bool notify)
        {
            var opt = FontCatalog.Get(uiFontId);
            fontSizeScale = Mathf.Clamp(opt.SizeScale > 0.01f ? opt.SizeScale : 1.15f, FontSizeMin, FontSizeMax);
            letterSpacing = Mathf.Clamp(opt.LetterSpacing, LetterSpacingMin, LetterSpacingMax);
            PlayerPrefs.SetFloat(PrefFontSize(language), fontSizeScale);
            PlayerPrefs.SetFloat(PrefLetterSpacing(language), letterSpacing);
            if (notify)
            {
                PlayerPrefs.Save();
                Notify();
            }
        }

        public static float BgmVolume
        {
            get { EnsureLoaded(); return bgmVolume; }
            set
            {
                EnsureLoaded();
                bgmVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PrefBgm, bgmVolume);
                PlayerPrefs.Save();
                Notify();
            }
        }

        public static float SfxVolume
        {
            get { EnsureLoaded(); return sfxVolume; }
            set
            {
                EnsureLoaded();
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
                PlayerPrefs.Save();
                Notify();
            }
        }

        /// <summary>0=Slow, 1=Normal, 2=Fast</summary>
        public static int TextSpeed
        {
            get { EnsureLoaded(); return textSpeed; }
            set
            {
                EnsureLoaded();
                textSpeed = Mathf.Clamp(value, 0, 2);
                PlayerPrefs.SetInt(PrefTextSpeed, textSpeed);
                PlayerPrefs.Save();
                Notify();
            }
        }

        public static float TypewriterCps => TextSpeed switch
        {
            0 => 22f,
            2 => 90f,
            _ => 42f
        };

        public static bool AutoPlay
        {
            get { EnsureLoaded(); return autoPlay; }
            set
            {
                EnsureLoaded();
                if (autoPlay == value) return;
                autoPlay = value;
                PlayerPrefs.SetInt(PrefAutoPlay, autoPlay ? 1 : 0);
                PlayerPrefs.Save();
                Notify();
            }
        }

        public static float AutoDelay
        {
            get { EnsureLoaded(); return autoDelay; }
            set
            {
                EnsureLoaded();
                autoDelay = Mathf.Clamp(value, 0.3f, 5f);
                PlayerPrefs.SetFloat(PrefAutoDelay, autoDelay);
                PlayerPrefs.Save();
                Notify();
            }
        }

        public static bool Fullscreen
        {
            get { EnsureLoaded(); return fullscreen; }
            set
            {
                EnsureLoaded();
                if (fullscreen == value) return;
                fullscreen = value;
                PlayerPrefs.SetInt(PrefFullscreen, fullscreen ? 1 : 0);
                PlayerPrefs.Save();
                ApplyDisplay();
                Notify();
            }
        }

        public static bool IsEnglish => Language == GameLanguage.En;

        public static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            var lang = PlayerPrefs.GetString(PrefLang, "zh");
            language = lang == "en" ? GameLanguage.En : GameLanguage.Zh;
            MigrateLegacyFontPrefsIfNeeded();
            LoadFontProfileFor(language);

            bgmVolume = PlayerPrefs.HasKey(PrefBgm) ? Mathf.Clamp01(PlayerPrefs.GetFloat(PrefBgm)) : 0.7f;
            sfxVolume = PlayerPrefs.HasKey(PrefSfx) ? Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfx)) : 0.8f;
            textSpeed = PlayerPrefs.HasKey(PrefTextSpeed) ? Mathf.Clamp(PlayerPrefs.GetInt(PrefTextSpeed), 0, 2) : 1;
            autoPlay = PlayerPrefs.GetInt(PrefAutoPlay, 0) == 1;
            autoDelay = PlayerPrefs.HasKey(PrefAutoDelay)
                ? Mathf.Clamp(PlayerPrefs.GetFloat(PrefAutoDelay), 0.3f, 5f)
                : 1.2f;
            fullscreen = PlayerPrefs.GetInt(PrefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            ApplyDisplay();
        }

        public static void ApplyDisplay()
        {
            EnsureLoaded();
            if (Screen.fullScreen != fullscreen)
                Screen.fullScreen = fullscreen;
        }

        /// <summary>Master BGM level before per-clip gain (matches prior ~0.38 peak feel at default 0.7).</summary>
        public static float BgmMaster => 0.38f * BgmVolume / 0.7f;

        public static float SfxMaster => 0.45f * SfxVolume / 0.8f;

        static string LangCode(GameLanguage lang) => lang == GameLanguage.En ? "en" : "zh";

        static string PrefFont(GameLanguage lang) => "sci.font." + LangCode(lang);
        static string PrefFontSize(GameLanguage lang) => "sci.fontSize." + LangCode(lang);
        static string PrefLetterSpacing(GameLanguage lang) => "sci.letterSpacing." + LangCode(lang);

        static string DefaultFontId(GameLanguage lang) =>
            lang == GameLanguage.En ? DefaultFontEn : DefaultFontZh;

        static bool IsKnownFont(string id)
        {
            for (int i = 0; i < FontCatalog.All.Length; i++)
            {
                if (FontCatalog.All[i].Id == id) return true;
            }
            return false;
        }

        /// <summary>
        /// One-time: copy legacy global font keys into the currently saved language slot.
        /// The other language keeps its catalog defaults until the player sets them.
        /// </summary>
        static void MigrateLegacyFontPrefsIfNeeded()
        {
            string fontKey = PrefFont(language);
            if (PlayerPrefs.HasKey(fontKey)) return;
            if (!PlayerPrefs.HasKey(PrefFontLegacy)
                && !PlayerPrefs.HasKey(PrefFontSizeLegacy)
                && !PlayerPrefs.HasKey(PrefLetterSpacingLegacy))
                return;

            string id = PlayerPrefs.GetString(PrefFontLegacy, DefaultFontId(language));
            if (!IsKnownFont(id)) id = DefaultFontId(language);
            PlayerPrefs.SetString(fontKey, id);

            var opt = FontCatalog.Get(id);
            float size = PlayerPrefs.HasKey(PrefFontSizeLegacy)
                ? PlayerPrefs.GetFloat(PrefFontSizeLegacy)
                : opt.SizeScale;
            float spacing = PlayerPrefs.HasKey(PrefLetterSpacingLegacy)
                ? PlayerPrefs.GetFloat(PrefLetterSpacingLegacy)
                : opt.LetterSpacing;
            PlayerPrefs.SetFloat(PrefFontSize(language), Mathf.Clamp(size, FontSizeMin, FontSizeMax));
            PlayerPrefs.SetFloat(PrefLetterSpacing(language), Mathf.Clamp(spacing, LetterSpacingMin, LetterSpacingMax));
            PlayerPrefs.Save();
        }

        static void LoadFontProfileFor(GameLanguage lang)
        {
            string defId = DefaultFontId(lang);
            string id = PlayerPrefs.GetString(PrefFont(lang), defId);
            if (!IsKnownFont(id)) id = defId;
            uiFontId = id;

            var opt = FontCatalog.Get(uiFontId);
            if (PlayerPrefs.HasKey(PrefFontSize(lang)))
                fontSizeScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefFontSize(lang)), FontSizeMin, FontSizeMax);
            else
                fontSizeScale = Mathf.Clamp(opt.SizeScale > 0.01f ? opt.SizeScale : 1.15f, FontSizeMin, FontSizeMax);

            if (PlayerPrefs.HasKey(PrefLetterSpacing(lang)))
                letterSpacing = Mathf.Clamp(PlayerPrefs.GetFloat(PrefLetterSpacing(lang)), LetterSpacingMin, LetterSpacingMax);
            else
                letterSpacing = Mathf.Clamp(opt.LetterSpacing, LetterSpacingMin, LetterSpacingMax);
        }

        static void Notify() => OnChanged?.Invoke();
    }
}
