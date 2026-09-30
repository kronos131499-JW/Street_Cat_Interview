using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>Applies saved overrides to existing and newly-created runtime UI elements.</summary>
    public sealed class UILayoutOverrideRuntime : MonoBehaviour
    {
        readonly List<RectTransform> _rects = new List<RectTransform>(256);
        readonly HashSet<int> _applied = new HashSet<int>();
        int _seenRevision = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindObjectOfType<UILayoutOverrideRuntime>() != null) return;
            var host = new GameObject("UILayoutOverrideRuntime");
            host.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(host);
            host.AddComponent<UILayoutOverrideRuntime>();
        }

        static bool _scanRequested = true;

        /// <summary>Scan on the next LateUpdate. Call after building a screen.</summary>
        public static void RequestScan() => _scanRequested = true;

        void LateUpdate()
        {
            bool revisionChanged = _seenRevision != UILayoutOverrides.Revision;
            if (revisionChanged)
            {
                _seenRevision = UILayoutOverrides.Revision;
                _applied.Clear();
                _scanRequested = true;
            }

            // New widgets used to sit at the code size for up to 1.5s, then snap
            // to the saved layout. Apply them the frame they appear. Already-settled
            // objects stay in _applied so this does not restamp them every frame.
            _scanRequested = false;
            var data = UILayoutOverrides.Asset;
            if (data == null || data.entries == null || data.entries.Count == 0) return;
            var canvases = FindObjectsOfType<Canvas>();
            for (var c = 0; c < canvases.Length; c++)
            {
                var canvas = canvases[c];
                if (canvas == null || !canvas.isRootCanvas) continue;
                _rects.Clear();
                canvas.GetComponentsInChildren(true, _rects);
                for (var i = 0; i < _rects.Count; i++)
                {
                    var target = _rects[i];
                    if (target == null || target == canvas.transform) continue;
                    var id = target.GetInstanceID();
                    if (_applied.Contains(id)) continue;
                    UILayoutOverrides.TryApply(canvas, target);
                    _applied.Add(id);
                }
            }
        }
    }
}
