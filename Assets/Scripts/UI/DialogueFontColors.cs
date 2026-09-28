using UnityEngine;

namespace StreetCat.UI
{
    public static class DialogueFontColors
    {
        const string ResourcePath = "DialogueFontColors";
        static DialogueFontColorData _cached;
        static DialogueFontColorData _fallback;

        public static DialogueFontColorData Asset
        {
            get
            {
                if (_cached == null) _cached = Resources.Load<DialogueFontColorData>(ResourcePath);
                return _cached;
            }
        }

        public static DialogueFontColorData Current
        {
            get
            {
                var asset = Asset;
                if (asset != null) return asset;
                if (_fallback == null)
                {
                    _fallback = ScriptableObject.CreateInstance<DialogueFontColorData>();
                    _fallback.hideFlags = HideFlags.HideAndDontSave;
                    _fallback.ApplyDefaults();
                }
                return _fallback;
            }
        }

#if UNITY_EDITOR
        public static DialogueFontColorData EnsureAsset()
        {
            var existing = Asset;
            if (existing != null) return existing;
            const string folder = "Assets/Resources";
            const string path = folder + "/DialogueFontColors.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            var asset = ScriptableObject.CreateInstance<DialogueFontColorData>();
            asset.ApplyDefaults();
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _cached = asset;
            return asset;
        }

        public static void Save(DialogueFontColorData asset)
        {
            if (asset == null) return;
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            if (Application.isPlaying && GameUI.Instance != null)
                GameUI.Instance.RefreshDialogueFontColors();
        }

        public static void ResetToDefaults()
        {
            var asset = EnsureAsset();
            if (asset == null) return;
            UnityEditor.Undo.RecordObject(asset, "Reset Dialogue Font Colors");
            asset.ApplyDefaults();
            Save(asset);
        }
#endif
    }
}
