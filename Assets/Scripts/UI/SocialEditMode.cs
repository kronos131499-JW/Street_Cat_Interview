#if UNITY_EDITOR
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>Play Mode toggle: drag social phone frame in the Game view.</summary>
    public static class SocialEditMode
    {
        const string PrefKey = "StreetCat.SocialEditMode";

        public static bool Enabled
        {
            get => UnityEditor.EditorPrefs.GetBool(PrefKey, false);
            set => UnityEditor.EditorPrefs.SetBool(PrefKey, value);
        }
    }
}
#endif
