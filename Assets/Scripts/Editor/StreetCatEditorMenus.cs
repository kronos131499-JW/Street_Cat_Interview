using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public static class StreetCatEditorMenus
    {
        [MenuItem("StreetCat/Play Chapter1 From SampleScene")]
        static void Play()
        {
            if (!EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;
        }

        [MenuItem("StreetCat/Log Persistent Save Path")]
        static void LogSavePath()
        {
            Debug.Log(Application.persistentDataPath);
        }
    }
}
