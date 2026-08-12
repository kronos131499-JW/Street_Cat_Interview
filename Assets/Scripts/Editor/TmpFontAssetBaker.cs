using System.IO;
using StreetCat.Loc;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace StreetCat.Editor
{
    /// <summary>
    /// One-shot baker: builds Dynamic multi-atlas TMP font assets under
    /// Assets/Resources/Fonts/TMP/ so Play Mode does not rely solely on
    /// runtime <see cref="TMP_FontAsset.CreateFontAsset"/>.
    /// </summary>
    public static class TmpFontAssetBaker
    {
        const string OutputDir = "Assets/Resources/Fonts/TMP";
        const string Probe = "街角专访Aa1";

        [MenuItem("StreetCat/Fonts/Bake TMP Font Assets (Dynamic CJK)")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(OutputDir.Replace('\\', '/'));

            int ok = 0;
            int fail = 0;
            foreach (var opt in FontCatalog.All)
            {
                if (string.IsNullOrEmpty(opt.ResourcesName))
                {
                    // "system" — bake SiYuan as the CJK stand-in used at runtime.
                    if (opt.Id != "system") continue;
                }

                string resourcesName = string.IsNullOrEmpty(opt.ResourcesName) ? "SiYuanHeiTi" : opt.ResourcesName;
                string fontPath = FindFontAssetPath(resourcesName);
                if (string.IsNullOrEmpty(fontPath))
                {
                    Debug.LogWarning("[TmpFontAssetBaker] No Font asset for " + resourcesName);
                    fail++;
                    continue;
                }

                var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                if (source == null)
                {
                    Debug.LogWarning("[TmpFontAssetBaker] Failed to load Font at " + fontPath);
                    fail++;
                    continue;
                }

                string outPath = OutputDir + "/" + resourcesName + ".asset";
                if (BakeOne(source, resourcesName, outPath, !opt.LatinOnly))
                    ok++;
                else
                    fail++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "TMP Font Baker",
                "Baked " + ok + " TMP font asset(s). Failures: " + fail +
                ".\n\nOutput: " + OutputDir +
                "\n\nIf CJK still shows □, confirm SiYuanHeiTi baked and Console has no CreateFontAsset errors.",
                "OK");
        }

        [MenuItem("StreetCat/Fonts/Import Windows SimHei (TTF CJK)")]
        public static void ImportWindowsSimHei()
        {
            const string src = @"C:\Windows\Fonts\simhei.ttf";
            const string dest = "Assets/Resources/Fonts/SimHei.ttf";
            if (!File.Exists(src))
            {
                EditorUtility.DisplayDialog(
                    "TMP Font Baker",
                    "C:\\Windows\\Fonts\\simhei.ttf not found.\nInstall 黑体 or copy another TTF CJK font to Assets/Resources/Fonts/SimHei.ttf.",
                    "OK");
                return;
            }

            Directory.CreateDirectory("Assets/Resources/Fonts");
            File.Copy(src, dest, overwrite: true);
            AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(dest) as TrueTypeFontImporter;
            if (importer != null)
            {
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }

            EditorUtility.DisplayDialog(
                "TMP Font Baker",
                "Imported SimHei.ttf into Resources/Fonts.\nTmpFontCatalog will prefer it when SiYuan (CFF OTF) fails FontEngine.\n\nNext: StreetCat → Fonts → Bake SiYuan TMP Only (or Bake All).",
                "OK");
        }

        [MenuItem("StreetCat/Fonts/Bake SiYuan TMP Only")]
        public static void BakeSiYuanOnly()
        {
            Directory.CreateDirectory(OutputDir.Replace('\\', '/'));
            string fontPath = FindFontAssetPath("SiYuanHeiTi");
            if (string.IsNullOrEmpty(fontPath))
            {
                EditorUtility.DisplayDialog("TMP Font Baker", "SiYuanHeiTi font not found under Assets/Resources/Fonts.", "OK");
                return;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            bool ok = BakeOne(source, "SiYuanHeiTi", OutputDir + "/SiYuanHeiTi.asset", needCjk: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "TMP Font Baker",
                ok ? "SiYuanHeiTi TMP asset baked." : "Bake failed — see Console.",
                "OK");
        }

        static bool BakeOne(Font source, string name, string outPath, bool needCjk)
        {
            if (source == null) return false;

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(outPath);

            TMP_FontAsset asset = null;
            try
            {
                // Match TmpFontCatalog runtime sampling so rebakes stay sharp at UI 40–48pt.
                asset = TMP_FontAsset.CreateFontAsset(
                    source,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048,
                    AtlasPopulationMode.Dynamic,
                    true);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[TmpFontAssetBaker] CreateFontAsset exception for " + name + ": " + e.Message);
                return false;
            }

            if (asset == null)
            {
                Debug.LogError("[TmpFontAssetBaker] CreateFontAsset returned null for " + name +
                               ". Enable Include Font Data on the Font importer.");
                return false;
            }

            asset.name = name;
            asset.isMultiAtlasTexturesEnabled = true;
            asset.TryAddCharacters(Probe, out var missing);
            if (needCjk && !string.IsNullOrEmpty(missing) && missing.IndexOf('街') >= 0)
            {
                Debug.LogError("[TmpFontAssetBaker] CJK probe failed for " + name + ". missing='" + missing +
                               "'. CFF/OTF may be unsupported — try a TTF CJK face.");
                Object.DestroyImmediate(asset);
                return false;
            }

            AssetDatabase.CreateAsset(asset, outPath);

            // Persist atlas + material as sub-assets (required for Dynamic runtime use).
            if (asset.material != null)
            {
                asset.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    var tex = asset.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = name + " Atlas " + i;
                    AssetDatabase.AddObjectToAsset(tex, asset);
                }
            }

            EditorUtility.SetDirty(asset);
            Debug.Log("[TmpFontAssetBaker] Wrote " + outPath + (string.IsNullOrEmpty(missing) ? "" : " (partial missing: " + missing + ")"));
            return true;
        }

        static string FindFontAssetPath(string resourcesName)
        {
            string[] exts = { ".otf", ".ttf", ".ttc" };
            foreach (var ext in exts)
            {
                string p = "Assets/Resources/Fonts/" + resourcesName + ext;
                if (File.Exists(p)) return p;
            }

            string[] guids = AssetDatabase.FindAssets(resourcesName + " t:Font");
            for (int i = 0; i < guids.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (p.Contains("/Resources/Fonts/")) return p;
            }
            return null;
        }
    }
}
