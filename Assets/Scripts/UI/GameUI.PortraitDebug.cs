#if UNITY_EDITOR || DEVELOPMENT_BUILD
using StreetCat.Data;
using StreetCat.Interview;
using StreetCat.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    public partial class GameUI
    {
        bool portraitDebugPanelVisible;
        Vector2 portraitDebugScroll;
        string portraitDebugCharId = "shenhe";
        string portraitDebugBoundLineKey;
        int portraitDebugInterviewRevision;
        static Rect portraitDebugPanelScreenRect;
        static bool portraitDebugPanelVisibleStatic;
        bool portraitDebugInputCaptured;
        bool _advanceCatcherRaycastBeforePortraitDebug = true;
        bool _advanceCatcherActiveBeforePortraitDebug = true;
        bool _dialogueClickInteractableBeforePortraitDebug = true;

        public static bool PortraitDebugPanelOpen => portraitDebugPanelVisibleStatic;

        public static bool PortraitDebugPanelConsumesMouse()
        {
            if (!portraitDebugPanelVisibleStatic) return false;
            return portraitDebugPanelScreenRect.width > 0f
                   && portraitDebugPanelScreenRect.Contains(
                       new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }

        bool HandlePortraitDebugHotkey()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                SetPortraitDebugPanelVisible(!portraitDebugPanelVisible);
                return portraitDebugPanelVisible;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && portraitDebugPanelVisible)
            {
                SetPortraitDebugPanelVisible(false);
                return true;
            }

            return false;
        }

        void SetPortraitDebugPanelVisible(bool on)
        {
            portraitDebugPanelVisible = on;
            portraitDebugPanelVisibleStatic = on;
            if (on)
                SyncPortraitDebugContext();
            else
                RestorePortraitDebugInputBlock();
            SetAdvanceEnabled(canClickAdvance, waitingForChoice);
        }

        void SyncPortraitDebugContext()
        {
            var lineKey = GetPortraitDebugLineKey();
            portraitDebugBoundLineKey = lineKey;

            if (TryGetCurrentPortraitLine(out var speaker, out var kind, out _, out _))
            {
                var charId = PortraitDebugCatalog.MatchCharacterId(speaker, kind);
                if (!string.IsNullOrEmpty(charId))
                    portraitDebugCharId = charId;
            }

            if (!string.IsNullOrEmpty(lineKey))
            {
                var saved = PortraitDebugOverrides.GetSaved(lineKey);
                if (!string.IsNullOrEmpty(saved))
                    PortraitDebugOverrides.SetPreview(lineKey, saved);
            }
        }

        string GetPortraitDebugLineKey()
        {
            if (mode == Mode.Dialogue)
            {
                var sd = SceneDirector.Instance;
                if (sd?.Current != null)
                    return PortraitDebugOverrides.BuildLineKey(sd.Current.id, sd.LineIndex);
                return null;
            }

            if (mode == Mode.Investigate && !investigateHotspotsVisible
                && inspectQueue.Count > 0 && inspectIndex >= 0 && inspectIndex < inspectQueue.Count)
            {
                var source = portraitDebugBeatSourceId ?? "unknown";
                return PortraitDebugOverrides.BuildBeatKey("inspect", source, inspectIndex);
            }

            if (mode == Mode.Talk && talkQueue.Count > 0
                && talkIndex >= 0 && talkIndex < talkQueue.Count)
            {
                var source = portraitDebugBeatSourceId ?? "unknown";
                return PortraitDebugOverrides.BuildBeatKey("talk", source, talkIndex);
            }

            if (mode == Mode.Talk && talkAwaitingClickReturn
                && !string.IsNullOrEmpty(portraitDebugBeatSourceId))
            {
                return PortraitDebugOverrides.BuildSingleBeatKey("talk", portraitDebugBeatSourceId);
            }

            if (mode == Mode.Writing && !string.IsNullOrEmpty(portraitDebugBeatSourceId))
                return PortraitDebugOverrides.BuildSingleBeatKey("writing", portraitDebugBeatSourceId);

            if (mode == Mode.Interview)
            {
                var ic = InterviewController.Instance;
                if (ic == null) return null;
                var subject = ic.Subject == InterviewSubject.Dafu ? "dafu" : "lin";
                return PortraitDebugOverrides.BuildSingleBeatKey("interview",
                    subject + ":" + portraitDebugInterviewRevision);
            }

            if (mode == Mode.Epilogue && epilogueQueue.Count > 0
                && epilogueIndex >= 0 && epilogueIndex < epilogueQueue.Count)
            {
                return PortraitDebugOverrides.BuildBeatKey("epilogue",
                    portraitDebugBeatSourceId ?? "default", epilogueIndex);
            }

            return null;
        }

        string GetPortraitDebugUnavailableHint()
        {
            if (mode == Mode.Investigate && investigateHotspotsVisible)
                return ToolLang.T("当前在调查地图，请先点击场景物件进入对话。",
                    "You're on the investigation map. Click an object to start a conversation first.");
            if (mode == Mode.Talk && talkQueue.Count == 0 && !talkAwaitingClickReturn)
                return ToolLang.T("当前在话题菜单，请先选择一个话题。",
                    "You're in the topic menu. Pick a topic first.");
            if (mode == Mode.Title || mode == Mode.Menu || mode == Mode.Backlog || mode == Mode.Notebook)
                return ToolLang.T("请在有角色立绘的对话中使用（F9 测试跳转）。",
                    "Use this during a dialogue with a character portrait (F9 test jump).");

            var lineKey = GetPortraitDebugLineKey();
            if (!string.IsNullOrEmpty(lineKey) && !TryGetCurrentPortraitLine(out _, out _, out _, out _))
                return ToolLang.T("当前句为旁白/系统，无立绘可改。",
                    "This line is narration/system — no portrait to change.");

            return ToolLang.T("当前界面不支持立绘调试。", "Portrait debug isn't available on this screen.");
        }

        bool TryGetCurrentPortraitLine(out string speaker, out LineSpeaker kind,
            out string portraitTag, out string text)
        {
            speaker = null;
            kind = LineSpeaker.Character;
            portraitTag = null;
            text = null;

            if (mode == Mode.Dialogue)
            {
                var line = SceneDirector.Instance?.CurrentDisplayLine;
                if (line == null || line.speaker == LineSpeaker.Narration || line.speaker == LineSpeaker.System)
                    return false;
                speaker = line.speakerName;
                kind = line.speaker;
                portraitTag = line.portrait;
                text = line.text;
                return true;
            }

            if (mode == Mode.Investigate && !investigateHotspotsVisible
                && inspectIndex >= 0 && inspectIndex < inspectQueue.Count)
            {
                var beat = inspectQueue[inspectIndex];
                if (beat.narration || beat.system) return false;
                speaker = string.IsNullOrEmpty(beat.speaker) ? "小凌" : beat.speaker;
                kind = LineSpeaker.Character;
                portraitTag = beat.portrait;
                text = beat.text;
                return true;
            }

            if (mode == Mode.Talk && talkIndex >= 0 && talkIndex < talkQueue.Count)
            {
                var beat = talkQueue[talkIndex];
                if (beat.narration || beat.system) return false;
                speaker = string.IsNullOrEmpty(beat.speakerName) ? "保安叔叔" : beat.speakerName;
                kind = LineSpeaker.Character;
                portraitTag = beat.portrait;
                text = beat.text;
                return true;
            }

            if (mode == Mode.Talk && talkAwaitingClickReturn && activeTalkTopic != null)
            {
                speaker = "保安叔叔";
                kind = LineSpeaker.Character;
                portraitTag = activeTalkTopic.portrait;
                text = typewriterFull;
                return true;
            }

            if (mode == Mode.Writing && !string.IsNullOrEmpty(portraitDebugBeatSourceId))
            {
                speaker = "沈禾";
                kind = LineSpeaker.Character;
                portraitTag = "认真";
                text = typewriterFull;
                return portraitDebugBeatSourceId != "reinterview_menu";
            }

            if (mode == Mode.Interview)
            {
                var ic = InterviewController.Instance;
                if (ic == null) return false;
                speaker = ic.Subject == InterviewSubject.Dafu ? "大福" : "林女士";
                kind = LineSpeaker.Character;
                return true;
            }

            return false;
        }

        void TickPortraitDebugMode()
        {
            if (!portraitDebugPanelVisible)
            {
                if (portraitDebugInputCaptured)
                {
                    portraitDebugInputCaptured = false;
                    RestorePortraitDebugInputBlock();
                }
                return;
            }

            BlockInputForPortraitDebug();

            var lineKey = GetPortraitDebugLineKey();
            if (lineKey != portraitDebugBoundLineKey)
            {
                portraitDebugBoundLineKey = lineKey;
                SyncPortraitDebugContext();
            }
        }

        void BlockInputForPortraitDebug()
        {
            if (!portraitDebugInputCaptured)
            {
                if (advanceCatcher != null)
                {
                    _advanceCatcherRaycastBeforePortraitDebug = advanceCatcher.raycastTarget;
                    _advanceCatcherActiveBeforePortraitDebug = advanceCatcher.gameObject.activeSelf;
                }
                if (dialogueClick != null)
                    _dialogueClickInteractableBeforePortraitDebug = dialogueClick.interactable;
                portraitDebugInputCaptured = true;
            }

            if (advanceCatcher != null)
            {
                advanceCatcher.raycastTarget = false;
                advanceCatcher.gameObject.SetActive(false);
                var btn = advanceCatcher.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
            if (dialogueClick != null)
                dialogueClick.interactable = false;
            if (hideDialogueBtn != null)
                hideDialogueBtn.interactable = false;
        }

        void RestorePortraitDebugInputBlock()
        {
            if (advanceCatcher != null)
            {
                advanceCatcher.raycastTarget = _advanceCatcherRaycastBeforePortraitDebug;
                advanceCatcher.gameObject.SetActive(_advanceCatcherActiveBeforePortraitDebug);
            }
            if (dialogueClick != null)
                dialogueClick.interactable = _dialogueClickInteractableBeforePortraitDebug;
            if (hideDialogueBtn != null)
                hideDialogueBtn.interactable = true;
        }

        void DrawPortraitDebugImGui()
        {
            if (!portraitDebugPanelVisible || !Application.isPlaying) return;

            const float w = 300f;
            float h = Mathf.Min(Screen.height - 24f, 820f);
            var outer = new Rect(Screen.width - w - 12f, 12f, w, h);
            portraitDebugPanelScreenRect = outer;

            GUI.Box(outer, ToolLang.T("立绘调试 (F11)", "Portrait Debug (F11)"));

            var inner = new Rect(outer.x + 8f, outer.y + 22f, outer.width - 16f, outer.height - 30f);
            portraitDebugScroll = GUI.BeginScrollView(inner, portraitDebugScroll,
                new Rect(0f, 0f, inner.width - 22f, 900f));

            GUILayout.BeginArea(new Rect(0f, 0f, inner.width - 24f, 900f));

            var lineKey = GetPortraitDebugLineKey();
            if (string.IsNullOrEmpty(lineKey) || !TryGetCurrentPortraitLine(out _, out _, out _, out _))
            {
                GUILayout.Label(GetPortraitDebugUnavailableHint(),
                    new GUIStyle(GUI.skin.label) { wordWrap = true });
                GUILayout.EndArea();
                GUI.EndScrollView();
                ConsumePortraitDebugPanelMouse();
                return;
            }

            var saved = PortraitDebugOverrides.GetSaved(lineKey);
            var preview = PortraitDebugOverrides.PreviewPortraitKey;
            var hasPreview = PortraitDebugOverrides.HasPreviewForLine(lineKey);

            var confirmedPrefix = ToolLang.T("已确认 · ", "Confirmed · ");
            GUILayout.Label(ToolLang.T("当前句：", "Current line: ") + lineKey, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label(
                hasPreview
                    ? (saved == preview
                        ? confirmedPrefix + PortraitDebugCatalog.LabelForKey(preview)
                        : ToolLang.T("预览中 · ", "Previewing · ") + PortraitDebugCatalog.LabelForKey(preview))
                    : (string.IsNullOrEmpty(saved)
                        ? ToolLang.T("默认立绘", "Default portrait")
                        : confirmedPrefix + PortraitDebugCatalog.LabelForKey(saved)),
                new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUI.enabled = hasPreview && preview != saved;
            if (GUILayout.Button(ToolLang.T("确认本句立绘", "Confirm for this line")))
            {
                PortraitDebugOverrides.ConfirmPreview(lineKey);
                ApplyPortraitDebugToCurrentLine();
            }
            GUI.enabled = true;
            if (GUILayout.Button(ToolLang.T("恢复默认", "Reset to default")))
            {
                PortraitDebugOverrides.ClearSaved(lineKey);
                ReapplyDefaultPortraitForCurrentLine();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label(ToolLang.T("角色", "Character"), new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            // English names are wider; wrap to rows of three so they fit the 300px panel.
            var perRow = ToolLang.English ? 3 : PortraitDebugCatalog.All.Length;
            var buttonWidth = ToolLang.English ? 80f : 48f;
            for (int i = 0; i < PortraitDebugCatalog.All.Length; i++)
            {
                if (i % perRow == 0) GUILayout.BeginHorizontal();
                var g = PortraitDebugCatalog.All[i];
                var style = portraitDebugCharId == g.Id ? GUI.skin.box : GUI.skin.button;
                if (GUILayout.Button(PortraitDebugCatalog.DisplayLabel(g), style, GUILayout.Width(buttonWidth)))
                    portraitDebugCharId = g.Id;
                if (i % perRow == perRow - 1 || i == PortraitDebugCatalog.All.Length - 1) GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            var group = PortraitDebugCatalog.Find(portraitDebugCharId);
            if (group != null)
                DrawPortraitDebugGroup(group.Value, lineKey, saved, preview);

            GUILayout.EndArea();
            GUI.EndScrollView();
            ConsumePortraitDebugPanelMouse();
        }

        void ConsumePortraitDebugPanelMouse()
        {
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp)
            {
                if (portraitDebugPanelScreenRect.Contains(Event.current.mousePosition))
                    Event.current.Use();
            }
        }

        void DrawPortraitDebugGroup(PortraitDebugCatalog.Group g, string lineKey, string saved, string preview)
        {
            const int cols = 2;
            for (int i = 0; i < g.Keys.Length; i++)
            {
                if (i % cols == 0)
                    GUILayout.BeginHorizontal();

                var key = g.Keys[i];
                var label = PortraitDebugCatalog.LabelForKey(key);
                var picked = preview == key || (string.IsNullOrEmpty(preview) && saved == key);
                var style = picked ? GUI.skin.box : GUI.skin.button;
                if (GUILayout.Button(label, style, GUILayout.MinWidth(120f)))
                {
                    PortraitDebugOverrides.SetPreview(lineKey, key);
                    PreviewPortraitOnScreen(key);
                }

                if (i % cols == cols - 1 || i == g.Keys.Length - 1)
                    GUILayout.EndHorizontal();
            }
        }

        void PreviewPortraitOnScreen(string portraitKey)
        {
            if (string.IsNullOrEmpty(portraitKey) || mode == Mode.Title) return;
            if (mode == Mode.Interview)
            {
                SetInterviewLeftPortrait(portraitKey);
                return;
            }
            SetPortrait(portraitKey);
            if (portraitImage != null && portraitImage.enabled)
                portraitImage.color = Color.white;
        }

        void ApplyPortraitDebugToCurrentLine()
        {
            if (!TryGetCurrentPortraitLine(out var speaker, out var kind, out var portraitTag, out var text))
                return;
            var key = PortraitDebugOverrides.GetSaved(GetPortraitDebugLineKey());
            if (string.IsNullOrEmpty(key)) return;

            if (mode == Mode.Interview)
            {
                SetInterviewLeftPortrait(key);
                return;
            }

            SetSpeaker(speaker, kind, portraitTag, text);
        }

        void ReapplyDefaultPortraitForCurrentLine()
        {
            if (!TryGetCurrentPortraitLine(out var speaker, out var kind, out var portraitTag, out var text))
            {
                SetPortrait(null);
                if (mode == Mode.Interview)
                    SetInterviewLeftPortrait(null);
                return;
            }

            if (mode == Mode.Interview)
            {
                ApplyInterviewPortraits();
                return;
            }

            ApplyPortrait(speaker, kind, portraitTag, text);
        }

        void NotifyPortraitDebugLineChanged()
        {
            PortraitDebugOverrides.OnLineChanged(GetPortraitDebugLineKey());
        }

        void OnGUI()
        {
            if (!Application.isPlaying) return;
#if UNITY_EDITOR
            DrawPortraitEditImGui();
            DrawSocialEditImGui();
#endif
            DrawPortraitDebugImGui();
        }
    }
}
#endif
