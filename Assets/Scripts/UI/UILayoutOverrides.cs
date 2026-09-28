using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>Loads, applies, and (in Editor) persists generic RectTransform overrides.</summary>
    public static class UILayoutOverrides
    {
        const string ResourcePath = "UILayoutOverrides";
        static UILayoutOverrideData _cached;
        static int _revision = 0;

        public static UILayoutOverrideData Asset
        {
            get
            {
                if (_cached == null) _cached = Resources.Load<UILayoutOverrideData>(ResourcePath);
                return _cached;
            }
        }

        public static int Revision => _revision;

        public static string GetPath(Canvas canvas, RectTransform target)
        {
            if (canvas == null || target == null) return null;
            var root = canvas.transform;
            if (target != root && !target.IsChildOf(root)) return null;
            var builder = new StringBuilder(128);
            builder.Append(Escape(canvas.name));
            if (target == root) return builder.ToString();
            var chain = new List<Transform>();
            for (var current = target.transform; current != null && current != root; current = current.parent)
                chain.Add(current);
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                builder.Append('/');
                builder.Append(Escape(chain[i].name));
                builder.Append('#');
                builder.Append(GetSameNameIndex(chain[i]));
            }
            return builder.ToString();
        }

        public static bool TryApply(Canvas canvas, RectTransform target)
        {
            var data = Asset;
            if (data == null) return false;
            var entry = data.Find(GetPath(canvas, target));
            if (entry == null) return false;
            Apply(target, entry);
            return true;
        }

        public static void Apply(RectTransform target, UILayoutOverrideEntry entry)
        {
            if (target == null || entry == null) return;
            if (entry.ignoreParentLayout)
            {
                var layoutElement = target.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layoutElement == null)
                    layoutElement = target.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layoutElement.ignoreLayout = true;
            }
            if (entry.deleted)
            {
                target.gameObject.SetActive(false);
                return;
            }
            target.anchorMin = entry.anchorMin;
            target.anchorMax = entry.anchorMax;
            target.pivot = entry.pivot;
            target.sizeDelta = entry.sizeDelta;
            target.anchoredPosition = entry.anchoredPosition;
        }

        static int GetSameNameIndex(Transform target)
        {
            if (target.parent == null) return 0;
            var result = 0;
            for (var i = 0; i < target.GetSiblingIndex(); i++)
                if (target.parent.GetChild(i).name == target.name) result++;
            return result;
        }

        static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("%", "%25").Replace("/", "%2F").Replace("#", "%23");
        }

#if UNITY_EDITOR
        public static UILayoutOverrideData EnsureAsset()
        {
            var existing = Asset;
            if (existing != null) return existing;
            const string folder = "Assets/Resources";
            const string path = folder + "/UILayoutOverrides.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            var asset = ScriptableObject.CreateInstance<UILayoutOverrideData>();
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _cached = asset;
            _revision++;
            Debug.Log("[UI Layout] created " + path);
            return asset;
        }

        public static bool Save(Canvas canvas, RectTransform target)
        {
            var path = GetPath(canvas, target);
            if (string.IsNullOrEmpty(path)) return false;
            var asset = EnsureAsset();
            if (asset == null) return false;
            UnityEditor.Undo.RecordObject(asset, "Save UI Layout");
            asset.Set(path, target);
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            _revision++;
            Debug.Log("[UI Layout] saved " + path);
            return true;
        }

        public static bool Remove(Canvas canvas, RectTransform target)
        {
            var asset = Asset;
            var path = GetPath(canvas, target);
            if (asset == null || string.IsNullOrEmpty(path)) return false;
            UnityEditor.Undo.RecordObject(asset, "Remove UI Layout");
            if (!asset.Remove(path)) return false;
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _revision++;
            Debug.Log("[UI Layout] removed " + path);
            return true;
        }

        public static bool Delete(Canvas canvas, RectTransform target)
        {
            var path = GetPath(canvas, target);
            if (string.IsNullOrEmpty(path)) return false;
            var asset = EnsureAsset();
            if (asset == null) return false;
            UnityEditor.Undo.RecordObject(asset, "Delete UI Component");
            asset.SetDeleted(path, target, true);
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            _revision++;
            Debug.Log("[UI Layout] deleted " + path);
            return true;
        }

        public static bool Restore(string path)
        {
            var asset = Asset;
            var entry = asset != null ? asset.Find(path) : null;
            if (entry == null || !entry.deleted) return false;
            UnityEditor.Undo.RecordObject(asset, "Restore UI Component");
            entry.deleted = false;
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _revision++;
            Debug.Log("[UI Layout] restored " + path);
            return true;
        }

        public static int RestoreAllDeleted()
        {
            var asset = Asset;
            if (asset == null || asset.entries == null) return 0;
            var count = 0;
            UnityEditor.Undo.RecordObject(asset, "Restore All Deleted UI Components");
            for (var i = 0; i < asset.entries.Count; i++)
            {
                var entry = asset.entries[i];
                if (entry == null || !entry.deleted) continue;
                entry.deleted = false;
                count++;
            }
            if (count == 0) return 0;
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _revision++;
            Debug.Log("[UI Layout] restored " + count + " deleted component(s)");
            return count;
        }
#endif
    }
}
