#if UNITY_EDITOR
using UnityEditor;

namespace StreetCat.UI
{
    public static class UILayoutEditMode
    {
        const string Prefix = "StreetCat.UILayoutEdit.";
        public static bool Enabled { get => EditorPrefs.GetBool(Prefix + "Enabled", false); set => EditorPrefs.SetBool(Prefix + "Enabled", value); }
        public static bool SnapEnabled { get => EditorPrefs.GetBool(Prefix + "Snap", true); set => EditorPrefs.SetBool(Prefix + "Snap", value); }
        public static float GridSize { get => EditorPrefs.GetFloat(Prefix + "Grid", 10f); set => EditorPrefs.SetFloat(Prefix + "Grid", value); }
        public static bool ShowAllFrames { get => EditorPrefs.GetBool(Prefix + "ShowAll", true); set => EditorPrefs.SetBool(Prefix + "ShowAll", value); }
        /// <summary>When true, Game-view clicks will not change the current selection.</summary>
        public static bool LockSelection { get => EditorPrefs.GetBool(Prefix + "LockSel", false); set => EditorPrefs.SetBool(Prefix + "LockSel", value); }
        /// <summary>Skip near-fullscreen dimmers / catchers when picking.</summary>
        public static bool SkipFullscreenCatchers { get => EditorPrefs.GetBool(Prefix + "SkipFS", true); set => EditorPrefs.SetBool(Prefix + "SkipFS", value); }
        /// <summary>Clicks select the text itself so font and size can be edited.</summary>
        public static bool TextFocus { get => EditorPrefs.GetBool(Prefix + "TextFocus", false); set => EditorPrefs.SetBool(Prefix + "TextFocus", value); }
    }
}
#endif
