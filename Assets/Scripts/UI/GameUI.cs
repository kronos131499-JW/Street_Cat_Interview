using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Investigation;
using StreetCat.Interview;
using StreetCat.Narrative;
using StreetCat.Notebook;
using StreetCat.Writing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreetCat.UI
{
    public class GameUI : MonoBehaviour
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
        Text locationText;
        Text objectiveText;
        Text chapterChip;
        Image dialoguePanel;
        Button dialogueClick;
        Image namePlate;
        Text nameText;
        Text bodyText;
        Text statusText;
        Text stageHint;
        CanvasGroup locationFade;
        CanvasGroup stageHintFade;
        Coroutine sceneTitleCo;
        string lastSceneTitleKey;
        CanvasGroup dialogueFade;
        Button hideDialogueBtn;
        Text hideDialogueLabel;
        Image sceneFadeImage;
        CanvasGroup sceneFadeCg;
        Coroutine sceneFadeCo;
        bool dialogueHidden;
        bool sceneTransitioning;
        Transform buttonRoot;
        Transform choiceRoot;
        InputField inputField;
        GameObject titleRoot;
        Text titleBrand;
        Text titleSubtitle;
        Text titleTagline;

        // Menu / backlog / notebook
        GameObject menuRoot;
        GameObject backlogRoot;
        GameObject notebookRoot;
        GameObject saveLoadRoot;
        Text backlogText;
        ScrollRect backlogScroll;
        Text notebookLegendText;
        Text notebookDetailText;
        Text notebookInspireText;
        Transform notebookTopicList;
        Transform notebookTabRow;
        ScrollRect notebookDetailScroll;
        string notebookSelectedTopicId;
        int notebookTab; // 0=主题 1=待确认 2=提问记录
        readonly List<GameObject> notebookSpawned = new List<GameObject>();
        Text saveLoadTitle;
        Transform saveLoadList;
        bool saveLoadIsSave; // true=存档, false=读档
        int pendingOverwriteSlot = -999;
        GameObject confirmRoot;
        Text confirmText;
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
        Text interviewSubjectText;
        Text interviewStatusText;
        Text interviewLogText;
        ScrollRect interviewScroll;
        Transform interviewHintRoot;
        Transform interviewActionRoot;
        InputField interviewInput;
        readonly List<GameObject> interviewSpawned = new List<GameObject>();
        readonly List<GameObject> interviewPresetSpawned = new List<GameObject>();

        readonly List<GameObject> spawnedButtons = new List<GameObject>();
        WritingDirection pendingDir = WritingDirection.GuardCatToday;
        readonly List<string> selectedMats = new List<string>();
        int phrasingA;
        int phrasingB;
        ArticleAssembler assembler = new ArticleAssembler();
        string lastInspectText;
        Font font;
        /// <summary>Title / menu typography (OS CJK when available).</summary>
        Font titleFont;
        Coroutine fadeCo;
        Coroutine typewriterCo;
        Coroutine portraitFadeCo;
        Coroutine hintPulseCo;
        Coroutine interviewLlmCo;
        const float SceneTitleFadeIn = 0.35f;
        const float SceneTitleHold = 2.2f;
        const float SceneTitleFadeOut = 0.55f;
        string typewriterFull = "";
        bool typewriterRunning;
        float skipHoldTimer;
        Image advanceCatcher;
        Image choiceHostImage;
        CanvasGroup portraitFade;
        Image atmosphereWash;
        ScrollRect dialogueScroll;
        GameObject investigateRoot;
        Text investigateTitle;
        Text investigateIntelHint;
        Transform investigateHotspotLayer;
        Transform investigateActions;
        Text investigateHoverLabel;
        Transform titleActionRoot;
        readonly List<GameObject> investigateSpawned = new List<GameObject>();
        Text clickHintText;
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
            font = ResolveUiFont();
            titleFont = ResolveTitleFont() ?? font;
            BuildCanvas();
        }

        static Font ResolveUiFont()
        {
            var os = TryOsFont(new[]
            {
                "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC",
                "Noto Sans CJK SC", "Source Han Sans SC", "SimHei", "微软雅黑"
            }, 28);
            if (os != null) return os;
            var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtin != null ? builtin : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        static Font ResolveTitleFont()
        {
            return TryOsFont(new[]
            {
                "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC",
                "STSong", "SimSun", "微软雅黑", "华文楷体", "KaiTi"
            }, 40);
        }

        static Font TryOsFont(string[] names, int size)
        {
            if (names == null) return null;
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    var f = Font.CreateDynamicFontFromOSFont(name, size);
                    if (f != null) return f;
                }
                catch
                {
                    // ignore missing OS fonts
                }
            }
            return null;
        }

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

            // Top HUD — sits in letterbox band, never over stage
            var topBar = CreateImage(canvasGo.transform, "TopBar", VnTheme.TopBar);
            Stretch(topBar.rectTransform, new Vector2(0, VnTheme.TopHudBottom), new Vector2(1, 1f - VnTheme.LetterboxH),
                Vector2.zero, Vector2.zero);

            chapterChip = CreateUiText(topBar.transform, "ChapterChip", 17, TextAnchor.MiddleLeft,
                VnTheme.Accent, new Vector2(40, 0), new Vector2(380, 36));
            chapterChip.text = "第一章　·　编外保安大福";

            objectiveText = CreateUiText(topBar.transform, "Objective", 16, TextAnchor.MiddleLeft,
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

            // Full-stage click catcher (VN: click anywhere to advance) — under title/dialogue/choices
            advanceCatcher = CreateFillImage(canvasGo.transform, "AdvanceCatcher", new Color(0, 0, 0, 0.001f));
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

            bodyText = bodyContent.AddComponent<Text>();
            bodyText.font = font;
            bodyText.fontSize = 25;
            bodyText.color = VnTheme.TextPrimary;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.lineSpacing = 1.2f;
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
            clickHintText.text = "点击对话框继续";

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
            hudActions.transform.SetParent(topBar.transform, false);
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

            BuildInvestigateOverlay(canvasGo.transform);
            BuildInterviewOverlay(canvasGo.transform);
            BuildMenuOverlay(canvasGo.transform);
            BuildBacklogOverlay(canvasGo.transform);
            BuildNotebookOverlay(canvasGo.transform);
            BuildSaveLoadOverlay(canvasGo.transform);
            BuildConfirmOverlay(canvasGo.transform);
            BuildHideDialogueControl(canvasGo.transform);
            BuildSceneFadeOverlay(canvasGo.transform);
        }

        void BuildHideDialogueControl(Transform parent)
        {
            var go = new GameObject("HideDialogue", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-18f, 18f);
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
            hideDialogueLabel.text = "隐藏对白";
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
                hideDialogueLabel.text = dialogueHidden ? "显示对白" : "隐藏对白";
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
            // Full-stage click restores (also advances only after restore on next click).
            if (advanceCatcher != null)
                advanceCatcher.gameObject.SetActive(true);
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
            investigateTitle.fontStyle = FontStyle.Bold;

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
            investigateHoverLabel.fontStyle = FontStyle.Bold;
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

        void BuildInterviewOverlay(Transform parent)
        {
            interviewRoot = new GameObject("InterviewOverlay", typeof(RectTransform));
            interviewRoot.transform.SetParent(parent, false);
            Stretch(interviewRoot.GetComponent<RectTransform>(), new Vector2(0.04f, VnTheme.LetterboxH + 0.02f), new Vector2(0.96f, VnTheme.TopHudBottom - 0.01f),
                Vector2.zero, Vector2.zero);

            var shell = CreateImage(interviewRoot.transform, "Shell", VnTheme.Paper);
            StretchFull(shell.rectTransform);
            // Shell catches leftover clicks so nothing under the overlay steals them.
            shell.raycastTarget = true;
            var shellEdge = CreateImage(shell.transform, "TopEdge", VnTheme.Accent);
            Stretch(shellEdge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), new Vector2(0, 0));
            shellEdge.raycastTarget = false;

            var header = CreateImage(shell.transform, "Header", new Color(0.08f, 0.09f, 0.11f, 1f));
            Stretch(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(0, 0));
            header.raycastTarget = false;

            interviewSubjectText = CreateUiText(header.transform, "Subject", 28, TextAnchor.MiddleLeft,
                VnTheme.TextPrimary, new Vector2(28, 0), new Vector2(700, 40));
            var subRt = interviewSubjectText.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0, 0.5f);
            subRt.anchorMax = new Vector2(0, 0.5f);
            subRt.pivot = new Vector2(0, 0.5f);
            interviewSubjectText.fontStyle = FontStyle.Bold;
            interviewSubjectText.text = "自由采访";

            interviewStatusText = CreateUiText(header.transform, "Status", 18, TextAnchor.MiddleRight,
                VnTheme.TextMuted, new Vector2(-28, 0), new Vector2(900, 40));
            var ist = interviewStatusText.GetComponent<RectTransform>();
            ist.anchorMin = ist.anchorMax = new Vector2(1, 0.5f);
            ist.pivot = new Vector2(1, 0.5f);

            var composer = CreateImage(shell.transform, "Composer", new Color(0.07f, 0.08f, 0.1f, 1f));
            Stretch(composer.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 204));
            composer.raycastTarget = true;
            var composerEdge = CreateImage(composer.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(composerEdge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2), new Vector2(0, 0));
            composerEdge.raycastTarget = false;

            interviewHintRoot = new GameObject("Presets", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            interviewHintRoot.SetParent(composer.transform, false);
            var hrt = interviewHintRoot.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 0);
            hrt.anchorMax = new Vector2(1, 0);
            hrt.pivot = new Vector2(0.5f, 0);
            hrt.anchoredPosition = new Vector2(0, 114);
            hrt.sizeDelta = new Vector2(-40, 34);
            var hhlg = interviewHintRoot.GetComponent<HorizontalLayoutGroup>();
            hhlg.spacing = 8;
            hhlg.childAlignment = TextAnchor.MiddleLeft;
            hhlg.childForceExpandWidth = false;
            hhlg.childForceExpandHeight = true;
            hhlg.childControlWidth = true;
            hhlg.childControlHeight = true;
            hhlg.padding = new RectOffset(20, 20, 0, 0);

            interviewInput = CreateVnInput(composer.transform);
            var iirt = interviewInput.GetComponent<RectTransform>();
            iirt.anchorMin = new Vector2(0, 0);
            iirt.anchorMax = new Vector2(1, 0);
            iirt.pivot = new Vector2(0.5f, 0);
            iirt.anchoredPosition = new Vector2(-70, 58);
            iirt.sizeDelta = new Vector2(-200, 48);
            interviewInput.lineType = InputField.LineType.SingleLine;
            interviewInput.GetComponent<Image>().color = VnTheme.InputBg;

            var sendGo = new GameObject("Send", typeof(RectTransform), typeof(Image), typeof(Button));
            sendGo.transform.SetParent(composer.transform, false);
            var srt = sendGo.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(1, 0);
            srt.pivot = new Vector2(1, 0);
            srt.anchoredPosition = new Vector2(-24, 58);
            srt.sizeDelta = new Vector2(110, 48);
            sendGo.GetComponent<Image>().color = new Color(0.22f, 0.18f, 0.12f, 1f);
            sendGo.GetComponent<Image>().raycastTarget = true;
            sendGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                SubmitInterviewQuestion();
            });
            var sendLabel = CreateUiText(sendGo.transform, "L", 20, TextAnchor.MiddleCenter, VnTheme.Accent, Vector2.zero, new Vector2(110, 48));
            StretchFull(sendLabel.GetComponent<RectTransform>());
            sendLabel.text = "发送";
            sendLabel.raycastTarget = false;

            interviewActionRoot = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            interviewActionRoot.SetParent(composer.transform, false);
            var art = interviewActionRoot.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 0);
            art.anchorMax = new Vector2(1, 0);
            art.pivot = new Vector2(0.5f, 0);
            art.anchoredPosition = new Vector2(0, 12);
            art.sizeDelta = new Vector2(-40, 40);
            var ahlg = interviewActionRoot.GetComponent<HorizontalLayoutGroup>();
            ahlg.spacing = 10;
            ahlg.childAlignment = TextAnchor.MiddleRight;
            ahlg.childForceExpandWidth = false;
            ahlg.childControlWidth = true;
            ahlg.childControlHeight = true;
            ahlg.padding = new RectOffset(20, 20, 0, 0);

            var logPanel = CreateImage(shell.transform, "LogPanel", new Color(0.06f, 0.07f, 0.09f, 1f));
            // Keep clear of composer (204px) so log never eats preset/send/action clicks.
            Stretch(logPanel.rectTransform, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(16, 212), new Vector2(-16, -84));
            logPanel.raycastTarget = false;

            interviewScroll = logPanel.gameObject.AddComponent<ScrollRect>();
            interviewScroll.horizontal = false;
            interviewScroll.vertical = true;
            interviewScroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateImage(logPanel.transform, "Viewport", new Color(0, 0, 0, 0.01f));
            Stretch(viewport.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 16), new Vector2(-20, -16));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(0, 0);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            interviewLogText = content.AddComponent<Text>();
            interviewLogText.font = font;
            interviewLogText.fontSize = 24;
            interviewLogText.color = VnTheme.TextPrimary;
            interviewLogText.alignment = TextAnchor.UpperLeft;
            interviewLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            interviewLogText.verticalOverflow = VerticalWrapMode.Overflow;
            interviewLogText.lineSpacing = 1.25f;
            interviewLogText.raycastTarget = false;
            interviewLogText.supportRichText = true;

            interviewScroll.viewport = viewport.rectTransform;
            interviewScroll.content = crt;

            // Composer must paint/raycast above LogPanel (built later in hierarchy).
            composer.transform.SetAsLastSibling();

            interviewRoot.SetActive(false);
        }

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
            var tx = tgo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 16;
            tx.alignment = TextAnchor.MiddleCenter;
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
            prt.sizeDelta = new Vector2(420, 500);
            var menuEdge = CreateImage(panel.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(menuEdge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2.5f), new Vector2(0, 0));

            var title = CreateUiText(panel.transform, "MenuTitle", 26, TextAnchor.UpperCenter,
                VnTheme.Accent, new Vector2(0, -22), new Vector2(360, 40));
            title.text = "菜单";
            title.fontStyle = FontStyle.Bold;
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

            void Item(string label, UnityEngine.Events.UnityAction act)
            {
                var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(list.transform, false);
                go.GetComponent<Image>().color = VnTheme.Button;
                go.GetComponent<LayoutElement>().preferredHeight = 46;
                go.GetComponent<Button>().onClick.AddListener(act);
                var tg = new GameObject("T", typeof(RectTransform));
                tg.transform.SetParent(go.transform, false);
                StretchFull(tg.GetComponent<RectTransform>());
                var tx = tg.AddComponent<Text>();
                tx.font = font;
                tx.fontSize = 20;
                tx.alignment = TextAnchor.MiddleCenter;
                tx.color = VnTheme.TextPrimary;
                tx.text = label;
                tx.raycastTarget = false;
            }

            Item("继续", CloseMenu);
            Item("回看", () => { CloseMenuSilent(); OpenBacklog(); });
            Item("自动存档（读）", () =>
            {
                CloseMenuSilent();
                if (SaveSystem.SlotExists(SaveSystem.AutoSlot))
                    ChapterFlowController.Instance.LoadSlot(SaveSystem.AutoSlot);
                else
                    statusText.text = "还没有自动存档";
            });
            Item("存档", () => { CloseMenuSilent(); OpenSaveLoad(true); });
            Item("读档", () => { CloseMenuSilent(); OpenSaveLoad(false); });
            Item("笔记", () => { CloseMenuSilent(); OpenNotebook(); });
            Item("返回标题", () =>
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
            var ctx = ct.AddComponent<Text>();
            ctx.font = font;
            ctx.fontSize = 18;
            ctx.alignment = TextAnchor.MiddleCenter;
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
                var tx = tg.AddComponent<Text>();
                tx.font = font;
                tx.fontSize = 20;
                tx.alignment = TextAnchor.MiddleCenter;
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
                var tx = label.AddComponent<Text>();
                tx.font = font;
                tx.fontSize = 18;
                tx.alignment = TextAnchor.UpperLeft;
                tx.color = VnTheme.TextPrimary;
                tx.horizontalOverflow = HorizontalWrapMode.Wrap;
                tx.verticalOverflow = VerticalWrapMode.Truncate;
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

            var title = CreateUiText(panel.transform, "Title", 24, TextAnchor.UpperLeft,
                VnTheme.Accent, new Vector2(28, -20), new Vector2(400, 36));
            title.text = "对话回看";

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var srt = scrollGo.GetComponent<RectTransform>();
            Stretch(srt, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f), Vector2.zero, Vector2.zero);
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

            backlogText = content.AddComponent<Text>();
            backlogText.font = font;
            backlogText.fontSize = 22;
            backlogText.color = VnTheme.TextPrimary;
            backlogText.alignment = TextAnchor.UpperLeft;
            backlogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            backlogText.verticalOverflow = VerticalWrapMode.Overflow;
            backlogText.lineSpacing = 1.2f;

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
            var ctx = ct.AddComponent<Text>();
            ctx.font = font;
            ctx.fontSize = 18;
            ctx.alignment = TextAnchor.MiddleCenter;
            ctx.color = VnTheme.TextPrimary;
            ctx.text = "关闭";
            ctx.raycastTarget = false;

            backlogRoot.SetActive(false);
        }

        void BuildNotebookOverlay(Transform parent)
        {
            notebookRoot = new GameObject("NotebookOverlay", typeof(RectTransform));
            notebookRoot.transform.SetParent(parent, false);
            StretchFull(notebookRoot.GetComponent<RectTransform>());
            var dim = CreateImage(notebookRoot.transform, "Dim", VnTheme.OverlayDim);
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = CreateImage(notebookRoot.transform, "Panel", VnTheme.Paper);
            Stretch(panel.rectTransform, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.93f), Vector2.zero, Vector2.zero);
            var edge = CreateImage(panel.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(edge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), new Vector2(0, 0));

            var title = CreateUiText(panel.transform, "Title", 26, TextAnchor.UpperLeft,
                VnTheme.Accent, new Vector2(28, -18), new Vector2(420, 36));
            title.text = "记者笔记";
            title.fontStyle = FontStyle.Bold;

            notebookLegendText = CreateUiText(panel.transform, "Legend", 15, TextAnchor.UpperLeft,
                VnTheme.TextMuted, new Vector2(28, -52), new Vector2(700, 24));
            notebookLegendText.text = "○ 新线索　　◐ 还有疑问　　● 已充分了解";

            notebookTabRow = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            notebookTabRow.SetParent(panel.transform, false);
            var trt = notebookTabRow.GetComponent<RectTransform>();
            Stretch(trt, new Vector2(0.04f, 0.86f), new Vector2(0.72f, 0.93f), Vector2.zero, Vector2.zero);
            var th = notebookTabRow.GetComponent<HorizontalLayoutGroup>();
            th.spacing = 8;
            th.childForceExpandWidth = true;
            th.childForceExpandHeight = true;

            void Tab(string label, int idx)
            {
                var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(notebookTabRow, false);
                go.GetComponent<Image>().color = VnTheme.Button;
                go.GetComponent<LayoutElement>().preferredHeight = 36;
                int captured = idx;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    notebookTab = captured;
                    RefreshNotebookPanel();
                });
                var tg = new GameObject("T", typeof(RectTransform));
                tg.transform.SetParent(go.transform, false);
                StretchFull(tg.GetComponent<RectTransform>());
                var tx = tg.AddComponent<Text>();
                tx.font = font;
                tx.fontSize = 17;
                tx.alignment = TextAnchor.MiddleCenter;
                tx.color = VnTheme.TextPrimary;
                tx.text = label;
                tx.raycastTarget = false;
            }

            Tab("采访主题", 0);
            Tab("待确认", 1);
            Tab("提问记录", 2);

            var closeBtn = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(panel.transform, false);
            var cbrt = closeBtn.GetComponent<RectTransform>();
            cbrt.anchorMin = cbrt.anchorMax = new Vector2(1, 1);
            cbrt.pivot = new Vector2(1, 1);
            cbrt.anchoredPosition = new Vector2(-16, -14);
            cbrt.sizeDelta = new Vector2(100, 36);
            closeBtn.GetComponent<Image>().color = VnTheme.Button;
            closeBtn.GetComponent<Button>().onClick.AddListener(CloseNotebook);
            var ct = new GameObject("T", typeof(RectTransform));
            ct.transform.SetParent(closeBtn.transform, false);
            StretchFull(ct.GetComponent<RectTransform>());
            var ctx = ct.AddComponent<Text>();
            ctx.font = font;
            ctx.fontSize = 18;
            ctx.alignment = TextAnchor.MiddleCenter;
            ctx.color = VnTheme.TextPrimary;
            ctx.text = "关闭";
            ctx.raycastTarget = false;

            var listHost = new GameObject("TopicListHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            listHost.transform.SetParent(panel.transform, false);
            Stretch(listHost.GetComponent<RectTransform>(), new Vector2(0.04f, 0.12f), new Vector2(0.34f, 0.84f), Vector2.zero, Vector2.zero);
            listHost.GetComponent<Image>().color = new Color(0, 0, 0, 0.22f);
            var listScroll = listHost.GetComponent<ScrollRect>();
            listScroll.horizontal = false;
            var listVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            listVp.transform.SetParent(listHost.transform, false);
            StretchFull(listVp.GetComponent<RectTransform>());
            listVp.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            var listContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContent.transform.SetParent(listVp.transform, false);
            notebookTopicList = listContent.transform;
            var lcrt = listContent.GetComponent<RectTransform>();
            lcrt.anchorMin = new Vector2(0, 1);
            lcrt.anchorMax = new Vector2(1, 1);
            lcrt.pivot = new Vector2(0.5f, 1);
            lcrt.sizeDelta = Vector2.zero;
            var lv = listContent.GetComponent<VerticalLayoutGroup>();
            lv.spacing = 6;
            lv.padding = new RectOffset(8, 8, 8, 8);
            lv.childForceExpandWidth = true;
            lv.childControlHeight = true;
            listContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listScroll.viewport = listVp.GetComponent<RectTransform>();
            listScroll.content = lcrt;

            var detailHost = new GameObject("DetailHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            detailHost.transform.SetParent(panel.transform, false);
            Stretch(detailHost.GetComponent<RectTransform>(), new Vector2(0.36f, 0.20f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero);
            detailHost.GetComponent<Image>().color = new Color(0, 0, 0, 0.22f);
            notebookDetailScroll = detailHost.GetComponent<ScrollRect>();
            notebookDetailScroll.horizontal = false;
            var dVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            dVp.transform.SetParent(detailHost.transform, false);
            StretchFull(dVp.GetComponent<RectTransform>());
            dVp.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            var dContent = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            dContent.transform.SetParent(dVp.transform, false);
            var dcrt = dContent.GetComponent<RectTransform>();
            dcrt.anchorMin = new Vector2(0, 1);
            dcrt.anchorMax = new Vector2(1, 1);
            dcrt.pivot = new Vector2(0.5f, 1);
            dcrt.sizeDelta = Vector2.zero;
            dContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            notebookDetailText = dContent.AddComponent<Text>();
            notebookDetailText.font = font;
            notebookDetailText.fontSize = 20;
            notebookDetailText.color = VnTheme.TextPrimary;
            notebookDetailText.alignment = TextAnchor.UpperLeft;
            notebookDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            notebookDetailText.verticalOverflow = VerticalWrapMode.Overflow;
            notebookDetailText.lineSpacing = 1.15f;
            notebookDetailScroll.viewport = dVp.GetComponent<RectTransform>();
            notebookDetailScroll.content = dcrt;

            var inspireBtn = new GameObject("Inspire", typeof(RectTransform), typeof(Image), typeof(Button));
            inspireBtn.transform.SetParent(panel.transform, false);
            Stretch(inspireBtn.GetComponent<RectTransform>(), new Vector2(0.36f, 0.04f), new Vector2(0.96f, 0.17f), Vector2.zero, Vector2.zero);
            inspireBtn.GetComponent<Image>().color = VnTheme.ButtonPrimary;
            inspireBtn.GetComponent<Button>().onClick.AddListener(UseNotebookInspiration);
            notebookInspireText = CreateUiText(inspireBtn.transform, "InspireLabel", 17, TextAnchor.MiddleLeft,
                VnTheme.Accent, Vector2.zero, Vector2.zero);
            StretchFull(notebookInspireText.rectTransform);
            notebookInspireText.rectTransform.offsetMin = new Vector2(16, 6);
            notebookInspireText.rectTransform.offsetMax = new Vector2(-16, -6);
            notebookInspireText.text = "✦ 提问灵感";
            notebookInspireText.alignment = TextAnchor.MiddleLeft;
            notebookInspireText.horizontalOverflow = HorizontalWrapMode.Wrap;
            notebookInspireText.verticalOverflow = VerticalWrapMode.Truncate;

            notebookRoot.SetActive(false);
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

            var logoCn = CreateTitleSprite(left.transform, "LogoCn", "title_logo_cn", Color.white, false);
            TitleMenuLayout.Apply(logoCn.rectTransform, "logo_cn",
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.96f));
            logoCn.preserveAspect = true;
            MakeTitleEditable(logoCn.gameObject, "logo_cn");

            var logoEn = CreateTitleSprite(left.transform, "LogoEn", "title_logo_en", Color.white, false);
            TitleMenuLayout.Apply(logoEn.rectTransform, "logo_en",
                new Vector2(0.10f, 0.68f), new Vector2(0.90f, 0.80f));
            logoEn.preserveAspect = true;
            MakeTitleEditable(logoEn.gameObject, "logo_en");

            titleBrand = CreateUiText(left.transform, "Brand", 48, TextAnchor.MiddleCenter,
                new Color(0.12f, 0.10f, 0.09f, 0.96f), Vector2.zero, new Vector2(420, 64));
            titleBrand.font = titleFont != null ? titleFont : font;
            titleBrand.text = "街角专访";
            titleBrand.fontStyle = FontStyle.Bold;
            TitleMenuLayout.Apply(titleBrand.GetComponent<RectTransform>(), "logo_cn",
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f));
            titleBrand.gameObject.SetActive(logoCn.sprite == null);

            var quoteBox = CreateTitleSprite(left.transform, "QuoteBox", "title_quote_box_l", Color.white, false);
            TitleMenuLayout.Apply(quoteBox.rectTransform, "quote_box",
                new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.36f));
            quoteBox.preserveAspect = true;
            MakeTitleEditable(quoteBox.gameObject, "quote_box");

            titleSubtitle = CreateUiText(left.transform, "Sub", 17, TextAnchor.UpperLeft,
                new Color(0.28f, 0.22f, 0.16f, 0.88f), Vector2.zero, new Vector2(360, 80));
            titleSubtitle.font = titleFont != null ? titleFont : font;
            titleSubtitle.lineSpacing = 1.15f;
            titleSubtitle.text = "此间　·　社会观察专栏\n街角的声音，值得被听见。";
            TitleMenuLayout.Apply(titleSubtitle.GetComponent<RectTransform>(), "subtitle",
                new Vector2(0.16f, 0.14f), new Vector2(0.88f, 0.32f));
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

            var contentsLabel = CreateUiText(right.transform, "ContentsLabel", 20, TextAnchor.MiddleCenter,
                new Color(0.30f, 0.24f, 0.18f, 0.78f), Vector2.zero, new Vector2(280, 36));
            contentsLabel.font = titleFont != null ? titleFont : font;
            contentsLabel.text = "CONTENTS";
            contentsLabel.fontStyle = FontStyle.Bold;
            TitleMenuLayout.Apply(contentsLabel.GetComponent<RectTransform>(), "contents_header",
                new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.96f));

            titleActionRoot = new GameObject("TitleActions", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            titleActionRoot.SetParent(right.transform, false);
            // Narrower column so tape buttons aren't stretched full page-width.
            TitleMenuLayout.Apply(titleActionRoot.GetComponent<RectTransform>(), "title_actions",
                new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.82f));
            MakeTitleEditable(titleActionRoot.gameObject, "title_actions");
            var tah = titleActionRoot.GetComponent<VerticalLayoutGroup>();
            tah.spacing = 14;
            tah.childAlignment = TextAnchor.UpperCenter;
            tah.childForceExpandWidth = false;
            tah.childForceExpandHeight = false;
            tah.childControlWidth = true;
            tah.childControlHeight = true;
            tah.padding = new RectOffset(12, 12, 6, 6);

            titleTagline = CreateUiText(right.transform, "Tag", 15, TextAnchor.MiddleCenter,
                new Color(0.32f, 0.26f, 0.20f, 0.72f), Vector2.zero, new Vector2(360, 40));
            titleTagline.font = titleFont != null ? titleFont : font;
            titleTagline.text = "第一章　　编外保安大福";
            TitleMenuLayout.Apply(titleTagline.GetComponent<RectTransform>(), "tagline",
                new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.18f));
            MakeTitleEditable(titleTagline.gameObject, "tagline");

            BuildTitleDeskProps(titleRoot.transform);
        }

        void MakeTitleEditable(GameObject go, string id)
        {
#if UNITY_EDITOR
            if (go == null || string.IsNullOrEmpty(id)) return;
            var target = go.GetComponent<RectTransform>();
            var title = TitleMenuLayout.DisplayNames.TryGetValue(id, out var n) ? n : id;

            // Unity: only one Graphic per GameObject. Text hosts need a child hit Image.
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

        Text CreateUiText(Transform parent, string name, int size, TextAnchor align, Color color, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        InputField CreateVnInput(Transform parent)
        {
            var go = new GameObject("InterviewInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 44);
            rt.sizeDelta = new Vector2(-80, 44);
            go.GetComponent<Image>().color = VnTheme.InputBg;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(14, 6), new Vector2(-14, -6));
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.color = VnTheme.TextPrimary;
            text.supportRichText = false;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(go.transform, false);
            Stretch(phGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(14, 6), new Vector2(-14, -6));
            var ph = phGo.AddComponent<Text>();
            ph.font = font;
            ph.fontSize = 22;
            ph.color = new Color(1, 1, 1, 0.28f);
            ph.text = "想问什么？";

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
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
                choiceHostImage.color = new Color(0, 0, 0, 0.001f);
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
            SpawnButton(buttonRoot, label, action, primary, 118);
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

            // Shorter + taller tape strips (avoid full-page-width ribbons).
            float btnH = primary ? 70f : 62f;
            float btnW = primary ? 300f : 280f;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = btnH;
            le.preferredHeight = btnH;
            le.minWidth = btnW;
            le.preferredWidth = btnW;
            le.flexibleWidth = 0f;

            string iconKey = TitleIconForLabel(label);
            var iconSpr = VnArt.GetTitle(iconKey);
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
                irt.sizeDelta = new Vector2(30f, 30f);
            }

            if (primary)
            {
                var clipSpr = VnArt.GetTitle("deco_paperclip");
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

            var labelColor = primary
                ? new Color(0.14f, 0.08f, 0.05f, 0.96f)
                : new Color(0.18f, 0.14f, 0.10f, 0.94f);
            var tgo = new GameObject("Label", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(iconSpr != null ? 52f : 20f, 2f), new Vector2(primary ? -28f : -16f, -2f));
            var tx = tgo.AddComponent<Text>();
            tx.font = titleFont != null ? titleFont : font;
            tx.fontSize = primary ? 23 : 20;
            tx.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
            tx.alignment = TextAnchor.MiddleLeft;
            tx.color = labelColor;
            tx.text = string.Format("{0:00}  {1}", index, label);
            tx.raycastTarget = false;

            spawnedButtons.Add(go);
        }

        static string TitleIconForLabel(string label)
        {
            if (label.Contains("新游戏")) return "icon_play";
            if (label.Contains("继续") || label.Contains("自动档")) return "icon_cassette";
            if (label.Contains("读档")) return "icon_map";
            if (label.Contains("笔记") || label.Contains("清除")) return "icon_doc";
            if (label.Contains("设置")) return "icon_gear";
            if (label.Contains("退出")) return "icon_exit";
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
            var idle = inspected
                ? new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0.05f)
                : new Color(1f, 1f, 1f, 0.02f);
            var hover = new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0.18f);
            img.color = idle;

            var outline = CreateImage(go.transform, "Outline", new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0f));
            StretchFull(outline.rectTransform);
            outline.raycastTarget = false;

            Image MakeEdge(string n, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
            {
                var e = CreateImage(go.transform, n, new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0f));
                Stretch(e.rectTransform, aMin, aMax, offMin, offMax);
                e.raycastTarget = false;
                return e;
            }
            var topE = MakeEdge("T", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2), Vector2.zero);
            var botE = MakeEdge("B", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 2));
            var leftE = MakeEdge("L", new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(2, 0));
            var rightE = MakeEdge("R", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-2, 0), Vector2.zero);

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
                var edgeCol = on
                    ? new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0.85f)
                    : new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0f);
                topE.color = edgeCol;
                botE.color = edgeCol;
                leftE.color = edgeCol;
                rightE.color = edgeCol;
                outline.color = on
                    ? new Color(VnTheme.Accent.r, VnTheme.Accent.g, VnTheme.Accent.b, 0.08f)
                    : new Color(0, 0, 0, 0);
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
            var tx = tgo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 16;
            tx.alignment = TextAnchor.MiddleCenter;
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
            var tx = tgo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = wide ? 22 : 17;
            tx.alignment = TextAnchor.MiddleCenter;
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
            dialoguePanel.gameObject.SetActive(showDialogue);
            titleRoot.SetActive(showTitle);
            // Scene name is a brief toast (see RequestSceneTitleReveal), not persistent chrome.
            if (!showLocation || showTitle || mode == Mode.Investigate || mode == Mode.Interview)
                HideSceneTitleImmediate();
            chapterChip.gameObject.SetActive(!showTitle);
            objectiveText.gameObject.SetActive(!showTitle);
            if (interviewRoot != null && mode != Mode.Interview)
                interviewRoot.SetActive(false);
            if (investigateRoot != null && mode != Mode.Investigate)
                investigateRoot.SetActive(false);
            if (buttonRoot != null)
                buttonRoot.gameObject.SetActive(mode != Mode.Interview && mode != Mode.Investigate);
            if (choiceRoot != null)
            {
                bool showChoices = mode != Mode.Interview && mode != Mode.Investigate;
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
            interviewRoot.transform.SetAsLastSibling();
            BringOverlayStackToFront();
        }

        void BringOverlayStackToFront()
        {
            if (menuRoot != null) menuRoot.transform.SetAsLastSibling();
            if (backlogRoot != null) backlogRoot.transform.SetAsLastSibling();
            if (notebookRoot != null) notebookRoot.transform.SetAsLastSibling();
            if (saveLoadRoot != null) saveLoadRoot.transform.SetAsLastSibling();
            if (confirmRoot != null) confirmRoot.transform.SetAsLastSibling();
            if (hideDialogueBtn != null) hideDialogueBtn.transform.SetAsLastSibling();
            if (sceneFadeImage != null) sceneFadeImage.transform.SetAsLastSibling();
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
        /// </summary>
        void LayoutPortraitRect(Sprite sprite)
        {
            if (portraitImage == null) return;

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

        void ClearInterviewChromeButtons()
        {
            foreach (var go in interviewSpawned)
                if (go) Destroy(go);
            interviewSpawned.Clear();
        }

        void AddInterviewAction(string label, UnityEngine.Events.UnityAction action, bool primary = false)
        {
            var go = new GameObject("Act", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(interviewActionRoot, false);
            go.GetComponent<Image>().color = primary ? new Color(0.2f, 0.16f, 0.12f, 1f) : VnTheme.Button;
            go.GetComponent<Image>().raycastTarget = true;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 36;
            le.preferredHeight = 36;
            le.minWidth = 110;
            le.preferredWidth = Mathf.Max(110, 18 + label.Length * 18);
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                action();
            });
            var tgo = new GameObject("L", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            StretchFull(tgo.GetComponent<RectTransform>());
            var tx = tgo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 17;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = primary ? VnTheme.Accent : VnTheme.TextPrimary;
            tx.text = label;
            tx.raycastTarget = false;
            interviewSpawned.Add(go);
        }

        void SubmitInterviewQuestion()
        {
            if (mode != Mode.Interview) return;
            if (interviewInput == null || InterviewController.Instance == null)
                return;
            // Block double-submit (Enter + 发送) — second submit used to dump rule lines mid-wait.
            if (interviewLlmCo != null)
                return;
            if (InterviewController.Instance.IsTranslating)
                return;

            var q = (interviewInput.text ?? "").Trim();
            if (string.IsNullOrEmpty(q))
                return;
            interviewInput.text = "";

            var ic = InterviewController.Instance;
            var who = ic.Subject == InterviewSubject.Dafu ? "大福" : "林女士";
            bool llmReady = LlmClient.Instance != null
                && LlmClient.Instance.IsConfigured
                && ic.Subject != InterviewSubject.None;

            // When LLM is configured, NEVER append rule speaker lines in Ask — wait for DeepSeek.
            var reply = ic.Ask(q, deferSpeakerLines: llmReady);

            // Hostile: rule silence only — no free LLM (avoids long defensiveness).
            bool skipLlm = reply != null
                           && string.Equals(reply.intent, "hostile", StringComparison.Ordinal);

            if (llmReady && reply != null
                && !skipLlm
                && !reply.shouldEnd
                && reply.understood
                && reply.replyLines != null
                && reply.replyLines.Count > 0)
            {
                DialogueHistory.Instance?.Add("小凌", q, "interview");
                ic.SetTranslatingPlaceholder(true);
                RefreshInterviewView();
                interviewLlmCo = StartCoroutine(PreferLlmInterviewReplyCo(q, reply, who));
                return;
            }

            // No LLM key, hostile, or reply not worth translating: show rule lines once.
            if (llmReady && reply != null)
                ic.AppendSpeakerReply(reply);
            DialogueHistory.Instance?.Add("小凌", q, "interview");
            RecordInterviewReplyHistory(who, reply, reply?.replyLines);
            RefreshInterviewView();
        }

        void RecordInterviewReplyHistory(string who, InterviewReply reply, IList<string> lines)
        {
            if (reply == null || DialogueHistory.Instance == null) return;
            if (!string.IsNullOrEmpty(reply.behavior))
                DialogueHistory.Instance.Add("", "（" + reply.behavior + "）", "interview");
            if (lines == null) return;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    DialogueHistory.Instance.Add(who, line.Trim(), "interview");
            }
        }

        /// <summary>Wait for DeepSeek; only then show lines. Rule text is fallback-only.</summary>
        IEnumerator PreferLlmInterviewReplyCo(string question, InterviewReply reply, string who)
        {
            var llm = LlmClient.Instance;
            var ic = InterviewController.Instance;
            // Keep a frozen copy of rule lines — reply.replyLines must not be overwritten before fallback.
            var ruleLines = reply?.replyLines != null
                ? new List<string>(reply.replyLines)
                : new List<string>();

            if (llm == null || !llm.IsConfigured || ic == null || reply == null)
            {
                FinishInterviewReply(ic, reply, who, ruleLines, null);
                yield break;
            }

            // Repeat / stale「刚才说过了」must not be stuffed into the LLM as RULE facts.
            bool omitRuleFacts = reply.isRepeat
                                 || LooksLikeStaleRepeatRule(ruleLines);
            string facts = "";
            if (!omitRuleFacts)
            {
                facts = string.Join("\n", ruleLines);
                if (!string.IsNullOrEmpty(reply.behavior))
                    facts = "行为：" + reply.behavior + "\n台词：\n" + facts;
            }
            else if (reply.cognitiveBoundary)
            {
                // Boundary: keep a short confused cue only — no invented medical facts.
                facts = "（认知边界：保持困惑，短答「不知道/那是什么」，勿解释人类医疗）";
            }
            // Freer answer mode: question-first; rule lines are soft reference only.
            var userMsg = ic.BuildFreeAnswerUserMessage(facts, question, reply);

            // Wait out rate-limit / min-interval instead of dumping rule lines early.
            while (llm.IsCoolingDown)
            {
                if (mode != Mode.Interview)
                {
                    ic.SetTranslatingPlaceholder(false);
                    interviewLlmCo = null;
                    yield break;
                }
                float left = Mathf.Max(0.1f, llm.SecondsUntilReady);
                yield return new WaitForSecondsRealtime(Mathf.Min(0.5f, left));
            }

            string rephrased = null;
            yield return llm.RephraseCoroutine(ic.BuildStylePrompt(reply), userMsg, question, text => rephrased = text);

            if (mode != Mode.Interview || ic.Subject == InterviewSubject.None)
            {
                ic.SetTranslatingPlaceholder(false);
                interviewLlmCo = null;
                yield break;
            }

            var aiLines = string.IsNullOrWhiteSpace(rephrased) ? null : SplitLlmReplyLines(rephrased);
            string outcome;
            string detail = null;
            // Design gate: reject leaks / over-explanation; fall back to rule lines.
            if (aiLines != null && !ic.AcceptRephrasedLines(aiLines, reply, out var reject))
            {
                detail = reject;
                Debug.LogWarning("[Interview] LLM rejected by design filter: " + reject
                    + "\nRule:\n" + string.Join("\n", ruleLines)
                    + "\nAI:\n" + rephrased);
                aiLines = null;
                outcome = "rejected_fallback_rule";
            }
            else if (aiLines != null && aiLines.Count > 0)
            {
                outcome = "ai_ok";
                Debug.Log("[Interview] LLM free answer ok (" + who + " / " + (reply.intent ?? "?") + ")\nQ: "
                    + question + "\nAI:\n" + string.Join("\n", aiLines));
            }
            else
            {
                outcome = "fallback_rule";
                detail = llm.LastError ?? "empty";
                Debug.Log("[Interview] LLM fallback to rule lines (" + who + "): " + detail);
            }

            InterviewDebugLog.Exchange(
                question,
                reply.intent,
                freeMode: true,
                ruleText: facts,
                aiText: rephrased,
                outcome: outcome,
                detail: detail);

            FinishInterviewReply(ic, reply, who, ruleLines, aiLines);
        }

        static bool LooksLikeStaleRepeatRule(IList<string> ruleLines)
        {
            if (ruleLines == null || ruleLines.Count == 0) return false;
            var joined = string.Join("", ruleLines);
            return joined.Contains("刚才说过了")
                   || joined.Contains("这段我刚才说过了")
                   || joined.Contains("没更多了")
                   || joined.Contains("换个问法");
        }

        void FinishInterviewReply(
            InterviewController ic,
            InterviewReply reply,
            string who,
            List<string> ruleLines,
            List<string> aiLines)
        {
            ic?.SetTranslatingPlaceholder(false);
            var lines = (aiLines != null && aiLines.Count > 0) ? aiLines : ruleLines;
            if (reply != null)
                reply.replyLines = lines != null ? new List<string>(lines) : new List<string>();
            ic?.AppendSpeakerReply(reply, lines);
            RecordInterviewReplyHistory(who, reply, lines);
            RefreshInterviewView();
            interviewLlmCo = null;
        }

        static List<string> SplitLlmReplyLines(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return result;
            var parts = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (var p in parts)
            {
                var line = p.Trim();
                if (line.Length == 0)
                    continue;
                // Strip common speaker prefixes if the model echoes them.
                if (line.StartsWith("大福：") || line.StartsWith("大福:"))
                    line = line.Substring(3).Trim();
                else if (line.StartsWith("林女士：") || line.StartsWith("林女士:"))
                    line = line.Substring(4).Trim();
                if (line.Length > 0)
                    result.Add(line);
            }
            if (result.Count == 0)
                result.Add(text.Trim());
            return result;
        }

        void FormatInterviewLog(StringBuilder sb)
        {
            var log = InterviewController.Instance.Log;
            foreach (var line in log)
            {
                if (line.StartsWith("小凌："))
                {
                    sb.AppendLine();
                    sb.Append("<color=#E8C07A>").Append(line).Append("</color>\n");
                }
                else if (line.StartsWith("大福：") || line.StartsWith("林女士："))
                {
                    sb.Append("<color=#F2EDE6>").Append(line).Append("</color>\n");
                }
                else if (line.StartsWith("（") || line.StartsWith("("))
                {
                    sb.Append("<color=#9AA3AD>").Append(line).Append("</color>\n");
                }
                else if (line.StartsWith("系统"))
                {
                    sb.Append("<color=#D4B56A>").Append(line).Append("</color>\n");
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
        }

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
            objectiveText.text = string.IsNullOrEmpty(gs?.Data.currentObjective) ? "" : "目标　" + gs.Data.currentObjective;
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
                    stageHint.text = scene.title;
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
                bodyText.color = VnTheme.TextMuted;
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
                bodyText.color = VnTheme.TextInner;
                lastHistorySpeaker = "";
                ApplyPortrait(name, kind, portraitTag, lineText);
                return;
            }
            else if (kind == LineSpeaker.System)
            {
                nameText.text = "系统";
                bodyText.color = VnTheme.TextSystem;
                lastHistorySpeaker = "系统";
                SetPortrait(null);
                return;
            }
            else
            {
                nameText.text = name;
                bodyText.color = VnTheme.TextPrimary;
                lastHistorySpeaker = name;
            }

            ApplyPortrait(name, kind, portraitTag, lineText);
        }

        void ApplyPortrait(string name, LineSpeaker kind, string portraitTag, string lineText = null)
        {
            var tag = portraitTag;
            if (!string.IsNullOrEmpty(tag) && (tag.Contains("无立绘") || tag == "none"))
            {
                SetPortrait(null);
                return;
            }

            var isXiaoling = (!string.IsNullOrEmpty(name) && name.Contains("小凌"))
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

            bool useTypewriter = mode == Mode.Dialogue || mode == Mode.Talk
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
            const float cps = 42f;
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
        }

        void Update()
        {
            if (mode == Mode.Title)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
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

            // Hold Ctrl to continuously skip (classic VN)
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!inputFocused && canClickAdvance && !waitingForChoice && ctrl && mode == Mode.Dialogue)
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
            if (mode != Mode.Dialogue && !(mode == Mode.Investigate && !investigateHotspotsVisible) && mode != Mode.Talk)
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

        void TrySkipDialogue()
        {
            if (!canClickAdvance || waitingForChoice)
                return;
            if (mode != Mode.Dialogue)
                return;
            if (mode == Mode.Menu || mode == Mode.Backlog || mode == Mode.Notebook || mode == Mode.Title)
                return;
            if (inputField != null && inputField.gameObject.activeSelf && inputField.isFocused)
                return;
            if (typewriterRunning)
                CompleteTypewriter();
            SceneDirector.Instance?.SkipToBreak(RecordSkippedLine);
        }

        void RecordSkippedLine(ScriptLine line)
        {
            if (line == null) return;
            // Keep prop show/hide in sync when fast-forwarding past sticky cues.
            if (!string.IsNullOrEmpty(line.prop))
                SetProp(line.prop);
            else if (line.hideProp)
                SetProp(null);
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
                speaker = "系统";
                kind = "system";
            }
            else
            {
                speaker = line.speakerName ?? "";
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
            bool allowClick = (canClickAdvance || typewriterRunning || inspectClick || talkBeats || talkClick) && !hasChoices;
            if (dialogueClick != null)
                dialogueClick.interactable = allowClick;
            if (advanceCatcher != null)
            {
                bool showCatcher = allowClick &&
                    (mode == Mode.Dialogue || inspectClick || talkBeats || talkClick);
                advanceCatcher.gameObject.SetActive(showCatcher);
                var btn = advanceCatcher.GetComponent<Button>();
                if (btn != null) btn.interactable = showCatcher;
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
            bool show = !waitingForChoice && (canClickAdvance || typewriterRunning || inspectClick || talkBeats || talkClick)
                && (mode == Mode.Dialogue || inspectClick || talkBeats || talkClick);
            clickHintText.gameObject.SetActive(show);
            if (!show) return;
            if (typewriterRunning)
                clickHintText.text = "点击显示全文";
            else if (inspectClick)
                clickHintText.text = playingGuardAppear
                    ? (inspectIndex >= inspectQueue.Count - 1 ? "点击返回平面图" : "点击继续")
                    : (inspectIndex >= inspectQueue.Count - 1 ? "点击返回调查" : "点击继续");
            else if (talkBeats)
                clickHintText.text = talkIndex >= talkQueue.Count - 1 ? "点击返回话题" : "点击继续";
            else if (talkClick)
                clickHintText.text = "点击返回话题";
            else
                clickHintText.text = "点击继续　长按Ctrl跳过";
            if (hintPulseCo != null) StopCoroutine(hintPulseCo);
            if (show && !typewriterRunning)
                hintPulseCo = StartCoroutine(PulseHint());
        }

        IEnumerator PulseHint()
        {
            while (clickHintText != null && clickHintText.gameObject.activeSelf && !typewriterRunning && canClickAdvance)
            {
                float t = 0f;
                while (t < 1.2f)
                {
                    t += Time.unscaledDeltaTime;
                    float a = 0.35f + 0.35f * (0.5f + 0.5f * Mathf.Sin(t * 4f));
                    var c = clickHintText.color;
                    c.a = a;
                    clickHintText.color = c;
                    yield return null;
                }
            }
            hintPulseCo = null;
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
            inputField.gameObject.SetActive(false);
            if (menuRoot) menuRoot.SetActive(false);
            if (backlogRoot) backlogRoot.SetActive(false);
            if (saveLoadRoot) saveLoadRoot.SetActive(false);
            if (notebookRoot) notebookRoot.SetActive(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(false, true, false);
            ClearButtons();
            statusText.text = "";
            ApplyAtmosphere();
            ApplyStageArt();
            SetPortrait(null);
            SetProp(null);
            if (titleTagline != null && (titleTagline.text == "存档已清除" || string.IsNullOrEmpty(titleTagline.text)))
                titleTagline.text = "第一章　　编外保安大福";

            AddAction("新游戏", () => ChapterFlowController.Instance.StartNewGame(), true);
            AddAction("继续", () => ChapterFlowController.Instance.ContinueOrNew());
            AddAction("读档", () => OpenSaveLoad(false));
            AddAction("清除存档", () =>
            {
                SaveSystem.Delete();
                if (titleTagline != null)
                    titleTagline.text = "存档已清除";
            });
            AddAction("退出", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        public void ShowDialogueMode()
        {
            mode = Mode.Dialogue;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "dialogue";
            inputField.gameObject.SetActive(false);
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
            AddAction("跳过", TrySkipDialogue);
            AddAction("回看", OpenBacklog);
            AddAction("笔记", OpenNotebook);
            AddAction("菜单", OpenMenu);
            SetAdvanceEnabled(true);
            statusText.text = "点击继续　·　Ctrl / 跳过";
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

            // Cue-only beat (bg / bgm / sfx / hideProp / bare jump): apply and auto-advance.
            // Prop-show beats WAIT for click (visual cue the player must acknowledge).
            bool cueOnly = string.IsNullOrEmpty(line.text)
                && string.IsNullOrEmpty(line.prop)
                && (line.choices == null || line.choices.Count == 0)
                && !line.openInvestigation && !line.openTalkMenu && !line.openWriting && !line.openInterview
                && (!string.IsNullOrEmpty(line.background)
                    || !string.IsNullOrEmpty(line.bgm)
                    || !string.IsNullOrEmpty(line.sfx)
                    || line.hideProp
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
            if (lineKind == LineSpeaker.System) speaker = "系统";
            SetSpeaker(speaker, lineKind, line.portrait, line.text);
            var kind = lineKind == LineSpeaker.System ? "system"
                : (lineKind == LineSpeaker.Narration || lineKind == LineSpeaker.Inner) ? "narration"
                : "dialogue";
            // Empty prop-only beats still wait for click; do not pollute backlog.
            SetBody(line.text, !string.IsNullOrEmpty(line.text), kind);

            ClearButtons();
            if (line.choices != null && line.choices.Count > 0)
            {
                statusText.text = "做出选择";
                SetAdvanceEnabled(false, true);
                for (int i = 0; i < line.choices.Count; i++)
                {
                    int idx = i;
                    AddChoice(line.choices[i].label, () => SceneDirector.Instance.Choose(idx));
                }
                AddAction("回看", OpenBacklog);
                AddAction("笔记", OpenNotebook);
                AddAction("菜单", OpenMenu);
            }
            else
            {
                statusText.text = "点击继续　·　Ctrl / 跳过";
                SetAdvanceEnabled(true);
                AddAction("跳过", TrySkipDialogue);
                AddAction("回看", OpenBacklog);
                AddAction("笔记", OpenNotebook);
                AddAction("菜单", OpenMenu);
            }
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
                    AddAction("跳过", TrySkipDialogue);
                    AddAction("回看", OpenBacklog);
                    AddAction("笔记", OpenNotebook);
                    AddAction("菜单", OpenMenu);
                    SetAdvanceEnabled(!waitingForChoice, waitingForChoice);
                    statusText.text = waitingForChoice ? "做出选择" : "点击继续　·　Ctrl / 跳过";
                    break;
                case Mode.Investigate: ShowInvestigationMode(); break;
                case Mode.Talk: ShowTalkMenu(); break;
                case Mode.Interview: RefreshInterviewView(); break;
                case Mode.Writing: ShowWritingDirectionPick(); break;
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
            SetProp(null);
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
            AddAction("笔记", OpenNotebook);
            AddAction("菜单", OpenMenu);
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
            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
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
            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
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
                AddAction("回看", OpenBacklog);
                AddAction("菜单", OpenMenu);
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
            AddAction("结束交谈", () =>
            {
                talkAwaitingClickReturn = false;
                if (InvestigationService.Instance.CanWaitForDafu())
                    StartWaitForDafuOutro();
                else
                    ShowInvestigationMode();
            }, true);
            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
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
            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
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
            foreach (var topic in InvestigationService.Instance.PostInterviewTopics)
            {
                var t = topic;
                AddChoice(t.label, () =>
                {
                    var reply = InvestigationService.Instance.Talk(t);
                    SetSpeaker("保安叔叔", LineSpeaker.Character, t.portrait);
                    SetBody(reply, true, "talk");
                    ClearButtons();
                    if (!string.IsNullOrEmpty(t.nextSceneId))
                    {
                        talkAwaitingClickReturn = false;
                        AddAction("等待回复", () =>
                        {
                            SfxController.Instance?.PlayScriptLabel("信息发送");
                            StartLinContactChat();
                        }, true);
                    }
                    else
                    {
                        statusText.text = "点击返回话题";
                        talkAwaitingClickReturn = true;
                        talkIsPostInterview = true;
                        SetAdvanceEnabled(true);
                    }
                    AddAction("笔记", OpenNotebook);
                    AddAction("菜单", OpenMenu);
                });
            }
            AddAction("返回调查", () =>
            {
                talkAwaitingClickReturn = false;
                ShowInvestigationMode();
            });
        }

        public void ShowInterview(InterviewSubject subject, bool returnToWritingAfter = false)
        {
            mode = Mode.Interview;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = subject == InterviewSubject.Lin ? "interview_lin" : "interview_dafu";
            SetAdvanceEnabled(false);
            SaveSystem.Autosave();
            InterviewController.Instance.Begin(subject, returnToWritingAfter);

            SetProp(null);
            SetChrome(false, false, false);
            SetInterviewChrome(true);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            SetStageBackground(subject == InterviewSubject.Lin ? "咖啡馆_午后" : "保安亭_傍晚");
            RefreshHeader();

            interviewSubjectText.text = subject == InterviewSubject.Dafu
                ? (returnToWritingAfter ? "补充采访　·　大福" : "喵语翻译器　·　采访大福")
                : (returnToWritingAfter ? "补充采访　·　林女士" : "自由采访　·　林女士");
            interviewInput.gameObject.SetActive(true);
            interviewInput.text = "";
            interviewInput.placeholder.GetComponent<Text>().text =
                subject == InterviewSubject.Dafu ? "想问大福什么？" : "想问林女士什么？";

            ClearButtons();
            RefreshInterviewView();
        }

        void RefreshInterviewView()
        {
            if (mode != Mode.Interview)
                mode = Mode.Interview;
            SetInterviewChrome(true);
            RefreshHeader();

            interviewSubjectText.text = InterviewController.Instance.Subject == InterviewSubject.Dafu
                ? "喵语翻译器　·　采访大福"
                : "自由采访　·　林女士";

            var sb = new StringBuilder();
            FormatInterviewLog(sb);
            interviewLogText.text = sb.ToString().TrimEnd();
            Canvas.ForceUpdateCanvases();
            if (interviewScroll != null)
                interviewScroll.verticalNormalizedPosition = 0f;

            var st = InterviewController.Instance.Stats;
            interviewStatusText.text = st == null
                ? ""
                : $"{st.StatusText}　　信任 {st.trust}　压力 {st.stress}　注意力 {st.attention}"
                  + (InterviewController.Instance.CanComplete() ? "　　可结束采访" : "");

            ClearInterviewChromeButtons();
            ApplyStageArt();
            if (InterviewController.Instance.Subject == InterviewSubject.Dafu)
                SetPortrait(VnArt.ResolvePortrait("大福", LineSpeaker.Character));
            else
                SetPortrait(VnArt.ResolvePortrait("林女士", LineSpeaker.Character));

            AddInterviewAction("结束采访", TryEndInterview);
            if (InterviewController.Instance.IsReinterviewFromWriting)
                AddInterviewAction("返回写稿", () =>
                {
                    SetInterviewChrome(false);
                    InterviewController.Instance.AbandonToWriting();
                });
            AddInterviewAction("回看", OpenBacklog);
            AddInterviewAction("笔记", OpenNotebook);
            AddInterviewAction("菜单", OpenMenu);

            RefreshInterviewPresets();
        }

        void ClearInterviewPresets()
        {
            foreach (var go in interviewPresetSpawned)
                if (go) Destroy(go);
            interviewPresetSpawned.Clear();
        }

        void RefreshInterviewPresets()
        {
            ClearInterviewPresets();
            if (interviewHintRoot == null || InterviewController.Instance == null)
                return;

            var subject = InterviewController.Instance.Subject;
            if (subject == InterviewSubject.None)
            {
                interviewHintRoot.gameObject.SetActive(false);
                return;
            }

            var presets = ReporterNotebook.Instance != null
                ? ReporterNotebook.Instance.GetPresetAskQuestions(subject, 4)
                : null;
            if (presets == null || presets.Count == 0)
            {
                interviewHintRoot.gameObject.SetActive(false);
                return;
            }

            interviewHintRoot.gameObject.SetActive(true);
            int shown = Mathf.Min(4, presets.Count);
            for (int i = 0; i < shown; i++)
                SpawnInterviewPresetChip(presets[i]);
        }

        void SpawnInterviewPresetChip(string question)
        {
            if (interviewHintRoot == null || string.IsNullOrEmpty(question))
                return;

            string label = question.Length <= 14 ? question : question.Substring(0, 13) + "…";
            var go = new GameObject("Preset", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(interviewHintRoot, false);
            go.GetComponent<Image>().color = new Color(0.14f, 0.13f, 0.12f, 0.98f);
            go.GetComponent<Image>().raycastTarget = true;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 30;
            le.preferredHeight = 30;
            le.minWidth = 72;
            le.preferredWidth = Mathf.Clamp(22 + label.Length * 15, 96, 220);
            le.flexibleWidth = 0;
            string fill = question;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                FillInterviewInput(fill);
            });
            var tgo = new GameObject("L", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            StretchFull(tgo.GetComponent<RectTransform>());
            var tx = tgo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 15;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = VnTheme.TextMuted;
            tx.text = label;
            tx.raycastTarget = false;
            interviewPresetSpawned.Add(go);
        }

        void FillInterviewInput(string question)
        {
            if (interviewInput == null || string.IsNullOrEmpty(question))
                return;
            if (!interviewInput.gameObject.activeInHierarchy)
                return;
            interviewInput.text = question;
            interviewInput.ActivateInputField();
            interviewInput.caretPosition = interviewInput.text.Length;
            if (statusText) statusText.text = "已填入预设提问，可修改后发送";
        }

        void TryEndInterview()
        {
            if (!InterviewController.Instance.CanComplete())
            {
                var msg = "现在结束的话，似乎还有不少事情没有问清楚。\n\n" + InterviewController.Instance.MissingSummary();
                interviewLogText.text = interviewLogText.text + "\n\n<color=#A8C0D4>" + msg.Replace("\n", "\n") + "</color>";
                Canvas.ForceUpdateCanvases();
                if (interviewScroll != null)
                    interviewScroll.verticalNormalizedPosition = 0f;

                ClearInterviewChromeButtons();
                AddInterviewAction("继续采访", () => RefreshInterviewView());
                AddInterviewAction("确认结束", () =>
                {
                    SetInterviewChrome(false);
                    InterviewController.Instance.End(true);
                }, true);
                return;
            }
            SetInterviewChrome(false);
            InterviewController.Instance.End(true);
        }

        public void ShowWriting()
        {
            mode = Mode.Writing;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "writing";
            SetAdvanceEnabled(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            inputField.gameObject.SetActive(false);
            SetChrome(true, false, true);
            stageHint.text = "写稿";
            SetStageBackground("编辑部工位_上午");
            RefreshHeader();
            selectedMats.Clear();
            EnsureCoreMaterials();
            ShowWritingDirectionPick();
        }

        void EnsureCoreMaterials()
        {
            var gs = GameState.Instance;
            foreach (var id in gs.Data.intel)
                MaterialUnlockTable.TryUnlockFromIntel(id);
            if (gs.HasIntel(IntelIds.DafuAppearTime) || gs.HasIntel(IntelIds.DafuRestSpot))
                gs.UnlockMaterial(MaterialIds.M01);
            if (gs.HasIntel(IntelIds.CommunityCare) || gs.HasIntel(IntelIds.DafuNoOwner))
                gs.UnlockMaterial(MaterialIds.M14);
        }

        void ShowWritingDirectionPick()
        {
            RefreshHeader();
            SetSpeaker("沈禾", LineSpeaker.Character, "认真");
            var unlocked = GameState.Instance.Data.unlockedMaterials.Count;
            var body = "选一个报道立意。素材决定你能写什么，立意决定你想讲什么。\n\n已解锁素材 " +
                       unlocked + " 张。";
            if (unlocked < 8)
                body += "\n\n素材还不够成稿。如果采访里还有没问到的，可以回去补充。";
            SetBody(body);
            ClearButtons();
            AddChoice("《大福今天也在上班》　从流浪猫到社区保安", () =>
            {
                pendingDir = WritingDirection.GuardCatToday;
                ShowMaterialPick();
            });
            AddChoice("《救下一只猫以后》　一次没有以收养结束的救助", () =>
            {
                pendingDir = WritingDirection.RescueWithoutAdoption;
                ShowMaterialPick();
            });
            AddReInterviewActions(unlocked < 8);
            AddAction("笔记", OpenNotebook);
        }

        void ShowMaterialPick()
        {
            SetSpeaker("系统", LineSpeaker.System);
            var unlocked = GameState.Instance.Data.unlockedMaterials.Count;
            var sb = new StringBuilder();
            sb.AppendLine($"选择 8～10 张素材（点选切换）。必须包含「回到槐安社区」。");
            sb.AppendLine($"已选　{selectedMats.Count}　/　已解锁　{unlocked}");
            sb.AppendLine();
            if (unlocked < 8)
                sb.AppendLine("已解锁素材不足 8 张，可返回采访补齐后再选。");
            else if (selectedMats.Count < 8)
                sb.AppendLine("至少选 8 张才能生成文章。素材不够时可返回采访补充。");
            if (selectedMats.Count == 0)
                sb.AppendLine("尚未选中素材。下方列表可滚动浏览。");
            else
            {
                sb.AppendLine("已选中：");
                foreach (var id in selectedMats)
                {
                    var m = MaterialCatalog.Get(id);
                    sb.AppendLine("·　" + (m != null ? m.title : id));
                }
            }
            SetBody(sb.ToString(), false);
            statusText.text = "点素材名切换选中　·　列表可滚动";
            ClearButtons();
            foreach (var id in GameState.Instance.Data.unlockedMaterials)
            {
                var mid = id;
                var m = MaterialCatalog.Get(mid);
                var selected = selectedMats.Contains(mid);
                var label = (selected ? "●　" : "○　") + (m != null ? m.title : mid)
                    + (m != null ? "　[" + m.type + "]" : "");
                AddChoice(label, () =>
                {
                    if (selectedMats.Contains(mid)) selectedMats.Remove(mid);
                    else if (selectedMats.Count < 10) selectedMats.Add(mid);
                    ShowMaterialPick();
                });
            }
            AddAction("下一步", ShowPhrasing, true);
            AddAction("返回立意", ShowWritingDirectionPick);
            AddReInterviewActions(selectedMats.Count < 8 || unlocked < 8);
            AddAction("笔记", OpenNotebook);
        }

        void ShowPhrasing()
        {
            SetSpeaker("系统", LineSpeaker.System);
            SetBody("关键表述只在对应素材被选入时生效，将直接影响沈禾对事实严谨度的审核。\n\n"
                + "01｜麻绳来源\n"
                + "A. 疑似人为虐待，麻绳被故意勒上（推测写成事实）\n"
                + "B. 无人目睹，无法确认是否人为伤害\n\n"
                + "02｜康复后的去向\n"
                + "A. 把大福扔回了外面（误导性措辞）\n"
                + "B. 送回槐安社区，并确认有人继续照料");
            ClearButtons();
            AddChoice("麻绳｜A 故意勒伤（风险）", () => { phrasingA = 0; ShowPhrasingRelease(); });
            AddChoice("麻绳｜B 无法确认（稳妥）", () => { phrasingA = 1; ShowPhrasingRelease(); });
            AddAction("返回改选材", ShowMaterialPick);
            AddReInterviewActions(false);
        }

        void ShowPhrasingRelease()
        {
            SetSpeaker("系统", LineSpeaker.System);
            SetBody("再选康复后去向表述：\n\n"
                + "A. 把大福扔回了外面（误导）\n"
                + "B. 送回槐安社区，并确认有人继续照料（稳妥）");
            ClearButtons();
            AddChoice("放归｜A 扔回外面（风险）", () => { phrasingB = 0; GenerateArticle(); });
            AddChoice("放归｜B 送回社区（稳妥）", () => { phrasingB = 1; GenerateArticle(); });
            AddAction("返回上一步", ShowPhrasing);
            AddAction("返回改选材", ShowMaterialPick);
        }

        void GenerateArticle()
        {
            if (!assembler.CanAssemble(pendingDir, selectedMats, out var err))
            {
                SetSpeaker("沈禾", LineSpeaker.Character, "认真");
                SetBody("现在还不能成稿。\n\n" + err + "\n\n可以改选材，或返回采访补齐素材。");
                statusText.text = err;
                ClearButtons();
                AddAction("返回改选材", ShowMaterialPick, true);
                AddAction("重选立意", ShowWritingDirectionPick);
                AddReInterviewActions(true);
                AddAction("笔记", OpenNotebook);
                return;
            }
            assembler.Assemble(pendingDir, selectedMats, phrasingA, phrasingB);
            GameState.Instance.Data.writingDirection = (int)pendingDir;
            GameState.Instance.Data.selectedMaterials = new List<string>(selectedMats);
            GameState.Instance.Data.lastArticleTitle = assembler.Title;
            GameState.Instance.Data.lastArticleBody = assembler.Body;
            GameState.Instance.Data.lastReviewScore = assembler.Score;
            SetStageBackground("沈禾办公室_上午");
            BgmController.Instance?.PlayScriptLabel("编辑部日常_01（循环）");
            SetSpeaker("沈禾", LineSpeaker.Character, assembler.CanPublish ? "淡淡认可" : "认真");
            SetBody("稿件已提交。\n\n" + assembler.Body + "\n\n—— 沈禾审核 ——\n" + assembler.ReviewText);
            statusText.text = assembler.CanPublish
                ? $"审核通过　{assembler.Score}"
                : $"审核退回　分支{assembler.ReviewBranch}";
            ClearButtons();
            if (assembler.CanPublish)
                AddAction("确认发布", () => ChapterFlowController.Instance.OnArticlePublished(), true);
            else
            {
                AddAction("返回写稿", ShowMaterialPick, true);
                AddAction("查看记者笔记", OpenNotebook);
                AddReInterviewActions(true);
            }
            AddAction("重选立意", ShowWritingDirectionPick);
        }

        /// <summary>
        /// Offers re-interview jumps based on which interviews are unlocked.
        /// When highlight is true (insufficient materials / failed assemble / can't publish), label is emphasized.
        /// </summary>
        void AddReInterviewActions(bool highlight)
        {
            var dafuDone = GameState.Instance.HasFlag(FlagIds.DafuInterviewDone);
            var linDone = GameState.Instance.HasFlag(FlagIds.LinInterviewDone);
            if (!dafuDone && !linDone)
                return;

            if (dafuDone && linDone)
            {
                AddAction(highlight ? "重新采访…" : "返回采访", ShowReInterviewMenu, highlight);
                return;
            }

            if (dafuDone)
            {
                AddAction(highlight ? "重新采访大福" : "返回采访大福",
                    () => ChapterFlowController.Instance.BeginReInterview(InterviewSubject.Dafu),
                    highlight);
            }
            if (linDone)
            {
                AddAction(highlight ? "重新采访林女士" : "返回采访林女士",
                    () => ChapterFlowController.Instance.BeginReInterview(InterviewSubject.Lin),
                    highlight);
            }
        }

        void ShowReInterviewMenu()
        {
            SetSpeaker("系统", LineSpeaker.System);
            SetBody("写稿素材不够时，可以回去补充采访。已获得的情报与素材卡会保留。\n\n要重新采访谁？");
            statusText.text = "补充采访";
            ClearButtons();
            if (GameState.Instance.HasFlag(FlagIds.DafuInterviewDone))
                AddChoice("重新采访大福", () =>
                    ChapterFlowController.Instance.BeginReInterview(InterviewSubject.Dafu));
            if (GameState.Instance.HasFlag(FlagIds.LinInterviewDone))
                AddChoice("重新采访林女士", () =>
                    ChapterFlowController.Instance.BeginReInterview(InterviewSubject.Lin));
            AddAction("返回写稿", () =>
            {
                if (selectedMats.Count > 0)
                    ShowMaterialPick();
                else
                    ShowWritingDirectionPick();
            }, true);
            AddAction("笔记", OpenNotebook);
        }

        public void ShowEpilogue()
        {
            mode = Mode.Epilogue;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "epilogue";
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
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
            var sb = new StringBuilder();
            sb.AppendLine("文章已发布：" + GameState.Instance.Data.lastArticleTitle);
            sb.AppendLine();
            sb.AppendLine("文章发布以后，有不少人第一次知道，大福以前受过那么严重的伤。");
            sb.AppendLine("也有人讨论，救下一只流浪猫以后，是不是一定要把它带回家。");
            sb.AppendLine("林女士没有再解释更多。救治和收养是两件事。");
            sb.AppendLine();
            sb.AppendLine("—— 几天后 ——");
            sb.AppendLine();
            if (dir == WritingDirection.GuardCatToday)
            {
                sb.AppendLine("偶尔会有人来问，大福今天有没有上班。");
                sb.AppendLine("但大福并不知道自己成了报道里的主角。它还是按照自己的时间出现。");
                sb.AppendLine("下午四点多，大福又来了。和文章发布以前没什么不同。");
                sb.AppendLine();
                sb.AppendLine("大福今天也在上班。");
            }
            else
            {
                sb.AppendLine("还是有人问，林女士为什么没有把大福带回家。");
                sb.AppendLine("也有人说，第一次知道一场救助并不一定要以收养结束。");
                sb.AppendLine("林女士没有成为大福的主人。但大福还是活了下来，并且回到了熟悉的地方。");
                sb.AppendLine();
                sb.AppendLine("救下一只猫以后，故事并不会立刻结束。");
            }
            sb.AppendLine();
            sb.AppendLine("报道能记录的，只是它生活里很短的一段。");
            sb.AppendLine("至于大福，它还有明天的饭要吃，还有熟悉的地方要去。");
            sb.AppendLine("它的日子还在继续。");
            SetSpeaker("", LineSpeaker.Narration);
            SetBody(sb.ToString());
            statusText.text = $"审核 {GameState.Instance.Data.lastReviewScore}　素材 {GameState.Instance.Data.selectedMaterials.Count}/{GameState.Instance.Data.unlockedMaterials.Count}";
            ClearButtons();
            AddAction("第一章 完", () => ChapterFlowController.Instance.OnChapterComplete(), true);
        }

        void OpenNotebook()
        {
            if (mode != Mode.Menu && mode != Mode.Backlog && mode != Mode.Notebook)
            {
                returnFromOverlay = mode;
                savedWaitingForChoice = waitingForChoice;
            }
            mode = Mode.Notebook;
            SetAdvanceEnabled(false);
            if (inputField) inputField.gameObject.SetActive(false);
            if (menuRoot) menuRoot.SetActive(false);
            if (backlogRoot) backlogRoot.SetActive(false);
            if (saveLoadRoot) saveLoadRoot.SetActive(false);

            // Keep underlying investigate/interview chrome; notebook is a full overlay.
            // Do not call SetInvestigateChrome from interview — it also hides interviewRoot.
            if (returnFromOverlay == Mode.Investigate)
            {
                /* leave investigate chrome */
            }
            else if (returnFromOverlay == Mode.Interview)
            {
                /* leave interview chrome */
            }
            else
            {
                SetInvestigateChrome(false);
                SetInterviewChrome(false);
            }

            ReporterNotebook.Instance?.RefreshFromState();
            notebookTab = 0;
            notebookSelectedTopicId = null;
            if (notebookRoot != null)
            {
                notebookRoot.SetActive(true);
                BringOverlayStackToFront();
            }
            RefreshNotebookPanel();
        }

        void CloseNotebook()
        {
            if (notebookRoot) notebookRoot.SetActive(false);
            // Explicit exit so we never treat Notebook as the resume target.
            if (returnFromOverlay == Mode.Notebook || returnFromOverlay == Mode.Menu || returnFromOverlay == Mode.Backlog)
            {
                var ui = GameState.Instance != null ? GameState.Instance.Data.uiMode : "";
                if (ui == "investigate") returnFromOverlay = Mode.Investigate;
                else if (!string.IsNullOrEmpty(ui) && ui.StartsWith("interview")) returnFromOverlay = Mode.Interview;
                else if (ui == "writing") returnFromOverlay = Mode.Writing;
                else returnFromOverlay = Mode.Dialogue;
            }
            ResumeOverlayReturn();
        }

        void ClearNotebookSpawned()
        {
            foreach (var go in notebookSpawned)
                if (go) Destroy(go);
            notebookSpawned.Clear();
        }

        void RefreshNotebookPanel()
        {
            if (notebookRoot == null || !notebookRoot.activeSelf) return;
            var nb = ReporterNotebook.Instance;
            if (nb == null)
            {
                if (notebookDetailText) notebookDetailText.text = "记者笔记尚未初始化。";
                return;
            }

            ClearNotebookSpawned();

            if (notebookTab == 0)
            {
                bool any = false;
                foreach (var t in nb.VisibleTopics())
                {
                    any = true;
                    if (string.IsNullOrEmpty(notebookSelectedTopicId))
                        notebookSelectedTopicId = t.id;
                    SpawnNotebookTopicButton(t);
                }
                if (!any)
                {
                    notebookSelectedTopicId = null;
                    notebookDetailText.text =
                        "还没有写入笔记。\n\n调查社区、与保安交谈，或开始自由采访后，采访主题会陆续出现在这里。";
                    notebookInspireText.text = "✦ 暂无提问灵感";
                }
                else
                {
                    ShowNotebookTopicDetail(notebookSelectedTopicId);
                }
            }
            else if (notebookTab == 1)
            {
                notebookSelectedTopicId = null;
                var gaps = nb.PendingGaps();
                var sb = new StringBuilder();
                sb.AppendLine("【待确认】");
                sb.AppendLine("大福无法解释、或仍需向人类核实的问题。");
                sb.AppendLine();
                if (gaps.Count == 0)
                    sb.AppendLine("（当前没有待确认条目）");
                else
                {
                    foreach (var g in gaps)
                        sb.AppendLine("？ " + g);
                }
                notebookDetailText.text = sb.ToString();
                notebookInspireText.text = "🔎 可向保安 / 林女士核实这些缺口";
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("【提问记录】");
                sb.AppendLine("自由采访中已关联到笔记主题的问答摘要。");
                sb.AppendLine();
                var log = nb.QaLog;
                if (log == null || log.Count == 0)
                    sb.AppendLine("（还没有采访问答记录）");
                else
                {
                    for (int i = log.Count - 1; i >= 0; i--)
                    {
                        var q = log[i];
                        if (q == null) continue;
                        var topic = nb.Topics.Find(x => x.id == q.topicId);
                        var title = topic != null ? topic.title : "未归类";
                        sb.AppendLine($"▸ {title}");
                        sb.AppendLine("问：" + q.question);
                        sb.AppendLine($"{q.speaker}：" + (string.IsNullOrEmpty(q.answerSummary) ? "……" : q.answerSummary));
                        sb.AppendLine();
                    }
                }
                notebookDetailText.text = sb.ToString();
                notebookInspireText.text = "✦ 在「采访主题」页可使用提问灵感填入输入框";
            }

            Canvas.ForceUpdateCanvases();
            if (notebookDetailScroll != null)
                notebookDetailScroll.verticalNormalizedPosition = 1f;
        }

        void SpawnNotebookTopicButton(NotebookTopic topic)
        {
            var go = new GameObject(topic.id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(notebookTopicList, false);
            bool selected = topic.id == notebookSelectedTopicId;
            go.GetComponent<Image>().color = selected ? VnTheme.ButtonPrimary : VnTheme.Button;
            go.GetComponent<LayoutElement>().preferredHeight = 52;
            string id = topic.id;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                notebookSelectedTopicId = id;
                notebookTab = 0;
                RefreshNotebookPanel();
            });
            var tg = new GameObject("T", typeof(RectTransform));
            tg.transform.SetParent(go.transform, false);
            StretchFull(tg.GetComponent<RectTransform>());
            var tx = tg.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 17;
            tx.alignment = TextAnchor.MiddleLeft;
            tx.color = VnTheme.TextPrimary;
            tx.text = $"  {ReporterNotebook.StatusMark(topic.status)}  {topic.title}";
            tx.raycastTarget = false;
            notebookSpawned.Add(go);
        }

        void ShowNotebookTopicDetail(string topicId)
        {
            var nb = ReporterNotebook.Instance;
            var t = nb.Topics.Find(x => x.id == topicId);
            if (t == null)
            {
                notebookDetailText.text = "";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{ReporterNotebook.StatusMark(t.status)}  {t.title}");
            sb.AppendLine(ReporterNotebook.StatusLabel(t.status));
            sb.AppendLine();
            if (t.notes.Count == 0)
                sb.AppendLine("（该主题尚无具体笔记）");
            else
            {
                foreach (var n in t.notes)
                    sb.AppendLine("· " + n.text);
            }
            var src = nb.SourcesLine(t);
            if (!string.IsNullOrEmpty(src))
            {
                sb.AppendLine();
                sb.AppendLine(src);
            }

            var qa = nb.QaForTopic(t.id);
            if (qa.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("—— 相关提问 ——");
                int show = Mathf.Min(4, qa.Count);
                for (int i = qa.Count - show; i < qa.Count; i++)
                {
                    var q = qa[i];
                    sb.AppendLine("问：" + q.question);
                    if (!string.IsNullOrEmpty(q.answerSummary))
                        sb.AppendLine($"{q.speaker}：" + q.answerSummary);
                }
            }

            notebookDetailText.text = sb.ToString();

            if (t.status == TopicStatus.Complete || string.IsNullOrEmpty(t.inspiration))
                notebookInspireText.text = "● 主要事实已足够，可继续提问但不主动提示";
            else if (t.inspirationIsInvestigate)
                notebookInspireText.text = "🔎 " + t.inspiration;
            else
                notebookInspireText.text = "✦ 提问灵感（点击填入采访框）：" + t.inspiration;
        }

        void UseNotebookInspiration()
        {
            if (notebookTab != 0 || string.IsNullOrEmpty(notebookSelectedTopicId))
                return;
            var nb = ReporterNotebook.Instance;
            if (nb == null) return;
            var t = nb.Topics.Find(x => x.id == notebookSelectedTopicId);
            if (t == null) return;

            if (t.inspirationIsInvestigate || string.IsNullOrEmpty(nb.GetInspirationQuestion(t.id)))
            {
                statusText.text = t.inspirationIsInvestigate
                    ? "这是调查提示，请先寻找其他信息来源。"
                    : "当前没有可填入的采访问题。";
                return;
            }

            var q = nb.GetInspirationQuestion(t.id);
            bool inInterview = returnFromOverlay == Mode.Interview ||
                               (GameState.Instance != null &&
                                !string.IsNullOrEmpty(GameState.Instance.Data.uiMode) &&
                                GameState.Instance.Data.uiMode.StartsWith("interview"));
            if (inInterview && interviewInput != null)
            {
                CloseNotebook();
                if (interviewInput.gameObject.activeInHierarchy)
                {
                    interviewInput.text = q;
                    interviewInput.ActivateInputField();
                    interviewInput.caretPosition = interviewInput.text.Length;
                }
                if (statusText) statusText.text = "已填入提问灵感，可修改后发送";
            }
            else
            {
                GUIUtility.systemCopyBuffer = q;
                if (statusText) statusText.text = "提问灵感已复制，进入采访后可粘贴使用";
            }
        }
    }
}
