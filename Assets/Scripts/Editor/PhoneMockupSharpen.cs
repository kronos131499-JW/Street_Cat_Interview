using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>
    /// The phone overlay draws the ~948x1659 chat mockups into a frame that is only
    /// ever 45-55% of that on screen, and every glyph in them is baked pixels, so no
    /// import setting can recover the detail the minification throws away.
    /// Pre-sharpening the source compensates for it. This is only safe because the
    /// phone frame is height-clamped and never shows this art near 1:1.
    /// </summary>
    public static class PhoneMockupSharpen
    {
        // Both folders hold byte-identical copies; they have to move together.
        static readonly string[] Folders =
        {
            "Assets/Resources/VnArt/UI/Messages",
            "Assets/Art/UI/Messages"
        };

        const string BackupFolder = "Temp/PhoneMockupBackup";

        /// <summary>Blur radius in source pixels; roughly one on-screen pixel after the ~0.5x fit.</summary>
        const int Radius = 2;
        const float Amount = 0.85f;

        [MenuItem("StreetCat/Art/Presharpen Phone Chat Mockups", priority = 23)]
        [MenuItem("街角专访/美术/手机聊天长图预锐化（缩小显示补偿）", priority = 23)]
        static void Sharpen()
        {
            var paths = CollectPaths();
            if (paths.Count == 0)
            {
                Debug.LogWarning("[StreetCat] 预锐化：没找到手机聊天长图。");
                return;
            }

            if (!EditorUtility.DisplayDialog("手机聊天长图预锐化",
                    $"将改写 {paths.Count} 张源图的像素（原图先备份到 {BackupFolder}）。\n"
                    + "这些图在游戏里只会缩到约一半显示，锐化是为了补偿缩放损失。\n"
                    + "可用「还原」菜单或 git checkout 撤销。",
                    "开始", "取消"))
                return;

            int done = 0, skipped = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];
                    if (EditorUtility.DisplayCancelableProgressBar("StreetCat 预锐化",
                            path, (float)i / paths.Count))
                        break;

                    // A backup already on disk means this file was sharpened before;
                    // stacking passes would ring badly.
                    if (File.Exists(BackupPathFor(path)))
                    {
                        skipped++;
                        continue;
                    }

                    if (SharpenFile(path)) done++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log($"[StreetCat] 预锐化完成：处理 {done} 张，跳过 {skipped} 张（已锐化过）。"
                      + $"\n原图备份：{Path.GetFullPath(BackupFolder)}");
        }

        [MenuItem("StreetCat/Art/Restore Phone Chat Mockups", priority = 24)]
        [MenuItem("街角专访/美术/手机聊天长图还原", priority = 24)]
        static void Restore()
        {
            int done = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var path in CollectPaths())
                {
                    var backup = BackupPathFor(path);
                    if (!File.Exists(backup)) continue;
                    File.Copy(backup, AbsolutePath(path), overwrite: true);
                    File.Delete(backup);
                    done++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log($"[StreetCat] 已还原 {done} 张手机聊天长图。"
                      + (done == 0 ? "\n没有找到备份；如果备份被清掉了，用 git checkout 还原。" : ""));
        }

        static bool SharpenFile(string assetPath)
        {
            var full = AbsolutePath(assetPath);
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(full);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StreetCat] 读取失败 {assetPath}: {e.Message}");
                return false;
            }

            // Decode outside the AssetDatabase so the importer's readable/compression
            // settings don't decide what pixels we get back.
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: false);
            try
            {
                if (!tex.LoadImage(bytes, markNonReadable: false))
                {
                    Debug.LogWarning($"[StreetCat] 解码失败 {assetPath}");
                    return false;
                }

                var pixels = tex.GetPixels32();
                UnsharpMask(pixels, tex.width, tex.height);
                tex.SetPixels32(pixels);
                tex.Apply(updateMipmaps: false);

                var encoded = tex.EncodeToPNG();
                if (encoded == null || encoded.Length == 0)
                {
                    Debug.LogWarning($"[StreetCat] 编码失败 {assetPath}");
                    return false;
                }

                var backup = BackupPathFor(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.WriteAllBytes(backup, bytes);
                File.WriteAllBytes(full, encoded);
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        /// <summary>
        /// RGB only. Alpha carries the rounded phone corners, and sharpening it would
        /// leave a bright rim once the frame is composited over the dim wash.
        /// </summary>
        static void UnsharpMask(Color32[] pixels, int width, int height)
        {
            int count = width * height;
            var channel = new float[count];
            var blurred = new float[count];
            var scratch = new float[count];

            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < count; i++)
                    channel[i] = Channel(pixels[i], c);

                BoxBlurHorizontal(channel, scratch, width, height);
                BoxBlurVertical(scratch, blurred, width, height);

                for (int i = 0; i < count; i++)
                {
                    float v = channel[i] + Amount * (channel[i] - blurred[i]);
                    SetChannel(ref pixels[i], c, (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255));
                }
            }
        }

        static void BoxBlurHorizontal(float[] src, float[] dst, int width, int height)
        {
            int window = Radius * 2 + 1;
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int k = -Radius; k <= Radius; k++)
                        sum += src[row + Mathf.Clamp(x + k, 0, width - 1)];
                    dst[row + x] = sum / window;
                }
            }
        }

        static void BoxBlurVertical(float[] src, float[] dst, int width, int height)
        {
            int window = Radius * 2 + 1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int k = -Radius; k <= Radius; k++)
                        sum += src[Mathf.Clamp(y + k, 0, height - 1) * width + x];
                    dst[y * width + x] = sum / window;
                }
            }
        }

        static float Channel(Color32 c, int index) => index == 0 ? c.r : index == 1 ? c.g : c.b;

        static void SetChannel(ref Color32 c, int index, byte value)
        {
            if (index == 0) c.r = value;
            else if (index == 1) c.g = value;
            else c.b = value;
        }

        static List<string> CollectPaths()
        {
            var result = new List<string>();
            foreach (var folder in Folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        result.Add(path);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        static string AbsolutePath(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        static string BackupPathFor(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", BackupFolder,
                assetPath.Replace('/', '_')));
    }
}
