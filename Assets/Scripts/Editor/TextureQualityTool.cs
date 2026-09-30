using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>
    /// Batch-fixes the import settings that make runtime art look soft:
    /// block compression on painted art, the 2048 clamp on oversized source
    /// files, stale per-platform overrides, and Point filtering on scaled UI.
    /// </summary>
    public static class TextureQualityTool
    {
        // Only these roots ship art. TextMesh Pro samples are left alone.
        static readonly string[] Roots = { "Assets/Resources", "Assets/Art" };
        static readonly string[] SkipContains = { "/TextMesh Pro/" };

        // Platforms whose overrides would otherwise silently win over the default.
        static readonly string[] PlatformNames =
        {
            "Standalone", "Windows Store Apps", "WebGL", "Android", "iPhone", "tvOS", "PS4", "PS5"
        };

        enum Mode
        {
            /// <summary>RGBA32 — pixel-exact, ~4x the VRAM.</summary>
            Uncompressed,
            /// <summary>BC7 on desktop — near-lossless, same VRAM as today's DXT.</summary>
            HighQuality
        }

        [MenuItem("StreetCat/Art/Audit Texture Sharpness (report only)", priority = 20)]
        [MenuItem("街角专访/美术/图片清晰度体检（只出报告）", priority = 20)]
        static void Audit()
        {
            var report = new StringBuilder("path,width,height,maxTextureSize,compression,filterMode,issues\n");
            int soft = 0, clamped = 0, total = 0;

            foreach (var path in CollectTexturePaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                total++;

                GetSourceSize(path, out int w, out int h);
                int longest = Mathf.Max(w, h);
                var def = importer.GetDefaultPlatformTextureSettings();
                var effective = EffectiveSettings(importer, def);

                var issues = new List<string>();
                if (effective.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    issues.Add("block-compressed");
                    soft++;
                }
                if (longest > 0 && effective.maxTextureSize < longest)
                {
                    issues.Add($"downscaled {longest}->{effective.maxTextureSize}");
                    clamped++;
                }
                if (importer.filterMode == FilterMode.Point)
                    issues.Add("point-filter");
                if (!importer.alphaIsTransparency)
                    issues.Add("alpha-not-transparency");

                if (issues.Count == 0) continue;
                report.Append(path).Append(',').Append(w).Append(',').Append(h).Append(',')
                    .Append(effective.maxTextureSize).Append(',').Append(effective.textureCompression).Append(',')
                    .Append(importer.filterMode).Append(',').Append(string.Join(" | ", issues)).Append('\n');
            }

            var outPath = Path.Combine(Application.dataPath, "../Temp/TextureAudit.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, report.ToString(), new UTF8Encoding(true));
            Debug.Log($"[StreetCat] 清晰度体检：共 {total} 张，{soft} 张被块压缩，{clamped} 张被 maxTextureSize 缩小。"
                      + $"\n明细：{Path.GetFullPath(outPath)}");
        }

        [MenuItem("StreetCat/Art/Fix Texture Sharpness - Uncompressed (max quality)", priority = 21)]
        [MenuItem("街角专访/美术/图片高清重导入（不压缩·最高画质）", priority = 21)]
        static void FixUncompressed() => Apply(Mode.Uncompressed);

        [MenuItem("StreetCat/Art/Fix Texture Sharpness - BC7 (saves VRAM)", priority = 22)]
        [MenuItem("街角专访/美术/图片高清重导入（BC7·省显存）", priority = 22)]
        static void FixHighQuality() => Apply(Mode.HighQuality);

        static void Apply(Mode mode)
        {
            var paths = CollectTexturePaths();
            var changed = new List<string>();
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];
                    if (EditorUtility.DisplayCancelableProgressBar("StreetCat 高清重导入",
                            path, (float)i / Mathf.Max(1, paths.Count)))
                        break;

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    if (!ApplyTo(importer, path, mode)) continue;
                    EditorUtility.SetDirty(importer);
                    AssetDatabase.WriteImportSettingsIfDirty(path);
                    changed.Add(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log($"[StreetCat] 高清重导入完成（{mode}）：扫描 {paths.Count} 张，修改 {changed.Count} 张。"
                      + "\n重导入可能需要几分钟；完成后重新进入 Play 查看效果。");
        }

        static bool ApplyTo(TextureImporter importer, string path, Mode mode)
        {
            GetSourceSize(path, out int w, out int h);
            int wanted = MaxSizeFor(Mathf.Max(w, h));
            var compression = mode == Mode.Uncompressed
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.CompressedHQ;

            bool dirty = false;

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                dirty = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }
            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                dirty = true;
            }
            // Aniso only costs when mips exist; mipped mockups benefit, flat UI is unaffected.
            int aniso = importer.mipmapEnabled ? 4 : 1;
            if (importer.anisoLevel != aniso)
            {
                importer.anisoLevel = aniso;
                dirty = true;
            }

            var def = importer.GetDefaultPlatformTextureSettings();
            if (def.maxTextureSize != wanted || def.textureCompression != compression
                || def.compressionQuality != 100 || def.crunchedCompression)
            {
                def.maxTextureSize = wanted;
                def.textureCompression = compression;
                def.compressionQuality = 100;
                def.crunchedCompression = false;
                def.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
                importer.SetPlatformTextureSettings(def);
                dirty = true;
            }

            // Per-platform overrides are what actually ship; a stale one silently
            // re-compresses the texture no matter what the default says.
            foreach (var platform in PlatformNames)
            {
                try
                {
                    var ps = importer.GetPlatformTextureSettings(platform);
                    if (!ps.overridden) continue;
                    ps.overridden = false;
                    importer.SetPlatformTextureSettings(ps);
                    dirty = true;
                }
                catch (Exception)
                {
                    // Platform module not installed — nothing to override.
                }
            }

            return dirty;
        }

        static TextureImporterPlatformSettings EffectiveSettings(
            TextureImporter importer, TextureImporterPlatformSettings def)
        {
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            return standalone.overridden ? standalone : def;
        }

        static int MaxSizeFor(int longestSide)
        {
            if (longestSide <= 0) return 2048;
            int size = 32;
            while (size < longestSide && size < 8192) size <<= 1;
            return size;
        }

        static List<string> CollectTexturePaths()
        {
            var result = new List<string>();
            var seen = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", Roots))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !seen.Add(path)) continue;
                bool skip = false;
                foreach (var token in SkipContains)
                    if (path.Contains(token)) { skip = true; break; }
                if (skip) continue;
                result.Add(path);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        /// <summary>
        /// Reads the on-disk size. LoadAssetAtPath would report the already-clamped
        /// size, which hides exactly the downscaling this tool is meant to undo.
        /// </summary>
        static void GetSourceSize(string path, out int width, out int height)
        {
            width = height = 0;
            try
            {
                var full = Path.Combine(Application.dataPath, "..", path);
                using var stream = File.OpenRead(full);
                using var reader = new BinaryReader(stream);
                var signature = reader.ReadBytes(8);
                if (signature.Length == 8 && signature[0] == 0x89 && signature[1] == 'P'
                    && signature[2] == 'N' && signature[3] == 'G')
                {
                    reader.ReadBytes(8); // IHDR length + type
                    width = ReadBigEndianInt32(reader);
                    height = ReadBigEndianInt32(reader);
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StreetCat] 读取源图尺寸失败 {path}: {e.Message}");
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return;
            width = tex.width;
            height = tex.height;
        }

        static int ReadBigEndianInt32(BinaryReader reader)
        {
            var b = reader.ReadBytes(4);
            if (b.Length < 4) return 0;
            return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
        }
    }
}
