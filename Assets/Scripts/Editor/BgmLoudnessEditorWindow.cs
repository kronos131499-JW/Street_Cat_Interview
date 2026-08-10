using System.Collections.Generic;
using System.IO;
using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>
    /// Tune per-track default loudness (multiplier on peak-normalized gain).
    /// Heavy audio peak work is staggered — never LoadAudioData for all clips in OnEnable/OnGUI.
    /// </summary>
    public class BgmLoudnessEditorWindow : EditorWindow
    {
        Vector2 scroll;
        readonly Dictionary<string, float> draft = new Dictionary<string, float>();
        readonly Dictionary<string, float> autoPeakLocal = new Dictionary<string, float>();
        readonly Dictionary<string, int> peakRetries = new Dictionary<string, int>();
        readonly Queue<string> peakQueue = new Queue<string>();
        readonly HashSet<string> peakQueued = new HashSet<string>();
        bool listReady;
        bool analyzingPeaks;
        string analyzingKey;
        double lastRepaint;
        double nextPeakAnalyzeAt;
        const int MaxPeakRetries = 40;
        const double PeakAnalyzeInterval = 0.05;

        static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
        {
            { "bgm_title", "主菜单" },
            { "bgm_editorial_01", "编辑部日常 01" },
            { "bgm_editorial_02", "编辑部日常 02" },
            { "bgm_shenhe_office", "沈禾办公室" },
            { "bgm_community_afternoon", "社区午后" },
            { "bgm_community_dusk", "社区傍晚" },
            { "bgm_community", "社区（旧）" },
            { "bgm_guard_booth", "保安亭" },
            { "bgm_dafu", "大福 / 采访" },
            { "bgm_cafe", "咖啡馆 / 林女士" },
            { "bgm_epilogue", "后日谈" },
            { "bgm_interview", "采访（旧）" },
            { "bgm_writing", "写稿（旧）" },
            { "bgm_magazine", "杂志（旧）" },
        };

        [MenuItem("街角专访/BGM 默认响度")]
        public static void Open()
        {
            var win = GetWindow<BgmLoudnessEditorWindow>("BGM 响度");
            win.minSize = new Vector2(420, 480);
            win.Show();
        }

        void OnEnable()
        {
            listReady = false;
            draft.Clear();
            autoPeakLocal.Clear();
            peakRetries.Clear();
            peakQueue.Clear();
            peakQueued.Clear();
            analyzingPeaks = false;
            analyzingKey = null;
            nextPeakAnalyzeAt = 0;
            EditorApplication.update += OnEditorUpdate;
            // Defer AssetDatabase work off the menu-click stack so the window appears immediately.
            EditorApplication.delayCall += DeferredReload;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.delayCall -= DeferredReload;
        }

        void DeferredReload()
        {
            if (this == null) return;
            ReloadDraft();
            listReady = true;
            Repaint();
        }

        void OnEditorUpdate()
        {
            // At most one peak analyze every PeakAnalyzeInterval (LoadAudioData + GetData is expensive).
            if (Application.isPlaying && BgmController.Instance != null && peakQueue.Count > 0
                && EditorApplication.timeSinceStartup >= nextPeakAnalyzeAt)
            {
                nextPeakAnalyzeAt = EditorApplication.timeSinceStartup + PeakAnalyzeInterval;
                var key = peakQueue.Dequeue();
                peakQueued.Remove(key);
                analyzingPeaks = true;
                analyzingKey = key;
                try
                {
                    BgmController.Instance.GetAutoPeakGain(key);
                    if (BgmController.Instance.TryGetCachedAutoPeakGain(key, out var auto))
                    {
                        autoPeakLocal[key] = auto;
                        peakRetries.Remove(key);
                    }
                    else
                    {
                        peakRetries.TryGetValue(key, out int n);
                        n++;
                        peakRetries[key] = n;
                        if (n >= MaxPeakRetries)
                            autoPeakLocal[key] = 1f;
                        else if (peakQueued.Add(key))
                            peakQueue.Enqueue(key); // still loading — retry later
                    }
                }
                finally
                {
                    analyzingKey = null;
                    analyzingPeaks = peakQueue.Count > 0;
                }
                Repaint();
                return;
            }

            analyzingPeaks = false;

            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - lastRepaint < 0.25) return;
            lastRepaint = EditorApplication.timeSinceStartup;
            Repaint();
        }

        void ReloadDraft()
        {
            draft.Clear();
            autoPeakLocal.Clear();
            peakRetries.Clear();
            peakQueue.Clear();
            peakQueued.Clear();
            var asset = BgmLoudnessData.EnsureAsset();
            var keys = ListBgmKeys();
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                draft[k] = asset != null ? asset.GetMultiplier(k) : BgmLoudnessData.DefaultMultiplier;
            }
        }

        /// <summary>
        /// List BGM keys from asset paths only — never LoadAssetAtPath&lt;AudioClip&gt;
        /// (that can decode / pull large MP3s onto the main thread).
        /// </summary>
        static List<string> ListBgmKeys()
        {
            var list = new List<string>();
            var seen = new HashSet<string>();
            const string folder = "Assets/Resources/Audio/Bgm";
            if (AssetDatabase.IsValidFolder(folder))
            {
                var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(path)) continue;
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrEmpty(name) || name.EndsWith("_SOURCE")) continue;
                    if (!seen.Add(name)) continue;
                    list.Add(name);
                }
            }

            if (list.Count == 0)
            {
                // Fallback: known display names only (still no LoadAll audio).
                foreach (var kv in DisplayNames)
                {
                    if (seen.Add(kv.Key))
                        list.Add(kv.Key);
                }
            }

            list.Sort();
            return list;
        }

        void EnqueuePeakIfNeeded(string key)
        {
            if (!Application.isPlaying || BgmController.Instance == null) return;
            if (autoPeakLocal.ContainsKey(key)) return;
            if (BgmController.Instance.TryGetCachedAutoPeakGain(key, out var cached))
            {
                autoPeakLocal[key] = cached;
                return;
            }
            if (peakQueued.Add(key))
                peakQueue.Enqueue(key);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("BGM · 每曲默认响度", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "在峰值归一化之上再乘一个系数（1.0 = 不额外调整）。\n" +
                "建议 Play 时点「试听」并拖滑条，即时生效；改完点「保存到资源」。\n" +
                "写入 Assets/Resources/BgmLoudness.asset。\n" +
                "峰值分析在后台逐曲进行，打开本窗口不会一次解码全部 BGM。",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = listReady;
            if (GUILayout.Button("刷新列表", GUILayout.Width(90)))
            {
                ReloadDraft();
                Repaint();
            }
            if (GUILayout.Button("全部重置为 1.0", GUILayout.Width(120)))
            {
                var keys = new List<string>(draft.Keys);
                for (int i = 0; i < keys.Count; i++)
                    draft[keys[i]] = BgmLoudnessData.DefaultMultiplier;
                ApplyDraftLive();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("保存到资源", GUILayout.Width(100)))
                SaveDraft();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (!listReady)
            {
                EditorGUILayout.HelpBox("正在读取曲目列表…", MessageType.None);
                return;
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("未在 Play：可先改数值并保存；试听与峰值预估需进入 Play Mode。", MessageType.None);
            else if (BgmController.Instance == null)
                EditorGUILayout.HelpBox("Play 中但找不到 BgmController。", MessageType.Warning);
            else
            {
                if (!string.IsNullOrEmpty(BgmController.Instance.CurrentKey))
                    EditorGUILayout.LabelField("正在播放", BgmController.Instance.CurrentKey);
                if (analyzingPeaks || peakQueue.Count > 0)
                {
                    string tip = string.IsNullOrEmpty(analyzingKey)
                        ? $"峰值分析排队中（剩余 {peakQueue.Count}）…"
                        : $"正在分析峰值：{analyzingKey}（剩余 {peakQueue.Count}）…";
                    EditorGUILayout.HelpBox(tip, MessageType.None);
                }
            }

            EditorGUILayout.Space(8);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            var ordered = new List<string>(draft.Keys);
            ordered.Sort();
            bool changed = false;
            for (int i = 0; i < ordered.Count; i++)
            {
                var key = ordered[i];
                string label = DisplayNames.TryGetValue(key, out var dn) ? dn + "  (" + key + ")" : key;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EnqueuePeakIfNeeded(key);
                float auto = 1f;
                bool hasAuto = autoPeakLocal.TryGetValue(key, out auto);

                EditorGUI.BeginChangeCheck();
                float mul = EditorGUILayout.Slider("响度倍率", draft[key],
                    BgmLoudnessData.MinMultiplier, BgmLoudnessData.MaxMultiplier);
                if (EditorGUI.EndChangeCheck())
                {
                    draft[key] = mul;
                    changed = true;
                }

                if (!Application.isPlaying)
                {
                    EditorGUILayout.LabelField("（Play 后显示峰值增益预估）", EditorStyles.miniLabel);
                }
                else if (hasAuto)
                {
                    float effective = auto * draft[key];
                    EditorGUILayout.LabelField(
                        $"峰值增益 ≈ {auto:F2}  →  实际 ≈ {effective:F2}",
                        EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("峰值增益：排队分析中…", EditorStyles.miniLabel);
                }

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = Application.isPlaying && BgmController.Instance != null;
                if (GUILayout.Button("试听", GUILayout.Width(64)))
                {
                    BgmController.Instance.ClearScriptSticky();
                    ForcePlay(key);
                }
                if (GUILayout.Button("停止", GUILayout.Width(64)))
                {
                    BgmController.Instance.ClearScriptSticky();
                    BgmController.Instance.StopAll();
                }
                GUI.enabled = true;
                if (GUILayout.Button("重置", GUILayout.Width(64)))
                {
                    draft[key] = BgmLoudnessData.DefaultMultiplier;
                    changed = true;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            if (changed)
                ApplyDraftLive();
        }

        static void ForcePlay(string key)
        {
            var bgm = BgmController.Instance;
            if (bgm == null) return;
            // Play() early-outs when key == currentKey; nudge by stopping first.
            if (bgm.CurrentKey == key)
                bgm.StopAll();
            bgm.InvalidateGainCache();
            bgm.Play(key);
        }

        void ApplyDraftLive()
        {
            var asset = BgmLoudnessData.EnsureAsset();
            if (asset == null) return;
            foreach (var kv in draft)
                asset.SetMultiplier(kv.Key, kv.Value);
            // Don't SaveAssets on every slider tick — only mark dirty + live apply.
            EditorUtility.SetDirty(asset);
            BgmLoudnessData.InvalidateCache();
            if (Application.isPlaying && BgmController.Instance != null)
            {
                // Clears final gain only; peak cache stays so we don't re-decode audio.
                BgmController.Instance.InvalidateGainCache();
                BgmController.Instance.RefreshCurrentLoudness();
            }
        }

        void SaveDraft()
        {
            var asset = BgmLoudnessData.EnsureAsset();
            if (asset == null) return;
            foreach (var kv in draft)
                asset.SetMultiplier(kv.Key, kv.Value);
            asset.EditorSave();
            if (Application.isPlaying && BgmController.Instance != null)
            {
                BgmController.Instance.InvalidateGainCache();
                BgmController.Instance.RefreshCurrentLoudness();
            }
            Debug.Log("[BgmLoudness] saved Resources/BgmLoudness.asset (" + draft.Count + " tracks)");
            EditorUtility.DisplayDialog("BGM 响度", "已保存到 Assets/Resources/BgmLoudness.asset", "OK");
        }
    }
}
