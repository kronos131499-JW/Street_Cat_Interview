using System;
using UnityEngine;

namespace StreetCat.Loc
{
    /// <summary>
    /// Persisted player preferences (language, audio, reading, display).
    /// Font family / size / letter-spacing are stored per language.
    /// Supports a "last session" profile and a player-saved "defaults" profile.
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
        const string PrefPictureHold = "sci.pictureHold";
        const string PrefFullscreen = "sci.fullscreen";
        const string PrefRememberLast = "sci.rememberLast";
        const string PrefHasCustomDefaults = "sci.default.has";

        // Player-saved defaults snapshot (from "Save as default").
        const string PrefDefLang = "sci.default.lang";
        const string PrefDefBgm = "sci.default.bgm";
        const string PrefDefSfx = "sci.default.sfx";
        const string PrefDefTextSpeed = "sci.default.textSpeed";
        const string PrefDefAutoPlay = "sci.default.autoPlay";
        const string PrefDefAutoDelay = "sci.default.autoDelay";
        const string PrefDefPictureHold = "sci.default.pictureHold";
        const string PrefDefFullscreen = "sci.default.fullscreen";
        const string PrefDefFontZh = "sci.default.font.zh";
        const string PrefDefFontEn = "sci.default.font.en";
        const string PrefDefFontSizeZh = "sci.default.fontSize.zh";
        const string PrefDefFontSizeEn = "sci.default.fontSize.en";
        const string PrefDefLetterSpZh = "sci.default.letterSpacing.zh";
        const string PrefDefLetterSpEn = "sci.default.letterSpacing.en";
        const string PrefDefFontWeightZh = "sci.default.fontWeight.zh";
        const string PrefDefFontWeightEn = "sci.default.fontWeight.en";

        const string DefaultFontZh = "vn_gothic";
        const string DefaultFontEn = "vn_gothic";

        public const float FontSizeMin = 0.75f;
        public const float FontSizeMax = 1.6f;
        public const float LetterSpacingMin = 0f;
        public const float LetterSpacingMax = 10f;
        public const float PictureHoldMin = 0f;
        public const float PictureHoldMax = 5f;
        public const int FontWeightDefault = 500;
        public static readonly int[] FontWeightSteps = { 300, 400, 500, 600, 700, 800 };

        public static event Action OnChanged;

        static bool loaded;
        static GameLanguage language = GameLanguage.Zh;
        static string uiFontId = DefaultFontZh;
        static float fontSizeScale = 1.15f;
        static float letterSpacing = 2.5f;
        static int fontWeight = FontWeightDefault;
        static float bgmVolume = 0.7f;
        static float sfxVolume = 0.8f;
        static int textSpeed = 1; // 0 slow, 1 normal, 2 fast
        static bool autoPlay;
        static float autoDelay = 1.2f;
        static float pictureHold = 1f;
        static bool fullscreen = true;
        static bool rememberLast = true;

        public static GameLanguage Language
        {
            get { EnsureLoaded(); return language; }
            set
            {
                EnsureLoaded();
                if (language == value) return;
                language = value;
                PersistString(PrefLang, value == GameLanguage.En ? "en" : "zh");
                LoadFontProfileFor(language);
                FlushPersist();
                UiLoc.Reload();
                ScriptLoc.Reload();
                HardTextLoc.Reload();
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
                PersistString(PrefFont(language), uiFontId);
                ApplyFontRecommendedMetrics(notify: false);
                FlushPersist();
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
                PersistFloat(PrefFontSize(language), fontSizeScale);
                FlushPersist();
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
                PersistFloat(PrefLetterSpacing(language), letterSpacing);
                FlushPersist();
                Notify();
            }
        }

        /// <summary>SDF weight step for the active language: 300 thin through 800 heavy.</summary>
        public static int FontWeight
        {
            get { EnsureLoaded(); return fontWeight; }
            set
            {
                EnsureLoaded();
                int v = SnapFontWeight(value);
                if (fontWeight == v) return;
                fontWeight = v;
                PersistInt(PrefFontWeight(language), fontWeight);
                FlushPersist();
                Notify();
            }
        }

        public static int SnapFontWeight(int weight)
        {
            int best = FontWeightSteps[0];
            int bestDist = int.MaxValue;
            for (int i = 0; i < FontWeightSteps.Length; i++)
            {
                int dist = Mathf.Abs(FontWeightSteps[i] - weight);
                if (dist < bestDist)
                {
                    best = FontWeightSteps[i];
                    bestDist = dist;
                }
            }
            return best;
        }

