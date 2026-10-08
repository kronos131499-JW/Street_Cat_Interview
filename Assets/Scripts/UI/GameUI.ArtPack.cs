using System.Collections.Generic;
using StreetCat.Data;
using StreetCat.Interview;
using StreetCat.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Applies exported 拆拆拆 UI pieces onto the existing runtime chrome.
    /// Components only — never full-screen mockups (AAA效果图 / 效果图) and never magenta placeholders.
    /// </summary>
    public partial class GameUI
    {
        const string ArtPackRoot = "VnArt/UI/ArtPack/";
        static readonly Dictionary<string, Sprite> ArtPackCache = new Dictionary<string, Sprite>();

        // Deep brown #2A1202. Every parchment dialogue role uses this same ink.
        static readonly Color DialogueInk = new Color(42f / 255f, 18f / 255f, 2f / 255f, 1f);
        static readonly Color ArtPackInk = DialogueInk;
        static readonly Color ArtPackInkMuted = DialogueInk;
        static readonly Color ArtPackInkInner = DialogueInk;
        static readonly Color ArtPackInkSystem = DialogueInk;

        // The paper window inside 自由采访/照片框, measured off the sprite (376x530):
        // x 54..336, y 65..372 top-down. Anchors are relative to the frame's own rect,
        // so they hold at any window aspect as long as the art itself doesn't change.
        static readonly Vector2 InterviewPhotoWindowMin = new Vector2(0.1436f, 0.2962f);
        static readonly Vector2 InterviewPhotoWindowMax = new Vector2(0.8963f, 0.8774f);

        // Brown window inside FreeInterview/01_photo_background_lin (1086x1448),
        // same outer-edge convention as above.
        static readonly Vector2 LinPhotoWindowMin = new Vector2(0.110f, 0.345f);
        static readonly Vector2 LinPhotoWindowMax = new Vector2(0.912f, 0.880f);

        bool artPackParchmentActive;
        bool artPackNotebookTabActive;

        static Sprite LoadArtPackSprite(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            if (ArtPackCache.TryGetValue(relativePath, out var cached)) return cached;

            var resourcePath = ArtPackRoot + relativePath;
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }
            if (relativePath.StartsWith("手机界面帖子"))
                SharpenSocialMockup(sprite);
            ArtPackCache[relativePath] = sprite;
            return sprite;
        }

        /// <summary>
        /// Phone mockups are ~1700px tall and drawn much smaller. A negative mip bias
        /// keeps the cat photos and UI type from turning to mush when minified.
        /// </summary>
        static void SharpenSocialMockup(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            var tex = sprite.texture;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 1;
            if (tex.mipmapCount > 1)
                tex.mipMapBias = -0.75f;
        }

        bool ApplyArtPackImage(Image target, string relativePath, bool preserveAspect = false)
        {
            if (target == null) return false;
            var sprite = LoadArtPackSprite(relativePath);
            if (sprite == null) return false;

            target.sprite = sprite;
            target.type = Image.Type.Simple;
            target.preserveAspect = preserveAspect;
            target.color = Color.white;
            return true;
        }

        bool ApplyArtPackButton(Button button, string idlePath, string hoverPath, bool preserveAspect = true)
        {
            if (button == null) return false;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null) return false;

            var idle = LoadArtPackSprite(idlePath);
            if (idle == null) return false;
            var hover = LoadArtPackSprite(hoverPath) ?? idle;

            image.sprite = idle;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = hover,
                pressedSprite = hover,
                selectedSprite = hover,
                disabledSprite = idle
            };
            return true;
        }

        static Image FindImage(GameObject root, string path)
        {
            if (root == null) return null;
            var child = root.transform.Find(path);
            return child != null ? child.GetComponent<Image>() : null;
        }

        static void HideChild(Transform parent, string name)
        {
            if (parent == null) return;
            var child = parent.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }

        void ApplyArtPackFixedSkin()
        {
            ApplyDialogueParchment();
            ApplyBacklogSkin();
            ApplyInterviewComponentSkin();
            ApplyNotebookSkin();
            ApplyWritingBoardSkin();
        }

        void ApplyDialogueParchment()
        {
            if (!ApplyArtPackImage(dialoguePanel, "对话界面/底框"))
                return;

            artPackParchmentActive = true;
            HideChild(dialoguePanel.transform, "Edge");

            // Shorter band ≈ parchment banner aspect; avoids a tall empty box with text hugging the top.
            Stretch(dialoguePanel.rectTransform,
                new Vector2(0.04f, VnTheme.LetterboxH + 0.014f),
                new Vector2(0.96f, 0.235f),
                Vector2.zero, Vector2.zero);
            var dlgCanvas = dialoguePanel.canvas;
            if (dlgCanvas != null)
                UILayoutOverrides.TryApply(
                    dlgCanvas.rootCanvas != null ? dlgCanvas.rootCanvas : dlgCanvas,
                    dialoguePanel.rectTransform);

            if (namePlate != null)
            {
                HideChild(namePlate.transform, "NameAccent");
                var nprt = namePlate.rectTransform;
                if (ApplyArtPackImage(namePlate, "对话界面/character_name", true))
                {
                    nprt.anchoredPosition = new Vector2(36f, -6f);
                    nprt.sizeDelta = new Vector2(360f, 120f);
                    if (nameText != null)
                    {
                        // Centered box so 通用 UI 布局编辑器 can drag the name.
                        // A saved override is applied immediately and wins next play.
                        var nrt = nameText.rectTransform;
                        nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0.5f);
                        nrt.pivot = new Vector2(0.5f, 0.5f);
                        nrt.sizeDelta = new Vector2(280f, 52f);
                        nrt.anchoredPosition = new Vector2(0f, -8f);
                        var canvas = nrt.GetComponentInParent<Canvas>();
                        if (canvas != null)
                            UILayoutOverrides.TryApply(canvas.rootCanvas != null ? canvas.rootCanvas : canvas, nrt);
                    }
                }
                else
                {
                    namePlate.color = new Color(0.96f, 0.92f, 0.84f, 0.98f);
                    nprt.anchoredPosition = new Vector2(64f, 6f);
                    nprt.sizeDelta = new Vector2(180f, 34f);
                }
            }

            // Keep glyphs on the solid parchment, clear of the torn edge and the notebook tab
            // (the tab override sits ~50px inside the panel's right edge).
            var bodyHost = dialoguePanel.transform.Find("BodyHost") as RectTransform;
            if (bodyHost != null)
                Stretch(bodyHost, Vector2.zero, Vector2.one, new Vector2(84f, 28f), new Vector2(-120f, -46f));

            if (buttonRoot != null)
            {
                var br = buttonRoot.GetComponent<RectTransform>();
                // Keep End talk / Skip clear of the protruding notebook tab.
                br.anchoredPosition = new Vector2(-36f, 10f);
            }
            if (statusText != null)
                statusText.rectTransform.anchoredPosition = new Vector2(76f, 12f);
            if (clickHintText != null)
                clickHintText.rectTransform.anchoredPosition = new Vector2(-136f, 12f);

            if (bodyText != null)
            {
                // Overflow (not Truncate): the viewport scrolls. Truncate was keeping
                // one nearly full-width line and dropping only the leftover word.
                bodyText.overflowMode = TextOverflowModes.Overflow;
                bodyText.lineSpacing = 16f;
                bodyText.extraPadding = true;
                bodyText.margin = new Vector4(4f, 2f, 8f, 2f);
                bodyText.enableWordWrapping = true;
                SharpenDialogueTmp(bodyText);
            }
            if (nameText != null)
                SharpenDialogueTmp(nameText);
            if (statusText != null)
                SharpenDialogueTmp(statusText);
            if (clickHintText != null)
                SharpenDialogueTmp(clickHintText);

            ApplyDialogueNotebookTab();
            RefreshDialogueFontColors();
        }

        static void SharpenDialogueTmp(TextMeshProUGUI t, FontStyles style = FontStyles.Normal)
        {
            if (t == null) return;
            t.enableAutoSizing = false;
            // Goes through ApplyFontWeight rather than ApplyCrisp alone: this runs after
            // ApplyActiveFonts has set weights, and clearing dilate here used to leave the
            // dialogue as the only text on screen stuck at the thinnest cut.
            VnText.ApplyFontWeight(t, GameSettings.FontWeight);
            t.fontStyle |= style;
        }

        void ApplyDialogueNotebookTab()
        {
            if (dialoguePanel == null) return;
            var spr = LoadArtPackSprite("对话界面/记者笔记");
            if (spr == null) return;

            var existing = dialoguePanel.transform.Find("ArtNotebookTab");
            GameObject go;
            if (existing != null)
                go = existing.gameObject;
            else
            {
                go = new GameObject("ArtNotebookTab", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(dialoguePanel.transform, false);
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SfxController.Instance?.PlayUi();
                    OpenNotebook();
                });
            }

            var rt = go.GetComponent<RectTransform>();
            // Sit mostly outside the parchment so footer actions stay clear.
            // Reapplied every call so Play Mode picks up size/position tweaks.
            rt.anchorMin = new Vector2(1f, 0.06f);
            rt.anchorMax = new Vector2(1f, 0.94f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(10f, 0f);
            rt.sizeDelta = new Vector2(148f, 0f);

            var img = go.GetComponent<Image>();
            img.sprite = spr;
            img.preserveAspect = true;
            img.color = Color.white;
            artPackNotebookTabActive = true;
        }

        void ApplyBacklogSkin()
        {
            var panel = FindImage(backlogRoot, "Panel");
            if (!ApplyArtPackImage(panel, "对话回看/底板")) return;
            HideChild(panel != null ? panel.transform : null, "Edge");
            // Wood-grain panel needs light copy; ArtPackInk is near-black and unreadable.
            var white = Color.white;
            if (backlogText != null) backlogText.color = white;
            if (backlogTitleText != null) backlogTitleText.color = white;
            if (backlogCloseLabel != null) backlogCloseLabel.color = white;
        }

        void ApplyInterviewComponentSkin()
        {
            if (interviewRoot == null) return;
            var desk = FindImage(interviewRoot, "ArtDesk");
            if (desk == null)
            {
                var go = new GameObject("ArtDesk", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(interviewRoot.transform, false);
                go.transform.SetAsFirstSibling();
                desk = go.GetComponent<Image>();
                StretchFull(desk.rectTransform);
            }
            if (!ApplyArtPackImage(desk, "自由采访/背景", false))
                return;
            desk.raycastTarget = false;
            desk.rectTransform.SetAsFirstSibling();

            var catcher = FindImage(interviewRoot, "HitCatcher");
            if (catcher != null)
            {
                StretchFull(catcher.rectTransform);
                catcher.color = new Color(0f, 0f, 0f, 0.001f);
            }

            var left = interviewRoot.transform.Find("LeftColumn") as RectTransform;
            var center = interviewRoot.transform.Find("CenterColumn") as RectTransform;
            var right = interviewRoot.transform.Find("RightColumn") as RectTransform;
            // Fractions of the 1675×939 reference, which the desk plate is stretched to.
            if (left != null)
                Stretch(left, new Vector2(0.022f, 0.04f), new Vector2(0.198f, 0.97f), Vector2.zero, Vector2.zero);
            if (center != null)
                Stretch(center, new Vector2(0.230f, 0.045f), new Vector2(0.815f, 0.96f), Vector2.zero, Vector2.zero);
            if (right != null)
                Stretch(right, new Vector2(0.828f, 0.03f), new Vector2(0.988f, 0.97f), Vector2.zero, Vector2.zero);

            var status = interviewRoot.transform.Find("LeftColumn/StatusPad") as RectTransform;
            var portrait = interviewRoot.transform.Find("LeftColumn/PortraitPad") as RectTransform;
            // Inset from the tape and the card's right edge. 0.98 put the numbers off the paper.
            if (status != null)
                Stretch(status, new Vector2(0.06f, 0.56f), new Vector2(0.90f, 0.94f), Vector2.zero, Vector2.zero);
            if (portrait != null)
                Stretch(portrait, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.50f), Vector2.zero, Vector2.zero);

            var inspire = interviewRoot.transform.Find("RightColumn/InspirePad") as RectTransform;
            var tools = interviewRoot.transform.Find("RightColumn/ToolbarPad") as RectTransform;
            // ASK ABOUT panel is the painted top of the right strip; chips sit under that title.
            if (inspire != null)
                Stretch(inspire, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
            if (tools != null)
                Stretch(tools, new Vector2(0f, 0f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero);

            EnsureEndInterviewRibbonHit(right);

            ClearInterviewPaperFace("LeftColumn/StatusPad");
            ClearInterviewPaperFace("LeftColumn/PortraitPad");
            ClearInterviewPaperFace("CenterColumn");
            ClearInterviewPaperFace("RightColumn/InspirePad");
            ClearInterviewPaperFace("RightColumn/ToolbarPad");

            HidePath(interviewRoot.transform, "LeftColumn/StatusPad/Paper/Header");
            if (interviewTitleText != null)
                interviewTitleText.gameObject.SetActive(false);
            if (interviewInspireHeaderText != null)
                interviewInspireHeaderText.gameObject.SetActive(false);
            if (interviewInspireHintText != null)
                interviewInspireHintText.gameObject.SetActive(false);
            HidePath(interviewRoot.transform, "CenterColumn/MainPaper/Sep");
            HidePath(interviewRoot.transform, "CenterColumn/MainPaper/Paperclip");

            SkinInterviewMeter("Trust", "自由采访/信任 图标", "自由采访/信任 字", "自由采访/信任 框",
                new Color(0.95f, 0.62f, 0.18f, 1f));
            SkinInterviewMeter("Stress", "自由采访/压力 图标", "自由采访/压力 字", "自由采访/压力 框",
                new Color(0.78f, 0.22f, 0.18f, 1f));
            SkinInterviewMeter("Focus", "自由采访/专注 图标", "自由采访/专注 字", "自由采访/专注 框",
                new Color(0.20f, 0.55f, 0.72f, 1f));

            ApplyInterviewPhotoFrame();

            var inputBar = FindImage(interviewRoot, "CenterColumn/MainPaper/InputBar");
            if (inputBar != null && FreeInterviewArt.Input != null)
            {
                StampFreePlate(inputBar, FreeInterviewArt.Input, preserveAspect: false);
                inputBar.color = Color.white;
            }
            else if (inputBar != null && ApplyArtPackImage(inputBar, "自由采访/打字框", false))
            {
                inputBar.color = Color.white;
                inputBar.type = Image.Type.Simple;
            }

            if (ApplyArtPackImage(interviewSendBtnImage, "自由采访/发送键", true)
                && interviewSendLabel != null)
                interviewSendLabel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Dafu uses 自由采访/照片框; Ms. Lin has her own captioned polaroid.
        /// Runs from the interview skin pass, after InterviewController.Begin has set the subject.
        /// </summary>
        void ApplyInterviewPhotoFrame()
        {
            if (interviewRoot == null) return;
            var frameHost = interviewRoot.transform.Find("LeftColumn/PortraitPad");
            if (frameHost == null) return;

            var frame = FindImage(interviewRoot, "LeftColumn/PortraitPad/ArtFrame");
            if (frame == null)
            {
                frame = CreateImage(frameHost, "ArtFrame", Color.white);
                frame.raycastTarget = false;
            }

            var lin = InterviewController.Instance != null
                      && InterviewController.Instance.Subject == InterviewSubject.Lin;
            var linPlate = lin ? FreeInterviewArt.PhotoLin : null;
            bool applied;
            if (linPlate != null)
            {
                frame.sprite = linPlate;
                frame.type = Image.Type.Simple;
                frame.preserveAspect = false;
                frame.color = Color.white;
                applied = true;
            }
            else
            {
                applied = ApplyArtPackImage(frame, "自由采访/照片框", false);
            }

            if (applied)
            {
                StretchFull(frame.rectTransform);
                // A fitter (rather than preserveAspect) makes the rect match the drawn
                // polaroid, so the headshot can anchor to the paper window inside it.
                var card = frame.sprite.rect;
                var fitter = frame.GetComponent<AspectRatioFitter>();
                if (fitter == null)
                    fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = card.width / Mathf.Max(1f, card.height);

                if (interviewPortraitImage != null)
                {
                    interviewPortraitImage.transform.SetParent(frame.transform, false);
                    Stretch(interviewPortraitImage.rectTransform,
                        linPlate != null ? LinPhotoWindowMin : InterviewPhotoWindowMin,
                        linPlate != null ? LinPhotoWindowMax : InterviewPhotoWindowMax,
                        Vector2.zero, Vector2.zero);
                    interviewPortraitImage.transform.SetAsLastSibling();
                }
            }
            frame.transform.SetSiblingIndex(0);
            HidePath(interviewRoot.transform, "LeftColumn/PortraitPad/Paper/NamePlate");
        }

        void EnsureEndInterviewRibbonHit(RectTransform rightColumn)
        {
            if (rightColumn == null) return;
            var existing = rightColumn.Find("EndInterviewHit");
            GameObject go;
            if (existing != null)
                go = existing.gameObject;
            else
            {
                go = new GameObject("EndInterviewHit", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(rightColumn, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.001f);
                img.raycastTarget = true;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SfxController.Instance?.PlayUi();
                    TryEndInterview();
                });
            }
            go.transform.SetAsLastSibling();
            // Baked into 自由采访/背景 — orange "END INTERVIEW" tape above the tool chips.
            Stretch(go.GetComponent<RectTransform>(),
                new Vector2(-0.04f, 0.26f), new Vector2(0.98f, 0.37f),
                Vector2.zero, Vector2.zero);
        }

        void ClearInterviewPaperFace(string padPath)
        {
            var pad = interviewRoot.transform.Find(padPath);
            if (pad == null) return;
            HideChild(pad, "Shadow");
            var paper = pad.Find("Paper");
            if (paper == null && padPath == "CenterColumn")
                paper = pad.Find("MainPaper");
            if (paper == null) return;
            var img = paper.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0.001f);
            }
            HideChild(paper, "Tape");
            HideChild(paper, "Paperclip");
            // Tapes are created with the same name; hide every Tape child.
            for (int i = paper.childCount - 1; i >= 0; i--)
            {
                var child = paper.GetChild(i);
                if (child.name == "Tape" || child.name == "Paperclip")
                    child.gameObject.SetActive(false);
            }
        }

        void SkinInterviewMeter(string rowName, string iconPath, string wordPath, string framePath, Color fill)
        {
            var row = interviewRoot.transform.Find("LeftColumn/StatusPad/Paper/" + rowName);
            if (row == null) return;

            var label = row.Find("Label");
            if (label != null)
            {
                label.gameObject.SetActive(true);
                Stretch(label as RectTransform, new Vector2(0.18f, 0.52f), new Vector2(0.62f, 1f),
                    Vector2.zero, Vector2.zero);
                var tx = label.GetComponent<TextMeshProUGUI>();
                if (tx != null)
                {
                    tx.alignment = VnText.ToAlignment(TextAnchor.LowerLeft);
                    tx.color = new Color(0.28f, 0.19f, 0.13f, 1f);
                    tx.fontStyle = FontStyles.Normal;
                    tx.extraPadding = true;
                    VnText.ApplyFontWeight(tx, GameSettings.FontWeight);
                    ApplyLetterSpacing(tx, 0f);
                    tx.enableWordWrapping = false;
                    tx.overflowMode = TextOverflowModes.Overflow;
                }
            }
            var word = row.Find("ArtWord");
            if (word != null) word.gameObject.SetActive(false);

            var builtIcon = row.Find("Icon")?.GetComponent<Image>();
            if (builtIcon != null)
                builtIcon.gameObject.SetActive(false);
            var icon = FindOrCreateRowIcon(row, "ArtIcon");
            if (icon != null && ApplyArtPackImage(icon, iconPath, true))
            {
                Stretch(icon.rectTransform, new Vector2(0.00f, 0.28f), new Vector2(0.16f, 0.92f),
                    Vector2.zero, Vector2.zero);
                icon.preserveAspect = true;
                icon.color = Color.white;
            }

            var value = row.Find("Value") as RectTransform;
            if (value != null)
            {
                Stretch(value, new Vector2(0.68f, 0.50f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
                value.SetAsLastSibling();
                value.gameObject.SetActive(true);
                var valueTx = value.GetComponent<TextMeshProUGUI>();
                if (valueTx != null)
                {
                    valueTx.fontSize = 20f;
                    valueTx.alignment = VnText.ToAlignment(TextAnchor.MiddleRight);
                    valueTx.color = new Color(0.28f, 0.19f, 0.13f, 1f);
                    valueTx.enableWordWrapping = false;
                    valueTx.overflowMode = TextOverflowModes.Overflow;
                    VnText.ApplyFontWeight(valueTx, GameSettings.FontWeight);
                }
            }

            var track = row.Find("Track");
            if (track != null)
            {
                var trackImg = track.GetComponent<Image>();
                var empty = FreeInterviewArt.BarEmpty;
                if (trackImg != null)
                {
                    if (empty != null)
                    {
                        trackImg.sprite = empty;
                        trackImg.type = Image.Type.Simple;
                        trackImg.color = Color.white;
                    }
                    else
                    {
                        trackImg.sprite = null;
                        trackImg.type = Image.Type.Simple;
                        trackImg.color = new Color(0.86f, 0.80f, 0.72f, 1f);
                    }
                }
                var trt = track as RectTransform;
                if (trt != null)
                    Stretch(trt, new Vector2(0.18f, 0.06f), new Vector2(0.98f, 0.40f), Vector2.zero, Vector2.zero);
                var fillImg = track.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null)
                {
                    var crayon = FillSpriteForRow(rowName);
                    if (crayon != null)
                    {
                        fillImg.sprite = crayon;
                        fillImg.color = Color.white;
                        fillImg.type = Image.Type.Filled;
                        fillImg.fillMethod = Image.FillMethod.Horizontal;
                        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                    }
                    else
                    {
                        fillImg.sprite = null;
                        fillImg.type = Image.Type.Simple;
                        fillImg.color = fill;
                    }
                    var frt = fillImg.rectTransform;
                    frt.anchorMin = Vector2.zero;
                    frt.anchorMax = Vector2.one;
                    frt.offsetMin = new Vector2(3f, 3f);
                    frt.offsetMax = new Vector2(-3f, -3f);
                }
            }
        }

        static Sprite FillSpriteForRow(string rowName)
        {
            if (rowName == "Trust") return FreeInterviewArt.TrustFill;
            if (rowName == "Stress") return FreeInterviewArt.PressureFill;
            if (rowName == "Focus") return FreeInterviewArt.FocusFill;
            return null;
        }

        Image FindOrCreateRowIcon(Transform row, string name)
        {
            var existing = row.Find(name);
            if (existing != null) return existing.GetComponent<Image>();
            var img = CreateImage(row, name, Color.white);
            img.raycastTarget = false;
            return img;
        }

        static void HidePath(Transform root, string path)
        {
            if (root == null) return;
            var t = root.Find(path);
            if (t != null) t.gameObject.SetActive(false);
        }

        void ApplyNotebookSkin()
        {
            if (notebookRoot == null) return;
            var desk = FindImage(notebookRoot, "Desk");
            bool deskArt = ApplyArtPackImage(desk, "记者笔记/背景", false);
            if (deskArt)
                LayoutNotebookOnArtDesk(desk);

            if (notebookInspirePanel == null) return;
            // Card sprite already includes its torn edge; keep aspect so it doesn't squash.
            if (!ApplyArtPackImage(notebookInspirePanel, "记者笔记/问题灵感", true))
                return;
            notebookInspirePanel.type = Image.Type.Simple;
            notebookInspirePanel.preserveAspect = false;
            notebookInspirePanel.color = Color.white;
            // Fit the card to its own aspect so body copy stays on the sticker
            // when the window isn't the aspect the host rect was tuned for.
            var card = notebookInspirePanel.sprite.rect;
            var fitter = notebookInspirePanel.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = notebookInspirePanel.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = card.width / Mathf.Max(1f, card.height);

            var host = notebookInspirePanel.transform.parent;
            if (host != null)
            {
                host.SetAsLastSibling();
                if (deskArt)
                {
                    // Orange backplate and drop shadow fight the transparent card margin.
                    HideChild(host, "Shadow");
                    HideChild(host, "OpaqueBack");
                    Stretch(host.GetComponent<RectTransform>(),
                        new Vector2(0.685f, 0.03f), new Vector2(0.968f, 0.38f),
                        Vector2.zero, Vector2.zero);
                }
            }

            // Art already has paperclip + "QUESTION INSPIRATION" — hide duplicates.
            HideChild(notebookInspirePanel.transform, "Paperclip");
            HideChild(notebookInspirePanel.transform, "Bulb");
            if (notebookInspireHeaderText != null)
                notebookInspireHeaderText.gameObject.SetActive(false);

            // Baked "QUESTION INSPIRATION" ends ~45% down the sprite. Keep live
            // copy in the blank brown area under that stamp, above the torn foot.
            if (notebookInspireBodyText != null)
            {
                Stretch(notebookInspireBodyText.rectTransform, new Vector2(0.10f, 0.14f), new Vector2(0.88f, 0.46f),
                    Vector2.zero, Vector2.zero);
                notebookInspireBodyText.color = new Color(0.08f, 0.05f, 0.03f, 1f);
                notebookInspireBodyText.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
                notebookInspireBodyText.lineSpacing = 8f;
            }
        }

        void LayoutNotebookOnArtDesk(Image desk)
        {
            StretchFull(desk.rectTransform);

            HideChild(desk.transform, "PageShadow");
            var page = desk.transform.Find("NotebookPage");
            if (page != null)
            {
                HideChild(page, "Spiral");
                HideChild(page, "PageDoodle");
                var pageImg = page.GetComponent<Image>();
                if (pageImg != null)
                {
                    pageImg.sprite = null;
                    pageImg.color = new Color(1f, 1f, 1f, 0f);
                }
                // Text sits on the baked lined page, clear of the spiral rings.
                Stretch(page.GetComponent<RectTransform>(),
                    new Vector2(0.42f, 0.12f), new Vector2(0.93f, 0.90f),
                    Vector2.zero, Vector2.zero);
            }

            var left = desk.transform.Find("LeftColumn");
            if (left != null)
            {
                Stretch(left.GetComponent<RectTransform>(),
                    new Vector2(0.025f, 0.012f), new Vector2(0.385f, 0.985f),
                    Vector2.zero, Vector2.zero);
                var header = left.Find("Header");
                if (header != null)
                {
                    HideChild(header, "CatIcon");
                    Stretch(header.GetComponent<RectTransform>(),
                        new Vector2(0.02f, 0.90f), new Vector2(0.98f, 0.985f),
                        Vector2.zero, Vector2.zero);
                }
                var gridHost = left.Find("StickyGridHost");
                if (gridHost != null)
                {
                    Stretch(gridHost.GetComponent<RectTransform>(),
                        new Vector2(0f, 0.09f), new Vector2(1f, 0.88f),
                        Vector2.zero, Vector2.zero);
                    var content = gridHost.Find("Viewport/Content");
                    var grid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
                    if (grid != null)
                    {
                        grid.cellSize = new Vector2(236f, 210f);
                        grid.spacing = new Vector2(14f, 12f);
                    }
                }
                var modes = left.Find("ModeRow");
                if (modes != null)
                {
                    Stretch(modes.GetComponent<RectTransform>(),
                        new Vector2(0.04f, 0.004f), new Vector2(0.96f, 0.078f),
                        Vector2.zero, Vector2.zero);
                    for (int i = 0; i < modes.childCount; i++)
                    {
                        var child = modes.GetChild(i);
                        var img = child.GetComponent<Image>();
                        if (img != null) img.color = new Color(0.32f, 0.20f, 0.11f, 0.92f);
                        var tx = child.GetComponentInChildren<TextMeshProUGUI>();
                        if (tx != null) tx.color = new Color(0.96f, 0.93f, 0.86f, 1f);
                    }
                }
            }

            // Baked art already says REPORTER'S NOTES on the wood.
            if (notebookTitleText != null)
                notebookTitleText.gameObject.SetActive(false);

            var close = desk.transform.Find("Close");
            if (close != null)
            {
                var cimg = close.GetComponent<Image>();
                if (cimg != null) cimg.color = new Color(0.28f, 0.18f, 0.10f, 0.92f);
                if (notebookCloseLabel != null)
                    notebookCloseLabel.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            }
        }

        void ApplyWritingBoardSkin()
        {
            if (writingMatsRoot == null) return;

            StampMaterialPlate(FindImage(writingMatsRoot, "Wood"), MaterialCardArt.Wood, false);
            StampMaterialPlate(FindImage(writingMatsRoot, "Board"), MaterialCardArt.Board, false);
            var titlePlate = FindImage(writingMatsRoot, "TitlePlate");
            StampMaterialPlate(titlePlate, MaterialCardArt.ChapterTitle, false);
            if (titlePlate != null)
            {
                titlePlate.enabled = true;
                titlePlate.raycastTarget = false;
            }
            if (writingTapeTitle != null)
                writingTapeTitle.gameObject.SetActive(true);

            StampMaterialPlate(FindImage(writingMatsRoot, "Structure"), MaterialCardArt.Structure, false);
            var cardsBacking = FindImage(writingMatsRoot, "CardsBacking");
            if (cardsBacking != null) cardsBacking.gameObject.SetActive(false);
            StampMaterialPlate(FindImage(writingMatsRoot, "DetailPaper"), MaterialCardArt.Detail, false);
            StampMaterialPlate(FindImage(writingMatsRoot, "DetailPaper/Underline"), MaterialCardArt.Underline, false);
            StampMaterialPlate(FindImage(writingMatsRoot, "DetailPaper/Separator"), MaterialCardArt.Separator, false);
            StampMaterialPlate(FindImage(writingMatsRoot, "DetailPaper/IdTag"), MaterialCardArt.IdTag, true);
            var typeChip = FindImage(writingMatsRoot, "DetailPaper/Tag");
            if (typeChip != null && MaterialCardArt.DetailButton != null)
            {
                typeChip.sprite = MaterialCardArt.DetailButton;
                typeChip.color = Color.white;
                typeChip.type = Image.Type.Simple;
                typeChip.preserveAspect = true;
            }
            var idTag = FindImage(writingMatsRoot, "DetailPaper/IdTag");
            if (idTag != null) idTag.preserveAspect = true;

            SkinMaterialButton(writingPreviewBtn, MaterialCardArt.PreviewButton, MaterialCardArt.Cream);
            SkinMaterialButton(writingGoBtn, MaterialCardArt.WriteButton, MaterialCardArt.Cream);
            var back = writingMatsRoot.transform.Find("BackDirBtn");
            var notebook = writingMatsRoot.transform.Find("NotebookBtn");
            var reinterview = writingMatsRoot.transform.Find("ReInterviewBtn");
            if (back != null) SkinMaterialButton(back.GetComponent<Button>(), MaterialCardArt.NavBack, MaterialCardArt.Ink);
            if (notebook != null) SkinMaterialButton(notebook.GetComponent<Button>(), MaterialCardArt.NavNotebook, MaterialCardArt.Ink);
            if (reinterview != null) SkinMaterialButton(reinterview.GetComponent<Button>(), MaterialCardArt.NavReturn, MaterialCardArt.Ink);
        }

        static void StampMaterialPlate(Image target, Sprite sprite, bool preserveAspect)
        {
            if (target == null || sprite == null) return;
            target.sprite = sprite;
            target.color = Color.white;
            target.type = Image.Type.Simple;
            target.preserveAspect = preserveAspect;
            target.raycastTarget = false;
        }

        static void SkinMaterialButton(Button button, Sprite sprite, Color labelColor)
        {
            if (button == null || sprite == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
            }
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.color = labelColor;
            }
        }

        void ApplyWritingDeskSkin()
        {
            if (writingDeskRoot == null) return;
            if (TryApplyWritingSectionSkin())
                return;
            if (writingDeskBoard != null)
                writingDeskBoard.gameObject.SetActive(false);
            var desk = FindImage(writingDeskRoot, "Desk");
            writingDeskArtOn = GameSettings.IsEnglish && ApplyArtPackImage(desk, "成稿界面/背景", false);
            var paper = FindImage(writingDeskRoot, "Paper");
            if (paper != null)
            {
                if (writingDeskArtOn)
                {
                    paper.sprite = null;
                    paper.color = Color.clear;
                }
                else
                {
                    paper.sprite = null;
                    paper.color = new Color(0.96f, 0.93f, 0.86f, 1f);
                }
            }

            var cover = writingDeskRoot.transform.Find("Paper/HeadlineCover");
            if (cover != null) cover.gameObject.SetActive(writingDeskArtOn);

            string[] hide =
            {
                "Paper/LeftColumn/Kicker",
                "Paper/LeftColumn/Date",
                "Paper/LeftColumn/DraftLabel",
                "Paper/LeftColumn/SourcesLabel",
                "Paper/LeftColumn/Sources",
                "Paper/RightColumn/S1",
                "Paper/RightColumn/S3",
                "Paper/VRule"
            };
            for (int i = 0; i < hide.Length; i++)
            {
                var t = writingDeskRoot.transform.Find(hide[i]);
                if (t != null) t.gameObject.SetActive(!writingDeskArtOn);
            }

            if (paper != null)
            {
                for (int i = 0; i < paper.transform.childCount; i++)
                {
                    var child = paper.transform.GetChild(i);
                    if (child.name.StartsWith("Line"))
                        child.gameObject.SetActive(!writingDeskArtOn);
                }
            }

            var statusIcon = FindImage(writingDeskRoot, "Paper/RightColumn/StatusIcon");
            if (statusIcon != null)
            {
                statusIcon.enabled = false;
                statusIcon.raycastTarget = false;
            }

            LayoutWritingDeskChrome(writingDeskArtOn);

            var bar = writingDeskRoot.transform.Find("ActionBar");
            if (bar != null)
            {
                foreach (var btn in bar.GetComponentsInChildren<Button>(true))
                {
                    if (btn == null) continue;
                    var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    ApplyWritingActionArt(btn, btn.name, label);
                }
            }
        }

        Sprite ArtPackSocialSprite(string resourceKey)
        {
            switch (resourceKey)
            {
                case "social_post_01_feed": return LoadArtPackSprite("手机界面帖子1");
                case "social_post_02_feed": return LoadArtPackSprite("手机界面帖子2");
                case "social_post_03_feed": return LoadArtPackSprite("手机界面帖子3");
                case "social_post_03_detail": return LoadArtPackSprite("手机界面帖子3-1");
                default: return null;
            }
        }

        Sprite ArtPackStageSprite(string label)
        {
            if (string.IsNullOrEmpty(label)) return null;
            if (label.Contains("文章发布") || label.Contains("章节结束")) return LoadArtPackSprite("猫猫3");
            return null;
        }

        void ApplyHudChipArt(Button button, string key, TextMeshProUGUI labelText)
        {
            if (button == null) return;
            string icon = HudIconName(key);
            if (string.IsNullOrEmpty(icon)) return;
            var iconSpr = LoadArtPackSprite($"对话界面/{icon} 图标");
            if (iconSpr == null) return;

            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }
            button.transition = Selectable.Transition.ColorTint;

            var iconTf = button.transform.Find("ArtIcon");
            Image iconImg;
            if (iconTf != null)
                iconImg = iconTf.GetComponent<Image>();
            else
            {
                iconImg = CreateImage(button.transform, "ArtIcon", Color.white);
                iconImg.raycastTarget = false;
            }
            iconImg.sprite = iconSpr;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;

            var capTf = button.transform.Find("ArtCaption");
            if (capTf != null) capTf.gameObject.SetActive(false);

            bool en = GameSettings.IsEnglish;
            if (labelText == null) return;

            if (en)
            {
                Stretch(iconImg.rectTransform, new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.98f),
                    Vector2.zero, Vector2.zero);
                labelText.gameObject.SetActive(true);
                labelText.fontSize = 11;
                Stretch(labelText.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.34f),
                    Vector2.zero, Vector2.zero);
            }
            else
            {
                Stretch(iconImg.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f),
                    Vector2.zero, Vector2.zero);
                labelText.gameObject.SetActive(false);
            }
        }

        static string HudIconName(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.Contains("ui.backlog") || key.Contains("回看") || key.Contains("Backlog") || key.Contains("Review"))
                return "回看";
            if (key.Contains("ui.menu") || key.Contains("菜单") || key.Contains("Menu"))
                return "菜单";
            if (key.Contains("ui.skip") || key.Contains("跳过") || key.Contains("Skip"))
                return "跳过";
            if (key.Contains("hide_dialogue") || key.Contains("show_dialogue")
                || key.Contains("隐藏") || key.Contains("Hide") || key.Contains("Show"))
                return "隐藏对话";
            return null;
        }

        void ApplyHideDialogueArt(Button button, TextMeshProUGUI labelText)
        {
            if (button == null) return;
            var iconSpr = LoadArtPackSprite("对话界面/隐藏对话 图标");
            if (iconSpr == null) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }
            var iconTf = button.transform.Find("ArtIcon");
            Image iconImg;
            if (iconTf != null)
                iconImg = iconTf.GetComponent<Image>();
            else
            {
                iconImg = CreateImage(button.transform, "ArtIcon", Color.white);
                iconImg.raycastTarget = false;
            }
            iconImg.sprite = iconSpr;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;

            var capTf = button.transform.Find("ArtCaption");
            if (capTf != null) capTf.gameObject.SetActive(false);

            bool en = GameSettings.IsEnglish;
            if (en)
            {
                Stretch(iconImg.rectTransform, new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.98f),
                    Vector2.zero, Vector2.zero);
                if (labelText != null)
                {
                    labelText.gameObject.SetActive(true);
                    labelText.fontSize = 11;
                    Stretch(labelText.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.34f),
                        Vector2.zero, Vector2.zero);
                }
            }
            else
            {
                Stretch(iconImg.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f),
                    Vector2.zero, Vector2.zero);
                if (labelText != null)
                    labelText.gameObject.SetActive(false);
            }
        }

        static bool ContainsAny(string text, params string[] tokens)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < tokens.Length; i++)
                if (text.IndexOf(tokens[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        void ApplyDialogueChoiceArt(Button button)
        {
            if (button == null) return;
            ApplyArtPackImage(button.GetComponent<Image>(), "对话界面/选项框");
        }

        void ApplyNotebookStickyArt(Image image, int visualIndex)
        {
            int n = Mathf.Abs(visualIndex) % 6 + 1;
            // Fill the grid cell — preserveAspect letterboxes and makes stickies look tiny.
            ApplyArtPackImage(image, $"记者笔记/贴纸{n}", true);
        }

        void ApplyWritingCardArt(Image image, int visualIndex)
        {
            var sprite = MaterialCardArt.Card(visualIndex);
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        void ApplyWritingDotArt(Image image, bool selected)
        {
            if (image == null) return;
            var sprite = MaterialCardArt.SelectionDot;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
                image.color = selected ? MaterialCardArt.Cream : new Color(0.55f, 0.52f, 0.48f, 1f);
            }

            var hole = image.transform.Find("Hole");
            if (hole != null)
            {
                hole.gameObject.SetActive(!selected);
                var holeImg = hole.GetComponent<Image>();
                if (holeImg != null && sprite != null)
                {
                    holeImg.sprite = sprite;
                    holeImg.color = new Color(0.45f, 0.30f, 0.20f, 1f);
                    holeImg.preserveAspect = true;
                }
            }
        }

        void ApplyWritingActionArt(Button button, string name, TextMeshProUGUI label)
        {
            if (button == null) return;
            if (name == "GoWriteBtn" || name == "PreviewBtn"
                || name == "BackDirBtn" || name == "NotebookBtn" || name == "ReInterviewBtn")
            {
                if (writingMatsRoot != null && button.transform.IsChildOf(writingMatsRoot.transform))
                {
                    ApplyWritingBoardSkin();
                    return;
                }
            }
            if (name == "BackMats" || name == "PreviewDesk" || name == "AiPolish" || name == "Submit")
            {
                string idle;
                string hover;
                if (name == "BackMats")
                {
                    idle = "成稿界面/返回图标（未选中）";
                    hover = "成稿界面/返回图标（选中）";
                }
                else if (name == "PreviewDesk")
                {
                    idle = "成稿界面/预览图标（未选中）";
                    hover = "成稿界面/预览图标（选中）";
                }
                else if (name == "AiPolish")
                {
                    idle = "成稿界面/编辑图标（未选中）";
                    hover = "成稿界面/编辑图标（选中）";
                }
                else
                {
                    idle = "成稿界面/提交图标（未选中）";
                    hover = "成稿界面/提交图标（选中）";
                }

                var image = button.GetComponent<Image>();
                if (GameSettings.IsEnglish && ApplyArtPackButton(button, idle, hover, false))
                {
                    if (label != null) label.gameObject.SetActive(false);
                }
                else if (label != null)
                {
                    label.gameObject.SetActive(true);
                    if (image != null)
                    {
                        image.sprite = null;
                        image.color = name == "Submit"
                            ? new Color(0.90f, 0.45f, 0.16f, 1f)
                            : new Color(0.20f, 0.34f, 0.48f, 1f);
                    }
                }
                return;
            }
        }

        void ApplyInterviewActionArt(Button button, string label, TextMeshProUGUI labelText)
        {
            if (button == null) return;
            string idle = null;
            string hover = null;
            if (ContainsAny(label, "笔记", "Notebook", "Notes"))
            {
                idle = "自由采访/笔记框（未选中）";
                hover = "自由采访/笔记框（选中）";
            }
            else if (ContainsAny(label, "回看", "回放", "Backlog", "Review"))
            {
                idle = "自由采访/回放框（未选中）";
                hover = "自由采访/回放框（选中）";
            }
            else if (ContainsAny(label, "菜单", "目录", "Menu"))
            {
                idle = "自由采访/目录框（未选中）";
                hover = "自由采访/目录框（选中）";
            }
            else
                return;

            var idleSpr = LoadArtPackSprite(idle);
            var hoverSpr = LoadArtPackSprite(hover);
            if (idleSpr == null || idleSpr.rect.width > 600f) return;
            if (hoverSpr == null || hoverSpr.rect.width > 600f)
                hoverSpr = idleSpr;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null) return;
            image.sprite = idleSpr;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            var box = button.GetComponent<LayoutElement>();
            if (box != null)
                box.ignoreLayout = true;
            var rt = button.GetComponent<RectTransform>();
            // Fractions of the Tools rect (top-down): three equal slots under the END INTERVIEW tape.
            float slotY;
            if (ContainsAny(label, "回看", "回放", "Backlog", "Review"))
                slotY = 0.36f;
            else if (ContainsAny(label, "笔记", "Notebook", "Notes"))
                slotY = 0.57f;
            else
                slotY = 0.78f;
            PlaceInterviewToolPaint(rt, idleSpr, idle, 0.13f, slotY, 0.74f, 0.175f);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = hoverSpr,
                pressedSprite = hoverSpr,
                selectedSprite = hoverSpr,
                disabledSprite = idleSpr
            };
            if (labelText != null)
                labelText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Painted bounds (x, y from bottom, w, h) of the interview tool strips. The files carry
        /// uneven transparent padding (notes idle has 12px), which made Notes draw smaller.
        /// </summary>
        static readonly Dictionary<string, RectInt> InterviewToolPaint = new Dictionary<string, RectInt>
        {
            { "自由采访/回放框（未选中）", new RectInt(6, 1, 236, 69) },
            { "自由采访/回放框（选中）", new RectInt(6, 3, 236, 69) },
            { "自由采访/目录框（未选中）", new RectInt(6, 3, 236, 69) },
            { "自由采访/目录框（选中）", new RectInt(5, 0, 235, 66) },
            { "自由采访/笔记框（未选中）", new RectInt(12, 5, 236, 68) },
            { "自由采访/笔记框（选中）", new RectInt(5, 2, 236, 68) },
        };

        /// <summary>
        /// Anchors the button so the painted part of <paramref name="sprite"/> fills the slot
        /// (x, y top-down, w, h in parent fractions) and the transparent margin spills outside it.
        /// </summary>
        static void PlaceInterviewToolPaint(RectTransform rt, Sprite sprite, string path,
            float x, float y, float w, float h)
        {
            if (rt == null) return;
            if (sprite == null || !InterviewToolPaint.TryGetValue(path, out var paint)
                || paint.width <= 0 || paint.height <= 0)
            {
                PlaceInterviewTool(rt, x, y, w, h);
                return;
            }
            float sw = sprite.rect.width;
            float sh = sprite.rect.height;
            float bottom = 1f - (y + h);
            float top = 1f - y;
            rt.anchorMin = new Vector2(
                x - paint.x / (float)paint.width * w,
                bottom - paint.y / (float)paint.height * h);
            rt.anchorMax = new Vector2(
                x + w + (sw - paint.x - paint.width) / paint.width * w,
                top + (sh - paint.y - paint.height) / paint.height * h);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        bool ApplyTitleButtonArt(Button button, string label)
        {
            if (button == null || GameSettings.IsEnglish) return false;
            string idle = null, hover = null;
            if (ContainsAny(label, "新游戏", "开始", "New Game", "Start"))
            {
                idle = "菜单界面/1开始（未选中）";
                hover = "菜单界面/1开始（选中）";
            }
            else if (ContainsAny(label, "继续", "Continue", "自动档"))
            {
                idle = "菜单界面/2继续（未选中）";
                hover = "菜单界面/2继续（选中）";
            }
            else if (ContainsAny(label, "设置", "Settings"))
            {
                idle = "菜单界面/5设置（未选中）";
                hover = "菜单界面/5设置（选中）";
            }
            else if (ContainsAny(label, "退出", "Quit"))
            {
                idle = "菜单界面/6退出（未选中）";
                hover = "菜单界面/6退出（选中）";
            }
            else
                return false;

            return ApplyArtPackButton(button, idle, hover, false);
        }

        bool TryApplyArtPackTitleBackground(Image desk, GameObject magazine, GameObject leftPage,
            Image contentsHeader, TextMeshProUGUI contentsLabel)
        {
            if (!ApplyArtPackImage(desk, "菜单界面/背景")) return false;
            if (magazine != null)
            {
                var host = magazine.transform.parent as RectTransform;
                if (host != null) StretchFull(host);
                magazine.SetActive(false);
            }
            if (leftPage != null) leftPage.SetActive(false);
            if (contentsHeader != null) contentsHeader.gameObject.SetActive(false);
            if (contentsLabel != null) contentsLabel.gameObject.SetActive(false);
            foreach (var propName in new[] { "PropTranslator", "PropNotes", "PropPolaroidA", "PropPolaroidB", "PropScraps" })
            {
                var prop = titleRoot != null ? titleRoot.transform.Find(propName) : null;
                if (prop != null) prop.gameObject.SetActive(false);
            }
            return true;
        }
    }
}
