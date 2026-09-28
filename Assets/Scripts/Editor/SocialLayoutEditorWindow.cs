#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public class SocialLayoutEditorWindow : EditorWindow
    {
        [MenuItem("街角专访/社交帖子布局编辑器")]
        public static void Open()
        {
            var win = GetWindow<SocialLayoutEditorWindow>("社交帖子布局");
            win.minSize = new Vector2(360f, 380f);
        }

        [MenuItem("街角专访/切换社交帖子编辑模式")]
        public static void ToggleEditMode()
        {
            StreetCat.UI.SocialEditMode.Enabled = !StreetCat.UI.SocialEditMode.Enabled;
            Debug.Log(StreetCat.UI.SocialEditMode.Enabled
                ? "[社交] 编辑模式 ON — Play 后进 SC-03 看帖，或点预览；Game 视图拖青色框调位置/大小。"
                : "[社交] 编辑模式 OFF");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("社交帖子 · Game 视图编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "1. 勾选「启用编辑模式」\n" +
                "2. Play → 进到 SC-03 社交帖子（或点「预览帖子」）\n" +
                "3. Game 视图拖青色框：中间平移，四角缩放\n" +
                "4. 松手自动保存到 Resources/SocialLayout.asset\n" +
                "5. F8 可隐藏左侧滑条面板",
                MessageType.Info);

            EditorGUILayout.Space(8);
            var edit = StreetCat.UI.SocialEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft("启用 Game 视图编辑模式", edit);
            if (next != edit)
                StreetCat.UI.SocialEditMode.Enabled = next;

            EditorGUILayout.Space(8);
            if (GUILayout.Button("创建 / 选中 Layout 资源"))
            {
                var asset = StreetCat.UI.SocialLayout.EnsureAsset();
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            if (GUILayout.Button("恢复默认尺寸"))
            {
                if (EditorUtility.DisplayDialog("重置社交布局",
                        "用默认 520×854 覆盖 SocialLayout.asset？", "重置", "取消"))
                {
                    StreetCat.UI.SocialLayout.ResetToDefaults();
                    if (Application.isPlaying && StreetCat.UI.GameUI.Instance != null)
                        StreetCat.UI.GameUI.Instance.RefreshSocialLayoutFromAsset();
                }
            }

            EditorGUILayout.Space(8);
            var data = StreetCat.UI.SocialLayout.Asset ?? StreetCat.UI.SocialLayout.EnsureAsset();
            if (data == null) return;

            EditorGUILayout.LabelField("当前参数", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            data.width = EditorGUILayout.Slider("宽度", data.width, 220f, 1100f);
            data.height = EditorGUILayout.Slider("高度", data.height, 360f, 1600f);
            data.anchorX = EditorGUILayout.Slider("水平位置", data.anchorX, 0.15f, 0.85f);
            data.anchorY = EditorGUILayout.Slider("垂直位置", data.anchorY, 0.25f, 0.95f);
            data.detailScale = EditorGUILayout.Slider("详情放大", data.detailScale, 0.85f, 1.4f);
            if (EditorGUI.EndChangeCheck())
            {
                data.Clamp();
                EditorUtility.SetDirty(data);
                if (Application.isPlaying && StreetCat.UI.GameUI.Instance != null)
                    StreetCat.UI.GameUI.Instance.RefreshSocialLayoutFromAsset();
            }

            if (GUILayout.Button("保存到 asset"))
                StreetCat.UI.SocialLayout.SaveCurrent();

            DrawSocialSaveStatus();

            if (Application.isPlaying && StreetCat.UI.GameUI.Instance != null)
            {
                EditorGUILayout.Space(6);
                if (GUILayout.Button("预览帖子 1（SC-03 素材）"))
                    StreetCat.UI.GameUI.Instance.DebugPreviewSocialPost("social_post_01_feed");
                EditorGUILayout.HelpBox(
                    StreetCat.UI.SocialEditMode.Enabled
                        ? "Play 中 · 编辑已开。Game 视图拖青色框；F8=滑条面板。"
                        : "Play 中 · 请勾选上方「启用编辑模式」。",
                    MessageType.None);
            }
        }

        static void DrawSocialSaveStatus()
        {
            var msg = StreetCat.UI.SocialLayout.LastSaveMessage;
            if (string.IsNullOrEmpty(msg))
            {
                EditorGUILayout.HelpBox(
                    "尚未保存 — 拖动松手或点「保存到 asset」后这里显示路径与时间。",
                    MessageType.None);
                return;
            }
            var ok = StreetCat.UI.SocialLayout.LastSaveOk;
            EditorGUILayout.HelpBox((ok ? "✓ " : "✗ ") + msg, ok ? MessageType.Info : MessageType.Error);
        }

        void OnInspectorUpdate() => Repaint();
    }
}
#endif
