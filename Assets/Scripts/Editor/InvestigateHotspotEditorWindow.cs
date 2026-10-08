using StreetCat.Investigation;
using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public class InvestigateHotspotEditorWindow : EditorWindow
    {
        static string T(string zh, string en) => ToolLang.T(zh, en);

        [MenuItem("街角专访/调查热点编辑器")]
        public static void Open()
        {
            var win = GetWindow<InvestigateHotspotEditorWindow>(T("调查热点", "Hotspots"));
            win.minSize = new Vector2(320, 280);
            win.Show();
        }

        [MenuItem("街角专访/切换调查热点编辑模式")]
        [MenuItem("StreetCat/Toggle Hotspot Edit Mode")]
        public static void ToggleEditMode()
        {
            InvestigateHotspotEditMode.Enabled = !InvestigateHotspotEditMode.Enabled;
            Debug.Log(InvestigateHotspotEditMode.Enabled
                ? T("[调查热点] 编辑模式 ON — Play 进入调查后，在 Game 视图拖拽橙色框；松手自动保存。",
                    "[Hotspots] Edit mode ON — enter an investigation in Play Mode and drag the orange boxes in the Game view; release to auto-save.")
                : T("[调查热点] 编辑模式 OFF", "[Hotspots] Edit mode OFF"));
        }

        void OnGUI()
        {
            titleContent.text = T("调查热点", "Hotspots");
            ToolLang.DrawToggle();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(T("调查热点 · Game 视图编辑", "Investigation Hotspots · Edit in Game view"), EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            EditorGUILayout.HelpBox(T(
                "1. 打开本窗口或用菜单「切换调查热点编辑模式」打开编辑\n" +
                "2. 点击 Play，进入槐安社区调查界面\n" +
                "3. 在 Game 视图拖拽橙色半透明框：中间拖动整体，四角拖动缩放\n" +
                "4. 松手后自动写入 Resources/InvestigateHotspotLayout.asset",
                "1. Enable edit mode here or via the \"Toggle Hotspot Edit Mode\" menu\n" +
                "2. Press Play and open the Huai'an Community investigation map\n" +
                "3. In the Game view, drag the orange boxes: drag the middle to move, a corner to resize\n" +
                "4. Releasing the mouse saves to Resources/InvestigateHotspotLayout.asset"),
                MessageType.Info);

            EditorGUILayout.Space(8);
            var edit = InvestigateHotspotEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft(T("启用 Game 视图编辑模式", "Enable Game-view edit mode"), edit);
            if (next != edit)
                InvestigateHotspotEditMode.Enabled = next;

            EditorGUILayout.Space(8);
            if (GUILayout.Button(T("创建 / 选中 Layout 资源", "Create / select layout asset")))
            {
                var asset = InvestigateHotspotLayout.EnsureAsset();
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            if (GUILayout.Button(T("从默认值重置资源", "Reset asset to defaults")))
            {
                if (EditorUtility.DisplayDialog(T("重置热点布局", "Reset hotspot layout"),
                        T("用代码里的默认坐标覆盖 InvestigateHotspotLayout.asset？",
                          "Overwrite InvestigateHotspotLayout.asset with the built-in default coordinates?"),
                        T("重置", "Reset"), T("取消", "Cancel")))
                {
                    var asset = InvestigateHotspotLayout.EnsureAsset();
                    asset.entries.Clear();
                    foreach (var kv in InvestigateHotspotLayout.DefaultHuaianMap)
                        asset.entries.Add(new HotspotRectEntry { id = kv.Key, rect = kv.Value });
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                    InvestigateHotspotLayout.InvalidateCache();
                }
            }

            EditorGUILayout.Space(8);
            var data = InvestigateHotspotLayout.Asset;
            if (data == null)
            {
                EditorGUILayout.HelpBox(T("尚未创建 Layout 资源。点上面的按钮创建。",
                    "No layout asset yet. Click the button above to create one."), MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(T("当前坐标", "Current rects (xMin, yMin, xMax, yMax)"), EditorStyles.boldLabel);
            foreach (var e in data.entries)
            {
                if (e == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(e.id, GUILayout.Width(100));
                EditorGUILayout.LabelField(
                    $"({e.rect.x:F2}, {e.rect.y:F2}, {e.rect.z:F2}, {e.rect.w:F2})");
                EditorGUILayout.EndHorizontal();
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    InvestigateHotspotEditMode.Enabled
                        ? T("Play 中 · 编辑已开。请进入调查界面后在 Game 视图拖拽。",
                            "Playing · Editing. Open the investigation map and drag in the Game view.")
                        : T("Play 中 · 请勾选上方「启用编辑模式」。",
                            "Playing · Tick \"Enable Game-view edit mode\" above."),
                    MessageType.None);
            }
        }

        void OnInspectorUpdate() => Repaint();
    }
}
