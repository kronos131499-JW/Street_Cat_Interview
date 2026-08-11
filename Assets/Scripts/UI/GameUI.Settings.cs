using StreetCat.Core;
using StreetCat.Loc;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StreetCat.UI
{
    public partial class GameUI
    {
        bool reopenMenuAfterSettings;

        void BuildSettingsOverlay(Transform parent)
        {
            settingsRoot = new GameObject("SettingsOverlay", typeof(RectTransform));
            settingsRoot.transform.SetParent(parent, false);
            StretchFull(settingsRoot.GetComponent<RectTransform>());

            var dim = CreateImage(settingsRoot.transform, "Dim", VnTheme.OverlayDim);
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = CreateImage(settingsRoot.transform, "Panel", VnTheme.Paper);
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(540, 720);
            var edge = CreateImage(panel.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(edge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2.5f), new Vector2(0, 0));

            settingsTitleText = CreateUiText(panel.transform, "Title", 26, TextAnchor.UpperCenter,
                VnTheme.Accent, new Vector2(0, -18), new Vector2(440, 36));
            settingsTitleText.fontStyle = FontStyles.Bold;
            var tr = settingsTitleText.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1);

            // Scrollable settings list (font + audio + reading options).
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Stretch(scrollRt, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            scrollGo.GetComponent<Image>().raycastTarget = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            viewport.GetComponent<Image>().raycastTarget = true;

            var list = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(viewport.transform, false);
            var lrt = list.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(0, 0);
            var v = list.GetComponent<VerticalLayoutGroup>();
            v.spacing = 8;
            v.padding = new RectOffset(8, 8, 4, 12);
            v.childForceExpandWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = lrt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            // Language
            AddSettingsLabel(list.transform, "LangLabel", "ui.settings.language");
            var langRow = AddSettingsRow(list.transform, "LangRow");
            settingsLangZhBtn = AddSettingsToggle(langRow, "Zh", "ui.settings.lang_zh", () =>
            {
                GameSettings.Language = GameLanguage.Zh;
            });
            settingsLangEnBtn = AddSettingsToggle(langRow, "En", "ui.settings.lang_en", () =>
            {
                GameSettings.Language = GameLanguage.En;
            });

            // Font (cycle for testing bundled faces)
            AddSettingsLabel(list.transform, "FontLabel", "ui.settings.font");
            var fontRow = AddSettingsRow(list.transform, "FontRow");
            AddSettingsPlainButton(fontRow, "FontPrev", "‹", () => GameSettings.CycleUiFont(-1));
            settingsFontNameLabel = CreateUiText(fontRow, "FontName", 15, TextAnchor.MiddleCenter,
                VnTheme.TextPrimary, Vector2.zero, new Vector2(220, 38));
            var fontNameLe = settingsFontNameLabel.gameObject.AddComponent<LayoutElement>();
            fontNameLe.flexibleWidth = 1f;
            fontNameLe.preferredHeight = 42;
            settingsFontNameLabel.enableWordWrapping = true;
            settingsFontNameLabel.overflowMode = TextOverflowModes.Overflow;
            settingsFontNameLabel.lineSpacing = -10f;
            AddSettingsPlainButton(fontRow, "FontNext", "›", () => GameSettings.CycleUiFont(1));

            AddSettingsLabel(list.transform, "FontSizeLabel", "ui.settings.font_size");
            settingsFontSizeSlider = AddSettingsSlider(list.transform, "FontSize", v =>
            {
                GameSettings.FontSizeScale = Mathf.Lerp(GameSettings.FontSizeMin, GameSettings.FontSizeMax, v);
                if (settingsFontSizeValue != null)
                    settingsFontSizeValue.text = Mathf.RoundToInt(GameSettings.FontSizeScale * 100f) + "%";
            }, out settingsFontSizeValue);

            AddSettingsLabel(list.transform, "LetterSpLabel", "ui.settings.letter_spacing");
            settingsLetterSpacingSlider = AddSettingsSlider(list.transform, "LetterSp", v =>
            {
                GameSettings.LetterSpacing = Mathf.Lerp(GameSettings.LetterSpacingMin, GameSettings.LetterSpacingMax, v);
                if (settingsLetterSpacingValue != null)
                    settingsLetterSpacingValue.text = GameSettings.LetterSpacing.ToString("0.0");
            }, out settingsLetterSpacingValue);

            // BGM
            AddSettingsLabel(list.transform, "BgmLabel", "ui.settings.bgm");
            settingsBgmSlider = AddSettingsSlider(list.transform, "Bgm", v =>
            {
                GameSettings.BgmVolume = v;
                if (settingsBgmValue != null)
                    settingsBgmValue.text = Mathf.RoundToInt(v * 100) + "%";
            }, out settingsBgmValue);

            // SFX
            AddSettingsLabel(list.transform, "SfxLabel", "ui.settings.sfx");
            settingsSfxSlider = AddSettingsSlider(list.transform, "Sfx", v =>
            {
                GameSettings.SfxVolume = v;
                if (settingsSfxValue != null)
                    settingsSfxValue.text = Mathf.RoundToInt(v * 100) + "%";
            }, out settingsSfxValue);
            AddSettingsButton(list.transform, "TestSfx", "ui.settings.test_sfx", () =>
            {
                SfxController.Instance?.PlayUi();
            });

            // Text speed
            AddSettingsLabel(list.transform, "SpeedLabel", "ui.settings.text_speed");
            var speedRow = AddSettingsRow(list.transform, "SpeedRow");
            settingsSpeedSlowBtn = AddSettingsToggle(speedRow, "Slow", "ui.settings.speed_slow",
                () => GameSettings.TextSpeed = 0);
            settingsSpeedNormalBtn = AddSettingsToggle(speedRow, "Normal", "ui.settings.speed_normal",
                () => GameSettings.TextSpeed = 1);
            settingsSpeedFastBtn = AddSettingsToggle(speedRow, "Fast", "ui.settings.speed_fast",
                () => GameSettings.TextSpeed = 2);

            // Auto play
            AddSettingsLabel(list.transform, "AutoLabel", "ui.settings.auto_play");
            var autoRow = AddSettingsRow(list.transform, "AutoRow");
            settingsAutoOnBtn = AddSettingsToggle(autoRow, "On", "ui.settings.auto_on",
                () => GameSettings.AutoPlay = true);
            settingsAutoOffBtn = AddSettingsToggle(autoRow, "Off", "ui.settings.auto_off",
                () => GameSettings.AutoPlay = false);

            AddSettingsLabel(list.transform, "DelayLabel", "ui.settings.auto_delay");
            settingsAutoDelaySlider = AddSettingsSlider(list.transform, "Delay", v =>
            {
                GameSettings.AutoDelay = Mathf.Lerp(0.3f, 5f, v);
                if (settingsAutoDelayValue != null)
                    settingsAutoDelayValue.text = GameSettings.AutoDelay.ToString("0.0") + "s";
            }, out settingsAutoDelayValue);

            // Display
            AddSettingsLabel(list.transform, "DisplayLabel", "ui.settings.display");
            var dispRow = AddSettingsRow(list.transform, "DispRow");
            settingsFullscreenBtn = AddSettingsToggle(dispRow, "Full", "ui.settings.fullscreen",
                () => GameSettings.Fullscreen = true);
            settingsWindowedBtn = AddSettingsToggle(dispRow, "Win", "ui.settings.windowed",
                () => GameSettings.Fullscreen = false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            {
                var go = new GameObject("DebugJump", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(list.transform, false);
                go.GetComponent<Image>().color = VnTheme.ButtonPrimary;
                go.GetComponent<LayoutElement>().preferredHeight = 44;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SfxController.Instance?.PlayUi();
                    CloseSettings();
                    ToggleDebugJumpPanel();
                });
                var tx = CreateUiText(go.transform, "T", 17, TextAnchor.MiddleCenter, VnTheme.TextPrimary,
                    Vector2.zero, Vector2.zero);
                StretchFull(tx.GetComponent<RectTransform>());
                tx.text = "测试跳转 (F9)";
                tx.raycastTarget = false;
            }
#endif
            AddSettingsButton(list.transform, "Close", "ui.settings.close", CloseSettings);

            settingsRoot.SetActive(false);
            SyncSettingsWidgets();
            RefreshSettingsLabels();
        }

        void AddSettingsLabel(Transform parent, string name, string locKey)
        {
            var t = CreateUiText(parent, name, 16, TextAnchor.MiddleLeft, VnTheme.AccentSoft,
                Vector2.zero, new Vector2(400, 22));
            t.text = UiLoc.T(locKey);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
            var tag = t.gameObject.AddComponent<LocTag>();
            tag.key = locKey;
        }

        Transform AddSettingsRow(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 40;
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return go.transform;
        }

        TextMeshProUGUI AddSettingsToggle(Transform parent, string name, string locKey, UnityEngine.Events.UnityAction act)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = VnTheme.Button;
            go.GetComponent<LayoutElement>().preferredHeight = 38;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                act();
                SyncSettingsWidgets();
            });
            var tx = CreateUiText(go.transform, "T", 17, TextAnchor.MiddleCenter, VnTheme.TextPrimary,
                Vector2.zero, Vector2.zero);
            StretchFull(tx.GetComponent<RectTransform>());
            tx.text = UiLoc.T(locKey);
            tx.raycastTarget = false;
            var tag = go.AddComponent<LocTag>();
            tag.key = locKey;
            tag.target = tx;
            return tx;
        }

        void AddSettingsPlainButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction act)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = VnTheme.Button;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 38;
            le.preferredWidth = 44;
            le.flexibleWidth = 0f;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                act();
                SyncSettingsWidgets();
            });
            var tx = CreateUiText(go.transform, "T", 22, TextAnchor.MiddleCenter, VnTheme.TextPrimary,
                Vector2.zero, Vector2.zero);
            StretchFull(tx.GetComponent<RectTransform>());
            tx.text = label;
            tx.raycastTarget = false;
        }

        Slider AddSettingsSlider(Transform parent, string name, System.Action<float> onChanged, out TextMeshProUGUI valueLabel)
        {
            var row = new GameObject(name + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 36;
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleCenter;

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            sliderGo.transform.SetParent(row.transform, false);
            sliderGo.GetComponent<LayoutElement>().flexibleWidth = 1;
            sliderGo.GetComponent<LayoutElement>().preferredHeight = 28;

            var bg = CreateImage(sliderGo.transform, "Background", new Color(0.15f, 0.15f, 0.18f, 0.9f));
            StretchFull(bg.rectTransform);
            bg.raycastTarget = false;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(6, 8), new Vector2(-6, -8));
            var fill = CreateImage(fillArea.transform, "Fill", VnTheme.Accent);
            StretchFull(fill.rectTransform);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            StretchFull(handleArea.GetComponent<RectTransform>());
            var handle = CreateImage(handleArea.transform, "Handle", VnTheme.TextPrimary);
            var hrt = handle.rectTransform;
            hrt.sizeDelta = new Vector2(18, 18);

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = hrt;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(v => onChanged(v));

            valueLabel = CreateUiText(row.transform, "Val", 15, TextAnchor.MiddleRight, VnTheme.TextPrimary,
                Vector2.zero, new Vector2(56, 28));
            valueLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;
            return slider;
        }

        void AddSettingsButton(Transform parent, string name, string locKey, UnityEngine.Events.UnityAction act)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = VnTheme.Button;
            go.GetComponent<LayoutElement>().preferredHeight = 44;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                act();
            });
            var tx = CreateUiText(go.transform, "T", 19, TextAnchor.MiddleCenter, VnTheme.TextPrimary,
                Vector2.zero, Vector2.zero);
            StretchFull(tx.GetComponent<RectTransform>());
            tx.text = UiLoc.T(locKey);
            tx.raycastTarget = false;
            var tag = go.AddComponent<LocTag>();
            tag.key = locKey;
            tag.target = tx;
        }

        void OpenSettings()
        {
            reopenMenuAfterSettings = false;
            if (settingsRoot == null) return;
            SfxController.Instance?.PlayUi();
            if (menuRoot) menuRoot.SetActive(false);
            settingsRoot.SetActive(true);
            settingsRoot.transform.SetAsLastSibling();
            SyncSettingsWidgets();
            RefreshSettingsLabels();
        }

        void OpenSettingsFromMenu()
        {
            reopenMenuAfterSettings = true;
            if (settingsRoot == null) return;
            SfxController.Instance?.PlayUi();
            if (menuRoot) menuRoot.SetActive(false);
            settingsRoot.SetActive(true);
            settingsRoot.transform.SetAsLastSibling();
            SyncSettingsWidgets();
            RefreshSettingsLabels();
        }

        void CloseSettings()
        {
            if (settingsRoot) settingsRoot.SetActive(false);
            if (reopenMenuAfterSettings)
            {
                reopenMenuAfterSettings = false;
                if (menuRoot != null && mode == Mode.Menu)
                    menuRoot.SetActive(true);
            }
        }

        void SyncSettingsWidgets()
        {
            if (settingsFontNameLabel != null)
            {
                var opt = FontCatalog.Get(GameSettings.UiFontId);
                settingsFontNameLabel.text = opt.LatinOnly
                    ? opt.DisplayName + "\n" + UiLoc.T("ui.settings.font_latin_hint")
                    : opt.DisplayName;
            }
            if (settingsFontSizeSlider != null)
            {
                float t = Mathf.InverseLerp(GameSettings.FontSizeMin, GameSettings.FontSizeMax, GameSettings.FontSizeScale);
                settingsFontSizeSlider.SetValueWithoutNotify(t);
                if (settingsFontSizeValue != null)
                    settingsFontSizeValue.text = Mathf.RoundToInt(GameSettings.FontSizeScale * 100f) + "%";
            }
            if (settingsLetterSpacingSlider != null)
            {
                float t = Mathf.InverseLerp(GameSettings.LetterSpacingMin, GameSettings.LetterSpacingMax, GameSettings.LetterSpacing);
                settingsLetterSpacingSlider.SetValueWithoutNotify(t);
                if (settingsLetterSpacingValue != null)
                    settingsLetterSpacingValue.text = GameSettings.LetterSpacing.ToString("0.0");
            }
            if (settingsBgmSlider != null)
            {
                settingsBgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
                if (settingsBgmValue != null)
                    settingsBgmValue.text = Mathf.RoundToInt(GameSettings.BgmVolume * 100) + "%";
            }
            if (settingsSfxSlider != null)
            {
                settingsSfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
                if (settingsSfxValue != null)
                    settingsSfxValue.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100) + "%";
            }
            if (settingsAutoDelaySlider != null)
            {
                float t = Mathf.InverseLerp(0.3f, 5f, GameSettings.AutoDelay);
                settingsAutoDelaySlider.SetValueWithoutNotify(t);
                if (settingsAutoDelayValue != null)
                    settingsAutoDelayValue.text = GameSettings.AutoDelay.ToString("0.0") + "s";
            }

            HighlightToggle(settingsLangZhBtn, GameSettings.Language == GameLanguage.Zh);
            HighlightToggle(settingsLangEnBtn, GameSettings.Language == GameLanguage.En);
            HighlightToggle(settingsSpeedSlowBtn, GameSettings.TextSpeed == 0);
            HighlightToggle(settingsSpeedNormalBtn, GameSettings.TextSpeed == 1);
            HighlightToggle(settingsSpeedFastBtn, GameSettings.TextSpeed == 2);
            HighlightToggle(settingsAutoOnBtn, GameSettings.AutoPlay);
            HighlightToggle(settingsAutoOffBtn, !GameSettings.AutoPlay);
            HighlightToggle(settingsFullscreenBtn, GameSettings.Fullscreen);
            HighlightToggle(settingsWindowedBtn, !GameSettings.Fullscreen);
        }

        static void HighlightToggle(TextMeshProUGUI label, bool on)
        {
            if (label == null) return;
            var img = label.transform.parent != null ? label.transform.parent.GetComponent<Image>() : null;
            if (img != null)
                img.color = on ? VnTheme.AccentSoft : VnTheme.Button;
            label.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
        }

        void RefreshSettingsLabels()
        {
            if (settingsTitleText != null)
                settingsTitleText.text = UiLoc.T("ui.settings.title");
            if (settingsRoot == null) return;
            foreach (var tag in settingsRoot.GetComponentsInChildren<LocTag>(true))
            {
                if (tag == null || string.IsNullOrEmpty(tag.key)) continue;
                var tx = tag.target != null ? tag.target : tag.GetComponent<TextMeshProUGUI>();
                if (tx == null) tx = tag.GetComponentInChildren<TextMeshProUGUI>();
                if (tx != null) tx.text = UiLoc.T(tag.key);
            }
        }

        void RefreshLocalizedChrome()
        {
            RefreshSettingsLabels();
            if (menuTitleText != null)
                menuTitleText.text = UiLoc.T("ui.menu");
            if (backlogTitleText != null)
                backlogTitleText.text = UiLoc.T("ui.backlog.title", "对话回看");
            if (menuRoot != null)
            {
                foreach (var tag in menuRoot.GetComponentsInChildren<LocTag>(true))
                {
                    if (tag == null || string.IsNullOrEmpty(tag.key)) continue;
                    var tx = tag.target != null ? tag.target : tag.GetComponentInChildren<TextMeshProUGUI>();
                    if (tx != null) tx.text = UiLoc.T(tag.key);
                }
            }
            if (hideDialogueLabel != null)
                hideDialogueLabel.text = dialogueHidden
                    ? UiLoc.T("ui.show_dialogue")
                    : UiLoc.T("ui.hide_dialogue");
            RefreshNotebookLocalizedChrome();
            RefreshWritingMatsLocalizedChrome();
            RefreshInterviewMeterLabels();
            if (mode == Mode.Title)
                RebuildTitleActionsOnly();
            else if (IsSkippableDialogueContext())
                RebuildSkippableDialogueActions();
        }

        void RebuildTitleActionsOnly()
        {
            ClearButtons();
            AddAction(UiLoc.T("ui.title.new_game"), () => ChapterFlowController.Instance.StartNewGame(), true);
            AddAction(UiLoc.T("ui.title.continue"), () => ChapterFlowController.Instance.ContinueOrNew());
            AddAction(UiLoc.T("ui.title.load"), () => OpenSaveLoad(false));
            AddAction(UiLoc.T("ui.title.clear_saves"), () =>
            {
                SaveSystem.Delete();
                SetTitleTaglineMessage(true);
            });
            AddAction(UiLoc.T("ui.title.settings"), OpenSettings);
            AddAction(UiLoc.T("ui.title.quit"), () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            ApplyTitleLanguageVisuals();
        }

        void ApplyTitleLanguageVisuals()
        {
            bool en = GameSettings.IsEnglish;
            if (titleLogoCn != null)
                titleLogoCn.gameObject.SetActive(!en && titleLogoCn.sprite != null);
            if (titleLogoEn != null)
                titleLogoEn.gameObject.SetActive(en && titleLogoEn.sprite != null);
            if (titleBrand != null)
            {
                bool showBrand = (en ? titleLogoEn == null || titleLogoEn.sprite == null
                    : titleLogoCn == null || titleLogoCn.sprite == null);
                titleBrand.gameObject.SetActive(showBrand);
                titleBrand.text = UiLoc.T("ui.title.brand");
                StyleTitleMenuFittedText(titleBrand, 50, true);
            }
            if (titleSubtitle != null)
            {
                titleSubtitle.text = UiLoc.T("ui.title.subtitle");
                StyleTitleMenuBodyText(titleSubtitle, 20);
            }
            if (titleContentsLabel != null)
            {
                titleContentsLabel.text = UiLoc.T("ui.title.contents");
                StyleTitleMenuText(titleContentsLabel, 22, true);
            }
            if (titleTagline != null && titleTaglineCleared)
            {
                titleTagline.text = UiLoc.T("ui.title.saves_cleared");
                StyleTitleMenuText(titleTagline, 17, true);
            }
        }

        void StopAutoPlay()
        {
            if (autoPlayCo != null)
            {
                StopCoroutine(autoPlayCo);
                autoPlayCo = null;
            }
        }

        void ScheduleAutoPlayIfNeeded()
        {
            StopAutoPlay();
            if (!GameSettings.AutoPlay) return;
            if (mode != Mode.Dialogue) return;
            if (waitingForChoice) return;
            if (!canClickAdvance && !typewriterRunning) return;
            autoPlayCo = StartCoroutine(AutoPlayRoutine());
        }

        System.Collections.IEnumerator AutoPlayRoutine()
        {
            while (typewriterRunning)
                yield return null;
            float wait = GameSettings.AutoDelay;
            float t = 0f;
            while (t < wait)
            {
                if (!GameSettings.AutoPlay || mode != Mode.Dialogue || waitingForChoice)
                    yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            autoPlayCo = null;
            if (GameSettings.AutoPlay && mode == Mode.Dialogue && !waitingForChoice)
                TryAdvanceByClick();
        }
    }

    /// <summary>Marks TMP UI text for language refresh.</summary>
    public class LocTag : MonoBehaviour
    {
        public string key;
        public TextMeshProUGUI target;
    }
}
