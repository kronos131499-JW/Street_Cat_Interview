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
        CanvasGroup dialogueFade;
        Transform buttonRoot;
        Transform choiceRoot;
        InputField inputField;
        GameObject titleRoot;
        Text titleBrand;
        Text titleSubtitle;
        Text titleTagline;

        // Menu / backlog
        GameObject menuRoot;
        GameObject backlogRoot;
        GameObject saveLoadRoot;
        Text backlogText;
        ScrollRect backlogScroll;
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

        readonly List<GameObject> spawnedButtons = new List<GameObject>();
        WritingDirection pendingDir = WritingDirection.GuardCatToday;
        readonly List<string> selectedMats = new List<string>();
        int phrasingA;
        int phrasingB;
        ArticleAssembler assembler = new ArticleAssembler();
        string lastInspectText;
        Font font;
        Coroutine fadeCo;
        Coroutine typewriterCo;
        Coroutine portraitFadeCo;
        Coroutine hintPulseCo;
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

        void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            BuildCanvas();
        }

        void Start()
        {
            SceneDirector.Instance.Bind(OnScriptLine, OnSceneEnd, ShowInvestigationMode, ShowTalkMenu);
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
            var lbBot = CreateImage(canvasGo.transform, "LetterboxBottom", VnTheme.Letterbox);
            Stretch(lbBot.rectTransform, new Vector2(0, 0), new Vector2(1, VnTheme.LetterboxH), Vector2.zero, Vector2.zero);

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

            // Stage / location — upper stage only, never overlaps dialogue
            locationText = CreateUiText(canvasGo.transform, "Location", 48, TextAnchor.MiddleCenter,
                new Color(1, 1, 1, 0.10f), Vector2.zero, new Vector2(1400, 80));
            var lrt = locationText.GetComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, VnTheme.StageCenterY);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            locationText.text = "此　间";

            stageHint = CreateUiText(canvasGo.transform, "StageHint", 20, TextAnchor.MiddleCenter,
                new Color(VnTheme.TextMuted.r, VnTheme.TextMuted.g, VnTheme.TextMuted.b, 0.45f),
                Vector2.zero, new Vector2(900, 36));
            var srt = stageHint.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, VnTheme.StageCenterY - 0.06f);

            // Decorative stage frame lines
            var stageLineL = CreateImage(canvasGo.transform, "StageLineL", VnTheme.AccentDim);
            Stretch(stageLineL.rectTransform, new Vector2(0.18f, 0.62f), new Vector2(0.32f, 0.62f), new Vector2(0, -1), new Vector2(0, 0));
            var stageLineR = CreateImage(canvasGo.transform, "StageLineR", VnTheme.AccentDim);
            Stretch(stageLineR.rectTransform, new Vector2(0.68f, 0.62f), new Vector2(0.82f, 0.62f), new Vector2(0, -1), new Vector2(0, 0));

            // Character portrait — above dialogue band to avoid clipping
            portraitImage = CreateImage(canvasGo.transform, "Portrait", Color.white);
            Stretch(portraitImage.rectTransform, new Vector2(0.70f, VnTheme.DialogueTop + 0.02f), new Vector2(0.98f, 0.88f),
                Vector2.zero, Vector2.zero);
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

            // Title hero layer
            titleRoot = new GameObject("TitleRoot", typeof(RectTransform));
            titleRoot.transform.SetParent(canvasGo.transform, false);
            StretchFull(titleRoot.GetComponent<RectTransform>());
            titleBrand = CreateUiText(titleRoot.transform, "Brand", 78, TextAnchor.MiddleCenter,
                VnTheme.TextPrimary, new Vector2(0, 60), new Vector2(1200, 110));
            titleBrand.text = "街角专访";
            titleBrand.fontStyle = FontStyle.Bold;
            var brt = titleBrand.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.60f);

            var titleRule = CreateImage(titleRoot.transform, "TitleRule", VnTheme.Accent);
            Stretch(titleRule.rectTransform, new Vector2(0.38f, 0.545f), new Vector2(0.62f, 0.545f), new Vector2(0, -1.5f), new Vector2(0, 0));

            titleSubtitle = CreateUiText(titleRoot.transform, "Sub", 26, TextAnchor.MiddleCenter,
                VnTheme.Accent, new Vector2(0, 0), new Vector2(1000, 50));
            titleSubtitle.text = "此间　·　社会观察专栏";
            var sbrt = titleSubtitle.GetComponent<RectTransform>();
            sbrt.anchorMin = sbrt.anchorMax = new Vector2(0.5f, 0.50f);

            titleTagline = CreateUiText(titleRoot.transform, "Tag", 18, TextAnchor.MiddleCenter,
                VnTheme.TextMuted, new Vector2(0, -40), new Vector2(900, 40));
            titleTagline.text = "第一章　　编外保安大福";
            var trt = titleTagline.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.44f);

            titleActionRoot = new GameObject("TitleActions", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            titleActionRoot.SetParent(titleRoot.transform, false);
            var tart = titleActionRoot.GetComponent<RectTransform>();
            tart.anchorMin = new Vector2(0.2f, 0.18f);
            tart.anchorMax = new Vector2(0.8f, 0.18f);
            tart.pivot = new Vector2(0.5f, 0.5f);
            tart.sizeDelta = new Vector2(0, 52);
            var tah = titleActionRoot.GetComponent<HorizontalLayoutGroup>();
            tah.spacing = 14;
            tah.childAlignment = TextAnchor.MiddleCenter;
            tah.childForceExpandWidth = false;
            tah.childControlWidth = true;

            // Dialogue box — fixed bottom zone (letterbox → DialogueTop)
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
            BuildSaveLoadOverlay(canvasGo.transform);
            BuildConfirmOverlay(canvasGo.transform);
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
            var shellEdge = CreateImage(shell.transform, "TopEdge", VnTheme.Accent);
            Stretch(shellEdge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), new Vector2(0, 0));

            var header = CreateImage(shell.transform, "Header", new Color(0.08f, 0.09f, 0.11f, 1f));
            Stretch(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(0, 0));

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
            Stretch(composer.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 168));
            var composerEdge = CreateImage(composer.transform, "Edge", VnTheme.DialogueEdge);
            Stretch(composerEdge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2), new Vector2(0, 0));

            interviewHintRoot = new GameObject("Hints", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            interviewHintRoot.SetParent(composer.transform, false);
            var hrt = interviewHintRoot.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1);
            hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1);
            hrt.anchoredPosition = new Vector2(0, -12);
            hrt.sizeDelta = new Vector2(-40, 40);
            var hintLayout = interviewHintRoot.GetComponent<HorizontalLayoutGroup>();
            hintLayout.spacing = 8;
            hintLayout.childAlignment = TextAnchor.MiddleLeft;
            hintLayout.childForceExpandWidth = false;
            hintLayout.childControlWidth = true;
            hintLayout.padding = new RectOffset(20, 20, 0, 0);

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
            sendGo.GetComponent<Button>().onClick.AddListener(SubmitInterviewQuestion);
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
            ahlg.padding = new RectOffset(20, 20, 0, 0);

            var logPanel = CreateImage(shell.transform, "LogPanel", new Color(0.06f, 0.07f, 0.09f, 1f));
            Stretch(logPanel.rectTransform, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(16, 180), new Vector2(-16, -84));

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
            go.GetComponent<Button>().onClick.AddListener(action);
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
            var parent = mode == Mode.Title && titleActionRoot != null ? titleActionRoot : buttonRoot;
            SpawnButton(parent, label, action, primary, mode == Mode.Title ? 150 : 118);
        }

        void AddInvestigateHotspot(string id, string title, bool inspected, UnityEngine.Events.UnityAction action)
        {
            if (investigateHotspotLayer == null) return;
            if (!InvestigateHotspotLayout.TryGet(id, "bg_huaian_community", out var rect))
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
            btn.onClick.AddListener(action);

            var trigger = go.AddComponent<EventTrigger>();
            void AddTrig(EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> cb)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(cb);
                trigger.triggers.Add(entry);
            }

            void SetHover(bool on)
            {
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
            if (!wide)
                btn.onClick.AddListener(() => { SfxController.Instance?.PlayUi(); action(); });
            else
                btn.onClick.AddListener(action);

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
            locationText.gameObject.SetActive(showLocation && !showTitle && mode != Mode.Investigate);
            stageHint.gameObject.SetActive(showLocation && !showTitle && mode != Mode.Investigate);
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
                choiceRoot.parent?.gameObject.SetActive(showChoices);
                choiceRoot.gameObject.SetActive(showChoices);
            }
            if (advanceCatcher != null && (!showDialogue || showTitle || mode == Mode.Investigate || mode == Mode.Interview))
                advanceCatcher.gameObject.SetActive(false);
            ApplyAtmosphere();
        }

        void SetInterviewChrome(bool on)
        {
            if (interviewRoot != null)
                interviewRoot.SetActive(on);
            if (investigateRoot != null)
                investigateRoot.SetActive(false);
            if (dialoguePanel != null)
                dialoguePanel.gameObject.SetActive(!on && mode != Mode.Title && mode != Mode.Investigate);
            if (locationText != null)
                locationText.gameObject.SetActive(!on && mode != Mode.Title && mode != Mode.Investigate);
            if (stageHint != null)
                stageHint.gameObject.SetActive(!on && mode != Mode.Title && mode != Mode.Investigate);
            if (buttonRoot != null)
                buttonRoot.gameObject.SetActive(!on);
            if (choiceRoot != null)
            {
                choiceRoot.parent?.gameObject.SetActive(!on);
                choiceRoot.gameObject.SetActive(!on);
            }
            if (inputField != null)
                inputField.gameObject.SetActive(false);
            ApplyAtmosphere();
        }

        void SetInvestigateChrome(bool on)
        {
            investigateHotspotsVisible = on;
            if (investigateRoot != null)
                investigateRoot.SetActive(on);
            if (interviewRoot != null)
                interviewRoot.SetActive(false);
            if (dialoguePanel != null)
                dialoguePanel.gameObject.SetActive(!on);
            if (locationText != null)
                locationText.gameObject.SetActive(false);
            if (stageHint != null)
                stageHint.gameObject.SetActive(false);
            if (buttonRoot != null)
                buttonRoot.gameObject.SetActive(!on);
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
            if (mode == Mode.Title)
                label = "Title";
            else if (mode == Mode.Interview)
            {
                var sceneLabel = SceneDirector.Instance?.Current?.backgroundLabel;
                label = !string.IsNullOrEmpty(sceneLabel) && sceneLabel.Contains("保安亭")
                    ? sceneLabel
                    : "采访";
            }
            else if (mode == Mode.Investigate)
                label = "槐安社区";
            else if (mode == Mode.Writing)
                label = "写稿";
            else if (mode == Mode.Notebook)
                label = "笔记";
            else if (mode == Mode.Epilogue)
                label = "后日谈";
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

        void SetPortrait(string portraitKey)
        {
            if (portraitImage == null) return;
            if (mode == Mode.Title || (mode == Mode.Investigate && investigateHotspotsVisible))
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

        void AddInterviewHint(string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Hint", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(interviewHintRoot, false);
            go.GetComponent<Image>().color = new Color(0.14f, 0.13f, 0.11f, 1f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 36;
            le.preferredHeight = 36;
            le.minWidth = 140;
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = VnTheme.ButtonHover;
            btn.colors = colors;
            btn.onClick.AddListener(action);
            var tick = CreateImage(go.transform, "Tick", VnTheme.Accent);
            var tr = tick.rectTransform;
            tr.anchorMin = new Vector2(0, 0.25f);
            tr.anchorMax = new Vector2(0, 0.75f);
            tr.sizeDelta = new Vector2(3, 0);
            tr.anchoredPosition = Vector2.zero;
            var tgo = new GameObject("L", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-10, 0));
            var tx = tgo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 16;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = VnTheme.TextPrimary;
            tx.text = label;
            tx.raycastTarget = false;
            interviewSpawned.Add(go);
        }

        void AddInterviewAction(string label, UnityEngine.Events.UnityAction action, bool primary = false)
        {
            var go = new GameObject("Act", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(interviewActionRoot, false);
            go.GetComponent<Image>().color = primary ? new Color(0.2f, 0.16f, 0.12f, 1f) : VnTheme.Button;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 36;
            le.preferredHeight = 36;
            le.minWidth = 110;
            go.GetComponent<Button>().onClick.AddListener(action);
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
            if (interviewInput == null || InterviewController.Instance == null)
                return;
            var q = interviewInput.text;
            interviewInput.text = "";
            var reply = InterviewController.Instance.Ask(q);
            if (!string.IsNullOrEmpty(q) && DialogueHistory.Instance != null)
            {
                DialogueHistory.Instance.Add("小凌", q, "interview");
                if (reply != null)
                {
                    if (!string.IsNullOrEmpty(reply.behavior))
                        DialogueHistory.Instance.Add("", "（" + reply.behavior + "）", "interview");
                    var who = InterviewController.Instance.Subject == InterviewSubject.Dafu ? "大福" : "林女士";
                    foreach (var line in reply.replyLines)
                        DialogueHistory.Instance.Add(who, line, "interview");
                }
            }
            RefreshInterviewView(reply?.systemHint);
        }

        void FormatInterviewLog(StringBuilder sb, string extra)
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
            if (!string.IsNullOrEmpty(extra))
            {
                sb.AppendLine();
                sb.Append("<color=#D4B56A>提示　").Append(extra).Append("</color>");
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
            if (scene != null && !string.IsNullOrEmpty(scene.backgroundLabel))
            {
                locationText.text = scene.backgroundLabel.Replace("_", "　");
                stageHint.text = scene.title;
            }
            ApplyAtmosphere();
            ApplyStageArt();
        }

        void SetSpeaker(string name, LineSpeaker kind)
        {
            if (string.IsNullOrEmpty(name) || kind == LineSpeaker.Narration)
            {
                namePlate.gameObject.SetActive(false);
                bodyText.color = VnTheme.TextMuted;
                lastHistorySpeaker = "";
                SetPortrait(null);
                return;
            }
            namePlate.gameObject.SetActive(true);
            if (kind == LineSpeaker.Inner)
            {
                nameText.text = name + " · 内心";
                bodyText.color = VnTheme.TextInner;
                lastHistorySpeaker = name + "（内心）";
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

            SetPortrait(VnArt.ResolvePortrait(name, kind));
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
                if (confirmRoot != null && confirmRoot.activeSelf)
                {
                    confirmRoot.SetActive(false);
                    pendingOverwriteSlot = -999;
                    return;
                }
                if (saveLoadRoot != null && saveLoadRoot.activeSelf) { CloseSaveLoad(); return; }
                if (mode == Mode.Backlog) { CloseBacklog(); return; }
                if (mode == Mode.Menu) { CloseMenu(); return; }
                if (mode == Mode.Notebook) { ResumeOverlayReturn(); return; }
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
                SfxController.Instance?.PlayUi();
                return;
            }

            if (!canClickAdvance)
                return;
            if (mode == Mode.Dialogue)
            {
                SfxController.Instance?.PlayAdvance();
                SceneDirector.Instance?.Advance();
            }
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
            SfxController.Instance?.PlayUi();
            SceneDirector.Instance?.SkipToBreak(RecordSkippedLine);
        }

        void RecordSkippedLine(ScriptLine line)
        {
            if (line == null || DialogueHistory.Instance == null)
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
                speaker = (string.IsNullOrEmpty(line.speakerName) ? "小凌" : line.speakerName) + "（内心）";
                kind = "inner";
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
            if (dialogueClick != null)
                dialogueClick.interactable = canClickAdvance || typewriterRunning;
            if (advanceCatcher != null)
            {
                bool showCatcher = mode == Mode.Dialogue && (canClickAdvance || typewriterRunning) && !hasChoices;
                advanceCatcher.gameObject.SetActive(showCatcher);
                var btn = advanceCatcher.GetComponent<Button>();
                if (btn != null) btn.interactable = showCatcher;
            }
            if (choiceHostImage != null && !hasChoices && (spawnedButtons == null || !HasWideChoices()))
            {
                // keep host only when choices present — AddChoice turns it on
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
            bool show = mode == Mode.Dialogue && !waitingForChoice && (canClickAdvance || typewriterRunning);
            clickHintText.gameObject.SetActive(show);
            if (!show) return;
            clickHintText.text = typewriterRunning ? "点击显示全文" : "点击继续　长按Ctrl跳过";
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
            canClickAdvance = false;
            waitingForChoice = false;
            inputField.gameObject.SetActive(false);
            if (menuRoot) menuRoot.SetActive(false);
            if (backlogRoot) backlogRoot.SetActive(false);
            if (saveLoadRoot) saveLoadRoot.SetActive(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(false, true, false);
            ClearButtons();
            statusText.text = "";
            ApplyAtmosphere();
            ApplyStageArt();
            SetPortrait(null);
            AddAction("新游戏", () => ChapterFlowController.Instance.StartNewGame(), true);
            AddAction("读取自动档", () => ChapterFlowController.Instance.ContinueOrNew());
            AddAction("读档", () => OpenSaveLoad(false));
            AddAction("清除存档", () =>
            {
                SaveSystem.Delete();
                titleTagline.text = "存档已清除";
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
            RefreshHeader();
            ClearButtons();
            AddAction("跳过", TrySkipDialogue);
            AddAction("回看", OpenBacklog);
            AddAction("笔记", OpenNotebook);
            AddAction("菜单", OpenMenu);
            SetAdvanceEnabled(true);
            statusText.text = "点击继续　·　Ctrl / 跳过";
        }

        void OnScriptLine(ScriptLine line)
        {
            mode = Mode.Dialogue;
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            RefreshHeader();
            ApplyStageArt();
            inputField.gameObject.SetActive(false);

            var speaker = line.speakerName;
            if (line.speaker == LineSpeaker.Narration) speaker = "";
            if (line.speaker == LineSpeaker.System) speaker = "系统";
            SetSpeaker(speaker, line.speaker);
            var kind = line.speaker == LineSpeaker.Inner ? "inner"
                : line.speaker == LineSpeaker.System ? "system" : "dialogue";
            SetBody(line.text, true, kind);

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
            if (mode != Mode.Menu && mode != Mode.Backlog)
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
            if (mode != Mode.Menu && mode != Mode.Backlog)
            {
                returnFromOverlay = mode;
                savedWaitingForChoice = waitingForChoice;
            }
            mode = Mode.Backlog;
            SetAdvanceEnabled(false);
            if (menuRoot) menuRoot.SetActive(false);
            backlogRoot.SetActive(true);
            var hist = DialogueHistory.Instance != null ? DialogueHistory.Instance.BuildPlainText() : "";
            backlogText.text = string.IsNullOrEmpty(hist) ? "（还没有可回看的对话）" : hist;
            Canvas.ForceUpdateCanvases();
            backlogScroll.verticalNormalizedPosition = 0f;
        }

        void CloseBacklog()
        {
            if (backlogRoot) backlogRoot.SetActive(false);
            ResumeOverlayReturn();
        }

        void ResumeOverlayReturn()
        {
            waitingForChoice = savedWaitingForChoice;
            switch (returnFromOverlay)
            {
                case Mode.Dialogue:
                    mode = Mode.Dialogue;
                    SetAdvanceEnabled(!waitingForChoice, waitingForChoice);
                    statusText.text = waitingForChoice ? "做出选择" : "点击继续　·　Ctrl / 跳过";
                    break;
                case Mode.Investigate: ShowInvestigationMode(); break;
                case Mode.Talk: ShowTalkMenu(); break;
                case Mode.Interview: RefreshInterviewView(); break;
                case Mode.Writing: ShowWritingDirectionPick(); break;
                case Mode.Epilogue: ShowEpilogue(); break;
                case Mode.Notebook: OpenNotebook(); break;
                default:
                    mode = Mode.Dialogue;
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

            ClearButtons();
            var service = InvestigationService.Instance;
            foreach (var h in service.Hotspots)
            {
                var id = h.id;
                var title = h.title;
                var inspected = h.inspected;
                AddInvestigateHotspot(id, title, inspected, () => ShowHotspotInspect(id));
            }

            if (GameState.Instance.HasFlag(FlagIds.GuardUnlocked) || GameState.Instance.Data.currentSceneId == SceneIds.SC05)
                AddInvestigateAction("与保安交谈", () =>
                {
                    SetInvestigateChrome(false);
                    GameState.Instance.SetScene(SceneIds.SC05);
                    SceneDirector.Instance.PlayScene(SceneIds.SC05);
                    ShowDialogueMode();
                }, true);
            if (service.CanWaitForDafu())
                AddInvestigateAction("等待大福", () =>
                {
                    SetInvestigateChrome(false);
                    ChapterFlowController.Instance.GoToScene(SceneIds.SC06);
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

        void ShowHotspotInspect(string hotspotId)
        {
            var service = InvestigationService.Instance;
            lastInspectText = service.Inspect(hotspotId);
            SfxController.Instance?.PlayInspect();

            mode = Mode.Investigate;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "investigate";
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            locationText.text = "槐安社区";
            RefreshHeader();
            ApplyAtmosphere();

            SetSpeaker("小凌", LineSpeaker.Inner);
            SetBody(lastInspectText, true, "investigate");
            statusText.text = "调查结果";
            ClearButtons();
            AddAction("返回调查", ShowInvestigationMode, true);
            AddAction("笔记", OpenNotebook);
            AddAction("菜单", OpenMenu);
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
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            locationText.text = "保安亭";
            stageHint.text = "交谈";
            RefreshHeader();
            SetSpeaker("系统", LineSpeaker.System);
            SetBody("想向保安叔叔了解什么？");
            statusText.text = "选择一个话题";
            ClearButtons();
            foreach (var topic in InvestigationService.Instance.GuardTopics)
            {
                var t = topic;
                AddChoice(t.label, () =>
                {
                    var reply = InvestigationService.Instance.Talk(t);
                    SetSpeaker("保安叔叔", LineSpeaker.Character);
                    SetBody(reply, true, "talk");
                    statusText.text = "";
                    ClearButtons();
                    AddAction("返回话题", ShowTalkMenu);
                    AddAction("结束交谈", () =>
                    {
                        if (InvestigationService.Instance.CanWaitForDafu())
                            GameState.Instance.SetObjective("等待大福出现。");
                        ShowInvestigationMode();
                    }, true);
                    AddAction("回看", OpenBacklog);
                    AddAction("菜单", OpenMenu);
                });
            }
            AddAction("结束交谈", () => ShowInvestigationMode(), true);
            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
        }

        void ShowPostInterviewTalk()
        {
            mode = Mode.Talk;
            SetAdvanceEnabled(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            locationText.text = "保安亭";
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
                    SetSpeaker("保安叔叔", LineSpeaker.Character);
                    SetBody(reply);
                    ClearButtons();
                    if (!string.IsNullOrEmpty(t.nextSceneId))
                    {
                        AddAction("等待回复", () =>
                        {
                            SetSpeaker("林女士", LineSpeaker.Character);
                            SetBody("你好，我是林敏。保安和我说了。明天下午我会去社区，你到保安亭附近等我吧。");
                            ClearButtons();
                            AddAction("前往采访", () => ChapterFlowController.Instance.GoToScene(SceneIds.SC09), true);
                        }, true);
                    }
                    else AddAction("返回", ShowPostInterviewTalk);
                    AddAction("笔记", OpenNotebook);
                });
            }
            AddAction("返回调查", ShowInvestigationMode);
        }

        public void ShowInterview(InterviewSubject subject, bool returnToWritingAfter = false)
        {
            mode = Mode.Interview;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = subject == InterviewSubject.Lin ? "interview_lin" : "interview_dafu";
            SetAdvanceEnabled(false);
            SaveSystem.Autosave();
            InterviewController.Instance.Begin(subject, returnToWritingAfter);

            SetChrome(false, false, false);
            SetInterviewChrome(true);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            RefreshHeader();

            interviewSubjectText.text = subject == InterviewSubject.Dafu
                ? (returnToWritingAfter ? "补充采访　·　大福" : "喵语翻译器　·　采访大福")
                : (returnToWritingAfter ? "补充采访　·　林女士" : "自由采访　·　林女士");
            interviewInput.gameObject.SetActive(true);
            interviewInput.text = "";
            interviewInput.placeholder.GetComponent<Text>().text =
                subject == InterviewSubject.Dafu ? "想问大福什么？" : "想问林女士什么？";

            ClearButtons();
            RefreshInterviewView(returnToWritingAfter
                ? "补充采访开始。已有情报与素材会保留。"
                : "采访开始。可以直接输入问题，或点下方提示填入。");
        }

        void RefreshInterviewView(string extra = null)
        {
            if (mode != Mode.Interview)
                mode = Mode.Interview;
            SetInterviewChrome(true);
            RefreshHeader();

            var who = InterviewController.Instance.Subject == InterviewSubject.Dafu ? "大福" : "林女士";
            interviewSubjectText.text = InterviewController.Instance.Subject == InterviewSubject.Dafu
                ? "喵语翻译器　·　采访大福"
                : "自由采访　·　林女士";

            var sb = new StringBuilder();
            FormatInterviewLog(sb, extra);
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

            if (InterviewController.Instance.Subject == InterviewSubject.Dafu)
            {
                AddInterviewHint("现在的生活", () => interviewInput.text = "你平时都在哪里活动？");
                AddInterviewHint("过去的伤", () => interviewInput.text = "你的脖子以前疼吗？");
                AddInterviewHint("认识的人", () => interviewInput.text = "有没有人连续很多天给你带吃的？");
                AddInterviewHint("被带走", () => interviewInput.text = "后来有人把你抓走了吗？");
                AddInterviewHint("送回社区", () => interviewInput.text = "最后是谁把你送回社区的？");
            }
            else
            {
                AddInterviewHint("第一次发现", () => interviewInput.text = "您第一次是怎么发现大福的？");
                AddInterviewHint("连续投喂", () => interviewInput.text = "您是怎么连续投喂它的？");
                AddInterviewHint("抓捕送医", () => interviewInput.text = "后来怎么抓捕送医的？");
                AddInterviewHint("治疗经过", () => interviewInput.text = "住院期间还发生了什么？");
                AddInterviewHint("为什么没收养", () => interviewInput.text = "为什么没有收养大福？");
            }

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
        }

        void TryEndInterview()
        {
            if (!InterviewController.Instance.CanComplete())
            {
                var msg = "现在结束的话，似乎还有不少事情没有问清楚。\n\n" + InterviewController.Instance.MissingSummary();
                interviewLogText.text = interviewLogText.text + "\n\n<color=#A8C0D4>小凌（内心）　" + msg.Replace("\n", "\n") + "</color>";
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
            locationText.text = "此间杂志社";
            stageHint.text = "写稿";
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
            SetSpeaker("沈禾", LineSpeaker.Character);
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
            SetSpeaker("沈禾", LineSpeaker.Character);
            SetBody("最后确认两处关键表述，再生成初稿。");
            ClearButtons();
            AddChoice("放归：确认照料后的决定", () => phrasingA = 0);
            AddChoice("放归：中性——未进入收养", () => phrasingA = 1);
            AddChoice("麻绳：无法确认来源", () => phrasingB = 0);
            AddChoice("麻绳：伤势确认、成因未知", () => phrasingB = 1);
            AddAction("生成文章", GenerateArticle, true);
            AddAction("返回改选材", ShowMaterialPick);
            AddReInterviewActions(false);
        }

        void GenerateArticle()
        {
            if (!assembler.CanAssemble(pendingDir, selectedMats, out var err))
            {
                SetSpeaker("沈禾", LineSpeaker.Character);
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
            SetSpeaker("沈禾", LineSpeaker.Character);
            SetBody(assembler.Body + "\n\n—— 主编审核 ——\n" + assembler.ReviewText);
            statusText.text = $"评分 {assembler.Score}";
            ClearButtons();
            if (assembler.CanPublish)
                AddAction("发布报道", () => ChapterFlowController.Instance.OnArticlePublished(), true);
            else
            {
                AddAction("返回改选材", ShowMaterialPick, true);
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
            locationText.text = "几天后";
            stageHint.text = "后日谈";
            RefreshHeader();
            var dir = (WritingDirection)Mathf.Max(0, GameState.Instance.Data.writingDirection);
            var sb = new StringBuilder();
            sb.AppendLine("文章已发布：" + GameState.Instance.Data.lastArticleTitle);
            sb.AppendLine();
            sb.AppendLine("文章发布以后，有不少人第一次知道，大福以前受过那么严重的伤。");
            sb.AppendLine();
            if (dir == WritingDirection.GuardCatToday)
            {
                sb.AppendLine("偶尔会有人来问，大福今天有没有上班。");
                sb.AppendLine("但大福并不知道自己成了报道里的主角。");
                sb.AppendLine("下午四点多，它又来了。");
                sb.AppendLine();
                sb.AppendLine("大福今天也在上班。");
            }
            else
            {
                sb.AppendLine("还是有人问，林女士为什么没有把大福带回家。");
                sb.AppendLine("也有人说，第一次知道一场救助并不一定要以收养结束。");
                sb.AppendLine();
                sb.AppendLine("救下一只猫以后，故事并不会立刻结束。");
            }
            sb.AppendLine();
            sb.AppendLine("报道能记录的，只是它生活里很短的一段。");
            sb.AppendLine("它的日子还在继续。");
            SetSpeaker("小凌", LineSpeaker.Inner);
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
            inputField.gameObject.SetActive(false);
            if (menuRoot) menuRoot.SetActive(false);
            if (backlogRoot) backlogRoot.SetActive(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SetChrome(true, false, true);
            locationText.text = "记者笔记";
            stageHint.text = "已确认 / 待确认";
            RefreshHeader();
            SetSpeaker("笔记", LineSpeaker.System);
            SetBody(ReporterNotebook.Instance.BuildSummary(), false);
            statusText.text = "";
            ClearButtons();
            if (returnFromOverlay == Mode.Writing ||
                (GameState.Instance != null && GameState.Instance.Data.uiMode == "writing"))
                AddReInterviewActions(GameState.Instance.Data.unlockedMaterials.Count < 8);
            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
            AddAction("返回", () =>
            {
                ResumeOverlayReturn();
            }, true);
        }
    }
}
