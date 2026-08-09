using StreetCat.Investigation;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public class InvestigateHotspotEditorWindow : EditorWindow
    {
        [MenuItem("街角专访/调查热点编辑器")]
        public static void Open()
        {
            var win = GetWindow<InvestigateHotspotEditorWindow>("调查热点");
            win.minSize = new Vector2(320, 280);
            win.Show();
        }

        [MenuItem("街角专访/切换调查热点编辑模式")]
        public static void ToggleEditMode()
        {
            InvestigateHotspotEditMode.Enabled = !InvestigateHotspotEditMode.Enabled;
            Debug.Log(InvestigateHotspotEditMode.Enabled
                ? "[调查热点] 编辑模式 ON — Play 进入调查后，在 Game 视图拖拽橙色框；松手自动保存。"
                : "[调查热点] 编辑模式 OFF");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("调查热点 · Game 视图编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "1. 打开本窗口或用菜单「切换调查热点编辑模式」打开编辑\n" +
                "2. 点击 Play，进入槐安社区调查界面\n" +
                "3. 在 Game 视图拖拽橙色半透明框：中间拖动整体，四角拖动缩放\n" +
                "4. 松手后自动写入 Resources/InvestigateHotspotLayout.asset",
                MessageType.Info);

            EditorGUILayout.Space(8);
            var edit = InvestigateHotspotEditMode.Enabled;
            var next = EditorGUILayout.ToggleLeft("启用 Game 视图编辑模式", edit);
            if (next != edit)
                InvestigateHotspotEditMode.Enabled = next;

            EditorGUILayout.Space(8);
            if (GUILayout.Button("创建 / 选中 Layout 资源"))
            {
                var asset = InvestigateHotspotLayout.EnsureAsset();
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            if (GUILayout.Button("从默认值重置资源"))
            {
                if (EditorUtility.DisplayDialog("重置热点布局",
                        "用代码里的默认坐标覆盖 InvestigateHotspotLayout.asset？", "重置", "取消"))
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
                EditorGUILayout.HelpBox("尚未创建 Layout 资源。点上面的按钮创建。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("当前坐标", EditorStyles.boldLabel);
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
                        ? "Play 中 · 编辑已开。请进入调查界面后在 Game 视图拖拽。"
                        : "Play 中 · 请勾选上方「启用编辑模式」。",
                    MessageType.None);
            }
        }

        void OnInspectorUpdate() => Repaint();
    }
}
