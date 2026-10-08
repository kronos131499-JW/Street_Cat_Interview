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
        Image interviewBannerCard;
        Image interviewSendBtnImage;
        TextMeshProUGUI interviewSendLabel;
        Transform interviewLogContent;
        Transform interviewToolsRow;
        readonly List<GameObject> interviewBubbleSpawned = new List<GameObject>();
        Sprite interviewCircleMaskSprite;
        readonly Dictionary<int, Sprite> interviewAvatarCrops = new Dictionary<int, Sprite>();

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
            // ~13% status + portrait
            // FreeInterview reference canvas 1675×939: left paper occupies ~19% of the width.
            Stretch(col.GetComponent<RectTransform>(),
                new Vector2(0.020f, 0.04f), new Vector2(0.210f, 0.97f),
                Vector2.zero, Vector2.zero);

            BuildInterviewStatusPad(col.transform);
            BuildInterviewPortraitPad(col.transform);
        }

        void BuildInterviewStatusPad(Transform parent)
        {
            var host = new GameObject("StatusPad", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Stretch(host.GetComponent<RectTransform>(),
                new Vector2(0f, 0.52f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, -4f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(host.transform, "Paper");
            StampFreePlate(paper, FreeInterviewArt.StatusPanel, preserveAspect: true);

            // Caption stays for label refresh, but the blank status sheet has no header band.
            var header = CreateUiText(paper.transform, "Header", 14, TextAnchor.MiddleLeft, IvInk,
                Vector2.zero, Vector2.zero);
            header.gameObject.SetActive(false);
            interviewMeterCaption = header;

            // Three separate bands. A full-card row stacks every bar on top of the others.
            interviewTrustFill = BuildFreeMeter(paper.transform, "Trust",
                FreeInterviewArt.TrustIcon, FreeInterviewArt.TrustFill,
                new Vector2(0.04f, 0.66f), new Vector2(0.96f, 0.98f),
                out interviewTrustLabel, out interviewTrustValue);
            interviewStressFill = BuildFreeMeter(paper.transform, "Stress",
                FreeInterviewArt.PressureIcon, FreeInterviewArt.PressureFill,
                new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.64f),
                out interviewStressLabel, out interviewStressValue);
            interviewFocusFill = BuildFreeMeter(paper.transform, "Focus",
                FreeInterviewArt.FocusIcon, FreeInterviewArt.FocusFill,
                new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.32f),
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

            valueTx = CreateUiText(rowGo.transform, "Value", 13, TextAnchor.MiddleRight, IvInk,
                Vector2.zero, Vector2.zero);
            Stretch(valueTx.rectTransform, new Vector2(0.55f, 0.45f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            valueTx.fontStyle = FontStyles.Bold;
            valueTx.enableWordWrapping = false;
            valueTx.raycastTarget = false;
            valueTx.text = "—";
            valueTx.color = new Color(0.12f, 0.10f, 0.08f, 1f);

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

        /// <summary>
        /// One meter on the blank status sheet: icon, name, empty slot, crayon fill.
        /// Numbers stay off the paper; the fill length is the value.
        /// </summary>
        Image BuildFreeMeter(Transform parent, string name, Sprite icon, Sprite fillSprite,
            Vector2 bandMin, Vector2 bandMax,
            out TextMeshProUGUI labelTx, out TextMeshProUGUI valueTx)
        {
            var rowGo = new GameObject(name, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            Stretch(rowGo.GetComponent<RectTransform>(), bandMin, bandMax, Vector2.zero, Vector2.zero);

            var iconImg = CreateImage(rowGo.transform, "Icon", Color.white);
            Stretch(iconImg.rectTransform, new Vector2(0f, 0.28f), new Vector2(0.16f, 0.92f),
                Vector2.zero, Vector2.zero);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            if (icon != null)
            {
                iconImg.sprite = icon;
                iconImg.color = Color.white;
            }

            labelTx = CreateUiText(rowGo.transform, "Label", 16, TextAnchor.LowerLeft, FreeInterviewArt.Ink,
                Vector2.zero, Vector2.zero);
            Stretch(labelTx.rectTransform, new Vector2(0.18f, 0.52f), new Vector2(0.70f, 1f),
                Vector2.zero, Vector2.zero);
            labelTx.fontStyle = FontStyles.Bold;
            labelTx.enableWordWrapping = false;
            labelTx.overflowMode = TextOverflowModes.Overflow;
            labelTx.raycastTarget = false;

            valueTx = CreateUiText(rowGo.transform, "Value", 16, TextAnchor.LowerRight, FreeInterviewArt.Ink,
                Vector2.zero, Vector2.zero);
            Stretch(valueTx.rectTransform, new Vector2(0.62f, 0.52f), new Vector2(0.96f, 1f),
                Vector2.zero, Vector2.zero);
            valueTx.fontStyle = FontStyles.Bold;
            valueTx.enableWordWrapping = false;
            valueTx.raycastTarget = false;

            var track = CreateImage(rowGo.transform, "Track", IvBarTrack);
            Stretch(track.rectTransform, new Vector2(0.18f, 0.08f), new Vector2(0.96f, 0.42f),
                Vector2.zero, Vector2.zero);
            track.raycastTarget = false;
            var empty = FreeInterviewArt.BarEmpty;
            if (empty != null)
            {
                track.sprite = empty;
                track.type = Image.Type.Simple;
                track.color = Color.white;
            }

            var fillImg = CreateImage(track.transform, "Fill", Color.white);
            StretchFull(fillImg.rectTransform);
            fillImg.rectTransform.offsetMin = new Vector2(3f, 3f);
            fillImg.rectTransform.offsetMax = new Vector2(-3f, -3f);
            fillImg.raycastTarget = false;
            if (fillSprite != null)
            {
                fillImg.sprite = fillSprite;
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImg.fillAmount = 0f;
                fillImg.color = Color.white;
            }
            else
            {
                fillImg.color = IvTrust;
                fillImg.type = Image.Type.Simple;
            }
            return fillImg;
        }

        static void StampFreePlate(Image image, Sprite sprite, bool preserveAspect)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
            image.type = sprite.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
        }

        void BuildInterviewPortraitPad(Transform parent)
        {
            var host = new GameObject("PortraitPad", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Stretch(host.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(1f, 0.48f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(4f, -5f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(host.transform, "Paper");
            StampFreePlate(paper, FreeInterviewArt.Photo, preserveAspect: true);

            // Brown window inside 01_photo_background. The frame itself stays unclipped.
            var window = new GameObject("PhotoWindow", typeof(RectTransform), typeof(RectMask2D));
            window.transform.SetParent(paper.transform, false);
            Stretch(window.GetComponent<RectTransform>(),
                new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.92f),
                Vector2.zero, Vector2.zero);

            interviewPortraitImage = CreateImage(window.transform, "Portrait", Color.white);
            Stretch(interviewPortraitImage.rectTransform,
                new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.98f),
                Vector2.zero, Vector2.zero);
            interviewPortraitImage.preserveAspect = true;
            interviewPortraitImage.raycastTarget = false;
            interviewPortraitImage.enabled = false;
            // The polaroid already prints the name. Do not stack a live "Dafu" label on it.
        }

        void BuildInterviewCenterColumn(Transform parent)
        {
            var col = new GameObject("CenterColumn", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            // ~60% center chat — leave room for EN Ask Ideas chips on the right
            Stretch(col.GetComponent<RectTransform>(),
                new Vector2(0.215f, 0.04f), new Vector2(0.810f, 0.97f),
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

            // Ghost EN subtitle removed — fought the real title and looked like a layering bug.
            interviewTitleSubText = CreateUiText(paper.transform, "TitleSub", 10, TextAnchor.UpperCenter,
                new Color(0.42f, 0.38f, 0.34f, 0.42f), Vector2.zero, Vector2.zero);
            Stretch(interviewTitleSubText.rectTransform, new Vector2(0.25f, 0.875f), new Vector2(0.75f, 0.915f),
                Vector2.zero, Vector2.zero);
            interviewTitleSubText.text = UiLoc.T("ui.interview.title_sub", "INTERVIEW");
            interviewTitleSubText.characterSpacing = 4f;
            interviewTitleSubText.gameObject.SetActive(false);

            // Quiet note on the lower sheet. Kept clear of the typing line.
            interviewBannerCard = CreateImage(paper.transform, "BannerCard", new Color(0.93f, 0.86f, 0.74f, 0.55f));
            Stretch(interviewBannerCard.rectTransform, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.48f),
                Vector2.zero, Vector2.zero);
            interviewBannerCard.raycastTarget = false;
            interviewBannerCard.gameObject.SetActive(false);
            var bannerRule = CreateImage(interviewBannerCard.transform, "Rule", new Color(0.62f, 0.32f, 0.18f, 0.9f));
            Stretch(bannerRule.rectTransform, new Vector2(0f, 0.06f), new Vector2(0.012f, 0.94f),
                Vector2.zero, Vector2.zero);
            bannerRule.raycastTarget = false;

            interviewBannerText = CreateUiText(paper.transform, "Banner", 18, TextAnchor.UpperLeft,
                new Color(0.33f, 0.20f, 0.14f, 1f), Vector2.zero, Vector2.zero);
            Stretch(interviewBannerText.rectTransform, new Vector2(0.08f, 0.17f), new Vector2(0.93f, 0.46f),
                Vector2.zero, Vector2.zero);
            interviewBannerText.enableWordWrapping = true;
            interviewBannerText.overflowMode = TextOverflowModes.Overflow;
            interviewBannerText.lineSpacing = 6f;
            interviewBannerText.margin = new Vector4(6f, 4f, 8f, 4f);
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
            vlg.padding = new RectOffset(8, 8, 18, 16);
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

            // Fixed input bar — solid field; ArtPack typing-frame sprite is a full mockup strip
            // and fights the real TMP field when stamped here.
            var inputBar = CreateImage(paper.transform, "InputBar", IvInputBg);
            Stretch(inputBar.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.82f, 0.115f),
                Vector2.zero, Vector2.zero);
            StampFreePlate(inputBar, FreeInterviewArt.Input, preserveAspect: false);
            inputBar.raycastTarget = true;

            interviewInput = CreateVnInput(inputBar.transform);
            StretchFull(interviewInput.GetComponent<RectTransform>());
            var iirt = interviewInput.GetComponent<RectTransform>();
            iirt.offsetMin = new Vector2(14f, 6f);
            iirt.offsetMax = new Vector2(-10f, -6f);
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
                ph.color = new Color(0.28f, 0.24f, 0.20f, 0.78f);
                ph.fontSize = 17;
                ph.text = UiLoc.T("ui.interview.input_placeholder", "输入你的问题...");
                ApplyLetterSpacing(ph, 0f);
            }
            // 打字框 sprite is 949px wide; the brown fold ends at x≈80 (~8%).
            // Keep placeholder and typed text in the white field to the right of it.
            if (interviewInput.textViewport != null)
            {
                var area = interviewInput.textViewport;
                area.anchorMin = new Vector2(0.10f, 0f);
                area.anchorMax = Vector2.one;
                area.offsetMin = new Vector2(4f, 2f);
                area.offsetMax = new Vector2(-8f, -2f);
            }

            var sendGo = new GameObject("Send", typeof(RectTransform), typeof(Image), typeof(Button));
            sendGo.transform.SetParent(paper.transform, false);
            Stretch(sendGo.GetComponent<RectTransform>(),
                new Vector2(0.835f, 0.025f), new Vector2(0.96f, 0.11f),
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
            // ~23% right — EN ask chips need wrap width
            Stretch(col.GetComponent<RectTransform>(),
                new Vector2(0.825f, 0.04f), new Vector2(0.985f, 0.97f),
                Vector2.zero, Vector2.zero);

            BuildInterviewInspirePad(col.transform);
            BuildInterviewToolbarPad(col.transform);
        }

        void BuildInterviewInspirePad(Transform parent)
        {
            var host = new GameObject("InspirePad", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Stretch(host.GetComponent<RectTransform>(),
                new Vector2(0f, 0.40f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, -4f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(host.transform, "Paper");
            // Skip paperclip on this pad — it eats chip space and looks cluttered.

            interviewInspireHeaderText = CreateUiText(paper.transform, "Header", 15, TextAnchor.MiddleLeft,
                IvInk, Vector2.zero, Vector2.zero);
            Stretch(interviewInspireHeaderText.rectTransform, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.985f),
                Vector2.zero, Vector2.zero);
            interviewInspireHeaderText.fontStyle = FontStyles.Bold;
            interviewInspireHeaderText.text = UiLoc.T("ui.interview.inspiration", "提问灵感");

            interviewInspireHintText = CreateUiText(paper.transform, "InspireHint", 11, TextAnchor.UpperLeft,
                new Color(0.22f, 0.18f, 0.14f, 0.90f), Vector2.zero, Vector2.zero);
            Stretch(interviewInspireHintText.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.91f),
                Vector2.zero, Vector2.zero);
            interviewInspireHintText.text = UiLoc.T("ui.interview.inspire_hint", "点击填入输入框，不会直接发送");
            interviewInspireHintText.enableWordWrapping = true;
            interviewInspireHintText.overflowMode = TextOverflowModes.Truncate;
            interviewInspireHintText.raycastTarget = false;

            interviewHintRoot = new GameObject("Chips", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            interviewHintRoot.SetParent(paper.transform, false);
            Stretch(interviewHintRoot.GetComponent<RectTransform>(),
                new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.83f),
                Vector2.zero, Vector2.zero);
            var vlg = interviewHintRoot.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.padding = new RectOffset(2, 2, 2, 2);

            interviewCoachTipText = CreateUiText(paper.transform, "CoachTip", 12, TextAnchor.UpperLeft,
                IvInkMuted, Vector2.zero, Vector2.zero);
            Stretch(interviewCoachTipText.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.15f),
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
                new Vector2(0f, 0f), new Vector2(1f, 0.38f),
                Vector2.zero, Vector2.zero);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, -4f);
            shadow.raycastTarget = false;

            var paper = CreatePaperFace(host.transform, "Paper");
            // No paperclip — End + tools need the full pad.

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
            var clipSpr = VnArt.GetTitle("Shared/deco_paperclip");
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
            var tapeSpr = VnArt.GetTitle("Shared/btn_tape_idle");
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
                interviewTitleSubText.gameObject.SetActive(false);
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

            var go = new GameObject("Tools", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            go.transform.SetParent(interviewActionRoot, false);
            var le = go.GetComponent<LayoutElement>();
            // Fills the rest of the sheet so the strips can sit low, under the END INTERVIEW tape.
            le.flexibleWidth = 1f;
            le.minHeight = 226f;
            le.preferredHeight = 232f;
            le.flexibleHeight = 1f;
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 5f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            interviewToolsRow = go.transform;
            return interviewToolsRow;
        }

        Transform EnsureEndConfirmHost()
        {
            if (interviewActionRoot == null) return null;
            var go = new GameObject("EndConfirm", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(interviewActionRoot, false);
            var rt = go.GetComponent<RectTransform>();
            // The orange END INTERVIEW tape is baked across the top of this sheet.
            rt.anchorMin = new Vector2(0.08f, 0.06f);
            rt.anchorMax = new Vector2(0.92f, 0.58f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(6, 6, 4, 4);
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            interviewSpawned.Add(go);
            return go.transform;
        }

        void AddInterviewAction(string label, UnityEngine.Events.UnityAction action, bool primary = false,
            bool fullWidth = false, Transform host = null)
        {
            if (interviewActionRoot == null) return;

            bool useFull = primary || fullWidth;
            Transform parent = host != null
                ? host
                : (useFull ? interviewActionRoot : EnsureInterviewToolsRow());
            if (parent == null) return;

            // Plain Loc text only — no emoji/symbol Icon layer (missing glyphs → □ spam).
            var go = new GameObject(primary ? "ActEnd" : "Act", typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var face = go.GetComponent<Image>();
            face.color = primary
                ? new Color(0.68f, 0.30f, 0.18f, 1f)
                : new Color(0.97f, 0.94f, 0.88f, 1f);
            if (host != null)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.22f, 0.12f, 0.06f, 0.22f);
                shadow.effectDistance = new Vector2(0f, -2f);
            }
            go.GetComponent<Image>().raycastTarget = true;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                action();
            });

            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            if (host != null)
            {
                le.minHeight = 52f;
                le.preferredHeight = 56f;
            }
            else if (primary)
            {
                le.minHeight = 46f;
                le.preferredHeight = 50f;
            }
            else if (useFull)
            {
                le.minHeight = 38f;
                le.preferredHeight = 42f;
            }
            else
            {
                // Same box for Review / Notes / Menu. 230×72 matches the paper-strip art.
                le.minWidth = 230f;
                le.preferredWidth = 230f;
                le.flexibleWidth = 0f;
                le.minHeight = 72f;
                le.preferredHeight = 72f;
                le.flexibleHeight = 0f;
            }

            var labelTx = CreateUiText(go.transform, "L", primary ? 16 : 15, TextAnchor.MiddleCenter,
                primary ? new Color(0.99f, 0.96f, 0.92f, 1f) : new Color(0.30f, 0.18f, 0.12f, 1f),
                Vector2.zero, Vector2.zero);
            Stretch(labelTx.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                Vector2.zero, Vector2.zero);
            labelTx.text = label;
            labelTx.fontStyle = FontStyles.Bold;
            labelTx.enableWordWrapping = false;
            labelTx.overflowMode = TextOverflowModes.Overflow;
            labelTx.raycastTarget = false;
            ApplyLetterSpacing(labelTx, 0f);

            ApplyInterviewActionArt(go.GetComponent<Button>(), label, labelTx);

            interviewSpawned.Add(go);
        }

        static void PlaceInterviewTool(RectTransform rt, float x, float y, float w, float h)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(x, 1f - (y + h));
            rt.anchorMax = new Vector2(x + w, 1f - y);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
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
            if (LlmClient.Instance != null)
                LlmClient.Instance.ReloadApiKey();
            bool llmReady = LlmClient.Instance != null
                && LlmClient.Instance.IsConfigured
                && ic.Subject != InterviewSubject.None;

            var reply = ic.Ask(q, deferSpeakerLines: llmReady);

            bool skipLlm = reply != null && InterviewRuleEngine.IsLlmUnsafeIntent(reply.intent);

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
                DialogueHistory.Instance.Add("", InterviewLoc.FormatBehavior(reply.behavior), "interview");
            if (lines == null) return;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var shown = InterviewLoc.LocalizeReplyLine(line.Trim());
                DialogueHistory.Instance.Add(who, shown, "interview");
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
                    facts = StreetCat.Interview.InterviewLoc.FactsBlockLabel(reply.behavior, facts);
            }
            else if (reply.cognitiveBoundary)
            {
                facts = StreetCat.Interview.InterviewLoc.CognitiveBoundaryFacts;
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
            if (!HasSpokenLines(aiLines))
                aiLines = null;
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

            if (HasSpokenLines(aiLines))
                aiLines = ic.EnforceDafuFoodQuota(aiLines, reply, question);
            else
            {
                aiLines = null;
                if (reply.scriptLines != null && reply.scriptLines.Count > 0
                    && LooksLikeStaleRepeatRule(ruleLines))
                    ruleLines = new List<string>(reply.scriptLines);
                ruleLines = ic.EnforceDafuFoodQuota(ruleLines, reply, question);
            }

            FinishInterviewReply(ic, reply, who, ruleLines, aiLines);
        }

        static bool HasSpokenLines(IList<string> lines)
        {
            if (lines == null) return false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    return true;
            }
            return false;
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
            var lines = HasSpokenLines(aiLines) ? aiLines : ruleLines;
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
            interviewAvatarCrops.Clear();
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
            if (IsMaterialNotice(line)) return;

            if (line.StartsWith("小凌：") || line.StartsWith("小凌:")
                || line.StartsWith("Ling: ") || line.StartsWith("Ling:"))
            {
                var body = StripSpeakerPrefix(line);
                SpawnInterviewBubble(body, BubbleKind.Player, "小凌");
            }
            else if (line.StartsWith("大福：") || line.StartsWith("大福:")
                     || line.StartsWith("林女士：") || line.StartsWith("林女士:")
                     || line.StartsWith("Dafu: ") || line.StartsWith("Dafu:")
                     || line.StartsWith("Ms. Lin: ") || line.StartsWith("Ms. Lin:"))
            {
                var body = StripSpeakerPrefix(line);
                var who = InterviewController.Instance != null
                          && InterviewController.Instance.Subject == InterviewSubject.Lin
                    ? "林女士"
                    : "大福";
                SpawnInterviewBubble(body, BubbleKind.Npc, who);
            }
            else if (InterviewLoc.TryUnwrapActionLine(line, out _))
            {
                SpawnInterviewBubble(InterviewLoc.FormatActionLogLine(line), BubbleKind.System, null);
            }
            else
            {
                SpawnInterviewBubble(line, BubbleKind.System, null);
            }
        }

        static bool IsMaterialNotice(string line)
        {
            if (string.IsNullOrEmpty(line)) return false;
            return line.Contains("【素材】")
                   || line.Contains("[Materials]")
                   || line.Contains("[Material]");
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

            float contentW = EstimateInterviewChatContentWidth();
            float fontPx = (isSystem ? 16f : 22f) * scale;
            float measured = body.Length * fontPx * 0.55f;
            float maxFrac = kind == BubbleKind.Npc ? 0.90f : 0.96f;
            float bubbleW = isSystem
                ? Mathf.Clamp(measured, 280f, Mathf.Max(280f, contentW * 0.86f))
                : Mathf.Max(480f, contentW * maxFrac);

            var row = new GameObject("Bubble", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            row.transform.SetParent(interviewLogContent, false);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 0f;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            int leftPad = kind == BubbleKind.Npc ? 72 : 4;
            hlg.padding = new RectOffset(leftPad, 8, 10, 4);
            var rowLe = row.GetComponent<LayoutElement>();
            rowLe.minHeight = isSystem ? 32f : 36f;
            rowLe.flexibleWidth = 1f;

            var bubbleGo = new GameObject("Face", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            bubbleGo.transform.SetParent(row.transform, false);
            var bubble = bubbleGo.GetComponent<Image>();
            Sprite plate = null;
            if (kind == BubbleKind.Player)
                plate = FreeInterviewArt.DialogueCream;
            else if (kind == BubbleKind.Npc)
                plate = FreeInterviewArt.DialoguePeach;
            if (plate != null)
            {
                bubble.sprite = plate;
                bubble.type = Image.Type.Simple;
                bubble.preserveAspect = false;
                bubble.color = Color.white;
            }
            else if (kind == BubbleKind.Player)
                bubble.color = IvBubblePlayer;
            else if (kind == BubbleKind.Npc)
                bubble.color = IvBubbleNpc;
            else if (isMaterial)
                bubble.color = IvBubbleMaterial;
            else
                bubble.color = IvBubbleSystem;
            bubble.raycastTarget = false;

            var ble = bubbleGo.GetComponent<LayoutElement>();
            ble.minWidth = 320f;
            ble.preferredWidth = bubbleW;
            ble.flexibleWidth = 0f;
            float innerW = Mathf.Max(80f, bubbleW - 72f);
            int lines = Mathf.Max(1, Mathf.CeilToInt(measured / innerW));
            float stripAspect = plate != null && plate.rect.height > 1f
                ? plate.rect.width / plate.rect.height
                : 7.5f;
            float bubbleH = isSystem
                ? lines * fontPx * 1.15f + 8f
                : Mathf.Max(bubbleW / stripAspect, lines * fontPx * 1.15f + 36f);
            ble.minHeight = bubbleH;
            ble.preferredHeight = bubbleH;
            ble.layoutPriority = 2;

            var bVlg = bubbleGo.GetComponent<VerticalLayoutGroup>();
            bVlg.padding = new RectOffset(32, 32, 28, 16);
            bVlg.childAlignment = TextAnchor.UpperLeft;
            bVlg.childControlWidth = true;
            bVlg.childControlHeight = true;
            bVlg.childForceExpandWidth = true;
            bVlg.childForceExpandHeight = false;
            var fitter = bubbleGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            if (!isSystem && !string.IsNullOrEmpty(avatarWho))
                AttachSpeakerTab(bubbleGo.transform, avatarWho, scale);

            var tgo = new GameObject("T", typeof(RectTransform), typeof(LayoutElement));
            tgo.transform.SetParent(bubbleGo.transform, false);
            var tx = tgo.AddComponent<TextMeshProUGUI>();
            tx.font = font;
            tx.fontSize = Mathf.RoundToInt(fontPx);
            VnText.ApplyFontWeight(tx, GameSettings.FontWeight);
            tx.color = isSystem ? IvInkMuted : FreeInterviewArt.Ink;
            tx.alignment = VnText.ToAlignment(isSystem ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
            tx.text = body;
            tx.fontStyle = isAction ? FontStyles.Italic : FontStyles.Normal;
            tx.enableWordWrapping = true;
            tx.overflowMode = TextOverflowModes.Overflow;
            tx.raycastTarget = false;
            ApplyLetterSpacing(tx, 0f);

            var tLe = tgo.GetComponent<LayoutElement>();
            tLe.minWidth = 80f;
            tLe.preferredWidth = innerW;
            tLe.flexibleWidth = 1f;

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(row.transform, false);
            var sle = spacer.GetComponent<LayoutElement>();
            sle.flexibleWidth = 1f;
            sle.minWidth = 0f;

            interviewBubbleSpawned.Add(row);
        }

        void AttachSpeakerTab(Transform bubble, string who, float scale)
        {
            var tabGo = new GameObject("NameTab", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            tabGo.transform.SetParent(bubble, false);
            var tab = tabGo.GetComponent<Image>();
            var tabSprite = FreeInterviewArt.NameTab;
            if (tabSprite != null)
            {
                tab.sprite = tabSprite;
                tab.type = Image.Type.Simple;
                tab.preserveAspect = true;
                tab.color = Color.white;
            }
            else
            {
                tab.color = IvNamePlate;
            }
            tab.raycastTarget = false;
            var le = tabGo.GetComponent<LayoutElement>();
            le.ignoreLayout = true;

            var rt = tabGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(22f, 10f);
            rt.sizeDelta = new Vector2(108f, 34f);

            var label = CreateUiText(tabGo.transform, "Name", Mathf.RoundToInt(15f * scale),
                TextAnchor.MiddleCenter, FreeInterviewArt.Ink, Vector2.zero, Vector2.zero);
            VnText.ApplyFontWeight(label, GameSettings.FontWeight);
            StretchFull(label.rectTransform);
            label.text = ScriptLoc.MapSpeaker(who);
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
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
            le.layoutPriority = 2;
            var hrt = host.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(0f, 1f);
            hrt.pivot = new Vector2(0f, 1f);
            hrt.sizeDelta = new Vector2(size, size);

            var face = CreateImage(host.transform, "Face", Color.white);
            var frt = face.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            face.preserveAspect = true;
            face.raycastTarget = false;

            // Fixed default portrait. Expression swaps move the head and the circle clips the chin.
            var spr = VnArt.GetPortrait(FixedInterviewAvatarKey(who));

            if (spr != null)
            {
                face.sprite = GetInterviewAvatarCrop(spr, who);
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

        static string FixedInterviewAvatarKey(string who)
        {
            if (!string.IsNullOrEmpty(who) && (who.Contains("小凌") || who.Contains("Ling")))
                return "ch_xiaoling_default";
            if (!string.IsNullOrEmpty(who)
                && (who.Contains("林女士") || who.Contains("Ms. Lin") || who.Contains("Ms Lin") || who == "Lin"))
                return "ch_lin_default";
            return "ch_dafu_default";
        }

        /// <summary>
        /// Head window in sprite space (x, y from the bottom, width, height), 0–1.
        /// Loose on purpose: a tight crop puts the chin on the circle edge and clips it.
        /// </summary>
        static Rect InterviewAvatarCrop01(string who, string spriteName)
        {
            var n = spriteName ?? "";
            if (n.Contains("xiaoling")
                || (!string.IsNullOrEmpty(who) && (who.Contains("小凌") || who.Contains("Ling"))))
                return new Rect(0.16f, 0.40f, 0.57f, 0.38f);
            if (n.Contains("ch_lin")
                || (!string.IsNullOrEmpty(who)
                    && (who.Contains("林女士") || who.Contains("Ms. Lin") || who.Contains("Ms Lin") || who == "Lin")))
                return new Rect(0.12f, 0.50f, 0.57f, 0.38f);
            return new Rect(0.20f, 0.38f, 0.60f, 0.40f);
        }

        Sprite GetInterviewAvatarCrop(Sprite source, string who)
        {
            if (source == null || source.texture == null) return source;
            int id = source.GetInstanceID();
            if (interviewAvatarCrops.TryGetValue(id, out var cached) && cached != null)
                return cached;

            var norm = InterviewAvatarCrop01(who, source.name);
            var texRect = source.textureRect;
            var raw = new Rect(
                texRect.x + norm.x * texRect.width,
                texRect.y + norm.y * texRect.height,
                norm.width * texRect.width,
                norm.height * texRect.height);
            var tex = source.texture;
            float x = Mathf.Clamp(raw.x, 0f, tex.width - 1f);
            float y = Mathf.Clamp(raw.y, 0f, tex.height - 1f);
            float w = Mathf.Clamp(raw.width, 1f, tex.width - x);
            float h = Mathf.Clamp(raw.height, 1f, tex.height - y);
            var crop = Sprite.Create(tex, new Rect(x, y, w, h), new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            crop.name = source.name + "_avatar";
            interviewAvatarCrops[id] = crop;
            return crop;
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
            if (fill.type == Image.Type.Filled)
            {
                fill.fillAmount = t;
                return;
            }
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            portraitDebugInterviewRevision++;
            var lineKey = GetPortraitDebugLineKey();
            var debugKey = PortraitDebugOverrides.ResolveForLine(lineKey);
            if (!string.IsNullOrEmpty(debugKey))
            {
                SetInterviewLeftPortrait(debugKey);
                return;
            }
#endif

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

            // Expression portraits, not the fixed profile headshot. The polaroid
            // used to swap with the interview mood; the profile photo froze it.
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
            {
                GameState.Instance.Data.uiMode = subject == InterviewSubject.Lin ? "interview_lin" : "interview_dafu";
                GameState.Instance.Data.reinterviewReturnToWriting = returnToWritingAfter;
            }
            SetAdvanceEnabled(false);
            SaveSystem.Autosave();
            InterviewController.Instance.Begin(subject, returnToWritingAfter);

            SetProp(null);
            SetChrome(false, false, false);
            SetInterviewChrome(true);
            // TopBar chapter/objective row removed — keep both off while interviewing.
            if (chapterChip != null)
                chapterChip.gameObject.SetActive(false);
            if (objectiveText != null)
                objectiveText.gameObject.SetActive(false);
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
            // The missing-key note is a developer hint. The reference sheet has no banner
            // across the blank page, so it stays off the paper.
        }

        void HideInterviewBanner()
        {
            if (interviewBannerText != null)
            {
                interviewBannerText.text = "";
                interviewBannerText.gameObject.SetActive(false);
            }
            if (interviewBannerCard != null)
                interviewBannerCard.gameObject.SetActive(false);
            SetInterviewLogBottom(0.135f);
        }

        void ShowInterviewBanner(string msg)
        {
            if (interviewBannerText == null) return;
            interviewBannerText.gameObject.SetActive(true);
            interviewBannerText.text = msg;
            interviewBannerText.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
            interviewBannerText.color = new Color(0.33f, 0.20f, 0.14f, 1f);
            interviewBannerText.lineSpacing = 6f;
            ApplyLetterSpacing(interviewBannerText, 0f);
            if (interviewBannerCard != null)
                interviewBannerCard.gameObject.SetActive(true);
            SetInterviewLogBottom(0.50f);
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

            // End is the orange tape baked into the interview background (EndInterviewHit).
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

            // Full question — wrap inside widened right column (no mid-word hard truncate).
            var go = new GameObject("Preset", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(interviewHintRoot, false);
            var img = go.GetComponent<Image>();
            var card = FreeInterviewArt.QuestionCard;
            if (card != null)
            {
                img.sprite = card;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.color = new Color(0.98f, 0.95f, 0.90f, 0.98f);
            }
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            var pressed = new Color(0.94f, 0.88f, 0.78f, 1f);
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.98f, 0.94f, 1f);
            colors.pressedColor = pressed;
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 112f;
            le.preferredHeight = 118f;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;

            string fill = question;
            btn.onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                FillInterviewInput(fill);
            });

            var tgo = new GameObject("L", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(18f, 14f), new Vector2(-16f, -12f));
            var tx = tgo.AddComponent<TextMeshProUGUI>();
            tx.font = font;
            tx.fontSize = Mathf.RoundToInt(20f * GameSettings.FontSizeScale);
            VnText.ApplyFontWeight(tx, GameSettings.FontWeight);
            tx.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
            tx.color = FreeInterviewArt.Ink;
            // The blank question card has no number disc. The index stays unused.
            tx.text = question;
            _ = colorIdx;
            tx.raycastTarget = false;
            tx.enableWordWrapping = true;
            tx.overflowMode = TextOverflowModes.Truncate;
            tx.maxVisibleLines = 3;
            tx.lineSpacing = 0f;
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
                var confirmHost = EnsureEndConfirmHost();
                AddInterviewAction(UiLoc.T("ui.interview.continue", "继续采访"), () =>
                {
                    HideInterviewBanner();
                    RefreshInterviewView();
                }, fullWidth: true, host: confirmHost);
                AddInterviewAction(UiLoc.T("ui.interview.confirm_end", "确认结束"), () =>
                {
                    HideInterviewBanner();
                    SetInterviewChrome(false);
                    InterviewController.Instance.End(true);
                }, primary: true, host: confirmHost);
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
                interviewTitleSubText.gameObject.SetActive(false);
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
                interviewInspireHintText.color = new Color(0.22f, 0.18f, 0.14f, 0.90f);
            }
            if (interviewBannerText != null)
            {
                interviewBannerText.font = font;
                interviewBannerText.fontSize = Mathf.RoundToInt(18f * scale);
                interviewBannerText.color = new Color(0.33f, 0.20f, 0.14f, 1f);
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
                interviewCoachTipText.color = new Color(0.22f, 0.18f, 0.14f, 0.92f);
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
                interviewInput.pointSize = Mathf.RoundToInt(22f * scale);
                if (interviewInput.textComponent != null)
                {
                    interviewInput.textComponent.font = font;
                    interviewInput.textComponent.fontSize = Mathf.RoundToInt(22f * scale);
                    interviewInput.textComponent.color = IvInk;
                    VnText.ApplyFontWeight(interviewInput.textComponent, GameSettings.FontWeight);
                    ApplyLetterSpacing(interviewInput.textComponent, 0f);
                }
                if (interviewInput.placeholder is TextMeshProUGUI ph)
                {
                    ph.font = font;
                    ph.fontSize = Mathf.RoundToInt(20f * scale);
                    ph.color = new Color(0.28f, 0.24f, 0.20f, 0.78f);
                    ApplyLetterSpacing(ph, 0f);
                }
            }
        }

        void ApplyMeterLabelFont(TextMeshProUGUI tx, float scale)
        {
            if (tx == null) return;
            tx.font = font;
            tx.fontSize = Mathf.RoundToInt(20f * scale);
            tx.color = new Color(0.28f, 0.19f, 0.13f, 1f);
            tx.extraPadding = true;
            tx.fontStyle = FontStyles.Normal;
            VnText.ApplyFontWeight(tx, GameSettings.FontWeight);
            ApplyLetterSpacing(tx, 0f);
            tx.enableWordWrapping = false;
            tx.overflowMode = TextOverflowModes.Overflow;
            tx.alignment = VnText.ToAlignment(TextAnchor.LowerLeft);
        }

        void ApplyMeterValueFont(TextMeshProUGUI tx, float scale)
        {
            if (tx == null) return;
            tx.font = font;
            tx.fontSize = Mathf.RoundToInt(20f * scale);
            tx.extraPadding = true;
            tx.fontStyle = FontStyles.Normal;
            tx.color = new Color(0.22f, 0.16f, 0.12f, 1f);
            VnText.ApplyFontWeight(tx, GameSettings.FontWeight);
            tx.alignment = VnText.ToAlignment(TextAnchor.MiddleRight);
            tx.enableWordWrapping = false;
            tx.overflowMode = TextOverflowModes.Overflow;
        }
    }
}
