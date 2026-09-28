using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>Applies the production UI exported in the 拆拆拆 art pack.</summary>
    public partial class GameUI
    {
        const string ArtPackRoot = "VnArt/UI/ArtPack/";
        static readonly Dictionary<string, Sprite> ArtPackCache = new Dictionary<string, Sprite>();
        static readonly HashSet<string> MissingArtWarnings = new HashSet<string>();

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

        bool ApplyArtPackImage(Image target, string relativePath, string screen, string component,
            bool preserveAspect = false, bool placeholderWhenMissing = true)
        {
            if (target == null) return false;
            var sprite = LoadArtPackSprite(relativePath);
            if (sprite == null)
            {
                if (placeholderWhenMissing)
                    ApplyMissingArtPlaceholder(target, screen, component, relativePath);
                return false;
            }

            target.sprite = sprite;
            target.type = Image.Type.Simple;
            target.preserveAspect = preserveAspect;
            target.color = Color.white;
            return true;
        }

        void ApplyMissingArtPlaceholder(Image target, string screen, string component, string expectedPath)
        {
            if (target == null) return;
            target.sprite = null;
            target.color = new Color(0.92f, 0.08f, 0.72f, 0.78f);

            var key = screen + "/" + component + "/" + expectedPath;
            if (MissingArtWarnings.Add(key))
                Debug.LogWarning($"[UI Art Missing] {screen} > {component}: {expectedPath}");

            var existing = target.transform.Find("MissingArtLabel");
            TextMeshProUGUI label;
            if (existing != null)
                label = existing.GetComponent<TextMeshProUGUI>();
            else
            {
                var go = new GameObject("MissingArtLabel", typeof(RectTransform));
                go.transform.SetParent(target.transform, false);
                StretchFull(go.GetComponent<RectTransform>());
                label = go.AddComponent<TextMeshProUGUI>();
                label.font = font;
                label.fontSize = 14f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.enableWordWrapping = true;
                label.raycastTarget = false;
            }
            if (label != null)
                label.text = $"缺图\n{screen} / {component}";
        }

        bool ApplyArtPackButton(Button button, string idlePath, string hoverPath,
            string screen, string component, bool preserveAspect = true)
        {
            if (button == null) return false;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null) return false;

            var idle = LoadArtPackSprite(idlePath);
            var hover = LoadArtPackSprite(hoverPath);
            if (idle == null || hover == null)
            {
                ApplyMissingArtPlaceholder(image, screen, component, idle == null ? idlePath : hoverPath);
                button.transition = Selectable.Transition.ColorTint;
                return false;
            }

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

        Image FindImage(GameObject root, string path)
        {
            if (root == null) return null;
            var child = root.transform.Find(path);
            return child != null ? child.GetComponent<Image>() : null;
        }

        void ApplyArtPackFixedSkin()
        {
            ApplyArtPackImage(dialoguePanel, "对话界面/底框", "对话界面", "对话底框");
            if (choiceHostImage != null)
            {
                choiceHostImage.sprite = null;
                choiceHostImage.type = Image.Type.Simple;
                choiceHostImage.color = new Color(0f, 0f, 0f, 0.001f);
            }

            ApplyArtPackImage(FindImage(backlogRoot, "Dim"), "对话回看/背景", "对话回看", "背景");
            if (backlogRoot != null)
            {
                var backdrop = CreateImage(backlogRoot.transform, "ArtPackPanel", Color.white);
                StretchFull(backdrop.rectTransform);
                backdrop.transform.SetSiblingIndex(1);
                backdrop.raycastTarget = false;
                ApplyArtPackImage(backdrop, "对话回看/底板", "对话回看", "回看底板");
                var legacyPanel = FindImage(backlogRoot, "Panel");
                if (legacyPanel != null) legacyPanel.color = new Color(1f, 1f, 1f, 0.001f);
            }

            ApplyArtPackImage(FindImage(interviewRoot, "HitCatcher"), "自由采访/背景", "自由采访", "背景");
            ApplyArtPackImage(FindImage(interviewRoot, "LeftColumn/PortraitPad/Paper"),
                "自由采访/照片框", "自由采访", "受访者照片框", true);
            ApplyArtPackImage(FindImage(interviewRoot, "CenterColumn/MainPaper/InputBar"),
                "自由采访/打字框", "自由采访", "提问输入框", true);
            ApplyArtPackImage(interviewSendBtnImage, "自由采访/发送键", "自由采访", "发送键", true);
            ApplyArtPackImage(FindImage(interviewRoot, "LeftColumn/PortraitPad/Paper/NamePlate"),
                "自由采访/大福名字", "自由采访", "受访者名字牌", true);
            ApplyInterviewMeterSkin("Trust", "信任");
            ApplyInterviewMeterSkin("Stress", "压力");
            ApplyInterviewMeterSkin("Focus", "专注");

            ApplyArtPackImage(FindImage(notebookRoot, "Dim"), "记者笔记/背景", "记者笔记", "背景");
            var notebookDesk = FindImage(notebookRoot, "Desk");
            if (notebookDesk != null) notebookDesk.color = new Color(1f, 1f, 1f, 0.001f);
            ApplyArtPackImage(notebookInspirePanel, "记者笔记/问题灵感", "记者笔记", "问题灵感卡", true);

            ApplyArtPackImage(FindImage(writingMatsRoot, "Frame"), "写稿素材卡库/背景",
                "写稿素材卡库", "背景");
            var legacyCork = FindImage(writingMatsRoot, "Frame/Cork");
            if (legacyCork != null) legacyCork.color = new Color(1f, 1f, 1f, 0.001f);
            ApplyArtPackImage(FindImage(writingMatsRoot, "Frame/Cork/ParagraphStrip"),
                "写稿素材卡库/文章结构", "写稿素材卡库", "文章结构", true);
            ApplyArtPackImage(FindImage(writingMatsRoot, "Frame/Cork/DetailPaper"),
                "写稿素材卡库/右侧便签", "写稿素材卡库", "右侧素材详情便签", true);

            ApplyArtPackImage(FindImage(writingDeskRoot, "Desk"), "成稿界面/背景", "成稿界面", "背景");
            var deskPaper = FindImage(writingDeskRoot, "Paper");
            if (deskPaper != null) deskPaper.color = new Color(1f, 1f, 1f, 0.001f);

            ApplyMissingArtPlaceholder(FindImage(menuRoot, "Panel"), "游戏菜单", "菜单面板", "游戏菜单/面板");
            ApplyMissingArtPlaceholder(FindImage(saveLoadRoot, "Panel"), "存读档界面", "存读档面板", "存读档界面/面板");
            ApplyMissingArtPlaceholder(FindImage(confirmRoot, "Panel"), "确认弹窗", "确认面板", "确认弹窗/面板");
            ApplyMissingArtPlaceholder(FindImage(settingsRoot, "Panel"), "设置界面", "设置面板", "设置界面/面板");
            ApplyMissingButtonPlaceholders(menuRoot, "游戏菜单");
            ApplyMissingButtonPlaceholders(saveLoadRoot, "存读档界面");
            ApplyMissingButtonPlaceholders(confirmRoot, "确认弹窗");
            ApplyMissingButtonPlaceholders(settingsRoot, "设置界面");
            ApplyMissingButtonPlaceholders(backlogRoot, "对话回看");
            ApplyMissingButtonPlaceholders(notebookRoot, "记者笔记");
        }

        void ApplyMissingButtonPlaceholders(GameObject root, string screen)
        {
            if (root == null) return;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
                if (image == null || image.sprite != null) continue;
                ApplyMissingArtPlaceholder(image, screen, button.gameObject.name + "按钮",
                    screen + "/" + button.gameObject.name + "（普通、悬停、按下）");
            }
        }

        void ApplyInterviewMeterSkin(string rowName, string artName)
        {
            if (interviewRoot == null) return;
            var row = interviewRoot.transform.Find("LeftColumn/StatusPad/Paper/" + rowName);
            if (row == null) return;

            var frame = CreateImage(row, "ArtFrame", Color.white);
            StretchFull(frame.rectTransform);
            frame.transform.SetAsFirstSibling();
            frame.raycastTarget = false;
            ApplyArtPackImage(frame, $"自由采访/{artName} 框", "自由采访", artName + "状态框", true);

            var icon = CreateImage(row, "ArtIcon", Color.white);
            Stretch(icon.rectTransform, new Vector2(0f, 0.05f), new Vector2(0.22f, 0.95f), Vector2.zero, Vector2.zero);
            icon.raycastTarget = false;
            ApplyArtPackImage(icon, $"自由采访/{artName} 图标", "自由采访", artName + "图标", true);

            var word = CreateImage(row, "ArtText", Color.white);
            Stretch(word.rectTransform, new Vector2(0.20f, 0.48f), new Vector2(0.60f, 0.95f), Vector2.zero, Vector2.zero);
            word.raycastTarget = false;
            ApplyArtPackImage(word, $"自由采访/{artName} 字", "自由采访", artName + "文字", true);

            var value = CreateImage(row, "ArtValue", Color.white);
            Stretch(value.rectTransform, new Vector2(0.58f, 0.48f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);
            value.transform.SetAsFirstSibling();
            value.raycastTarget = false;
            ApplyArtPackImage(value, "自由采访/信任压力专注 数值底框", "自由采访", artName + "数值底框", true);

            var oldLabel = row.Find("Label");
            if (oldLabel != null) oldLabel.gameObject.SetActive(false);
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

        void ApplyHudChipArt(Button button, string label, TextMeshProUGUI labelText)
        {
            if (button == null) return;
            string icon = label.Contains("回看") ? "回看" : "菜单";
            bool ok = ApplyArtPackButton(button, $"对话界面/{icon} 图标", $"对话界面/{icon} 图标",
                "对话界面", icon + "工具按钮", false);
            if (ok && labelText != null) labelText.gameObject.SetActive(false);
        }

        void ApplyHideDialogueArt(Button button, TextMeshProUGUI labelText)
        {
            if (button == null) return;
            bool ok = ApplyArtPackButton(button, "对话界面/隐藏对话 图标", "对话界面/隐藏对话 图标",
                "对话界面", "隐藏对话按钮", false);
            if (ok && labelText != null) labelText.gameObject.SetActive(false);
        }

        bool ApplyTitleButtonArt(Button button, int index)
        {
            switch (index)
            {
                case 1:
                    return ApplyArtPackButton(button, "菜单界面/1开始（未选中）", "菜单界面/1开始（选中）",
                        "主菜单", "开始按钮");
                case 2:
                    return ApplyArtPackButton(button, "菜单界面/2继续（未选中）", "菜单界面/2继续（选中）",
                        "主菜单", "继续按钮");
                case 3:
                    ApplyMissingArtPlaceholder(button.GetComponent<Image>(), "主菜单", "读档按钮",
                        "菜单界面/读档（未选中、选中）");
                    return false;
                case 4:
                    ApplyMissingArtPlaceholder(button.GetComponent<Image>(), "主菜单", "清除存档按钮",
                        "菜单界面/清除存档（未选中、选中）");
                    return false;
                case 5:
                    return ApplyArtPackButton(button, "菜单界面/5设置（未选中）", "菜单界面/5设置（选中）",
                        "主菜单", "设置按钮");
                case 6:
                    return ApplyArtPackButton(button, "菜单界面/6退出（未选中）", "菜单界面/6退出（选中）",
                        "主菜单", "退出按钮");
                default:
                    return false;
            }
        }

        void ApplyDialogueChoiceArt(Button button)
        {
            if (button == null) return;
            ApplyArtPackImage(button.GetComponent<Image>(), "对话界面/选项框",
                "对话界面", "动态选项按钮", false);
        }

        void ApplyNotebookStickyArt(Image image, int visualIndex)
        {
            int n = Mathf.Abs(visualIndex) % 6 + 1;
            ApplyArtPackImage(image, $"记者笔记/贴纸{n}", "记者笔记", $"主题贴纸 {n}", true);
        }

        void ApplyWritingCardArt(Image image, int visualIndex)
        {
            int n = Mathf.Abs(visualIndex) % 6 + 1;
            ApplyArtPackImage(image, $"写稿素材卡库/中间贴纸{n}", "写稿素材卡库", $"素材卡 {n}", true);
        }

        void ApplyWritingActionArt(Button button, string name, TextMeshProUGUI label)
        {
            if (button == null) return;
            bool applied;
            if (name == "GoWriteBtn")
                applied = ApplyArtPackButton(button, "写稿素材卡库/写作按键", "写稿素材卡库/写作按键",
                    "写稿素材卡库", "前往写稿按钮");
            else if (name == "BackMats")
                applied = ApplyArtPackButton(button, "成稿界面/返回图标（未选中）", "成稿界面/返回图标（选中）",
                    "成稿界面", "返回修改素材按钮");
            else if (name == "AiPolish")
                applied = ApplyArtPackButton(button, "成稿界面/编辑图标（未选中）", "成稿界面/编辑图标（选中）",
                    "成稿界面", "AI 优化/编辑按钮");
            else if (name == "Submit")
                applied = ApplyArtPackButton(button, "成稿界面/提交图标（未选中）", "成稿界面/提交图标（选中）",
                    "成稿界面", "提交按钮");
            else
            {
                ApplyMissingArtPlaceholder(button.GetComponent<Image>(),
                    name == "ClosePreview" ? "成稿预览" : "写稿素材卡库",
                    label != null ? label.text + "按钮" : name + "按钮",
                    (name == "ClosePreview" ? "成稿预览" : "写稿素材卡库") + "/对应按钮（未选中、选中）");
                return;
            }

            if (applied && label != null)
                label.gameObject.SetActive(false);
        }

        void ApplyInterviewActionArt(Button button, string label, TextMeshProUGUI labelText)
        {
            if (button == null) return;
            bool applied = false;
            if (label.Contains("笔记") || label.Contains("Notebook"))
                applied = ApplyArtPackButton(button, "自由采访/笔记框（未选中）", "自由采访/笔记框（选中）",
                    "自由采访", "记者笔记按钮");
            else if (label.Contains("回看") || label.Contains("回放") || label.Contains("Backlog"))
                applied = ApplyArtPackButton(button, "自由采访/回放框（未选中）", "自由采访/回放框（选中）",
                    "自由采访", "回放按钮");
            else if (label.Contains("菜单") || label.Contains("目录") || label.Contains("Menu"))
                applied = ApplyArtPackButton(button, "自由采访/目录框（未选中）", "自由采访/目录框（选中）",
                    "自由采访", "目录按钮");
            else
                ApplyMissingArtPlaceholder(button.GetComponent<Image>(), "自由采访", label + "按钮",
                    "自由采访/对应按钮（未选中、选中）");

            if (applied && labelText != null)
                labelText.gameObject.SetActive(false);
        }
    }
}
