using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>Loads, applies, and (in Editor) persists generic RectTransform overrides.</summary>
    public static class UILayoutOverrides
    {
        const string ResourcePath = "UILayoutOverrides";
        static UILayoutOverrideData _cached;
        static int _revision = 0;

        public static UILayoutOverrideData Asset
        {
            get
            {
                if (_cached == null) _cached = Resources.Load<UILayoutOverrideData>(ResourcePath);
                return _cached;
            }
        }

        public static int Revision => _revision;

#if UNITY_EDITOR
        /// <summary>Last Editor save/remove/delete/restore status for UI feedback.</summary>
        public static string LastOperationMessage { get; private set; } = "";
        public static bool LastOperationOk { get; private set; }
        public static string AssetDiskPath => "Assets/Resources/UILayoutOverrides.asset";

        static void RecordOperation(bool ok, string message)
        {
            LastOperationOk = ok;
            LastOperationMessage = message ?? "";
            if (ok) Debug.Log("[UI Layout] " + LastOperationMessage);
            else Debug.LogWarning("[UI Layout] " + LastOperationMessage);
        }
#endif

        public static string GetPath(Canvas canvas, RectTransform target)
        {
            if (canvas == null || target == null) return null;
            var root = canvas.transform;
            if (target != root && !target.IsChildOf(root)) return null;
            var builder = new StringBuilder(128);
            builder.Append(Escape(canvas.name));
            if (target == root) return builder.ToString();
            var chain = new List<Transform>();
            for (var current = target.transform; current != null && current != root; current = current.parent)
                chain.Add(current);
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                builder.Append('/');
                builder.Append(Escape(chain[i].name));
                builder.Append('#');
                builder.Append(GetSameNameIndex(chain[i]));
            }
            return builder.ToString();
        }

        public static bool TryApply(Canvas canvas, RectTransform target)
        {
            if (target != null && IsProtectedSystemOverlay(target.name))
                return false;
            var path = GetPath(canvas, target);
            if (IsSocialOwnedPath(path) || IsInvestigateHotspotPath(path))
                return false;
            var data = Asset;
            if (data == null) return false;
            var entry = data.Find(path);
            if (entry == null) return false;
            Apply(target, entry);
            return true;
        }

        /// <summary>Full-screen system layers must never be warped by layout overrides.</summary>
        public static bool IsProtectedSystemOverlay(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            return objectName == "SceneFade"
                   || objectName == "AdvanceCatcher"
                   || objectName == "Atmosphere"
                   || objectName == "Vignette"
                   || objectName == "EventSystem"
                   || objectName == "SocialOverlay";
        }

        /// <summary>Phone overlay is owned by SocialLayout.asset — never generic UI overrides.</summary>
        public static bool IsSocialOwnedPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.IndexOf("SocialOverlay", System.StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Map hotspots are owned by InvestigateHotspotLayout.asset.
        /// A generic override here wins on the next visit and looks like the hotspot editor failed to save.
        /// </summary>
        public static bool IsInvestigateHotspotPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.IndexOf("HotspotLayer", System.StringComparison.Ordinal) >= 0
                   && path.IndexOf("/Spot_", System.StringComparison.Ordinal) >= 0;
        }

        public static bool IsInvestigateHotspotSpot(RectTransform target)
        {
            if (target == null || string.IsNullOrEmpty(target.name) || !target.name.StartsWith("Spot_"))
                return false;
            return target.parent != null && target.parent.name == "HotspotLayer";
        }

        public static void Apply(RectTransform target, UILayoutOverrideEntry entry)
        {
            if (target == null || entry == null) return;
            if (entry.ignoreParentLayout)
            {
                var layoutElement = target.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layoutElement == null)
                    layoutElement = target.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layoutElement.ignoreLayout = true;
            }
            if (entry.deleted)
            {
                target.gameObject.SetActive(false);
                return;
            }
            target.anchorMin = entry.anchorMin;
            target.anchorMax = entry.anchorMax;
            target.pivot = entry.pivot;
            target.sizeDelta = entry.sizeDelta;
            target.anchoredPosition = entry.anchoredPosition;
            var fitter = target.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            ApplyTextStyle(target.GetComponent<TextMeshProUGUI>(), entry);
        }

        public static bool HasTextStyle(UILayoutOverrideEntry entry)
        {
            if (entry == null) return false;
            return !string.IsNullOrEmpty(entry.fontId)
                   || entry.fontSize > 1f
                   || entry.overrideLetterSpacing
                   || entry.overrideFontStyle
                   || entry.overrideColor;
        }

        public static void ApplyTextStyle(TextMeshProUGUI text, UILayoutOverrideEntry entry)
        {
            if (text == null || entry == null || !HasTextStyle(entry)) return;
            if (!string.IsNullOrEmpty(entry.fontId))
            {
                var face = StreetCat.Loc.TmpFontCatalog.Resolve(entry.fontId);
                if (face != null) text.font = face;
            }
            if (entry.fontSize > 1f)
            {
                text.enableAutoSizing = false;
                text.fontSize = entry.fontSize;
            }
            if (entry.overrideLetterSpacing)
                VnText.ApplyLetterSpacing(text, entry.letterSpacing);
            if (entry.overrideFontStyle)
            {
                var style = text.fontStyle;
                style = entry.bold ? (style | FontStyles.Bold) : (style & ~FontStyles.Bold);
                text.fontStyle = style;
            }
            if (entry.overrideColor)
                text.color = entry.textColor;

            var input = text.GetComponentInParent<TMP_InputField>();
            if (input != null && text.font != null &&
                (input.textComponent == text || input.placeholder == text))
                input.fontAsset = text.font;
        }

        /// <summary>
        /// Put per-text font/size/spacing/color back after the global settings font is applied.
        /// </summary>
        public static void ReapplyTextStyles()
        {
            var data = Asset;
            if (data == null || data.entries == null || data.entries.Count == 0) return;
            var canvases = Object.FindObjectsOfType<Canvas>(true);
            var rects = new List<RectTransform>(256);
            for (var c = 0; c < canvases.Length; c++)
            {
                var canvas = canvases[c];
                if (canvas == null || !canvas.isRootCanvas) continue;
                rects.Clear();
                canvas.GetComponentsInChildren(true, rects);
                for (var i = 0; i < rects.Count; i++)
                {
                    var target = rects[i];
                    if (target == null) continue;
                    var entry = data.Find(GetPath(canvas, target));
                    if (entry == null || entry.deleted || !HasTextStyle(entry)) continue;
                    ApplyTextStyle(target.GetComponent<TextMeshProUGUI>(), entry);
                }
            }
        }

        static int GetSameNameIndex(Transform target)
        {
            if (target.parent == null) return 0;
            var result = 0;
            for (var i = 0; i < target.GetSiblingIndex(); i++)
                if (target.parent.GetChild(i).name == target.name) result++;
            return result;
        }

        static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("%", "%25").Replace("/", "%2F").Replace("#", "%23");
        }

