using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace StreetCat.Loc
{
    /// <summary>
    /// Maps <see cref="FontCatalog"/> options to runtime <see cref="TMP_FontAsset"/>s.
    /// Prefers pre-baked Resources TMP assets; otherwise builds dynamic SDF atlases from
    /// the same TTF/OTF sources. Always chains a CJK-capable fallback (legacy UI Text
    /// used OS substitution; TMP does not — without a fallback, Chinese becomes □).
    /// </summary>
    public static class TmpFontCatalog
    {
        const string PrebakedDir = "Fonts/TMP/";
        const string ProbeLatin = "Aa1";
        const string ProbeCjk = "街角";

        static readonly Dictionary<string, TMP_FontAsset> Cache = new Dictionary<string, TMP_FontAsset>();
        /// <summary>Keep source <see cref="Font"/>s alive so FontEngine can re-load faces for dynamic glyphs.</summary>
        static readonly List<Font> SourceKeepAlive = new List<Font>();
        static TMP_FontAsset cjkFallbackAsset;
        static TMP_FontAsset liberationAsset;
        static bool loggedMissingShader;
        static bool buildingCjkFallback;

        public static TMP_FontAsset Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) id = FontCatalog.All[0].Id;
            if (Cache.TryGetValue(id, out var cached) && cached != null)
                return cached;

            var opt = FontCatalog.Get(id);
            bool needCjk = !opt.LatinOnly;

            TMP_FontAsset asset = LoadPrebaked(opt);
            if (asset == null)
                asset = CreateFromBundledFont(opt);

            if (asset == null && (id == "system" || needCjk))
                asset = CreateFromOsFontName(needCjk);

            if (asset == null && id != "siyuan")
            {
                Debug.LogWarning("[TmpFontCatalog] Falling back to siyuan for id='" + id + "'");
                asset = Resolve("siyuan");
            }

            if (asset == null && id != "simhei")
            {
                Debug.LogWarning("[TmpFontCatalog] Falling back to simhei for id='" + id + "'");
                asset = Resolve("simhei");
            }

            if (asset == null)
            {
                Debug.LogError("[TmpFontCatalog] Failed to create TMP font for '" + id +
                               "'. Using LiberationSans (Latin-only) + CJK fallback if available.");
                asset = GetLiberationFallback();
            }

            if (asset != null)
            {
                EnsureMaterial(asset);
                AttachCjkFallbackIfNeeded(asset);
                if (!ValidateGlyphs(asset, needCjk))
                {
                    Debug.LogError("[TmpFontCatalog] Font '" + id +
                                   "' cannot render required glyphs (atlas/face). Check Console / font import.");
                }
            }

            Cache[id] = asset;
            return asset;
        }

        public static TMP_FontAsset ResolveActive() => Resolve(GameSettings.UiFontId);

        static TMP_FontAsset LoadPrebaked(FontCatalog.Option opt)
        {
            string key = !string.IsNullOrEmpty(opt.ResourcesName) ? opt.ResourcesName : opt.Id;
            var baked = Resources.Load<TMP_FontAsset>(PrebakedDir + key);
            if (baked == null) return null;

            baked.isMultiAtlasTexturesEnabled = true;
            EnsureMaterial(baked);
            return baked;
        }

        static TMP_FontAsset CreateFromBundledFont(FontCatalog.Option opt)
        {
            Font source = null;
            if (!string.IsNullOrEmpty(opt.ResourcesName))
            {
                source = Resources.Load<Font>("Fonts/" + opt.ResourcesName);
                if (source == null)
                    Debug.LogWarning("[TmpFontCatalog] Missing Resources/Fonts/" + opt.ResourcesName);
            }

            if (source == null && (string.IsNullOrEmpty(opt.ResourcesName) || opt.Id == "system"))
            {
                // Prefer bundled CJK face over Font.CreateDynamicFontFromOSFont —
                // OS dynamic fonts usually cannot be loaded by FontEngine for TMP atlases.
                source = LoadBundledCjkFont();
            }

            if (source == null)
                return null;

            var created = CreateDynamic(source, "TMP_" + opt.Id, !opt.LatinOnly);
            // SiYuanHeiTi is CFF OpenType; if FontEngine rejects it, retry TrueType SimHei if present.
            if (created == null && opt.ResourcesName == "SiYuanHeiTi")
            {
                var ttf = Resources.Load<Font>("Fonts/SimHei");
                if (ttf != null)
                    created = CreateDynamic(ttf, "TMP_" + opt.Id + "_simhei", !opt.LatinOnly);
            }
            return created;
        }

        static Font LoadBundledCjkFont()
        {
            // Prefer TTF (SimHei) when present — more reliable with TMP FontEngine than CFF OTF.
            var ttf = Resources.Load<Font>("Fonts/SimHei");
            if (ttf != null) return ttf;
            return Resources.Load<Font>("Fonts/SiYuanHeiTi");
        }

        /// <summary>
        /// OS dynamic fonts often fail FontEngine.LoadFontFace; validate before accepting.
        /// </summary>
        static TMP_FontAsset CreateFromOsFontName(bool needCjk)
        {
            var osFont = FontCatalog.ResolveSystemCjk();
            if (osFont == null) return null;

            var fromOs = CreateDynamic(osFont, "TMP_OS_CJK", needCjk);
            if (fromOs != null && ValidateGlyphs(fromOs, needCjk))
                return fromOs;

            if (fromOs != null)
                Debug.LogWarning("[TmpFontCatalog] OS font TMP asset failed glyph probe; discarding.");
            return null;
        }

        static TMP_FontAsset CreateDynamic(Font source, string name, bool needCjk)
        {
            if (source == null) return null;

            if (!SourceKeepAlive.Contains(source))
                SourceKeepAlive.Add(source);

            try
            {
                // Explicit multi-atlas: CJK exhausts a single atlas quickly.
                var asset = TMP_FontAsset.CreateFontAsset(
                    source,
                    80,
                    8,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048,
                    AtlasPopulationMode.Dynamic,
                    true);

                if (asset == null)
                {
                    Debug.LogError("[TmpFontCatalog] CreateFontAsset returned null for '" + name +
                                   "' (source='" + source.name + "'). Include Font Data must be enabled on the Font importer.");
                    return null;
                }

                asset.name = name;
                asset.hideFlags = HideFlags.DontSave;
                asset.isMultiAtlasTexturesEnabled = true;
                EnsureMaterial(asset);

                if (!ValidateGlyphs(asset, needCjk))
                {
                    Debug.LogError("[TmpFontCatalog] Dynamic font '" + name +
                                   "' failed glyph probe (needCjk=" + needCjk + "). source='" + source.name + "'.");
                    return null;
                }

                Debug.Log("[TmpFontCatalog] Created dynamic TMP font '" + name + "' from '" + source.name + "'.");
                return asset;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[TmpFontCatalog] CreateFontAsset failed for " + name + ": " + e.Message);
                return null;
            }
        }

        static bool ValidateGlyphs(TMP_FontAsset asset, bool needCjk)
        {
            if (asset == null) return false;
            if (asset.material == null || asset.material.shader == null)
                return false;

            // With fallbacks attached, primary Latin fonts may lack CJK locally — that is OK.
            string probe = needCjk ? ProbeCjk + ProbeLatin : ProbeLatin;
            if (asset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                asset.TryAddCharacters(probe, out string missing);
                if (needCjk && !string.IsNullOrEmpty(missing) && ContainsCjk(missing))
                {
                    // Still OK if a CJK fallback can provide them.
                    var cjk = cjkFallbackAsset;
                    if (cjk == null || cjk == asset || !cjk.HasCharacters(ProbeCjk))
                        return false;
                }
            }

            if (!asset.HasCharacters(ProbeLatin))
            {
                // Liberation / primary should always cover basic Latin after probe.
                if (asset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                    asset.TryAddCharacters(ProbeLatin);
                if (!asset.HasCharacters(ProbeLatin))
                    return false;
            }

            if (needCjk)
            {
                bool ok = asset.HasCharacters(ProbeCjk);
                if (!ok && asset.fallbackFontAssetTable != null)
                {
                    for (int i = 0; i < asset.fallbackFontAssetTable.Count; i++)
                    {
                        var fb = asset.fallbackFontAssetTable[i];
                        if (fb != null && fb.HasCharacters(ProbeCjk))
                        {
                            ok = true;
                            break;
                        }
                    }
                }
                if (!ok) return false;
            }

            return true;
        }

        static bool ContainsCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= 0x4E00 && c <= 0x9FFF) return true;
            }
            return false;
        }

        static void EnsureMaterial(TMP_FontAsset asset)
        {
            if (asset == null) return;
            var shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (shader == null)
                shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader == null)
            {
                if (!loggedMissingShader)
                {
                    loggedMissingShader = true;
                    Debug.LogError("[TmpFontCatalog] TMP SDF shader not found. Import TMP Essentials: Window → TextMeshPro → Import TMP Essentials.");
                }
                return;
            }

            if (asset.material == null)
            {
                var mat = new Material(shader);
                mat.name = asset.name + " Material";
                mat.hideFlags = HideFlags.DontSave;
                if (asset.atlasTexture != null)
                    mat.SetTexture(ShaderUtilities.ID_MainTex, asset.atlasTexture);
                asset.material = mat;
            }
            else if (asset.material.shader == null || asset.material.shader.name.Contains("InternalError"))
            {
                asset.material.shader = shader;
            }
        }

        static void AttachCjkFallbackIfNeeded(TMP_FontAsset asset)
        {
            if (asset == null) return;
            // If primary already has CJK in atlas (or can add it), skip.
            if (asset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                asset.TryAddCharacters(ProbeCjk);
            if (asset.HasCharacters(ProbeCjk)) return;

            var cjk = GetCjkFallback();
            if (cjk == null || cjk == asset) return;

            if (asset.fallbackFontAssetTable == null)
                asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!asset.fallbackFontAssetTable.Contains(cjk))
                asset.fallbackFontAssetTable.Add(cjk);
        }

        static TMP_FontAsset GetCjkFallback()
        {
            if (cjkFallbackAsset != null) return cjkFallbackAsset;
            if (buildingCjkFallback) return null;

            if (Cache.TryGetValue("siyuan", out var cached) && cached != null && cached.HasCharacters(ProbeCjk))
            {
                cjkFallbackAsset = cached;
                return cjkFallbackAsset;
            }

            buildingCjkFallback = true;
            try
            {
                var baked = Resources.Load<TMP_FontAsset>(PrebakedDir + "SimHei")
                            ?? Resources.Load<TMP_FontAsset>(PrebakedDir + "SiYuanHeiTi");
                if (baked != null)
                {
                    baked.isMultiAtlasTexturesEnabled = true;
                    EnsureMaterial(baked);
                    if (baked.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                        baked.TryAddCharacters(ProbeCjk + ProbeLatin);
                    if (baked.HasCharacters(ProbeCjk))
                    {
                        cjkFallbackAsset = baked;
                        return cjkFallbackAsset;
                    }
                }

                var source = LoadBundledCjkFont();
                if (source != null)
                {
                    var created = CreateDynamic(source, "TMP_cjk_fallback", needCjk: true);
                    if (created != null)
                    {
                        cjkFallbackAsset = created;
                        string cacheKey = source.name != null && source.name.IndexOf("SimHei", System.StringComparison.OrdinalIgnoreCase) >= 0
                            ? "simhei"
                            : "siyuan";
                        if (!Cache.ContainsKey(cacheKey))
                            Cache[cacheKey] = created;
                    }
                }
            }
            finally
            {
                buildingCjkFallback = false;
            }

            return cjkFallbackAsset;
        }

        static TMP_FontAsset GetLiberationFallback()
        {
            if (liberationAsset != null) return liberationAsset;
            liberationAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (liberationAsset == null)
                liberationAsset = TMP_Settings.defaultFontAsset;
            return liberationAsset;
        }

        /// <summary>
        /// Convert legacy UILetterSpacing pixel tracking to TMP characterSpacing
        /// (percentage of font size).
        /// </summary>
        public static float PixelSpacingToCharacterSpacing(float pixelSpacing, float fontSize)
        {
            if (fontSize < 1f || Mathf.Approximately(pixelSpacing, 0f)) return 0f;
            return (pixelSpacing / fontSize) * 100f;
        }
    }
}
