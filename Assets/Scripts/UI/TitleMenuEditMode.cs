#if UNITY_EDITOR
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>Play Mode toggle: drag title-menu elements in the Game view.</summary>
    public static class TitleMenuEditMode
    {
        const string PrefKey = "StreetCat.TitleMenuEditMode";

        public static bool Enabled
        {
            get => UnityEditor.EditorPrefs.GetBool(PrefKey, false);
            set => UnityEditor.EditorPrefs.SetBool(PrefKey, value);
        }
    }
}
#endif