#if UNITY_EDITOR
        static string Timestamp() => System.DateTime.Now.ToString("HH:mm:ss");

        static void DropStaleHotspotOverride(string path)
        {
            var asset = Asset;
            if (asset == null || string.IsNullOrEmpty(path) || !asset.Remove(path)) return;
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            _revision++;
        }

        public static UILayoutOverrideData EnsureAsset()
        {
            var existing = Asset;
            if (existing != null) return existing;
            const string folder = "Assets/Resources";
            const string path = folder + "/UILayoutOverrides.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            var asset = ScriptableObject.CreateInstance<UILayoutOverrideData>();
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _cached = asset;
            _revision++;
            RecordOperation(true, "created " + path + " @ " + Timestamp());
            return asset;
        }

        public static bool Save(Canvas canvas, RectTransform target)
        {
            if (target != null && IsProtectedSystemOverlay(target.name))
            {
                RecordOperation(false, "save blocked — system overlay (" + target.name + ") @ " + Timestamp());
                return false;
            }
            var path = GetPath(canvas, target);
            if (IsSocialOwnedPath(path))
            {
                if (SocialLayout.TrySaveFromRect(target))
                {
                    RecordOperation(true, "saved social phone → SocialLayout.asset @ " + Timestamp());
                    return true;
                }
                RecordOperation(false, "save blocked — use 社交帖子布局编辑器 for " + path + " @ " + Timestamp());
                return false;
            }
            if (IsInvestigateHotspotPath(path) || IsInvestigateHotspotSpot(target))
            {
                if (StreetCat.Investigation.InvestigateHotspotLayout.TrySaveFromRect(target))
                {
                    DropStaleHotspotOverride(path);
                    RecordOperation(true, "saved hotspot → InvestigateHotspotLayout.asset | " + target.name + " @ " + Timestamp());
                    return true;
                }
                RecordOperation(false, "hotspot save failed — " + target.name + " @ " + Timestamp());
                return false;
            }
            if (string.IsNullOrEmpty(path))
            {
                RecordOperation(false, "save failed — invalid path @ " + Timestamp());
                return false;
            }
            var asset = EnsureAsset();
            if (asset == null)
            {
                RecordOperation(false, "save failed — no asset @ " + Timestamp());
                return false;
            }
            UnityEditor.Undo.RecordObject(asset, "Save UI Layout");
            asset.Set(path, target);
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            _revision++;
            RecordOperation(true, "saved → " + AssetDiskPath + " | " + path + " @ " + Timestamp());
            return true;
        }

        public static bool SaveTextStyle(
            Canvas canvas,
            RectTransform target,
            string fontId,
            bool customSize,
            float fontSize,
            bool customSpacing,
            float spacing,
            bool customStyle,
            bool bold,
            bool customColor,
            Color color)
        {
            if (target != null && IsProtectedSystemOverlay(target.name))
            {
                RecordOperation(false, "text style blocked — system overlay @ " + Timestamp());
                return false;
            }
            var path = GetPath(canvas, target);
            if (string.IsNullOrEmpty(path) || IsSocialOwnedPath(path) || IsInvestigateHotspotPath(path))
            {
                RecordOperation(false, "text style failed — invalid path @ " + Timestamp());
                return false;
            }
            var asset = EnsureAsset();
            if (asset == null)
            {
                RecordOperation(false, "text style failed — no asset @ " + Timestamp());
                return false;
            }
            UnityEditor.Undo.RecordObject(asset, "Save Text Style");
            if (asset.Find(path) == null)
                asset.Set(path, target);
            var entry = asset.Find(path);
            if (entry == null)
            {
                RecordOperation(false, "text style failed — no entry @ " + Timestamp());
                return false;
            }
            entry.fontId = fontId ?? "";
            entry.fontSize = customSize ? Mathf.Clamp(fontSize, 8f, 96f) : 0f;
            entry.overrideLetterSpacing = customSpacing;
            entry.letterSpacing = Mathf.Clamp(spacing, 0f, 20f);
            entry.overrideFontStyle = customStyle;
            entry.bold = bold;
            entry.overrideColor = customColor;
            entry.textColor = color;
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            _revision++;
            RecordOperation(true, "text style → " + path + " @ " + Timestamp());
            return true;
        }

        public static bool Remove(Canvas canvas, RectTransform target)
        {
            var asset = Asset;
            var path = GetPath(canvas, target);
            if (asset == null || string.IsNullOrEmpty(path))
            {
                RecordOperation(false, "remove failed — no entry @ " + Timestamp());
                return false;
            }
            UnityEditor.Undo.RecordObject(asset, "Remove UI Layout");
            if (!asset.Remove(path))
            {
                RecordOperation(false, "remove failed — not found: " + path + " @ " + Timestamp());
                return false;
            }
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _revision++;
            RecordOperation(true, "removed entry " + path + " from " + AssetDiskPath + " @ " + Timestamp());
            return true;
        }

        public static bool Delete(Canvas canvas, RectTransform target)
        {
            if (target != null && IsProtectedSystemOverlay(target.name))
            {
                RecordOperation(false, "delete blocked — system overlay (" + target.name + ") @ " + Timestamp());
                return false;
            }
            if (IsInvestigateHotspotSpot(target))
            {
                RecordOperation(false, "调查热点不能从通用布局删除，请用调查热点编辑器移动 @ " + Timestamp());
                return false;
            }
            var path = GetPath(canvas, target);
            if (IsSocialOwnedPath(path))
            {
                RecordOperation(false, "delete blocked — social overlay owned by SocialLayout @ " + Timestamp());
                return false;
            }
            if (string.IsNullOrEmpty(path))
            {
                RecordOperation(false, "delete failed — invalid path @ " + Timestamp());
                return false;
            }
            var asset = EnsureAsset();
            if (asset == null)
            {
                RecordOperation(false, "delete failed — no asset @ " + Timestamp());
                return false;
            }
            UnityEditor.Undo.RecordObject(asset, "Delete UI Component");
            asset.SetDeleted(path, target, true);
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            _revision++;
            RecordOperation(true, "deleted (hidden) " + path + " @ " + Timestamp());
            return true;
        }

        public static bool ApplyUndoSnapshot(bool hadSavedEntry, UILayoutOverrideEntry snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.path))
            {
                RecordOperation(false, "undo failed — empty snapshot @ " + Timestamp());
                return false;
            }
            var asset = EnsureAsset();
            if (asset == null)
            {
                RecordOperation(false, "undo failed — no asset @ " + Timestamp());
                return false;
            }
            UnityEditor.Undo.RecordObject(asset, "Undo UI Layout");
            asset.WriteSnapshot(hadSavedEntry, snapshot);
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = asset;
            _revision++;
            RecordOperation(true, "undid → " + snapshot.path + " @ " + Timestamp());
            return true;
        }

        public static bool Restore(string path)
        {
            var asset = Asset;
            var entry = asset != null ? asset.Find(path) : null;
            if (entry == null || !entry.deleted)
            {
                RecordOperation(false, "restore failed — " + (path ?? "?") + " @ " + Timestamp());
                return false;
            }
            UnityEditor.Undo.RecordObject(asset, "Restore UI Component");
            entry.deleted = false;
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _revision++;
            RecordOperation(true, "restored " + path + " @ " + Timestamp());
            return true;
        }

        public static int RestoreAllDeleted()
        {
            var asset = Asset;
            if (asset == null || asset.entries == null) return 0;
            var count = 0;
            UnityEditor.Undo.RecordObject(asset, "Restore All Deleted UI Components");
            for (var i = 0; i < asset.entries.Count; i++)
            {
                var entry = asset.entries[i];
                if (entry == null || !entry.deleted) continue;
                entry.deleted = false;
                count++;
            }
            if (count == 0) return 0;
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            _revision++;
            RecordOperation(true, "restored " + count + " deleted component(s) @ " + Timestamp());
            return count;
        }
#endif
    }
}
