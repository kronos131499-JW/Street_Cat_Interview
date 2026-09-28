using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    [Serializable]
    public sealed class UILayoutOverrideEntry
    {
        public string path;
        [Tooltip("When enabled, the matching runtime UI object is hidden.")]
        public bool deleted;
        [Tooltip("Keep this component outside its parent LayoutGroup so it can be positioned freely.")]
        public bool ignoreParentLayout;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
    }

    [CreateAssetMenu(menuName = "Street Cat/UI Layout Overrides", fileName = "UILayoutOverrides")]
    public sealed class UILayoutOverrideData : ScriptableObject
    {
        public List<UILayoutOverrideEntry> entries = new List<UILayoutOverrideEntry>();

        public UILayoutOverrideEntry Find(string path)
        {
            if (string.IsNullOrEmpty(path) || entries == null) return null;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.path == path) return entry;
            }
            return null;
        }

        public void Set(string path, RectTransform target)
        {
            if (string.IsNullOrEmpty(path) || target == null) return;
            var entry = Find(path);
            if (entry == null)
            {
                entry = new UILayoutOverrideEntry { path = path };
                entries.Add(entry);
            }
            entry.anchorMin = target.anchorMin;
            entry.anchorMax = target.anchorMax;
            entry.pivot = target.pivot;
            entry.anchoredPosition = target.anchoredPosition;
            entry.sizeDelta = target.sizeDelta;
            entry.deleted = false;
            var layoutElement = target.GetComponent<UnityEngine.UI.LayoutElement>();
            entry.ignoreParentLayout = layoutElement != null && layoutElement.ignoreLayout;
        }

        public void SetDeleted(string path, RectTransform target, bool deleted)
        {
            if (string.IsNullOrEmpty(path)) return;
            var entry = Find(path);
            if (entry == null)
            {
                if (target == null) return;
                Set(path, target);
                entry = Find(path);
            }
            entry.deleted = deleted;
        }

        public bool Remove(string path)
        {
            var entry = Find(path);
            return entry != null && entries.Remove(entry);
        }
    }
}
