#if UNITY_EDITOR
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Play-mode toggle: drag portrait slot in Game view + on-screen sliders (F10).
    /// </summary>
    public static class PortraitEditMode
    {
        const string PrefKey = "StreetCat.PortraitEditMode";

        public static bool Enabled
        {
            get => UnityEditor.EditorPrefs.GetBool(PrefKey, false);
            set => UnityEditor.EditorPrefs.SetBool(PrefKey, value);
        }
    }
}
#endif
