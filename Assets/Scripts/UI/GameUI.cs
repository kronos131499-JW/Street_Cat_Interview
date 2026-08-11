using System;
using System.Collections;
using System.Collections.Generic;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Investigation;
using StreetCat.Interview;
using StreetCat.Loc;
using StreetCat.Narrative;
using StreetCat.Notebook;
using StreetCat.Writing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace StreetCat.UI
{
    public partial class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        enum Mode { Title, Dialogue, Investigate, Talk, Interview, Writing, Epilogue, Notebook, Menu, Backlog }

        Mode mode;
        Mode returnFromOverlay;

        // VN chrome
        RectTransform canvasRt;
        Image bgImage;
        Image stageArt;
        Image propImage;
        Image portraitImage;
        Image vignetteImage;
        TextMeshProUGUI locationText;
        TextMeshProUGUI objectiveText;
        TextMeshProUGUI chapterChip;
        Image dialoguePanel;
        Button dialogueClick;
        Image namePlate;
        TextMeshProUGUI nameText;
        TextMeshProUGUI bodyText;
        TextMeshProUGUI statusText;
        TextMeshProUGUI stageHint;
        CanvasGroup locationFade;
        CanvasGroup stageHintFade;
        Coroutine sceneTitleCo;
        string lastSceneTitleKey;
        CanvasGroup dialogueFade;
        Button hideDialogueBtn;
        TextMeshProUGUI hideDialogueLabel;
        Image sceneFadeImage;
        CanvasGroup sceneFadeCg;
        Coroutine sceneFadeCo;
        bool dialogueHidden;
        bool sceneTransitioning;
        Transform buttonRoot;
        Transform choiceRoot;
        TMP_InputField inputField;
        GameObject titleRoot;
        Image titleLogoCn;
        Image titleLogoEn;
        TextMeshProUGUI titleBrand;
        TextMeshProUGUI titleSubtitle;
        TextMeshProUGUI titleContentsLabel;
        TextMeshProUGUI titleTagline;
        bool titleTaglineCleared;

        // Menu / backlog / notebook
        GameObject menuRoot;
        TextMeshProUGUI menuTitleText;
        GameObject settingsRoot;
        TextMeshProUGUI settingsTitleText;
        TextMeshProUGUI settingsBgmValue;
        TextMeshProUGUI settingsSfxValue;
        TextMeshProUGUI settingsAutoDelayValue;
        Slider settingsBgmSlider;
        Slider settingsSfxSlider;
        Slider settingsAutoDelaySlider;
        TextMeshProUGUI settingsLangZhBtn;
        TextMeshProUGUI settingsLangEnBtn;
        TextMeshProUGUI settingsFontNameLabel;
        TextMeshProUGUI settingsFontSizeValue;
        TextMeshProUGUI settingsLetterSpacingValue;
        Slider settingsFontSizeSlider;
        Slider settingsLetterSpacingSlider;
        TextMeshProUGUI settingsSpeedSlowBtn;
        TextMeshProUGUI settingsSpeedNormalBtn;
        TextMeshProUGUI settingsSpeedFastBtn;
        TextMeshProUGUI settingsAutoOnBtn;
        TextMeshProUGUI settingsAutoOffBtn;
        TextMeshProUGUI settingsFullscreenBtn;
        TextMeshProUGUI settingsWindowedBtn;
        Coroutine autoPlayCo;
        GameObject backlogRoot;
        GameObject notebookRoot;
        GameObject saveLoadRoot;
        TextMeshProUGUI backlogTitleText;
        TextMeshProUGUI backlogText;
        ScrollRect backlogScroll;
        TextMeshProUGUI notebookTitleText;
        TextMeshProUGUI notebookCloseLabel;
        TextMeshProUGUI notebookDetailTitleText;
        TextMeshProUGUI notebookStatusChipText;
        Image notebookStatusChipBg;
        TextMeshProUGUI notebookDetailBodyText;
        TextMeshProUGUI notebookSourceText;
        TextMeshProUGUI notebookInspireHeaderText;
        TextMeshProUGUI notebookInspireBodyText;
        Button notebookInspireButton;
        Image notebookInspirePanel;
        Transform notebookStickyGrid;
        Transform notebookModeRow;
        ScrollRect notebookDetailScroll;
        Image notebookPageImage;
        string notebookSelectedTopicId;
        int notebookTab; // 0=主题 1=待确认 2=提问记录
        readonly List<GameObject> notebookSpawned = new List<GameObject>();
        Sprite notebookLinedPaperSprite;
        Sprite notebookNavySprite;
        TextMeshProUGUI saveLoadTitle;
        Transform saveLoadList;
        bool saveLoadIsSave; // true=存档, false=读档
        int pendingOverwriteSlot = -999;
        GameObject confirmRoot;
        TextMeshProUGUI confirmText;
        bool canClickAdvance;
        bool waitingForChoice;
        bool savedWaitingForChoice;
        string lastHistorySpeaker = "";
        /// <summary>Last 小凌 portrait tag (常态/惊讶/…) for「未标注时沿用上一张」.</summary>
        string stickyXiaolingPortrait = "常态";
        /// <summary>Runtime 【背景】 override from script / investigate hotspots.</summary>
        string stageBackgroundOverride;
        /// <summary>Script scene id last synced for stage BG / title toast.</summary>
        string lastBgSceneId;

        // Interview full-screen layout
        GameObject interviewRoot;
        TextMeshProUGUI interviewSubjectText;
        TextMeshProUGUI interviewStatusText;
        TextMeshProUGUI interviewLogText;
        ScrollRect interviewScroll;
        Transform interviewHintRoot;
        Transform interviewActionRoot;
        TMP_InputField interviewInput;
        readonly List<GameObject> interviewSpawned = new List<GameObject>();
        readonly List<GameObject> interviewPresetSpawned = new List<GameObject>();

        readonly List<GameObject> spawnedButtons = new List<GameObject>();
        WritingDirection pendingDir = WritingDirection.GuardCatToday;
        readonly List<string> selectedMats = new List<string>();
        ArticleAssembler assembler = new ArticleAssembler();
        Coroutine writingReviewCo;
        Coroutine writingPolishCo;
        string writingPolishKey;
        bool writingAiPolishUsed;
        string lastInspectText;
        TMP_FontAsset font;
        /// <summary>Title / menu typography (OS CJK when available).</summary>
        TMP_FontAsset titleFont;
        Coroutine fadeCo;
        Coroutine typewriterCo;
        Coroutine portraitFadeCo;
        Coroutine interviewLlmCo;
        const float SceneTitleFadeIn = 0.35f;
        const float SceneTitleHold = 2.2f;
        const float SceneTitleFadeOut = 0.55f;
        string typewriterFull = "";
        bool typewriterRunning;
        float skipHoldTimer;
        Image advanceCatcher;
        Image topBarImage;
        Image choiceHostImage;
        CanvasGroup portraitFade;
        Image atmosphereWash;
        ScrollRect dialogueScroll;
        GameObject investigateRoot;
        TextMeshProUGUI investigateTitle;
        TextMeshProUGUI investigateIntelHint;
        Transform investigateHotspotLayer;
        Transform investigateActions;
        TextMeshProUGUI investigateHoverLabel;
        Transform titleActionRoot;
        readonly List<GameObject> investigateSpawned = new List<GameObject>();
        TextMeshProUGUI clickHintText;
        bool investigateHotspotsVisible;
        bool backlogOpenedFromNotebook;
        bool talkAwaitingClickReturn;
        bool talkIsPostInterview;
        readonly List<TalkBeat> talkQueue = new List<TalkBeat>();
        int talkIndex;
        TalkTopic activeTalkTopic;

        void Awake()
        {
            Instance = this;
            GameSettings.EnsureLoaded();
            font = ResolveUiFont();
            titleFont = ResolveTitleFont() ?? font;
            BuildCanvas();
            ApplyActiveFonts();
            GameSettings.OnChanged += OnGameSettingsChanged;
        }

        void OnDestroy()
        {
            GameSettings.OnChanged -= OnGameSettingsChanged;
            if (Instance == this) Instance = null;
        }

        void OnGameSettingsChanged()
        {
            ApplyActiveFonts();
            ApplyTitleLanguageVisuals();
            RefreshLocalizedChrome();
            if (settingsRoot != null && settingsRoot.activeSelf)
                SyncSettingsWidgets();
            // Safe refresh: do not re-enter OnScriptLine (would Advance cue lines / dup history).
            if (mode == Mode.Dialogue)
                RefreshCurrentDialogueDisplay();
            else if (IsSkippableDialogueContext())
                RebuildSkippableDialogueActions();
            else if (mode == Mode.Talk)
                RefreshHeader();
        }

        /// <summary>Re-apply localized speaker/body/choices without side effects.</summary>
        void RefreshCurrentDialogueDisplay()
        {
            var display = SceneDirector.Instance?.CurrentDisplayLine;
            if (display == null) return;
            if (string.IsNullOrEmpty(display.text)
                && (display.choices == null || display.choices.Count == 0))
            {
                RefreshHeader();
                return;
            }

            var speaker = display.speakerName;
            var lineKind = display.speaker;
            if (lineKind == LineSpeaker.Narration || speaker == "旁白" || speaker == "Narration")
            {
                speaker = "";
                lineKind = LineSpeaker.Narration;
            }
            if (lineKind == LineSpeaker.System)
                speaker = ScriptLoc.MapSpeaker("系统");
            else if (!string.IsNullOrEmpty(speaker))
                speaker = ScriptLoc.MapSpeaker(speaker);

            SetSpeaker(speaker, lineKind, display.portrait, display.text);
            var kind = lineKind == LineSpeaker.System ? "system"
                : (lineKind == LineSpeaker.Narration || lineKind == LineSpeaker.Inner) ? "narration"
                : "dialogue";
            SetBody(display.text, false, kind);

            if (display.choices != null && display.choices.Count > 0 && waitingForChoice)
            {
                ClearButtons();
                statusText.text = UiLoc.T("ui.make_choice");
                SetAdvanceEnabled(false, true);
                for (int i = 0; i < display.choices.Count; i++)
                {
                    int idx = i;
                    AddChoice(display.choices[i].label, () => SceneDirector.Instance.Choose(idx));
                }
                AddStandardDialogueActions(includeSkip: false);
            }
            else if (!waitingForChoice)
            {
                statusText.text = UiLoc.T("ui.status_advance");
                ClearButtons();
                AddStandardDialogueActions(includeSkip: true);
                SetAdvanceEnabled(true);
            }
            RefreshHeader();
            RefreshAdvanceHint();
        }

        static TMP_FontAsset ResolveUiFont() => TmpFontCatalog.Resolve(GameSettings.UiFontId);

        static TMP_FontAsset ResolveTitleFont() => TmpFontCatalog.Resolve(GameSettings.UiFontId);

        void ApplyActiveFonts()
        {
            font = ResolveUiFont();
            titleFont = ResolveTitleFont() ?? font;
            if (font == null)
            {
                Debug.LogError("[GameUI] ResolveUiFont returned null — all text will be □. Check TmpFontCatalog / Resources/Fonts.");
                return;
            }
            if (canvasRt == null) return;

            float scale = GameSettings.FontSizeScale;
            float spacing = GameSettings.LetterSpacing;

            var texts = canvasRt.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                if (t == null) continue;
                bool titleish = titleRoot != null && t.transform.IsChildOf(titleRoot.transform);
                t.font = titleish ? titleFont : font;
                // Letter-spacing mesh hack breaks Wrap — only apply to non-wrapping lines.
                // Writing corkboard: keep tracking off (scrapbook cards + chrome stay crisp).
                bool wraps = t.enableWordWrapping;
                bool underWriting = (writingMatsRoot != null && t.transform.IsChildOf(writingMatsRoot.transform))
                    || (writingDeskRoot != null && t.transform.IsChildOf(writingDeskRoot.transform));
                ApplyLetterSpacing(t, (wraps || underWriting) ? 0f : spacing);
            }

            if (bodyText != null)
            {
                bodyText.font = font;
                bodyText.fontSize = Mathf.RoundToInt(24f * scale);
                bodyText.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
                bodyText.enableWordWrapping = true;
                bodyText.overflowMode = TextOverflowModes.Overflow;
                ApplyLetterSpacing(bodyText, 0f);
                var contentRt = bodyText.rectTransform;
                if (dialogueScroll != null && dialogueScroll.viewport != null)
                {
                    contentRt.anchorMin = new Vector2(0f, 1f);
                    contentRt.anchorMax = new Vector2(1f, 1f);
                    contentRt.pivot = new Vector2(0.5f, 1f);
                    contentRt.offsetMin = new Vector2(0f, contentRt.offsetMin.y);
                    contentRt.offsetMax = new Vector2(0f, contentRt.offsetMax.y);
                    float w = dialogueScroll.viewport.rect.width;
                    if (w > 1f)
                        contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                }
            }
            if (nameText != null)
            {
                nameText.font = font;
                nameText.fontSize = Mathf.RoundToInt(20f * scale);
                nameText.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
                nameText.enableWordWrapping = false;
                nameText.color = VnTheme.TextPrimary;
                ApplyLetterSpacing(nameText, spacing * 0.35f);
            }
            if (statusText != null)
            {
                statusText.font = font;
                statusText.fontSize = Mathf.RoundToInt(15f * scale);
                statusText.alignment = VnText.ToAlignment(TextAnchor.LowerLeft);
                statusText.enableWordWrapping = false;
                ApplyLetterSpacing(statusText, spacing * 0.35f);
            }
            if (clickHintText != null)
            {
                clickHintText.font = font;
                clickHintText.fontSize = Mathf.RoundToInt(16f * Mathf.Max(1f, scale));
                clickHintText.enableWordWrapping = false;
                ApplyLetterSpacing(clickHintText, spacing * 0.35f);
            }
            if (objectiveText != null)
            {
                objectiveText.font = font;
                objectiveText.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
                objectiveText.enableWordWrapping = true;
                ApplyLetterSpacing(objectiveText, 0f);
            }
            if (locationText != null)
            {
                locationText.font = font;
                ApplyLetterSpacing(locationText, spacing * 0.35f);
            }
            if (backlogTitleText != null)
            {
                backlogTitleText.font = font;
                ApplyLetterSpacing(backlogTitleText, 0f);
            }
            if (backlogText != null)
            {
                backlogText.font = font;
                backlogText.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
                backlogText.enableWordWrapping = true;
                ApplyLetterSpacing(backlogText, 0f);
            }
            ApplyNotebookFonts();
            ApplyInterviewFonts();
            ApplyWritingFonts();
            if (inputField != null)
            {
                inputField.fontAsset = font;
                if (inputField.textComponent != null)
                    inputField.textComponent.font = font;
                if (inputField.placeholder is TMP_Text ph)
                    ph.font = font;
            }
        }

        static void ApplyLetterSpacing(TMP_Text t, float spacing) =>
            VnText.ApplyLetterSpacing(t, spacing);

        void Start()
        {
            SceneDirector.Instance.Bind(OnScriptLine, OnSceneEnd, ShowInvestigationMode, ShowTalkMenu,
                () => ChapterFlowController.Instance.OpenWritingDeskFromScript(),
                OpenLinInterviewFromScript);
            ShowTitle();
        }

        #region Build

        void BuildCanvas()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                DontDestroyOnLoad(es);
            }

            var canvasGo = new GameObject("VnCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasRt = canvasGo.GetComponent<RectTransform>();
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Full-bleed dusk gradient + cinematic layers
            bgImage = CreateFillImage(canvasGo.transform, "Bg", VnTheme.BgBottom);
            var grad = VnTheme.TripleGradient(VnTheme.BgTop, VnTheme.BgMid, VnTheme.BgBottom, 160);
            bgImage.sprite = VnTheme.SpriteFromTexture(grad);
            bgImage.type = Image.Type.Simple;
            bgImage.color = Color.white;

            // Stage art sits above solid gradient, under atmosphere / vignette
            stageArt = CreateFillImage(canvasGo.transform, "StageArt", Color.white);
            stageArt.type = Image.Type.Simple;
            stageArt.preserveAspect = false;
            stageArt.raycastTarget = false;
            stageArt.enabled = false;

            atmosphereWash = CreateFillImage(canvasGo.transform, "Atmosphere", VnTheme.StageWash);
            atmosphereWash.raycastTarget = false;

            vignetteImage = CreateFillImage(canvasGo.transform, "Vignette", Color.white);
            vignetteImage.sprite = VnTheme.SpriteFromTexture(VnTheme.SoftVignette(160));
            vignetteImage.type = Image.Type.Simple;
            vignetteImage.color = Color.white;
            vignetteImage.raycastTarget = false;

            // Letterboxes for cinematic immersion
            var lbTop = CreateImage(canvasGo.transform, "LetterboxTop", VnTheme.Letterbox);
            Stretch(lbTop.rectTransform, new Vector2(0, 1f - VnTheme.LetterboxH), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            lbTop.raycastTarget = false;
            var lbBot = CreateImage(canvasGo.transform, "LetterboxBottom", VnTheme.Letterbox);
            Stretch(lbBot.rectTransform, new Vector2(0, 0), new Vector2(1, VnTheme.LetterboxH), Vector2.zero, Vector2.zero);
            lbBot.raycastTarget = false;

            // Thin amber hairline under top letterbox
            var accent = CreateImage(canvasGo.transform, "AccentStrip", VnTheme.AccentDim);
            Stretch(accent.rectTransform, new Vector2(0.12f, 1f - VnTheme.LetterboxH), new Vector2(0.88f, 1f - VnTheme.LetterboxH),
                new Vector2(0, -2), new Vector2(0, 0));
            accent.raycastTarget = false;

            // Top HUD — sits in letterbox band, never over stage
            topBarImage = CreateImage(canvasGo.transform, "TopBar", VnTheme.TopBar);
            Stretch(topBarImage.rectTransform, new Vector2(0, VnTheme.TopHudBottom), new Vector2(1, 1f - VnTheme.LetterboxH),
                Vector2.zero, Vector2.zero);

            chapterChip = CreateUiText(topBarImage.transform, "ChapterChip", 17, TextAnchor.MiddleLeft,
                VnTheme.Accent, new Vector2(40, 0), new Vector2(380, 36));
            chapterChip.text = "第一章　·　编外保安大福";

            objectiveText = CreateUiText(topBarImage.transform, "Objective", 16, TextAnchor.MiddleLeft,
                VnTheme.TextMuted, new Vector2(420, 0), new Vector2(720, 36));
            var ort = objectiveText.GetComponent<RectTransform>();
            ort.anchorMin = new Vector2(0, 0.5f);
            ort.anchorMax = new Vector2(0, 0.5f);
            ort.pivot = new Vector2(0, 0.5f);

            // Stage / location toast — brief scene-name reveal on enter (not persistent HUD)
            locationText = CreateUiText(canvasGo.transform, "Location", 48, TextAnchor.MiddleCenter,
                new Color(1, 1, 1, 0.10f), Vector2.zero, new Vector2(1400, 80));
            var lrt = locationText.GetComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, VnTheme.StageCenterY);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            locationText.text = "此　间";
            locationFade = locationText.gameObject.AddComponent<CanvasGroup>();
            locationFade.alpha = 0f;
            locationFade.blocksRaycasts = false;
            locationText.gameObject.SetActive(false);

            stageHint = CreateUiText(canvasGo.transform, "StageHint", 20, TextAnchor.MiddleCenter,
                new Color(VnTheme.TextMuted.r, VnTheme.TextMuted.g, VnTheme.TextMuted.b, 0.45f),
                Vector2.zero, new Vector2(900, 36));
            var srt = stageHint.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, VnTheme.StageCenterY - 0.06f);
            stageHintFade = stageHint.gameObject.AddComponent<CanvasGroup>();
            stageHintFade.alpha = 0f;
            stageHintFade.blocksRaycasts = false;
            stageHint.gameObject.SetActive(false);

            // Decorative stage frame lines
            var stageLineL = CreateImage(canvasGo.transform, "StageLineL", VnTheme.AccentDim);
            Stretch(stageLineL.rectTransform, new Vector2(0.18f, 0.62f), new Vector2(0.32f, 0.62f), new Vector2(0, -1), new Vector2(0, 0));
            var stageLineR = CreateImage(canvasGo.transform, "StageLineR", VnTheme.AccentDim);
            Stretch(stageLineR.rectTransform, new Vector2(0.68f, 0.62f), new Vector2(0.82f, 0.62f), new Vector2(0, -1), new Vector2(0, 0));

            // Center-stage prop (above BG / wash, under portraits + dialogue).
            propImage = CreateImage(canvasGo.transform, "Prop", Color.white);
            var propRt = propImage.rectTransform;
            propRt.anchorMin = propRt.anchorMax = new Vector2(0.5f, VnTheme.StageCenterY);
            propRt.pivot = new Vector2(0.5f, 0.5f);
            propRt.sizeDelta = new Vector2(560, 560);
            propImage.type = Image.Type.Simple;
            propImage.preserveAspect = true;
            propImage.raycastTarget = false;
            propImage.enabled = false;
            propImage.gameObject.SetActive(false);

            // SC-03 phone / social feed (above prop, under portrait + dialogue).
            BuildSocialOverlay(canvasGo.transform);

            // Character portrait — upper-right of dialogue box (rests on dialogue top edge).
            // preserveAspect + fixed slot: sprites must share similar canvas aspect
            // (1024x1536). LayoutPortrait keeps on-screen height stable across swaps.
            portraitImage = CreateImage(canvasGo.transform, "Portrait", Color.white);
            LayoutPortraitRect(null);
            portraitImage.type = Image.Type.Simple;
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            portraitImage.enabled = false;
            portraitImage.gameObject.SetActive(false);
            portraitFade = portraitImage.gameObject.AddComponent<CanvasGroup>();
            portraitFade.alpha = 0f;
            portraitFade.blocksRaycasts = false;

            // Stage click catcher (VN: click to advance). Stops below TopHud so 回看/菜单 stay clickable.
            // Kept under title/dialogue/choices; TopBar is raised above this via EnsureTopHudClickable.
            advanceCatcher = CreateFillImage(canvasGo.transform, "AdvanceCatcher", new Color(0, 0, 0, 0.001f));
            Stretch(advanceCatcher.rectTransform, Vector2.zero, new Vector2(1f, VnTheme.TopHudBottom),
                Vector2.zero, Vector2.zero);
            advanceCatcher.raycastTarget = true;
            var advBtn = advanceCatcher.gameObject.AddComponent<Button>();
            advBtn.transition = Selectable.Transition.None;
            advBtn.onClick.AddListener(TryAdvanceByClick);
            advanceCatcher.gameObject.SetActive(false);

            // Title: magazine-on-desk (Resources/VnArt/Title)
            BuildTitleScreen(canvasGo.transform);

            // Dialogue box — original full-width bottom band
            dialoguePanel = CreateImage(canvasGo.transform, "DialogueBox", VnTheme.DialoguePanel);
            Stretch(dialoguePanel.rectTransform, new Vector2(0.07f, VnTheme.LetterboxH + 0.01f), new Vector2(0.93f, VnTheme.DialogueTop),
                Vector2.zero, Vector2.zero);
            var paper = VnArt.GetUi("tex_paper_dark");
            if (paper != null)
            {
                dialoguePanel.sprite = paper;
                dialoguePanel.type = Image.Type.Simple;
                dialoguePanel.color = new Color(0.5f, 0.48f, 0.45f, 0.9f);
            }
            var edge = CreateImage(dialoguePanel.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(edge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2.5f), new Vector2(0, 0));

            dialogueFade = dialoguePanel.gameObject.AddComponent<CanvasGroup>();

            namePlate = CreateImage(dialoguePanel.transform, "NamePlate", VnTheme.NamePlate);
            var nprt = namePlate.rectTransform;
            nprt.anchorMin = new Vector2(0, 1);
            nprt.anchorMax = new Vector2(0, 1);
            nprt.pivot = new Vector2(0, 0);
            nprt.anchoredPosition = new Vector2(24, 6);
            nprt.sizeDelta = new Vector2(210, 40);
            var nameAccent = CreateImage(namePlate.transform, "NameAccent", VnTheme.Accent);
            Stretch(nameAccent.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(3, 0));

            nameText = CreateUiText(namePlate.transform, "Name", 20, TextAnchor.MiddleCenter,
                VnTheme.TextPrimary, Vector2.zero, new Vector2(190, 36));
            StretchFull(nameText.GetComponent<RectTransform>());
            nameText.text = "小凌";

            // Scrollable dialogue body — prevents overflow into choices
            var bodyHost = CreateImage(dialoguePanel.transform, "BodyHost", new Color(0, 0, 0, 0.001f));
            Stretch(bodyHost.rectTransform, Vector2.zero, Vector2.one, new Vector2(28, 48), new Vector2(-28, -48));
            dialogueScroll = bodyHost.gameObject.AddComponent<ScrollRect>();
            dialogueScroll.horizontal = false;
            dialogueScroll.vertical = true;
            dialogueScroll.movementType = ScrollRect.MovementType.Clamped;
            dialogueScroll.scrollSensitivity = 24f;

            var bodyViewport = CreateImage(bodyHost.transform, "Viewport", new Color(0, 0, 0, 0.01f));
            StretchFull(bodyViewport.rectTransform);
            bodyViewport.gameObject.AddComponent<RectMask2D>();

            var bodyContent = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            bodyContent.transform.SetParent(bodyViewport.transform, false);
            var bcrt = bodyContent.GetComponent<RectTransform>();
            bcrt.anchorMin = new Vector2(0, 1);
            bcrt.anchorMax = new Vector2(1, 1);
            bcrt.pivot = new Vector2(0.5f, 1);
            bcrt.sizeDelta = Vector2.zero;
            bodyContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            bodyText = bodyContent.AddComponent<TextMeshProUGUI>();
            bodyText.font = font;
            bodyText.fontSize = 25;
            bodyText.color = VnTheme.TextPrimary;
            bodyText.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Overflow;
            bodyText.lineSpacing = 20f;
            bodyText.raycastTarget = false;

            dialogueScroll.viewport = bodyViewport.rectTransform;
            dialogueScroll.content = bcrt;

            statusText = CreateUiText(dialoguePanel.transform, "Status", 15, TextAnchor.LowerLeft,
                VnTheme.TextMuted, new Vector2(28, 12), new Vector2(480, 24));
            var stRt = statusText.GetComponent<RectTransform>();
            stRt.anchorMin = stRt.anchorMax = new Vector2(0, 0);
            stRt.pivot = new Vector2(0, 0);

            clickHintText = CreateUiText(dialoguePanel.transform, "ClickHint", 14, TextAnchor.LowerRight,
                new Color(VnTheme.TextMuted.r, VnTheme.TextMuted.g, VnTheme.TextMuted.b, 0.55f),
                new Vector2(-28, 12), new Vector2(280, 24));
            var chRt = clickHintText.GetComponent<RectTransform>();
            chRt.anchorMin = chRt.anchorMax = new Vector2(1, 0);
            chRt.pivot = new Vector2(1, 0);
            clickHintText.text = UiLoc.T("ui.click_continue");

            // Choice band — soft panel above dialogue
            choiceHostImage = CreateImage(canvasGo.transform, "ChoiceHost", new Color(0, 0, 0, 0.001f));
            Stretch(choiceHostImage.rectTransform, new Vector2(0.16f, VnTheme.ChoiceBottom), new Vector2(0.84f, VnTheme.ChoiceTop),
                Vector2.zero, Vector2.zero);
            var choiceHost = choiceHostImage;
            var choiceScroll = choiceHost.gameObject.AddComponent<ScrollRect>();
            choiceScroll.horizontal = false;
            choiceScroll.vertical = true;
            choiceScroll.movementType = ScrollRect.MovementType.Clamped;
            var choiceViewport = CreateImage(choiceHost.transform, "Viewport", new Color(0, 0, 0, 0.01f));
            StretchFull(choiceViewport.rectTransform);
            choiceViewport.gameObject.AddComponent<RectMask2D>();
            choiceRoot = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).transform;
            choiceRoot.SetParent(choiceViewport.transform, false);
            var crt = choiceRoot.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = Vector2.zero;
            var vlg = choiceRoot.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            choiceRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            choiceScroll.viewport = choiceViewport.rectTransform;
            choiceScroll.content = crt;
            choiceHost.gameObject.SetActive(false);

            // Action row — inside dialogue footer strip (right side), no overlap with body
            buttonRoot = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            buttonRoot.SetParent(dialoguePanel.transform, false);
            var br = buttonRoot.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.35f, 0);
            br.anchorMax = new Vector2(1, 0);
            br.pivot = new Vector2(1, 0);
            br.anchoredPosition = new Vector2(-16, 8);
            br.sizeDelta = new Vector2(0, 34);
            var hlg = buttonRoot.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childForceExpandWidth = false;
            hlg.childControlWidth = true;

            inputField = CreateVnInput(dialoguePanel.transform);
            inputField.gameObject.SetActive(false);

            dialogueClick = dialoguePanel.gameObject.AddComponent<Button>();
            dialogueClick.transition = Selectable.Transition.None;
            dialogueClick.onClick.AddListener(TryAdvanceByClick);
            dialoguePanel.raycastTarget = true;

            var hudActions = new GameObject("HudActions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            hudActions.transform.SetParent(topBarImage.transform, false);
            var hart = hudActions.GetComponent<RectTransform>();
            hart.anchorMin = hart.anchorMax = new Vector2(1, 0.5f);
            hart.pivot = new Vector2(1, 0.5f);
            hart.anchoredPosition = new Vector2(-28, 0);
            hart.sizeDelta = new Vector2(260, 36);
            var hhlg = hudActions.GetComponent<HorizontalLayoutGroup>();
            hhlg.spacing = 8;
            hhlg.childAlignment = TextAnchor.MiddleRight;
            hhlg.childForceExpandWidth = false;
            SpawnHudChip(hudActions.transform, "回看", OpenBacklog);
            SpawnHudChip(hudActions.transform, "菜单", OpenMenu);
            EnsureTopHudClickable();

            BuildInvestigateOverlay(canvasGo.transform);
            BuildInterviewOverlay(canvasGo.transform);
            BuildMenuOverlay(canvasGo.transform);
            BuildBacklogOverlay(canvasGo.transform);
            BuildNotebookOverlay(canvasGo.transform);
            BuildWritingMaterialsOverlay(canvasGo.transform);
            BuildWritingDeskOverlay(canvasGo.transform);
            BuildSaveLoadOverlay(canvasGo.transform);
            BuildConfirmOverlay(canvasGo.transform);
            BuildSettingsOverlay(canvasGo.transform);
            BuildHideDialogueControl(canvasGo.transform);
            BuildSceneFadeOverlay(canvasGo.transform);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BuildDebugJumpPanel(canvasGo.transform);
#endif
        }

        void BuildHideDialogueControl(Transform parent)
        {
            var go = new GameObject("HideDialogue", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-18f, 210f);
            rt.sizeDelta = new Vector2(112f, 34f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.12f, 0.82f);
            hideDialogueBtn = go.GetComponent<Button>();
            hideDialogueBtn.transition = Selectable.Transition.ColorTint;
            var colors = hideDialogueBtn.colors;
            colors.highlightedColor = VnTheme.ButtonHover;
            colors.pressedColor = VnTheme.AccentSoft;
            hideDialogueBtn.colors = colors;
            hideDialogueBtn.onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                ToggleDialogueHidden();
            });

            hideDialogueLabel = CreateUiText(go.transform, "Label", 15, TextAnchor.MiddleCenter,
                VnTheme.TextPrimary, Vector2.zero, Vector2.zero);
            StretchFull(hideDialogueLabel.GetComponent<RectTransform>());
            hideDialogueLabel.text = UiLoc.T("ui.hide_dialogue");
            hideDialogueLabel.raycastTarget = false;
            go.SetActive(false);
        }

        void BuildSceneFadeOverlay(Transform parent)
        {
            sceneFadeImage = CreateFillImage(parent, "SceneFade", Color.black);
            sceneFadeImage.raycastTarget = false;
            sceneFadeCg = sceneFadeImage.gameObject.AddComponent<CanvasGroup>();
            sceneFadeCg.alpha = 0f;
            sceneFadeCg.blocksRaycasts = false;
            sceneFadeCg.interactable = false;
            sceneFadeImage.gameObject.SetActive(true);
            sceneFadeImage.transform.SetAsLastSibling();
        }

        /// <summary>1–2s blackout around scene switches; BGM crossfades via BgmController.</summary>
        public void RunSceneTransition(Action enterScene)
        {
            if (enterScene == null) return;
            if (sceneTransitioning)
            {
                enterScene();
                return;
            }
            if (sceneFadeCo != null)
                StopCoroutine(sceneFadeCo);
            sceneFadeCo = StartCoroutine(SceneFadeCo(enterScene));
        }

        IEnumerator SceneFadeCo(Action enterScene)
        {
            sceneTransitioning = true;
            SetDialogueHidden(false);
            if (sceneFadeImage != null)
            {
                sceneFadeImage.transform.SetAsLastSibling();
                sceneFadeImage.raycastTarget = true;
            }
            if (sceneFadeCg != null)
                sceneFadeCg.blocksRaycasts = true;

            // Soft BGM dip while fading to black (new scene BGM will crossfade in).
            BgmController.Instance?.BeginTransitionDip(0.85f);

            float t = 0f;
            const float fadeIn = 0.55f;
            const float hold = 0.55f;
            const float fadeOut = 0.55f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                if (sceneFadeCg != null)
                    sceneFadeCg.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeIn));
                yield return null;
            }
            if (sceneFadeCg != null)
                sceneFadeCg.alpha = 1f;

            yield return new WaitForSecondsRealtime(hold);

            try
            {
                enterScene?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                if (sceneFadeCg != null)
                    sceneFadeCg.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeOut));
                yield return null;
            }
            if (sceneFadeCg != null)
            {
                sceneFadeCg.alpha = 0f;
                sceneFadeCg.blocksRaycasts = false;
            }
            if (sceneFadeImage != null)
                sceneFadeImage.raycastTarget = false;

            sceneTransitioning = false;
            sceneFadeCo = null;
        }

        bool CanHideDialogue()
        {
            if (mode == Mode.Title || mode == Mode.Interview || mode == Mode.Writing || mode == Mode.Epilogue)
                return false;
            if (mode == Mode.Menu || mode == Mode.Backlog || mode == Mode.Notebook)
                return false;
            if (mode == Mode.Investigate && investigateHotspotsVisible)
                return false;
            return mode == Mode.Dialogue || mode == Mode.Talk
                   || (mode == Mode.Investigate && !investigateHotspotsVisible);
        }

        void ToggleDialogueHidden()
        {
            if (!CanHideDialogue() && !dialogueHidden) return;
            SetDialogueHidden(!dialogueHidden);
        }

        void SetDialogueHidden(bool hidden)
        {
            bool wasHidden = dialogueHidden;
            dialogueHidden = hidden && CanHideDialogue();
            if (hideDialogueLabel != null)
                hideDialogueLabel.text = dialogueHidden
                    ? UiLoc.T("ui.show_dialogue")
                    : UiLoc.T("ui.hide_dialogue");
            if (!dialogueHidden && wasHidden && dialoguePanel != null
                && (mode == Mode.Dialogue || mode == Mode.Talk
                    || (mode == Mode.Investigate && !investigateHotspotsVisible)))
            {
                dialoguePanel.gameObject.SetActive(true);
            }
            ApplyDialogueHiddenChrome();
        }

        void ApplyDialogueHiddenChrome()
        {
            if (hideDialogueBtn != null)
            {
                bool showBtn = CanHideDialogue() || dialogueHidden;
                hideDialogueBtn.gameObject.SetActive(showBtn);
                if (showBtn)
                    hideDialogueBtn.transform.SetAsLastSibling();
            }

            if (!dialogueHidden)
                return;

            if (dialoguePanel != null)
                dialoguePanel.gameObject.SetActive(false);
            if (choiceHostImage != null)
                choiceHostImage.gameObject.SetActive(false);
            if (portraitImage != null)
                portraitImage.gameObject.SetActive(false);
            // Stage click restores (also advances only after restore on next click).
            if (advanceCatcher != null)
            {
                advanceCatcher.gameObject.SetActive(true);
                EnsureTopHudClickable();
            }
        }

        /// <summary>
        /// Keep TopBar (回看/菜单) above stage catchers / interview HitCatcher so chrome
        /// chips remain clickable while overlays still cover the stage.
        /// </summary>
        void EnsureTopHudClickable()
        {
            if (topBarImage == null) return;
            int topIdx = topBarImage.transform.GetSiblingIndex();
            int raiseAbove = topIdx;
            if (advanceCatcher != null)
                raiseAbove = Mathf.Max(raiseAbove, advanceCatcher.transform.GetSiblingIndex());
            // Interview overlay is brought above gameplay and previously swallowed TopBar clicks.
            if (interviewRoot != null && interviewRoot.activeInHierarchy)
                raiseAbove = Mathf.Max(raiseAbove, interviewRoot.transform.GetSiblingIndex());
            if (topIdx <= raiseAbove)
                topBarImage.transform.SetSiblingIndex(raiseAbove + 1);
            // Menus / backlog / confirm must stay above the TopBar chips.
            BringOverlayStackToFront();
        }

        void BuildInvestigateOverlay(Transform parent)
        {
            // Full-stage investigate chrome: hotspots over scene art, thin HUD, bottom action chips
            investigateRoot = new GameObject("InvestigateOverlay", typeof(RectTransform));
            investigateRoot.transform.SetParent(parent, false);
            StretchFull(investigateRoot.GetComponent<RectTransform>());

            investigateHotspotLayer = new GameObject("HotspotLayer", typeof(RectTransform)).transform;
            investigateHotspotLayer.SetParent(investigateRoot.transform, false);
            Stretch(investigateHotspotLayer.GetComponent<RectTransform>(),
                new Vector2(0f, VnTheme.LetterboxH), new Vector2(1f, VnTheme.TopHudBottom),
                Vector2.zero, Vector2.zero);

            var chrome = CreateImage(investigateRoot.transform, "ThinChrome", new Color(0.05f, 0.06f, 0.08f, 0.55f));
            Stretch(chrome.rectTransform, new Vector2(0.04f, VnTheme.TopHudBottom - 0.055f), new Vector2(0.55f, VnTheme.TopHudBottom - 0.008f),
                Vector2.zero, Vector2.zero);
            chrome.raycastTarget = false;
            var chromeEdge = CreateImage(chrome.transform, "Edge", VnTheme.AccentDim);
            Stretch(chromeEdge.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(3, 0));
            chromeEdge.raycastTarget = false;

            investigateTitle = CreateUiText(chrome.transform, "Title", 20, TextAnchor.MiddleLeft,
                VnTheme.Accent, new Vector2(18, 0), new Vector2(420, 28));
            var tr = investigateTitle.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 0.5f);
            tr.anchorMax = new Vector2(0, 0.5f);
            tr.pivot = new Vector2(0, 0.5f);
            investigateTitle.text = "槐安社区　·　调查";
            investigateTitle.fontStyle = FontStyles.Bold;

            investigateIntelHint = CreateUiText(chrome.transform, "IntelHint", 15, TextAnchor.MiddleLeft,
                VnTheme.TextMuted, new Vector2(18, -14), new Vector2(480, 22));
            var ihr = investigateIntelHint.GetComponent<RectTransform>();
            ihr.anchorMin = ihr.anchorMax = new Vector2(0, 0.5f);
            ihr.pivot = new Vector2(0, 0.5f);
            investigateIntelHint.text = "点击场景中的物件调查";

            investigateHoverLabel = CreateUiText(investigateRoot.transform, "HoverLabel", 18, TextAnchor.MiddleCenter,
                VnTheme.TextPrimary, Vector2.zero, new Vector2(280, 36));
            var hlRt = investigateHoverLabel.GetComponent<RectTransform>();
            hlRt.anchorMin = hlRt.anchorMax = new Vector2(0.5f, 0.5f);
            hlRt.pivot = new Vector2(0.5f, 0f);
            investigateHoverLabel.fontStyle = FontStyles.Bold;
            investigateHoverLabel.gameObject.SetActive(false);
            var hoverBg = CreateImage(investigateHoverLabel.transform, "Bg", new Color(0.06f, 0.07f, 0.09f, 0.82f));
            Stretch(hoverBg.rectTransform, Vector2.zero, Vector2.one, new Vector2(-14, -6), new Vector2(14, 6));
            hoverBg.raycastTarget = false;
            hoverBg.transform.SetAsFirstSibling();

            investigateActions = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            investigateActions.SetParent(investigateRoot.transform, false);
            var ar = investigateActions.GetComponent<RectTransform>();
            Stretch(ar, new Vector2(0.04f, VnTheme.LetterboxH + 0.01f), new Vector2(0.96f, VnTheme.LetterboxH + 0.075f),
                Vector2.zero, Vector2.zero);
            var ahlg = investigateActions.GetComponent<HorizontalLayoutGroup>();
            ahlg.spacing = 8;
            ahlg.childAlignment = TextAnchor.MiddleRight;
            ahlg.childForceExpandWidth = false;
            ahlg.childControlWidth = true;
            ahlg.padding = new RectOffset(4, 4, 0, 0);

            investigateRoot.SetActive(false);
        }

        // BuildInterviewOverlay → GameUI.Interview.cs (scrapbook redesign)

        void SpawnHudChip(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Hud_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.9f);
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = 88;
            le.preferredHeight = 34;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                action();
            });
            var tgo = new GameObject("T", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            StretchFull(tgo.GetComponent<RectTransform>());
            var tx = tgo.AddComponent<TextMeshProUGUI>();
            tx.font = font;
            tx.fontSize = 16;
            tx.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
            tx.color = VnTheme.TextPrimary;
            tx.text = label;
            tx.raycastTarget = false;
        }

        void BuildMenuOverlay(Transform parent)
        {
            menuRoot = new GameObject("MenuOverlay", typeof(RectTransform));
            menuRoot.transform.SetParent(parent, false);
            StretchFull(menuRoot.GetComponent<RectTransform>());
            var dim = CreateImage(menuRoot.transform, "Dim", VnTheme.OverlayDim);
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = CreateImage(menuRoot.transform, "Panel", VnTheme.Paper);
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(420, 560);
            var menuEdge = CreateImage(panel.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(menuEdge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2.5f), new Vector2(0, 0));

            var title = CreateUiText(panel.transform, "MenuTitle", 26, TextAnchor.UpperCenter,
                VnTheme.Accent, new Vector2(0, -22), new Vector2(360, 40));
            menuTitleText = title;
            title.text = UiLoc.T("ui.menu");
            title.fontStyle = FontStyles.Bold;
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1);

            var list = new GameObject("MenuList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            list.transform.SetParent(panel.transform, false);
            var lrt = list.GetComponent<RectTransform>();
            Stretch(lrt, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
            var v = list.GetComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childForceExpandWidth = true;
            v.childControlHeight = true;

            void Item(string locKey, UnityEngine.Events.UnityAction act)
            {
                var go = new GameObject(locKey, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(list.transform, false);
                go.GetComponent<Image>().color = VnTheme.Button;
                go.GetComponent<LayoutElement>().preferredHeight = 46;
                go.GetComponent<Button>().onClick.AddListener(act);
                var tg = new GameObject("T", typeof(RectTransform));
                tg.transform.SetParent(go.transform, false);
                StretchFull(tg.GetComponent<RectTransform>());
                var tx = tg.AddComponent<TextMeshProUGUI>();
                tx.font = font;
                tx.fontSize = 20;
                tx.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
                tx.color = VnTheme.TextPrimary;
                tx.text = UiLoc.T(locKey);
                tx.raycastTarget = false;
                var tag = go.AddComponent<LocTag>();
                tag.key = locKey;
                tag.target = tx;
            }

            Item("ui.menu.resume", CloseMenu);
            Item("ui.menu.backlog", () => { CloseMenuSilent(); OpenBacklog(); });
            Item("ui.menu.auto_load", () =>
            {
                CloseMenuSilent();
                if (SaveSystem.SlotExists(SaveSystem.AutoSlot))
                    ChapterFlowController.Instance.LoadSlot(SaveSystem.AutoSlot);
                else
                    statusText.text = UiLoc.T("ui.no_autosave");
            });
            Item("ui.menu.save", () => { CloseMenuSilent(); OpenSaveLoad(true); });
            Item("ui.menu.load", () => { CloseMenuSilent(); OpenSaveLoad(false); });
            Item("ui.menu.notebook", () => { CloseMenuSilent(); OpenNotebook(); });
            Item("ui.menu.settings", () => { OpenSettingsFromMenu(); });
            Item("ui.menu.title", () =>
            {
                CloseMenuSilent();
                ChapterFlowController.Instance.GoToTitle();
            });

            menuRoot.SetActive(false);
        }

        void BuildSaveLoadOverlay(Transform parent)
        {
            saveLoadRoot = new GameObject("SaveLoadOverlay", typeof(RectTransform));
            saveLoadRoot.transform.SetParent(parent, false);
            StretchFull(saveLoadRoot.GetComponent<RectTransform>());
            var dim = CreateImage(saveLoadRoot.transform, "Dim", VnTheme.OverlayDim);
            StretchFull(dim.rectTransform);

            var panel = CreateImage(saveLoadRoot.transform, "Panel", VnTheme.Paper);
            Stretch(panel.rectTransform, new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.88f), Vector2.zero, Vector2.zero);
            var slEdge = CreateImage(panel.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(slEdge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2.5f), new Vector2(0, 0));

            saveLoadTitle = CreateUiText(panel.transform, "Title", 26, TextAnchor.UpperLeft,
                VnTheme.Accent, new Vector2(28, -20), new Vector2(400, 36));
            saveLoadTitle.text = "存档";

            var closeBtn = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(panel.transform, false);
            var cbrt = closeBtn.GetComponent<RectTransform>();
            cbrt.anchorMin = cbrt.anchorMax = new Vector2(1, 1);
            cbrt.pivot = new Vector2(1, 1);
            cbrt.anchoredPosition = new Vector2(-16, -12);
            cbrt.sizeDelta = new Vector2(100, 36);
            closeBtn.GetComponent<Image>().color = VnTheme.Button;
            closeBtn.GetComponent<Button>().onClick.AddListener(CloseSaveLoad);
            var ct = new GameObject("T", typeof(RectTransform));
            ct.transform.SetParent(closeBtn.transform, false);
            StretchFull(ct.GetComponent<RectTransform>());
            var ctx = ct.AddComponent<TextMeshProUGUI>();
            ctx.font = font;
            ctx.fontSize = 18;
            ctx.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
            ctx.color = VnTheme.TextPrimary;
            ctx.text = "关闭";
            ctx.raycastTarget = false;

            var listGo = new GameObject("SlotList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(panel.transform, false);
            saveLoadList = listGo.transform;
            var lrt = listGo.GetComponent<RectTransform>();
            Stretch(lrt, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
            var v = listGo.GetComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childForceExpandWidth = true;
            v.childControlHeight = true;
            listGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            saveLoadRoot.SetActive(false);
        }

        void BuildConfirmOverlay(Transform parent)
        {
            confirmRoot = new GameObject("ConfirmOverlay", typeof(RectTransform));
            confirmRoot.transform.SetParent(parent, false);
            StretchFull(confirmRoot.GetComponent<RectTransform>());
            var dim = CreateImage(confirmRoot.transform, "Dim", VnTheme.OverlayDim);
            StretchFull(dim.rectTransform);

            var panel = CreateImage(confirmRoot.transform, "Panel", VnTheme.Paper);
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(500, 210);
            var cfEdge = CreateImage(panel.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(cfEdge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2.5f), new Vector2(0, 0));

            confirmText = CreateUiText(panel.transform, "Msg", 22, TextAnchor.UpperCenter,
                VnTheme.TextPrimary, new Vector2(0, -36), new Vector2(460, 80));
            confirmText.text = "覆盖该存档？";
            var ctr = confirmText.GetComponent<RectTransform>();
            ctr.anchorMin = ctr.anchorMax = new Vector2(0.5f, 1);

            void CBtn(string label, float x, UnityEngine.Events.UnityAction act)
            {
                var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(panel.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.anchoredPosition = new Vector2(x, 28);
                rt.sizeDelta = new Vector2(140, 44);
                go.GetComponent<Image>().color = VnTheme.Button;
                go.GetComponent<Button>().onClick.AddListener(act);
                var tg = new GameObject("T", typeof(RectTransform));
                tg.transform.SetParent(go.transform, false);
                StretchFull(tg.GetComponent<RectTransform>());
                var tx = tg.AddComponent<TextMeshProUGUI>();
                tx.font = font;
                tx.fontSize = 20;
                tx.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
                tx.color = VnTheme.TextPrimary;
                tx.text = label;
                tx.raycastTarget = false;
            }

            CBtn("确认", -90, ConfirmOverwriteYes);
            CBtn("取消", 90, () => { confirmRoot.SetActive(false); pendingOverwriteSlot = -999; });

            confirmRoot.SetActive(false);
        }

        void OpenSaveLoad(bool isSave)
        {
            if (mode == Mode.Title && isSave) return;
            if (mode != Mode.Menu && mode != Mode.Backlog && mode != Mode.Notebook && mode != Mode.Title)
            {
                // keep returnFromOverlay if already set from menu
            }
            else if (mode != Mode.Title && mode != Mode.Menu)
            {
                returnFromOverlay = mode;
                savedWaitingForChoice = waitingForChoice;
            }

            saveLoadIsSave = isSave;
            mode = Mode.Menu; // treat as overlay
            SetAdvanceEnabled(false);
            saveLoadTitle.text = isSave ? "存档" : "读档";
            RefreshSaveLoadSlots();
            saveLoadRoot.SetActive(true);
        }

        void CloseSaveLoad()
        {
            if (saveLoadRoot) saveLoadRoot.SetActive(false);
            if (confirmRoot) confirmRoot.SetActive(false);
            if (mode == Mode.Title || titleRoot.activeSelf)
            {
                ShowTitle();
                return;
            }
            ResumeOverlayReturn();
        }

        void RefreshSaveLoadSlots()
        {
            foreach (Transform child in saveLoadList)
                Destroy(child.gameObject);

            // Save: only manual slots (auto is automatic)
            // Load: auto + manual
            var slots = SaveSystem.ListSlots(!saveLoadIsSave);
            if (saveLoadIsSave)
            {
                // manual only
                slots = new List<SaveSlotInfo>();
                for (int i = 0; i < SaveSystem.ManualSlotCount; i++)
                    slots.Add(SaveSystem.GetSlotInfo(i));
            }

            foreach (var info in slots)
            {
                var captured = info;
                var go = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(saveLoadList, false);
                go.GetComponent<Image>().color = info.empty ? new Color(0.12f, 0.12f, 0.14f, 0.9f) : VnTheme.Button;
                go.GetComponent<LayoutElement>().preferredHeight = 72;
                go.GetComponent<Button>().onClick.AddListener(() => OnSlotClicked(captured));

                var label = new GameObject("L", typeof(RectTransform));
                label.transform.SetParent(go.transform, false);
                Stretch(label.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(16, 8), new Vector2(-16, -8));
                var tx = label.AddComponent<TextMeshProUGUI>();
                tx.font = font;
                tx.fontSize = 18;
                tx.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
                tx.color = VnTheme.TextPrimary;
                tx.enableWordWrapping = true;
                tx.overflowMode = TextOverflowModes.Truncate;
                tx.raycastTarget = false;
                if (info.empty)
                    tx.text = (saveLoadIsSave ? $"存档位 {info.slot + 1}" : info.label) + "\n空";
                else
                    tx.text = info.label + "\n" + info.detail;
            }
        }

        void OnSlotClicked(SaveSlotInfo info)
        {
            if (saveLoadIsSave)
            {
                if (!info.empty)
                {
                    pendingOverwriteSlot = info.slot;
                    confirmText.text = $"存档位 {info.slot + 1} 已有数据，确认覆盖？";
                    confirmRoot.SetActive(true);
                    return;
                }
                SyncUiModeToSave();
                SaveSystem.SaveManual(info.slot);
                statusText.text = $"已写入存档位 {info.slot + 1}";
                RefreshSaveLoadSlots();
            }
            else
            {
                if (info.empty)
                {
                    statusText.text = "该槽位为空";
                    return;
                }
                if (saveLoadRoot) saveLoadRoot.SetActive(false);
                if (confirmRoot) confirmRoot.SetActive(false);
                if (menuRoot) menuRoot.SetActive(false);
                ChapterFlowController.Instance.LoadSlot(info.slot);
            }
        }

        void ConfirmOverwriteYes()
        {
            confirmRoot.SetActive(false);
            if (pendingOverwriteSlot < 0) return;
            SyncUiModeToSave();
            SaveSystem.SaveManual(pendingOverwriteSlot);
            statusText.text = $"已覆盖存档位 {pendingOverwriteSlot + 1}";
            pendingOverwriteSlot = -999;
            RefreshSaveLoadSlots();
        }

        void SyncUiModeToSave()
        {
            if (GameState.Instance == null) return;
            switch (returnFromOverlay)
            {
                case Mode.Investigate: GameState.Instance.Data.uiMode = "investigate"; break;
                case Mode.Interview:
                    GameState.Instance.Data.uiMode =
                        InterviewController.Instance != null &&
                        InterviewController.Instance.Subject == InterviewSubject.Lin
                            ? "interview_lin" : "interview_dafu";
                    break;
                case Mode.Writing: GameState.Instance.Data.uiMode = "writing"; break;
                case Mode.Epilogue: GameState.Instance.Data.uiMode = "epilogue"; break;
                default: GameState.Instance.Data.uiMode = "dialogue"; break;
            }
        }

        void BuildBacklogOverlay(Transform parent)
        {
            backlogRoot = new GameObject("BacklogOverlay", typeof(RectTransform));
            backlogRoot.transform.SetParent(parent, false);
            StretchFull(backlogRoot.GetComponent<RectTransform>());
            var dim = CreateImage(backlogRoot.transform, "Dim", VnTheme.OverlayDim);
            StretchFull(dim.rectTransform);

            var panel = CreateImage(backlogRoot.transform, "Panel", VnTheme.Paper);
            Stretch(panel.rectTransform, new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.88f), Vector2.zero, Vector2.zero);
            var edge = CreateImage(panel.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(edge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), new Vector2(0, 0));

            // Dedicated top header so title never sits over scroll content.
            const float headerH = 56f;
            backlogTitleText = CreateUiText(panel.transform, "Title", 26, TextAnchor.MiddleLeft,
                VnTheme.Accent, Vector2.zero, Vector2.zero);
            backlogTitleText.text = UiLoc.T("ui.backlog.title", "对话回看");
            backlogTitleText.fontStyle = FontStyles.Bold;
            var titleRt = backlogTitleText.GetComponent<RectTransform>();
            Stretch(titleRt, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(28f, -headerH), new Vector2(-120f, -8f));

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var srt = scrollGo.GetComponent<RectTransform>();
            Stretch(srt, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 1f),
                Vector2.zero, new Vector2(0f, -headerH));
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            backlogScroll = scrollGo.GetComponent<ScrollRect>();
            backlogScroll.horizontal = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(0, 0);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            backlogText = content.AddComponent<TextMeshProUGUI>();
            backlogText.font = font;
            backlogText.fontSize = 22;
            backlogText.color = VnTheme.TextPrimary;
            backlogText.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
            backlogText.enableWordWrapping = true;
            backlogText.overflowMode = TextOverflowModes.Overflow;
            backlogText.lineSpacing = 20f;

            backlogScroll.viewport = viewport.GetComponent<RectTransform>();
            backlogScroll.content = crt;

            var closeBtn = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(panel.transform, false);
            var cbrt = closeBtn.GetComponent<RectTransform>();
            cbrt.anchorMin = cbrt.anchorMax = new Vector2(1, 1);
            cbrt.pivot = new Vector2(1, 1);
            cbrt.anchoredPosition = new Vector2(-16, -12);
            cbrt.sizeDelta = new Vector2(100, 36);
            closeBtn.GetComponent<Image>().color = VnTheme.Button;
            closeBtn.GetComponent<Button>().onClick.AddListener(CloseBacklog);
            var ct = new GameObject("T", typeof(RectTransform));
            ct.transform.SetParent(closeBtn.transform, false);
            StretchFull(ct.GetComponent<RectTransform>());
            var ctx = ct.AddComponent<TextMeshProUGUI>();
            ctx.font = font;
            ctx.fontSize = 18;
            ctx.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
            ctx.color = VnTheme.TextPrimary;
            ctx.text = "关闭";
            ctx.raycastTarget = false;

            backlogRoot.SetActive(false);
        }

        void BuildTitleScreen(Transform canvas)
        {
            titleRoot = new GameObject("TitleRoot", typeof(RectTransform));
            titleRoot.transform.SetParent(canvas, false);
            StretchFull(titleRoot.GetComponent<RectTransform>());

            var desk = CreateTitleSprite(titleRoot.transform, "DeskBg", "title_desk_bg", Color.white, true);
            if (desk.sprite == null)
                desk.color = new Color(0.12f, 0.08f, 0.06f, 1f);

            var magHost = new GameObject("MagazineHost", typeof(RectTransform));
            magHost.transform.SetParent(titleRoot.transform, false);
            TitleMenuLayout.Apply(magHost.GetComponent<RectTransform>(), "magazine_host",
                new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.96f));
            MakeTitleEditable(magHost, "magazine_host");

            var shadow = CreateTitleSprite(magHost.transform, "MagazineShadow", "title_magazine_shadow",
                new Color(1f, 1f, 1f, 0.45f), true);
            var shadowRt = shadow.rectTransform;
            shadowRt.anchoredPosition = new Vector2(18f, -22f);
            shadow.raycastTarget = false;

            var mag = CreateTitleSprite(magHost.transform, "Magazine", "title_magazine_open", Color.white, true);
            mag.raycastTarget = false;

            // Left page art + branding
            var left = new GameObject("LeftPage", typeof(RectTransform));
            left.transform.SetParent(magHost.transform, false);
            TitleMenuLayout.Apply(left.GetComponent<RectTransform>(), "left_page",
                new Vector2(0.04f, 0.08f), new Vector2(0.48f, 0.92f));
            MakeTitleEditable(left, "left_page");

            var feature = CreateTitleSprite(left.transform, "FeatureArt", "title_feature_art", Color.white, false);
            TitleMenuLayout.Apply(feature.rectTransform, "feature_art",
                new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.96f));
            feature.preserveAspect = true;
            MakeTitleEditable(feature.gameObject, "feature_art");

            titleLogoCn = CreateTitleSprite(left.transform, "LogoCn", "title_logo_cn", Color.white, false);
            TitleMenuLayout.Apply(titleLogoCn.rectTransform, "logo_cn",
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.96f));
            titleLogoCn.preserveAspect = true;
            MakeTitleEditable(titleLogoCn.gameObject, "logo_cn");

            titleLogoEn = CreateTitleSprite(left.transform, "LogoEn", "title_logo_en", Color.white, false);
            TitleMenuLayout.Apply(titleLogoEn.rectTransform, "logo_en",
                new Vector2(0.10f, 0.68f), new Vector2(0.90f, 0.80f));
            titleLogoEn.preserveAspect = true;
            MakeTitleEditable(titleLogoEn.gameObject, "logo_en");

            titleBrand = CreateUiText(left.transform, "Brand", 50, TextAnchor.MiddleCenter,
                new Color(0.10f, 0.08f, 0.07f, 0.98f), Vector2.zero, new Vector2(420, 64));
            titleBrand.text = UiLoc.T("ui.title.brand");
            StyleTitleMenuFittedText(titleBrand, 50, true);
            TitleMenuLayout.Apply(titleBrand.GetComponent<RectTransform>(), "logo_cn",
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f));
            titleBrand.gameObject.SetActive(titleLogoCn.sprite == null);

            var quoteBox = CreateTitleSprite(left.transform, "QuoteBox", "title_quote_box_l", Color.white, false);
            TitleMenuLayout.Apply(quoteBox.rectTransform, "quote_box",
                new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.36f));
            quoteBox.preserveAspect = true;
            MakeTitleEditable(quoteBox.gameObject, "quote_box");

            titleSubtitle = CreateUiText(left.transform, "Sub", 20, TextAnchor.MiddleLeft,
                new Color(0.16f, 0.12f, 0.09f, 0.94f), Vector2.zero, new Vector2(360, 80));
            titleSubtitle.text = UiLoc.T("ui.title.subtitle");
            StyleTitleMenuBodyText(titleSubtitle, 20);
            TitleMenuLayout.Apply(titleSubtitle.GetComponent<RectTransform>(), "subtitle",
                new Vector2(0.08f, 0.13f), new Vector2(0.62f, 0.34f));
            MakeTitleEditable(titleSubtitle.gameObject, "subtitle");

            var blurb = CreateTitleSprite(left.transform, "BlurbDeco", "title_blurb_deco", Color.white, false);
            TitleMenuLayout.Apply(blurb.rectTransform, "blurb_deco",
                new Vector2(0.55f, 0.02f), new Vector2(0.98f, 0.22f));
            blurb.preserveAspect = true;
            MakeTitleEditable(blurb.gameObject, "blurb_deco");

            // Right page menu
            var right = new GameObject("RightPage", typeof(RectTransform));
            right.transform.SetParent(magHost.transform, false);
            TitleMenuLayout.Apply(right.GetComponent<RectTransform>(), "right_page",
                new Vector2(0.52f, 0.10f), new Vector2(0.96f, 0.92f));
            MakeTitleEditable(right, "right_page");

            var contentsHeader = CreateTitleSprite(right.transform, "ContentsHeader", "title_contents_header",
                Color.white, false);
            TitleMenuLayout.Apply(contentsHeader.rectTransform, "contents_header",
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.96f));
            contentsHeader.preserveAspect = true;
            MakeTitleEditable(contentsHeader.gameObject, "contents_header");

            titleContentsLabel = CreateUiText(right.transform, "ContentsLabel", 22, TextAnchor.MiddleCenter,
                new Color(0.16f, 0.12f, 0.09f, 0.95f), Vector2.zero, new Vector2(280, 36));
            titleContentsLabel.text = UiLoc.T("ui.title.contents");
            StyleTitleMenuText(titleContentsLabel, 22, true);
            TitleMenuLayout.Apply(titleContentsLabel.GetComponent<RectTransform>(), "contents_label",
                new Vector2(0.12f, 0.88f), new Vector2(0.88f, 0.98f));
            MakeTitleEditable(titleContentsLabel.gameObject, "contents_label");

            titleActionRoot = new GameObject("TitleActions", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            titleActionRoot.SetParent(right.transform, false);
            // Narrower column so tape buttons aren't stretched full page-width.
            TitleMenuLayout.Apply(titleActionRoot.GetComponent<RectTransform>(), "title_actions",
                new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.82f));
            MakeTitleEditable(titleActionRoot.gameObject, "title_actions");
            var tah = titleActionRoot.GetComponent<VerticalLayoutGroup>();
            tah.spacing = TitleMenuLayout.ButtonSpacing;
            tah.childAlignment = TextAnchor.UpperCenter;
            tah.childForceExpandWidth = false;
            tah.childForceExpandHeight = false;
            tah.childControlWidth = true;
            tah.childControlHeight = true;
            tah.padding = new RectOffset(12, 12, 6, 6);

            // Chapter tagline removed from title ("第一章　编外保安大福").
            // Cleared-saves notice is shown on demand via SetTitleTaglineMessage(true).
            titleTagline = null;
            titleTaglineCleared = false;

            BuildTitleDeskProps(titleRoot.transform);
            ApplyTitleLanguageVisuals();
        }

        static void MakeTitleSpriteCrisp(Sprite spr)
        {
            if (spr == null || spr.texture == null) return;
            spr.texture.filterMode = FilterMode.Point;
            spr.texture.anisoLevel = 0;
            spr.texture.mipMapBias = 0f;
        }

        void SetTitleTaglineMessage(bool cleared)
        {
            titleTaglineCleared = cleared;
            if (!cleared)
            {
                if (titleTagline != null)
                    titleTagline.gameObject.SetActive(false);
                return;
            }

            // Only show "saves cleared" notice — no default chapter tagline.
            if (titleTagline == null && titleRoot != null)
            {
                var right = titleRoot.transform.Find("MagazineHost/RightPage");
                var parent = right != null ? right : titleRoot.transform;
                titleTagline = CreateUiText(parent, "TagCleared", 17, TextAnchor.MiddleCenter,
                    new Color(0.18f, 0.14f, 0.10f, 0.92f), Vector2.zero, new Vector2(360, 40));
                StyleTitleMenuText(titleTagline, 17, true);
                TitleMenuLayout.Apply(titleTagline.GetComponent<RectTransform>(), "tagline",
                    new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.18f));
                MakeTitleEditable(titleTagline.gameObject, "tagline");
            }
            if (titleTagline != null)
            {
                titleTagline.gameObject.SetActive(true);
                titleTagline.text = UiLoc.T("ui.title.saves_cleared");
                StyleTitleMenuText(titleTagline, 17, true);
            }
        }

        void StyleTitleMenuText(TextMeshProUGUI t, int size, bool bold)
        {
            if (t == null) return;
            t.font = titleFont != null ? titleFont : font;
            float scale = GameSettings.FontSizeScale;
            // Render larger then scale down via layout — sharper than tiny Dynamic fonts.
            t.fontSize = Mathf.RoundToInt(Mathf.Max(size, 28) * scale);
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.enableAutoSizing = false;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            // Drop Outline/Shadow — they soft-blur CJK/Latin UI Text on the title screen.
            foreach (var fx in t.GetComponents<Shadow>())
            {
                if (fx != null) fx.enabled = false;
            }
            ApplyLetterSpacing(t, GameSettings.LetterSpacing);
        }

        /// <summary>
        /// Left-page magazine blurb: wrap + BestFit inside the quote rect.
        /// Letter-spacing is off (mesh tracking breaks Wrap).
        /// </summary>
        void StyleTitleMenuBodyText(TextMeshProUGUI t, int preferredSize)
        {
            if (t == null) return;
            t.font = titleFont != null ? titleFont : font;
            float scale = GameSettings.FontSizeScale;
            // Do not force the button-style min-28 size — it overflows the quote box.
            int maxSize = Mathf.RoundToInt(Mathf.Clamp(preferredSize * scale, 14f, 30f));
            int minSize = Mathf.Max(11, Mathf.RoundToInt(maxSize * 0.55f));
            t.fontSize = maxSize;
            t.fontStyle = FontStyles.Normal;
            t.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Truncate;
            t.enableAutoSizing = true;
            t.fontSizeMin = minSize;
            t.fontSizeMax = maxSize;
            t.lineSpacing = 15f;
            foreach (var fx in t.GetComponents<Shadow>())
            {
                if (fx != null) fx.enabled = false;
            }
            ApplyLetterSpacing(t, 0f);
        }

        /// <summary>Single-line title brand fallback when logo sprites are missing.</summary>
        void StyleTitleMenuFittedText(TextMeshProUGUI t, int preferredSize, bool bold)
        {
            if (t == null) return;
            t.font = titleFont != null ? titleFont : font;
            float scale = GameSettings.FontSizeScale;
            int maxSize = Mathf.RoundToInt(Mathf.Clamp(preferredSize * scale, 22f, 56f));
            int minSize = Mathf.Max(14, Mathf.RoundToInt(maxSize * 0.45f));
            t.fontSize = maxSize;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = true;
            t.fontSizeMin = minSize;
            t.fontSizeMax = maxSize;
            foreach (var fx in t.GetComponents<Shadow>())
            {
                if (fx != null) fx.enabled = false;
            }
            // Tracking widens glyphs after BestFit — keep light or off so EN fits.
            ApplyLetterSpacing(t, GameSettings.LetterSpacing * 0.35f);
        }

        /// <summary>Play Mode: apply tape button W/H/spacing from TitleMenuLayout (editor sliders).</summary>
        public void ApplyTitleButtonMetrics()
        {
            if (titleActionRoot == null) return;
            var tah = titleActionRoot.GetComponent<VerticalLayoutGroup>();
            if (tah != null)
                tah.spacing = TitleMenuLayout.ButtonSpacing;

            float w = TitleMenuLayout.ButtonWidth;
            float h = TitleMenuLayout.ButtonHeight;
            for (int i = 0; i < titleActionRoot.childCount; i++)
            {
                var child = titleActionRoot.GetChild(i);
                var le = child.GetComponent<LayoutElement>();
                if (le == null) continue;
            bool primary = child.name.Contains("新游戏") || child.name.Contains("New Game");
                float bw = primary ? w + 12f : w;
                float bh = primary ? h + 4f : h;
                le.minWidth = bw;
                le.preferredWidth = bw;
                le.minHeight = bh;
                le.preferredHeight = bh;
                le.flexibleWidth = 0f;

                var label = child.Find("Label");
                if (label != null)
                {
                    var tx = label.GetComponent<TextMeshProUGUI>();
                    if (tx != null)
                        StyleTitleMenuText(tx, primary ? 24 : 21, true);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(titleActionRoot as RectTransform);
        }

        void MakeTitleEditable(GameObject go, string id)
        {
#if UNITY_EDITOR
            if (go == null || string.IsNullOrEmpty(id)) return;
            var target = go.GetComponent<RectTransform>();
            var title = TitleMenuLayout.DisplayNames.TryGetValue(id, out var n) ? n : id;

            // Unity: only one Graphic per GameObject. TextMeshProUGUI hosts need a child hit Image.
            Image hitImg = go.GetComponent<Image>();
            GameObject host = go;
            bool ownsHit = false;
            if (hitImg == null)
            {
                if (go.GetComponent<Graphic>() != null)
                {
                    var hitTf = go.transform.Find("TitleEditHit");
                    if (hitTf == null)
                    {
                        var hitGo = new GameObject("TitleEditHit", typeof(RectTransform), typeof(Image));
                        hitGo.transform.SetParent(go.transform, false);
                        StretchFull(hitGo.GetComponent<RectTransform>());
                        hitTf = hitGo.transform;
                    }
                    host = hitTf.gameObject;
                    hitImg = host.GetComponent<Image>();
                    if (hitImg == null)
                        hitImg = host.AddComponent<Image>();
                    hitImg.color = new Color(1f, 1f, 1f, 0.001f);
                    ownsHit = true;
                }
                else
                {
                    hitImg = go.AddComponent<Image>();
                    hitImg.color = new Color(1f, 1f, 1f, 0.001f);
                    ownsHit = true;
                }
            }

            var drag = host.GetComponent<DraggableTitleElement>();
            if (drag == null)
                drag = host.AddComponent<DraggableTitleElement>();
            drag.Configure(id, title, target, hitImg, ownsHit);
#endif
        }

        void BuildTitleDeskProps(Transform titleParent)
        {
            void Prop(string name, string layoutId, string key, Vector2 aMin, Vector2 aMax, bool clickNotes = false)
            {
                var img = CreateTitleSprite(titleParent, name, key, Color.white, false);
                TitleMenuLayout.Apply(img.rectTransform, layoutId, aMin, aMax);
                img.preserveAspect = true;
                img.raycastTarget = true;
                MakeTitleEditable(img.gameObject, layoutId);
                if (!clickNotes || img.sprite == null) return;

                var btn = img.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                var colors = btn.colors;
                colors.highlightedColor = new Color(1f, 0.96f, 0.9f, 1f);
                colors.pressedColor = new Color(0.9f, 0.88f, 0.82f, 1f);
                btn.colors = colors;
                btn.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    if (TitleMenuEditMode.Enabled) return;
#endif
                    SfxController.Instance?.PlayUi();
                    OpenNotebook();
                });
            }

            Prop("PropTranslator", "prop_translator", "prop_translator", new Vector2(0.01f, 0.02f), new Vector2(0.14f, 0.42f));
            Prop("PropNotes", "prop_notes", "prop_field_notes", new Vector2(0.86f, 0.02f), new Vector2(0.99f, 0.38f), true);
            Prop("PropPolaroidA", "prop_polaroid_a", "prop_polaroid_a", new Vector2(0.00f, 0.55f), new Vector2(0.12f, 0.88f));
            Prop("PropPolaroidB", "prop_polaroid_b", "prop_polaroid_b", new Vector2(0.88f, 0.52f), new Vector2(0.995f, 0.86f));
            Prop("PropScraps", "prop_scraps", "prop_scraps", new Vector2(0.78f, 0.00f), new Vector2(0.92f, 0.22f));
        }

        Image CreateTitleSprite(Transform parent, string name, string key, Color color, bool stretchFull)
        {
            var img = CreateImage(parent, name, color);
            if (stretchFull)
                StretchFull(img.rectTransform);
            var spr = VnArt.GetTitle(key);
            if (spr != null)
            {
                img.sprite = spr;
                img.type = Image.Type.Simple;
                img.preserveAspect = !stretchFull;
            }
            else
            {
                img.color = new Color(color.r, color.g, color.b, color.a * 0.35f);
            }
            return img;
        }

        Image CreateFillImage(Transform parent, string name, Color color)
        {
            var img = CreateImage(parent, name, color);
            StretchFull(img.rectTransform);
            return img;
        }

        Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        TextMeshProUGUI CreateUiText(Transform parent, string name, int size, TextAnchor align, Color color, Vector2 pos, Vector2 sizeDelta)
        {
            return VnText.Create(parent, name, font, size, align, color, pos, sizeDelta, wrap: true, raycastTarget: false);
        }

        TMP_InputField CreateVnInput(Transform parent)
        {
            var input = VnText.CreateInput(
                parent,
                "InterviewInput",
                font,
                22,
                VnTheme.TextPrimary,
                new Color(1f, 1f, 1f, 0.28f),
                "想问什么？");
            var rt = input.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 44);
            rt.sizeDelta = new Vector2(-80, 44);
            return input;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }

        void ClearButtons()
        {
            foreach (var go in spawnedButtons)
                if (go) Destroy(go);
            spawnedButtons.Clear();
            ClearInvestigateSpawned();
            if (choiceHostImage != null)
            {
                // Empty ChoiceHost is still a large invisible raycast slab (blocks title / stage).
                choiceHostImage.color = new Color(0, 0, 0, 0.001f);
                choiceHostImage.gameObject.SetActive(false);
            }
        }

        void ClearInvestigateSpawned()
        {
            foreach (var go in investigateSpawned)
                if (go) Destroy(go);
            investigateSpawned.Clear();
        }

        void AddAction(string label, UnityEngine.Events.UnityAction action, bool primary = false)
        {
            if (mode == Mode.Title && titleActionRoot != null)
            {
                SpawnTitleMenuButton(label, action, primary);
                return;
            }
            // Top HUD chips already cover backlog / menu — don't duplicate them in the footer row.
            if (IsBuiltInDialogueChromeAction(label))
                return;
            SpawnButton(buttonRoot, label, action, primary, 118);
        }

        /// <summary>
        /// True when the label is already exposed by permanent classic VN chrome (top HUD chips).
        /// Skip / notebook still spawn as classic dialogue footer actions.
        /// </summary>
        bool IsBuiltInDialogueChromeAction(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            return label == UiLoc.T("ui.backlog")
                || label == UiLoc.T("ui.menu")
                || label == "回看" || label == "Backlog"
                || label == "菜单" || label == "Menu";
        }

        void SpawnTitleMenuButton(string label, UnityEngine.Events.UnityAction action, bool primary)
        {
            int index = spawnedButtons.Count + 1;
            var go = new GameObject("TitleBtn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(titleActionRoot, false);

            var img = go.GetComponent<Image>();
            string idleKey = primary ? "btn_tape_primary_idle" : "btn_tape_idle";
            string hoverKey = primary ? "btn_tape_primary_hover" : "btn_tape_hover";
            string pressedKey = primary ? "btn_tape_primary_hover" : "btn_tape_pressed";
            var idle = VnArt.GetTitle(idleKey);
            var hover = VnArt.GetTitle(hoverKey);
            var pressed = VnArt.GetTitle(pressedKey);
            MakeTitleSpriteCrisp(idle);
            MakeTitleSpriteCrisp(hover);
            MakeTitleSpriteCrisp(pressed);
            if (idle != null)
            {
                img.sprite = idle;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = Color.white;
            }
            else
            {
                img.color = primary ? VnTheme.ButtonPrimary : VnTheme.Button;
            }

            var btn = go.GetComponent<Button>();
            if (idle != null && hover != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                btn.spriteState = new SpriteState
                {
                    highlightedSprite = hover,
                    pressedSprite = pressed != null ? pressed : hover,
                    selectedSprite = hover,
                    disabledSprite = idle
                };
            }
            else
            {
                var colors = btn.colors;
                colors.highlightedColor = VnTheme.ButtonHover;
                colors.pressedColor = VnTheme.AccentSoft;
                btn.colors = colors;
            }

            btn.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                if (TitleMenuEditMode.Enabled) return;
#endif
                SfxController.Instance?.PlayUi();
                action();
            });

            // Size from TitleMenuLayout (editable in 主菜单布局编辑器).
            float btnW = TitleMenuLayout.ButtonWidth + (primary ? 12f : 0f);
            float btnH = TitleMenuLayout.ButtonHeight + (primary ? 4f : 0f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = btnH;
            le.preferredHeight = btnH;
            le.minWidth = btnW;
            le.preferredWidth = btnW;
            le.flexibleWidth = 0f;

            string iconKey = TitleIconForLabel(label);
            var iconSpr = VnArt.GetTitle(iconKey);
            MakeTitleSpriteCrisp(iconSpr);
            if (iconSpr != null)
            {
                var icon = CreateImage(go.transform, "Icon", Color.white);
                icon.sprite = iconSpr;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var irt = icon.rectTransform;
                irt.anchorMin = new Vector2(0f, 0.5f);
                irt.anchorMax = new Vector2(0f, 0.5f);
                irt.pivot = new Vector2(0f, 0.5f);
                irt.anchoredPosition = new Vector2(18f, 0f);
                irt.sizeDelta = new Vector2(36f, 36f);
            }

            if (primary)
            {
                var clipSpr = VnArt.GetTitle("deco_paperclip");
                MakeTitleSpriteCrisp(clipSpr);
                if (clipSpr != null)
                {
                    var clip = CreateImage(go.transform, "Paperclip", Color.white);
                    clip.sprite = clipSpr;
                    clip.preserveAspect = true;
                    clip.raycastTarget = false;
                    var crt = clip.rectTransform;
                    crt.anchorMin = new Vector2(1f, 0.55f);
                    crt.anchorMax = new Vector2(1f, 0.55f);
                    crt.pivot = new Vector2(0.5f, 0.5f);
                    crt.sizeDelta = new Vector2(34f, 34f);
                    crt.anchoredPosition = new Vector2(-10f, 4f);
                }
            }

            float padL = iconSpr != null ? 56f : 22f;
            float padR = primary ? -28f : -16f;
            var labelColor = primary
                ? new Color(0.10f, 0.06f, 0.04f, 0.98f)
                : new Color(0.12f, 0.09f, 0.06f, 0.96f);
            var tgo = new GameObject("Label", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(padL, 2f), new Vector2(padR, -2f));
            var tx = tgo.AddComponent<TextMeshProUGUI>();
            tx.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
            tx.color = labelColor;
            tx.text = string.Format("{0:00}  {1}", index, label);
            tx.raycastTarget = false;
            StyleTitleMenuText(tx, primary ? 24 : 21, true);

            spawnedButtons.Add(go);
        }

        static string TitleIconForLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return "icon_doc";
            if (label.Contains("新游戏") || label.Contains("New Game")) return "icon_play";
            if (label.Contains("继续") || label.Contains("Continue") || label.Contains("自动档")) return "icon_cassette";
            if (label.Contains("读档") || label.Contains("Load")) return "icon_map";
            if (label.Contains("笔记") || label.Contains("Notebook") || label.Contains("清除") || label.Contains("Clear")) return "icon_doc";
            if (label.Contains("设置") || label.Contains("Settings")) return "icon_gear";
            if (label.Contains("退出") || label.Contains("Quit")) return "icon_exit";
            return "icon_doc";
        }

        void AddInvestigateHotspot(string id, string title, bool inspected, UnityEngine.Events.UnityAction action)
        {
            if (investigateHotspotLayer == null) return;
            var layoutKey = VnArt.ResolveBackground(
                !string.IsNullOrEmpty(stageBackgroundOverride) ? stageBackgroundOverride : "槐安社区_社区平面图");
            if (!InvestigateHotspotLayout.TryGet(id, layoutKey, out var rect))
            {
                // Fallback strip so missing layout still remains clickable
                rect = new Vector4(0.1f, 0.4f, 0.25f, 0.55f);
            }

            var go = new GameObject("Spot_" + id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(investigateHotspotLayer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(rect.x, rect.y);
            rt.anchorMax = new Vector2(rect.z, rect.w);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            var mark = VnTheme.InvestigateDone;
            var idle = inspected
                ? VnTheme.InvestigateDoneFill
                : new Color(1f, 1f, 1f, 0.02f);
            var hover = inspected
                ? VnTheme.InvestigateDoneHover
                : new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0.18f);
            img.color = idle;

            var outlineIdle = inspected
                ? new Color(mark.r, mark.g, mark.b, 0.10f)
                : new Color(0, 0, 0, 0);
            var outline = CreateImage(go.transform, "Outline", outlineIdle);
            StretchFull(outline.rectTransform);
            outline.raycastTarget = false;

            var edgeIdle = inspected
                ? new Color(mark.r, mark.g, mark.b, 0.45f)
                : new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0f);
            Image MakeEdge(string n, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
            {
                var e = CreateImage(go.transform, n, edgeIdle);
                Stretch(e.rectTransform, aMin, aMax, offMin, offMax);
                e.raycastTarget = false;
                return e;
            }
            var topE = MakeEdge("T", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2), Vector2.zero);
            var botE = MakeEdge("B", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 2));
            var leftE = MakeEdge("L", new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(2, 0));
            var rightE = MakeEdge("R", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-2, 0), Vector2.zero);

            if (inspected)
            {
                var badge = CreateImage(go.transform, "DoneBadge",
                    new Color(0.22f, 0.34f, 0.26f, 0.92f));
                badge.raycastTarget = false;
                var brt = badge.rectTransform;
                brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(1f, 1f);
                brt.anchoredPosition = new Vector2(-3f, -3f);
                brt.sizeDelta = new Vector2(22f, 22f);

                var checkGo = new GameObject("Check", typeof(RectTransform));
                checkGo.transform.SetParent(badge.transform, false);
                StretchFull(checkGo.GetComponent<RectTransform>());
                var check = checkGo.AddComponent<TextMeshProUGUI>();
                check.font = font;
                check.fontSize = 15;
                check.fontStyle = FontStyles.Bold;
                check.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
                check.color = new Color(0.78f, 0.92f, 0.80f, 1f);
                check.text = "✓";
                check.raycastTarget = false;
            }

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                if (InvestigateHotspotEditMode.Enabled) return;
#endif
                action();
            });

