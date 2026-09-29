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
        [Tooltip("0 keeps the widget's own font size. Notebook text edits store the chosen size here.")]
        public float fontSize;
        [Tooltip("Empty keeps the player's global font from settings.")]
        public string fontId;
        public bool overrideLetterSpacing;
        public float letterSpacing;
        public bool overrideFontStyle;
        public bool bold;
        public bool overrideColor;
        public Color textColor = Color.white;

        public UILayoutOverrideEntry Clone()
        {
            var copy = new UILayoutOverrideEntry();
            copy.CopyFrom(this);
            return copy;
        }

        public void CopyFrom(UILayoutOverrideEntry other)
        {
            if (other == null) return;
            path = other.path;
            deleted = other.deleted;
            ignoreParentLayout = other.ignoreParentLayout;
            anchorMin = other.anchorMin;
            anchorMax = other.anchorMax;
            pivot = other.pivot;
            anchoredPosition = other.anchoredPosition;
            sizeDelta = other.sizeDelta;
            fontSize = other.fontSize;
            fontId = other.fontId;
            overrideLetterSpacing = other.overrideLetterSpacing;
            letterSpacing = other.letterSpacing;
            overrideFontStyle = other.overrideFontStyle;
            bold = other.bold;
            overrideColor = other.overrideColor;
            textColor = other.textColor;
        }

        public void Capture(string entryPath, RectTransform target)
        {
            path = entryPath;
            deleted = target != null && !target.gameObject.activeSelf;
            if (target == null) return;
            anchorMin = target.anchorMin;
            anchorMax = target.anchorMax;
            pivot = target.pivot;
            anchoredPosition = target.anchoredPosition;
            sizeDelta = target.sizeDelta;
            var text = target.GetComponent<TMPro.TextMeshProUGUI>();
            fontSize = text != null ? text.fontSize : 0f;
            var layoutElement = target.GetComponent<UnityEngine.UI.LayoutElement>();
            ignoreParentLayout = layoutElement != null && layoutElement.ignoreLayout;
            var saved = UILayoutOverrides.Asset != null ? UILayoutOverrides.Asset.Find(entryPath) : null;
            if (saved != null)
            {
                fontSize = saved.fontSize;
                fontId = saved.fontId;
                overrideLetterSpacing = saved.overrideLetterSpacing;
                letterSpacing = saved.letterSpacing;
                overrideFontStyle = saved.overrideFontStyle;
                bold = saved.bold;
                overrideColor = saved.overrideColor;
                textColor = saved.textColor;
            }
        }
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
            string keepFontId = null;
            var keepSpacing = false;
            var keepSpacingValue = 0f;
            var keepStyle = false;
            var keepBold = false;
            var keepColor = false;
            var keepColorValue = Color.white;
            if (entry != null)
            {
                keepFontId = entry.fontId;
                keepSpacing = entry.overrideLetterSpacing;
                keepSpacingValue = entry.letterSpacing;
                keepStyle = entry.overrideFontStyle;
                keepBold = entry.bold;
                keepColor = entry.overrideColor;
                keepColorValue = entry.textColor;
            }
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
            var text = target.GetComponent<TMPro.TextMeshProUGUI>();
            entry.fontSize = text != null ? text.fontSize : 0f;
            entry.fontId = keepFontId;
            entry.overrideLetterSpacing = keepSpacing;
            entry.letterSpacing = keepSpacingValue;
            entry.overrideFontStyle = keepStyle;
            entry.bold = keepBold;
            entry.overrideColor = keepColor;
            entry.textColor = keepColorValue;
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

        public UILayoutOverrideEntry CloneEntry(string path)
        {
            var entry = Find(path);
            return entry != null ? entry.Clone() : null;
        }

        public void WriteSnapshot(bool existed, UILayoutOverrideEntry snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.path)) return;
            if (!existed)
            {
                Remove(snapshot.path);
                return;
            }
            var entry = Find(snapshot.path);
            if (entry == null)
            {
                entry = snapshot.Clone();
                entries.Add(entry);
                return;
            }
            entry.CopyFrom(snapshot);
        }
    }
}
