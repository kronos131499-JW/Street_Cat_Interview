using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public class PortraitLayoutEditorWindow : EditorWindow
    {
        static string T(string zh, string en) => ToolLang.T(zh, en);

        [MenuItem("街角专访/立绘布局编辑器")]
        public static void Open()
        {
            var win = GetWindow<PortraitLayoutEditorWindow>(T("立绘布局", "Portrait Layout"));
            win.minSize = new Vector2(340, 360);
            win.Show();
        }

        [MenuItem("街角专访/切换立绘编辑模式")]
        [MenuItem("StreetCat/Toggle Portrait Edit Mode")]
        public static void ToggleEditMode()
        {
            PortraitEditMode.Enabled = !PortraitEditMode.Enabled;
            Debug.Log(PortraitEditMode.Enabled
                ? T("[立绘] 编辑模式 ON — Play 后 F9 跳到 SC-02，F10 开关滑条面板；Game 视图拖青色框。",
                    "[Portrait] Edit mode ON — in Play Mode press F9 to jump to SC-02, F10 toggles the slider panel; drag the cyan box in the Game view.")
                : T("[立绘] 编辑模式 OFF", "[Portrait] Edit mode OFF"));
        }

        void OnGUI()
        {
            titleContent.text = T("立绘布局", "Portrait Layout");
            ToolLang.DrawToggle();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(T("VN 立绘 · Game 视图编辑", "VN Portrait · Edit in Game view"), EditorStyles.boldLabel);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(T(
                "1. 勾选「启用编辑模式」\n" +
                "2. Play → F9 测试跳转 → SC-02 喵语翻译器（有沈禾立绘）\n" +
                "3. Game 视图：拖青色框（中间移动，四角缩放）\n" +
                "4. 左侧滑条面板：F10 显示/隐藏；改完点「保存」\n" +
                "5. F11 立绘调试：按角色选表情/立绘\n" +
                "6. 写入 Resources/PortraitLayout.asset",
                "1. Tick \"Enable Game-view edit mode\"\n" +
                "2. Play → F9 test jump → SC-02 Meow Translator (has Shen He's portrait)\n" +
                "3. Game view: drag the cyan box (middle moves, corners resize)\n" +
                "4. Left slider panel: F10 shows/hides it; click \"Save\" when done\n" +
                "5. F11 portrait debug: pick a character's expression per line\n" +
                "6. Saved to Resources/PortraitLayout.asset"),
                MessageType.Info);

            EditorGUILayout.Space(8);
            var edit = PortraitEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft(T("启用 Game 视图编辑模式", "Enable Game-view edit mode"), edit);
            if (next != edit)
                PortraitEditMode.Enabled = next;

            EditorGUILayout.Space(8);
            if (GUILayout.Button(T("创建 / 选中 Layout 资源", "Create / select layout asset")))
            {
                var asset = PortraitLayout.EnsureAsset();
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            if (GUILayout.Button(T("从 VnTheme 默认值重置", "Reset to VnTheme defaults")))
            {
                if (EditorUtility.DisplayDialog(T("重置立绘布局", "Reset portrait layout"),
                        T("用代码默认参数覆盖 PortraitLayout.asset？",
                          "Overwrite PortraitLayout.asset with the built-in default values?"),
                        T("重置", "Reset"), T("取消", "Cancel")))
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

            EditorGUILayout.LabelField(T("当前参数", "Current values"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            data.slotLeft = EditorGUILayout.Slider(T("左边界", "Slot left"), data.slotLeft, 0.35f, 0.95f);
            data.slotRight = EditorGUILayout.Slider(T("右边界", "Slot right"), data.slotRight, 0.45f, 1f);
            data.slotTop = EditorGUILayout.Slider(T("顶边界（↓变小）", "Slot top (lower = smaller)"), data.slotTop, minTop, 1f);
            data.slotBottom = EditorGUILayout.Slider(T("底边界", "Slot bottom"), data.slotBottom, minBottom, 0.85f);
            data.heightScale = EditorGUILayout.Slider(T("缩放", "Scale"), data.heightScale, 0.45f, 1.8f);
            data.centerBias = EditorGUILayout.Slider(T("水平位置", "Horizontal position"), data.centerBias, 0f, 1f);
            data.offsetY = EditorGUILayout.Slider(T("垂直微调（负=下移）", "Vertical offset (− = down)"), data.offsetY, -0.35f, 0.35f);
            if (EditorGUI.EndChangeCheck())
            {
                data.Clamp();
                EditorUtility.SetDirty(data);
                if (Application.isPlaying && GameUI.Instance != null)
                    GameUI.Instance.RefreshPortraitLayoutFromAsset();
            }

            if (GUILayout.Button(T("保存到 asset", "Save to asset")))
                PortraitLayout.SaveCurrent();

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    PortraitEditMode.Enabled
                        ? T("Play 中 · 编辑已开。F10=滑条面板，Game 视图拖青色框。",
                            "Playing · Editing. F10 = slider panel; drag the cyan box in the Game view.")
                        : T("Play 中 · 请勾选上方「启用编辑模式」。",
                            "Playing · Tick \"Enable Game-view edit mode\" above."),
                    MessageType.None);
            }
        }

        void OnInspectorUpdate() => Repaint();
    }
}
