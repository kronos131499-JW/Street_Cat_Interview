#if UNITY_EDITOR
using UnityEngine;

namespace StreetCat.Investigation
{
    /// <summary>
    /// Play-mode toggle: when enabled, investigate hotspots can be dragged in the Game view.
    /// </summary>
    public static class InvestigateHotspotEditMode
    {
        const string PrefKey = "StreetCat.InvestigateHotspotEditMode";

        public static bool Enabled
        {
            get => UnityEditor.EditorPrefs.GetBool(PrefKey, false);
            set => UnityEditor.EditorPrefs.SetBool(PrefKey, value);
        }
    }
}
#endif
