using System.Collections.Generic;
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

        // Near-black ink on cream parchment — muted browns wash out and look "light gray".
        static readonly Color ArtPackInk = new Color(0.02f, 0.02f, 0.02f, 1f);
        static readonly Color ArtPackInkMuted = new Color(0.12f, 0.10f, 0.09f, 1f);
        static readonly Color ArtPackInkInner = new Color(0.10f, 0.12f, 0.16f, 1f);
        static readonly Color ArtPackInkSystem = new Color(0.35f, 0.16f, 0.05f, 1f);

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
                Stretch(bodyHost, Vector2.zero, Vector2.one, new Vector2(76f, 42f), new Vector2(-136f, -30f));

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
                bodyText.lineSpacing = 10f;
                bodyText.extraPadding = true;
                bodyText.margin = new Vector4(6f, 4f, 8f, 4f);
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

        static void SharpenDialogueTmp(TextMeshProUGUI t)
        {
            if (t == null) return;
            t.enableAutoSizing = false;
            t.fontStyle = FontStyles.Normal;
            // Instance the material so softness tweaks don't dirty the shared SDF asset.
            var mat = t.fontMaterial;
            if (mat == null) return;
            // Face tint must stay white — gray face × dark vertex = washed mid-gray on parchment.
            if (mat.HasProperty(ShaderUtilities.ID_FaceColor))
                mat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            if (mat.HasProperty(ShaderUtilities.ID_OutlineSoftness))
                mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            // Dilate 0.1 fattens the SDF edge into a gray halo on the cream paper.
            if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
            if (mat.HasProperty(ShaderUtilities.ID_Sharpness))
                mat.SetFloat(ShaderUtilities.ID_Sharpness, 0.75f);
            if (mat.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
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
            if (left != null)
                Stretch(left, new Vector2(0.018f, 0.04f), new Vector2(0.225f, 0.96f), Vector2.zero, Vector2.zero);
            if (center != null)
                Stretch(center, new Vector2(0.215f, 0.05f), new Vector2(0.785f, 0.955f), Vector2.zero, Vector2.zero);
            if (right != null)
                Stretch(right, new Vector2(0.785f, 0.025f), new Vector2(0.985f, 0.97f), Vector2.zero, Vector2.zero);

            var status = interviewRoot.transform.Find("LeftColumn/StatusPad") as RectTransform;
            var portrait = interviewRoot.transform.Find("LeftColumn/PortraitPad") as RectTransform;
            if (status != null)
                Stretch(status, new Vector2(0f, 0.50f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            if (portrait != null)
                Stretch(portrait, new Vector2(0f, 0.02f), new Vector2(1f, 0.48f), Vector2.zero, Vector2.zero);

            var inspire = interviewRoot.transform.Find("RightColumn/InspirePad") as RectTransform;
            var tools = interviewRoot.transform.Find("RightColumn/ToolbarPad") as RectTransform;
            if (inspire != null)
                Stretch(inspire, new Vector2(0f, 0.36f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            if (tools != null)
                Stretch(tools, new Vector2(0f, 0f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero);

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

            var frameHost = interviewRoot.transform.Find("LeftColumn/PortraitPad");
            if (frameHost != null)
            {
                var frame = FindImage(interviewRoot, "LeftColumn/PortraitPad/ArtFrame");
                if (frame == null)
                {
                    frame = CreateImage(frameHost, "ArtFrame", Color.white);
                    frame.raycastTarget = false;
                }
                if (ApplyArtPackImage(frame, "自由采访/照片框", true))
                    StretchFull(frame.rectTransform);
                frame.transform.SetSiblingIndex(0);
                if (interviewPortraitImage != null)
                {
                    Stretch(interviewPortraitImage.rectTransform,
                        new Vector2(0.14f, 0.34f), new Vector2(0.86f, 0.78f),
                        Vector2.zero, Vector2.zero);
                    interviewPortraitImage.transform.SetAsLastSibling();
                }
                HidePath(interviewRoot.transform, "LeftColumn/PortraitPad/Paper/NamePlate");
            }

            var inputBar = FindImage(interviewRoot, "CenterColumn/MainPaper/InputBar");
            if (inputBar != null && ApplyArtPackImage(inputBar, "自由采访/打字框", false))
            {
                inputBar.color = Color.white;
                inputBar.type = Image.Type.Simple;
            }

            if (ApplyArtPackImage(interviewSendBtnImage, "自由采访/发送键", true)
                && interviewSendLabel != null)
                interviewSendLabel.gameObject.SetActive(false);
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
                new Vector2(-0.02f, 0.30f), new Vector2(1.06f, 0.48f),
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
            if (label != null) label.gameObject.SetActive(false);
            var icon = FindOrCreateRowIcon(row, "ArtIcon");
            var word = FindOrCreateRowIcon(row, "ArtWord");
            if (icon != null && ApplyArtPackImage(icon, iconPath, true))
            {
                var rt = icon.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.35f);
                rt.anchorMax = new Vector2(0.18f, 0.95f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            if (word != null && ApplyArtPackImage(word, wordPath, true))
            {
                var rt = word.rectTransform;
                rt.anchorMin = new Vector2(0.20f, 0.55f);
                rt.anchorMax = new Vector2(0.72f, 0.95f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var track = row.Find("Track");
            if (track != null)
            {
                var trackImg = track.GetComponent<Image>();
                if (trackImg != null && ApplyArtPackImage(trackImg, framePath, false))
                    trackImg.color = Color.white;
                var trt = track as RectTransform;
                if (trt != null)
                    Stretch(trt, new Vector2(0.20f, 0.08f), new Vector2(0.78f, 0.48f), Vector2.zero, Vector2.zero);
                var fillImg = track.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null)
                    fillImg.color = fill;
            }
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

            var frame = FindImage(writingMatsRoot, "Frame");
            var cork = FindImage(writingMatsRoot, "Frame/Cork");
            if (ApplyArtPackImage(frame, "写稿素材卡库/背景", false) && cork != null)
            {
                cork.sprite = null;
                cork.color = Color.clear;
                cork.enabled = true;
                cork.raycastTarget = false;
            }

            bool en = GameSettings.IsEnglish;
            var titlePlate = FindImage(writingMatsRoot, "TitlePlate");
            bool titleArt = en && ApplyArtPackImage(titlePlate, "写稿素材卡库/标题", false);
            if (titlePlate != null)
            {
                titlePlate.enabled = titleArt;
                titlePlate.raycastTarget = false;
                if (titleArt)
                    titlePlate.transform.SetAsLastSibling();
            }
            if (writingTapeTitle != null)
                writingTapeTitle.gameObject.SetActive(!titleArt);

            var selectedArt = FindImage(writingMatsRoot, "Frame/Cork/SelectedHeader/LabelArt");
            bool selectedLabel = en && ApplyArtPackImage(selectedArt, "写稿素材卡库/已选素材", true);
            if (writingSelectedCountText != null)
                writingSelectedCountText.gameObject.SetActive(!selectedLabel);

            var structure = FindImage(writingMatsRoot, "Frame/Cork/ParagraphStrip/StructureArt");
            bool structureArt = en && ApplyArtPackImage(structure, "写稿素材卡库/文章结构", true);
            var stripTitle = writingMatsRoot.transform.Find("Frame/Cork/ParagraphStrip/StripTitle");
            if (stripTitle != null)
                stripTitle.gameObject.SetActive(!structureArt);

            ApplyArtPackImage(FindImage(writingMatsRoot, "Frame/Cork/DetailPaper"),
                "写稿素材卡库/右侧便签", false);
            var clip = writingMatsRoot.transform.Find("Frame/Cork/DetailPaper/Paperclip");
            if (clip != null) clip.gameObject.SetActive(false);
            var doodle = writingMatsRoot.transform.Find("Frame/Cork/DetailPaper/CatDoodle");
            if (doodle != null) doodle.gameObject.SetActive(false);
        }

        void ApplyWritingDeskSkin()
        {
            if (writingDeskRoot == null) return;
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

            var matsHeader = writingDeskRoot.transform.Find("Paper/RightColumn/MatsHeader") as RectTransform;
            if (matsHeader != null)
            {
                if (writingDeskArtOn)
                    Stretch(matsHeader, new Vector2(0.78f, 0.685f), new Vector2(0.92f, 0.725f),
                        Vector2.zero, Vector2.zero);
                else
                    Stretch(matsHeader, new Vector2(0.67f, 0.675f), new Vector2(0.92f, 0.73f),
                        Vector2.zero, Vector2.zero);
            }

            var statusIcon = FindImage(writingDeskRoot, "Paper/RightColumn/StatusIcon");
            if (statusIcon != null)
            {
                bool icon = writingDeskArtOn && ApplyArtPackImage(statusIcon, "成稿界面/生成状态小图标", true);
                statusIcon.enabled = icon;
                statusIcon.raycastTarget = false;
            }

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
            int n = Mathf.Abs(visualIndex) % 6 + 1;
            // Fill the cell — preserveAspect letterboxes dark cork into the card.
            ApplyArtPackImage(image, $"写稿素材卡库/中间贴纸{n}", false);
        }

        void ApplyWritingDotArt(Image image, bool selected)
        {
            if (image == null) return;
            var path = selected
                ? "写稿素材卡库/已选素材小点（选中）"
                : "写稿素材卡库/已选素材小点（未选中）";
            if (ApplyArtPackImage(image, path, true))
                image.color = Color.white;
        }

        void ApplyWritingActionArt(Button button, string name, TextMeshProUGUI label)
        {
            if (button == null) return;
            if (name == "GoWriteBtn" || name == "PreviewBtn")
            {
                string path = name == "PreviewBtn" ? "写稿素材卡库/预览按键" : "写稿素材卡库/写作按键";
                string locKey = name == "PreviewBtn" ? "ui.writing.preview" : "ui.writing.go_write";
                string fallback = name == "PreviewBtn" ? "预览文章" : "前往写稿";
                var image = button.GetComponent<Image>();
                if (GameSettings.IsEnglish && ApplyArtPackButton(button, path, path, false))
                {
                    if (label != null) label.gameObject.SetActive(false);
                }
                else if (label != null)
                {
                    label.gameObject.SetActive(true);
                    label.text = UiLoc.T(locKey, fallback);
                    if (image != null)
                    {
                        image.sprite = null;
                        image.color = name == "PreviewBtn"
                            ? new Color(0.16f, 0.28f, 0.48f, 1f)
                            : new Color(0.83f, 0.36f, 0.18f, 1f);
                    }
                }
                return;
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
            image.preserveAspect = true;
            image.color = Color.white;
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
