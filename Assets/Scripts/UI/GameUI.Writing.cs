using System.Collections.Generic;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Loc;
using StreetCat.Narrative;
using StreetCat.Writing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StreetCat.UI
{
    /// <summary>
    /// Corkboard scrapbook UI for writing materials (素材卡库) + writing-desk flow.
    /// </summary>
    public partial class GameUI
    {
        static readonly Color WmFrame = new Color(0.14f, 0.12f, 0.11f, 1f);
        static readonly Color WmCorkA = new Color(0.42f, 0.30f, 0.20f, 1f);
        static readonly Color WmCorkB = new Color(0.52f, 0.38f, 0.26f, 1f);
        static readonly Color WmInk = new Color(0.16f, 0.13f, 0.10f, 1f);
        static readonly Color WmInkMuted = new Color(0.38f, 0.32f, 0.26f, 1f);
        static readonly Color WmPaper = new Color(0.96f, 0.93f, 0.86f, 1f);
        static readonly Color WmStrip = new Color(0.95f, 0.90f, 0.78f, 1f);
        static readonly Color WmOrange = new Color(0.83f, 0.36f, 0.18f, 1f);
        static readonly Color WmTeal = new Color(0.18f, 0.31f, 0.35f, 1f);
        static readonly Color WmRedBar = new Color(0.78f, 0.22f, 0.18f, 1f);
        static readonly Color WmFact = new Color(0.72f, 0.84f, 0.68f, 1f);
        static readonly Color WmDetail = new Color(0.92f, 0.84f, 0.48f, 1f);
        static readonly Color WmEmotion = new Color(0.78f, 0.72f, 0.86f, 1f);
        static readonly Color WmLocked = new Color(0.62f, 0.60f, 0.56f, 1f);
        static readonly Color WmPeach = new Color(0.92f, 0.70f, 0.58f, 1f);

        static readonly string[] WmParagraphKeys =
        {
            "ui.writing.para_01", "ui.writing.para_02", "ui.writing.para_03", "ui.writing.para_04"
        };

        static readonly string[] WmParagraphFallback =
        {
            "段落 01  现在的大福",
            "段落 02  受伤与救助",
            "段落 03  治疗与抉择",
            "段落 04  回到社区"
        };

        GameObject writingMatsRoot;
        GameObject writingPreviewRoot;
        TextMeshProUGUI writingTapeTitle;
        TextMeshProUGUI writingSelectedCountText;
        Transform writingProgressDots;
        Transform writingParagraphList;
        Transform writingCardGrid;
        ScrollRect writingCardScroll;
        TextMeshProUGUI writingDetailTitle;
        TextMeshProUGUI writingDetailTag;
        TextMeshProUGUI writingDetailSource;
        TextMeshProUGUI writingDetailBody;
        Image writingDetailTagBg;
        TextMeshProUGUI writingPreviewBody;
        TextMeshProUGUI writingStatusHint;
        Button writingGoBtn;
        Button writingReInterviewBtn;
        readonly List<GameObject> writingSpawned = new List<GameObject>();
        readonly List<Image> writingDotImages = new List<Image>();
        Sprite writingCorkSprite;
        int writingFocusParagraph;
        string writingFocusMatId;
        bool writingMatsActive;
        const int WritingMaxSelect = 10;
        /// <summary>Soft floor for UI hints; real gate is four paragraphs each covered (see ArticleAssembler.CanAssemble).</summary>
        const int WritingMinSelect = 4;

        void BuildWritingMaterialsOverlay(Transform parent)
        {
            writingMatsRoot = new GameObject("WritingMaterialsOverlay", typeof(RectTransform));
            writingMatsRoot.transform.SetParent(parent, false);
            StretchFull(writingMatsRoot.GetComponent<RectTransform>());

            var frame = CreateImage(writingMatsRoot.transform, "Frame", WmFrame);
            Stretch(frame.rectTransform, new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.97f),
                Vector2.zero, Vector2.zero);

            var cork = CreateImage(frame.transform, "Cork", WmCorkA);
            Stretch(cork.rectTransform, new Vector2(0.012f, 0.018f), new Vector2(0.988f, 0.982f),
                Vector2.zero, Vector2.zero);
            ApplyWritingCorkTexture(cork);

            // Top-left tape title (axis-aligned — rotation blurs UI Text with pixelPerfect canvas)
            var tapeHost = new GameObject("TapeTitle", typeof(RectTransform));
            tapeHost.transform.SetParent(cork.transform, false);
            Stretch(tapeHost.GetComponent<RectTransform>(), new Vector2(0.02f, 0.90f), new Vector2(0.38f, 0.99f),
                Vector2.zero, Vector2.zero);

            var tapeImg = CreateImage(tapeHost.transform, "Tape", new Color(0.90f, 0.82f, 0.62f, 0.95f));
            StretchFull(tapeImg.rectTransform);
            var tapeSpr = VnArt.GetTitle("btn_tape_idle");
            if (tapeSpr != null)
            {
                tapeImg.sprite = tapeSpr;
                tapeImg.preserveAspect = false;
                tapeImg.type = Image.Type.Simple;
                tapeImg.color = Color.white;
            }

            writingTapeTitle = CreateUiText(tapeHost.transform, "Label", 24, TextAnchor.MiddleCenter,
                WmInk, Vector2.zero, Vector2.zero);
            Stretch(writingTapeTitle.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(18f, 4f), new Vector2(-18f, -4f));
            writingTapeTitle.fontStyle = FontStyles.Bold;
            writingTapeTitle.enableWordWrapping = false;
            writingTapeTitle.overflowMode = TextOverflowModes.Overflow;
            writingTapeTitle.text = UiLoc.T("ui.writing.tape_title", "第一章 写稿 / 素材卡库");

            // Top-right selected count + dots
            var topRight = new GameObject("SelectedHeader", typeof(RectTransform));
            topRight.transform.SetParent(cork.transform, false);
            Stretch(topRight.GetComponent<RectTransform>(), new Vector2(0.58f, 0.90f), new Vector2(0.98f, 0.985f),
                Vector2.zero, Vector2.zero);

            writingSelectedCountText = CreateUiText(topRight.transform, "Count", 22, TextAnchor.MiddleRight,
                WmInk, Vector2.zero, Vector2.zero);
            Stretch(writingSelectedCountText.rectTransform, new Vector2(0f, 0.35f), new Vector2(0.42f, 1f),
                Vector2.zero, Vector2.zero);
            writingSelectedCountText.fontStyle = FontStyles.Bold;

            writingProgressDots = new GameObject("Dots", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            writingProgressDots.SetParent(topRight.transform, false);
            Stretch(writingProgressDots.GetComponent<RectTransform>(), new Vector2(0.44f, 0.15f), new Vector2(1f, 0.95f),
                Vector2.zero, Vector2.zero);
            var dh = writingProgressDots.GetComponent<HorizontalLayoutGroup>();
            dh.spacing = 6f;
            dh.childAlignment = TextAnchor.MiddleRight;
            dh.childForceExpandWidth = false;
            dh.childForceExpandHeight = false;
            dh.childControlWidth = false;
            dh.childControlHeight = false;
            writingDotImages.Clear();
            for (int i = 0; i < WritingMaxSelect; i++)
            {
                var dot = CreateImage(writingProgressDots, "Dot" + i, new Color(0.55f, 0.52f, 0.48f, 1f));
                var drt = dot.rectTransform;
                drt.sizeDelta = new Vector2(14f, 14f);
                writingDotImages.Add(dot);
            }

            // Left paragraph strip
            var stripShadow = CreateImage(cork.transform, "StripShadow", new Color(0f, 0f, 0f, 0.28f));
            Stretch(stripShadow.rectTransform, new Vector2(0.025f, 0.12f), new Vector2(0.23f, 0.88f),
                Vector2.zero, Vector2.zero);
            stripShadow.rectTransform.anchoredPosition = new Vector2(4f, -5f);
            stripShadow.raycastTarget = false;

            var strip = CreateImage(cork.transform, "ParagraphStrip", WmStrip);
            Stretch(strip.rectTransform, new Vector2(0.02f, 0.13f), new Vector2(0.225f, 0.885f),
                Vector2.zero, Vector2.zero);
            EnsureLinedPaperSprite();
            if (notebookLinedPaperSprite != null)
            {
                strip.sprite = notebookLinedPaperSprite;
                strip.type = Image.Type.Tiled;
                strip.color = Color.white;
            }

            var stripPin = CreateImage(strip.transform, "Pin", WmOrange);
            var pinRt = stripPin.rectTransform;
            pinRt.anchorMin = pinRt.anchorMax = new Vector2(0.5f, 1f);
            pinRt.pivot = new Vector2(0.5f, 0.5f);
            pinRt.anchoredPosition = new Vector2(0f, 6f);
            pinRt.sizeDelta = new Vector2(12f, 12f);
            stripPin.raycastTarget = false;

            var stripTitle = CreateUiText(strip.transform, "StripTitle", 17, TextAnchor.UpperLeft,
                WmInkMuted, Vector2.zero, Vector2.zero);
            Stretch(stripTitle.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.95f, 0.97f),
                Vector2.zero, Vector2.zero);
            stripTitle.fontStyle = FontStyles.Bold;
            stripTitle.text = UiLoc.T("ui.writing.structure", "文章结构（固定段落）");
            var stripTitleTag = stripTitle.gameObject.AddComponent<LocTag>();
            stripTitleTag.key = "ui.writing.structure";
            stripTitleTag.target = stripTitle;

            writingParagraphList = new GameObject("ParagraphList", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            writingParagraphList.SetParent(strip.transform, false);
            Stretch(writingParagraphList.GetComponent<RectTransform>(), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.84f),
                Vector2.zero, Vector2.zero);
            var pv = writingParagraphList.GetComponent<VerticalLayoutGroup>();
            pv.spacing = 6f;
            pv.childForceExpandHeight = true;
            pv.childForceExpandWidth = true;
            pv.childControlHeight = true;
            pv.childControlWidth = true;
            pv.padding = new RectOffset(4, 4, 4, 4);

            for (int i = 0; i < 4; i++)
                SpawnWritingParagraphRow(i);

            // Center card grid
            var gridHost = new GameObject("CardGridHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            gridHost.transform.SetParent(cork.transform, false);
            Stretch(gridHost.GetComponent<RectTransform>(), new Vector2(0.24f, 0.16f), new Vector2(0.68f, 0.88f),
                Vector2.zero, Vector2.zero);
            gridHost.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            writingCardScroll = gridHost.GetComponent<ScrollRect>();
            writingCardScroll.horizontal = false;
            writingCardScroll.movementType = ScrollRect.MovementType.Clamped;
            writingCardScroll.scrollSensitivity = 32f;

            var gridVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            gridVp.transform.SetParent(gridHost.transform, false);
            StretchFull(gridVp.GetComponent<RectTransform>());
            gridVp.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);

            var gridContent = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridContent.transform.SetParent(gridVp.transform, false);
            writingCardGrid = gridContent.transform;
            var gcrt = gridContent.GetComponent<RectTransform>();
            gcrt.anchorMin = new Vector2(0, 1);
            gcrt.anchorMax = new Vector2(1, 1);
            gcrt.pivot = new Vector2(0.5f, 1);
            gcrt.sizeDelta = Vector2.zero;
            var grid = gridContent.GetComponent<GridLayoutGroup>();
            // Wider cells — previous 168×148 forced 12–15px type that looked tiny/blurry.
            grid.cellSize = new Vector2(248f, 210f);
            grid.spacing = new Vector2(14f, 16f);
            grid.padding = new RectOffset(8, 8, 10, 10);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            gridContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            writingCardScroll.viewport = gridVp.GetComponent<RectTransform>();
            writingCardScroll.content = gcrt;

            // Right detail paper
            var detailShadow = CreateImage(cork.transform, "DetailShadow", new Color(0f, 0f, 0f, 0.32f));
            Stretch(detailShadow.rectTransform, new Vector2(0.695f, 0.16f), new Vector2(0.975f, 0.88f),
                Vector2.zero, Vector2.zero);
            detailShadow.rectTransform.anchoredPosition = new Vector2(5f, -6f);
            detailShadow.raycastTarget = false;

            var detail = CreateImage(cork.transform, "DetailPaper", WmPaper);
            Stretch(detail.rectTransform, new Vector2(0.69f, 0.17f), new Vector2(0.97f, 0.885f),
                Vector2.zero, Vector2.zero);

            var clip = CreateImage(detail.transform, "Paperclip", Color.white);
            var clipRt = clip.rectTransform;
            clipRt.anchorMin = clipRt.anchorMax = new Vector2(0.08f, 1f);
            clipRt.pivot = new Vector2(0.5f, 0.85f);
            clipRt.anchoredPosition = new Vector2(0f, 10f);
            clipRt.sizeDelta = new Vector2(40f, 58f);
            var clipSpr = VnArt.GetTitle("deco_paperclip");
            if (clipSpr != null)
            {
                clip.sprite = clipSpr;
                clip.preserveAspect = true;
            }
            else
            {
                clip.color = new Color(0.72f, 0.74f, 0.78f, 0.95f);
            }
            clip.raycastTarget = false;

            writingDetailTitle = CreateUiText(detail.transform, "Title", 26, TextAnchor.UpperLeft,
                WmInk, Vector2.zero, Vector2.zero);
            Stretch(writingDetailTitle.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f),
                Vector2.zero, Vector2.zero);
            writingDetailTitle.fontStyle = FontStyles.Bold;
            writingDetailTitle.overflowMode = TextOverflowModes.Overflow;
            writingDetailTitle.enableAutoSizing = false;

            writingDetailTagBg = CreateImage(detail.transform, "Tag", WmOrange);
            Stretch(writingDetailTagBg.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.46f, 0.76f),
                Vector2.zero, Vector2.zero);
            writingDetailTag = CreateUiText(writingDetailTagBg.transform, "TagLabel", 16, TextAnchor.MiddleCenter,
                Color.white, Vector2.zero, Vector2.zero);
            StretchFull(writingDetailTag.rectTransform);
            writingDetailTag.fontStyle = FontStyles.Bold;
            writingDetailTag.enableWordWrapping = false;
            writingDetailTag.enableAutoSizing = false;

            writingDetailSource = CreateUiText(detail.transform, "Source", 16, TextAnchor.MiddleLeft,
                WmInkMuted, Vector2.zero, Vector2.zero);
            Stretch(writingDetailSource.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.67f),
                Vector2.zero, Vector2.zero);
            writingDetailSource.enableAutoSizing = false;

            var detailHost = new GameObject("DetailBodyHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            detailHost.transform.SetParent(detail.transform, false);
            Stretch(detailHost.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.56f),
                Vector2.zero, Vector2.zero);
            detailHost.GetComponent<Image>().color = new Color(1, 1, 1, 0.001f);
            var detailScroll = detailHost.GetComponent<ScrollRect>();
            detailScroll.horizontal = false;
            detailScroll.movementType = ScrollRect.MovementType.Clamped;

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
            writingDetailBody = dContent.AddComponent<TextMeshProUGUI>();
            writingDetailBody.font = font;
            writingDetailBody.fontSize = 20;
            writingDetailBody.color = WmInk;
            writingDetailBody.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
            writingDetailBody.enableWordWrapping = true;
            writingDetailBody.overflowMode = TextOverflowModes.Overflow;
            writingDetailBody.lineSpacing = 45f;
            writingDetailBody.enableAutoSizing = false;
            writingDetailBody.raycastTarget = false;
            detailScroll.viewport = dVp.GetComponent<RectTransform>();
            detailScroll.content = dcrt;

            var doodle = CreateImage(detail.transform, "CatDoodle", new Color(0.45f, 0.40f, 0.36f, 0.45f));
            var doodleRt = doodle.rectTransform;
            doodleRt.anchorMin = doodleRt.anchorMax = new Vector2(1f, 0f);
            doodleRt.pivot = new Vector2(1f, 0f);
            doodleRt.anchoredPosition = new Vector2(-10f, 10f);
            doodleRt.sizeDelta = new Vector2(56f, 56f);
            var doodleSpr = VnArt.GetPortrait("ch_dafu_relaxed");
            if (doodleSpr != null)
            {
                doodle.sprite = doodleSpr;
                doodle.preserveAspect = true;
                doodle.color = new Color(1f, 1f, 1f, 0.55f);
            }
            doodle.raycastTarget = false;

            // Status hint (above bottom action row so it does not cover buttons)
            writingStatusHint = CreateUiText(cork.transform, "StatusHint", 16, TextAnchor.MiddleLeft,
                new Color(0.95f, 0.90f, 0.82f, 0.92f), Vector2.zero, Vector2.zero);
            Stretch(writingStatusHint.rectTransform, new Vector2(0.03f, 0.105f), new Vector2(0.68f, 0.145f),
                Vector2.zero, Vector2.zero);

            // Bottom-right actions
            writingGoBtn = SpawnWritingActionButton(cork.transform, "GoWriteBtn",
                UiLoc.T("ui.writing.go_write", "前往写稿"), WmOrange, new Vector2(0.72f, 0.025f), new Vector2(0.97f, 0.11f),
                OnWritingGoToDesk);
            var goTag = writingGoBtn.gameObject.AddComponent<LocTag>();
            goTag.key = "ui.writing.go_write";
            goTag.target = writingGoBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Secondary: back + notebook
            var backBtn = SpawnWritingActionButton(cork.transform, "BackDirBtn",
                UiLoc.T("ui.writing.back_direction", "返回立意"), new Color(0.28f, 0.24f, 0.20f, 0.92f),
                new Vector2(0.02f, 0.025f), new Vector2(0.14f, 0.10f),
                () => { writingMatsActive = false; HideWritingMaterialsBoard(); ShowWritingDirectionPick(); });
            var backTag = backBtn.gameObject.AddComponent<LocTag>();
            backTag.key = "ui.writing.back_direction";
            backTag.target = backBtn.GetComponentInChildren<TextMeshProUGUI>();

            var nbBtn = SpawnWritingActionButton(cork.transform, "NotebookBtn",
                UiLoc.T("ui.notebook", "笔记"), new Color(0.28f, 0.24f, 0.20f, 0.92f),
                new Vector2(0.15f, 0.025f), new Vector2(0.25f, 0.10f), OpenNotebook);

            writingReInterviewBtn = SpawnWritingActionButton(cork.transform, "ReInterviewBtn",
                UiLoc.T("ui.writing.reinterview", "返回采访"), new Color(0.32f, 0.22f, 0.18f, 0.95f),
                new Vector2(0.26f, 0.025f), new Vector2(0.40f, 0.10f), ShowReInterviewMenu);
            var riTag = writingReInterviewBtn.gameObject.AddComponent<LocTag>();
            riTag.key = "ui.writing.reinterview";
            riTag.target = writingReInterviewBtn.GetComponentInChildren<TextMeshProUGUI>();

            // ArticlePreview overlay kept in code but unwired — players edit on the writing desk.
            BuildWritingPreviewPanel(cork.transform);

            writingMatsRoot.SetActive(false);
        }

        void ApplyWritingFonts()
        {
            if (font == null) return;
            if (writingMatsRoot == null && writingDeskRoot == null) return;
            float scale = GameSettings.FontSizeScale;
            void Chrome(TextMeshProUGUI t, int baseSize, bool bold = false, bool wrap = false)
            {
                if (t == null) return;
                t.font = font;
                t.fontSize = Mathf.RoundToInt(baseSize * scale);
                if (bold) t.fontStyle = FontStyles.Bold;
                t.enableAutoSizing = false;
                if (wrap)
                {
                    t.enableWordWrapping = true;
                    ApplyLetterSpacing(t, 0f);
                }
                else
                {
                    // Chrome labels stay overflow + zero tracking (scrapbook readability).
                    ApplyLetterSpacing(t, 0f);
                }
            }

            Chrome(writingTapeTitle, 24, true);
            Chrome(writingSelectedCountText, 22, true);
            Chrome(writingDetailTitle, 26, true, wrap: true);
            Chrome(writingDetailTag, 16, true);
            Chrome(writingDetailSource, 16);
            Chrome(writingStatusHint, 16, wrap: true);

            if (writingDetailBody != null)
            {
                writingDetailBody.font = font;
                writingDetailBody.fontSize = Mathf.RoundToInt(20f * scale);
                writingDetailBody.lineSpacing = 45f;
                writingDetailBody.enableWordWrapping = true;
                writingDetailBody.overflowMode = TextOverflowModes.Overflow;
                writingDetailBody.enableAutoSizing = false;
                ApplyLetterSpacing(writingDetailBody, 0f);
            }
            if (writingPreviewBody != null)
            {
                writingPreviewBody.font = font;
                writingPreviewBody.fontSize = Mathf.RoundToInt(20f * scale);
                writingPreviewBody.lineSpacing = 45f;
                writingPreviewBody.enableWordWrapping = true;
                writingPreviewBody.overflowMode = TextOverflowModes.Overflow;
                writingPreviewBody.enableAutoSizing = false;
                ApplyLetterSpacing(writingPreviewBody, 0f);
            }
            // Paragraph strip + action buttons built once at overlay create time.
            if (writingParagraphList != null)
            {
                foreach (var t in writingParagraphList.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (t == null || t.name != "Label") continue;
                    Chrome(t, 17);
                }
            }
            if (writingMatsRoot != null)
            {
                foreach (var btn in writingMatsRoot.GetComponentsInChildren<Button>(true))
                {
                    if (btn == null) continue;
                    var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label == null || label.name != "Label") continue;
                    // Skip material cards (spawned under CardGrid Content).
                    if (writingCardGrid != null && label.transform.IsChildOf(writingCardGrid))
                        continue;
                    Chrome(label, 18, true);
                }
            }

            if (writingDeskRoot != null)
            {
                Chrome(wdHeadline, 32, true, wrap: true);
                Chrome(wdKicker, 14);
                Chrome(wdDate, 14);
                Chrome(wdMatsCount, 16, true);
                Chrome(wdMatsList, 13, wrap: true);
                Chrome(wdMatsHint, 13);
                Chrome(wdSourcesLine, 14, wrap: true);
                Chrome(wdStatusLines, 13, wrap: true);
                Chrome(wdDirGuardTx, 16, true, wrap: true);
                Chrome(wdDirRescueTx, 16, true, wrap: true);
                if (wdDraftBody != null)
                {
                    wdDraftBody.font = font;
                    wdDraftBody.fontSize = Mathf.RoundToInt(18f * scale);
                    wdDraftBody.lineSpacing = 15f;
                    wdDraftBody.enableWordWrapping = true;
                    wdDraftBody.overflowMode = TextOverflowModes.Overflow;
                    ApplyLetterSpacing(wdDraftBody, 0f);
                }
                if (wdDraftCharCount != null)
                {
                    wdDraftCharCount.font = font;
                    wdDraftCharCount.fontSize = Mathf.RoundToInt(13f * scale);
                    ApplyLetterSpacing(wdDraftCharCount, 0f);
                }
                if (wdDraftInput != null)
                {
                    wdDraftInput.fontAsset = font;
                    wdDraftInput.pointSize = Mathf.RoundToInt(18f * scale);
                    if (wdDraftInput.placeholder is TextMeshProUGUI ph)
                    {
                        ph.font = font;
                        ph.fontSize = Mathf.RoundToInt(18f * scale);
                    }
                }
                foreach (var btn in writingDeskRoot.GetComponentsInChildren<Button>(true))
                {
                    var label = btn != null ? btn.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                    if (label != null && label.name == "T")
                        Chrome(label, 15, true);
                }
            }

            if (writingMatsActive)
                RefreshWritingMaterialsBoard();
            if (writingDeskActive)
                RefreshWritingDesk();
        }

        void BuildWritingPreviewPanel(Transform cork)
        {
            writingPreviewRoot = new GameObject("ArticlePreview", typeof(RectTransform));
            writingPreviewRoot.transform.SetParent(cork, false);
            StretchFull(writingPreviewRoot.GetComponent<RectTransform>());

            var dim = CreateImage(writingPreviewRoot.transform, "Dim", new Color(0.05f, 0.04f, 0.03f, 0.72f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var paper = CreateImage(writingPreviewRoot.transform, "Paper", WmPaper);
            Stretch(paper.rectTransform, new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.88f),
                Vector2.zero, Vector2.zero);

            var title = CreateUiText(paper.transform, "Title", 22, TextAnchor.MiddleCenter,
                WmInk, Vector2.zero, Vector2.zero);
            Stretch(title.rectTransform, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.98f),
                Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyles.Bold;
            title.text = UiLoc.T("ui.writing.preview_title", "文章预览");
            var titleTag = title.gameObject.AddComponent<LocTag>();
            titleTag.key = "ui.writing.preview_title";
            titleTag.target = title;

            var host = new GameObject("BodyHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            host.transform.SetParent(paper.transform, false);
            Stretch(host.GetComponent<RectTransform>(), new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.88f),
                Vector2.zero, Vector2.zero);
            host.GetComponent<Image>().color = new Color(1, 1, 1, 0.001f);
            var scroll = host.GetComponent<ScrollRect>();
            scroll.horizontal = false;

            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            vp.transform.SetParent(host.transform, false);
            StretchFull(vp.GetComponent<RectTransform>());
            vp.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(vp.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = Vector2.zero;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            writingPreviewBody = content.AddComponent<TextMeshProUGUI>();
            writingPreviewBody.font = font;
            writingPreviewBody.fontSize = 20;
            writingPreviewBody.color = WmInk;
            writingPreviewBody.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
            writingPreviewBody.enableWordWrapping = true;
            writingPreviewBody.overflowMode = TextOverflowModes.Overflow;
            writingPreviewBody.lineSpacing = 45f;
            writingPreviewBody.enableAutoSizing = false;
            writingPreviewBody.raycastTarget = false;
            scroll.viewport = vp.GetComponent<RectTransform>();
            scroll.content = crt;

            var close = SpawnWritingActionButton(paper.transform, "ClosePreview",
                UiLoc.T("ui.writing.preview_close", "关闭预览"), WmTeal,
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.10f),
                () => { if (writingPreviewRoot) writingPreviewRoot.SetActive(false); });
            var closeTag = close.gameObject.AddComponent<LocTag>();
            closeTag.key = "ui.writing.preview_close";
            closeTag.target = close.GetComponentInChildren<TextMeshProUGUI>();

            writingPreviewRoot.SetActive(false);
        }

        Button SpawnWritingActionButton(Transform parent, string name, string label, Color bg,
            Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), aMin, aMax, Vector2.zero, Vector2.zero);
            go.GetComponent<Image>().color = bg;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            btn.onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                onClick?.Invoke();
            });
            float scale = GameSettings.FontSizeScale;
            var tx = CreateUiText(go.transform, "Label", Mathf.RoundToInt(18f * scale),
                TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.zero);
            StretchFull(tx.rectTransform);
            tx.fontStyle = FontStyles.Bold;
            tx.enableWordWrapping = false;
            tx.enableAutoSizing = false;
            tx.text = label;
            tx.raycastTarget = false;
            ApplyLetterSpacing(tx, 0f);
            return btn;
        }

        void SpawnWritingParagraphRow(int index)
        {
            var go = new GameObject("Para" + index, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(writingParagraphList, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);
            go.GetComponent<LayoutElement>().flexibleHeight = 1f;
            int captured = index;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                writingFocusParagraph = captured;
                writingFocusMatId = null;
                RefreshWritingMaterialsBoard();
            });

            var bar = CreateImage(go.transform, "SelBar", WmRedBar);
            Stretch(bar.rectTransform, new Vector2(0f, 0.15f), new Vector2(0f, 0.85f),
                new Vector2(0f, 0f), new Vector2(5f, 0f));
            bar.gameObject.name = "SelBar";

            float scale = GameSettings.FontSizeScale;
            var label = CreateUiText(go.transform, "Label", Mathf.RoundToInt(17f * scale),
                TextAnchor.MiddleLeft, WmInk, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero);
            label.enableAutoSizing = false;
            label.text = UiLoc.T(WmParagraphKeys[index], WmParagraphFallback[index]);
            ApplyLetterSpacing(label, 0f);
            var tag = label.gameObject.AddComponent<LocTag>();
            tag.key = WmParagraphKeys[index];
            tag.target = label;

            var circle = CreateImage(go.transform, "Circle", new Color(0.62f, 0.58f, 0.52f, 1f));
            var crt = circle.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.90f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(16f, 16f);
            circle.raycastTarget = false;
        }

        void ApplyWritingCorkTexture(Image cork)
        {
            EnsureWritingCorkSprite();
            if (writingCorkSprite != null)
            {
                cork.sprite = writingCorkSprite;
                cork.type = Image.Type.Tiled;
                cork.color = Color.white;
                return;
            }
            var paper = VnArt.GetUi("tex_paper_dark");
            if (paper != null)
            {
                cork.sprite = paper;
                cork.type = Image.Type.Tiled;
                cork.color = new Color(0.72f, 0.52f, 0.36f, 1f);
            }
        }

        void EnsureWritingCorkSprite()
        {
            if (writingCorkSprite != null) return;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                float n2 = Mathf.PerlinNoise(x * 0.45f + 12f, y * 0.45f + 7f);
                var c = Color.Lerp(WmCorkA, WmCorkB, n);
                c = Color.Lerp(c, new Color(0.35f, 0.24f, 0.16f, 1f), n2 * 0.25f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply(false, false);
            writingCorkSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        void ShowWritingMaterialsBoard()
        {
            if (writingMatsRoot == null) return;
            writingMatsActive = true;
            if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(false);
            if (buttonRoot != null) buttonRoot.gameObject.SetActive(false);
            if (choiceRoot != null)
            {
                choiceRoot.parent?.gameObject.SetActive(false);
                choiceRoot.gameObject.SetActive(false);
            }
            if (choiceHostImage != null) choiceHostImage.gameObject.SetActive(false);
            if (advanceCatcher != null) advanceCatcher.gameObject.SetActive(false);
            ClearButtons();
            if (writingPreviewRoot) writingPreviewRoot.SetActive(false);
            writingMatsRoot.SetActive(true);
            writingMatsRoot.transform.SetAsLastSibling();
            BringOverlayStackToFront();
            RefreshWritingMatsLocalizedChrome();
            RefreshWritingMaterialsBoard();
        }

        void HideWritingMaterialsBoard()
        {
            if (writingMatsRoot != null) writingMatsRoot.SetActive(false);
            if (writingPreviewRoot != null) writingPreviewRoot.SetActive(false);
        }

        void RefreshWritingMatsLocalizedChrome()
        {
            if (writingMatsRoot == null) return;
            if (writingTapeTitle != null)
                writingTapeTitle.text = UiLoc.T("ui.writing.tape_title", "第一章 写稿 / 素材卡库");
            foreach (var tag in writingMatsRoot.GetComponentsInChildren<LocTag>(true))
            {
                if (tag == null || string.IsNullOrEmpty(tag.key)) continue;
                var tx = tag.target != null ? tag.target : tag.GetComponentInChildren<TextMeshProUGUI>();
                if (tx != null) tx.text = UiLoc.T(tag.key);
            }
            if (writingMatsRoot.activeSelf)
                RefreshWritingMaterialsBoard();
        }

        void ClearWritingSpawned()
        {
            foreach (var go in writingSpawned)
                if (go) Destroy(go);
            writingSpawned.Clear();
        }

        void RefreshWritingMaterialsBoard()
        {
            if (writingMatsRoot == null || !writingMatsRoot.activeSelf) return;
            var gs = GameState.Instance;
            if (gs == null) return;

            int selected = selectedMats.Count;
            if (writingSelectedCountText != null)
            {
                var fmt = UiLoc.T("ui.writing.selected_fmt", "已选素材 {0}/{1}");
                writingSelectedCountText.text = string.Format(fmt, selected, WritingMaxSelect);
            }

            for (int i = 0; i < writingDotImages.Count; i++)
            {
                if (writingDotImages[i] == null) continue;
                writingDotImages[i].color = i < selected ? WmOrange : new Color(0.55f, 0.52f, 0.48f, 1f);
            }

            RefreshWritingParagraphRows();
            RefreshWritingCardGrid();
            RefreshWritingDetailPanel();

            if (writingStatusHint != null)
            {
                var assembler = new ArticleAssembler();
                if (assembler.CanAssemble(pendingDir, selectedMats, out _))
                {
                    writingStatusHint.text = UiLoc.T("ui.writing.hint_ready",
                        "四段都有素材了。可以前往写稿——成稿会按你选的卡生成。");
                }
                else
                {
                    ArticleAssembler.CountParagraphCoverage(selectedMats, out int p1, out int p2, out int p3, out int p4);
                    int covered = (p1 > 0 ? 1 : 0) + (p2 > 0 ? 1 : 0) + (p3 > 0 ? 1 : 0) + (p4 > 0 ? 1 : 0);
                    writingStatusHint.text = string.Format(
                        UiLoc.T("ui.writing.hint_need_paras",
                            "成稿只需段落 01～04 各有至少一张素材（不强制指定某张卡）。已覆盖 {0}/4 段。"),
                        covered);
                }
            }

            if (writingReInterviewBtn != null)
            {
                bool canRe = gs.HasFlag(FlagIds.DafuInterviewDone) || gs.HasFlag(FlagIds.LinInterviewDone);
                writingReInterviewBtn.gameObject.SetActive(canRe);
            }
        }

        void RefreshWritingParagraphRows()
        {
            if (writingParagraphList == null) return;
            for (int i = 0; i < writingParagraphList.childCount; i++)
            {
                var row = writingParagraphList.GetChild(i);
                bool on = i == writingFocusParagraph;
                var bar = row.Find("SelBar");
                if (bar != null) bar.gameObject.SetActive(on);
                var circle = row.Find("Circle");
                if (circle != null)
                {
                    var img = circle.GetComponent<Image>();
                    if (img != null)
                        img.color = on ? WmOrange : new Color(0.62f, 0.58f, 0.52f, 1f);
                }
                var label = row.Find("Label");
                if (label != null)
                {
                    var tx = label.GetComponent<TextMeshProUGUI>();
                    if (tx != null)
                        tx.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
                }

                float fill = WritingParagraphFill(i);
                if (circle != null && !on && fill > 0.01f && fill < 0.99f)
                {
                    var img = circle.GetComponent<Image>();
                    if (img != null)
                        img.color = Color.Lerp(new Color(0.62f, 0.58f, 0.52f, 1f), WmOrange, fill);
                }
                else if (circle != null && !on && fill >= 0.99f)
                {
                    var img = circle.GetComponent<Image>();
                    if (img != null) img.color = WmOrange;
                }
            }
        }

        float WritingParagraphFill(int paraIndex)
        {
            int total = 0, picked = 0;
            foreach (var m in MaterialCatalog.All)
            {
                if (!MaterialMatchesParagraph(m, paraIndex)) continue;
                total++;
                if (selectedMats.Contains(m.id)) picked++;
            }
            if (total <= 0) return 0f;
            return (float)picked / total;
        }

        static bool MaterialMatchesParagraph(MaterialCard m, int paraIndex)
        {
            if (m == null) return false;
            switch (paraIndex)
            {
                case 0: return m.stage == ArticleStage.A_PresentLife || m.stage == ArticleStage.E_AfterReturn;
                case 1: return m.stage == ArticleStage.B_PastInjury;
                case 2: return m.stage == ArticleStage.C_RescueTreatment;
                case 3: return m.stage == ArticleStage.D_Release;
                default: return false;
            }
        }

        void RefreshWritingCardGrid()
        {
            ClearWritingSpawned();
            if (writingCardGrid == null) return;
            var unlocked = GameState.Instance != null
                ? new HashSet<string>(GameState.Instance.Data.unlockedMaterials)
                : new HashSet<string>();

            var list = new List<MaterialCard>();
            foreach (var m in MaterialCatalog.All)
            {
                if (MaterialMatchesParagraph(m, writingFocusParagraph))
                    list.Add(m);
            }

            if (list.Count == 0)
            {
                float scale = GameSettings.FontSizeScale;
                var empty = CreateUiText(writingCardGrid, "Empty", Mathf.RoundToInt(18f * scale),
                    TextAnchor.MiddleCenter,
                    new Color(0.95f, 0.90f, 0.82f, 0.85f), Vector2.zero, Vector2.zero);
                StretchFull(empty.rectTransform);
                empty.enableAutoSizing = false;
                empty.text = UiLoc.T("ui.writing.empty_para", "此段落暂无素材卡");
                ApplyLetterSpacing(empty, 0f);
                writingSpawned.Add(empty.gameObject);
                return;
            }

            if (string.IsNullOrEmpty(writingFocusMatId) ||
                list.Find(m => m.id == writingFocusMatId) == null)
            {
                var firstUnlocked = list.Find(m => unlocked.Contains(m.id));
                writingFocusMatId = firstUnlocked != null ? firstUnlocked.id : list[0].id;
            }

            int idx = 0;
            foreach (var m in list)
            {
                bool isUnlocked = unlocked.Contains(m.id);
                bool isSelected = selectedMats.Contains(m.id);
                bool isFocus = m.id == writingFocusMatId;
                SpawnWritingMaterialCard(m, isUnlocked, isSelected, isFocus, idx);
                idx++;
            }
        }

        void SpawnWritingMaterialCard(MaterialCard m, bool unlocked, bool selected, bool focus, int visualIndex)
        {
            var go = new GameObject(m.id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(writingCardGrid, false);
            writingSpawned.Add(go);

            var shadow = CreateImage(go.transform, "Shadow", new Color(0f, 0f, 0f, 0.28f));
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, -4f);
            shadow.raycastTarget = false;

            var bg = go.GetComponent<Image>();
            bg.color = unlocked ? ColorForMaterialType(m.type, visualIndex) : WmLocked;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            string mid = m.id;
            btn.onClick.AddListener(() => OnWritingCardClicked(mid));

            // No card tilt — rotated UI Text + pixelPerfect canvas reads as soft/blurry glyphs.

            if (focus)
            {
                var rim = CreateImage(go.transform, "FocusRim", WmOrange);
                Stretch(rim.rectTransform, Vector2.zero, Vector2.one, new Vector2(-3f, -3f), new Vector2(3f, 3f));
                rim.transform.SetAsFirstSibling();
                rim.raycastTarget = false;
                shadow.transform.SetAsFirstSibling();
            }

            // Tape / paperclip deco
            if (visualIndex % 3 == 0)
            {
                var tape = CreateImage(go.transform, "Tape", new Color(0.90f, 0.82f, 0.62f, 0.9f));
                var trt = tape.rectTransform;
                trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = new Vector2(0f, 4f);
                trt.sizeDelta = new Vector2(64f, 16f);
                var tSpr = VnArt.GetTitle(selected ? "btn_tape_primary_idle" : "btn_tape_idle");
                if (tSpr != null)
                {
                    tape.sprite = tSpr;
                    tape.preserveAspect = true;
                    tape.color = Color.white;
                }
                tape.raycastTarget = false;
            }
            else if (visualIndex % 3 == 1)
            {
                var clip = CreateImage(go.transform, "Clip", Color.white);
                var crt = clip.rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0.12f, 1f);
                crt.pivot = new Vector2(0.5f, 0.8f);
                crt.anchoredPosition = new Vector2(0f, 6f);
                crt.sizeDelta = new Vector2(26f, 36f);
                var cSpr = VnArt.GetTitle("deco_paperclip");
                if (cSpr != null)
                {
                    clip.sprite = cSpr;
                    clip.preserveAspect = true;
                }
                else clip.color = new Color(0.7f, 0.72f, 0.76f, 0.9f);
                clip.raycastTarget = false;
            }

            float scale = GameSettings.FontSizeScale;
            int Sz(float baseSize) => Mathf.RoundToInt(baseSize * scale);

            var idTx = CreateUiText(go.transform, "Id", Sz(15), TextAnchor.UpperLeft, WmInkMuted, Vector2.zero, Vector2.zero);
            Stretch(idTx.rectTransform, new Vector2(0.07f, 0.80f), new Vector2(0.38f, 0.96f), Vector2.zero, Vector2.zero);
            idTx.fontStyle = FontStyles.Bold;
            idTx.enableWordWrapping = false;
            idTx.overflowMode = TextOverflowModes.Overflow;
            idTx.enableAutoSizing = false;
            idTx.text = m.id;
            ApplyLetterSpacing(idTx, 0f);

            var tagBg = CreateImage(go.transform, "TypeTag", new Color(1f, 1f, 1f, 0.55f));
            Stretch(tagBg.rectTransform, new Vector2(0.40f, 0.80f), new Vector2(0.93f, 0.95f), Vector2.zero, Vector2.zero);
            var tagTx = CreateUiText(tagBg.transform, "T", Sz(14), TextAnchor.MiddleCenter, WmInk, Vector2.zero, Vector2.zero);
            StretchFull(tagTx.rectTransform);
            tagTx.fontStyle = FontStyles.Bold;
            tagTx.enableWordWrapping = false;
            tagTx.enableAutoSizing = false;
            tagTx.text = MaterialTypeLabel(m.type);
            ApplyLetterSpacing(tagTx, 0f);

            var titleTx = CreateUiText(go.transform, "Title", Sz(19), TextAnchor.UpperLeft, WmInk, Vector2.zero, Vector2.zero);
            Stretch(titleTx.rectTransform, new Vector2(0.07f, 0.50f), new Vector2(0.93f, 0.78f), Vector2.zero, Vector2.zero);
            titleTx.fontStyle = FontStyles.Bold;
            titleTx.enableWordWrapping = true;
            titleTx.overflowMode = TextOverflowModes.Truncate;
            titleTx.enableAutoSizing = false;
            titleTx.text = unlocked ? m.title : "？？？";
            ApplyLetterSpacing(titleTx, 0f);

            var bodyTx = CreateUiText(go.transform, "Body", Sz(15), TextAnchor.UpperLeft, WmInkMuted, Vector2.zero, Vector2.zero);
            Stretch(bodyTx.rectTransform, new Vector2(0.07f, 0.12f), new Vector2(0.78f, 0.48f), Vector2.zero, Vector2.zero);
            bodyTx.enableWordWrapping = true;
            bodyTx.overflowMode = TextOverflowModes.Truncate;
            bodyTx.lineSpacing = 20f;
            bodyTx.enableAutoSizing = false;
            bodyTx.text = unlocked ? Shorten(m.body, 52) : UiLoc.T("ui.writing.locked", "尚未解锁");
            ApplyLetterSpacing(bodyTx, 0f);

            var status = CreateImage(go.transform, "Status", Color.white);
            var srt = status.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.88f, 0.14f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(22f, 22f);
            status.raycastTarget = false;
            if (!unlocked)
                status.color = new Color(0.35f, 0.32f, 0.28f, 0.95f);
            else if (selected)
                status.color = new Color(0.22f, 0.55f, 0.32f, 1f);
            else
                status.color = new Color(0.55f, 0.52f, 0.48f, 0.85f);

            if (unlocked && selected)
            {
                var check = CreateUiText(status.transform, "Check", Sz(14), TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.zero);
                StretchFull(check.rectTransform);
                check.enableWordWrapping = false;
                check.enableAutoSizing = false;
                check.text = "✓";
                check.fontStyle = FontStyles.Bold;
                ApplyLetterSpacing(check, 0f);
            }
            else if (!unlocked)
            {
                var lockTx = CreateUiText(status.transform, "Lock", Sz(13), TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.zero);
                StretchFull(lockTx.rectTransform);
                lockTx.enableWordWrapping = false;
                lockTx.enableAutoSizing = false;
                lockTx.text = "锁";
                ApplyLetterSpacing(lockTx, 0f);
            }
        }

        void OnWritingCardClicked(string matId)
        {
            SfxController.Instance?.PlayUi();
            var unlocked = GameState.Instance != null && GameState.Instance.Data.unlockedMaterials.Contains(matId);
            writingFocusMatId = matId;
            if (!unlocked)
            {
                RefreshWritingMaterialsBoard();
                return;
            }

            if (selectedMats.Contains(matId))
                selectedMats.Remove(matId);
            else if (selectedMats.Count < WritingMaxSelect)
                selectedMats.Add(matId);
            else if (writingStatusHint != null)
                writingStatusHint.text = UiLoc.T("ui.writing.hint_max", "最多选择 10 张素材。");

            RefreshWritingMaterialsBoard();
        }

        void RefreshWritingDetailPanel()
        {
            if (writingDetailTitle == null) return;
            var m = MaterialCatalog.Get(writingFocusMatId);
            if (m == null)
            {
                writingDetailTitle.text = UiLoc.T("ui.writing.detail_empty", "点选一张素材卡");
                if (writingDetailTag != null) writingDetailTag.text = "";
                if (writingDetailTagBg != null) writingDetailTagBg.gameObject.SetActive(false);
                if (writingDetailSource != null) writingDetailSource.text = "";
                if (writingDetailBody != null) writingDetailBody.text = "";
                return;
            }

            bool unlocked = GameState.Instance != null &&
                            GameState.Instance.Data.unlockedMaterials.Contains(m.id);
            writingDetailTitle.text = unlocked ? (m.id + "  " + m.title) : (m.id + "  ？？？");
            if (writingDetailTagBg != null)
            {
                writingDetailTagBg.gameObject.SetActive(true);
                writingDetailTagBg.color = unlocked ? ColorForMaterialType(m.type, 0) : WmLocked;
            }
            if (writingDetailTag != null)
                writingDetailTag.text = MaterialTypeLabel(m.type);
            if (writingDetailSource != null)
            {
                var src = unlocked
                    ? (UiLoc.T("ui.writing.source_prefix", "来源：") + MaterialSourceLabel(m))
                    : UiLoc.T("ui.writing.source_locked", "来源：未解锁");
                writingDetailSource.text = src;
            }
            if (writingDetailBody != null)
                writingDetailBody.text = unlocked
                    ? m.body
                    : UiLoc.T("ui.writing.detail_locked", "继续采访与调查后，这张素材才会解锁。");
        }

        void OnWritingPreviewArticle()
        {
            // Preview button removed from player UI; desk is the editable 成稿 surface.
        }

        void OnWritingGoToDesk()
        {
            // Newspaper desk — direction + materials summary + live draft + submit.
            ShowWritingDesk();
        }

        static Color ColorForMaterialType(MaterialType type, int visualIndex)
        {
            switch (type)
            {
                case MaterialType.Fact: return visualIndex % 2 == 0 ? WmFact : new Color(0.62f, 0.78f, 0.70f, 1f);
                case MaterialType.Detail: return visualIndex % 2 == 0 ? WmDetail : WmPeach;
                case MaterialType.Emotion: return WmEmotion;
                default: return WmLocked;
            }
        }

        string MaterialTypeLabel(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.Fact: return UiLoc.T("ui.writing.type_fact", "事实");
                case MaterialType.Detail: return UiLoc.T("ui.writing.type_detail", "细节");
                case MaterialType.Emotion: return UiLoc.T("ui.writing.type_emotion", "情感");
                default: return UiLoc.T("ui.writing.type_unconfirmed", "待确认");
            }
        }

        string MaterialSourceLabel(MaterialCard m)
        {
            if (m == null) return "";
            switch (m.id)
            {
                case MaterialIds.M01:
                case MaterialIds.M14:
                case MaterialIds.M15:
                    return UiLoc.T("ui.writing.src_community", "社区观察");
                case MaterialIds.M02:
                case MaterialIds.M03:
                case MaterialIds.M04:
                    return UiLoc.T("ui.writing.src_dafu", "大福的记忆");
                case MaterialIds.M05:
                case MaterialIds.M06:
                case MaterialIds.M07:
                case MaterialIds.M08:
                case MaterialIds.M09:
                case MaterialIds.M10:
                case MaterialIds.M11:
                case MaterialIds.M12:
                case MaterialIds.M13:
                    return UiLoc.T("ui.writing.src_lin", "林女士的描述");
                case MaterialIds.M16:
                    return UiLoc.T("ui.writing.src_unconfirmed", "多方交叉仍未确认");
                default:
                    return UiLoc.T("ui.writing.src_notes", "记者笔记整理");
            }
        }

        static string Shorten(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= maxChars) return s;
            return s.Substring(0, maxChars) + "…";
        }

        // ── Writing desk flow (moved from GameUI.cs) ─────────────────────────

        public void ShowWriting()
        {
            mode = Mode.Writing;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "writing";
            SetAdvanceEnabled(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            inputField.gameObject.SetActive(false);
            HideWritingMaterialsBoard();
            HideWritingDesk();
            writingMatsActive = false;
            SetChrome(true, false, true);
            stageHint.text = "写稿";
            SetStageBackground("编辑部工位_上午");
            RefreshHeader();
            selectedMats.Clear();
            writingFocusParagraph = 0;
            writingFocusMatId = null;
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
            writingMatsActive = false;
            HideWritingMaterialsBoard();
            HideWritingDesk();
            if (dialoguePanel != null && mode == Mode.Writing)
                dialoguePanel.gameObject.SetActive(true);
            if (buttonRoot != null && mode == Mode.Writing)
                buttonRoot.gameObject.SetActive(true);
            SetChrome(true, false, true);
            RefreshHeader();
            SetSpeaker("沈禾", LineSpeaker.Character, "认真");
            var unlocked = GameState.Instance.Data.unlockedMaterials.Count;
            var body = "选一个报道立意。素材决定你能写什么，立意决定你想讲什么。\n\n已解锁素材 " +
                       unlocked + " 张。";
            if (unlocked < 8)
                body += "\n\n素材还不够成稿。如果采访里还有没问到的，可以回去补充。";
            SetBody(body);
            ClearButtons();
            AddChoice(ArticleAssembler.TitleFor(WritingDirection.GuardCatToday) + "　从流浪猫到社区保安", () =>
            {
                pendingDir = WritingDirection.GuardCatToday;
                ShowMaterialPick();
            });
            AddChoice(ArticleAssembler.TitleFor(WritingDirection.RescueWithoutAdoption) + "　一次没有以收养结束的救助", () =>
            {
                pendingDir = WritingDirection.RescueWithoutAdoption;
                ShowMaterialPick();
            });
            AddReInterviewActions(unlocked < 8);
            AddAction("笔记", OpenNotebook);
        }

        void ShowMaterialPick()
        {
            mode = Mode.Writing;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "writing";
            SetAdvanceEnabled(false);
            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            if (inputField) inputField.gameObject.SetActive(false);
            HideWritingDesk();
            ShowWritingMaterialsBoard();
        }

        void GenerateArticle()
        {
            HideWritingMaterialsBoard();
            // Preserve player edits from the desk input before tearing the overlay down.
            SyncWritingDeskDraftToAssembler();
            HideWritingDesk();
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

            if (writingPolishCo != null)
            {
                StopCoroutine(writingPolishCo);
                writingPolishCo = null;
            }

            // Keep edited/polished body — only Assemble when body is empty.
            if (string.IsNullOrWhiteSpace(assembler.Body))
                assembler.Assemble(pendingDir, selectedMats);

            GameState.Instance.Data.writingDirection = (int)pendingDir;
            GameState.Instance.Data.selectedMaterials = new List<string>(selectedMats);
            GameState.Instance.Data.lastArticleTitle = assembler.Title;
            GameState.Instance.Data.lastArticleBody = assembler.Body;

            SetStageBackground("沈禾办公室_上午");
            BgmController.Instance?.PlayScriptLabel("编辑部日常_01（循环）");
            SetSpeaker("沈禾", LineSpeaker.Character, "认真");
            // Body already edited on the desk — do not re-dump the full article here.
            SetBody("稿件已提交。正在送审…");
            statusText.text = "审核中…";
            ClearButtons();
            writingMatsActive = false;

            if (writingReviewCo != null)
                StopCoroutine(writingReviewCo);
            // Expand/polish is desk-button only — review never auto-expands.
            writingReviewCo = StartCoroutine(GenerateArticleReviewCo());
        }

        System.Collections.IEnumerator GenerateArticleReviewCo()
        {
            yield return ArticleReviewAi.ReviewCoroutine(
                assembler, pendingDir, selectedMats, null, skipExpand: true);

            GameState.Instance.Data.lastReviewScore = assembler.Score;
            GameState.Instance.Data.lastArticleBody = assembler.Body;
            GameState.Instance.Data.lastArticleTitle = assembler.Title;

            SetSpeaker("沈禾", LineSpeaker.Character, assembler.CanPublish ? "淡淡认可" : "认真");
            // Review / score only — full article stays on the writing desk.
            SetBody("—— 沈禾审核 ——\n" + assembler.ReviewText
                    + "\n\n评分　" + assembler.Score);
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
            writingReviewCo = null;
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
            writingMatsActive = false;
            HideWritingMaterialsBoard();
            SetChrome(true, false, true);
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
                if (selectedMats.Count > 0 || writingMatsActive)
                    ShowMaterialPick();
                else
                    ShowWritingDirectionPick();
            }, true);
            AddAction("笔记", OpenNotebook);
        }

        void ResumeWritingMode()
        {
            if (writingDeskActive)
                ShowWritingDesk();
            else if (writingMatsActive)
                ShowMaterialPick();
            else
                ShowWritingDirectionPick();
        }
    }
}