        public static string FontWeightName(int weight)
        {
            bool en = IsEnglish;
            switch (SnapFontWeight(weight))
            {
                case 300: return en ? "Light" : "细";
                case 400: return en ? "Regular" : "常规";
                case 600: return en ? "Semibold" : "半粗";
                case 700: return en ? "Bold" : "粗";
                case 800: return en ? "Heavy" : "特粗";
                default: return en ? "Medium" : "中等";
            }
        }

        public static float FontWeightSlider01(int weight)
        {
            int step = SnapFontWeight(weight);
            for (int i = 0; i < FontWeightSteps.Length; i++)
            {
                if (FontWeightSteps[i] == step)
                    return FontWeightSteps.Length <= 1 ? 0f : i / (float)(FontWeightSteps.Length - 1);
            }
            return 0.4f;
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
            PersistFloat(PrefFontSize(language), fontSizeScale);
            PersistFloat(PrefLetterSpacing(language), letterSpacing);
            if (notify)
            {
                FlushPersist();
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
                PersistFloat(PrefBgm, bgmVolume);
                FlushPersist();
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
                PersistFloat(PrefSfx, sfxVolume);
                FlushPersist();
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
                PersistInt(PrefTextSpeed, textSpeed);
                FlushPersist();
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
                PersistInt(PrefAutoPlay, autoPlay ? 1 : 0);
                FlushPersist();
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
                PersistFloat(PrefAutoDelay, autoDelay);
                FlushPersist();
                Notify();
            }
        }

        /// <summary>Seconds the full investigation picture stays up before dialogue (0–5). Default 1.</summary>
        public static float PictureHold
        {
            get { EnsureLoaded(); return pictureHold; }
            set
            {
                EnsureLoaded();
                float v = Mathf.Clamp(value, PictureHoldMin, PictureHoldMax);
                if (Mathf.Approximately(pictureHold, v)) return;
                pictureHold = v;
                PersistFloat(PrefPictureHold, pictureHold);
                FlushPersist();
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
                PersistInt(PrefFullscreen, fullscreen ? 1 : 0);
                FlushPersist();
                ApplyDisplay();
                Notify();
            }
        }

        /// <summary>
        /// When true (default), changes are written to the last-session profile and restored on launch.
        /// When false, launch loads the defaults profile; in-session tweaks stay until quit.
        /// </summary>
        public static bool RememberLastSettings
        {
            get { EnsureLoaded(); return rememberLast; }
            set
            {
                EnsureLoaded();
                if (rememberLast == value) return;
                rememberLast = value;
                // Always persist this toggle itself.
                PlayerPrefs.SetInt(PrefRememberLast, rememberLast ? 1 : 0);
                if (rememberLast)
                    WriteLastProfile();
                PlayerPrefs.Save();
                Notify();
            }
        }

        public static bool HasCustomDefaults
        {
            get
            {
                EnsureLoaded();
                return PlayerPrefs.GetInt(PrefHasCustomDefaults, 0) == 1;
            }
        }

        public static bool IsEnglish => Language == GameLanguage.En;

        public static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            rememberLast = PlayerPrefs.GetInt(PrefRememberLast, 1) == 1;
            MigrateLegacyFontPrefsIfNeeded();

            if (rememberLast)
                LoadLastProfile();
            else
                LoadDefaultsProfile();

