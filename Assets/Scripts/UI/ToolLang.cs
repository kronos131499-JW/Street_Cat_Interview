namespace StreetCat.UI
{
    /// <summary>
    /// Display language for the in-editor dev tools (UI layout, hotspot, portrait editors).
    /// Independent of the game's own language setting. Player builds always report Chinese.
    /// </summary>
    public static class ToolLang
    {
        const string PrefKey = "StreetCat.ToolsEnglish";

#if UNITY_EDITOR
        static bool? _english;

        public static bool English
        {
            get
            {
                if (_english == null)
                    _english = UnityEditor.EditorPrefs.GetBool(PrefKey, true);
                return _english.Value;
            }
            set
            {
                _english = value;
                UnityEditor.EditorPrefs.SetBool(PrefKey, value);
            }
        }
#else
        public static bool English => false;
#endif

        public static string T(string zh, string en) => English ? en : zh;

#if UNITY_EDITOR
        /// <summary>Language switch row drawn at the top of each tool window.</summary>
        public static void DrawToggle()
        {
            var next = UnityEditor.EditorGUILayout.ToggleLeft("English tool UI / 工具界面英文", English);
            if (next != English)
                English = next;
        }
#endif
    }
}
