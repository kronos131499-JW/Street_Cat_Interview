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
    /// Material-card library (木桌纸板) + writing-desk flow.
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
        TextMeshProUGUI writingDetailId;
        TextMeshProUGUI writingDetailTag;
        TextMeshProUGUI writingDetailSource;
        TextMeshProUGUI writingDetailBody;
        Image writingDetailTagBg;
        TextMeshProUGUI writingPreviewBody;
        TextMeshProUGUI writingStatusHint;
        Button writingGoBtn;
        Button writingPreviewBtn;
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

        /// <summary>Design pixels on the 1920×1080 canvas, origin top-left.</summary>
        static void PlaceScreen(RectTransform rt, float x, float y, float w, float h)
        {
            const float dw = 1920f;
            const float dh = 1080f;
            rt.anchorMin = new Vector2(x / dw, 1f - (y + h) / dh);
            rt.anchorMax = new Vector2((x + w) / dw, 1f - y / dh);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        void BuildWritingMaterialsOverlay(Transform parent)
        {
            writingMatsRoot = new GameObject("WritingMaterialsOverlay", typeof(RectTransform));
            writingMatsRoot.transform.SetParent(parent, false);
            StretchFull(writingMatsRoot.GetComponent<RectTransform>());

            var wood = CreateImage(writingMatsRoot.transform, "Wood", new Color(0.62f, 0.40f, 0.22f, 1f));
            StretchFull(wood.rectTransform);
            wood.raycastTarget = true;

            var board = CreateImage(writingMatsRoot.transform, "Board", new Color(0.42f, 0.28f, 0.18f, 1f));
            PlaceScreen(board.rectTransform, 48f, 96f, 1824f, 790f);
            board.raycastTarget = false;

            var titlePlate = CreateImage(writingMatsRoot.transform, "TitlePlate", Color.white);
            PlaceScreen(titlePlate.rectTransform, 28f, 18f, 640f, 168f);
            titlePlate.raycastTarget = false;

            writingTapeTitle = CreateUiText(titlePlate.transform, "TapeFallback", 28, TextAnchor.MiddleLeft,
                MaterialCardArt.Ink, Vector2.zero, Vector2.zero);
            Stretch(writingTapeTitle.rectTransform, new Vector2(0.10f, 0.16f), new Vector2(0.78f, 0.84f),
                Vector2.zero, Vector2.zero);
            writingTapeTitle.fontStyle = FontStyles.Bold;
            writingTapeTitle.enableWordWrapping = true;
            writingTapeTitle.overflowMode = TextOverflowModes.Overflow;
            writingTapeTitle.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
            writingTapeTitle.text = UiLoc.T("ui.writing.tape_title", "第一章 写稿 / 素材卡库").Replace(" / ", "\n");

            var topRight = new GameObject("SelectedHeader", typeof(RectTransform));
            topRight.transform.SetParent(writingMatsRoot.transform, false);
            PlaceScreen(topRight.GetComponent<RectTransform>(), 980f, 148f, 820f, 46f);

            writingSelectedCountText = CreateUiText(topRight.transform, "Count", 22, TextAnchor.MiddleRight,
                MaterialCardArt.Cream, Vector2.zero, Vector2.zero);
            Stretch(writingSelectedCountText.rectTransform, new Vector2(0f, 0f), new Vector2(0.48f, 1f),
                Vector2.zero, Vector2.zero);
            writingSelectedCountText.fontStyle = FontStyles.Bold;
            writingSelectedCountText.enableWordWrapping = false;

            writingProgressDots = new GameObject("Dots", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            writingProgressDots.SetParent(topRight.transform, false);
            Stretch(writingProgressDots.GetComponent<RectTransform>(), new Vector2(0.50f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            var dh = writingProgressDots.GetComponent<HorizontalLayoutGroup>();
            dh.spacing = 8f;
            dh.childAlignment = TextAnchor.MiddleRight;
            dh.childForceExpandWidth = false;
            dh.childForceExpandHeight = false;
            dh.childControlWidth = false;
            dh.childControlHeight = false;
            writingDotImages.Clear();
            for (int i = 0; i < WritingMaxSelect; i++)
            {
                var dot = CreateImage(writingProgressDots, "Dot" + i, Color.white);
                dot.rectTransform.sizeDelta = new Vector2(26f, 26f);
                var hole = CreateImage(dot.transform, "Hole", new Color(0.45f, 0.30f, 0.20f, 1f));
                Stretch(hole.rectTransform, new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f),
                    Vector2.zero, Vector2.zero);
                hole.raycastTarget = false;
                writingDotImages.Add(dot);
            }

            var strip = CreateImage(writingMatsRoot.transform, "Structure", WmPaper);
            PlaceScreen(strip.rectTransform, 86f, 200f, 400f, 640f);
            strip.raycastTarget = false;

            var stripTitle = CreateUiText(strip.transform, "StripTitle", 16, TextAnchor.MiddleLeft,
                MaterialCardArt.Ink, Vector2.zero, Vector2.zero);
            Stretch(stripTitle.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.94f, 0.98f),
                Vector2.zero, Vector2.zero);
            stripTitle.fontStyle = FontStyles.Bold;
            stripTitle.text = UiLoc.T("ui.writing.structure", "文章结构");
            var stripTitleTag = stripTitle.gameObject.AddComponent<LocTag>();
            stripTitleTag.key = "ui.writing.structure";
            stripTitleTag.target = stripTitle;

            writingParagraphList = new GameObject("ParagraphList", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            writingParagraphList.SetParent(strip.transform, false);
            Stretch(writingParagraphList.GetComponent<RectTransform>(), new Vector2(0.05f, 0.04f), new Vector2(0.96f, 0.86f),
                Vector2.zero, Vector2.zero);
            var pv = writingParagraphList.GetComponent<VerticalLayoutGroup>();
            pv.spacing = 10f;
            pv.childForceExpandHeight = true;
            pv.childForceExpandWidth = true;
            pv.childControlHeight = true;
            pv.childControlWidth = true;
            pv.padding = new RectOffset(2, 2, 2, 2);

            for (int i = 0; i < 4; i++)
                SpawnWritingParagraphRow(i);

            var cardsBacking = CreateImage(writingMatsRoot.transform, "CardsBacking", new Color(1f, 1f, 1f, 0f));
            cardsBacking.gameObject.SetActive(false);
            cardsBacking.raycastTarget = false;

            var gridHost = new GameObject("CardGridHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            gridHost.transform.SetParent(writingMatsRoot.transform, false);
            PlaceScreen(gridHost.GetComponent<RectTransform>(), 520f, 210f, 860f, 620f);
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
            grid.cellSize = new Vector2(268f, 280f);
            grid.spacing = new Vector2(18f, 16f);
            grid.padding = new RectOffset(8, 8, 6, 8);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            gridContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            writingCardScroll.viewport = gridVp.GetComponent<RectTransform>();
            writingCardScroll.content = gcrt;

            var detail = CreateImage(writingMatsRoot.transform, "DetailPaper", WmPaper);
            PlaceScreen(detail.rectTransform, 1400f, 186f, 440f, 670f);
            detail.raycastTarget = false;

            var idBg = CreateImage(detail.transform, "IdTag", Color.white);
            var idRt = idBg.rectTransform;
            idRt.anchorMin = idRt.anchorMax = new Vector2(0.08f, 0.90f);
            idRt.pivot = new Vector2(0f, 0.5f);
            idRt.sizeDelta = new Vector2(148f, 52f);
            idBg.preserveAspect = true;
            idBg.raycastTarget = false;
            writingDetailId = CreateUiText(idBg.transform, "Id", 20, TextAnchor.MiddleCenter,
                MaterialCardArt.Ink, Vector2.zero, Vector2.zero);
            StretchFull(writingDetailId.rectTransform);
            writingDetailId.fontStyle = FontStyles.Bold;
            writingDetailId.enableWordWrapping = false;

            writingDetailTitle = CreateUiText(detail.transform, "Title", 26, TextAnchor.UpperLeft,
                MaterialCardArt.Ink, Vector2.zero, Vector2.zero);
            Stretch(writingDetailTitle.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.90f, 0.86f),
                Vector2.zero, Vector2.zero);
            writingDetailTitle.fontStyle = FontStyles.Bold;
            writingDetailTitle.overflowMode = TextOverflowModes.Overflow;
            writingDetailTitle.enableAutoSizing = false;

            var underline = CreateImage(detail.transform, "Underline", new Color(0.42f, 0.55f, 0.32f, 1f));
            var ulRt = underline.rectTransform;
            ulRt.anchorMin = ulRt.anchorMax = new Vector2(0.08f, 0.72f);
            ulRt.pivot = new Vector2(0f, 0.5f);
            ulRt.sizeDelta = new Vector2(220f, 10f);
            underline.raycastTarget = false;

            writingDetailTagBg = CreateImage(detail.transform, "Tag", WmOrange);
            var tagRt = writingDetailTagBg.rectTransform;
            tagRt.anchorMin = tagRt.anchorMax = new Vector2(0.08f, 0.64f);
            tagRt.pivot = new Vector2(0f, 0.5f);
            tagRt.sizeDelta = new Vector2(132f, 40f);
            writingDetailTagBg.preserveAspect = true;
            writingDetailTag = CreateUiText(writingDetailTagBg.transform, "TagLabel", 16, TextAnchor.MiddleCenter,
                MaterialCardArt.Ink, Vector2.zero, Vector2.zero);
            StretchFull(writingDetailTag.rectTransform);
            writingDetailTag.fontStyle = FontStyles.Bold;
            writingDetailTag.enableWordWrapping = false;
            writingDetailTag.enableAutoSizing = false;

            var separator = CreateImage(detail.transform, "Separator", new Color(0.55f, 0.42f, 0.32f, 0.7f));
            Stretch(separator.rectTransform, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.575f),
                Vector2.zero, Vector2.zero);
            separator.raycastTarget = false;

            writingDetailSource = CreateUiText(detail.transform, "Source", 15, TextAnchor.MiddleLeft,
                WmInkMuted, Vector2.zero, Vector2.zero);
            Stretch(writingDetailSource.rectTransform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.56f),
                Vector2.zero, Vector2.zero);
            writingDetailSource.enableAutoSizing = false;

            var detailHost = new GameObject("DetailBodyHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            detailHost.transform.SetParent(detail.transform, false);
            Stretch(detailHost.GetComponent<RectTransform>(), new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.48f),
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
            writingDetailBody.fontSize = 16;
            writingDetailBody.color = MaterialCardArt.Ink;
            writingDetailBody.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
            writingDetailBody.enableWordWrapping = true;
            writingDetailBody.overflowMode = TextOverflowModes.Overflow;
            writingDetailBody.lineSpacing = 8f;
            writingDetailBody.enableAutoSizing = false;
            writingDetailBody.raycastTarget = false;
            detailScroll.viewport = dVp.GetComponent<RectTransform>();
            detailScroll.content = dcrt;

            writingStatusHint = CreateUiText(writingMatsRoot.transform, "StatusHint", 15, TextAnchor.MiddleLeft,
                MaterialCardArt.Hint, Vector2.zero, Vector2.zero);
            PlaceScreen(writingStatusHint.rectTransform, 56f, 900f, 980f, 48f);

            writingPreviewBtn = SpawnWritingActionButton(writingMatsRoot.transform, "PreviewBtn",
                UiLoc.T("ui.writing.preview", "预览文章"), new Color(0.16f, 0.28f, 0.48f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), OnWritingPreviewArticle);
            PlaceScreen(writingPreviewBtn.GetComponent<RectTransform>(), 1146f, 968f, 380f, 84f);
            var previewTag = writingPreviewBtn.gameObject.AddComponent<LocTag>();
            previewTag.key = "ui.writing.preview";
            previewTag.target = writingPreviewBtn.GetComponentInChildren<TextMeshProUGUI>();

            writingGoBtn = SpawnWritingActionButton(writingMatsRoot.transform, "GoWriteBtn",
                UiLoc.T("ui.writing.go_write", "前往写稿"), WmOrange, new Vector2(0f, 0f), new Vector2(1f, 1f),
                OnWritingGoToDesk);
            PlaceScreen(writingGoBtn.GetComponent<RectTransform>(), 1540f, 968f, 340f, 84f);
            var goTag = writingGoBtn.gameObject.AddComponent<LocTag>();
            goTag.key = "ui.writing.go_write";
            goTag.target = writingGoBtn.GetComponentInChildren<TextMeshProUGUI>();

            var backBtn = SpawnWritingActionButton(writingMatsRoot.transform, "BackDirBtn",
                UiLoc.T("ui.writing.back_direction", "返回立意"), new Color(0.93f, 0.88f, 0.76f, 0.94f),
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                () => { writingMatsActive = false; HideWritingMaterialsBoard(); ShowWritingDirectionPick(); });
            PlaceScreen(backBtn.GetComponent<RectTransform>(), 40f, 972f, 280f, 78f);
            var backLabel = backBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (backLabel != null) backLabel.color = MaterialCardArt.Ink;
            var backTag = backBtn.gameObject.AddComponent<LocTag>();
            backTag.key = "ui.writing.back_direction";
            backTag.target = backLabel;

            var nbBtn = SpawnWritingActionButton(writingMatsRoot.transform, "NotebookBtn",
                UiLoc.T("ui.notebook", "笔记"), new Color(0.93f, 0.88f, 0.76f, 0.94f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), OpenNotebook);
            PlaceScreen(nbBtn.GetComponent<RectTransform>(), 334f, 972f, 230f, 78f);
            var nbLabel = nbBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (nbLabel != null) nbLabel.color = MaterialCardArt.Ink;

            writingReInterviewBtn = SpawnWritingActionButton(writingMatsRoot.transform, "ReInterviewBtn",
                UiLoc.T("ui.writing.reinterview", "返回采访"), new Color(0.93f, 0.88f, 0.76f, 0.94f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), ShowReInterviewMenu);
            PlaceScreen(writingReInterviewBtn.GetComponent<RectTransform>(), 578f, 972f, 340f, 78f);
            var riLabel = writingReInterviewBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (riLabel != null) riLabel.color = MaterialCardArt.Ink;
            var riTag = writingReInterviewBtn.gameObject.AddComponent<LocTag>();
            riTag.key = "ui.writing.reinterview";
            riTag.target = riLabel;

            BuildWritingPreviewPanel(writingMatsRoot.transform);

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

            Chrome(writingTapeTitle, 28, true, wrap: true);
            if (writingTapeTitle != null)
                writingTapeTitle.color = MaterialCardArt.Ink;
            Chrome(writingSelectedCountText, 22, true);
            if (writingSelectedCountText != null)
                writingSelectedCountText.color = MaterialCardArt.Cream;
            Chrome(writingDetailId, 18, true);
            if (writingDetailId != null)
                writingDetailId.color = MaterialCardArt.Ink;
            Chrome(writingDetailTitle, 24, true, wrap: true);
            if (writingDetailTitle != null)
                writingDetailTitle.color = MaterialCardArt.Ink;
            Chrome(writingDetailTag, 15, true);
            if (writingDetailTag != null)
                writingDetailTag.color = MaterialCardArt.Ink;
            Chrome(writingDetailSource, 15);
            Chrome(writingStatusHint, 16, wrap: true);
            if (writingStatusHint != null)
                writingStatusHint.color = MaterialCardArt.Hint;

            if (writingDetailBody != null)
            {
                writingDetailBody.font = font;
                writingDetailBody.fontSize = Mathf.RoundToInt(16f * scale);
                writingDetailBody.lineSpacing = 8f;
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
                    if (t == null) continue;
                    if (t.name == "Label") Chrome(t, 16, wrap: true);
                    else if (t.name == "Num" || t.name == "Chevron") Chrome(t, 16, true);
                }
            }
            if (writingMatsRoot != null)
            {
                var stripTitle = writingMatsRoot.transform.Find("Structure/StripTitle");
                if (stripTitle != null)
                {
                    var tx = stripTitle.GetComponent<TextMeshProUGUI>();
                    Chrome(tx, 16, true, wrap: true);
                    if (tx != null) tx.color = MaterialCardArt.Ink;
                }
                foreach (var btn in writingMatsRoot.GetComponentsInChildren<Button>(true))
                {
                    if (btn == null) continue;
                    var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label == null || label.name != "Label") continue;
                    // Skip material cards (spawned under CardGrid Content).
                    if (writingCardGrid != null && label.transform.IsChildOf(writingCardGrid))
                        continue;
                    if (writingParagraphList != null && label.transform.IsChildOf(writingParagraphList))
                        continue;
                    Chrome(label, 18, true);
                }
            }

            if (writingDeskRoot != null)
            {
                bool art = writingDeskArtOn;
                Chrome(wdHeadline, art ? 36 : 32, true, wrap: true);
                Chrome(wdKicker, art ? 26 : 14, art);
                Chrome(wdDate, art ? 22 : 14);
                Chrome(wdMatsCount, art ? 18 : 16, true);
                Chrome(wdMatsList, art ? 16 : 13, wrap: true);
                Chrome(wdMatsHint, art ? 16 : 13);
                Chrome(wdSourcesLine, 14, wrap: true);
                Chrome(wdStatusLines, art ? 16 : 13, wrap: !art);
                Chrome(wdDirGuardTx, art ? 17 : 16, true, wrap: true);
                Chrome(wdDirRescueTx, art ? 17 : 16, true, wrap: true);
                if (art)
                {
                    Chrome(wdDirHeading, 22, true);
                    Chrome(wdMatsHeading, 22, true);
                    Chrome(wdStatusHeading, 22, true);
                    Chrome(wdStep1, 26, true);
                    Chrome(wdStep2, 26, true);
                    Chrome(wdStep3, 26, true);
                    Chrome(wdSrcObs, 14, true);
                    Chrome(wdSrcCat, 14, true);
                    Chrome(wdSrcHuman, 14, true);
                    Chrome(wdStatusDirTx, 16);
                    Chrome(wdStatusParaTx, 16);
                }
                if (wdDraftBody != null)
                {
                    wdDraftBody.font = font;
                    wdDraftBody.fontSize = Mathf.RoundToInt((writingDeskArtOn ? 20f : 18f) * scale);
                    wdDraftBody.lineSpacing = writingDeskArtOn ? 12f : 15f;
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
                    {
                        Chrome(label, writingDeskArtOn ? 16 : 15, false);
                        label.fontStyle = FontStyles.Normal;
                        label.extraPadding = true;
                        VnText.ApplyFontWeight(label, GameSettings.FontWeight);
                    }
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
            ApplyWritingActionArt(btn, name, tx);
            return btn;
        }

        void SpawnWritingParagraphRow(int index)
        {
            var go = new GameObject("Para" + index, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(writingParagraphList, false);
            var bg = go.GetComponent<Image>();
            bg.color = Color.white;
            go.GetComponent<LayoutElement>().flexibleHeight = 1f;
            int captured = index;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                writingFocusParagraph = captured;
                writingFocusMatId = null;
                RefreshWritingMaterialsBoard();
            });

            var numTab = CreateImage(go.transform, "NumTab", Color.white);
            Stretch(numTab.rectTransform, new Vector2(0.02f, 0.16f), new Vector2(0.22f, 0.84f),
                Vector2.zero, Vector2.zero);
            numTab.raycastTarget = false;
            numTab.preserveAspect = true;

            var num = CreateUiText(numTab.transform, "Num", 16, TextAnchor.MiddleCenter,
                MaterialCardArt.Ink, Vector2.zero, Vector2.zero);
            StretchFull(num.rectTransform);
            num.fontStyle = FontStyles.Bold;
            num.enableWordWrapping = false;
            num.text = (index + 1).ToString("00");

            float scale = GameSettings.FontSizeScale;
            var label = CreateUiText(go.transform, "Label", Mathf.RoundToInt(16f * scale),
                TextAnchor.MiddleLeft, MaterialCardArt.Ink, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform, new Vector2(0.26f, 0.08f), new Vector2(0.86f, 0.92f), Vector2.zero, Vector2.zero);
            label.enableAutoSizing = false;
            label.enableWordWrapping = true;
            label.text = UiLoc.T(WmParagraphKeys[index], WmParagraphFallback[index]);
            ApplyLetterSpacing(label, 0f);
            var tag = label.gameObject.AddComponent<LocTag>();
            tag.key = WmParagraphKeys[index];
            tag.target = label;

            var chevron = CreateUiText(go.transform, "Chevron", 18, TextAnchor.MiddleCenter,
                MaterialCardArt.Ink, Vector2.zero, Vector2.zero);
            Stretch(chevron.rectTransform, new Vector2(0.86f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);
            chevron.enableWordWrapping = false;
            chevron.text = ">";
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

        /// <summary>Play Mode debug: every catalog card becomes selectable on the board.</summary>
        public int UnlockAllMaterials()
        {
            var gs = GameState.Instance;
            if (gs == null) return 0;
            int added = 0;
            foreach (var card in MaterialCatalog.All)
            {
                if (card == null || string.IsNullOrEmpty(card.id)) continue;
                if (gs.Data.unlockedMaterials.Contains(card.id)) continue;
                gs.UnlockMaterial(card.id);
                added++;
            }
            if (writingMatsRoot != null && writingMatsRoot.activeSelf)
                RefreshWritingMaterialsBoard();
            if (writingDeskRoot != null && writingDeskRoot.activeSelf)
                RefreshWritingDesk();
            SaveSystem.Autosave();
            return gs.Data.unlockedMaterials.Count;
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
            ApplyWritingBoardSkin();
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
            // Tape art-pack sprite already bakes the chapter title — don't resurrect TMP over it.
            if (writingTapeTitle != null && writingTapeTitle.gameObject.activeSelf)
            {
                var title = UiLoc.T("ui.writing.tape_title", "第一章 写稿 / 素材卡库");
                writingTapeTitle.text = title.Replace(" / ", "\n");
            }
            foreach (var tag in writingMatsRoot.GetComponentsInChildren<LocTag>(true))
            {
                if (tag == null || string.IsNullOrEmpty(tag.key)) continue;
                var tx = tag.target != null ? tag.target : tag.GetComponentInChildren<TextMeshProUGUI>();
                if (tx != null && tx.gameObject.activeSelf) tx.text = UiLoc.T(tag.key);
            }
            // Re-apply GoWrite art so EN doesn't re-show a doubled TMP caption.
            if (writingGoBtn != null)
            {
                var goLabel = writingGoBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                ApplyWritingActionArt(writingGoBtn, "GoWriteBtn", goLabel);
            }
            if (writingPreviewBtn != null)
            {
                var previewLabel = writingPreviewBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                ApplyWritingActionArt(writingPreviewBtn, "PreviewBtn", previewLabel);
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
                ApplyWritingDotArt(writingDotImages[i], i < selected);
                if (writingDotImages[i].sprite == null)
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
                var bg = row.GetComponent<Image>();
                if (bg != null)
                {
                    var plate = on ? MaterialCardArt.SectionSelected : MaterialCardArt.SectionNormal;
                    if (plate != null)
                    {
                        bg.sprite = plate;
                        bg.color = Color.white;
                        bg.type = Image.Type.Simple;
                        bg.preserveAspect = false;
                    }
                    else
                    {
                        bg.sprite = null;
                        bg.color = on ? WmOrange : WmPaper;
                    }
                }

                var numTab = row.Find("NumTab");
                if (numTab != null)
                {
                    var img = numTab.GetComponent<Image>();
                    var tab = on ? MaterialCardArt.NumberSelected : MaterialCardArt.NumberNormal;
                    if (img != null && tab != null)
                    {
                        img.sprite = tab;
                        img.color = Color.white;
                        img.preserveAspect = true;
                    }
                    var num = numTab.Find("Num");
                    if (num != null)
                    {
                        var ntx = num.GetComponent<TextMeshProUGUI>();
                        if (ntx != null)
                            ntx.color = on ? MaterialCardArt.Cream : MaterialCardArt.Ink;
                    }
                }

                var label = row.Find("Label");
                if (label != null)
                {
                    var tx = label.GetComponent<TextMeshProUGUI>();
                    if (tx != null)
                    {
                        tx.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
                        tx.color = on ? MaterialCardArt.Cream : MaterialCardArt.Ink;
                        tx.text = ParagraphRowTitle(UiLoc.T(WmParagraphKeys[i], WmParagraphFallback[i]));
                    }
                }

                var chevron = row.Find("Chevron");
                if (chevron != null)
                {
                    var ctx = chevron.GetComponent<TextMeshProUGUI>();
                    if (ctx != null)
                        ctx.color = on ? MaterialCardArt.Cream : MaterialCardArt.Ink;
                }
            }
        }

        static string ParagraphRowTitle(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var s = raw.Trim();
            for (int n = 1; n <= 4; n++)
            {
                string num = n.ToString("00");
                int at = s.IndexOf(num, System.StringComparison.Ordinal);
                if (at >= 0 && at <= 8)
                    return s.Substring(at + num.Length).Trim();
            }
            return s;
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

            for (; idx < 6; idx++)
                SpawnWritingDecoSticky(idx);
        }

        void SpawnWritingDecoSticky(int visualIndex)
        {
            var go = new GameObject("DecoSticky", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(writingCardGrid, false);
            writingSpawned.Add(go);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.white;
            ApplyWritingCardArt(img, visualIndex);
        }

        void SpawnWritingMaterialCard(MaterialCard m, bool unlocked, bool selected, bool focus, int visualIndex)
        {
            var go = new GameObject(m.id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(writingCardGrid, false);
            writingSpawned.Add(go);

            var bg = go.GetComponent<Image>();
            bg.color = Color.white;
            ApplyWritingCardArt(bg, visualIndex);
            if (bg.sprite == null)
                bg.color = unlocked ? ColorForMaterialType(m.type, visualIndex) : WmLocked;
            else if (!unlocked)
                bg.color = new Color(0.72f, 0.72f, 0.72f, 1f);
            else
                bg.preserveAspect = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            string mid = m.id;
            btn.onClick.AddListener(() => OnWritingCardClicked(mid));

            float scale = GameSettings.FontSizeScale;
            int Sz(float baseSize) => Mathf.RoundToInt(baseSize * scale);

            var titleTx = CreateUiText(go.transform, "Title", Sz(22), TextAnchor.MiddleCenter, WmInk, Vector2.zero, Vector2.zero);
            Stretch(titleTx.rectTransform, new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.72f), Vector2.zero, Vector2.zero);
            titleTx.fontStyle = focus ? FontStyles.Bold : FontStyles.Normal;
            titleTx.enableWordWrapping = true;
            titleTx.overflowMode = TextOverflowModes.Truncate;
            titleTx.enableAutoSizing = false;
            titleTx.text = unlocked ? HardTextLoc.T(m.title) : "???";
            ApplyLetterSpacing(titleTx, 0f);

            if (selected)
            {
                var mark = CreateImage(go.transform, "Picked", Color.white);
                var mrt = mark.rectTransform;
                mrt.anchorMin = mrt.anchorMax = new Vector2(0.84f, 0.14f);
                mrt.pivot = new Vector2(0.5f, 0.5f);
                mrt.sizeDelta = new Vector2(28f, 28f);
                mark.raycastTarget = false;
                ApplyWritingDotArt(mark, true);
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

        void SetDetailDecor(bool visible)
        {
            if (writingMatsRoot == null) return;
            var underline = writingMatsRoot.transform.Find("DetailPaper/Underline");
            if (underline != null) underline.gameObject.SetActive(visible);
            var separator = writingMatsRoot.transform.Find("DetailPaper/Separator");
            if (separator != null) separator.gameObject.SetActive(visible);
        }

        void RefreshWritingDetailPanel()
        {
            if (writingDetailTitle == null) return;
            var m = MaterialCatalog.Get(writingFocusMatId);
            if (m == null)
            {
                writingDetailTitle.text = UiLoc.T("ui.writing.detail_empty", "点选一张素材卡");
                if (writingDetailId != null) writingDetailId.text = "";
                var idTag = writingDetailId != null ? writingDetailId.transform.parent : null;
                if (idTag != null) idTag.gameObject.SetActive(false);
                if (writingDetailTag != null) writingDetailTag.text = "";
                if (writingDetailTagBg != null) writingDetailTagBg.gameObject.SetActive(false);
                SetDetailDecor(false);
                if (writingDetailSource != null) writingDetailSource.text = "";
                if (writingDetailBody != null) writingDetailBody.text = "";
                return;
            }

            bool unlocked = GameState.Instance != null &&
                            GameState.Instance.Data.unlockedMaterials.Contains(m.id);
            if (writingDetailId != null)
            {
                writingDetailId.text = m.id;
                if (writingDetailId.transform.parent != null)
                    writingDetailId.transform.parent.gameObject.SetActive(true);
            }
            writingDetailTitle.text = unlocked ? HardTextLoc.T(m.title) : "???";
            SetDetailDecor(true);
            if (writingDetailTagBg != null)
            {
                writingDetailTagBg.gameObject.SetActive(true);
                if (writingDetailTagBg.sprite == null)
                    writingDetailTagBg.color = unlocked ? WmOrange : WmLocked;
                else
                    writingDetailTagBg.color = Color.white;
            }
            if (writingDetailTag != null)
            {
                writingDetailTag.text = MaterialTypeLabel(m.type);
                writingDetailTag.color = MaterialCardArt.Ink;
            }
            if (writingDetailSource != null)
            {
                var src = unlocked
                    ? (UiLoc.T("ui.writing.source_prefix", "来源：") + MaterialSourceLabel(m))
                    : UiLoc.T("ui.writing.source_locked", "来源：未解锁");
                writingDetailSource.text = src;
            }
            if (writingDetailBody != null)
                writingDetailBody.text = unlocked
                    ? HardTextLoc.T(m.body)
                    : UiLoc.T("ui.writing.detail_locked", "继续采访与调查后，这张素材才会解锁。");
        }

        void OnWritingPreviewArticle()
        {
            if (writingPreviewRoot == null) return;
            var assembler = new ArticleAssembler();
            if (!assembler.CanAssemble(pendingDir, selectedMats, out _))
            {
                if (writingStatusHint != null)
                    writingStatusHint.text = UiLoc.T("ui.writing.preview_blocked", "还不能预览成稿。");
                return;
            }

            assembler.Assemble(pendingDir, selectedMats);
            if (writingPreviewBody != null)
            {
                var note = UiLoc.T("ui.writing.preview_note", "（预览稿；提交后由沈河评分。）");
                writingPreviewBody.text = (assembler.Title ?? "") + "\n\n" + (assembler.Body ?? "") + "\n\n" + note;
            }
            writingPreviewRoot.SetActive(true);
            writingPreviewRoot.transform.SetAsLastSibling();
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
            stageHint.text = UiLoc.T("ui.writing.stage_hint", "写稿");
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            portraitDebugBeatSourceId = "direction_pick";
            NotifyPortraitDebugLineChanged();
#endif
            SetSpeaker("沈禾", LineSpeaker.Character, "认真");
            var unlocked = GameState.Instance.Data.unlockedMaterials.Count;
            var body = string.Format(
                UiLoc.T("ui.writing.pick_body",
                    "选一个报道立意。素材决定你能写什么，立意决定你想讲什么。\n\n已解锁素材 {0} 张。"),
                unlocked);
            if (unlocked < 8)
                body += UiLoc.T("ui.writing.pick_body_low",
                    "\n\n素材还不够成稿。如果采访里还有没问到的，可以回去补充。");
            SetBody(body);
            ClearButtons();
            AddChoice(
                ArticleAssembler.TitleFor(WritingDirection.GuardCatToday) + "　"
                + UiLoc.T("ui.writing.pick_guard_blurb", "从流浪猫到社区保安"),
                () =>
                {
                    pendingDir = WritingDirection.GuardCatToday;
                    ShowMaterialPick();
                });
            AddChoice(
                ArticleAssembler.TitleFor(WritingDirection.RescueWithoutAdoption) + "　"
                + UiLoc.T("ui.writing.pick_rescue_blurb", "一次没有以收养结束的救助"),
                () =>
                {
                    pendingDir = WritingDirection.RescueWithoutAdoption;
                    ShowMaterialPick();
                });
            AddReInterviewActions(unlocked < 8);
            AddAction(UiLoc.T("ui.notebook", "笔记"), OpenNotebook);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                portraitDebugBeatSourceId = "assemble_fail";
                NotifyPortraitDebugLineChanged();
#endif
                SetSpeaker("沈禾", LineSpeaker.Character, "认真");
                SetBody(string.Format(
                    UiLoc.T("ui.writing.cant_assemble",
                        "现在还不能成稿。\n\n{0}\n\n可以改选材，或返回采访补齐素材。"),
                    HardTextLoc.T(err)));
                statusText.text = HardTextLoc.T(err);
                ClearButtons();
                AddAction(UiLoc.T("ui.writing.back_reselect_mats", "返回改选材"), ShowMaterialPick, true);
                AddAction(UiLoc.T("ui.writing.reselect_dir", "重选立意"), ShowWritingDirectionPick);
                AddReInterviewActions(true);
                AddAction(UiLoc.T("ui.notebook", "笔记"), OpenNotebook);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            portraitDebugBeatSourceId = "submit_review";
            NotifyPortraitDebugLineChanged();
#endif
            SetSpeaker("沈禾", LineSpeaker.Character, "认真");
            // Body already edited on the desk — do not re-dump the full article here.
            SetBody(UiLoc.T("ui.writing.submitted", "稿件已提交。正在送审…"));
            statusText.text = UiLoc.T("ui.writing.reviewing", "审核中…");
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            portraitDebugBeatSourceId = "review_result";
            NotifyPortraitDebugLineChanged();
#endif
            SetSpeaker("沈禾", LineSpeaker.Character, assembler.CanPublish ? "淡淡认可" : "认真");
            // Review / score only — full article stays on the writing desk.
            SetBody(string.Format(
                UiLoc.T("ui.writing.review_header", "—— 沈禾审核 ——\n{0}\n\n评分　{1}"),
                HardTextLoc.T(assembler.ReviewText),
                assembler.Score));
            statusText.text = assembler.CanPublish
                ? string.Format(UiLoc.T("ui.writing.pass_fmt", "审核通过　{0}"), assembler.Score)
                : string.Format(UiLoc.T("ui.writing.fail_fmt", "审核退回　分支{0}"), assembler.ReviewBranch);
            ClearButtons();
            if (assembler.CanPublish)
                AddAction(UiLoc.T("ui.writing.confirm_publish", "确认发布"),
                    () => ChapterFlowController.Instance.OnArticlePublished(), true);
            else
            {
                AddAction(UiLoc.T("ui.writing.back_to_write", "返回写稿"), ShowMaterialPick, true);
                AddAction(UiLoc.T("ui.writing.view_notebook", "查看记者笔记"), OpenNotebook);
                AddReInterviewActions(true);
            }
            AddAction(UiLoc.T("ui.writing.reselect_dir", "重选立意"), ShowWritingDirectionPick);
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
                AddAction(
                    highlight
                        ? UiLoc.T("ui.writing.reinterview_any", "重新采访…")
                        : UiLoc.T("ui.writing.back_interview", "返回采访"),
                    ShowReInterviewMenu, highlight);
                return;
            }

            if (dafuDone)
            {
                AddAction(
                    highlight
                        ? UiLoc.T("ui.writing.reinterview_dafu", "重新采访大福")
                        : UiLoc.T("ui.writing.back_interview_dafu", "返回采访大福"),
                    () => ChapterFlowController.Instance.BeginReInterview(InterviewSubject.Dafu),
                    highlight);
            }
            if (linDone)
            {
                AddAction(
                    highlight
                        ? UiLoc.T("ui.writing.reinterview_lin", "重新采访林女士")
                        : UiLoc.T("ui.writing.back_interview_lin", "返回采访林女士"),
                    () => ChapterFlowController.Instance.BeginReInterview(InterviewSubject.Lin),
                    highlight);
            }
        }

        void ShowReInterviewMenu()
        {
            writingMatsActive = false;
            HideWritingMaterialsBoard();
            SetChrome(true, false, true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            portraitDebugBeatSourceId = "reinterview_menu";
            NotifyPortraitDebugLineChanged();
#endif
            SetSpeaker("系统", LineSpeaker.System);
            SetBody(UiLoc.T("ui.writing.reinterview_menu",
                "写稿素材不够时，可以回去补充采访。已获得的情报与素材卡会保留。\n\n要重新采访谁？"));
            statusText.text = UiLoc.T("ui.writing.reinterview_status", "补充采访");
            ClearButtons();
            if (GameState.Instance.HasFlag(FlagIds.DafuInterviewDone))
                AddChoice(UiLoc.T("ui.writing.reinterview_dafu", "重新采访大福"), () =>
                    ChapterFlowController.Instance.BeginReInterview(InterviewSubject.Dafu));
            if (GameState.Instance.HasFlag(FlagIds.LinInterviewDone))
                AddChoice(UiLoc.T("ui.writing.reinterview_lin", "重新采访林女士"), () =>
                    ChapterFlowController.Instance.BeginReInterview(InterviewSubject.Lin));
            AddAction(UiLoc.T("ui.writing.back_to_write", "返回写稿"), () =>
            {
                if (selectedMats.Count > 0 || writingMatsActive)
                    ShowMaterialPick();
                else
                    ShowWritingDirectionPick();
            }, true);
            AddAction(UiLoc.T("ui.notebook", "笔记"), OpenNotebook);
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
