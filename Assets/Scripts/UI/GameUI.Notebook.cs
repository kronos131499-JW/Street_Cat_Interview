using System.Text;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Loc;
using StreetCat.Notebook;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Scrapbook / investigative reporter notebook overlay.
    /// </summary>
    public partial class GameUI
    {
        static readonly Color NbInk = new Color(0.14f, 0.12f, 0.10f, 1f);
        static readonly Color NbInkMuted = new Color(0.32f, 0.28f, 0.24f, 1f);
        static readonly Color NbTitleMint = new Color(0.72f, 0.86f, 0.70f, 1f);
        static readonly Color NbPaper = new Color(0.94f, 0.89f, 0.74f, 1f);
        static readonly Color NbInspire = new Color(0.93f, 0.62f, 0.28f, 1f);
        static readonly Color NbNavy = new Color(0.10f, 0.14f, 0.22f, 1f);

        static readonly Color[] NbStickyColors =
        {
            new Color(0.55f, 0.68f, 0.48f, 1f), // green
            new Color(0.86f, 0.78f, 0.42f, 1f), // yellow
            new Color(0.86f, 0.62f, 0.38f, 1f), // orange
            new Color(0.62f, 0.60f, 0.56f, 1f), // grey
            new Color(0.62f, 0.52f, 0.68f, 1f), // purple
            new Color(0.48f, 0.62f, 0.74f, 1f), // blue
        };

        static readonly string[] NbTopicOrder =
        {
            "community", "past", "neck", "rescuer", "after", "return"
        };

        static readonly string[] NbStickyIcons =
        {
            "ch_dafu_relaxed", "ch_dafu_recall", "ch_dafu_annoyed",
            "ch_lin_recall", "ch_dafu_curious", "ch_lin_firm"
        };

        void BuildNotebookOverlay(Transform parent)
        {
            notebookRoot = new GameObject("NotebookOverlay", typeof(RectTransform));
            notebookRoot.transform.SetParent(parent, false);
            StretchFull(notebookRoot.GetComponent<RectTransform>());

            var dim = CreateImage(notebookRoot.transform, "Dim", new Color(0.02f, 0.03f, 0.05f, 0.82f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var desk = CreateImage(notebookRoot.transform, "Desk", NbNavy);
            Stretch(desk.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            ApplyNotebookDeskTexture(desk);

            // —— Left column ——
            var left = new GameObject("LeftColumn", typeof(RectTransform));
            left.transform.SetParent(desk.transform, false);
            Stretch(left.GetComponent<RectTransform>(), new Vector2(0.02f, 0.04f), new Vector2(0.34f, 0.96f), Vector2.zero, Vector2.zero);

            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(left.transform, false);
            Stretch(header.GetComponent<RectTransform>(), new Vector2(0.04f, 0.88f), new Vector2(0.96f, 1f), Vector2.zero, Vector2.zero);

            var catIcon = CreateImage(header.transform, "CatIcon", Color.white);
            var cir = catIcon.rectTransform;
            cir.anchorMin = cir.anchorMax = new Vector2(0f, 0.5f);
            cir.pivot = new Vector2(0f, 0.5f);
            cir.anchoredPosition = new Vector2(4f, 0f);
            cir.sizeDelta = new Vector2(44f, 44f);
            var catSpr = VnArt.GetPortrait("ch_dafu_curious");
            if (catSpr != null)
            {
                catIcon.sprite = catSpr;
                catIcon.preserveAspect = true;
                catIcon.color = new Color(0.55f, 0.78f, 0.50f, 1f);
            }
            else
            {
                catIcon.color = new Color(0.45f, 0.72f, 0.42f, 1f);
            }

            notebookTitleText = CreateUiText(header.transform, "Title", 28, TextAnchor.MiddleLeft,
                NbTitleMint, Vector2.zero, Vector2.zero);
            Stretch(notebookTitleText.rectTransform, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(56f, 0f), new Vector2(-4f, 0f));
            notebookTitleText.fontStyle = FontStyle.Bold;
            notebookTitleText.text = UiLoc.T("ui.notebook.title", "记者笔记");

            var gridHost = new GameObject("StickyGridHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            gridHost.transform.SetParent(left.transform, false);
            Stretch(gridHost.GetComponent<RectTransform>(), new Vector2(0.02f, 0.14f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero);
            gridHost.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            var gridScroll = gridHost.GetComponent<ScrollRect>();
            gridScroll.horizontal = false;
            gridScroll.movementType = ScrollRect.MovementType.Clamped;

            var gridVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            gridVp.transform.SetParent(gridHost.transform, false);
            StretchFull(gridVp.GetComponent<RectTransform>());
            gridVp.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);

            var gridContent = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridContent.transform.SetParent(gridVp.transform, false);
            notebookStickyGrid = gridContent.transform;
            var gcrt = gridContent.GetComponent<RectTransform>();
            gcrt.anchorMin = new Vector2(0, 1);
            gcrt.anchorMax = new Vector2(1, 1);
            gcrt.pivot = new Vector2(0.5f, 1);
            gcrt.sizeDelta = Vector2.zero;
            var grid = gridContent.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(118f, 128f);
            grid.spacing = new Vector2(14f, 16f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            gridContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            gridScroll.viewport = gridVp.GetComponent<RectTransform>();
            gridScroll.content = gcrt;

            notebookModeRow = new GameObject("ModeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            notebookModeRow.SetParent(left.transform, false);
            Stretch(notebookModeRow.GetComponent<RectTransform>(), new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero);
            var mh = notebookModeRow.GetComponent<HorizontalLayoutGroup>();
            mh.spacing = 8;
            mh.childForceExpandWidth = true;
            mh.childForceExpandHeight = true;
            mh.childControlWidth = true;
            mh.childControlHeight = true;
            SpawnNotebookModeButton(UiLoc.T("ui.notebook.tab_gaps", "待确认"), 1);
            SpawnNotebookModeButton(UiLoc.T("ui.notebook.tab_qa", "提问记录"), 2);

            // —— Right notebook page ——
            var pageShadow = CreateImage(desk.transform, "PageShadow", new Color(0f, 0f, 0f, 0.35f));
            Stretch(pageShadow.rectTransform, new Vector2(0.355f, 0.055f), new Vector2(0.955f, 0.945f), Vector2.zero, Vector2.zero);

            notebookPageImage = CreateImage(desk.transform, "NotebookPage", NbPaper);
            Stretch(notebookPageImage.rectTransform, new Vector2(0.36f, 0.07f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            EnsureLinedPaperSprite();
            if (notebookLinedPaperSprite != null)
            {
                notebookPageImage.sprite = notebookLinedPaperSprite;
                notebookPageImage.type = Image.Type.Tiled;
                notebookPageImage.color = Color.white;
            }

            BuildNotebookSpiral(notebookPageImage.transform);

            var doodle = CreateImage(notebookPageImage.transform, "PageDoodle", new Color(0.55f, 0.48f, 0.40f, 0.35f));
            var drt = doodle.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(1f, 1f);
            drt.pivot = new Vector2(1f, 1f);
            drt.anchoredPosition = new Vector2(-18f, -14f);
            drt.sizeDelta = new Vector2(72f, 72f);
            var doodleSpr = VnArt.GetPortrait("ch_dafu_relaxed");
            if (doodleSpr != null)
            {
                doodle.sprite = doodleSpr;
                doodle.preserveAspect = true;
            }
            doodle.raycastTarget = false;

            notebookDetailTitleText = CreateUiText(notebookPageImage.transform, "TopicTitle", 30, TextAnchor.UpperLeft,
                NbInk, Vector2.zero, Vector2.zero);
            Stretch(notebookDetailTitleText.rectTransform, new Vector2(0, 0.88f), new Vector2(0.62f, 0.98f),
                new Vector2(48f, 0f), new Vector2(-8f, -8f));
            notebookDetailTitleText.fontStyle = FontStyle.Bold;
            notebookDetailTitleText.verticalOverflow = VerticalWrapMode.Overflow;

            notebookStatusChipBg = CreateImage(notebookPageImage.transform, "StatusChip", new Color(0.96f, 0.92f, 0.78f, 0.92f));
            Stretch(notebookStatusChipBg.rectTransform, new Vector2(0.62f, 0.90f), new Vector2(0.92f, 0.97f),
                new Vector2(0f, 0f), new Vector2(-80f, -10f));
            notebookStatusChipText = CreateUiText(notebookStatusChipBg.transform, "Label", 16, TextAnchor.MiddleCenter,
                NbInkMuted, Vector2.zero, Vector2.zero);
            StretchFull(notebookStatusChipText.rectTransform);
            notebookStatusChipText.fontStyle = FontStyle.Bold;

            var detailHost = new GameObject("DetailHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            detailHost.transform.SetParent(notebookPageImage.transform, false);
            Stretch(detailHost.GetComponent<RectTransform>(), new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.86f),
                new Vector2(36f, 0f), new Vector2(-12f, 0f));
            detailHost.GetComponent<Image>().color = new Color(1, 1, 1, 0.001f);
            notebookDetailScroll = detailHost.GetComponent<ScrollRect>();
            notebookDetailScroll.horizontal = false;
            notebookDetailScroll.movementType = ScrollRect.MovementType.Clamped;
            notebookDetailScroll.scrollSensitivity = 28f;

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
            notebookDetailBodyText = dContent.AddComponent<Text>();
            notebookDetailBodyText.font = font;
            notebookDetailBodyText.fontSize = 21;
            notebookDetailBodyText.color = NbInk;
            notebookDetailBodyText.alignment = TextAnchor.UpperLeft;
            notebookDetailBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            notebookDetailBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            notebookDetailBodyText.lineSpacing = 1.55f;
            notebookDetailBodyText.raycastTarget = false;
            notebookDetailScroll.viewport = dVp.GetComponent<RectTransform>();
            notebookDetailScroll.content = dcrt;

            notebookSourceText = CreateUiText(notebookPageImage.transform, "Source", 16, TextAnchor.MiddleLeft,
                NbInkMuted, Vector2.zero, Vector2.zero);
            Stretch(notebookSourceText.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.72f, 0.20f),
                new Vector2(36f, 0f), new Vector2(0f, 0f));

            // —— Inspiration sticky (bottom-right overlay) ——
            var inspireHost = new GameObject("InspireHost", typeof(RectTransform));
            inspireHost.transform.SetParent(notebookPageImage.transform, false);
            Stretch(inspireHost.GetComponent<RectTransform>(), new Vector2(0.52f, 0.03f), new Vector2(0.97f, 0.22f),
                Vector2.zero, Vector2.zero);

            var inspireShadow = CreateImage(inspireHost.transform, "Shadow", new Color(0, 0, 0, 0.28f));
            StretchFull(inspireShadow.rectTransform);
            inspireShadow.rectTransform.anchoredPosition = new Vector2(5f, -6f);
            inspireShadow.raycastTarget = false;

            var inspireGo = new GameObject("InspireSticky", typeof(RectTransform), typeof(Image), typeof(Button));
            inspireGo.transform.SetParent(inspireHost.transform, false);
            StretchFull(inspireGo.GetComponent<RectTransform>());
            notebookInspirePanel = inspireGo.GetComponent<Image>();
            notebookInspirePanel.color = NbInspire;
            notebookInspireButton = inspireGo.GetComponent<Button>();
            notebookInspireButton.targetGraphic = notebookInspirePanel;
            notebookInspireButton.onClick.AddListener(UseNotebookInspiration);

            var clip = CreateImage(inspireGo.transform, "Paperclip", Color.white);
            var clipRt = clip.rectTransform;
            clipRt.anchorMin = clipRt.anchorMax = new Vector2(1f, 0.72f);
            clipRt.pivot = new Vector2(0.5f, 0.5f);
            clipRt.anchoredPosition = new Vector2(6f, 10f);
            clipRt.sizeDelta = new Vector2(36f, 52f);
            var clipSpr = VnArt.GetTitle("deco_paperclip");
            if (clipSpr != null)
            {
                clip.sprite = clipSpr;
                clip.preserveAspect = true;
            }
            else
            {
                clip.color = new Color(0.75f, 0.78f, 0.82f, 0.9f);
            }
            clip.raycastTarget = false;

            var bulb = CreateImage(inspireGo.transform, "Bulb", new Color(1f, 0.95f, 0.75f, 1f));
            var brt = bulb.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(12f, -10f);
            brt.sizeDelta = new Vector2(22f, 22f);
            bulb.raycastTarget = false;

            notebookInspireHeaderText = CreateUiText(inspireGo.transform, "InspireHeader", 17, TextAnchor.MiddleLeft,
                Color.white, Vector2.zero, Vector2.zero);
            Stretch(notebookInspireHeaderText.rectTransform, new Vector2(0, 0.62f), new Vector2(1, 0.95f),
                new Vector2(40f, 0f), new Vector2(-28f, -4f));
            notebookInspireHeaderText.fontStyle = FontStyle.Bold;
            notebookInspireHeaderText.text = UiLoc.T("ui.notebook.inspiration", "提问灵感");

            notebookInspireBodyText = CreateUiText(inspireGo.transform, "InspireBody", 16, TextAnchor.UpperLeft,
                new Color(0.18f, 0.12f, 0.06f, 1f), Vector2.zero, Vector2.zero);
            Stretch(notebookInspireBodyText.rectTransform, new Vector2(0, 0.05f), new Vector2(1, 0.62f),
                new Vector2(14f, 8f), new Vector2(-18f, -4f));
            notebookInspireBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            notebookInspireBodyText.verticalOverflow = VerticalWrapMode.Truncate;
            notebookInspireBodyText.lineSpacing = 1.25f;

            // Close button
            var closeBtn = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(desk.transform, false);
            var cbrt = closeBtn.GetComponent<RectTransform>();
            cbrt.anchorMin = cbrt.anchorMax = new Vector2(1, 1);
            cbrt.pivot = new Vector2(1, 1);
            cbrt.anchoredPosition = new Vector2(-10f, -10f);
            cbrt.sizeDelta = new Vector2(96f, 36f);
            closeBtn.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.24f, 0.92f);
            closeBtn.GetComponent<Button>().onClick.AddListener(CloseNotebook);
            notebookCloseLabel = CreateUiText(closeBtn.transform, "T", 17, TextAnchor.MiddleCenter,
                NbTitleMint, Vector2.zero, Vector2.zero);
            StretchFull(notebookCloseLabel.rectTransform);
            notebookCloseLabel.text = UiLoc.T("ui.notebook.close", "关闭");

            notebookRoot.SetActive(false);
        }

        void SpawnNotebookModeButton(string label, int tabIdx)
        {
            var go = new GameObject("Mode" + tabIdx, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(notebookModeRow, false);
            go.GetComponent<Image>().color = new Color(0.16f, 0.20f, 0.28f, 0.95f);
            go.GetComponent<LayoutElement>().preferredHeight = 36;
            int captured = tabIdx;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                notebookTab = captured;
                notebookSelectedTopicId = null;
                RefreshNotebookPanel();
            });
            var tx = CreateUiText(go.transform, "T", 15, TextAnchor.MiddleCenter, NbTitleMint, Vector2.zero, Vector2.zero);
            StretchFull(tx.rectTransform);
            tx.text = label;
            var tag = go.AddComponent<LocTag>();
            tag.key = tabIdx == 1 ? "ui.notebook.tab_gaps" : "ui.notebook.tab_qa";
            tag.target = tx;
        }

        void BuildNotebookSpiral(Transform page)
        {
            var strip = new GameObject("Spiral", typeof(RectTransform));
            strip.transform.SetParent(page, false);
            Stretch(strip.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(10f, 18f), new Vector2(42f, -18f));

            for (int i = 0; i < 14; i++)
            {
                float t = (i + 0.5f) / 14f;
                var ring = CreateImage(strip.transform, "Ring" + i, new Color(0.12f, 0.12f, 0.14f, 0.92f));
                var rt = ring.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f - t);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(18f, 14f);
                ring.raycastTarget = false;

                var hole = CreateImage(page, "Hole" + i, new Color(0.08f, 0.10f, 0.14f, 0.55f));
                var hrt = hole.rectTransform;
                hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 1f - t);
                hrt.pivot = new Vector2(0.5f, 0.5f);
                hrt.anchoredPosition = new Vector2(34f, 0f);
                hrt.sizeDelta = new Vector2(10f, 10f);
                hole.raycastTarget = false;
            }
        }

        void ApplyNotebookDeskTexture(Image desk)
        {
            var paper = VnArt.GetUi("tex_paper_dark");
            if (paper != null)
            {
                desk.sprite = paper;
                desk.type = Image.Type.Tiled;
                desk.color = new Color(0.55f, 0.65f, 0.85f, 1f);
                return;
            }
            EnsureNavySprite();
            if (notebookNavySprite != null)
            {
                desk.sprite = notebookNavySprite;
                desk.type = Image.Type.Tiled;
                desk.color = Color.white;
            }
        }

        void EnsureLinedPaperSprite()
        {
            if (notebookLinedPaperSprite != null) return;
            const int w = 8;
            const int h = 36;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            var paper = new Color(0.945f, 0.90f, 0.76f, 1f);
            var line = new Color(0.72f, 0.76f, 0.82f, 0.55f);
            for (int y = 0; y < h; y++)
            {
                bool isLine = y == 0 || y == 1;
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, isLine ? line : paper);
            }
            tex.Apply(false, false);
            notebookLinedPaperSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 36f);
        }

        void EnsureNavySprite()
        {
            if (notebookNavySprite != null) return;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.12f, y * 0.12f);
                var c = Color.Lerp(new Color(0.08f, 0.11f, 0.18f), new Color(0.14f, 0.18f, 0.28f), n);
                tex.SetPixel(x, y, c);
            }
            tex.Apply(false, false);
            notebookNavySprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        void ApplyNotebookFonts()
        {
            if (notebookRoot == null || font == null) return;
            float scale = GameSettings.FontSizeScale;
            void Chrome(Text t, int baseSize, bool bold = false)
            {
                if (t == null) return;
                t.font = font;
                t.fontSize = Mathf.RoundToInt(baseSize * scale);
                if (bold) t.fontStyle = FontStyle.Bold;
                ApplyLetterSpacing(t, 0f);
            }
            Chrome(notebookTitleText, 28, true);
            Chrome(notebookCloseLabel, 17);
            Chrome(notebookDetailTitleText, 30, true);
            Chrome(notebookStatusChipText, 16, true);
            Chrome(notebookSourceText, 16);
            Chrome(notebookInspireHeaderText, 17, true);
            Chrome(notebookInspireBodyText, 16);
            if (notebookDetailBodyText != null)
            {
                notebookDetailBodyText.font = font;
                notebookDetailBodyText.fontSize = Mathf.RoundToInt(21f * scale);
                notebookDetailBodyText.lineSpacing = 1.55f;
                notebookDetailBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                ApplyLetterSpacing(notebookDetailBodyText, 0f);
            }
        }

        void RefreshNotebookLocalizedChrome()
        {
            if (notebookTitleText != null)
                notebookTitleText.text = UiLoc.T("ui.notebook.title", "记者笔记");
            if (notebookCloseLabel != null)
                notebookCloseLabel.text = UiLoc.T("ui.notebook.close", "关闭");
            if (notebookInspireHeaderText != null)
                notebookInspireHeaderText.text = UiLoc.T("ui.notebook.inspiration", "提问灵感");
            if (notebookModeRow != null)
            {
                foreach (var tag in notebookModeRow.GetComponentsInChildren<LocTag>(true))
                {
                    if (tag == null || string.IsNullOrEmpty(tag.key)) continue;
                    var tx = tag.target != null ? tag.target : tag.GetComponentInChildren<Text>();
                    if (tx != null) tx.text = UiLoc.T(tag.key);
                }
            }
            if (notebookRoot != null && notebookRoot.activeSelf)
                RefreshNotebookPanel();
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
            if (returnFromOverlay != Mode.Investigate && returnFromOverlay != Mode.Interview)
            {
                SetInvestigateChrome(false);
                SetInterviewChrome(false);
            }

            ReporterNotebook.Instance?.RefreshFromState();
            notebookTab = 0;
            notebookSelectedTopicId = null;
            RefreshNotebookLocalizedChrome();
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

        void RefreshNotebookModeButtons()
        {
            if (notebookModeRow == null) return;
            for (int i = 0; i < notebookModeRow.childCount; i++)
            {
                var child = notebookModeRow.GetChild(i);
                var img = child.GetComponent<Image>();
                if (img == null) continue;
                bool on = (i == 0 && notebookTab == 1) || (i == 1 && notebookTab == 2);
                img.color = on
                    ? new Color(0.28f, 0.36f, 0.28f, 0.98f)
                    : new Color(0.16f, 0.20f, 0.28f, 0.95f);
            }
        }

        void RefreshNotebookPanel()
        {
            if (notebookRoot == null || !notebookRoot.activeSelf) return;
            var nb = ReporterNotebook.Instance;
            ClearNotebookSpawned();
            RefreshNotebookModeButtons();

            if (nb == null)
            {
                SetNotebookPageContent(
                    UiLoc.T("ui.notebook.title", "记者笔记"),
                    "",
                    UiLoc.T("ui.notebook.not_ready", "记者笔记尚未初始化。"),
                    "");
                SetNotebookInspiration(UiLoc.T("ui.notebook.inspire_none", "暂无提问灵感"), false);
                return;
            }

            // Always rebuild sticky grid from visible topics.
            bool any = false;
            foreach (var t in nb.VisibleTopics())
            {
                any = true;
                if (notebookTab == 0 && string.IsNullOrEmpty(notebookSelectedTopicId))
                    notebookSelectedTopicId = t.id;
                SpawnNotebookSticky(t);
            }

            if (notebookTab == 0)
            {
                if (!any)
                {
                    notebookSelectedTopicId = null;
                    SetNotebookPageContent(
                        UiLoc.T("ui.notebook.title", "记者笔记"),
                        "",
                        UiLoc.T("ui.notebook.empty",
                            "还没有写入笔记。\n\n调查社区、与保安交谈，或开始自由采访后，采访主题会陆续出现在这里。"),
                        "");
                    SetNotebookInspiration(UiLoc.T("ui.notebook.inspire_none", "暂无提问灵感"), false);
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
                sb.AppendLine(UiLoc.T("ui.notebook.gaps_intro", "大福无法解释、或仍需向人类核实的问题。"));
                sb.AppendLine();
                if (gaps.Count == 0)
                    sb.AppendLine(UiLoc.T("ui.notebook.gaps_empty", "（当前没有待确认条目）"));
                else
                {
                    foreach (var g in gaps)
                        sb.AppendLine("> " + g);
                }
                SetNotebookPageContent(
                    UiLoc.T("ui.notebook.tab_gaps", "待确认"),
                    "",
                    sb.ToString().TrimEnd(),
                    "");
                SetNotebookInspiration(UiLoc.T("ui.notebook.gaps_hint", "可向保安 / 林女士核实这些缺口"), false);
            }
            else
            {
                notebookSelectedTopicId = null;
                var sb = new StringBuilder();
                sb.AppendLine(UiLoc.T("ui.notebook.qa_intro", "自由采访中的原问原答记录（与当场台词一致）。"));
                sb.AppendLine();
                var log = nb.QaLog;
                if (log == null || log.Count == 0)
                    sb.AppendLine(UiLoc.T("ui.notebook.qa_empty", "（还没有采访问答记录）"));
                else
                {
                    for (int i = log.Count - 1; i >= 0; i--)
                    {
                        var q = log[i];
                        if (q == null) continue;
                        var topic = nb.Topics.Find(x => x.id == q.topicId);
                        var title = topic != null ? topic.title : UiLoc.T("ui.notebook.uncategorized", "未归类");
                        sb.AppendLine("> " + title);
                        sb.AppendLine(UiLoc.T("ui.notebook.qa_q", "问：") + q.question);
                        AppendQaAnswerLines(sb, q.speaker, q.answerSummary);
                        sb.AppendLine();
                    }
                }
                SetNotebookPageContent(
                    UiLoc.T("ui.notebook.tab_qa", "提问记录"),
                    "",
                    sb.ToString().TrimEnd(),
                    "");
                SetNotebookInspiration(UiLoc.T("ui.notebook.qa_inspire_hint", "在采访主题页可使用提问灵感填入输入框"), false);
            }

            Canvas.ForceUpdateCanvases();
            if (notebookDetailScroll != null)
                notebookDetailScroll.verticalNormalizedPosition = 1f;
        }

        void SpawnNotebookSticky(NotebookTopic topic)
        {
            int colorIdx = StickyColorIndex(topic.id);
            bool selected = notebookTab == 0 && topic.id == notebookSelectedTopicId;
            var color = NbStickyColors[colorIdx % NbStickyColors.Length];
            if (selected)
                color = Color.Lerp(color, Color.white, 0.12f);

            var go = new GameObject(topic.id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(notebookStickyGrid, false);
            go.GetComponent<LayoutElement>().preferredWidth = 118f;
            go.GetComponent<LayoutElement>().preferredHeight = 128f;

            var face = go.GetComponent<Image>();
            face.color = Color.white;

            var tape = CreateImage(go.transform, "Tape", Color.white);
            var trt = tape.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 6f);
            trt.sizeDelta = selected ? new Vector2(70f, 22f) : new Vector2(56f, 18f);
            var tapeKey = selected ? "btn_tape_primary_idle" : "btn_tape_idle";
            var tapeSpr = VnArt.GetTitle(tapeKey);
            if (tapeSpr != null)
            {
                tape.sprite = tapeSpr;
                tape.preserveAspect = true;
                tape.color = Color.white;
            }
            else
            {
                tape.color = new Color(0.92f, 0.88f, 0.72f, 0.75f);
            }
            tape.raycastTarget = false;

            var icon = CreateImage(go.transform, "Icon", new Color(0.12f, 0.10f, 0.08f, 0.82f));
            var irt = icon.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.58f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(54f, 54f);
            var iconSpr = VnArt.GetPortrait(NbStickyIcons[colorIdx % NbStickyIcons.Length]);
            if (iconSpr != null)
            {
                icon.sprite = iconSpr;
                icon.preserveAspect = true;
                icon.color = new Color(0.15f, 0.12f, 0.10f, 0.88f);
            }
            icon.raycastTarget = false;

            var label = CreateUiText(go.transform, "Label", 14, TextAnchor.UpperCenter, NbInk, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.34f), Vector2.zero, Vector2.zero);
            label.text = topic.title;
            label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            string id = topic.id;
            go.GetComponent<Button>().targetGraphic = face;
            var colors = go.GetComponent<Button>().colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
            colors.selectedColor = color;
            go.GetComponent<Button>().colors = colors;
            go.GetComponent<Button>().transition = Selectable.Transition.ColorTint;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                notebookSelectedTopicId = id;
                notebookTab = 0;
                RefreshNotebookPanel();
            });

            if (selected)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0.95f, 0.88f, 0.55f, 0.9f);
                outline.effectDistance = new Vector2(3f, -3f);
                go.transform.localScale = new Vector3(1.04f, 1.04f, 1f);
            }

            notebookSpawned.Add(go);
        }

        static int StickyColorIndex(string topicId)
        {
            for (int i = 0; i < NbTopicOrder.Length; i++)
                if (NbTopicOrder[i] == topicId) return i;
            return 0;
        }

        void ShowNotebookTopicDetail(string topicId)
        {
            var nb = ReporterNotebook.Instance;
            if (nb == null) return;
            var t = nb.Topics.Find(x => x.id == topicId);
            if (t == null)
            {
                SetNotebookPageContent("", "", "", "");
                return;
            }

            var sb = new StringBuilder();
            if (t.notes.Count == 0)
                sb.AppendLine(UiLoc.T("ui.notebook.no_notes", "（该主题尚无具体笔记）"));
            else
            {
                foreach (var n in t.notes)
                    sb.AppendLine("> " + n.text);
            }

            var qa = nb.QaForTopic(t.id);
            if (qa.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(UiLoc.T("ui.notebook.related_qa", "—— 相关提问 ——"));
                int show = Mathf.Min(4, qa.Count);
                for (int i = qa.Count - show; i < qa.Count; i++)
                {
                    var q = qa[i];
                    sb.AppendLine("> " + UiLoc.T("ui.notebook.qa_q", "问：") + q.question);
                    AppendQaAnswerLines(sb, q.speaker, q.answerSummary, indent: "  ");
                }
            }

            string source = nb.SourcesLine(t);
            if (!string.IsNullOrEmpty(source) && source.StartsWith("来源："))
                source = UiLoc.T("ui.notebook.source_prefix", "来源：") + source.Substring("来源：".Length);
            else if (string.IsNullOrEmpty(source))
                source = "";

            SetNotebookPageContent(t.title, LocalizedStatusLabel(t.status), sb.ToString().TrimEnd(), source);
            UpdateInspirationForTopic(t);
        }

        void UpdateInspirationForTopic(NotebookTopic t)
        {
            if (t == null)
            {
                SetNotebookInspiration(UiLoc.T("ui.notebook.inspire_none", "暂无提问灵感"), false);
                return;
            }

            if (t.status == TopicStatus.Complete || string.IsNullOrEmpty(t.inspiration))
            {
                SetNotebookInspiration(UiLoc.T("ui.notebook.inspire_complete", "主要事实已足够，可继续提问但不主动提示"), false);
                return;
            }

            if (t.inspirationIsInvestigate)
            {
                SetNotebookInspiration(t.inspiration, false);
                return;
            }

            SetNotebookInspiration(t.inspiration, true);
        }

        static void AppendQaAnswerLines(StringBuilder sb, string speaker, string answer, string indent = "")
        {
            if (sb == null) return;
            if (string.IsNullOrEmpty(answer))
            {
                sb.AppendLine(indent + (speaker ?? "") + "：……");
                return;
            }

            var parts = answer.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            bool first = true;
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                if (first)
                {
                    sb.AppendLine(indent + (speaker ?? "") + "：" + part.Trim());
                    first = false;
                }
                else
                {
                    sb.AppendLine(indent + part.Trim());
                }
            }
            if (first)
                sb.AppendLine(indent + (speaker ?? "") + "：……");
        }

        void SetNotebookPageContent(string title, string status, string body, string source)
        {
            if (notebookDetailTitleText != null)
                notebookDetailTitleText.text = title ?? "";
            if (notebookStatusChipText != null)
                notebookStatusChipText.text = status ?? "";
            if (notebookStatusChipBg != null)
                notebookStatusChipBg.gameObject.SetActive(!string.IsNullOrEmpty(status));
            if (notebookDetailBodyText != null)
                notebookDetailBodyText.text = body ?? "";
            if (notebookSourceText != null)
                notebookSourceText.text = source ?? "";
        }

        void SetNotebookInspiration(string body, bool clickable)
        {
            if (notebookInspireBodyText != null)
                notebookInspireBodyText.text = body ?? "";
            if (notebookInspirePanel != null)
                notebookInspirePanel.color = clickable ? NbInspire : Color.Lerp(NbInspire, new Color(0.55f, 0.52f, 0.48f), 0.35f);
            if (notebookInspireButton != null)
                notebookInspireButton.interactable = clickable;
        }

        static string LocalizedStatusLabel(TopicStatus s)
        {
            switch (s)
            {
                case TopicStatus.Complete:
                    return UiLoc.T("ui.notebook.status_complete", "已充分了解");
                case TopicStatus.Open:
                    return UiLoc.T("ui.notebook.status_open", "还有疑问");
                case TopicStatus.New:
                    return UiLoc.T("ui.notebook.status_new", "新线索");
                default:
                    return UiLoc.T("ui.notebook.status_untouched", "未发现");
            }
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
                if (statusText)
                {
                    statusText.text = t.inspirationIsInvestigate
                        ? UiLoc.T("ui.notebook.inspire_investigate", "这是调查提示，请先寻找其他信息来源。")
                        : UiLoc.T("ui.notebook.inspire_no_question", "当前没有可填入的采访问题。");
                }
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
                if (statusText) statusText.text = UiLoc.T("ui.notebook.inspire_filled", "已填入提问灵感，可修改后发送");
            }
            else
            {
                GUIUtility.systemCopyBuffer = q;
                if (statusText) statusText.text = UiLoc.T("ui.notebook.inspire_copied", "提问灵感已复制，进入采访后可粘贴使用");
            }
        }
    }
}
