using System;
using System.Collections;
using System.Collections.Generic;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Interview;
using StreetCat.Loc;
using StreetCat.Narrative;
using StreetCat.Notebook;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StreetCat.UI
{
    /// <summary>
    /// Scrapbook free-interview chrome: three-column layout (status+portrait / chat / inspire+toolbar).
    /// </summary>
    public partial class GameUI
    {
        static readonly Color IvPaper = new Color(0.97f, 0.94f, 0.88f, 0.90f);
        static readonly Color IvPaperShadow = new Color(0.12f, 0.10f, 0.08f, 0.28f);
        static readonly Color IvInk = new Color(0.16f, 0.13f, 0.10f, 1f);
        static readonly Color IvInkMuted = new Color(0.42f, 0.38f, 0.34f, 1f);
        static readonly Color IvSendBrown = new Color(0.38f, 0.26f, 0.18f, 1f);
        static readonly Color IvTrust = new Color(0.22f, 0.62f, 0.58f, 1f);
        static readonly Color IvStress = new Color(0.72f, 0.28f, 0.28f, 1f);
        static readonly Color IvFocus = new Color(0.90f, 0.72f, 0.22f, 1f);
        static readonly Color IvBarTrack = new Color(0.88f, 0.84f, 0.78f, 1f);
        static readonly Color IvInputBg = new Color(0.94f, 0.92f, 0.88f, 0.98f);
        static readonly Color IvChipFill = new Color(0.97f, 0.95f, 0.91f, 0.88f);
        static readonly Color IvChipOutline = new Color(0.62f, 0.56f, 0.48f, 0.55f);
        static readonly Color IvBubbleNpc = new Color(0.98f, 0.97f, 0.94f, 1f);
        static readonly Color IvBubblePlayer = new Color(0.82f, 0.90f, 0.78f, 1f);
        static readonly Color IvBubbleSystem = new Color(0.92f, 0.89f, 0.84f, 0.72f);
        static readonly Color IvBubbleMaterial = new Color(0.90f, 0.86f, 0.78f, 0.78f);
        static readonly Color IvDim = new Color(0.04f, 0.05f, 0.07f, 0.30f);
        static readonly Color IvSep = new Color(0.72f, 0.68f, 0.62f, 0.55f);
        static readonly Color IvNamePlate = new Color(0.93f, 0.90f, 0.84f, 0.98f);
        static readonly Color IvEndAccent = new Color(0.78f, 0.32f, 0.22f, 0.94f);

        static readonly Color[] IvChipMarkTints =
        {
            new Color(0.45f, 0.52f, 0.40f, 0.85f),
            new Color(0.58f, 0.44f, 0.28f, 0.85f),
            new Color(0.38f, 0.48f, 0.58f, 0.85f),
        };
        // Plain digits only — emoji/special symbols spam TMP missing-glyph □ on SimHei/Helvetica.
        static readonly string[] IvChipMarks = { "1", "2", "3" };

        Image interviewPortraitImage;
        Image interviewTrustFill;
        Image interviewStressFill;
        Image interviewFocusFill;
        TextMeshProUGUI interviewTrustLabel;
        TextMeshProUGUI interviewStressLabel;
        TextMeshProUGUI interviewFocusLabel;
        TextMeshProUGUI interviewTrustValue;
        TextMeshProUGUI interviewStressValue;
        TextMeshProUGUI interviewFocusValue;
        TextMeshProUGUI interviewMeterCaption;
        TextMeshProUGUI interviewTitleText;
        TextMeshProUGUI interviewTitleSubText;
        TextMeshProUGUI interviewInspireHeaderText;
        TextMeshProUGUI interviewInspireHintText;
        TextMeshProUGUI interviewCoachTipText;
        TextMeshProUGUI interviewBannerText;
        Image interviewSendBtnImage;
        TextMeshProUGUI interviewSendLabel;
        Transform interviewLogContent;
        Transform interviewToolsRow;
        readonly List<GameObject> interviewBubbleSpawned = new List<GameObject>();
        Sprite interviewCircleMaskSprite;

        // Legacy segment refs kept null-safe for any external callers.
        Image[] interviewTrustSegs;
        Image[] interviewStressSegs;
        Image[] interviewFocusSegs;

        void BuildInterviewOverlay(Transform parent)
        {
            interviewRoot = new GameObject("InterviewOverlay", typeof(RectTransform));
            interviewRoot.transform.SetParent(parent, false);
            StretchFull(interviewRoot.GetComponent<RectTransform>());

            // Dim scene BG so dusk art peeks through; TopBar chips are hidden during interview.
            var catcher = CreateImage(interviewRoot.transform, "HitCatcher", IvDim);
            Stretch(catcher.rectTransform, new Vector2(0f, 0f), new Vector2(1f, VnTheme.TopHudBottom),
                Vector2.zero, Vector2.zero);
            catcher.raycastTarget = true;

            BuildInterviewLeftColumn(interviewRoot.transform);
            BuildInterviewCenterColumn(interviewRoot.transform);
            BuildInterviewRightColumn(interviewRoot.transform);

            interviewRoot.SetActive(false);
        }

        void BuildInterviewLeftColumn(Transform parent)
        {
            var col = new GameObject("LeftColumn", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            // ~14% left — prioritize wide center chat
            Stretch(col.GetComponent<RectTransform>(),
                new Vector2(0.012f, 0.05f), new Vector2(0.148f, 0.955f),
                Vector2.zero, Vector2.zero);

            BuildInterviewStatusPad(col.transform);
            BuildInterviewPortraitPad(col.transform);
        }

        void BuildInterviewStatusPad(Transform parent)
        {
            var host = new GameObject("StatusPad", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Stretch(host.GetComponent<RectTransform>(),
                new Vector2(0f, 0.62f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, -4f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(host.transform, "Paper");
            AttachTape(paper.transform, new Vector2(0.12f, 1.02f), 54f, 18f, -8f);
            AttachPaperclip(paper.transform, new Vector2(0.92f, 1.04f), 30f, 42f, 14f);

            var header = CreateUiText(paper.transform, "Header", 14, TextAnchor.MiddleLeft, IvInk,
                Vector2.zero, Vector2.zero);
            Stretch(header.rectTransform, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.96f),
                Vector2.zero, Vector2.zero);
            header.fontStyle = FontStyles.Bold;
            header.text = UiLoc.T("ui.interview.status_header", "受访者状态");
            interviewMeterCaption = header;

            interviewTrustFill = BuildMeterBarRow(paper.transform, "Trust", 0, IvTrust,
                out interviewTrustLabel, out interviewTrustValue);
            interviewStressFill = BuildMeterBarRow(paper.transform, "Stress", 1, IvStress,
                out interviewStressLabel, out interviewStressValue);
            interviewFocusFill = BuildMeterBarRow(paper.transform, "Focus", 2, IvFocus,
                out interviewFocusLabel, out interviewFocusValue);
            RefreshInterviewMeterLabels();

            // Keep statusText alias wired.
            interviewStatusText = interviewMeterCaption;
            interviewSubjectText = CreateUiText(paper.transform, "SubjectHidden", 1, TextAnchor.MiddleLeft,
                Color.clear, Vector2.zero, Vector2.zero);
            interviewSubjectText.gameObject.SetActive(false);
        }

        Image BuildMeterBarRow(Transform parent, string name, int row, Color fill,
            out TextMeshProUGUI labelTx, out TextMeshProUGUI valueTx)
        {
            float top = 0.74f - row * 0.24f;
            float bot = top - 0.20f;

            var rowGo = new GameObject(name, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            Stretch(rowGo.GetComponent<RectTransform>(), new Vector2(0.06f, bot), new Vector2(0.94f, top),
                Vector2.zero, Vector2.zero);

            labelTx = CreateUiText(rowGo.transform, "Label", 13, TextAnchor.MiddleLeft, IvInk,
                Vector2.zero, Vector2.zero);
            Stretch(labelTx.rectTransform, new Vector2(0f, 0.45f), new Vector2(0.55f, 1f),
                Vector2.zero, Vector2.zero);
            labelTx.fontStyle = FontStyles.Bold;
            labelTx.enableWordWrapping = false;
            labelTx.overflowMode = TextOverflowModes.Overflow;
            labelTx.raycastTarget = false;

            valueTx = CreateUiText(rowGo.transform, "Value", 12, TextAnchor.MiddleRight, fill,
                Vector2.zero, Vector2.zero);
            Stretch(valueTx.rectTransform, new Vector2(0.55f, 0.45f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            valueTx.fontStyle = FontStyles.Bold;
            valueTx.enableWordWrapping = false;
            valueTx.raycastTarget = false;
            valueTx.text = "—";

            var track = CreateImage(rowGo.transform, "Track", IvBarTrack);
            Stretch(track.rectTransform, new Vector2(0f, 0.08f), new Vector2(1f, 0.40f),
                Vector2.zero, Vector2.zero);
            track.raycastTarget = false;

            var fillImg = CreateImage(track.transform, "Fill", fill);
            var frt = fillImg.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0f, 1f);
            frt.pivot = new Vector2(0f, 0.5f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            fillImg.raycastTarget = false;
            return fillImg;
        }

        void BuildInterviewPortraitPad(Transform parent)
        {
            var host = new GameObject("PortraitPad", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Stretch(host.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(1f, 0.58f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(4f, -5f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(host.transform, "Paper");
            AttachTape(paper.transform, new Vector2(0.1f, 1.01f), 48f, 16f, -12f);
            AttachTape(paper.transform, new Vector2(0.9f, 1.01f), 48f, 16f, 10f);

            interviewPortraitImage = CreateImage(paper.transform, "Portrait", Color.white);
            Stretch(interviewPortraitImage.rectTransform,
                new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.94f),
                Vector2.zero, Vector2.zero);
            interviewPortraitImage.preserveAspect = true;
            interviewPortraitImage.raycastTarget = false;
            interviewPortraitImage.enabled = false;

            var plate = CreateImage(paper.transform, "NamePlate", IvNamePlate);
            Stretch(plate.rectTransform, new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.14f),
                Vector2.zero, Vector2.zero);
            plate.raycastTarget = false;

            interviewSubjectText = CreateUiText(plate.transform, "Name", 16, TextAnchor.MiddleCenter, IvInk,
                Vector2.zero, Vector2.zero);
            StretchFull(interviewSubjectText.rectTransform);
            interviewSubjectText.fontStyle = FontStyles.Bold;
            interviewSubjectText.text = "";
        }

        void BuildInterviewCenterColumn(Transform parent)
        {
            var col = new GameObject("CenterColumn", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            // ~68% center chat — dialogue is the focus
            Stretch(col.GetComponent<RectTransform>(),
                new Vector2(0.158f, 0.05f), new Vector2(0.838f, 0.955f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(col.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(5f, -6f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(col.transform, "MainPaper");
            paper.raycastTarget = true;
            AttachPaperclip(paper.transform, new Vector2(0.97f, 0.98f), 36f, 50f, -6f);

            interviewTitleText = CreateUiText(paper.transform, "Title", 28, TextAnchor.MiddleCenter, IvInk,
                Vector2.zero, Vector2.zero);
            Stretch(interviewTitleText.rectTransform, new Vector2(0.08f, 0.905f), new Vector2(0.92f, 0.985f),
                Vector2.zero, Vector2.zero);
            interviewTitleText.fontStyle = FontStyles.Bold;
            interviewTitleText.text = UiLoc.T("ui.interview.title", "自由采访");

            // Soft muted EN label — avoids fighting TopBar + Chinese title.
            interviewTitleSubText = CreateUiText(paper.transform, "TitleSub", 10, TextAnchor.UpperCenter,
                new Color(0.42f, 0.38f, 0.34f, 0.42f), Vector2.zero, Vector2.zero);
            Stretch(interviewTitleSubText.rectTransform, new Vector2(0.25f, 0.875f), new Vector2(0.75f, 0.915f),
                Vector2.zero, Vector2.zero);
            interviewTitleSubText.text = UiLoc.T("ui.interview.title_sub", "INTERVIEW");
            interviewTitleSubText.characterSpacing = 4f;

            interviewBannerText = CreateUiText(paper.transform, "Banner", 13, TextAnchor.MiddleLeft,
                new Color(0.55f, 0.28f, 0.18f, 1f), Vector2.zero, Vector2.zero);
            Stretch(interviewBannerText.rectTransform, new Vector2(0.05f, 0.135f), new Vector2(0.95f, 0.22f),
                Vector2.zero, Vector2.zero);
            interviewBannerText.enableWordWrapping = true;
            interviewBannerText.overflowMode = TextOverflowModes.Truncate;
            interviewBannerText.gameObject.SetActive(false);

            // Scrollable chat log (bottom expands when end-warn banner is hidden)
            var logPanel = CreateImage(paper.transform, "ChatLog", new Color(0f, 0f, 0f, 0.001f));
            Stretch(logPanel.rectTransform, new Vector2(0.035f, 0.135f), new Vector2(0.965f, 0.875f),
                Vector2.zero, Vector2.zero);
            logPanel.raycastTarget = false;

            interviewScroll = logPanel.gameObject.AddComponent<ScrollRect>();
            interviewScroll.horizontal = false;
            interviewScroll.vertical = true;
            interviewScroll.movementType = ScrollRect.MovementType.Clamped;
            interviewScroll.scrollSensitivity = 28f;

            var viewport = CreateImage(logPanel.transform, "Viewport", new Color(0f, 0f, 0f, 0.01f));
            StretchFull(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = Vector2.zero;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 14);
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            interviewLogContent = content.transform;

            // Keep a dormant TMP for font apply / fallback callers.
            interviewLogText = CreateUiText(content.transform, "LogFallback", 1, TextAnchor.UpperLeft,
                Color.clear, Vector2.zero, Vector2.zero);
            interviewLogText.gameObject.SetActive(false);

            interviewScroll.viewport = viewport.rectTransform;
            interviewScroll.content = crt;

            // Separator between log and input
            var sep = CreateImage(paper.transform, "Sep", IvSep);
            Stretch(sep.rectTransform, new Vector2(0.05f, 0.125f), new Vector2(0.95f, 0.128f),
                Vector2.zero, Vector2.zero);
            sep.raycastTarget = false;

            // Fixed input bar
            var inputBar = CreateImage(paper.transform, "InputBar", IvInputBg);
            Stretch(inputBar.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.86f, 0.115f),
                Vector2.zero, Vector2.zero);
            inputBar.raycastTarget = true;

            interviewInput = CreateVnInput(inputBar.transform);
            StretchFull(interviewInput.GetComponent<RectTransform>());
            var iirt = interviewInput.GetComponent<RectTransform>();
            iirt.offsetMin = new Vector2(12f, 4f);
            iirt.offsetMax = new Vector2(-8f, -4f);
            interviewInput.lineType = TMP_InputField.LineType.SingleLine;
            interviewInput.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            if (interviewInput.textComponent != null)
            {
                interviewInput.textComponent.color = IvInk;
                interviewInput.textComponent.fontSize = 18;
                ApplyLetterSpacing(interviewInput.textComponent, 0f);
            }
            if (interviewInput.placeholder is TextMeshProUGUI ph)
            {
                ph.color = new Color(0.45f, 0.42f, 0.38f, 0.65f);
                ph.fontSize = 17;
                ph.text = UiLoc.T("ui.interview.input_placeholder", "输入你的问题...");
                ApplyLetterSpacing(ph, 0f);
            }

            var sendGo = new GameObject("Send", typeof(RectTransform), typeof(Image), typeof(Button));
            sendGo.transform.SetParent(paper.transform, false);
            Stretch(sendGo.GetComponent<RectTransform>(),
                new Vector2(0.875f, 0.025f), new Vector2(0.96f, 0.11f),
                Vector2.zero, Vector2.zero);
            interviewSendBtnImage = sendGo.GetComponent<Image>();
            interviewSendBtnImage.color = IvSendBrown;
            interviewSendBtnImage.raycastTarget = true;
            sendGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                SubmitInterviewQuestion();
            });
            interviewSendLabel = CreateUiText(sendGo.transform, "L", 15, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.zero);
            StretchFull(interviewSendLabel.rectTransform);
            interviewSendLabel.text = UiLoc.T("ui.interview.send", "发送");
            interviewSendLabel.raycastTarget = false;
        }

        void BuildInterviewRightColumn(Transform parent)
        {
            var col = new GameObject("RightColumn", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            // ~14% right
            Stretch(col.GetComponent<RectTransform>(),
                new Vector2(0.850f, 0.05f), new Vector2(0.988f, 0.955f),
                Vector2.zero, Vector2.zero);

            BuildInterviewInspirePad(col.transform);
            BuildInterviewToolbarPad(col.transform);
        }

        void BuildInterviewInspirePad(Transform parent)
        {
            var host = new GameObject("InspirePad", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Stretch(host.GetComponent<RectTransform>(),
                new Vector2(0f, 0.36f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, -4f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(host.transform, "Paper");
            AttachPaperclip(paper.transform, new Vector2(0.92f, 1.04f), 32f, 44f, -10f);

            interviewInspireHeaderText = CreateUiText(paper.transform, "Header", 15, TextAnchor.MiddleLeft,
                IvInk, Vector2.zero, Vector2.zero);
            Stretch(interviewInspireHeaderText.rectTransform, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.98f),
                Vector2.zero, Vector2.zero);
            interviewInspireHeaderText.fontStyle = FontStyles.Bold;
            interviewInspireHeaderText.text = UiLoc.T("ui.interview.inspiration", "提问灵感");

            interviewInspireHintText = CreateUiText(paper.transform, "InspireHint", 11, TextAnchor.UpperLeft,
                IvInkMuted, Vector2.zero, Vector2.zero);
            Stretch(interviewInspireHintText.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.90f),
                Vector2.zero, Vector2.zero);
            interviewInspireHintText.text = UiLoc.T("ui.interview.inspire_hint", "点击填入输入框，不会直接发送");
            interviewInspireHintText.enableWordWrapping = true;
            interviewInspireHintText.overflowMode = TextOverflowModes.Truncate;
            interviewInspireHintText.raycastTarget = false;

            interviewHintRoot = new GameObject("Chips", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            interviewHintRoot.SetParent(paper.transform, false);
            Stretch(interviewHintRoot.GetComponent<RectTransform>(),
                new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.80f),
                Vector2.zero, Vector2.zero);
            var vlg = interviewHintRoot.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.padding = new RectOffset(2, 2, 4, 4);

            interviewCoachTipText = CreateUiText(paper.transform, "CoachTip", 12, TextAnchor.UpperLeft,
                IvInkMuted, Vector2.zero, Vector2.zero);
            Stretch(interviewCoachTipText.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.16f),
                Vector2.zero, Vector2.zero);
            interviewCoachTipText.text = "";
            interviewCoachTipText.enableWordWrapping = true;
            interviewCoachTipText.overflowMode = TextOverflowModes.Truncate;
            interviewCoachTipText.raycastTarget = false;
        }

        void BuildInterviewToolbarPad(Transform parent)
        {
            var host = new GameObject("ToolbarPad", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Stretch(host.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(1f, 0.32f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, -4f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(host.transform, "Paper");
            AttachPaperclip(paper.transform, new Vector2(0.08f, 1.06f), 28f, 40f, 18f);

            interviewActionRoot = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            interviewActionRoot.SetParent(paper.transform, false);
            Stretch(interviewActionRoot.GetComponent<RectTransform>(),
                new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f),
                Vector2.zero, Vector2.zero);
            var vlg = interviewActionRoot.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(2, 2, 4, 4);
            interviewToolsRow = null;
        }

        Image CreatePaperFace(Transform parent, string name)
        {
            var paper = CreateImage(parent, name, IvPaper);
            StretchFull(paper.rectTransform);
            paper.raycastTarget = false;
            EnsureLinedPaperSprite();
            if (notebookLinedPaperSprite != null)
            {
                paper.sprite = notebookLinedPaperSprite;
                paper.type = Image.Type.Simple;
                paper.color = new Color(1f, 0.99f, 0.96f, 0.90f);
            }
            return paper;
        }

        void AttachPaperclip(Transform parent, Vector2 anchor, float w, float h, float rotZ)
        {
            var clip = CreateImage(parent, "Paperclip", Color.white);
            var clipRt = clip.rectTransform;
            clipRt.anchorMin = clipRt.anchorMax = anchor;
            clipRt.pivot = new Vector2(0.5f, 0.5f);
            clipRt.anchoredPosition = Vector2.zero;
            clipRt.sizeDelta = new Vector2(w, h);
            clipRt.localEulerAngles = new Vector3(0f, 0f, rotZ);
            var clipSpr = VnArt.GetTitle("deco_paperclip");
            if (clipSpr != null)
            {
                clip.sprite = clipSpr;
                clip.preserveAspect = true;
                clip.color = Color.white;
            }
            else
            {
                clip.color = new Color(0.72f, 0.74f, 0.78f, 0.95f);
            }
            clip.raycastTarget = false;
        }

        void AttachTape(Transform parent, Vector2 anchor, float w, float h, float rotZ)
        {
            var tape = CreateImage(parent, "Tape", Color.white);
            var trt = tape.rectTransform;
            trt.anchorMin = trt.anchorMax = anchor;
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(w, h);
            trt.localEulerAngles = new Vector3(0f, 0f, rotZ);
            var tapeSpr = VnArt.GetTitle("btn_tape_idle");
            if (tapeSpr != null)
            {
                tape.sprite = tapeSpr;
                tape.preserveAspect = true;
                tape.color = new Color(1f, 1f, 1f, 0.85f);
            }
            else
            {
                tape.color = new Color(0.92f, 0.88f, 0.72f, 0.7f);
            }
            tape.raycastTarget = false;
        }

        void EnsureCircleMaskSprite()
        {
            if (interviewCircleMaskSprite != null) return;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float r = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r;
                float dy = y - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((r - d) * 0.5f + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a > 0.5f ? 1f : 0f));
            }
            tex.Apply(false, false);
            interviewCircleMaskSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 64f);
        }

        void RefreshInterviewMeterLabels()
        {
            if (interviewTrustLabel != null)
                interviewTrustLabel.text = UiLoc.T("ui.interview.meter_trust", "信任");
            if (interviewStressLabel != null)
                interviewStressLabel.text = UiLoc.T("ui.interview.meter_pressure", "压力");
            if (interviewFocusLabel != null)
                interviewFocusLabel.text = UiLoc.T("ui.interview.meter_focus", "专注");
            if (interviewMeterCaption != null && interviewMeterCaption.name == "Header")
                interviewMeterCaption.text = UiLoc.T("ui.interview.status_header", "受访者状态");
            if (interviewTitleText != null)
                interviewTitleText.text = UiLoc.T("ui.interview.title", "自由采访");
            if (interviewTitleSubText != null)
            {
                interviewTitleSubText.text = UiLoc.T("ui.interview.title_sub", "INTERVIEW");
                interviewTitleSubText.color = new Color(0.42f, 0.38f, 0.34f, 0.42f);
            }
            if (interviewInspireHeaderText != null)
                interviewInspireHeaderText.text = UiLoc.T("ui.interview.inspiration", "提问灵感");
            if (interviewInspireHintText != null)
                interviewInspireHintText.text = UiLoc.T("ui.interview.inspire_hint", "点击填入输入框，不会直接发送");
            if (interviewSendLabel != null)
                interviewSendLabel.text = UiLoc.T("ui.interview.send", "发送");
        }

        void ClearInterviewChromeButtons()
        {
            foreach (var go in interviewSpawned)
                if (go) Destroy(go);
            interviewSpawned.Clear();
            if (interviewToolsRow != null)
            {
                Destroy(interviewToolsRow.gameObject);
                interviewToolsRow = null;
            }
        }

        Transform EnsureInterviewToolsRow()
        {
            if (interviewToolsRow != null) return interviewToolsRow;
            if (interviewActionRoot == null) return null;

            var go = new GameObject("Tools", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            go.transform.SetParent(interviewActionRoot, false);
            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minHeight = 40f;
            le.preferredHeight = 44f;
            le.flexibleHeight = 1f;
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            interviewToolsRow = go.transform;
            return interviewToolsRow;
        }

        void AddInterviewAction(string label, UnityEngine.Events.UnityAction action, bool primary = false,
            bool fullWidth = false)
        {
            if (interviewActionRoot == null) return;

            bool useFull = primary || fullWidth;
            Transform parent = useFull ? interviewActionRoot : EnsureInterviewToolsRow();
            if (parent == null) return;

            // Plain Loc text only — no emoji/symbol Icon layer (missing glyphs → □ spam).
            var go = new GameObject(primary ? "ActEnd" : "Act", typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = primary
                ? IvEndAccent
                : new Color(0.94f, 0.91f, 0.86f, 0.88f);
            go.GetComponent<Image>().raycastTarget = true;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                action();
            });

            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            if (primary)
            {
                le.minHeight = 44f;
                le.preferredHeight = 48f;
            }
            else if (useFull)
            {
                le.minHeight = 36f;
                le.preferredHeight = 40f;
            }
            else
            {
                le.minHeight = 36f;
                le.preferredHeight = 40f;
                le.flexibleHeight = 1f;
            }

            // Keep call order: primary end stays above tools without reordering siblings.
            var labelTx = CreateUiText(go.transform, "L", primary ? 16 : 13, TextAnchor.MiddleCenter,
                primary ? Color.white : IvInk, Vector2.zero, Vector2.zero);
            Stretch(labelTx.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                Vector2.zero, Vector2.zero);
            labelTx.text = label;
            labelTx.fontStyle = primary ? FontStyles.Bold : FontStyles.Normal;
            labelTx.enableWordWrapping = true;
            labelTx.overflowMode = TextOverflowModes.Truncate;
            labelTx.raycastTarget = false;
            ApplyLetterSpacing(labelTx, 0f);

            interviewSpawned.Add(go);
        }

        void SubmitInterviewQuestion()
        {
            if (mode != Mode.Interview) return;
            if (interviewInput == null || InterviewController.Instance == null)
                return;
            if (interviewLlmCo != null)
                return;
            if (InterviewController.Instance.IsTranslating)
                return;

            var q = (interviewInput.text ?? "").Trim();
            if (string.IsNullOrEmpty(q))
                return;
            interviewInput.text = "";
            HideInterviewBanner();

            var ic = InterviewController.Instance;
            var who = ic.Subject == InterviewSubject.Dafu ? "大福" : "林女士";
            bool llmReady = LlmClient.Instance != null
                && LlmClient.Instance.IsConfigured
                && ic.Subject != InterviewSubject.None;

            var reply = ic.Ask(q, deferSpeakerLines: llmReady);

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

            if (reply != null)
                reply.replyLines = ic.EnforceDafuFoodQuota(reply.replyLines, reply, q);

            if (llmReady && reply != null)
            {
                ic.AppendSpeakerReply(reply);
                ic.EndIfReplyCompleted(reply);
            }
            DialogueHistory.Instance?.Add("小凌", q, "interview");
            RecordInterviewReplyHistory(who, reply, reply?.replyLines);
            if (ic.Subject != InterviewSubject.None)
                RefreshInterviewView();
            else
                SetInterviewChrome(false);
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
            var ruleLines = reply?.replyLines != null
                ? new List<string>(reply.replyLines)
                : new List<string>();

            if (llm == null || !llm.IsConfigured || ic == null || reply == null)
            {
                ruleLines = ic != null
                    ? ic.EnforceDafuFoodQuota(ruleLines, reply, question)
                    : ruleLines;
                FinishInterviewReply(ic, reply, who, ruleLines, null);
                yield break;
            }

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
                facts = "（认知边界：保持困惑，短答「不知道/那是什么」，勿解释人类医疗）";
            }
            var userMsg = ic.BuildFreeAnswerUserMessage(facts, question, reply);

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

            if (aiLines != null && aiLines.Count > 0)
                aiLines = ic.EnforceDafuFoodQuota(aiLines, reply, question);
            else
                ruleLines = ic.EnforceDafuFoodQuota(ruleLines, reply, question);

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
            ic?.EndIfReplyCompleted(reply);
            RecordInterviewReplyHistory(who, reply, lines);
            if (ic != null && ic.Subject != InterviewSubject.None)
                RefreshInterviewView();
            else
                SetInterviewChrome(false);
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

        void ClearInterviewBubbles()
        {
            foreach (var go in interviewBubbleSpawned)
                if (go) Destroy(go);
            interviewBubbleSpawned.Clear();
        }

        void RebuildInterviewChatLog()
        {
            ClearInterviewBubbles();
            if (interviewLogContent == null) return;

            // Layout must run before we measure chat width for bubbles.
            Canvas.ForceUpdateCanvases();
            if (interviewScroll != null && interviewScroll.viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(interviewScroll.viewport);

            var ic = InterviewController.Instance;
            if (ic != null && ic.IsTranslating)
            {
                SpawnInterviewBubble(
                    UiLoc.T("ui.interview.translating", "……"),
                    BubbleKind.System,
                    null);
            }
            else if (ic != null)
            {
                foreach (var line in ic.Log)
                    SpawnInterviewLogLine(line);
            }

            Canvas.ForceUpdateCanvases();
            if (interviewLogContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(interviewLogContent as RectTransform);
            if (interviewScroll != null)
                interviewScroll.verticalNormalizedPosition = 0f;
        }

        enum BubbleKind { Player, Npc, System }

        void SpawnInterviewLogLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;

            if (line.StartsWith("小凌：") || line.StartsWith("小凌:"))
            {
                var body = StripSpeakerPrefix(line);
                SpawnInterviewBubble(body, BubbleKind.Player, "小凌");
            }
            else if (line.StartsWith("大福：") || line.StartsWith("大福:")
                     || line.StartsWith("林女士：") || line.StartsWith("林女士:"))
            {
                var body = StripSpeakerPrefix(line);
                var who = InterviewController.Instance != null
                          && InterviewController.Instance.Subject == InterviewSubject.Lin
                    ? "林女士"
                    : "大福";
                SpawnInterviewBubble(body, BubbleKind.Npc, who);
            }
            else
            {
                SpawnInterviewBubble(line, BubbleKind.System, null);
            }
        }

        static string StripSpeakerPrefix(string line)
        {
            int colon = line.IndexOf('：');
            if (colon < 0) colon = line.IndexOf(':');
            if (colon >= 0 && colon + 1 < line.Length)
                return line.Substring(colon + 1).Trim();
            return line;
        }

        void SpawnInterviewBubble(string body, BubbleKind kind, string avatarWho)
        {
            if (interviewLogContent == null || string.IsNullOrEmpty(body))
                return;

            float scale = GameSettings.FontSizeScale;
            bool isSystem = kind == BubbleKind.System;
            bool isMaterial = isSystem && (body.Contains("【素材】") || body.Contains("[Materials]")
                                          || body.Contains("[Material]"));
            bool isAction = isSystem && (body.StartsWith("（") || body.StartsWith("("));

            const float AvatarSize = 72f;
            const float AvatarGap = 12f;
            float contentW = EstimateInterviewChatContentWidth();
            // NPC/player bubbles: almost full chat row minus avatar. System: ~92% centered.
            float bubbleW = isSystem
                ? Mathf.Clamp(contentW * 0.92f, 640f, 1400f)
                : Mathf.Clamp(contentW - AvatarSize - AvatarGap - 16f, 560f, 1400f);

            var row = new GameObject("Bubble", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            row.transform.SetParent(interviewLogContent, false);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = AvatarGap;
            hlg.childAlignment = isSystem
                ? TextAnchor.MiddleCenter
                : (kind == BubbleKind.Player ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(4, 4, 2, 2);
            var rowLe = row.GetComponent<LayoutElement>();
            rowLe.minHeight = isSystem ? 32f : AvatarSize;
            rowLe.flexibleWidth = 1f;
            rowLe.preferredWidth = -1f;

            if (kind == BubbleKind.Player)
            {
                var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
                spacer.transform.SetParent(row.transform, false);
                var sle = spacer.GetComponent<LayoutElement>();
                sle.flexibleWidth = 1f;
                sle.minWidth = 8f;
            }
            else if (isSystem)
            {
                var spacerL = new GameObject("SpacerL", typeof(RectTransform), typeof(LayoutElement));
                spacerL.transform.SetParent(row.transform, false);
                var sle = spacerL.GetComponent<LayoutElement>();
                sle.flexibleWidth = 1f;
                sle.minWidth = 8f;
            }

            Image avatarImg = null;
            if (!isSystem && !string.IsNullOrEmpty(avatarWho))
                avatarImg = CreateCircularAvatar(row.transform, avatarWho, AvatarSize);

            var bubbleGo = new GameObject("Face", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            bubbleGo.transform.SetParent(row.transform, false);
            var bubble = bubbleGo.GetComponent<Image>();
            if (kind == BubbleKind.Player)
                bubble.color = IvBubblePlayer;
            else if (kind == BubbleKind.Npc)
                bubble.color = IvBubbleNpc;
            else if (isMaterial)
                bubble.color = IvBubbleMaterial;
            else
                bubble.color = IvBubbleSystem;
            bubble.raycastTarget = false;

            // Lock bubble width — TMP preferred-width otherwise collapses to ~1 character.
            var ble = bubbleGo.GetComponent<LayoutElement>();
            ble.minWidth = bubbleW;
            ble.preferredWidth = bubbleW;
            ble.flexibleWidth = 0f;
            ble.minHeight = isSystem ? 28f : 40f;

            var brt = bubbleGo.GetComponent<RectTransform>();
            brt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bubbleW);

            var bVlg = bubbleGo.GetComponent<VerticalLayoutGroup>();
            int padX = isSystem ? 16 : 18;
            int padY = isSystem ? 8 : 12;
            bVlg.padding = new RectOffset(padX, padX, padY, padY);
            bVlg.childControlWidth = true;
            bVlg.childControlHeight = true;
            bVlg.childForceExpandWidth = true;
            bVlg.childForceExpandHeight = false;
            var fitter = bubbleGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var tgo = new GameObject("T", typeof(RectTransform), typeof(LayoutElement));
            tgo.transform.SetParent(bubbleGo.transform, false);
            var tx = tgo.AddComponent<TextMeshProUGUI>();
            tx.font = font;
            tx.fontSize = Mathf.RoundToInt((isSystem ? 15f : 17f) * scale);
            tx.color = isSystem ? IvInkMuted : IvInk;
            tx.alignment = VnText.ToAlignment(isSystem ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft);
            tx.text = body;
            tx.fontStyle = isAction ? FontStyles.Italic : FontStyles.Normal;
            tx.enableWordWrapping = true;
            tx.overflowMode = TextOverflowModes.Overflow;
            tx.raycastTarget = false;
            ApplyLetterSpacing(tx, 0f);

            float textW = Mathf.Max(120f, bubbleW - padX * 2);
            var tLe = tgo.GetComponent<LayoutElement>();
            tLe.minWidth = textW;
            tLe.preferredWidth = textW;
            tLe.flexibleWidth = 1f;
            tgo.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textW);

            if (kind == BubbleKind.Player && avatarImg != null)
                avatarImg.transform.SetAsLastSibling();
            else if (kind == BubbleKind.Npc && avatarImg != null)
                avatarImg.transform.SetAsFirstSibling();

            if (kind != BubbleKind.Player)
            {
                var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
                spacer.transform.SetParent(row.transform, false);
                var sle = spacer.GetComponent<LayoutElement>();
                sle.flexibleWidth = 1f;
                sle.minWidth = 8f;
            }

            interviewBubbleSpawned.Add(row);
        }

        /// <summary>
        /// Usable width of the chat scroll viewport for bubble sizing.
        /// </summary>
        float EstimateInterviewChatContentWidth()
        {
            if (interviewScroll != null && interviewScroll.viewport != null)
            {
                float w = interviewScroll.viewport.rect.width;
                if (w >= 120f) return w;
            }

            if (interviewLogContent != null)
            {
                var rt = interviewLogContent as RectTransform;
                if (rt != null)
                {
                    float w = rt.rect.width;
                    if (w < 8f && rt.parent is RectTransform parentRt)
                        w = parentRt.rect.width;
                    if (w >= 120f)
                        return w;
                }
            }

            // Fallback: ~68% of 1920 reference minus paper chrome.
            return 1180f;
        }

        Image CreateCircularAvatar(Transform parent, string who, float size = 72f)
        {
            EnsureCircleMaskSprite();
            var host = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(Mask),
                typeof(LayoutElement));
            host.transform.SetParent(parent, false);
            var hostImg = host.GetComponent<Image>();
            hostImg.sprite = interviewCircleMaskSprite;
            hostImg.color = Color.white;
            hostImg.raycastTarget = false;
            host.GetComponent<Mask>().showMaskGraphic = false;
            var le = host.GetComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.minWidth = size;
            le.minHeight = size;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
            var hrt = host.GetComponent<RectTransform>();
            hrt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            hrt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);

            var face = CreateImage(host.transform, "Face", Color.white);
            // Zoom into upper body / head — full-body portraits look like noise at tiny sizes.
            var frt = face.rectTransform;
            frt.anchorMin = new Vector2(-0.25f, 0.20f);
            frt.anchorMax = new Vector2(1.25f, 1.55f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            face.preserveAspect = false;
            face.raycastTarget = false;

            Sprite spr = null;
            // Prefer the large left-column portrait already shown for the interviewee.
            if (who != "小凌" && interviewPortraitImage != null && interviewPortraitImage.enabled
                && interviewPortraitImage.sprite != null)
            {
                spr = interviewPortraitImage.sprite;
            }
            else
            {
                string key = who == "小凌"
                    ? VnArt.ResolvePortrait("小凌", LineSpeaker.Character, null)
                    : VnArt.ResolvePortrait(who, LineSpeaker.Character, null);
                if (who != "小凌" && InterviewController.Instance != null)
                {
                    var expression = InterviewPortraitService.PickExpression(
                        InterviewPortraitService.BuildContext(InterviewController.Instance));
                    key = VnArt.ResolvePortrait(who, LineSpeaker.Character, expression);
                }
                spr = VnArt.GetPortrait(key);
            }

            if (spr != null)
            {
                face.sprite = spr;
                face.color = Color.white;
                if (spr.texture != null && spr.texture.filterMode == FilterMode.Point)
                    spr.texture.filterMode = FilterMode.Bilinear;
            }
            else
            {
                face.color = new Color(0.75f, 0.72f, 0.68f, 1f);
            }
            return hostImg;
        }

        void UpdateInterviewMeters(InterviewerStats st)
        {
            RefreshInterviewMeterLabels();
            if (st == null)
            {
                SetMeterBarFill(interviewTrustFill, 0);
                SetMeterBarFill(interviewStressFill, 0);
                SetMeterBarFill(interviewFocusFill, 0);
                SetMeterValueText(interviewTrustValue, null);
                SetMeterValueText(interviewStressValue, null);
                SetMeterValueText(interviewFocusValue, null);
                return;
            }

            SetMeterBarFill(interviewTrustFill, st.trust);
            SetMeterBarFill(interviewStressFill, st.stress);
            SetMeterBarFill(interviewFocusFill, st.attention);
            SetMeterValueText(interviewTrustValue, st.trust);
            SetMeterValueText(interviewStressValue, st.stress);
            SetMeterValueText(interviewFocusValue, st.attention);
        }

        static void SetMeterValueText(TextMeshProUGUI valueTx, int? value0to100)
        {
            if (valueTx == null) return;
            valueTx.text = value0to100.HasValue
                ? Mathf.Clamp(value0to100.Value, 0, 100).ToString()
                : "—";
        }

        static void SetMeterBarFill(Image fill, int value0to100)
        {
            if (fill == null) return;
            float t = Mathf.Clamp01(value0to100 / 100f);
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(t, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void SetMeterFill(Image[] segs, int value0to100, Color fill)
        {
            if (segs == null) return;
            int n = segs.Length;
            int filled = Mathf.Clamp(Mathf.RoundToInt(value0to100 / 100f * n), 0, n);
            for (int i = 0; i < segs.Length; i++)
            {
                if (segs[i] == null) continue;
                segs[i].color = i < filled ? fill : IvBarTrack;
            }
        }

        void ApplyInterviewPortraits()
        {
            var ic = InterviewController.Instance;
            if (ic == null) return;

            // Three-column layout: left pad only — hide stage VN portraits.
            SetPortrait(null);

            var who = ic.Subject == InterviewSubject.Dafu ? "大福" : "林女士";
            var expression = InterviewPortraitService.PickExpression(
                InterviewPortraitService.BuildContext(ic));
            var key = VnArt.ResolvePortrait(who, LineSpeaker.Character, expression);
            SetInterviewLeftPortrait(key);
        }

        void SetInterviewLeftPortrait(string portraitKey)
        {
            if (interviewPortraitImage == null) return;
            if (string.IsNullOrEmpty(portraitKey))
            {
                interviewPortraitImage.sprite = null;
                interviewPortraitImage.enabled = false;
                return;
            }

            var sprite = VnArt.GetPortrait(portraitKey);
            if (sprite == null)
            {
                interviewPortraitImage.sprite = null;
                interviewPortraitImage.enabled = false;
                return;
            }

            interviewPortraitImage.sprite = sprite;
            interviewPortraitImage.color = Color.white;
            interviewPortraitImage.enabled = true;
            interviewPortraitImage.preserveAspect = true;
        }

        void LayoutInterviewPortraitSlot(Sprite sprite)
        {
            // Stage portrait slot unused during interview; left-column Image is driven instead.
            if (portraitImage != null)
            {
                portraitImage.enabled = false;
                portraitImage.gameObject.SetActive(false);
            }
            _ = sprite;
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
            // Paper title owns the header; TopBar chapter chip stays hidden while interviewing.
            if (chapterChip != null)
                chapterChip.gameObject.SetActive(false);
            if (objectiveText != null)
                objectiveText.gameObject.SetActive(true);
            SetStageBackground(subject == InterviewSubject.Lin ? "咖啡馆_午后" : "保安亭_傍晚");
            RefreshHeader();
            HideInterviewBanner();

            if (interviewSubjectText != null)
            {
                interviewSubjectText.text = subject == InterviewSubject.Dafu
                    ? UiLoc.T("ui.interview.subject_dafu", "大福")
                    : UiLoc.T("ui.interview.subject_lin", "林女士");
            }

            interviewInput.gameObject.SetActive(true);
            interviewInput.text = "";
            if (interviewInput.placeholder is TextMeshProUGUI ph)
            {
                ph.text = UiLoc.T("ui.interview.input_placeholder", "输入你的问题...");
            }

            ClearButtons();
            RefreshInterviewView();
        }

        void HideInterviewBanner()
        {
            if (interviewBannerText != null)
            {
                interviewBannerText.text = "";
                interviewBannerText.gameObject.SetActive(false);
            }
            SetInterviewLogBottom(0.135f);
        }

        void ShowInterviewBanner(string msg)
        {
            if (interviewBannerText == null) return;
            interviewBannerText.gameObject.SetActive(true);
            interviewBannerText.text = msg;
            ApplyLetterSpacing(interviewBannerText, 0f);
            SetInterviewLogBottom(0.225f);
        }

        void SetInterviewLogBottom(float minY)
        {
            if (interviewScroll == null) return;
            var rt = interviewScroll.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.035f, minY);
            rt.anchorMax = new Vector2(0.965f, 0.875f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void RefreshInterviewView()
        {
            // After End() subject is cleared — do not force interview chrome back open
            // (meter force-out / confirm-end navigate away asynchronously).
            if (InterviewController.Instance == null
                || InterviewController.Instance.Subject == InterviewSubject.None)
                return;

            if (mode != Mode.Interview)
                mode = Mode.Interview;
            SetInterviewChrome(true);
            RefreshHeader();
            RefreshInterviewMeterLabels();

            if (interviewSubjectText != null)
            {
                interviewSubjectText.text = InterviewController.Instance.Subject == InterviewSubject.Dafu
                    ? UiLoc.T("ui.interview.subject_dafu", "大福")
                    : UiLoc.T("ui.interview.subject_lin", "林女士");
            }

            ClearInterviewChromeButtons();
            ApplyStageArt();
            ApplyInterviewPortraits();

            // Portrait must be ready so chat avatars can reuse the same crisp sprite.
            RebuildInterviewChatLog();
            UpdateInterviewMeters(InterviewController.Instance?.Stats);

            // End stands alone (danger accent); quieter tools on a second row.
            AddInterviewAction(UiLoc.T("ui.interview.end_short", "结束"), TryEndInterview, primary: true);
            if (InterviewController.Instance.IsReinterviewFromWriting)
                AddInterviewAction(UiLoc.T("ui.interview.back_writing", "返回写稿"), () =>
                {
                    SetInterviewChrome(false);
                    InterviewController.Instance.AbandonToWriting();
                });
            AddInterviewAction(UiLoc.T("ui.menu.backlog", "回看"), OpenBacklog);
            AddInterviewAction(UiLoc.T("ui.menu.notebook", "笔记"), OpenNotebook);
            AddInterviewAction(UiLoc.T("ui.menu", "菜单"), OpenMenu);

            RefreshInterviewHints();
        }

        void ClearInterviewPresets()
        {
            foreach (var go in interviewPresetSpawned)
                if (go) Destroy(go);
            interviewPresetSpawned.Clear();
        }

        void RefreshInterviewHints()
        {
            ClearInterviewPresets();
            if (InterviewController.Instance == null)
            {
                if (interviewHintRoot != null)
                    interviewHintRoot.gameObject.SetActive(false);
                if (interviewCoachTipText != null)
                    interviewCoachTipText.text = "";
                return;
            }

            var subject = InterviewController.Instance.Subject;
            if (subject == InterviewSubject.None)
            {
                if (interviewHintRoot != null)
                    interviewHintRoot.gameObject.SetActive(false);
                if (interviewCoachTipText != null)
                    interviewCoachTipText.text = "";
                return;
            }

            var bundle = InterviewHintService.GetHints(
                InterviewHintService.BuildContext(InterviewController.Instance));

            if (interviewCoachTipText != null)
            {
                interviewCoachTipText.text = bundle?.CoachTip ?? "";
                ApplyLetterSpacing(interviewCoachTipText, 0f);
            }

            var presets = bundle?.AskChips;
            if (interviewHintRoot == null)
                return;
            if (presets == null || presets.Count == 0)
            {
                interviewHintRoot.gameObject.SetActive(false);
                return;
            }

            interviewHintRoot.gameObject.SetActive(true);
            int shown = Mathf.Min(3, presets.Count);
            for (int i = 0; i < shown; i++)
                SpawnInterviewPresetChip(presets[i], i);
        }

        void RefreshInterviewPresets() => RefreshInterviewHints();

        void SpawnInterviewPresetChip(string question, int colorIdx)
        {
            if (interviewHintRoot == null || string.IsNullOrEmpty(question))
                return;

            string label = question.Length <= 18 ? question : question.Substring(0, 17) + "…";
            var go = new GameObject("Preset", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement), typeof(Outline));
            go.transform.SetParent(interviewHintRoot, false);
            // Soft paper-note chips — fill input only, not primary CTAs.
            go.GetComponent<Image>().color = IvChipFill;
            go.GetComponent<Image>().raycastTarget = true;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = IvChipOutline;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = true;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 46f;
            le.preferredHeight = 50f;
            le.flexibleWidth = 1f;
            string fill = question;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                FillInterviewInput(fill);
            });

            var mark = CreateUiText(go.transform, "Mark", 12, TextAnchor.MiddleCenter,
                IvChipMarkTints[colorIdx % IvChipMarkTints.Length],
                Vector2.zero, Vector2.zero);
            Stretch(mark.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(6f, 4f), new Vector2(24f, -4f));
            mark.text = IvChipMarks[colorIdx % IvChipMarks.Length];
            mark.fontStyle = FontStyles.Bold;
            mark.raycastTarget = false;

            var tgo = new GameObject("L", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(26f, 4f), new Vector2(-8f, -4f));
            var tx = tgo.AddComponent<TextMeshProUGUI>();
            tx.font = font;
            tx.fontSize = 12;
            tx.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
            tx.color = IvInkMuted;
            tx.text = label;
            tx.raycastTarget = false;
            tx.enableWordWrapping = true;
            tx.overflowMode = TextOverflowModes.Truncate;
            ApplyLetterSpacing(tx, 0f);
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
            if (statusText)
                statusText.text = UiLoc.T("ui.interview.preset_filled", "已填入预设提问，可修改后发送");
            if (interviewCoachTipText != null)
                interviewCoachTipText.text = UiLoc.T("ui.interview.preset_filled", "已填入预设提问，可修改后发送");
        }

        void TryEndInterview()
        {
            if (!InterviewController.Instance.CanComplete())
            {
                var msg = UiLoc.T("ui.interview.end_warn", "现在结束的话，似乎还有不少事情没有问清楚。")
                          + "\n" + InterviewController.Instance.MissingSummary();
                ShowInterviewBanner(msg);

                ClearInterviewChromeButtons();
                AddInterviewAction(UiLoc.T("ui.interview.continue", "继续采访"), () =>
                {
                    HideInterviewBanner();
                    RefreshInterviewView();
                }, fullWidth: true);
                AddInterviewAction(UiLoc.T("ui.interview.confirm_end", "确认结束"), () =>
                {
                    HideInterviewBanner();
                    SetInterviewChrome(false);
                    InterviewController.Instance.End(true);
                }, primary: true);
                return;
            }
            HideInterviewBanner();
            SetInterviewChrome(false);
            InterviewController.Instance.End(true);
        }

        void ApplyInterviewFonts()
        {
            float scale = GameSettings.FontSizeScale;
            if (interviewTitleText != null)
            {
                interviewTitleText.font = font;
                interviewTitleText.fontSize = Mathf.RoundToInt(26f * scale);
                interviewTitleText.color = IvInk;
            }
            if (interviewTitleSubText != null)
            {
                interviewTitleSubText.font = font;
                interviewTitleSubText.fontSize = Mathf.RoundToInt(10f * scale);
                interviewTitleSubText.color = new Color(0.42f, 0.38f, 0.34f, 0.42f);
            }
            if (interviewInspireHeaderText != null)
            {
                interviewInspireHeaderText.font = font;
                interviewInspireHeaderText.fontSize = Mathf.RoundToInt(14f * scale);
            }
            if (interviewInspireHintText != null)
            {
                interviewInspireHintText.font = font;
                interviewInspireHintText.fontSize = Mathf.RoundToInt(11f * scale);
                interviewInspireHintText.color = IvInkMuted;
            }
            if (interviewBannerText != null)
            {
                interviewBannerText.font = font;
                interviewBannerText.fontSize = Mathf.RoundToInt(13f * scale);
            }
            if (interviewSubjectText != null)
            {
                interviewSubjectText.font = font;
                interviewSubjectText.fontSize = Mathf.RoundToInt(15f * scale);
            }
            if (interviewMeterCaption != null)
            {
                interviewMeterCaption.font = font;
                interviewMeterCaption.fontSize = Mathf.RoundToInt(13f * scale);
            }
            if (interviewCoachTipText != null)
            {
                interviewCoachTipText.font = font;
                interviewCoachTipText.fontSize = Mathf.RoundToInt(12f * scale);
            }
            ApplyMeterLabelFont(interviewTrustLabel, scale);
            ApplyMeterLabelFont(interviewStressLabel, scale);
            ApplyMeterLabelFont(interviewFocusLabel, scale);
            ApplyMeterValueFont(interviewTrustValue, scale);
            ApplyMeterValueFont(interviewStressValue, scale);
            ApplyMeterValueFont(interviewFocusValue, scale);
            if (interviewInput != null)
            {
                interviewInput.fontAsset = font;
                interviewInput.pointSize = Mathf.RoundToInt(17f * scale);
                if (interviewInput.textComponent != null)
                {
                    interviewInput.textComponent.font = font;
                    interviewInput.textComponent.fontSize = Mathf.RoundToInt(17f * scale);
                    interviewInput.textComponent.color = IvInk;
                    ApplyLetterSpacing(interviewInput.textComponent, 0f);
                }
                if (interviewInput.placeholder is TextMeshProUGUI ph)
                {
                    ph.font = font;
                    ph.fontSize = Mathf.RoundToInt(16f * scale);
                    ApplyLetterSpacing(ph, 0f);
                }
            }
        }

        void ApplyMeterLabelFont(TextMeshProUGUI tx, float scale)
        {
            if (tx == null) return;
            tx.font = font;
            tx.fontSize = Mathf.RoundToInt(13f * scale);
            tx.color = IvInk;
            tx.fontStyle = FontStyles.Bold;
            tx.enableWordWrapping = false;
            tx.overflowMode = TextOverflowModes.Overflow;
        }

        void ApplyMeterValueFont(TextMeshProUGUI tx, float scale)
        {
            if (tx == null) return;
            tx.font = font;
            tx.fontSize = Mathf.RoundToInt(12f * scale);
            tx.fontStyle = FontStyles.Bold;
            tx.enableWordWrapping = false;
            tx.overflowMode = TextOverflowModes.Overflow;
        }
    }
}
