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
            ArtPackCache[relativePath] = sprite;
            return sprite;
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

            if (namePlate != null)
            {
                namePlate.color = new Color(0.96f, 0.92f, 0.84f, 0.98f);
                HideChild(namePlate.transform, "NameAccent");
                var nprt = namePlate.rectTransform;
                nprt.anchoredPosition = new Vector2(64f, 6f);
                nprt.sizeDelta = new Vector2(180f, 34f);
            }

            // Deep insets: torn PNG edges + right notebook tab must not clip glyphs.
            var bodyHost = dialoguePanel.transform.Find("BodyHost") as RectTransform;
            if (bodyHost != null)
                Stretch(bodyHost, Vector2.zero, Vector2.one, new Vector2(88f, 28f), new Vector2(-148f, -36f));

            if (buttonRoot != null)
            {
                var br = buttonRoot.GetComponent<RectTransform>();
                // Keep End talk / Skip clear of the protruding notebook tab.
                br.anchoredPosition = new Vector2(-36f, 10f);
            }
            if (statusText != null)
                statusText.rectTransform.anchoredPosition = new Vector2(88f, 12f);
            if (clickHintText != null)
                clickHintText.rectTransform.anchoredPosition = new Vector2(-148f, 12f);

            if (bodyText != null)
            {
                bodyText.overflowMode = TextOverflowModes.Truncate;
                bodyText.lineSpacing = 4f;
                bodyText.extraPadding = true;
                bodyText.margin = new Vector4(6f, 4f, 6f, 4f);
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
            if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
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
            // Never stamp 自由采访/背景 onto HitCatcher — full mockup.
            // Never stamp 打字框 onto InputBar — fights the live TMP field.
            // Never stamp 照片框 — bakes "NEIGHBORHOOD SECURITY GUARD LIKES NAPS." and
            // a black plate that fights the live cat portrait + name plate.
            if (ApplyArtPackImage(interviewSendBtnImage, "自由采访/发送键", true)
                && interviewSendLabel != null)
                interviewSendLabel.gameObject.SetActive(false);
        }

        void ApplyNotebookSkin()
        {
            if (notebookInspirePanel == null) return;
            // Stretch to fill host (no preserveAspect) so lined notebook paper doesn't show through.
            if (!ApplyArtPackImage(notebookInspirePanel, "记者笔记/问题灵感", false))
                return;
            notebookInspirePanel.type = Image.Type.Simple;
            notebookInspirePanel.color = Color.white;

            // Keep inspire above the lined page after skinning.
            var host = notebookInspirePanel.transform.parent;
            if (host != null)
                host.SetAsLastSibling();

            // Art already has paperclip + "QUESTION INSPIRATION" — hide duplicates.
            HideChild(notebookInspirePanel.transform, "Paperclip");
            HideChild(notebookInspirePanel.transform, "Bulb");
            if (notebookInspireHeaderText != null)
                notebookInspireHeaderText.gameObject.SetActive(false);

            // Body copy sits in the blank brown area under the baked stamp.
            if (notebookInspireBodyText != null)
            {
                Stretch(notebookInspireBodyText.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.55f),
                    Vector2.zero, Vector2.zero);
                notebookInspireBodyText.color = new Color(0.08f, 0.05f, 0.03f, 1f);
                notebookInspireBodyText.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
                notebookInspireBodyText.lineSpacing = 8f;
            }
        }

        void ApplyWritingBoardSkin()
        {
            if (writingMatsRoot == null) return;

            // Full board mock (wood frame + cork). Procedural cork on top reads as a dark film.
            var frame = FindImage(writingMatsRoot, "Frame");
            var cork = FindImage(writingMatsRoot, "Frame/Cork");
            if (ApplyArtPackImage(frame, "写稿素材卡库/背景", false) && cork != null)
            {
                cork.sprite = null;
                cork.color = Color.clear;
                cork.enabled = true;
            }

            var tape = FindImage(writingMatsRoot, "Frame/Cork/TapeTitle/Tape");
            // Title sprite bakes English "CHAPTER ONE / WRITING…" — only use it in EN.
            if (GameSettings.IsEnglish
                && ApplyArtPackImage(tape, "写稿素材卡库/标题", true)
                && writingTapeTitle != null)
            {
                writingTapeTitle.gameObject.SetActive(false);
            }
            else if (writingTapeTitle != null)
            {
                writingTapeTitle.gameObject.SetActive(true);
                writingTapeTitle.text = UiLoc.T("ui.writing.tape_title", "第一章 写稿 / 素材卡库");
            }

            // Keep lined paper + UiLoc paragraph rows (full strip sprite would bury the list).
            ApplyArtPackImage(FindImage(writingMatsRoot, "Frame/Cork/DetailPaper"),
                "写稿素材卡库/右侧便签", true);

            // Shadows against art-pack cork look like an extra black film.
            var stripShadow = FindImage(writingMatsRoot, "Frame/Cork/StripShadow");
            if (stripShadow != null) stripShadow.gameObject.SetActive(false);
            var detailShadow = FindImage(writingMatsRoot, "Frame/Cork/DetailShadow");
            if (detailShadow != null) detailShadow.gameObject.SetActive(false);
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
            if (label.Contains("平面图")) return LoadArtPackSprite("槐安社区平面图");
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

            // Never show baked Chinese 「… 字」 captions — they cannot localize.
            var capTf = button.transform.Find("ArtCaption");
            if (capTf != null) capTf.gameObject.SetActive(false);

            bool en = GameSettings.IsEnglish;
            if (labelText == null) return;

            if (en)
            {
                // EN: icon + UiLoc TMP caption (spawn / RefreshLocalizedChrome owns the string).
                Stretch(iconImg.rectTransform, new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.98f),
                    Vector2.zero, Vector2.zero);
                labelText.gameObject.SetActive(true);
                labelText.fontSize = 11;
                Stretch(labelText.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.34f),
                    Vector2.zero, Vector2.zero);
            }
            else
            {
                // ZH: icons-only (字 sprites look muddy under the chips).
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
            ApplyArtPackImage(image, $"记者笔记/贴纸{n}", false);
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
            bool applied = false;
            if (name == "GoWriteBtn")
            {
                // Sprite bakes English "GO TO WRITING". EN: art only. ZH: solid button + UiLoc TMP.
                if (GameSettings.IsEnglish)
                {
                    applied = ApplyArtPackButton(button, "写稿素材卡库/写作按键", "写稿素材卡库/写作按键");
                    if (applied && label != null)
                        label.gameObject.SetActive(false);
                }
                else if (label != null)
                {
                    label.gameObject.SetActive(true);
                    label.text = UiLoc.T("ui.writing.go_write", "前往写稿");
                }
                return;
            }
            if (name == "BackMats")
                applied = ApplyArtPackButton(button, "成稿界面/返回图标（未选中）", "成稿界面/返回图标（选中）");
            else if (name == "AiPolish")
                applied = ApplyArtPackButton(button, "成稿界面/编辑图标（未选中）", "成稿界面/编辑图标（选中）");
            else if (name == "Submit")
                applied = ApplyArtPackButton(button, "成稿界面/提交图标（未选中）", "成稿界面/提交图标（选中）");
            else
                return;

            if (applied && label != null && !GameSettings.IsEnglish)
                label.gameObject.SetActive(false);
        }

        void ApplyInterviewActionArt(Button button, string label, TextMeshProUGUI labelText)
        {
            if (button == null) return;
            // Chinese art frames bake 回放/目录/笔记 glyphs — skip in EN and keep UiLoc TMP.
            if (GameSettings.IsEnglish) return;
            bool applied = false;
            if (ContainsAny(label, "笔记", "Notebook"))
                applied = ApplyArtPackButton(button, "自由采访/笔记框（未选中）", "自由采访/笔记框（选中）");
            else if (ContainsAny(label, "回看", "回放", "Backlog"))
                applied = ApplyArtPackButton(button, "自由采访/回放框（未选中）", "自由采访/回放框（选中）");
            else if (ContainsAny(label, "菜单", "目录", "Menu"))
                applied = ApplyArtPackButton(button, "自由采访/目录框（未选中）", "自由采访/目录框（选中）");
            else
                return;

            if (applied && labelText != null)
                labelText.gameObject.SetActive(false);
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
