using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.Narrative
{
    public enum LineSpeaker
    {
        Narration,
        Inner,
        Character,
        System
    }

    [Serializable]
    public class ScriptChoice
    {
        public string label;
        public string nextSceneId;
        public string setFlag;
        public string grantIntel;
        public string setObjective;
    }

    [Serializable]
    public class ScriptLine
    {
        public LineSpeaker speaker = LineSpeaker.Character;
        public string speakerName;
        public string text;
        public string setFlag;
        public string grantIntel;
        public string noteLine;
        public string setObjective;
        public string nextSceneId;
        public bool openInvestigation;
        public bool openTalkMenu;
        public List<ScriptChoice> choices = new List<ScriptChoice>();
    }

    [Serializable]
    public class ScriptScene
    {
        public string id;
        public string title;
        public string backgroundLabel;
        public List<ScriptLine> lines = new List<ScriptLine>();
    }

    [Serializable]
    public class ScriptDatabase
    {
        public List<ScriptScene> scenes = new List<ScriptScene>();

        public ScriptScene Get(string id)
        {
            return scenes.Find(s => s.id == id);
        }
    }

    public class SceneDirector : MonoBehaviour
    {
        public static SceneDirector Instance { get; private set; }

        ScriptDatabase db;
        ScriptScene current;
        int index;
        Action<ScriptLine> onLine;
        Action onSceneEnd;
        Action onOpenInvestigation;
        Action onOpenTalkMenu;

        public ScriptScene Current => current;
        public bool HasMore => current != null && index < current.lines.Count;
        public ScriptLine CurrentLine => HasMore ? current.lines[index] : null;

        void Awake()
        {
            Instance = this;
            LoadDatabase();
        }

        void LoadDatabase()
        {
            var asset = Resources.Load<TextAsset>("Chapter1/scripts");
            if (asset != null)
            {
                db = JsonUtility.FromJson<ScriptDatabase>(asset.text);
            }
            if (db == null || db.scenes == null || db.scenes.Count == 0)
            {
                db = BuiltInScripts.Create();
                Debug.LogWarning("[SceneDirector] Using built-in scripts fallback.");
            }
        }

        public void Bind(Action<ScriptLine> lineHandler, Action sceneEnd, Action openInvest, Action openTalk)
        {
            onLine = lineHandler;
            onSceneEnd = sceneEnd;
            onOpenInvestigation = openInvest;
            onOpenTalkMenu = openTalk;
        }

        public void PlayScene(string sceneId)
        {
            current = db.Get(sceneId);
            index = 0;
            if (current == null)
            {
                Debug.LogError($"[SceneDirector] Missing scene {sceneId}");
                return;
            }
            ShowCurrent();
        }

        public void Advance()
        {
            if (current == null)
                return;

            var line = CurrentLine;
            if (line != null && line.choices != null && line.choices.Count > 0)
                return; // wait for choice

            string jump = line != null ? line.nextSceneId : null;
            ApplyLineEffects(line);
            if (!string.IsNullOrEmpty(jump))
                return;

            index++;
            if (!HasMore)
            {
                onSceneEnd?.Invoke();
                return;
            }
            ShowCurrent();
        }

        /// <summary>
        /// Fast-forward through linear dialogue until a choice, investigation/talk gate,
        /// scene jump, or end of scene. Intermediate lines still apply flags/intel and
        /// invoke <paramref name="onSkippedIntermediate"/> for history.
        /// </summary>
        public void SkipToBreak(Action<ScriptLine> onSkippedIntermediate = null)
        {
            if (current == null)
                return;

            var line = CurrentLine;
            if (IsBreakLine(line))
                return;

            bool first = true;
            while (HasMore)
            {
                line = CurrentLine;
                if (line == null)
                    break;

                if (IsBreakLine(line))
                {
                    ShowCurrent();
                    return;
                }

                if (!first)
                    onSkippedIntermediate?.Invoke(line);
                first = false;

                string jump = line.nextSceneId;
                ApplyLineEffects(line);
                if (!string.IsNullOrEmpty(jump))
                    return;

                index++;
                if (!HasMore)
                {
                    onSceneEnd?.Invoke();
                    return;
                }
            }
        }

        public static bool IsBreakLine(ScriptLine line)
        {
            if (line == null) return true;
            if (line.choices != null && line.choices.Count > 0) return true;
            if (line.openInvestigation || line.openTalkMenu) return true;
            return false;
        }

        public void Choose(int choiceIndex)
        {
            var line = CurrentLine;
            if (line == null || line.choices == null || choiceIndex < 0 || choiceIndex >= line.choices.Count)
                return;

            var c = line.choices[choiceIndex];
            ApplyChoice(c);
            if (!string.IsNullOrEmpty(c.nextSceneId))
            {
                Core.ChapterFlowController.Instance.GoToScene(c.nextSceneId);
                return;
            }

            index++;
            if (!HasMore)
            {
                onSceneEnd?.Invoke();
                return;
            }
            ShowCurrent();
        }

        void ShowCurrent()
        {
            var line = CurrentLine;
            if (line == null)
                return;

            onLine?.Invoke(line);

            // Open gameplay panels after dialogue bind so their buttons are not overwritten.
            if (line.openInvestigation)
                onOpenInvestigation?.Invoke();
            if (line.openTalkMenu)
                onOpenTalkMenu?.Invoke();
        }

        void ApplyLineEffects(ScriptLine line)
        {
            if (line == null) return;
            var gs = Core.GameState.Instance;
            if (!string.IsNullOrEmpty(line.setFlag))
                gs.SetFlag(line.setFlag);
            if (!string.IsNullOrEmpty(line.grantIntel))
                gs.GrantIntel(line.grantIntel, line.noteLine);
            if (!string.IsNullOrEmpty(line.setObjective))
                gs.SetObjective(line.setObjective);
            if (!string.IsNullOrEmpty(line.nextSceneId))
                Core.ChapterFlowController.Instance.GoToScene(line.nextSceneId);
        }

        void ApplyChoice(ScriptChoice c)
        {
            var gs = Core.GameState.Instance;
            if (!string.IsNullOrEmpty(c.setFlag))
                gs.SetFlag(c.setFlag);
            if (!string.IsNullOrEmpty(c.grantIntel))
                gs.GrantIntel(c.grantIntel);
            if (!string.IsNullOrEmpty(c.setObjective))
                gs.SetObjective(c.setObjective);
        }
    }
}