            ApplyDisplay();
        }

        public static void ApplyDisplay()
        {
            EnsureLoaded();
            if (Screen.fullScreen != fullscreen)
                Screen.fullScreen = fullscreen;
        }

        /// <summary>Snapshot the current settings as the player's personal defaults.</summary>
        public static void SaveAsDefaults()
        {
            EnsureLoaded();
            PlayerPrefs.SetInt(PrefHasCustomDefaults, 1);
            PlayerPrefs.SetString(PrefDefLang, language == GameLanguage.En ? "en" : "zh");
            PlayerPrefs.SetFloat(PrefDefBgm, bgmVolume);
            PlayerPrefs.SetFloat(PrefDefSfx, sfxVolume);
            PlayerPrefs.SetInt(PrefDefTextSpeed, textSpeed);
            PlayerPrefs.SetInt(PrefDefAutoPlay, autoPlay ? 1 : 0);
            PlayerPrefs.SetFloat(PrefDefAutoDelay, autoDelay);
            PlayerPrefs.SetFloat(PrefDefPictureHold, pictureHold);
            PlayerPrefs.SetInt(PrefDefFullscreen, fullscreen ? 1 : 0);

            // Store font profiles for both languages: active from memory, other from last prefs / catalog.
            SaveDefaultFontSlot(GameLanguage.Zh);
            SaveDefaultFontSlot(GameLanguage.En);
            // Active language overrides with live values.
            if (language == GameLanguage.Zh)
            {
                PlayerPrefs.SetString(PrefDefFontZh, uiFontId);
                PlayerPrefs.SetFloat(PrefDefFontSizeZh, fontSizeScale);
                PlayerPrefs.SetFloat(PrefDefLetterSpZh, letterSpacing);
                PlayerPrefs.SetInt(PrefDefFontWeightZh, fontWeight);
            }
            else
            {
                PlayerPrefs.SetString(PrefDefFontEn, uiFontId);
                PlayerPrefs.SetFloat(PrefDefFontSizeEn, fontSizeScale);
                PlayerPrefs.SetFloat(PrefDefLetterSpEn, letterSpacing);
                PlayerPrefs.SetInt(PrefDefFontWeightEn, fontWeight);
            }

            if (rememberLast)
                WriteLastProfile();
            PlayerPrefs.Save();
            Notify();
        }

        /// <summary>Apply defaults (custom snapshot if any, else factory). Also updates last profile when remembering.</summary>
        public static void RestoreDefaults()
        {
            EnsureLoaded();
            var prevLang = language;
            LoadDefaultsProfile();
            if (rememberLast)
                WriteLastProfile();
            PlayerPrefs.Save();
            ApplyDisplay();
            if (prevLang != language)
            {
                UiLoc.Reload();
                ScriptLoc.Reload();
                HardTextLoc.Reload();
            }
            Notify();
        }

        /// <summary>Master BGM level before per-clip gain (matches prior ~0.38 peak feel at default 0.7).</summary>
        public static float BgmMaster => 0.38f * BgmVolume / 0.7f;

        public static float SfxMaster => 0.45f * SfxVolume / 0.8f;

        static string LangCode(GameLanguage lang) => lang == GameLanguage.En ? "en" : "zh";

        static string PrefFont(GameLanguage lang) => "sci.font." + LangCode(lang);
        static string PrefFontSize(GameLanguage lang) => "sci.fontSize." + LangCode(lang);
        static string PrefLetterSpacing(GameLanguage lang) => "sci.letterSpacing." + LangCode(lang);
        static string PrefFontWeight(GameLanguage lang) => "sci.fontWeight." + LangCode(lang);

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

        static void PersistString(string key, string value)
        {
            if (!rememberLast) return;
            PlayerPrefs.SetString(key, value);
        }

        static void PersistFloat(string key, float value)
        {
            if (!rememberLast) return;
            PlayerPrefs.SetFloat(key, value);
        }

        static void PersistInt(string key, int value)
        {
            if (!rememberLast) return;
            PlayerPrefs.SetInt(key, value);
        }

        static void FlushPersist()
        {
            if (rememberLast)
                PlayerPrefs.Save();
        }

        static void WriteLastProfile()
        {
            PlayerPrefs.SetString(PrefLang, language == GameLanguage.En ? "en" : "zh");
            PlayerPrefs.SetFloat(PrefBgm, bgmVolume);
            PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
            PlayerPrefs.SetInt(PrefTextSpeed, textSpeed);
            PlayerPrefs.SetInt(PrefAutoPlay, autoPlay ? 1 : 0);
            PlayerPrefs.SetFloat(PrefAutoDelay, autoDelay);
            PlayerPrefs.SetFloat(PrefPictureHold, pictureHold);
            PlayerPrefs.SetInt(PrefFullscreen, fullscreen ? 1 : 0);
            PlayerPrefs.SetString(PrefFont(language), uiFontId);
            PlayerPrefs.SetFloat(PrefFontSize(language), fontSizeScale);
            PlayerPrefs.SetFloat(PrefLetterSpacing(language), letterSpacing);
            PlayerPrefs.SetInt(PrefFontWeight(language), fontWeight);
        }

        static void LoadLastProfile()
        {
            var lang = PlayerPrefs.GetString(PrefLang, "zh");
            language = lang == "en" ? GameLanguage.En : GameLanguage.Zh;
            LoadFontProfileFor(language);

            bgmVolume = PlayerPrefs.HasKey(PrefBgm) ? Mathf.Clamp01(PlayerPrefs.GetFloat(PrefBgm)) : 0.7f;
            sfxVolume = PlayerPrefs.HasKey(PrefSfx) ? Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfx)) : 0.8f;
            textSpeed = PlayerPrefs.HasKey(PrefTextSpeed) ? Mathf.Clamp(PlayerPrefs.GetInt(PrefTextSpeed), 0, 2) : 1;
            autoPlay = PlayerPrefs.GetInt(PrefAutoPlay, 0) == 1;
            autoDelay = PlayerPrefs.HasKey(PrefAutoDelay)
                ? Mathf.Clamp(PlayerPrefs.GetFloat(PrefAutoDelay), 0.3f, 5f)
                : 1.2f;
            pictureHold = PlayerPrefs.HasKey(PrefPictureHold)
                ? Mathf.Clamp(PlayerPrefs.GetFloat(PrefPictureHold), PictureHoldMin, PictureHoldMax)
                : 1f;
            fullscreen = PlayerPrefs.GetInt(PrefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        }

        static void LoadDefaultsProfile()
        {
            if (PlayerPrefs.GetInt(PrefHasCustomDefaults, 0) == 1)
            {
                var lang = PlayerPrefs.GetString(PrefDefLang, "zh");
                language = lang == "en" ? GameLanguage.En : GameLanguage.Zh;
                bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefDefBgm, 0.7f));
                sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefDefSfx, 0.8f));
                textSpeed = Mathf.Clamp(PlayerPrefs.GetInt(PrefDefTextSpeed, 1), 0, 2);
                autoPlay = PlayerPrefs.GetInt(PrefDefAutoPlay, 0) == 1;
                autoDelay = Mathf.Clamp(PlayerPrefs.GetFloat(PrefDefAutoDelay, 1.2f), 0.3f, 5f);
                pictureHold = Mathf.Clamp(PlayerPrefs.GetFloat(PrefDefPictureHold, 1f), PictureHoldMin, PictureHoldMax);
                fullscreen = PlayerPrefs.GetInt(PrefDefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
                LoadFontFromDefaults(language);
            }
            else
            {
                ApplyFactoryDefaults();
            }
        }

        static void ApplyFactoryDefaults()
        {
            language = GameLanguage.Zh;
            bgmVolume = 0.7f;
            sfxVolume = 0.8f;
            textSpeed = 1;
            autoPlay = false;
            autoDelay = 1.2f;
            pictureHold = 1f;
            fullscreen = true;
            uiFontId = DefaultFontZh;
            var opt = FontCatalog.Get(uiFontId);
            fontSizeScale = Mathf.Clamp(opt.SizeScale > 0.01f ? opt.SizeScale : 1.15f, FontSizeMin, FontSizeMax);
            letterSpacing = Mathf.Clamp(opt.LetterSpacing, LetterSpacingMin, LetterSpacingMax);
            fontWeight = FontWeightDefault;
        }

        static void SaveDefaultFontSlot(GameLanguage lang)
        {
            string defId = DefaultFontId(lang);
            string id = PlayerPrefs.GetString(PrefFont(lang), defId);
            if (!IsKnownFont(id)) id = defId;
            var opt = FontCatalog.Get(id);
            float size = PlayerPrefs.HasKey(PrefFontSize(lang))
                ? PlayerPrefs.GetFloat(PrefFontSize(lang))
                : (opt.SizeScale > 0.01f ? opt.SizeScale : 1.15f);
            float spacing = PlayerPrefs.HasKey(PrefLetterSpacing(lang))
                ? PlayerPrefs.GetFloat(PrefLetterSpacing(lang))
                : opt.LetterSpacing;
            int weight = PlayerPrefs.HasKey(PrefFontWeight(lang))
                ? SnapFontWeight(PlayerPrefs.GetInt(PrefFontWeight(lang)))
                : FontWeightDefault;

            if (lang == GameLanguage.Zh)
            {
                PlayerPrefs.SetString(PrefDefFontZh, id);
                PlayerPrefs.SetFloat(PrefDefFontSizeZh, Mathf.Clamp(size, FontSizeMin, FontSizeMax));
                PlayerPrefs.SetFloat(PrefDefLetterSpZh, Mathf.Clamp(spacing, LetterSpacingMin, LetterSpacingMax));
                PlayerPrefs.SetInt(PrefDefFontWeightZh, weight);
            }
            else
            {
                PlayerPrefs.SetString(PrefDefFontEn, id);
                PlayerPrefs.SetFloat(PrefDefFontSizeEn, Mathf.Clamp(size, FontSizeMin, FontSizeMax));
                PlayerPrefs.SetFloat(PrefDefLetterSpEn, Mathf.Clamp(spacing, LetterSpacingMin, LetterSpacingMax));
                PlayerPrefs.SetInt(PrefDefFontWeightEn, weight);
            }
        }

        static void LoadFontFromDefaults(GameLanguage lang)
        {
            string defId = DefaultFontId(lang);
            string idKey = lang == GameLanguage.En ? PrefDefFontEn : PrefDefFontZh;
            string sizeKey = lang == GameLanguage.En ? PrefDefFontSizeEn : PrefDefFontSizeZh;
            string spKey = lang == GameLanguage.En ? PrefDefLetterSpEn : PrefDefLetterSpZh;
            string weightKey = lang == GameLanguage.En ? PrefDefFontWeightEn : PrefDefFontWeightZh;

            string id = PlayerPrefs.GetString(idKey, defId);
            if (!IsKnownFont(id)) id = defId;
            uiFontId = id;
            var opt = FontCatalog.Get(uiFontId);
            fontSizeScale = PlayerPrefs.HasKey(sizeKey)
                ? Mathf.Clamp(PlayerPrefs.GetFloat(sizeKey), FontSizeMin, FontSizeMax)
                : Mathf.Clamp(opt.SizeScale > 0.01f ? opt.SizeScale : 1.15f, FontSizeMin, FontSizeMax);
            letterSpacing = PlayerPrefs.HasKey(spKey)
                ? Mathf.Clamp(PlayerPrefs.GetFloat(spKey), LetterSpacingMin, LetterSpacingMax)
                : Mathf.Clamp(opt.LetterSpacing, LetterSpacingMin, LetterSpacingMax);
            fontWeight = PlayerPrefs.HasKey(weightKey)
                ? SnapFontWeight(PlayerPrefs.GetInt(weightKey))
                : FontWeightDefault;
        }

        /// <summary>
        /// One-time: copy legacy global font keys into the currently saved language slot.
        /// The other language keeps its catalog defaults until the player sets them.
        /// </summary>
        static void MigrateLegacyFontPrefsIfNeeded()
        {
            // Need language hint before full load — peek last lang key.
            var peek = PlayerPrefs.GetString(PrefLang, "zh");
            var peekLang = peek == "en" ? GameLanguage.En : GameLanguage.Zh;

            string fontKey = PrefFont(peekLang);
            if (PlayerPrefs.HasKey(fontKey)) return;
            if (!PlayerPrefs.HasKey(PrefFontLegacy)
                && !PlayerPrefs.HasKey(PrefFontSizeLegacy)
                && !PlayerPrefs.HasKey(PrefLetterSpacingLegacy))
                return;

            string id = PlayerPrefs.GetString(PrefFontLegacy, DefaultFontId(peekLang));
            if (!IsKnownFont(id)) id = DefaultFontId(peekLang);
            PlayerPrefs.SetString(fontKey, id);

            var opt = FontCatalog.Get(id);
            float size = PlayerPrefs.HasKey(PrefFontSizeLegacy)
                ? PlayerPrefs.GetFloat(PrefFontSizeLegacy)
                : opt.SizeScale;
            float spacing = PlayerPrefs.HasKey(PrefLetterSpacingLegacy)
                ? PlayerPrefs.GetFloat(PrefLetterSpacingLegacy)
                : opt.LetterSpacing;
            PlayerPrefs.SetFloat(PrefFontSize(peekLang), Mathf.Clamp(size, FontSizeMin, FontSizeMax));
            PlayerPrefs.SetFloat(PrefLetterSpacing(peekLang), Mathf.Clamp(spacing, LetterSpacingMin, LetterSpacingMax));
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

            fontWeight = PlayerPrefs.HasKey(PrefFontWeight(lang))
                ? SnapFontWeight(PlayerPrefs.GetInt(PrefFontWeight(lang)))
                : FontWeightDefault;
        }

        static void Notify() => OnChanged?.Invoke();
    }
}
