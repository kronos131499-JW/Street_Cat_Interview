using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StreetCat.Loc;
using StreetCat.Narrative;
using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>
    /// Follows the current script line in Play Mode and writes text back
    /// to the active language only.
    /// </summary>
    public sealed class DialogueTextEditorWindow : EditorWindow
    {
        const double SaveDelaySeconds = 0.35;

        [Serializable]
        class OverrideFile
        {
            public List<OverrideLine> lines = new List<OverrideLine>();
        }

        [Serializable]
        class OverrideLine
        {
            public string key;
            public string text;
            public string speakerName;
            public string[] choices;
        }

        string boundKey;
        string boundSceneId;
        int boundLineIndex = -1;
        bool boundEnglish;
        string editText = "";
        string editSpeaker = "";
        string[] editChoices = Array.Empty<string>();
        string committedText = "";
        string committedSpeaker = "";
        string[] committedChoices = Array.Empty<string>();
        string referenceText = "";
        string referenceSpeaker = "";
        bool editable;
        bool hasOverride;
        bool pendingSave;
        double saveAt;
        string status = "";
        Vector2 scroll;

        [MenuItem("街角专访/对白文本编辑器", priority = 8)]
        [MenuItem("StreetCat/Dialogue Text Editor", priority = 8)]
        public static void Open()
        {
            var window = GetWindow<DialogueTextEditorWindow>("对白文本");
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            FlushPending();
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnEditorUpdate()
        {
            if (!pendingSave) return;
            if (EditorApplication.timeSinceStartup < saveAt) return;
            FlushPending();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("对白文本编辑器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play 时会跟着当前这一句，包括槐安社区调查，以及和保安的交谈。改完会自动保存，而且只写入当前游戏语言：英文进 scripts_overrides_en.json，中文进 scripts_overrides_zh.json。",
                MessageType.Info);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode，并走到要改的那句对白或调查文本。窗口会自动换到这一句。下方可以改对话框的宽和高。", MessageType.None);
                DrawDialogueFrameSize();
                return;
            }

            bool english = GameSettings.IsEnglish;
            var ui = GameUI.Instance;
            string sentenceLabel = null;
            if (ui != null && ui.TryGetFreeEditorLine(out var caption, out var freeKey, out var freeText, out var freeSpeaker, out var freeChoices))
            {
                sentenceLabel = caption;
                if (freeKey != boundKey || english != boundEnglish)
                {
                    FlushPending();
                    BindFree(freeKey, freeText, freeSpeaker, english, freeChoices);
                }
            }
            else
            {
                var director = SceneDirector.Instance;
                var line = director != null ? director.CurrentLine : null;
                var scene = director != null ? director.Current : null;
                if (director == null || scene == null || line == null)
                {
                    EditorGUILayout.HelpBox("还没有正在播放的对白。在剧本里往下走，点开调查物件，或和保安交谈。", MessageType.None);
                    DrawDialogueFrameSize();
                    return;
                }

                var key = scene.id + ":" + director.LineIndex;
                sentenceLabel = scene.id + "  第 " + director.LineIndex + " 句";
                if (key != boundKey || english != boundEnglish)
                {
                    FlushPending();
                    Bind(scene.id, director.LineIndex, line, english);
                }
            }

            DrawHeader(sentenceLabel, english);
            DrawDialogueFrameSize();
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (!editable)
            {
                EditorGUILayout.HelpBox("这一句是演出指令或没有玩家能看到的正文，不能改成对白。", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("说话人（当前语言）");
            editSpeaker = EditorGUILayout.TextField(editSpeaker);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("正文（当前语言）");
            editText = EditorGUILayout.TextArea(editText, GUILayout.MinHeight(120f));

            if (editChoices.Length > 0)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("选项（当前语言）");
                for (int i = 0; i < editChoices.Length; i++)
                    editChoices[i] = EditorGUILayout.TextField("选项 " + (i + 1), editChoices[i] ?? "");
            }

            if (EditorGUI.EndChangeCheck() && !SameAsCommitted())
            {
                pendingSave = true;
                saveAt = EditorApplication.timeSinceStartup + SaveDelaySeconds;
                status = english ? "即将保存到英文…" : "即将保存到中文…";
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(english ? "中文对照（不会被这次修改写到）" : "英文对照（不会被这次修改写到）");
                EditorGUILayout.TextField("说话人", referenceSpeaker ?? "");
                EditorGUILayout.TextArea(referenceText ?? "", GUILayout.MinHeight(72f));
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(!CanDeleteBoundLine()))
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
                if (GUILayout.Button("删除这句台词"))
                    DeleteCurrentLine();
                GUI.backgroundColor = old;
            }

            EditorGUILayout.Space(6f);
            if (!string.IsNullOrEmpty(status))
                EditorGUILayout.HelpBox(status, MessageType.None);

            using (new EditorGUI.DisabledScope(!hasOverride && !pendingSave))
            {
                if (GUILayout.Button(english ? "恢复这句英文到原译" : "恢复这句中文到原文"))
                    RevertCurrent();
            }
        }

        bool CanDeleteBoundLine()
        {
            return editable && !string.IsNullOrEmpty(boundSceneId) && boundLineIndex >= 0;
        }

        void DeleteCurrentLine()
        {
            if (!CanDeleteBoundLine()) return;
            FlushPending();
            var label = boundSceneId + " 第 " + boundLineIndex + " 句";
            if (!EditorUtility.DisplayDialog(
                    "删除台词",
                    "删除「" + label + "」？这一句中文和英文都不会再出现。带选项或跳转的句子删掉后，那些选项也不会再出现。",
                    "删除",
                    "取消"))
                return;

            if (!WriteDeletedKey(boundSceneId, boundLineIndex))
            {
                status = "删除失败，没有写入文件。";
                return;
            }

            ScriptLoc.SetLineDeleted(boundSceneId, boundLineIndex, true);
            var director = SceneDirector.Instance;
            if (director != null && director.Current != null && director.Current.id == boundSceneId)
                director.ContinuePastDeleted();
            status = "已删除 " + label + "。中英文都不会再播放这句。";
        }

        static bool WriteDeletedKey(string sceneId, int lineIndex)
        {
            var fullPath = Path.Combine(Application.dataPath, "Resources", "Loc", "scripts_deleted.json");
            DeletedStore file = null;
            if (File.Exists(fullPath))
            {
                try
                {
                    file = JsonUtility.FromJson<DeletedStore>(File.ReadAllText(fullPath));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[DialogueText] scripts_deleted.json unreadable: " + ex.Message);
                }
            }
            if (file == null) file = new DeletedStore();
            if (file.keys == null) file.keys = new List<string>();
            var key = sceneId + ":" + lineIndex;
            if (!file.keys.Contains(key))
                file.keys.Add(key);
            try
            {
                File.WriteAllText(fullPath, JsonUtility.ToJson(file, true));
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DialogueText] could not write scripts_deleted.json: " + ex.Message);
                return false;
            }
        }

        [Serializable]
        class DeletedStore
        {
            public List<string> keys = new List<string>();
        }

        void DrawHeader(string sentenceLabel, bool english)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("句子", sentenceLabel ?? "");
            var lang = english ? "英文 English" : "中文";
            var file = english ? "scripts_overrides_en.json" : "scripts_overrides_zh.json";
            EditorGUILayout.LabelField("正在编辑", lang + (hasOverride ? "（已有单独修改）" : ""));
            EditorGUILayout.LabelField("保存到", file);
            EditorGUILayout.Space(6f);
        }

        void DrawDialogueFrameSize()
        {
            if (!Application.isPlaying) return;
            var ui = GameUI.Instance;
            if (ui == null || !ui.TryGetDialogueFrame(out var panel) || !panel.gameObject.activeInHierarchy)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("对话框大小", EditorStyles.boldLabel);
            var size = panel.rect.size;
            EditorGUI.BeginChangeCheck();
            var width = EditorGUILayout.Slider("宽度", size.x, 480f, 1900f);
            var height = EditorGUILayout.Slider("高度", size.y, 100f, 520f);
            if (!EditorGUI.EndChangeCheck()) return;

            ui.SetDialogueFrameSize(width, height);
            var canvas = panel.GetComponentInParent<Canvas>();
            var root = canvas != null && canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            if (root != null && UILayoutOverrides.Save(root, panel))
                status = "对话框大小已保存。";
        }

        void BindFree(string key, string baseText, string baseSpeaker, bool english, string[] baseChoices = null)
        {
            var snap = ScriptLoc.DescribeFree(key, baseText, baseSpeaker, english, baseChoices);
            boundKey = key;
            boundSceneId = null;
            boundLineIndex = -1;
            boundEnglish = english;
            pendingSave = false;
            if (snap == null)
            {
                editable = false;
                return;
            }

            editable = snap.editable;
            hasOverride = snap.hasUserOverride;
            editText = snap.text ?? "";
            editSpeaker = snap.speakerName ?? "";
            editChoices = snap.choiceLabels ?? Array.Empty<string>();
            referenceText = snap.referenceText ?? "";
            referenceSpeaker = snap.referenceSpeaker ?? "";
            CommitFields();
            status = "";
        }

        void Bind(string sceneId, int lineIndex, ScriptLine line, bool english)
        {
            var snap = ScriptLoc.Describe(sceneId, lineIndex, line, english);
            boundKey = sceneId + ":" + lineIndex;
            boundSceneId = sceneId;
            boundLineIndex = lineIndex;
            boundEnglish = english;
            pendingSave = false;
            if (snap == null)
            {
                editable = false;
                return;
            }

            editable = snap.editable;
            hasOverride = snap.hasUserOverride;
            editText = snap.text ?? "";
            editSpeaker = snap.speakerName ?? "";
            editChoices = snap.choiceLabels ?? Array.Empty<string>();
            referenceText = snap.referenceText ?? "";
            referenceSpeaker = snap.referenceSpeaker ?? "";
            CommitFields();
            status = "";
        }

        void FlushPending()
        {
            if (!pendingSave || string.IsNullOrEmpty(boundKey) || !editable)
            {
                pendingSave = false;
                return;
            }

            pendingSave = false;
            if (SameAsCommitted())
                return;

            if (!WriteOverride(boundEnglish, boundKey, editText, editSpeaker, editChoices, remove: false))
            {
                status = "保存失败，没有写入任何语言文件。";
                return;
            }

            ScriptLoc.SetUserOverride(boundEnglish, boundKey, editText, editSpeaker, editChoices);
            hasOverride = true;
            CommitFields();
            status = boundEnglish
                ? "已保存到英文。中文原文没有改。"
                : "已保存到中文。英文文本没有改。";
            RefreshPlayLine();
        }

        void RevertCurrent()
        {
            if (string.IsNullOrEmpty(boundKey)) return;
            pendingSave = false;
            if (!WriteOverride(boundEnglish, boundKey, "", "", Array.Empty<string>(), remove: true))
            {
                status = "恢复失败，文件没有改。";
                return;
            }
            ScriptLoc.ClearUserOverride(boundEnglish, boundKey);
            var ui = GameUI.Instance;
            if (ui != null && ui.TryGetFreeEditorLine(out _, out var freeKey, out var freeText, out var freeSpeaker, out var freeChoices)
                && freeKey == boundKey)
                BindFree(freeKey, freeText, freeSpeaker, GameSettings.IsEnglish, freeChoices);
            else if (ui != null && ui.TryGetInvestigateEditorLine(out var invKey, out var invText, out var invSpeaker)
                && invKey == boundKey)
                BindFree(invKey, invText, invSpeaker, GameSettings.IsEnglish);
            else
            {
                var director = SceneDirector.Instance;
                var line = director != null ? director.CurrentLine : null;
                var scene = director != null ? director.Current : null;
                if (scene != null && line != null)
                    Bind(scene.id, director.LineIndex, line, GameSettings.IsEnglish);
            }
            status = boundEnglish
                ? "这句英文已恢复成原译。"
                : "这句中文已恢复成原文。";
            RefreshPlayLine();
        }

        static void RefreshPlayLine()
        {
            if (!Application.isPlaying) return;
            var ui = GameUI.Instance;
            if (ui != null)
                ui.RefreshLocalizedDialogueLine();
        }

        void CommitFields()
        {
            committedText = editText ?? "";
            committedSpeaker = editSpeaker ?? "";
            committedChoices = Copy(editChoices);
        }

        bool SameAsCommitted()
        {
            if ((editText ?? "") != committedText) return false;
            if ((editSpeaker ?? "") != committedSpeaker) return false;
            if (editChoices == null && committedChoices == null) return true;
            if (editChoices == null || committedChoices == null) return false;
            if (editChoices.Length != committedChoices.Length) return false;
            for (int i = 0; i < editChoices.Length; i++)
            {
                if ((editChoices[i] ?? "") != (committedChoices[i] ?? ""))
                    return false;
            }
            return true;
        }

        static string[] Copy(string[] values)
        {
            if (values == null || values.Length == 0)
                return Array.Empty<string>();
            var copy = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                copy[i] = values[i] ?? "";
            return copy;
        }

        static bool WriteOverride(bool english, string key, string text, string speaker, string[] choices, bool remove)
        {
            var assetPath = english
                ? "Assets/Resources/Loc/scripts_overrides_en.json"
                : "Assets/Resources/Loc/scripts_overrides_zh.json";
            var fullPath = Path.Combine(Application.dataPath, "Resources", "Loc",
                english ? "scripts_overrides_en.json" : "scripts_overrides_zh.json");

            OverrideFile file = null;
            if (File.Exists(fullPath))
            {
                try
                {
                    file = JsonUtility.FromJson<OverrideFile>(File.ReadAllText(fullPath));
                }
                catch (Exception ex)
                {
                    Debug.LogError("[DialogueText] 读取失败，未写入，以免覆盖另一语言：" + ex.Message);
                    return false;
                }
            }

            if (file == null)
                file = new OverrideFile();
            if (file.lines == null)
                file.lines = new List<OverrideLine>();

            file.lines.RemoveAll(line => line != null && line.key == key);
            if (!remove)
            {
                file.lines.Add(new OverrideLine
                {
                    key = key,
                    text = text ?? "",
                    speakerName = speaker ?? "",
                    choices = Copy(choices)
                });
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? fullPath);
            File.WriteAllText(fullPath, JsonUtility.ToJson(file, true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }
    }
}