#if UNITY_EDITOR
            var drag = go.AddComponent<DraggableInvestigateHotspot>();
            drag.HotspotId = id;
            drag.Title = title;
            if (InvestigateHotspotEditMode.Enabled)
                img.color = new Color(1f, 0.55f, 0.15f, 0.35f);
#endif

            var trigger = go.AddComponent<EventTrigger>();
            void AddTrig(EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> cb)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(cb);
                trigger.triggers.Add(entry);
            }

            void SetHover(bool on)
            {
#if UNITY_EDITOR
                if (InvestigateHotspotEditMode.Enabled) return;
#endif
                img.color = on ? hover : idle;
                Color edgeCol;
                Color outlineCol;
                if (on)
                {
                    edgeCol = inspected
                        ? new Color(mark.r, mark.g, mark.b, 0.90f)
                        : new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0.85f);
                    outlineCol = inspected
                        ? new Color(mark.r, mark.g, mark.b, 0.18f)
                        : new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0.08f);
                }
                else
                {
                    edgeCol = edgeIdle;
                    outlineCol = outlineIdle;
                }
                topE.color = edgeCol;
                botE.color = edgeCol;
                leftE.color = edgeCol;
                rightE.color = edgeCol;
                outline.color = outlineCol;
                if (investigateHoverLabel != null)
                {
                    if (on)
                    {
                        investigateHoverLabel.text = title;
                        investigateHoverLabel.gameObject.SetActive(true);
                        var hl = investigateHoverLabel.GetComponent<RectTransform>();
                        var spotCenter = new Vector2((rect.x + rect.z) * 0.5f, rect.w);
                        hl.anchorMin = hl.anchorMax = spotCenter;
                        hl.anchoredPosition = new Vector2(0, 10);
                    }
                    else
                    {
                        investigateHoverLabel.gameObject.SetActive(false);
                    }
                }
            }

            AddTrig(EventTriggerType.PointerEnter, _ => SetHover(true));
            AddTrig(EventTriggerType.PointerExit, _ => SetHover(false));

            investigateSpawned.Add(go);
        }

        void AddInvestigateAction(string label, UnityEngine.Events.UnityAction action, bool primary = false)
        {
            var go = new GameObject("Act", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(investigateActions, false);
            go.GetComponent<Image>().color = primary
                ? new Color(0.18f, 0.15f, 0.12f, 0.92f)
                : new Color(0.10f, 0.11f, 0.13f, 0.88f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 36;
            le.preferredHeight = 36;
            le.minWidth = 108;
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = VnTheme.ButtonHover;
            btn.colors = colors;
            btn.onClick.AddListener(action);
            var tgo = new GameObject("L", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            StretchFull(tgo.GetComponent<RectTransform>());
            var tx = tgo.AddComponent<TextMeshProUGUI>();
            tx.font = font;
            tx.fontSize = 16;
            tx.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
            tx.color = primary ? VnTheme.Accent : VnTheme.TextPrimary;
            tx.text = label;
            tx.raycastTarget = false;
            investigateSpawned.Add(go);
        }

        void AddChoice(string label, UnityEngine.Events.UnityAction action)
        {
            if (choiceHostImage != null)
            {
                choiceHostImage.gameObject.SetActive(true);
                choiceHostImage.color = VnTheme.ChoicePanel;
            }
            int index = spawnedButtons.Count;
            SpawnButton(choiceRoot, label, () =>
            {
                SfxController.Instance?.PlayChoice();
                action();
            }, true, 0, true, index);
        }

        void SpawnButton(Transform parent, string label, UnityEngine.Events.UnityAction action, bool primary, float minW, bool wide = false, int staggerIndex = 0)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = wide ? new Color(0.11f, 0.11f, 0.13f, 0.96f)
                : (primary ? new Color(0.18f, 0.15f, 0.12f, 0.95f) : VnTheme.Button);
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = VnTheme.ButtonHover;
            colors.pressedColor = VnTheme.AccentSoft;
            btn.colors = colors;
            btn.onClick.AddListener(() =>
            {
                // wide choices already play PlayChoice upstream; avoid double-click
                if (!wide)
                    SfxController.Instance?.PlayUi();
                action();
            });

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = wide ? 52 : (mode == Mode.Title ? 48 : 36);
            le.preferredHeight = wide ? 52 : (mode == Mode.Title ? 48 : 36);
            if (wide)
            {
                le.minWidth = 520;
                le.flexibleWidth = 1;
            }
            else
            {
                le.minWidth = minW;
            }

            if (wide || primary)
            {
                var tick = CreateImage(go.transform, "Tick", VnTheme.Accent);
                var tr = tick.rectTransform;
                tr.anchorMin = new Vector2(0, 0.18f);
                tr.anchorMax = new Vector2(0, 0.82f);
                tr.pivot = new Vector2(0, 0.5f);
                tr.sizeDelta = new Vector2(4, 0);
                tr.anchoredPosition = Vector2.zero;
            }

            var tgo = new GameObject("Label", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-16, 0));
            var tx = tgo.AddComponent<TextMeshProUGUI>();
            tx.font = font;
            tx.fontSize = wide ? 22 : 17;
            tx.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
            tx.color = VnTheme.TextPrimary;
            tx.text = label;
            tx.raycastTarget = false;

            var cg = go.GetComponent<CanvasGroup>();
            if (wide && staggerIndex >= 0)
            {
                cg.alpha = 0f;
                StartCoroutine(StaggerChoiceIn(cg, staggerIndex * 0.045f));
            }

            spawnedButtons.Add(go);
        }

        IEnumerator StaggerChoiceIn(CanvasGroup cg, float delay)
        {
            if (cg == null) yield break;
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            float t = 0f;
            while (t < 0.18f && cg != null)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / 0.18f);
                yield return null;
            }
            if (cg != null) cg.alpha = 1f;
        }

        void SetChrome(bool showDialogue, bool showTitle, bool showLocation)
        {
            if (dialoguePanel != null)
                dialoguePanel.gameObject.SetActive(showDialogue);
            titleRoot.SetActive(showTitle);
            // Scene name is a brief toast (see RequestSceneTitleReveal), not persistent chrome.
            if (!showLocation || showTitle || mode == Mode.Investigate || mode == Mode.Interview)
                HideSceneTitleImmediate();
            // Title magazine has its own Settings entry — hide TopBar (回看/菜单) on main menu.
            if (topBarImage != null)
                topBarImage.gameObject.SetActive(!showTitle);
            chapterChip.gameObject.SetActive(!showTitle);
            objectiveText.gameObject.SetActive(!showTitle);
            if (interviewRoot != null && mode != Mode.Interview)
                interviewRoot.SetActive(false);
            if (investigateRoot != null && mode != Mode.Investigate)
                investigateRoot.SetActive(false);
            if (buttonRoot != null)
                buttonRoot.gameObject.SetActive(mode != Mode.Interview && mode != Mode.Investigate && mode != Mode.Title);
            if (choiceRoot != null)
            {
                // Title magazine lives under ChoiceHost in canvas order — never leave the slab on.
                bool showChoices = mode != Mode.Interview && mode != Mode.Investigate && mode != Mode.Title;
                if (!showChoices && choiceHostImage != null)
                    choiceHostImage.gameObject.SetActive(false);
                choiceRoot.parent?.gameObject.SetActive(showChoices);
                choiceRoot.gameObject.SetActive(showChoices);
            }
            // Hide catcher on title / interview / hotspot investigate; inspect & talk re-enable via SetAdvanceEnabled
            if (advanceCatcher != null && (!showDialogue || showTitle || mode == Mode.Interview
                || (mode == Mode.Investigate && investigateHotspotsVisible)))
                advanceCatcher.gameObject.SetActive(false);
            ApplyAtmosphere();
            if (!CanHideDialogue())
                dialogueHidden = false;
            ApplyDialogueHiddenChrome();
        }

        void SetInterviewChrome(bool on)
        {
            if (on)
                dialogueHidden = false;
            if (interviewRoot != null)
                interviewRoot.SetActive(on);
            if (!on && interviewCompanionPortrait != null)
            {
                interviewCompanionPortrait.enabled = false;
                interviewCompanionPortrait.gameObject.SetActive(false);
            }
            if (investigateRoot != null)
                investigateRoot.SetActive(false);
            if (dialoguePanel != null)
                dialoguePanel.gameObject.SetActive(!on && mode != Mode.Title && mode != Mode.Investigate);
            if (on)
                HideSceneTitleImmediate();
            if (hideDialogueBtn != null)
                hideDialogueBtn.gameObject.SetActive(!on && CanHideDialogue());
            if (buttonRoot != null)
                buttonRoot.gameObject.SetActive(!on);
            if (inputField != null)
                inputField.gameObject.SetActive(false);
            if (on)
            {
                // ChoiceHost itself (not only its viewport) is a large raycast slab — must be off in interview.
                if (choiceHostImage != null)
                    choiceHostImage.gameObject.SetActive(false);
                if (choiceRoot != null)
                {
                    choiceRoot.parent?.gameObject.SetActive(false);
                    choiceRoot.gameObject.SetActive(false);
                }
                if (advanceCatcher != null)
                    advanceCatcher.gameObject.SetActive(false);
                BringInterviewAboveGameplay();
            }
            ApplyAtmosphere();
        }

        /// <summary>
        /// Keep interview above dialogue/investigate/catcher, but under menu/backlog/saveload/confirm.
        /// </summary>
        void BringInterviewAboveGameplay()
        {
            if (interviewRoot == null) return;
            // Keep stage portraits visible above BG/props but under interview chrome.
            if (portraitImage != null)
                portraitImage.transform.SetAsLastSibling();
            interviewRoot.transform.SetAsLastSibling();
            // TopBar must sit above interview HitCatcher / meter pad; menus stay above TopBar.
            EnsureTopHudClickable();
        }

        void BringOverlayStackToFront()
        {
            // Writing corkboard sits under menu/notebook; those overlays must stay on top.
            if (writingMatsRoot != null && writingMatsRoot.activeSelf)
                writingMatsRoot.transform.SetAsLastSibling();
            if (writingDeskRoot != null && writingDeskRoot.activeSelf)
                writingDeskRoot.transform.SetAsLastSibling();
            if (menuRoot != null) menuRoot.transform.SetAsLastSibling();
            if (backlogRoot != null) backlogRoot.transform.SetAsLastSibling();
            if (notebookRoot != null) notebookRoot.transform.SetAsLastSibling();
            if (saveLoadRoot != null) saveLoadRoot.transform.SetAsLastSibling();
            if (confirmRoot != null) confirmRoot.transform.SetAsLastSibling();
            if (settingsRoot != null) settingsRoot.transform.SetAsLastSibling();
            if (hideDialogueBtn != null) hideDialogueBtn.transform.SetAsLastSibling();
            // Scene fade stays last for transitions; raycastTarget is off when idle.
            if (sceneFadeImage != null) sceneFadeImage.transform.SetAsLastSibling();
            // Keep an open writing desk above hide-dialogue chrome (fade still tops for blackouts).
            if (writingDeskRoot != null && writingDeskRoot.activeSelf)
            {
                writingDeskRoot.transform.SetAsLastSibling();
                if (sceneFadeImage != null)
                    sceneFadeImage.transform.SetAsLastSibling();
            }
        }

        void SetInvestigateChrome(bool on)
        {
            if (on)
                dialogueHidden = false;
            investigateHotspotsVisible = on;
            if (investigateRoot != null)
                investigateRoot.SetActive(on);
            if (interviewRoot != null)
                interviewRoot.SetActive(false);
            if (dialoguePanel != null)
                dialoguePanel.gameObject.SetActive(!on);
            HideSceneTitleImmediate();
            if (buttonRoot != null)
                buttonRoot.gameObject.SetActive(!on);
            if (on && choiceHostImage != null)
                choiceHostImage.gameObject.SetActive(false);
            if (choiceRoot != null)
            {
                choiceRoot.parent?.gameObject.SetActive(!on);
                choiceRoot.gameObject.SetActive(!on);
            }
            if (investigateHoverLabel != null && !on)
                investigateHoverLabel.gameObject.SetActive(false);
            ApplyAtmosphere();
        }

        void ApplyAtmosphere()
        {
            if (atmosphereWash == null) return;
            string key = locationText != null ? locationText.text : "";
            if (mode == Mode.Title) key = "杂志";
            else if (mode == Mode.Interview) key = "采访";
            else if (mode == Mode.Investigate) key = "社区";
            else if (mode == Mode.Writing || mode == Mode.Notebook) key = "杂志";
            atmosphereWash.color = VnTheme.AtmosphereForLocation(key);
            ApplyStageArt();
            ApplyBgm();
        }

        void ApplyBgm()
        {
            if (BgmController.Instance == null) return;
            string label = null;
            var scene = SceneDirector.Instance?.Current;
            if (scene != null && !string.IsNullOrEmpty(scene.backgroundLabel))
                label = scene.backgroundLabel;
            else if (locationText != null)
                label = locationText.text;
            BgmController.Instance.PlayForContext(mode.ToString(), label);
        }

        void ApplyStageArt()
        {
            if (stageArt == null) return;

            string label = null;
            if (!string.IsNullOrEmpty(stageBackgroundOverride))
                label = stageBackgroundOverride;
            else if (mode == Mode.Title)
                label = "Title";
            else if (mode == Mode.Interview)
            {
                var who = InterviewController.Instance != null
                    ? InterviewController.Instance.Subject
                    : InterviewSubject.Dafu;
                label = who == InterviewSubject.Lin ? "咖啡馆_午后" : "保安亭_傍晚";
            }
            else if (mode == Mode.Investigate)
                label = "槐安社区_午后";
            else if (mode == Mode.Writing)
                label = "编辑部工位_上午";
            else if (mode == Mode.Notebook)
                label = "编辑部_工位_傍晚";
            else if (mode == Mode.Epilogue)
                label = "槐安社区_午后";
            else
            {
                var scene = SceneDirector.Instance?.Current;
                if (scene != null && !string.IsNullOrEmpty(scene.backgroundLabel))
                    label = scene.backgroundLabel;
                else if (locationText != null)
                    label = locationText.text;
            }

            var key = VnArt.ResolveBackground(label);
            var sprite = VnArt.GetBg(key);
            if (sprite != null)
            {
                stageArt.sprite = sprite;
                stageArt.color = Color.white;
                stageArt.enabled = true;
            }
            else
            {
                stageArt.sprite = null;
                stageArt.enabled = false;
            }

            // Hide portraits on title / hotspot investigate view (inspect dialogue may show 小凌)
            if (mode == Mode.Title || (mode == Mode.Investigate && investigateHotspotsVisible))
                SetPortrait(null);
        }

        void SetStageBackground(string label)
        {
            if (string.IsNullOrEmpty(label)) return;
            stageBackgroundOverride = label;
            if (locationText != null && mode != Mode.Title)
                locationText.text = label.Replace("_", "　");
            ApplyStageArt();
        }

        void SetProp(string propKey)
        {
            if (propImage == null) return;

            if (string.IsNullOrEmpty(propKey))
            {
                propImage.sprite = null;
                propImage.enabled = false;
                propImage.gameObject.SetActive(false);
                return;
            }

            var sprite = VnArt.GetProp(propKey);
            if (sprite == null)
            {
                Debug.LogWarning("[GameUI] Prop sprite missing: " + propKey);
                propImage.sprite = null;
                propImage.enabled = false;
                propImage.gameObject.SetActive(false);
                return;
            }

            propImage.sprite = sprite;
            propImage.color = Color.white;
            propImage.enabled = true;
            propImage.gameObject.SetActive(true);
        }

        void SetPortrait(string portraitKey)
        {
            if (portraitImage == null) return;
            if (dialogueHidden || mode == Mode.Title || (mode == Mode.Investigate && investigateHotspotsVisible))
                portraitKey = null;

            if (string.IsNullOrEmpty(portraitKey))
            {
                if (portraitFadeCo != null) StopCoroutine(portraitFadeCo);
                if (portraitFade != null) portraitFade.alpha = 0f;
                portraitImage.sprite = null;
                portraitImage.enabled = false;
                portraitImage.gameObject.SetActive(false);
                return;
            }

            var sprite = VnArt.GetPortrait(portraitKey);
            if (sprite == null)
            {
                if (portraitFadeCo != null) StopCoroutine(portraitFadeCo);
                if (portraitFade != null) portraitFade.alpha = 0f;
                portraitImage.sprite = null;
                portraitImage.enabled = false;
                portraitImage.gameObject.SetActive(false);
                return;
            }

            bool same = portraitImage.enabled && portraitImage.sprite == sprite;
            portraitImage.sprite = sprite;
            portraitImage.color = Color.white;
            portraitImage.enabled = true;
            portraitImage.gameObject.SetActive(true);
            LayoutPortraitRect(sprite);
            if (!same)
            {
                if (portraitFadeCo != null) StopCoroutine(portraitFadeCo);
                portraitFadeCo = StartCoroutine(FadePortraitIn());
            }
            else if (portraitFade != null)
            {
                portraitFade.alpha = 1f;
            }
        }

        /// <summary>
        /// Keep a constant portrait slot height; width follows sprite aspect (clamped).
        /// Avoids landscape plates looking tiny inside a tall preserveAspect rect.
        /// Interview mode uses a dedicated left-side solo slot (see LayoutInterviewPortraitSlot).
        /// </summary>
        void LayoutPortraitRect(Sprite sprite)
        {
            if (portraitImage == null) return;
            if (mode == Mode.Interview)
            {
                LayoutInterviewPortraitSlot(sprite);
                return;
            }

            // ~25% larger than prior 0.70–0.94 × (DialogueTop-0.03)–0.76 slot;
            // still upper-right of dialogue, bottom rests on dialogue top edge.
            const float slotLeft = 0.65f;
            const float slotRight = 0.95f;
            const float slotTop = 0.89f;
            float slotBottom = VnTheme.DialogueTop - 0.03f;
            float slotW = slotRight - slotLeft;
            float slotH = slotTop - slotBottom;

            float heightNorm = slotH;
            float widthNorm = slotW;
            if (sprite != null)
            {
                float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
                // Fit height first so expression swaps keep the same visual stature.
                widthNorm = heightNorm * aspect;
                if (widthNorm > slotW)
                {
                    widthNorm = slotW;
                    heightNorm = widthNorm / aspect;
                }
            }

            float cx = (slotLeft + slotRight) * 0.5f;
            float left = cx - widthNorm * 0.5f;
            float right = cx + widthNorm * 0.5f;
            float bottom = slotBottom;
            float top = bottom + heightNorm;
            Stretch(portraitImage.rectTransform,
                new Vector2(left, bottom), new Vector2(right, top),
                Vector2.zero, Vector2.zero);
            portraitImage.preserveAspect = true;
            // Never SetNativeSize — pixel dimensions vary and would jitter layout.
        }

        IEnumerator FadePortraitIn()
        {
            if (portraitFade == null) yield break;
            portraitFade.alpha = 0f;
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                portraitFade.alpha = Mathf.SmoothStep(0f, 1f, t / 0.28f);
                yield return null;
            }
            portraitFade.alpha = 1f;
            portraitFadeCo = null;
        }

        // Interview ask/LLM/refresh -> GameUI.Interview.cs

        void PlayDialogueFade()
        {
            if (fadeCo != null) StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(FadeDialogue());
        }

        IEnumerator FadeDialogue()
        {
            dialogueFade.alpha = 0.35f;
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                dialogueFade.alpha = Mathf.Lerp(0.35f, 1f, t / 0.25f);
                yield return null;
            }
            dialogueFade.alpha = 1f;
        }

        #endregion

        void RefreshHeader()
        {
            var gs = GameState.Instance;
            objectiveText.text = string.IsNullOrEmpty(gs?.Data.currentObjective)
                ? ""
                : UiLoc.T("ui.objective_prefix") + ScriptLoc.MapObjective(gs.Data.currentObjective);
            var scene = SceneDirector.Instance?.Current;
            // Keep investigate / epilogue custom labels; elsewhere prefer runtime BG override.
            if (mode != Mode.Investigate && mode != Mode.Epilogue)
            {
                if (locationText != null)
                {
                    if (!string.IsNullOrEmpty(stageBackgroundOverride))
                        locationText.text = stageBackgroundOverride.Replace("_", "　");
                    else if (scene != null && !string.IsNullOrEmpty(scene.backgroundLabel))
                        locationText.text = scene.backgroundLabel.Replace("_", "　");
                }
                if (stageHint != null && scene != null && !string.IsNullOrEmpty(scene.title))
                    stageHint.text = ScriptLoc.SceneTitle(scene.id, scene.title);
            }
            ApplyAtmosphere();
            ApplyStageArt();
            if (CanRevealSceneTitle())
                RequestSceneTitleReveal();
        }

        bool CanRevealSceneTitle()
        {
            return mode == Mode.Dialogue || mode == Mode.Talk || mode == Mode.Writing
                || mode == Mode.Epilogue;
        }

        void RequestSceneTitleReveal(bool force = false)
        {
            if (!CanRevealSceneTitle())
                return;
            if (locationText == null && stageHint == null)
                return;

            var scene = SceneDirector.Instance?.Current;
            string loc = locationText != null ? locationText.text : "";
            string title = stageHint != null ? stageHint.text : "";
            if (string.IsNullOrEmpty(loc) && string.IsNullOrEmpty(title))
                return;

            string key = (scene != null ? scene.id : "") + "|" + loc + "|" + title;
            if (!force && key == lastSceneTitleKey)
                return;
            lastSceneTitleKey = key;

            if (sceneTitleCo != null)
                StopCoroutine(sceneTitleCo);
            sceneTitleCo = StartCoroutine(SceneTitleRevealCo());
        }

        void HideSceneTitleImmediate()
        {
            if (sceneTitleCo != null)
            {
                StopCoroutine(sceneTitleCo);
                sceneTitleCo = null;
            }
            if (locationFade != null)
                locationFade.alpha = 0f;
            if (stageHintFade != null)
                stageHintFade.alpha = 0f;
            if (locationText != null)
                locationText.gameObject.SetActive(false);
            if (stageHint != null)
                stageHint.gameObject.SetActive(false);
        }

        IEnumerator SceneTitleRevealCo()
        {
            if (locationText != null)
                locationText.gameObject.SetActive(true);
            if (stageHint != null)
                stageHint.gameObject.SetActive(true);
            if (locationFade != null)
                locationFade.alpha = 0f;
            if (stageHintFade != null)
                stageHintFade.alpha = 0f;

            float t = 0f;
            while (t < SceneTitleFadeIn)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / SceneTitleFadeIn);
                if (locationFade != null) locationFade.alpha = a;
                if (stageHintFade != null) stageHintFade.alpha = a;
                yield return null;
            }
            if (locationFade != null) locationFade.alpha = 1f;
            if (stageHintFade != null) stageHintFade.alpha = 1f;

            yield return new WaitForSecondsRealtime(SceneTitleHold);

            t = 0f;
            while (t < SceneTitleFadeOut)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / SceneTitleFadeOut);
                if (locationFade != null) locationFade.alpha = a;
                if (stageHintFade != null) stageHintFade.alpha = a;
                yield return null;
            }

            if (locationFade != null) locationFade.alpha = 0f;
            if (stageHintFade != null) stageHintFade.alpha = 0f;
            if (locationText != null) locationText.gameObject.SetActive(false);
            if (stageHint != null) stageHint.gameObject.SetActive(false);
            sceneTitleCo = null;
        }

        /// <summary>
        /// Drop a previous scene's 【背景】 override when the script scene id changes,
        /// so the enter toast uses the new scene's backgroundLabel / title.
        /// </summary>
        void SyncStageBackgroundToCurrentScene()
        {
            var scene = SceneDirector.Instance?.Current;
            string sceneId = scene != null ? scene.id : null;
            if (string.Equals(sceneId, lastBgSceneId, StringComparison.Ordinal))
                return;
            lastBgSceneId = sceneId;
            stageBackgroundOverride = null;
        }

        void SetSpeaker(string name, LineSpeaker kind, string portraitTag = null, string lineText = null)
        {
            if (string.IsNullOrEmpty(name) || kind == LineSpeaker.Narration)
            {
                namePlate.gameObject.SetActive(false);
                ApplyDialogueInkColors(LineSpeaker.Narration);
                lastHistorySpeaker = "";
                // Hide during narration so a leftover expression doesn't sit on unrelated prose.
                SetPortrait(null);
                return;
            }
            namePlate.gameObject.SetActive(true);
            if (kind == LineSpeaker.Inner)
            {
                // No「小凌（内心）」nameplate; show soft portrait for monologue.
                namePlate.gameObject.SetActive(false);
                ApplyDialogueInkColors(LineSpeaker.Inner);
                lastHistorySpeaker = "";
                ApplyPortrait(name, kind, portraitTag, lineText);
                return;
            }
            else if (kind == LineSpeaker.System)
            {
                nameText.text = ScriptLoc.MapSpeaker("系统");
                ApplyDialogueInkColors(LineSpeaker.System);
                lastHistorySpeaker = nameText.text;
                SetPortrait(null);
                return;
            }
            else
            {
                nameText.text = name;
                ApplyDialogueInkColors(LineSpeaker.Character);
                lastHistorySpeaker = name;
            }

            ApplyPortrait(name, kind, portraitTag, lineText);
        }

        /// <summary>
        /// Classic dialogue panel ink: mute narration, cool inner monologue, warm system, primary speech.
        /// </summary>
        void ApplyDialogueInkColors(LineSpeaker kind)
        {
            if (bodyText == null) return;
            switch (kind)
            {
                case LineSpeaker.Narration:
                    bodyText.color = VnTheme.TextMuted;
                    break;
                case LineSpeaker.Inner:
                    bodyText.color = VnTheme.TextInner;
                    break;
                case LineSpeaker.System:
                    bodyText.color = VnTheme.TextSystem;
                    break;
                default:
                    bodyText.color = VnTheme.TextPrimary;
                    break;
            }
        }

        void ApplyPortrait(string name, LineSpeaker kind, string portraitTag, string lineText = null)
        {
            var tag = portraitTag;
            if (!string.IsNullOrEmpty(tag) && (tag.Contains("无立绘") || tag == "none"))
            {
                SetPortrait(null);
                return;
            }

            var isXiaoling = (!string.IsNullOrEmpty(name) && (name.Contains("小凌") || name.Contains("Ling")))
                || kind == LineSpeaker.Inner;
            if (isXiaoling)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    var inferred = VnArt.SuggestXiaolingExpression(lineText, kind);
                    tag = !string.IsNullOrEmpty(inferred) ? inferred : stickyXiaolingPortrait;
                }
                stickyXiaolingPortrait = string.IsNullOrEmpty(tag) ? stickyXiaolingPortrait : tag;
            }

            var key = VnArt.ResolvePortrait(name, kind, tag);
            SetPortrait(key);

            if (portraitImage != null && portraitImage.enabled)
            {
                // Soften inner monologue; full opacity for spoken lines
                portraitImage.color = kind == LineSpeaker.Inner
                    ? new Color(0.92f, 0.94f, 0.96f, 0.9f)
                    : Color.white;
            }
        }

        void SetBody(string text, bool recordHistory = true, string historyKind = "dialogue")
        {
            typewriterFull = text ?? "";
            if (typewriterCo != null)
            {
                StopCoroutine(typewriterCo);
                typewriterCo = null;
            }

            bool useTypewriter = mode == Mode.Dialogue || mode == Mode.Talk || mode == Mode.Epilogue
                || (mode == Mode.Investigate && !investigateHotspotsVisible);
            if (useTypewriter && typewriterFull.Length > 0)
            {
                typewriterRunning = true;
                bodyText.text = "";
                typewriterCo = StartCoroutine(TypewriterRoutine(typewriterFull));
            }
            else
            {
                typewriterRunning = false;
                bodyText.text = typewriterFull;
            }

            Canvas.ForceUpdateCanvases();
            if (dialogueScroll != null)
                dialogueScroll.verticalNormalizedPosition = 1f;
            PlayDialogueFade();
            if (recordHistory && DialogueHistory.Instance != null)
                DialogueHistory.Instance.Add(lastHistorySpeaker, typewriterFull, historyKind);
            RefreshAdvanceHint();
        }

        IEnumerator TypewriterRoutine(string full)
        {
            float cps = GameSettings.TypewriterCps;
            float acc = 0f;
            int shown = 0;
            int typeTick = 0;
            while (shown < full.Length)
            {
                acc += Time.unscaledDeltaTime * cps;
                int next = Mathf.Min(full.Length, shown + Mathf.Max(1, (int)acc));
                if (next > shown)
                {
                    acc -= next - shown;
                    shown = next;
                    bodyText.text = full.Substring(0, shown);
                    typeTick++;
                    if (typeTick % 3 == 0)
                        SfxController.Instance?.PlayType();
                    Canvas.ForceUpdateCanvases();
                    if (dialogueScroll != null)
                        dialogueScroll.verticalNormalizedPosition = 0f;
                }
                yield return null;
            }
            bodyText.text = full;
            typewriterRunning = false;
            typewriterCo = null;
            RefreshAdvanceHint();
            ScheduleAutoPlayIfNeeded();
        }

        void CompleteTypewriter()
        {
            if (!typewriterRunning) return;
            if (typewriterCo != null)
            {
                StopCoroutine(typewriterCo);
                typewriterCo = null;
            }
            bodyText.text = typewriterFull;
            typewriterRunning = false;
            Canvas.ForceUpdateCanvases();
            if (dialogueScroll != null)
                dialogueScroll.verticalNormalizedPosition = 0f;
            RefreshAdvanceHint();
            ScheduleAutoPlayIfNeeded();
        }

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (HandleDebugJumpHotkey())
                return;
