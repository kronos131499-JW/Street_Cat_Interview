using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public class PortraitLayoutEditorWindow : EditorWindow
    {
        [MenuItem("街角专访/立绘布局编辑器")]
        public static void Open()
        {
            var win = GetWindow<PortraitLayoutEditorWindow>("立绘布局");
            win.minSize = new Vector2(340, 360);
            win.Show();
        }

        [MenuItem("街角专访/切换立绘编辑模式")]
        public static void ToggleEditMode()
        {
            PortraitEditMode.Enabled = !PortraitEditMode.Enabled;
            Debug.Log(PortraitEditMode.Enabled
                ? "[立绘] 编辑模式 ON — Play 后 F9 跳到 SC-02，F10 开关滑条面板；Game 视图拖青色框。"
                : "[立绘] 编辑模式 OFF");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("VN 立绘 · Game 视图编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "1. 勾选「启用编辑模式」\n" +
                "2. Play → F9 测试跳转 → SC-02 喵语翻译器（有沈禾立绘）\n" +
                "3. Game 视图：拖青色框（中间移动，四角缩放）\n" +
                "4. 左侧滑条面板：F10 显示/隐藏；改完点「保存」\n" +
                "5. F11 立绘调试：按角色选表情/立绘\n" +
                "6. 写入 Resources/PortraitLayout.asset",
                MessageType.Info);

            EditorGUILayout.Space(8);
            var edit = PortraitEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft("启用 Game 视图编辑模式", edit);
            if (next != edit)
                PortraitEditMode.Enabled = next;

            EditorGUILayout.Space(8);
            if (GUILayout.Button("创建 / 选中 Layout 资源"))
            {
                var asset = PortraitLayout.EnsureAsset();
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            if (GUILayout.Button("从 VnTheme 默认值重置"))
            {
                if (EditorUtility.DisplayDialog("重置立绘布局",
                        "用代码默认参数覆盖 PortraitLayout.asset？", "重置", "取消"))
                {
                    PortraitLayout.ResetToThemeDefaults();
                    if (Application.isPlaying && GameUI.Instance != null)
                        GameUI.Instance.RefreshPortraitLayoutFromAsset();
                }
            }

            EditorGUILayout.Space(8);
            var data = PortraitLayout.Asset ?? PortraitLayout.EnsureAsset();
            if (data == null) return;

            float minBottom = VnTheme.PortraitSlotBottomMin;
            float minTop = minBottom + PortraitLayout.MinSlotHeight;

            EditorGUILayout.LabelField("当前参数", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            data.slotLeft = EditorGUILayout.Slider("左边界", data.slotLeft, 0.35f, 0.95f);
            data.slotRight = EditorGUILayout.Slider("右边界", data.slotRight, 0.45f, 1f);
            data.slotTop = EditorGUILayout.Slider("顶边界（↓变小）", data.slotTop, minTop, 1f);
            data.slotBottom = EditorGUILayout.Slider("底边界", data.slotBottom, minBottom, 0.85f);
            data.heightScale = EditorGUILayout.Slider("缩放", data.heightScale, 0.45f, 1.8f);
            data.centerBias = EditorGUILayout.Slider("水平位置", data.centerBias, 0f, 1f);
            data.offsetY = EditorGUILayout.Slider("垂直微调（负=下移）", data.offsetY, -0.35f, 0.35f);
            if (EditorGUI.EndChangeCheck())
            {
                data.Clamp();
                EditorUtility.SetDirty(data);
                if (Application.isPlaying && GameUI.Instance != null)
                    GameUI.Instance.RefreshPortraitLayoutFromAsset();
            }

            if (GUILayout.Button("保存到 asset"))
                PortraitLayout.SaveCurrent();

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    PortraitEditMode.Enabled
                        ? "Play 中 · 编辑已开。F10=滑条面板，Game 视图拖青色框。"
                        : "Play 中 · 请勾选上方「启用编辑模式」。",
                    MessageType.None);
            }
        }

        void OnInspectorUpdate() => Repaint();
    }
}
