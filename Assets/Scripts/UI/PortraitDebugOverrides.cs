using System.Collections.Generic;

namespace StreetCat.UI
{
    /// <summary>
    /// Per-line portrait overrides for debug playtests (current session only).
    /// Click = preview; confirm applies only to that script line index.
    /// </summary>
    public static class PortraitDebugOverrides
    {
        static readonly Dictionary<string, string> SavedByLine = new Dictionary<string, string>();

        static string previewLineKey;
        static string previewPortraitKey;

        public static string PreviewLineKey => previewLineKey;
        public static string PreviewPortraitKey => previewPortraitKey;

        public static string BuildLineKey(string sceneId, int lineIndex)
            => string.IsNullOrEmpty(sceneId) ? null : "script:" + sceneId + ":" + lineIndex;

        public static string BuildBeatKey(string kind, string sourceId, int beatIndex)
        {
            if (string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(sourceId) || beatIndex < 0)
                return null;
            return kind + ":" + sourceId + ":" + beatIndex;
        }

        public static string BuildSingleBeatKey(string kind, string sourceId)
        {
            if (string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(sourceId)) return null;
            return kind + ":" + sourceId + ":single";
        }

        public static void SetPreview(string lineKey, string portraitKey)
        {
            previewLineKey = lineKey;
            previewPortraitKey = portraitKey;
        }

        public static void ClearPreview()
        {
            previewLineKey = null;
            previewPortraitKey = null;
        }

        public static bool HasPreviewForLine(string lineKey)
            => !string.IsNullOrEmpty(lineKey)
               && lineKey == previewLineKey
               && !string.IsNullOrEmpty(previewPortraitKey);

        public static void ConfirmPreview(string lineKey)
        {
            if (string.IsNullOrEmpty(lineKey) || previewLineKey != lineKey
                || string.IsNullOrEmpty(previewPortraitKey))
                return;
            SavedByLine[lineKey] = previewPortraitKey;
        }

        public static string GetSaved(string lineKey)
        {
            if (string.IsNullOrEmpty(lineKey)) return null;
            return SavedByLine.TryGetValue(lineKey, out var key) ? key : null;
        }

        public static void ClearSaved(string lineKey)
        {
            if (string.IsNullOrEmpty(lineKey)) return;
            SavedByLine.Remove(lineKey);
            if (previewLineKey == lineKey)
                ClearPreview();
        }

        public static void ClearAllSaved() => SavedByLine.Clear();

        /// <summary>Drop preview when advancing to a different line.</summary>
        public static void OnLineChanged(string lineKey)
        {
            if (previewLineKey != lineKey)
                ClearPreview();
        }

        /// <summary>Saved override for this line, or null.</summary>
        public static string ResolveForLine(string lineKey)
            => GetSaved(lineKey);
    }
}