#endif
            if (mode == Mode.Title)
            {
                if (Input.GetKeyDown(KeyCode.Escape) && settingsRoot != null && settingsRoot.activeSelf)
                    CloseSettings();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsRoot != null && settingsRoot.activeSelf)
                {
                    CloseSettings();
                    return;
                }
                if (dialogueHidden)
                {
                    SetDialogueHidden(false);
                    return;
                }
                if (confirmRoot != null && confirmRoot.activeSelf)
                {
                    confirmRoot.SetActive(false);
                    pendingOverwriteSlot = -999;
                    return;
                }
                if (saveLoadRoot != null && saveLoadRoot.activeSelf) { CloseSaveLoad(); return; }
                if (mode == Mode.Backlog) { CloseBacklog(); return; }
                if (mode == Mode.Menu) { CloseMenu(); return; }
                if (mode == Mode.Notebook) { CloseNotebook(); return; }
                OpenMenu();
                return;
            }

            if (mode == Mode.Menu || mode == Mode.Backlog || mode == Mode.Notebook)
                return;

            if (mode == Mode.Interview)
            {
                if (interviewInput != null && interviewInput.gameObject.activeInHierarchy &&
                    (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) &&
                    !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    SubmitInterviewQuestion();
                }
                return;
            }

            bool inputFocused = inputField != null && inputField.isFocused;
            if (!inputFocused && canClickAdvance && !waitingForChoice &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                TryAdvanceByClick();
            }

            // Hold Ctrl to continuously skip (classic VN) — scripted, inspect, and talk beats
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool ctrlSkipOk = !inputFocused && !waitingForChoice && ctrl && IsSkippableDialogueContext()
                && (mode != Mode.Dialogue || canClickAdvance);
            if (ctrlSkipOk)
            {
                skipHoldTimer -= Time.unscaledDeltaTime;
                if (skipHoldTimer <= 0f || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
                {
                    skipHoldTimer = 0.1f;
                    if (typewriterRunning)
                        CompleteTypewriter();
                    else
                        TrySkipDialogue();
                }
            }
            else
            {
                skipHoldTimer = 0f;
            }
        }

        void TryAdvanceByClick()
        {
            if (sceneTransitioning)
                return;
            if (dialogueHidden)
            {
                SetDialogueHidden(false);
                return;
            }
            if (waitingForChoice)
                return;
            if (mode == Mode.Menu || mode == Mode.Backlog || mode == Mode.Notebook || mode == Mode.Title)
                return;
            if (mode != Mode.Dialogue && !(mode == Mode.Investigate && !investigateHotspotsVisible)
                && mode != Mode.Talk && mode != Mode.Epilogue)
                return;
            if (inputField != null && inputField.gameObject.activeSelf && inputField.isFocused)
                return;

            // First click finishes typewriter (standard VN)
            if (typewriterRunning)
            {
                CompleteTypewriter();
                return;
            }

            // Investigation inspect beats: click advances / finishes
            if (mode == Mode.Investigate && !investigateHotspotsVisible && inspectQueue.Count > 0)
            {
                AdvanceInspectOrFinish();
                return;
            }

            // Guard talk multi-beat conversation
            if (mode == Mode.Talk && talkQueue.Count > 0)
            {
                AdvanceTalkBeatOrFinish();
                return;
            }

            // Epilogue (SC-11) multi-beat narration
            if (mode == Mode.Epilogue && epilogueQueue.Count > 0)
            {
                AdvanceEpilogueOrFinish();
                return;
            }

            // Guard talk reply: click returns to topic menu
            if (mode == Mode.Talk && talkAwaitingClickReturn)
            {
                talkAwaitingClickReturn = false;
                if (talkIsPostInterview)
                    ShowPostInterviewTalk();
                else
                    ShowTalkMenu();
                return;
            }

            if (!canClickAdvance)
                return;
            if (mode == Mode.Dialogue)
                SceneDirector.Instance?.Advance();
        }

        /// <summary>
        /// True when the player can skip through the current dialogue body
        /// (scripted VN, inspect beats, or talk beats / single replies).
        /// </summary>
        bool IsSkippableDialogueContext()
        {
            if (waitingForChoice) return false;
            if (mode == Mode.Dialogue) return true;
            if (mode == Mode.Investigate && !investigateHotspotsVisible && inspectQueue.Count > 0)
                return true;
            if (mode == Mode.Talk && (talkQueue.Count > 0 || talkAwaitingClickReturn))
                return true;
            if (mode == Mode.Epilogue && epilogueQueue.Count > 0)
                return true;
            return false;
        }

        void AddStandardDialogueActions(bool includeSkip)
        {
            if (includeSkip)
                AddAction(UiLoc.T("ui.skip"), TrySkipDialogue);
            AddAction(UiLoc.T("ui.backlog"), OpenBacklog);
            AddAction(UiLoc.T("ui.notebook"), OpenNotebook);
            AddAction(UiLoc.T("ui.menu"), OpenMenu);
        }

        void RebuildSkippableDialogueActions()
        {
            if (!IsSkippableDialogueContext()) return;
            ClearButtons();
            AddStandardDialogueActions(includeSkip: true);
            if (mode == Mode.Dialogue)
                SetAdvanceEnabled(!waitingForChoice, waitingForChoice);
            else
                SetAdvanceEnabled(true);
            RefreshAdvanceHint();
        }

        void TrySkipDialogue()
        {
            if (waitingForChoice)
                return;
            if (mode == Mode.Menu || mode == Mode.Backlog || mode == Mode.Notebook || mode == Mode.Title)
                return;
            if (inputField != null && inputField.gameObject.activeSelf && inputField.isFocused)
                return;
            if (!IsSkippableDialogueContext())
                return;
            if (typewriterRunning)
                CompleteTypewriter();

            // Inspect beats: jump to end of current queue (same break as clicking through).
            if (mode == Mode.Investigate && !investigateHotspotsVisible && inspectQueue.Count > 0)
            {
                inspectIndex = inspectQueue.Count - 1;
                AdvanceInspectOrFinish();
                return;
            }

            // Talk multi-beat: jump to end of current topic / outro queue.
            if (mode == Mode.Talk && talkQueue.Count > 0)
            {
                talkIndex = talkQueue.Count - 1;
                AdvanceTalkBeatOrFinish();
                return;
            }

            // Talk single reply: return to topic menu.
            if (mode == Mode.Talk && talkAwaitingClickReturn)
            {
                talkAwaitingClickReturn = false;
                if (talkIsPostInterview)
                    ShowPostInterviewTalk();
                else
                    ShowTalkMenu();
                return;
            }

            // Epilogue: jump to last beat, then chapter-end button.
            if (mode == Mode.Epilogue && epilogueQueue.Count > 0)
            {
                epilogueIndex = epilogueQueue.Count - 1;
                ShowEpilogueBeat();
                ShowEpilogueChapterEnd();
                return;
            }

            if (mode != Mode.Dialogue || !canClickAdvance)
                return;
            SceneDirector.Instance?.SkipToBreak(RecordSkippedLine);
        }

        void RecordSkippedLine(ScriptLine line)
        {
            if (line == null) return;
            // Keep prop / social show/hide in sync when fast-forwarding past sticky cues.
            if (!string.IsNullOrEmpty(line.prop))
                SetProp(line.prop);
            else if (line.hideProp)
                SetProp(null);
            if (!string.IsNullOrEmpty(line.social))
                ApplySocialCue(line.social, instant: true);
            if (DialogueHistory.Instance == null)
                return;
            string speaker;
            string kind;
            if (line.speaker == LineSpeaker.Narration)
            {
                speaker = "";
                kind = "dialogue";
            }
            else if (line.speaker == LineSpeaker.Inner)
            {
                speaker = "";
                kind = "narration";
            }
            else if (line.speaker == LineSpeaker.System)
            {
                speaker = ScriptLoc.MapSpeaker("系统");
                kind = "system";
            }
            else
            {
                speaker = ScriptLoc.MapSpeaker(line.speakerName ?? "");
                kind = "dialogue";
            }
            DialogueHistory.Instance.Add(speaker, line.text, kind);
        }

        void SetAdvanceEnabled(bool enabled, bool hasChoices = false)
        {
            canClickAdvance = enabled && !hasChoices;
            waitingForChoice = hasChoices;
            bool inspectClick = mode == Mode.Investigate && !investigateHotspotsVisible && inspectQueue.Count > 0;
            bool talkBeats = mode == Mode.Talk && talkQueue.Count > 0;
            bool talkClick = mode == Mode.Talk && talkAwaitingClickReturn;
            bool epilogueBeats = mode == Mode.Epilogue && epilogueQueue.Count > 0;
            bool allowClick = (canClickAdvance || typewriterRunning || inspectClick || talkBeats || talkClick || epilogueBeats) && !hasChoices;
            if (dialogueClick != null)
                dialogueClick.interactable = allowClick;
            if (advanceCatcher != null)
            {
                bool showCatcher = allowClick && !writingDeskActive && !writingMatsActive &&
                    (mode == Mode.Dialogue || inspectClick || talkBeats || talkClick || epilogueBeats);
                advanceCatcher.gameObject.SetActive(showCatcher);
                var btn = advanceCatcher.GetComponent<Button>();
                if (btn != null) btn.interactable = showCatcher;
                if (showCatcher)
                    EnsureTopHudClickable();
            }
            RefreshAdvanceHint();
        }

        bool HasWideChoices()
        {
            foreach (var go in spawnedButtons)
            {
                if (go != null && go.transform.parent == choiceRoot)
                    return true;
            }
            return false;
        }

        void RefreshAdvanceHint()
        {
            if (clickHintText == null) return;
            bool inspectClick = mode == Mode.Investigate && !investigateHotspotsVisible && inspectQueue.Count > 0;
            bool talkBeats = mode == Mode.Talk && talkQueue.Count > 0;
            bool talkClick = mode == Mode.Talk && talkAwaitingClickReturn;
            bool epilogueBeats = mode == Mode.Epilogue && epilogueQueue.Count > 0;
            bool show = !waitingForChoice && (canClickAdvance || typewriterRunning || inspectClick || talkBeats || talkClick || epilogueBeats)
                && (mode == Mode.Dialogue || inspectClick || talkBeats || talkClick || epilogueBeats);
            clickHintText.gameObject.SetActive(show);
            if (!show) return;
            if (typewriterRunning)
                clickHintText.text = UiLoc.T("ui.click_show_full");
            else if (inspectClick)
                clickHintText.text = playingGuardAppear
                    ? (inspectIndex >= inspectQueue.Count - 1 ? UiLoc.T("ui.click_return_map") : UiLoc.T("ui.click_continue"))
                    : (inspectIndex >= inspectQueue.Count - 1 ? UiLoc.T("ui.click_return_investigate") : UiLoc.T("ui.click_continue"));
            else if (talkBeats)
                clickHintText.text = talkIndex >= talkQueue.Count - 1 ? UiLoc.T("ui.click_return_topics") : UiLoc.T("ui.click_continue");
            else if (talkClick)
                clickHintText.text = UiLoc.T("ui.click_return_topics");
            else if (epilogueBeats)
                clickHintText.text = UiLoc.T("ui.click_continue");
            else
                clickHintText.text = UiLoc.T("ui.click_ctrl_skip");
            // Static idle indicator only — do not pulse/restart on every advance click.
            var c = clickHintText.color;
            c.a = 0.55f;
            clickHintText.color = c;
        }

        public void ShowTitle()
        {
            mode = Mode.Title;
            stageBackgroundOverride = null;
            lastBgSceneId = null;
            lastSceneTitleKey = null;
            HideSceneTitleImmediate();
            canClickAdvance = false;
            waitingForChoice = false;
            dialogueHidden = false;
            writingMatsActive = false;
            if (inputField) inputField.gameObject.SetActive(false);
            // Full gameplay teardown — chapter-end / epilogue can leave raycast slabs above TitleRoot.
            if (menuRoot) menuRoot.SetActive(false);
            if (backlogRoot) backlogRoot.SetActive(false);
            if (saveLoadRoot) saveLoadRoot.SetActive(false);
            if (notebookRoot) notebookRoot.SetActive(false);
            if (confirmRoot) confirmRoot.SetActive(false);
            if (settingsRoot) settingsRoot.SetActive(false);
            HideWritingMaterialsBoard();
            HideWritingDesk();
            SetAdvanceEnabled(false);
            if (advanceCatcher != null) advanceCatcher.gameObject.SetActive(false);
            if (choiceHostImage != null) choiceHostImage.gameObject.SetActive(false);
            if (sceneFadeCg != null)
            {
                sceneFadeCg.blocksRaycasts = false;
                sceneFadeCg.interactable = false;
            }
            if (sceneFadeImage != null)
                sceneFadeImage.raycastTarget = false;
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(false, true, false);
            ClearButtons();
            statusText.text = "";
            // Raise magazine above leftover gameplay chrome; keep fade/settings stack on top when needed.
            if (titleRoot != null)
                titleRoot.transform.SetAsLastSibling();
            BringOverlayStackToFront();
            // Drop script sticky so title always uses bgm_title, not last scene cue.
            BgmController.Instance?.ClearScriptSticky();
            ApplyAtmosphere();
            ApplyStageArt();
            SetPortrait(null);
            SetProp(null);
            SocialHide(instant: true);
            if (titleTaglineCleared)
                SetTitleTaglineMessage(false);

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

        public void ShowDialogueMode()
        {
            mode = Mode.Dialogue;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "dialogue";
            inputField.gameObject.SetActive(false);
            HideWritingMaterialsBoard();
            writingMatsActive = false;
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            SyncStageBackgroundToCurrentScene();
            var scene = SceneDirector.Instance?.Current;
            if (scene != null && !string.IsNullOrEmpty(scene.backgroundLabel))
                SetStageBackground(scene.backgroundLabel);
            else
                ApplyStageArt();
            RefreshHeader();
            ClearButtons();
            AddStandardDialogueActions(includeSkip: true);
            SetAdvanceEnabled(true);
            statusText.text = UiLoc.T("ui.status_advance");
        }

        void OpenLinInterviewFromScript()
        {
            GameState.Instance.SetFlag(FlagIds.LinCafeIntroDone);
            GameState.Instance.SetFlag(FlagIds.LinUnlocked);
            GameState.Instance.SetScene(SceneIds.SC09);
            GameState.Instance.Data.uiMode = "interview_lin";
            SaveSystem.Autosave();
            ShowInterview(InterviewSubject.Lin);
        }

        void OnScriptLine(ScriptLine line)
        {
            mode = Mode.Dialogue;
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            SyncStageBackgroundToCurrentScene();
            inputField.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(line.background))
                SetStageBackground(line.background);
            else
                ApplyStageArt();
            // After BG is applied so the toast key matches the visible location.
            RefreshHeader();

            if (!string.IsNullOrEmpty(line.bgm))
                BgmController.Instance?.PlayScriptLabel(line.bgm);
            if (!string.IsNullOrEmpty(line.sfx))
                SfxController.Instance?.PlayScriptLabel(line.sfx);

            // Center prop is sticky: only show/hide on explicit cues (do not clear when line.prop is empty).
            if (!string.IsNullOrEmpty(line.prop))
                SetProp(line.prop);
            else if (line.hideProp)
                SetProp(null);

            // Social phone overlay (SC-03); sticky until social=hide.
            bool socialHide = IsSocialHideCue(line.social);
            bool socialShow = !string.IsNullOrEmpty(line.social) && !socialHide;
            if (!string.IsNullOrEmpty(line.social))
                ApplySocialCue(line.social);

            // Cue-only beat (bg / bgm / sfx / hideProp / social hide / bare jump): apply and auto-advance.
            // Prop-show / social-show beats WAIT for click (visual cue the player must acknowledge).
            bool cueOnly = string.IsNullOrEmpty(line.text)
                && string.IsNullOrEmpty(line.prop)
                && !socialShow
                && (line.choices == null || line.choices.Count == 0)
                && !line.openInvestigation && !line.openTalkMenu && !line.openWriting && !line.openInterview
                && (!string.IsNullOrEmpty(line.background)
                    || !string.IsNullOrEmpty(line.bgm)
                    || !string.IsNullOrEmpty(line.sfx)
                    || line.hideProp
                    || socialHide
                    || !string.IsNullOrEmpty(line.nextSceneId));
            if (cueOnly)
            {
                SceneDirector.Instance.Advance();
                return;
            }

            var speaker = line.speakerName;
            var lineKind = line.speaker;
            if (lineKind == LineSpeaker.Narration || speaker == "旁白")
            {
                speaker = "";
                lineKind = LineSpeaker.Narration;
            }
            if (lineKind == LineSpeaker.System) speaker = ScriptLoc.MapSpeaker("系统");
            SetSpeaker(speaker, lineKind, line.portrait, line.text);
            var kind = lineKind == LineSpeaker.System ? "system"
                : (lineKind == LineSpeaker.Narration || lineKind == LineSpeaker.Inner) ? "narration"
                : "dialogue";
            // Empty prop-only beats still wait for click; do not pollute backlog.
            SetBody(line.text, !string.IsNullOrEmpty(line.text), kind);

            ClearButtons();
            if (line.choices != null && line.choices.Count > 0)
            {
                statusText.text = UiLoc.T("ui.make_choice");
                SetAdvanceEnabled(false, true);
                for (int i = 0; i < line.choices.Count; i++)
                {
                    int idx = i;
                    AddChoice(line.choices[i].label, () => SceneDirector.Instance.Choose(idx));
                }
                AddStandardDialogueActions(includeSkip: false);
            }
            else
            {
                statusText.text = UiLoc.T("ui.status_advance");
                SetAdvanceEnabled(true);
                AddStandardDialogueActions(includeSkip: true);
            }
            ScheduleAutoPlayIfNeeded();
        }

        void OpenMenu()
        {
            if (mode == Mode.Title) return;
            if (saveLoadRoot != null && saveLoadRoot.activeSelf) return;
            if (mode != Mode.Menu && mode != Mode.Backlog && mode != Mode.Notebook)
            {
                returnFromOverlay = mode;
                savedWaitingForChoice = waitingForChoice;
            }
            mode = Mode.Menu;
            SetAdvanceEnabled(false);
            if (advanceCatcher != null) advanceCatcher.gameObject.SetActive(false);
            SfxController.Instance?.PlayUi();
            menuRoot.SetActive(true);
            if (backlogRoot) backlogRoot.SetActive(false);
            if (notebookRoot) notebookRoot.SetActive(false);
            SyncUiModeToSave();
            SaveSystem.Autosave(); // soft autosave when opening menu
        }

        void CloseMenu()
        {
            CloseMenuSilent();
            ResumeOverlayReturn();
        }

        void CloseMenuSilent()
        {
            if (menuRoot) menuRoot.SetActive(false);
        }

        void OpenBacklog()
        {
            if (mode == Mode.Title) return;
            backlogOpenedFromNotebook = mode == Mode.Notebook;
            if (mode != Mode.Menu && mode != Mode.Backlog && mode != Mode.Notebook)
            {
                returnFromOverlay = mode;
                savedWaitingForChoice = waitingForChoice;
            }
            mode = Mode.Backlog;
            SetAdvanceEnabled(false);
            if (menuRoot) menuRoot.SetActive(false);
            if (notebookRoot) notebookRoot.SetActive(false);
            backlogRoot.SetActive(true);
            var hist = DialogueHistory.Instance != null ? DialogueHistory.Instance.BuildPlainText() : "";
            backlogText.text = string.IsNullOrEmpty(hist) ? "（还没有可回看的对话）" : hist;
            Canvas.ForceUpdateCanvases();
            backlogScroll.verticalNormalizedPosition = 0f;
        }

        void CloseBacklog()
        {
            if (backlogRoot) backlogRoot.SetActive(false);
            if (backlogOpenedFromNotebook)
            {
                backlogOpenedFromNotebook = false;
                OpenNotebook();
                return;
            }
            ResumeOverlayReturn();
        }

        void ResumeOverlayReturn()
        {
            waitingForChoice = savedWaitingForChoice;
            var dest = returnFromOverlay;
            if (dest == Mode.Title)
            {
                ShowTitle();
                return;
            }
            // Never resume into overlay modes (would appear as a no-op / loop).
            if (dest == Mode.Notebook || dest == Mode.Menu || dest == Mode.Backlog)
            {
                var ui = GameState.Instance != null ? GameState.Instance.Data.uiMode : "";
                if (ui == "investigate") dest = Mode.Investigate;
                else if (!string.IsNullOrEmpty(ui) && ui.StartsWith("interview")) dest = Mode.Interview;
                else if (ui == "writing") dest = Mode.Writing;
                else if (ui == "epilogue") dest = Mode.Epilogue;
                else dest = Mode.Dialogue;
            }

            switch (dest)
            {
                case Mode.Dialogue:
                    mode = Mode.Dialogue;
                    SetInvestigateChrome(false);
                    SetInterviewChrome(false);
                    SetChrome(true, false, true);
                    ClearButtons();
                    AddStandardDialogueActions(includeSkip: !waitingForChoice);
                    SetAdvanceEnabled(!waitingForChoice, waitingForChoice);
                    statusText.text = waitingForChoice
                        ? UiLoc.T("ui.make_choice")
                        : UiLoc.T("ui.status_advance");
                    break;
                case Mode.Investigate:
                    if (inspectQueue.Count > 0 && !investigateHotspotsVisible)
                    {
                        mode = Mode.Investigate;
                        SetInvestigateChrome(false);
                        SetInterviewChrome(false);
                        SetChrome(true, false, true);
                        RebuildSkippableDialogueActions();
                    }
                    else
                        ShowInvestigationMode();
                    break;
                case Mode.Talk:
                    if (talkQueue.Count > 0 || talkAwaitingClickReturn)
                    {
                        mode = Mode.Talk;
                        SetInvestigateChrome(false);
                        SetInterviewChrome(false);
                        SetChrome(true, false, true);
                        RebuildSkippableDialogueActions();
                    }
                    else if (talkIsPostInterview)
                        ShowPostInterviewTalk();
                    else
                        ShowTalkMenu();
                    break;
                case Mode.Interview: RefreshInterviewView(); break;
                case Mode.Writing: ResumeWritingMode(); break;
                case Mode.Epilogue: ShowEpilogue(); break;
                default:
                    mode = Mode.Dialogue;
                    SetChrome(true, false, true);
                    SetAdvanceEnabled(true);
                    break;
            }
        }

        void OnSceneEnd()
        {
            if (GameState.Instance.Data.currentSceneId == SceneIds.SC04)
                ShowInvestigationMode();
        }

        public void ShowInvestigationMode()
        {
            mode = Mode.Investigate;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "investigate";
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            HideWritingMaterialsBoard();
            writingMatsActive = false;
            SetProp(null);
            SocialHide(instant: true);
            SetChrome(false, false, false);
            SetInvestigateChrome(true);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            locationText.text = "槐安社区";
            stageHint.text = "午后 · 调查";
            RefreshHeader();

            if (investigateTitle != null)
                investigateTitle.text = "槐安社区　·　调查";
            if (investigateIntelHint != null)
            {
                investigateIntelHint.text = string.IsNullOrEmpty(lastInspectText)
                    ? "点击场景物件调查　·　已获情报　" + GameState.Instance.Data.intel.Count
                    : "已获情报　" + GameState.Instance.Data.intel.Count + "　·　可继续调查";
            }
            if (investigateHoverLabel != null)
                investigateHoverLabel.gameObject.SetActive(false);

            SetStageBackground("槐安社区_社区平面图");

            ClearButtons();
            var service = InvestigationService.Instance;
            foreach (var h in service.Hotspots)
            {
                var id = h.id;
                var title = h.title;
                var inspected = h.inspected;
                AddInvestigateHotspot(id, title, inspected, () => ShowHotspotInspect(id));
            }

            // 保安亭 / 交谈：出场演出解锁后出现在平面图；交互后才进入 SC-05
            if (GameState.Instance.HasFlag(FlagIds.GuardUnlocked) || GameState.Instance.Data.currentSceneId == SceneIds.SC05)
            {
                AddInvestigateHotspot("guard_booth", "保安亭",
                    GameState.Instance.HasFlag(FlagIds.GuardIntroDone),
                    EnterGuardBoothFromMap);
                AddInvestigateAction("与保安交谈", EnterGuardBoothFromMap, true);
            }
            if (service.CanWaitForDafu())
                AddInvestigateAction("等待大福", () =>
                {
                    SetInvestigateChrome(false);
                    if (GameState.Instance.HasFlag(FlagIds.WaitingForDafu))
                        ChapterFlowController.Instance.GoToScene(SceneIds.SC06);
                    else
                        StartWaitForDafuOutro();
                }, true);
            // Incomplete Dafu interview left the map — skip SC-06 intro and resume free interview.
            if (GameState.Instance.HasFlag(FlagIds.WaitingForDafu)
                && !GameState.Instance.HasFlag(FlagIds.DafuInterviewDone))
            {
                AddInvestigateAction("继续采访大福", () =>
                {
                    SetInvestigateChrome(false);
                    ChapterFlowController.Instance.GoToScene(SceneIds.SC07);
                }, true);
            }
            if (GameState.Instance.HasFlag(FlagIds.LinCafeIntroDone)
                && !GameState.Instance.HasFlag(FlagIds.LinInterviewDone))
            {
                AddInvestigateAction("继续采访林女士", () =>
                {
                    SetInvestigateChrome(false);
                    ChapterFlowController.Instance.GoToScene(SceneIds.SC09);
                }, true);
            }
            if (GameState.Instance.HasFlag(FlagIds.DafuInterviewDone))
                AddInvestigateAction("打听救助者", () =>
                {
                    SetInvestigateChrome(false);
                    ShowPostInterviewTalk();
                }, true);
            AddInvestigateAction("回看", OpenBacklog);
            AddInvestigateAction("笔记", OpenNotebook);
            AddInvestigateAction("菜单", OpenMenu);
        }

        readonly List<InspectBeat> inspectQueue = new List<InspectBeat>();
        int inspectIndex;
        readonly List<InspectBeat> epilogueQueue = new List<InspectBeat>();
        int epilogueIndex;
        /// <summary>Playing the post-intel guard-appear cutscene (door SE → map).</summary>
        bool playingGuardAppear;
        /// <summary>Playing 【结束交谈】 wait-for-Dafu outro → SC-06.</summary>
        bool playingWaitForDafuOutro;
        /// <summary>Playing Lin friend-request chat → SC-09.</summary>
        bool playingLinContactChat;

        void ShowHotspotInspect(string hotspotId)
        {
            var service = InvestigationService.Instance;
            lastInspectText = service.Inspect(hotspotId);
            inspectQueue.Clear();
            inspectQueue.AddRange(service.GetInspectBeats(hotspotId));
            if (inspectQueue.Count == 0)
                inspectQueue.Add(new InspectBeat { narration = true, text = lastInspectText });
            inspectIndex = 0;
            playingGuardAppear = false;
            SfxController.Instance?.PlayInspect();

            mode = Mode.Investigate;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "investigate";
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            var hotspot = service.Hotspots.Find(h => h.id == hotspotId);
            if (hotspot != null && !string.IsNullOrEmpty(hotspot.background))
                SetStageBackground(hotspot.background);
            else
                locationText.text = "槐安社区";
            RefreshHeader();
            ApplyAtmosphere();
            ShowInspectBeat();
        }

        void StartGuardAppearCutscene()
        {
            playingGuardAppear = true;
            inspectQueue.Clear();
            inspectQueue.AddRange(InvestigationService.BuildGuardAppearBeats());
            inspectIndex = 0;
            mode = Mode.Investigate;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "investigate";
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            SetChrome(true, false, true);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            RefreshHeader();
            ApplyAtmosphere();
            ShowInspectBeat();
        }

        void FinishGuardAppearAndReturnToMap()
        {
            playingGuardAppear = false;
            inspectQueue.Clear();
            inspectIndex = 0;
            // Stay on community investigation; 保安亭 hotspot is unlocked via GuardUnlocked.
            ShowInvestigationMode();
        }

        void EnterGuardBoothFromMap()
        {
            SetInvestigateChrome(false);
            if (GameState.Instance.HasFlag(FlagIds.GuardIntroDone))
            {
                RunSceneTransition(() =>
                {
                    GameState.Instance.SetScene(SceneIds.SC05);
                    ShowTalkMenu();
                });
                return;
            }

            ChapterFlowController.Instance.GoToScene(SceneIds.SC05);
        }

        void ShowInspectBeat()
        {
            if (inspectIndex < 0 || inspectIndex >= inspectQueue.Count)
            {
                if (playingGuardAppear)
                    FinishGuardAppearAndReturnToMap();
                else
                    ShowInvestigationMode();
                return;
            }

            var beat = inspectQueue[inspectIndex];
            if (!string.IsNullOrEmpty(beat.background))
                SetStageBackground(beat.background);
            if (!string.IsNullOrEmpty(beat.sfx))
                SfxController.Instance?.PlayScriptLabel(beat.sfx);
            string bodyKind;
            if (beat.system)
            {
                SetSpeaker("系统", LineSpeaker.System);
                bodyKind = "system";
            }
            else if (beat.narration)
            {
                SetSpeaker("", LineSpeaker.Narration);
                bodyKind = "narration";
            }
            else
            {
                var who = string.IsNullOrEmpty(beat.speaker) ? "小凌" : beat.speaker;
                SetSpeaker(who, LineSpeaker.Character, beat.portrait);
                bodyKind = playingGuardAppear ? "dialogue" : "investigate";
            }

            SetBody(beat.text, true, bodyKind);
            statusText.text = playingGuardAppear
                ? $"继续　{inspectIndex + 1}/{inspectQueue.Count}"
                : $"调查　{inspectIndex + 1}/{inspectQueue.Count}";
            ClearButtons();
            AddStandardDialogueActions(includeSkip: true);
            SetAdvanceEnabled(true);
        }

        void AdvanceInspectOrFinish()
        {
            if (inspectQueue.Count == 0)
            {
                if (playingGuardAppear)
                    FinishGuardAppearAndReturnToMap();
                else
                    ShowInvestigationMode();
                return;
            }
            if (inspectIndex >= inspectQueue.Count - 1)
            {
                inspectQueue.Clear();
                inspectIndex = 0;
                if (playingGuardAppear)
                {
                    FinishGuardAppearAndReturnToMap();
                    return;
                }

                // After both intel hotspots: play 保安亭出场, then return to平面图.
                if (InvestigationService.Instance != null &&
                    InvestigationService.Instance.ConsumePendingGuardAppear())
                {
                    StartGuardAppearCutscene();
                    return;
                }

                ShowInvestigationMode();
                return;
            }
            inspectIndex++;
            ShowInspectBeat();
        }

        public void ShowTalkMenu()
        {
            if (GameState.Instance.HasFlag(FlagIds.DafuInterviewDone) &&
                GameState.Instance.Data.currentSceneId == SceneIds.SC08)
            {
                ShowPostInterviewTalk();
                return;
            }
            mode = Mode.Talk;
            talkIsPostInterview = false;
            talkAwaitingClickReturn = false;
            talkQueue.Clear();
            talkIndex = 0;
            activeTalkTopic = null;
            // SC-05 openTalkMenu line does not Advance, so apply intro-done here.
            if (GameState.Instance.Data.currentSceneId == SceneIds.SC05)
                GameState.Instance.SetFlag(FlagIds.GuardIntroDone);
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            SetStageBackground("保安亭_午后");
            stageHint.text = "交谈";
            RefreshHeader();
            SetSpeaker("系统", LineSpeaker.System);
            SetBody("想向保安叔叔了解什么？");
            statusText.text = "选择一个话题";
            ClearButtons();
            foreach (var topic in InvestigationService.Instance.GuardTopics)
            {
                var t = topic;
                AddChoice(t.label, () => StartGuardTopic(t));
            }
            AddAction("结束交谈", () =>
            {
                talkAwaitingClickReturn = false;
                talkQueue.Clear();
                if (InvestigationService.Instance.CanWaitForDafu())
                    StartWaitForDafuOutro();
                else
                    ShowInvestigationMode();
            }, true);
            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
        }

        void StartWaitForDafuOutro()
        {
            mode = Mode.Talk;
            talkIsPostInterview = false;
            talkAwaitingClickReturn = false;
            playingWaitForDafuOutro = true;
            playingLinContactChat = false;
            activeTalkTopic = null;
            talkQueue.Clear();
            talkQueue.AddRange(InvestigationService.BuildWaitForDafuEndBeats());
            talkIndex = 0;
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            SetStageBackground("保安亭_午后");
            stageHint.text = "交谈";
            RefreshHeader();
            ClearButtons();
            // Actions rebuilt in ShowTalkBeat (includes Skip).
            ShowTalkBeat();
        }

        void StartLinContactChat()
        {
            mode = Mode.Talk;
            talkIsPostInterview = true;
            talkAwaitingClickReturn = false;
            playingWaitForDafuOutro = false;
            playingLinContactChat = true;
            activeTalkTopic = null;
            talkQueue.Clear();
            talkQueue.AddRange(InvestigationService.BuildLinContactBeats());
            talkIndex = 0;
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            SetStageBackground("保安亭_傍晚");
            stageHint.text = "消息";
            RefreshHeader();
            ClearButtons();
            // Actions rebuilt in ShowTalkBeat (includes Skip).
            ShowTalkBeat();
        }

        void StartGuardTopic(TalkTopic topic)
        {
            if (topic == null) return;
            talkIsPostInterview = false;
            talkAwaitingClickReturn = false;
            activeTalkTopic = topic;

            if (topic.beats != null && topic.beats.Count > 0)
            {
                talkQueue.Clear();
                talkQueue.AddRange(topic.beats);
                talkIndex = 0;
                ClearButtons();
                // Actions rebuilt in ShowTalkBeat (includes Skip).
                ShowTalkBeat();
                return;
            }

            // Legacy single-reply topics
            var reply = InvestigationService.Instance.Talk(topic);
            SetSpeaker("保安叔叔", LineSpeaker.Character, topic.portrait);
            SetBody(reply, true, "talk");
            statusText.text = "点击返回话题";
            ClearButtons();
            talkAwaitingClickReturn = true;
            AddAction(UiLoc.T("ui.skip"), TrySkipDialogue);
            AddAction("结束交谈", () =>
            {
                talkAwaitingClickReturn = false;
                if (InvestigationService.Instance.CanWaitForDafu())
                    StartWaitForDafuOutro();
                else
                    ShowInvestigationMode();
            }, true);
            AddAction(UiLoc.T("ui.backlog"), OpenBacklog);
            AddAction(UiLoc.T("ui.menu"), OpenMenu);
            SetAdvanceEnabled(true);
        }

        void ShowTalkBeat()
        {
            if (talkIndex < 0 || talkIndex >= talkQueue.Count)
            {
                FinishTalkTopicBeats();
                return;
            }

            var beat = talkQueue[talkIndex];
            if (!string.IsNullOrEmpty(beat.sfx))
                SfxController.Instance?.PlayScriptLabel(beat.sfx);

            string bodyKind;
            if (beat.system)
            {
                SetSpeaker("系统", LineSpeaker.System);
                bodyKind = "system";
            }
            else if (beat.narration)
            {
                SetSpeaker("", LineSpeaker.Narration);
                bodyKind = "narration";
            }
            else
            {
                var who = string.IsNullOrEmpty(beat.speakerName) ? "保安叔叔" : beat.speakerName;
                SetSpeaker(who, LineSpeaker.Character, beat.portrait);
                bodyKind = "talk";
            }

            SetBody(beat.text, true, bodyKind);
            statusText.text = playingLinContactChat
                ? $"消息　{talkIndex + 1}/{talkQueue.Count}"
                : $"交谈　{talkIndex + 1}/{talkQueue.Count}";
            ClearButtons();
            AddStandardDialogueActions(includeSkip: true);
            SetAdvanceEnabled(true);
        }

        void AdvanceTalkBeatOrFinish()
        {
            if (talkQueue.Count == 0)
            {
                FinishTalkTopicBeats();
                return;
            }
            if (talkIndex >= talkQueue.Count - 1)
            {
                FinishTalkTopicBeats();
                return;
            }
            talkIndex++;
            ShowTalkBeat();
        }

        void FinishTalkTopicBeats()
        {
            if (playingWaitForDafuOutro)
            {
                playingWaitForDafuOutro = false;
                talkQueue.Clear();
                talkIndex = 0;
                activeTalkTopic = null;
                GameState.Instance.SetFlag(FlagIds.WaitingForDafu);
                GameState.Instance.SetObjective("等待大福出现。");
                ChapterFlowController.Instance.GoToScene(SceneIds.SC06);
                return;
            }

            if (playingLinContactChat)
            {
                playingLinContactChat = false;
                talkQueue.Clear();
                talkIndex = 0;
                activeTalkTopic = null;
                GameState.Instance.SetFlag(FlagIds.LinUnlocked);
                GameState.Instance.SetObjective("明天下午15:00前往咖啡馆采访林女士。");
                ChapterFlowController.Instance.GoToScene(SceneIds.SC09);
                return;
            }

            var topic = activeTalkTopic;
            talkQueue.Clear();
            talkIndex = 0;
            activeTalkTopic = null;
            if (topic != null)
                InvestigationService.Instance.Talk(topic);
            ShowTalkMenu();
        }

        void ShowPostInterviewTalk()
        {
            mode = Mode.Talk;
            talkIsPostInterview = true;
            talkAwaitingClickReturn = false;
            SetAdvanceEnabled(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            SetStageBackground("保安亭_傍晚");
            stageHint.text = "核实线索";
            RefreshHeader();
            SetSpeaker("系统", LineSpeaker.System);
            SetBody("用大福提供的线索，向保安打听当年的救助者。");
            ClearButtons();
            var service = InvestigationService.Instance;
            foreach (var topic in service.PostInterviewTopics)
            {
                // Gate 「林姐的信息」 on LinIdentity from who_rescued finishing first.
                if (!service.CanAsk(topic))
                    continue;

                var t = topic;
                AddChoice(t.label, () =>
                {
                    if (!InvestigationService.Instance.CanAsk(t))
                    {
                        SetSpeaker("系统", LineSpeaker.System);
                        SetBody("（还缺少相关线索。先问清当初是谁救助的大福。）", true, "system");
                        statusText.text = "点击返回话题";
                        talkAwaitingClickReturn = true;
                        talkIsPostInterview = true;
                        ClearButtons();
                        AddStandardDialogueActions(includeSkip: true);
                        SetAdvanceEnabled(true);
                        return;
                    }

                    var reply = InvestigationService.Instance.Talk(t);
                    SetSpeaker("保安叔叔", LineSpeaker.Character, t.portrait);
                    SetBody(reply, true, "talk");
                    ClearButtons();
                    // nextSceneId (lin_info → WeChat) only after Talk succeeds.
                    if (!string.IsNullOrEmpty(t.nextSceneId) && t.done)
                    {
                        talkAwaitingClickReturn = false;
                        AddAction("等待回复", () =>
                        {
                            SfxController.Instance?.PlayScriptLabel("信息发送");
                            StartLinContactChat();
                        }, true);
                        AddAction(UiLoc.T("ui.notebook"), OpenNotebook);
                        AddAction(UiLoc.T("ui.menu"), OpenMenu);
                    }
                    else
                    {
                        statusText.text = "点击返回话题";
                        talkAwaitingClickReturn = true;
                        talkIsPostInterview = true;
                        AddStandardDialogueActions(includeSkip: true);
                        SetAdvanceEnabled(true);
                    }
                });
            }
            AddAction("返回调查", () =>
            {
                talkAwaitingClickReturn = false;
                ShowInvestigationMode();
            });
        }

        // Interview UI (Show/Refresh/presets/end) -> GameUI.Interview.cs

        public void ShowEpilogue()
        {
            mode = Mode.Epilogue;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "epilogue";
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            HideWritingMaterialsBoard();
            writingMatsActive = false;
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            SetStageBackground("文章发布页面");
            BgmController.Instance?.ClearScriptSticky();
            BgmController.Instance?.PlayScriptLabel("专题结束_01");
            locationText.text = "几天后";
            stageHint.text = "后日谈";
            RefreshHeader();

            var dir = (WritingDirection)Mathf.Max(0, GameState.Instance.Data.writingDirection);
            epilogueQueue.Clear();
            BuildEpilogueBeats(epilogueQueue, dir, GameState.Instance.Data.lastArticleTitle);
            epilogueIndex = 0;
            ShowEpilogueBeat();
        }

        static void BuildEpilogueBeats(List<InspectBeat> beats, WritingDirection dir, string articleTitle)
        {
            void AddSystem(string text, string background = null) =>
                beats.Add(new InspectBeat { system = true, text = text, background = background });
            void AddNarration(string text, string background = null) =>
                beats.Add(new InspectBeat { narration = true, text = text, background = background });

            AddSystem("文章已发布：" + (articleTitle ?? ""));
            AddNarration("文章发布以后，有不少人第一次知道，大福以前受过那么严重的伤。");
            AddNarration("也有人讨论，救下一只流浪猫以后，是不是一定要把它带回家。");
            AddNarration("林女士没有再解释更多。救治和收养是两件事。");
            AddSystem("—— 几天后 ——", "槐安社区_午后");

            if (dir == WritingDirection.GuardCatToday)
            {
                AddNarration("偶尔会有人来问，大福今天有没有上班。");
                AddNarration("但大福并不知道自己成了报道里的主角。它还是按照自己的时间出现。");
                AddNarration("下午四点多，大福又来了。");
                AddNarration("和文章发布以前没什么不同。");
                AddNarration("大福今天也在上班。");
            }
            else
            {
                AddNarration("还是有人问，林女士为什么没有把大福带回家。");
                AddNarration("也有人说，第一次知道一场救助并不一定要以收养结束。");
                AddNarration("林女士没有成为大福的主人。");
                AddNarration("但大福还是活了下来，并且回到了熟悉的地方。");
                AddNarration("救下一只猫以后，故事并不会立刻结束。");
            }

            AddNarration("报道能记录的，只是它生活里很短的一段。");
            AddNarration("至于大福，它还有明天的饭要吃，还有熟悉的地方要去。");
            AddNarration("它的日子还在继续。");
        }

        void ShowEpilogueBeat()
        {
            if (epilogueIndex < 0 || epilogueIndex >= epilogueQueue.Count)
            {
                ShowEpilogueChapterEnd();
                return;
            }

            var beat = epilogueQueue[epilogueIndex];
            if (!string.IsNullOrEmpty(beat.background))
                SetStageBackground(beat.background);

            string bodyKind;
            if (beat.system)
            {
                SetSpeaker("系统", LineSpeaker.System);
                bodyKind = "system";
            }
            else
            {
                SetSpeaker("", LineSpeaker.Narration);
                bodyKind = "narration";
            }

            SetBody(beat.text, true, bodyKind);
            statusText.text = $"后日谈　{epilogueIndex + 1}/{epilogueQueue.Count}";
            ClearButtons();
            AddStandardDialogueActions(includeSkip: true);
            SetAdvanceEnabled(true);
        }

        void AdvanceEpilogueOrFinish()
        {
            if (epilogueQueue.Count == 0)
            {
                ShowEpilogueChapterEnd();
                return;
            }
            if (epilogueIndex >= epilogueQueue.Count - 1)
            {
                ShowEpilogueChapterEnd();
                return;
            }
            epilogueIndex++;
            ShowEpilogueBeat();
        }

        void ShowEpilogueChapterEnd()
        {
            epilogueQueue.Clear();
            epilogueIndex = 0;
            SetAdvanceEnabled(false);
            if (GameState.Instance != null)
            {
                statusText.text =
                    $"审核 {GameState.Instance.Data.lastReviewScore}　素材 {GameState.Instance.Data.selectedMaterials.Count}/{GameState.Instance.Data.unlockedMaterials.Count}";
            }
            ClearButtons();
            AddAction("第一章 完", () => ChapterFlowController.Instance.OnChapterComplete(), true);
        }

    }
}
