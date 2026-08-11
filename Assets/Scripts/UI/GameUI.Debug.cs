#if UNITY_EDITOR || DEVELOPMENT_BUILD
using StreetCat.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StreetCat.UI
{
    public partial class GameUI
    {
        GameObject debugJumpRoot;
        bool debugJumpBuilt;

        void BuildDebugJumpPanel(Transform parent)
        {
            if (debugJumpBuilt) return;
            debugJumpBuilt = true;

            debugJumpRoot = new GameObject("DebugJumpPanel", typeof(RectTransform));
            debugJumpRoot.transform.SetParent(parent, false);
            var rootRt = debugJumpRoot.GetComponent<RectTransform>();
            Stretch(rootRt, new Vector2(0.02f, 0.08f), new Vector2(0.34f, 0.92f), Vector2.zero, Vector2.zero);

            var bg = CreateImage(debugJumpRoot.transform, "Bg", new Color(0.06f, 0.07f, 0.09f, 0.92f));
            StretchFull(bg.rectTransform);
            bg.raycastTarget = true;

            var title = CreateUiText(debugJumpRoot.transform, "Title", 18, TextAnchor.MiddleLeft,
                VnTheme.Accent, Vector2.zero, Vector2.zero);
            Stretch(title.GetComponent<RectTransform>(),
                new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f), Vector2.zero, Vector2.zero);
            title.text = "测试跳转 (F9)";
            title.fontStyle = FontStyles.Bold;
            title.raycastTarget = false;

            var hint = CreateUiText(debugJumpRoot.transform, "Hint", 13, TextAnchor.MiddleLeft,
                VnTheme.TextMuted, Vector2.zero, Vector2.zero);
            Stretch(hint.GetComponent<RectTransform>(),
                new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
            hint.text = "会重置当前进度并补齐前置旗标";
            hint.raycastTarget = false;

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(debugJumpRoot.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Stretch(scrollRt, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            scrollGo.GetComponent<Image>().raycastTarget = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            viewport.GetComponent<Image>().raycastTarget = true;

            var list = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(viewport.transform, false);
            var lrt = list.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(0, 0);
            var v = list.GetComponent<VerticalLayoutGroup>();
            v.spacing = 4;
            v.padding = new RectOffset(4, 4, 4, 8);
            v.childForceExpandWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = lrt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            foreach (var target in ChapterFlowController.DebugJumpTargets)
            {
                var jumpId = target.Id;
                var label = target.Label;
                var row = new GameObject("Jump_" + jumpId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                row.transform.SetParent(list.transform, false);
                row.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.2f, 0.95f);
                row.GetComponent<LayoutElement>().preferredHeight = 34;
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SfxController.Instance?.PlayUi();
                    SetDebugJumpPanelVisible(false);
                    if (ChapterFlowController.Instance != null)
                        ChapterFlowController.Instance.DebugJumpTo(jumpId);
                });
                var tx = CreateUiText(row.transform, "T", 14, TextAnchor.MiddleLeft,
                    VnTheme.TextPrimary, Vector2.zero, Vector2.zero);
                StretchFull(tx.GetComponent<RectTransform>());
                tx.text = "  " + label;
                tx.raycastTarget = false;
            }

            debugJumpRoot.SetActive(false);
        }

        void EnsureDebugJumpPanel()
        {
            if (debugJumpBuilt || canvasRt == null) return;
            BuildDebugJumpPanel(canvasRt);
        }

        void ToggleDebugJumpPanel()
        {
            EnsureDebugJumpPanel();
            if (debugJumpRoot == null) return;
            SetDebugJumpPanelVisible(!debugJumpRoot.activeSelf);
        }

        void SetDebugJumpPanelVisible(bool on)
        {
            if (debugJumpRoot == null) return;
            debugJumpRoot.SetActive(on);
            if (on)
                debugJumpRoot.transform.SetAsLastSibling();
        }

        /// <returns>True if Escape was consumed by the debug panel.</returns>
        bool HandleDebugJumpHotkey()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                ToggleDebugJumpPanel();
                return false;
            }
            if (Input.GetKeyDown(KeyCode.Escape) && debugJumpRoot != null && debugJumpRoot.activeSelf)
            {
                SetDebugJumpPanelVisible(false);
                return true;
            }
            return false;
        }

        /// <summary>Close menus/overlays before a debug jump swaps modes.</summary>
        public void DebugCloseOverlays()
        {
            if (settingsRoot) settingsRoot.SetActive(false);
            if (menuRoot) menuRoot.SetActive(false);
            if (backlogRoot) backlogRoot.SetActive(false);
            if (notebookRoot) notebookRoot.SetActive(false);
            if (saveLoadRoot) saveLoadRoot.SetActive(false);
            if (confirmRoot) confirmRoot.SetActive(false);
            SetDebugJumpPanelVisible(false);
            SetDialogueHidden(false);
            HideWritingMaterialsBoard();
            HideWritingDesk();
            writingMatsActive = false;

            // Mid-inspect / mid-talk UI queues live on GameUI; clear so the next mode isn't stuck.
            inspectQueue.Clear();
            inspectIndex = 0;
            talkQueue.Clear();
            talkIndex = 0;
            talkAwaitingClickReturn = false;
            playingGuardAppear = false;
            playingWaitForDafuOutro = false;
            playingLinContactChat = false;
            waitingForChoice = false;

            SetInvestigateChrome(false);
            SetInterviewChrome(false);
            SocialHide(instant: true);
            if (choiceHostImage != null) choiceHostImage.gameObject.SetActive(false);
            if (advanceCatcher != null) advanceCatcher.gameObject.SetActive(false);
        }

        public void DebugOpenNotebook()
        {
            OpenNotebook();
        }
    }
}
#endif
