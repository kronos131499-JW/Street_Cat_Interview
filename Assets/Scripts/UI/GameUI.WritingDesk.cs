using System;
using System.Collections.Generic;
using System.Text;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Loc;
using StreetCat.Writing;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Plan C — newspaper / manuscript writing desk (editable draft + 立意/选材/AI优化/提交).
    /// </summary>
    public partial class GameUI
    {
        static readonly Color WdDesk = new Color(0.12f, 0.18f, 0.28f, 1f);
        static readonly Color WdPaper = new Color(0.96f, 0.93f, 0.86f, 1f);
        static readonly Color WdInk = new Color(0.14f, 0.12f, 0.10f, 1f);
        static readonly Color WdMuted = new Color(0.42f, 0.38f, 0.34f, 1f);
        static readonly Color WdOrange = new Color(0.90f, 0.45f, 0.16f, 1f);
        static readonly Color WdNavy = new Color(0.16f, 0.28f, 0.40f, 1f);
        static readonly Color WdPillOff = new Color(0.90f, 0.88f, 0.82f, 1f);
        static readonly Color WdRule = new Color(0.55f, 0.50f, 0.44f, 0.45f);

        GameObject writingDeskRoot;
        Text wdHeadline;
        Text wdKicker;
        Text wdDate;
        Text wdDraftBody;
        InputField wdDraftInput;
        Text wdDraftCharCount;
        Text wdMatsList;
        Text wdMatsCount;
        Text wdMatsHint;
        Text wdSourcesLine;
        Text wdStatusLines;
        Image wdDirGuardBg;
        Image wdDirRescueBg;
        Text wdDirGuardTx;
        Text wdDirRescueTx;
        Button wdSubmitBtn;
        Text wdSubmitLabel;
        Button wdPolishBtn;
        Text wdPolishLabel;
        ScrollRect wdDraftScroll;
        bool writingDeskActive;

        void BuildWritingDeskOverlay(Transform parent)
        {
            writingDeskRoot = new GameObject("WritingDeskOverlay", typeof(RectTransform));
            writingDeskRoot.transform.SetParent(parent, false);
            StretchFull(writingDeskRoot.GetComponent<RectTransform>());

            var desk = CreateImage(writingDeskRoot.transform, "Desk", WdDesk);
            StretchFull(desk.rectTransform);
            desk.raycastTarget = true;

            var paper = CreateImage(writingDeskRoot.transform, "Paper", WdPaper);
            Stretch(paper.rectTransform, new Vector2(0.04f, 0.11f), new Vector2(0.96f, 0.96f),
                Vector2.zero, Vector2.zero);
            paper.raycastTarget = true;
            DrawManuscriptLines(paper.transform);

            // ── Left manuscript column ─────────────────────────────────────
            var left = new GameObject("LeftColumn", typeof(RectTransform));
            left.transform.SetParent(paper.transform, false);
            Stretch(left.GetComponent<RectTransform>(), new Vector2(0.03f, 0.02f), new Vector2(0.66f, 0.98f),
                Vector2.zero, Vector2.zero);

            wdKicker = CreateUiText(left.transform, "Kicker", 14, TextAnchor.MiddleLeft,
                WdMuted, Vector2.zero, Vector2.zero);
            Stretch(wdKicker.rectTransform, new Vector2(0f, 0.94f), new Vector2(0.55f, 1f),
                Vector2.zero, Vector2.zero);
            wdKicker.text = UiLoc.T("ui.writing.desk.kicker", "槐安社区特稿");

            wdDate = CreateUiText(left.transform, "Date", 14, TextAnchor.MiddleRight,
                WdMuted, Vector2.zero, Vector2.zero);
            Stretch(wdDate.rectTransform, new Vector2(0.55f, 0.94f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            wdDate.text = "2025 / 05 / 16";

            wdHeadline = CreateUiText(left.transform, "Headline", 32, TextAnchor.MiddleLeft,
                WdInk, Vector2.zero, Vector2.zero);
            Stretch(wdHeadline.rectTransform, new Vector2(0f, 0.84f), new Vector2(1f, 0.94f),
                Vector2.zero, Vector2.zero);
            wdHeadline.fontStyle = FontStyle.Bold;
            wdHeadline.horizontalOverflow = HorizontalWrapMode.Wrap;
            wdHeadline.verticalOverflow = VerticalWrapMode.Truncate;

            var draftLabel = CreateUiText(left.transform, "DraftLabel", 15, TextAnchor.MiddleLeft,
                WdNavy, Vector2.zero, Vector2.zero);
            Stretch(draftLabel.rectTransform, new Vector2(0f, 0.795f), new Vector2(0.62f, 0.84f),
                Vector2.zero, Vector2.zero);
            draftLabel.fontStyle = FontStyle.Bold;
            draftLabel.text = UiLoc.T("ui.writing.desk.draft_label", "成稿正文（可编辑）");
            TagLoc(draftLabel, "ui.writing.desk.draft_label");

            wdDraftCharCount = CreateUiText(left.transform, "CharCount", 13, TextAnchor.MiddleRight,
                WdMuted, Vector2.zero, Vector2.zero);
            Stretch(wdDraftCharCount.rectTransform, new Vector2(0.62f, 0.795f), new Vector2(1f, 0.84f),
                Vector2.zero, Vector2.zero);

            // Draft scroll row: ScrollRect + InputField content, Scrollbar OUTSIDE viewport
            // (never covered by InputField). Matches backlog ScrollRect pattern for sizing.
            const float draftScrollbarW = 16f;
            var draftRow = new GameObject("DraftRow", typeof(RectTransform));
            draftRow.transform.SetParent(left.transform, false);
            Stretch(draftRow.GetComponent<RectTransform>(), new Vector2(0f, 0.12f), new Vector2(1f, 0.79f),
                Vector2.zero, Vector2.zero);

            var draftHost = new GameObject("DraftHost", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            draftHost.transform.SetParent(draftRow.transform, false);
            Stretch(draftHost.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(-draftScrollbarW, 0f));
            var draftBg = draftHost.GetComponent<Image>();
            draftBg.color = new Color(1f, 1f, 1f, 0.15f);
            draftBg.raycastTarget = true;

            wdDraftScroll = draftHost.GetComponent<ScrollRect>();
            wdDraftScroll.horizontal = false;
            wdDraftScroll.vertical = true;
            wdDraftScroll.movementType = ScrollRect.MovementType.Clamped;
            wdDraftScroll.scrollSensitivity = 48f;
            wdDraftScroll.inertia = true;
            // Avoid AutoHideAndExpandViewport — it drives viewport/content RectTransforms and
            // fights our manual preferred-height sizing.
            wdDraftScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            vp.transform.SetParent(draftHost.transform, false);
            StretchFull(vp.GetComponent<RectTransform>());
            var vpImg = vp.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            vpImg.raycastTarget = true;
            var vpRelay = vp.AddComponent<WritingDeskScrollRelay>();
            vpRelay.scrollRect = wdDraftScroll;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(vp.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = Vector2.zero;

            // InputField IS the scroll content child (same size as Content).
            var inputGo = new GameObject("DraftInput", typeof(RectTransform), typeof(Image));
            inputGo.transform.SetParent(content.transform, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            StretchFull(inputRt);
            var inputBg = inputGo.GetComponent<Image>();
            inputBg.color = new Color(1f, 1f, 1f, 0.001f);
            inputBg.raycastTarget = true;

            // Text: top-stretch width, height = preferred (NOT vertical stretch). Vertical stretch
            // made preferredHeight unreliable vs viewport and left content unscrollable.
            const float draftPad = 8f;
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(inputGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.anchoredPosition = new Vector2(0f, -draftPad);
            textRt.sizeDelta = new Vector2(-(draftPad * 2f), 40f);
            wdDraftBody = textGo.AddComponent<Text>();
            wdDraftBody.font = font;
            wdDraftBody.fontSize = 18;
            wdDraftBody.color = WdInk;
            wdDraftBody.alignment = TextAnchor.UpperLeft;
            wdDraftBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            wdDraftBody.verticalOverflow = VerticalWrapMode.Overflow;
            wdDraftBody.lineSpacing = 1.15f;
            wdDraftBody.supportRichText = false;
            wdDraftBody.raycastTarget = false;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(inputGo.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = new Vector2(0f, 1f);
            phRt.anchorMax = new Vector2(1f, 1f);
            phRt.pivot = new Vector2(0.5f, 1f);
            phRt.anchoredPosition = new Vector2(0f, -draftPad);
            phRt.sizeDelta = new Vector2(-(draftPad * 2f), 40f);
            var ph = phGo.AddComponent<Text>();
            ph.font = font;
            ph.fontSize = 18;
            ph.color = new Color(WdMuted.r, WdMuted.g, WdMuted.b, 0.55f);
            ph.alignment = TextAnchor.UpperLeft;
            ph.horizontalOverflow = HorizontalWrapMode.Wrap;
            ph.verticalOverflow = VerticalWrapMode.Overflow;
            ph.raycastTarget = false;
            ph.text = UiLoc.T("ui.writing.desk.draft_placeholder", "成稿正文将显示在这里，可直接编辑…");

            var deskInput = inputGo.AddComponent<WritingDeskDraftInputField>();
            deskInput.scrollRect = wdDraftScroll;
            wdDraftInput = deskInput;
            wdDraftInput.textComponent = wdDraftBody;
            wdDraftInput.placeholder = ph;
            wdDraftInput.lineType = InputField.LineType.MultiLineNewline;
            wdDraftInput.contentType = InputField.ContentType.Standard;
            wdDraftInput.interactable = true;
            wdDraftInput.customCaretColor = true;
            wdDraftInput.caretColor = new Color(WdInk.r, WdInk.g, WdInk.b, 1f);
            wdDraftInput.caretWidth = 2;
            wdDraftInput.caretBlinkRate = 0.85f;
            wdDraftInput.selectionColor = new Color(0.90f, 0.55f, 0.22f, 0.40f);
            wdDraftInput.onValueChanged.AddListener(_ => OnWritingDeskDraftEdited());

            // Scrollbar sibling of DraftHost — outside InputField hierarchy so drag always hits.
            var sbGo = new GameObject("DraftScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbGo.transform.SetParent(draftRow.transform, false);
            var sbrt = sbGo.GetComponent<RectTransform>();
            sbrt.anchorMin = new Vector2(1f, 0f);
            sbrt.anchorMax = new Vector2(1f, 1f);
            sbrt.pivot = new Vector2(1f, 0.5f);
            sbrt.sizeDelta = new Vector2(draftScrollbarW, 0f);
            sbrt.anchoredPosition = Vector2.zero;
            var trackImg = sbGo.GetComponent<Image>();
            trackImg.color = new Color(WdMuted.r, WdMuted.g, WdMuted.b, 0.35f);
            trackImg.raycastTarget = true;

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(sbGo.transform, false);
            Stretch(slidingArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(2f, 4f), new Vector2(-2f, -4f));

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(slidingArea.transform, false);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.sizeDelta = Vector2.zero;
            handleRt.anchoredPosition = Vector2.zero;
            var handleImg = handleGo.GetComponent<Image>();
            handleImg.color = new Color(WdInk.r, WdInk.g, WdInk.b, 0.70f);
            handleImg.raycastTarget = true;

            var scrollbar = sbGo.GetComponent<Scrollbar>();
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1f;
            scrollbar.size = 1f;
            scrollbar.numberOfSteps = 0;

            wdDraftScroll.viewport = vp.GetComponent<RectTransform>();
            wdDraftScroll.content = crt;
            wdDraftScroll.verticalScrollbar = scrollbar;

            var srcLabel = CreateUiText(left.transform, "SourcesLabel", 14, TextAnchor.MiddleLeft,
                WdNavy, Vector2.zero, Vector2.zero);
            Stretch(srcLabel.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.11f),
                Vector2.zero, Vector2.zero);
            srcLabel.fontStyle = FontStyle.Bold;
            srcLabel.text = UiLoc.T("ui.writing.desk.sources", "信息来源");
            TagLoc(srcLabel, "ui.writing.desk.sources");

            wdSourcesLine = CreateUiText(left.transform, "Sources", 14, TextAnchor.MiddleLeft,
                WdMuted, Vector2.zero, Vector2.zero);
            Stretch(wdSourcesLine.rectTransform, new Vector2(0f, 0.01f), new Vector2(1f, 0.06f),
                Vector2.zero, Vector2.zero);
            wdSourcesLine.horizontalOverflow = HorizontalWrapMode.Wrap;

            // ── Right sidebar ──────────────────────────────────────────────
            var right = new GameObject("RightColumn", typeof(RectTransform));
            right.transform.SetParent(paper.transform, false);
            Stretch(right.GetComponent<RectTransform>(), new Vector2(0.68f, 0.02f), new Vector2(0.98f, 0.98f),
                Vector2.zero, Vector2.zero);

            var rule = CreateImage(paper.transform, "VRule", WdRule);
            Stretch(rule.rectTransform, new Vector2(0.665f, 0.04f), new Vector2(0.668f, 0.96f),
                Vector2.zero, Vector2.zero);
            rule.raycastTarget = false;

            var s1 = CreateUiText(right.transform, "S1", 16, TextAnchor.MiddleLeft,
                WdInk, Vector2.zero, Vector2.zero);
            Stretch(s1.rectTransform, new Vector2(0f, 0.92f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            s1.fontStyle = FontStyle.Bold;
            s1.text = UiLoc.T("ui.writing.desk.dir_title", "1. 写作方向");
            TagLoc(s1, "ui.writing.desk.dir_title");

            BuildDirButton(right.transform, "DirGuard",
                "ui.writing.desk.dir_guard", "大福今天也在上班",
                new Vector2(0f, 0.80f), new Vector2(1f, 0.91f),
                () =>
                {
                    pendingDir = WritingDirection.GuardCatToday;
                    RefreshWritingDesk();
                }, out wdDirGuardBg, out wdDirGuardTx);

            BuildDirButton(right.transform, "DirRescue",
                "ui.writing.desk.dir_rescue", "救下一只猫以后",
                new Vector2(0f, 0.68f), new Vector2(1f, 0.79f),
                () =>
                {
                    pendingDir = WritingDirection.RescueWithoutAdoption;
                    RefreshWritingDesk();
                }, out wdDirRescueBg, out wdDirRescueTx);

            wdMatsCount = CreateUiText(right.transform, "MatsHeader", 16, TextAnchor.MiddleLeft,
                WdInk, Vector2.zero, Vector2.zero);
            Stretch(wdMatsCount.rectTransform, new Vector2(0f, 0.60f), new Vector2(1f, 0.67f),
                Vector2.zero, Vector2.zero);
            wdMatsCount.fontStyle = FontStyle.Bold;

            wdMatsList = CreateUiText(right.transform, "MatsList", 13, TextAnchor.UpperLeft,
                WdInk, Vector2.zero, Vector2.zero);
            Stretch(wdMatsList.rectTransform, new Vector2(0f, 0.28f), new Vector2(1f, 0.60f),
                Vector2.zero, Vector2.zero);
            wdMatsList.horizontalOverflow = HorizontalWrapMode.Wrap;
            wdMatsList.verticalOverflow = VerticalWrapMode.Truncate;
            wdMatsList.lineSpacing = 1.1f;

            wdMatsHint = CreateUiText(right.transform, "MatsHint", 13, TextAnchor.MiddleLeft,
                WdOrange, Vector2.zero, Vector2.zero);
            Stretch(wdMatsHint.rectTransform, new Vector2(0f, 0.22f), new Vector2(1f, 0.28f),
                Vector2.zero, Vector2.zero);

            var s3 = CreateUiText(right.transform, "S3", 16, TextAnchor.MiddleLeft,
                WdInk, Vector2.zero, Vector2.zero);
            Stretch(s3.rectTransform, new Vector2(0f, 0.155f), new Vector2(1f, 0.22f),
                Vector2.zero, Vector2.zero);
            s3.fontStyle = FontStyle.Bold;
            s3.text = UiLoc.T("ui.writing.desk.status_title", "3. 生成状态");
            TagLoc(s3, "ui.writing.desk.status_title");

            wdStatusLines = CreateUiText(right.transform, "Status", 13, TextAnchor.UpperLeft,
                WdInk, Vector2.zero, Vector2.zero);
            Stretch(wdStatusLines.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.155f),
                Vector2.zero, Vector2.zero);
            wdStatusLines.horizontalOverflow = HorizontalWrapMode.Wrap;
            wdStatusLines.verticalOverflow = VerticalWrapMode.Truncate;

            // ── Bottom action bar ──────────────────────────────────────────
            var bar = CreateImage(writingDeskRoot.transform, "ActionBar", WdNavy);
            Stretch(bar.rectTransform, new Vector2(0.04f, 0.015f), new Vector2(0.96f, 0.095f),
                Vector2.zero, Vector2.zero);

            SpawnDeskBarButton(bar.transform, "BackMats",
                "ui.writing.desk.back_mats", "返回修改素材",
                new Vector2(0.01f, 0.12f), new Vector2(0.28f, 0.88f), WdNavy,
                () =>
                {
                    HideWritingDesk();
                    ShowMaterialPick();
                });

            var polishGo = SpawnDeskBarButton(bar.transform, "AiPolish",
                "ui.writing.desk.ai_polish", "AI 优化",
                new Vector2(0.30f, 0.12f), new Vector2(0.52f, 0.88f), WdNavy,
                OnWritingDeskAiPolish);
            wdPolishBtn = polishGo.GetComponent<Button>();
            wdPolishLabel = polishGo.GetComponentInChildren<Text>();

            var submitGo = SpawnDeskBarButton(bar.transform, "Submit",
                "ui.writing.desk.submit", "提交主编审核",
                new Vector2(0.54f, 0.08f), new Vector2(0.99f, 0.92f), WdOrange,
                OnWritingDeskSubmit);
            wdSubmitBtn = submitGo.GetComponent<Button>();
            wdSubmitLabel = submitGo.GetComponentInChildren<Text>();

            writingDeskRoot.SetActive(false);
        }

        void DrawManuscriptLines(Transform paper)
        {
            for (int i = 0; i < 18; i++)
            {
                float y = 0.08f + i * 0.048f;
                if (y > 0.92f) break;
                var line = CreateImage(paper, "Line" + i, new Color(0.55f, 0.50f, 0.42f, 0.12f));
                Stretch(line.rectTransform, new Vector2(0.03f, y), new Vector2(0.64f, y + 0.0025f),
                    Vector2.zero, Vector2.zero);
                line.raycastTarget = false;
            }
        }

        void BuildDirButton(Transform parent, string name, string locKey, string fallback,
            Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick,
            out Image bg, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), aMin, aMax, Vector2.zero, Vector2.zero);
            bg = go.GetComponent<Image>();
            bg.color = WdPillOff;
            go.GetComponent<Button>().onClick.AddListener(onClick);
            label = CreateUiText(go.transform, "T", 16, TextAnchor.MiddleCenter,
                WdInk, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4));
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.text = UiLoc.T(locKey, fallback);
            label.raycastTarget = false;
            TagLoc(label, locKey);
        }

        GameObject SpawnDeskBarButton(Transform parent, string name, string locKey, string fallback,
            Vector2 aMin, Vector2 aMax, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), aMin, aMax, Vector2.zero, Vector2.zero);
            var img = go.GetComponent<Image>();
            img.color = bgColor == WdOrange ? WdOrange : new Color(0.20f, 0.34f, 0.48f, 1f);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            var tx = CreateUiText(go.transform, "T", 15, TextAnchor.MiddleCenter,
                Color.white, Vector2.zero, Vector2.zero);
            StretchFull(tx.rectTransform);
            tx.fontStyle = FontStyle.Bold;
            tx.text = UiLoc.T(locKey, fallback);
            tx.raycastTarget = false;
            TagLoc(tx, locKey);
            return go;
        }

        static void TagLoc(Text tx, string key)
        {
            if (tx == null || string.IsNullOrEmpty(key)) return;
            var tag = tx.gameObject.GetComponent<LocTag>();
            if (tag == null) tag = tx.gameObject.AddComponent<LocTag>();
            tag.key = key;
            tag.target = tx;
        }

        void ShowWritingDesk()
        {
            if (writingDeskRoot == null) return;
            writingDeskActive = true;
            writingMatsActive = false;
            HideWritingMaterialsBoard();
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

            if (writingMatsRoot != null) writingMatsRoot.SetActive(false);
            if (writingPreviewRoot != null) writingPreviewRoot.SetActive(false);

            writingDeskRoot.SetActive(true);
            writingDeskRoot.transform.SetAsLastSibling();
            BringOverlayStackToFront();
            // BringOverlayStackToFront raises fade/hide-dialogue after desk; keep desk above UI chrome.
            writingDeskRoot.transform.SetAsLastSibling();
            if (sceneFadeImage != null)
                sceneFadeImage.transform.SetAsLastSibling();
            if (advanceCatcher != null)
                advanceCatcher.gameObject.SetActive(false);

            RefreshWritingDesk();
            ApplyWritingFonts();
            RebuildWritingDeskDraftLayout();
            if (isActiveAndEnabled)
                StartCoroutine(DeferredWritingDeskDraftLayout());
        }

        System.Collections.IEnumerator DeferredWritingDeskDraftLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            RebuildWritingDeskDraftLayout();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RebuildWritingDeskDraftLayout();
        }

        void HideWritingDesk()
        {
            writingDeskActive = false;
            if (writingPolishCo != null)
            {
                StopCoroutine(writingPolishCo);
                writingPolishCo = null;
            }
            if (writingDeskRoot != null) writingDeskRoot.SetActive(false);
        }

        void RefreshWritingDesk()
        {
            if (writingDeskRoot == null || !writingDeskRoot.activeSelf) return;

            bool guard = pendingDir == WritingDirection.GuardCatToday;
            StyleToggle(wdDirGuardBg, wdDirGuardTx, guard);
            StyleToggle(wdDirRescueBg, wdDirRescueTx, !guard);

            string title = ArticleAssembler.HeadlineFor(pendingDir);
            if (wdHeadline != null) wdHeadline.text = title;
            if (wdKicker != null)
                wdKicker.text = UiLoc.T("ui.writing.desk.kicker", "槐安社区特稿");

            int n = selectedMats != null ? selectedMats.Count : 0;
            if (wdMatsCount != null)
            {
                wdMatsCount.text = string.Format(
                    UiLoc.T("ui.writing.desk.mats_fmt", "2. 已选素材（{0}/{1}）"),
                    n, WritingMaxSelect);
            }

            var listSb = new StringBuilder();
            if (selectedMats != null)
            {
                for (int i = 0; i < selectedMats.Count; i++)
                {
                    var m = MaterialCatalog.All.Find(c => c != null && c.id == selectedMats[i]);
                    string label = m != null ? m.id + "  " + m.title : selectedMats[i];
                    if (i > 0 && i % 2 == 0) listSb.AppendLine();
                    else if (i > 0) listSb.Append("　　");
                    listSb.Append("· ").Append(label);
                }
            }
            if (wdMatsList != null) wdMatsList.text = listSb.ToString();

            ArticleAssembler.CountParagraphCoverage(selectedMats, out int cov1, out int cov2, out int cov3, out int cov4);
            int coveredParas = (cov1 > 0 ? 1 : 0) + (cov2 > 0 ? 1 : 0) + (cov3 > 0 ? 1 : 0) + (cov4 > 0 ? 1 : 0);
            if (wdMatsHint != null)
            {
                if (coveredParas < 4)
                    wdMatsHint.text = string.Format(
                        UiLoc.T("ui.writing.desk.need_paras", "段落覆盖 {0}/4"),
                        coveredParas);
                else if (n >= WritingMaxSelect)
                    wdMatsHint.text = UiLoc.T("ui.writing.desk.full", "已选满");
                else
                    wdMatsHint.text = UiLoc.T("ui.writing.desk.paras_ok", "四段已齐");
            }

            var sources = new HashSet<string>();
            if (selectedMats != null)
            {
                foreach (var id in selectedMats)
                {
                    var m = MaterialCatalog.All.Find(c => c != null && c.id == id);
                    if (m != null) sources.Add(MaterialSourceLabel(m));
                }
            }
            if (wdSourcesLine != null)
            {
                if (sources.Count == 0)
                    wdSourcesLine.text = UiLoc.T("ui.writing.desk.sources_empty", "（尚未选入素材）");
                else
                    wdSourcesLine.text = string.Join("  ·  ", sources);
            }

            bool canAssemble = assembler.CanAssemble(pendingDir, selectedMats, out var err);
            bool polishing = writingPolishCo != null;

            if (canAssemble)
            {
                string key = BuildWritingDraftKey();
                bool sameDraft = key == writingPolishKey && !string.IsNullOrWhiteSpace(assembler.Body);

                if (!sameDraft)
                {
                    if (writingPolishCo != null)
                    {
                        StopCoroutine(writingPolishCo);
                        writingPolishCo = null;
                    }
                    // New mats/dir → assemble offline skeleton once; do NOT auto-expand.
                    assembler.Assemble(pendingDir, selectedMats);
                    var richer = ArticleDraftAi.BuildOfflineFeature(
                        pendingDir, selectedMats, assembler.Title, assembler.Body);
                    if (ArticleDraftAi.CountContentChars(richer)
                        > ArticleDraftAi.CountContentChars(assembler.Body))
                        assembler.ReplaceBody(richer);
                    else
                        assembler.ReplaceBody(ArticleDraftAi.StripRelatedVerificationBlocks(assembler.Body));
                    writingPolishKey = key;
                    writingAiPolishUsed = false;
                    SetWritingDeskDraftText(assembler.Body);
                }
                else if (wdDraftInput != null && string.IsNullOrEmpty(wdDraftInput.text)
                         && !string.IsNullOrWhiteSpace(assembler.Body))
                {
                    SetWritingDeskDraftText(assembler.Body);
                }

                if (wdDraftInput != null)
                    wdDraftInput.interactable = !polishing;
            }
            else
            {
                if (writingPolishCo != null)
                {
                    StopCoroutine(writingPolishCo);
                    writingPolishCo = null;
                }
                writingAiPolishUsed = false;
                writingPolishKey = null;
                SetWritingDeskDraftText(
                    UiLoc.T("ui.writing.desk.draft_blocked", "现在还不能生成成稿预览。")
                    + "\n\n" + (err ?? ""));
                if (wdDraftInput != null)
                    wdDraftInput.interactable = false;
            }

            UpdateWritingDeskCharCount();

            var st = new StringBuilder();
            st.AppendLine(StatusItem(canAssemble,
                UiLoc.T("ui.writing.desk.st_ready", "生成条件已满足"),
                UiLoc.T("ui.writing.desk.st_not_ready", "生成条件未满足")));
            st.AppendLine(StatusItem(true,
                UiLoc.T("ui.writing.desk.st_dir", "已选择写作方向"),
                ""));
            st.AppendLine(StatusItem(coveredParas >= 4,
                UiLoc.T("ui.writing.desk.st_mats", "四段均已选材"),
                UiLoc.T("ui.writing.desk.st_mats_low", "还有段落缺素材")));
            if (canAssemble)
            {
                if (polishing)
                {
                    st.AppendLine(StatusItem(false,
                        UiLoc.T("ui.writing.desk.st_polishing", "AI 优化中…"),
                        UiLoc.T("ui.writing.desk.st_polishing", "AI 优化中…")));
                }
                else if (writingAiPolishUsed)
                {
                    st.AppendLine(StatusItem(true,
                        UiLoc.T("ui.writing.desk.st_polish_used", "已使用 AI 优化（本稿一次）"),
                        ""));
                }
                else
                {
                    st.AppendLine(StatusItem(true,
                        UiLoc.T("ui.writing.desk.st_editable", "正文可编辑；可 AI 优化一次"),
                        ""));
                }
            }
            if (wdStatusLines != null) wdStatusLines.text = st.ToString().TrimEnd();

            if (wdSubmitBtn != null) wdSubmitBtn.interactable = canAssemble && !polishing;
            if (wdSubmitLabel != null)
            {
                if (!canAssemble)
                    wdSubmitLabel.text = UiLoc.T("ui.writing.desk.submit_locked", "尚不能提交");
                else if (polishing)
                    wdSubmitLabel.text = UiLoc.T("ui.writing.desk.submit_polishing", "优化中…");
                else
                    wdSubmitLabel.text = UiLoc.T("ui.writing.desk.submit", "提交主编审核");
            }

            UpdateWritingDeskPolishButton(canAssemble, polishing);

            foreach (var t in new[]
                     {
                         wdHeadline, wdKicker, wdDate, wdDraftBody, wdDraftCharCount, wdMatsList, wdMatsCount,
                         wdMatsHint, wdSourcesLine, wdStatusLines, wdDirGuardTx, wdDirRescueTx
                     })
            {
                if (t != null) ApplyLetterSpacing(t, 0f);
            }

            RebuildWritingDeskDraftLayout();
        }

        void UpdateWritingDeskPolishButton(bool canAssemble, bool polishing)
        {
            if (wdPolishBtn == null) return;
            bool used = writingAiPolishUsed;
            wdPolishBtn.interactable = canAssemble && !polishing && !used;
            if (wdPolishLabel == null) return;
            if (polishing)
                wdPolishLabel.text = UiLoc.T("ui.writing.desk.ai_polishing", "AI 优化中…");
            else if (used)
                wdPolishLabel.text = UiLoc.T("ui.writing.desk.ai_polish_used", "已优化");
            else
                wdPolishLabel.text = UiLoc.T("ui.writing.desk.ai_polish", "AI 优化");
        }

        void SetWritingDeskDraftText(string text)
        {
            if (wdDraftInput != null)
                wdDraftInput.text = text ?? "";
            else if (wdDraftBody != null)
                wdDraftBody.text = text ?? "";
            UpdateWritingDeskCharCount();
            RebuildWritingDeskDraftLayout();
        }

        void OnWritingDeskDraftEdited()
        {
            UpdateWritingDeskCharCount();
            RebuildWritingDeskDraftLayout();
        }

        /// <summary>
        /// Grow ScrollRect content to measured draft height so wheel / scrollbar can scroll.
        /// Uses TextGenerator with an explicit wrap width (not stretched-rect preferredHeight).
        /// </summary>
        void RebuildWritingDeskDraftLayout()
        {
            if (wdDraftScroll == null || wdDraftBody == null) return;
            var content = wdDraftScroll.content;
            var viewport = wdDraftScroll.viewport;
            if (content == null || viewport == null) return;

            const float draftPad = 8f;
            Canvas.ForceUpdateCanvases();

            float viewW = viewport.rect.width;
            float viewH = viewport.rect.height;
            if (viewW < 8f || viewH < 8f)
                return;

            // Keep content full viewport width (top-stretch anchors → sizeDelta.x = 0).
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewW);
            var cSize = content.sizeDelta;
            cSize.x = 0f;
            content.sizeDelta = cSize;

            float textWidth = Mathf.Max(1f, viewW - draftPad * 2f);

            // Lay out text width before measuring so preferredHeight / TextGenerator agree.
            var textRt = wdDraftBody.rectTransform;
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.anchoredPosition = new Vector2(0f, -draftPad);
            textRt.sizeDelta = new Vector2(-(draftPad * 2f), 8f);
            Canvas.ForceUpdateCanvases();

            string body = wdDraftInput != null ? wdDraftInput.text : wdDraftBody.text;
            if (!string.IsNullOrEmpty(body) && wdDraftBody.text != body)
                wdDraftBody.text = body;

            float measured = MeasureWritingDeskDraftTextHeight(wdDraftBody, textWidth, body);
            float preferred = wdDraftBody.preferredHeight;
            float textH = Mathf.Max(measured, preferred);
            float contentH = Mathf.Max(viewH, textH + draftPad * 2f + 4f);

            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentH);
            cSize = content.sizeDelta;
            cSize.x = 0f;
            content.sizeDelta = cSize;

            textRt.sizeDelta = new Vector2(-(draftPad * 2f), Mathf.Max(textH, 1f));
            textRt.anchoredPosition = new Vector2(0f, -draftPad);

            if (wdDraftInput is WritingDeskDraftInputField deskField)
                deskField.textTopPad = draftPad;

            if (wdDraftInput != null && wdDraftInput.placeholder is Text ph)
            {
                var phRt = ph.rectTransform;
                phRt.anchorMin = new Vector2(0f, 1f);
                phRt.anchorMax = new Vector2(1f, 1f);
                phRt.pivot = new Vector2(0.5f, 1f);
                phRt.anchoredPosition = new Vector2(0f, -draftPad);
                phRt.sizeDelta = new Vector2(-(draftPad * 2f), Mathf.Max(textH, 1f));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();

            // Refresh scrollbar handle size after content bounds change.
            float n = wdDraftScroll.verticalNormalizedPosition;
            wdDraftScroll.verticalNormalizedPosition = Mathf.Clamp01(n);
            wdDraftScroll.velocity = Vector2.zero;

#if UNITY_EDITOR
            if (contentH > viewH + 1f && (Time.frameCount % 120 == 0))
                Debug.Log($"[WritingDesk] draft scrollable contentH={contentH:F0} viewH={viewH:F0} norm={n:F2}");
#endif
        }

        static float MeasureWritingDeskDraftTextHeight(Text text, float width, string content)
        {
            if (text == null || width < 1f)
                return 0f;
            if (string.IsNullOrEmpty(content))
                content = " ";

            var settings = text.GetGenerationSettings(new Vector2(width, 0f));
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.generateOutOfBounds = true;

            float px = text.cachedTextGeneratorForLayout.GetPreferredHeight(content, settings);
            float ppu = Mathf.Max(0.01f, text.pixelsPerUnit);
            return px / ppu;
        }

        void UpdateWritingDeskCharCount()
        {
            if (wdDraftCharCount == null) return;
            string body = wdDraftInput != null ? wdDraftInput.text : (wdDraftBody != null ? wdDraftBody.text : "");
            int chars = ArticleDraftAi.CountContentChars(body);
            wdDraftCharCount.text = string.Format(
                UiLoc.T("ui.writing.desk.chars_fmt", "约 {0} 字"),
                chars);
        }

        /// <summary>Push editable field text into assembler before polish / submit.</summary>
        void SyncWritingDeskDraftToAssembler()
        {
            if (wdDraftInput == null) return;
            string text = wdDraftInput.text;
            if (string.IsNullOrWhiteSpace(text)) return;
            // Ignore the blocked-state placeholder message.
            if (text.StartsWith(UiLoc.T("ui.writing.desk.draft_blocked", "现在还不能生成成稿预览。"),
                    StringComparison.Ordinal))
                return;
            assembler.ReplaceBody(text);
        }

        string BuildWritingDraftKey()
        {
            var sb = new StringBuilder();
            sb.Append((int)pendingDir).Append('|');
            if (selectedMats == null || selectedMats.Count == 0)
                return sb.ToString();
            var sorted = new List<string>(selectedMats);
            sorted.Sort(StringComparer.Ordinal);
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(sorted[i]);
            }
            return sb.ToString();
        }

        void OnWritingDeskAiPolish()
        {
            if (!assembler.CanAssemble(pendingDir, selectedMats, out _))
            {
                RefreshWritingDesk();
                return;
            }
            if (writingAiPolishUsed || writingPolishCo != null)
                return;

            SyncWritingDeskDraftToAssembler();
            string key = BuildWritingDraftKey();
            writingPolishKey = key;
            writingPolishCo = StartCoroutine(WritingDeskPolishCo(key));
            RefreshWritingDesk();
        }

        System.Collections.IEnumerator WritingDeskPolishCo(string key)
        {
            var dir = pendingDir;
            var mats = selectedMats != null ? new List<string>(selectedMats) : new List<string>();
            yield return ArticleDraftAi.ExpandCoroutine(assembler, dir, mats, null);

            if (key != writingPolishKey)
            {
                writingPolishCo = null;
                yield break;
            }

            writingAiPolishUsed = true;
            writingPolishCo = null;
            SetWritingDeskDraftText(assembler.Body);
            GameState.Instance.Data.lastArticleTitle = assembler.Title;
            GameState.Instance.Data.lastArticleBody = assembler.Body;
            if (writingDeskActive)
                RefreshWritingDesk();
        }

        static string StatusItem(bool ok, string okText, string badText)
        {
            if (ok) return "●  " + okText;
            return "○  " + (string.IsNullOrEmpty(badText) ? okText : badText);
        }

        static void StyleToggle(Image bg, Text tx, bool on)
        {
            if (bg != null) bg.color = on ? WdOrange : WdPillOff;
            if (tx != null) tx.color = on ? Color.white : WdInk;
        }

        void OnWritingDeskSubmit()
        {
            if (!assembler.CanAssemble(pendingDir, selectedMats, out _))
            {
                RefreshWritingDesk();
                return;
            }
            if (writingPolishCo != null)
                return;

            SyncWritingDeskDraftToAssembler();
            HideWritingDesk();
            if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(true);
            if (buttonRoot != null) buttonRoot.gameObject.SetActive(true);
            SetChrome(true, false, true);
            GenerateArticle();
        }
    }
}
