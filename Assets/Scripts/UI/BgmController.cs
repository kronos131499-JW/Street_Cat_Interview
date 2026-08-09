using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Crossfading BGM. Clips under Resources/Audio/Bgm/.
    /// Script 【BGM：…】 lines take priority via <see cref="PlayScriptLabel"/>.
    /// </summary>
    public class BgmController : MonoBehaviour
    {
        public static BgmController Instance { get; private set; }

        public static bool MusicEnabled = true;

        const float FadeSeconds = 1.4f;
        const float TargetVolume = 0.38f;

        AudioSource a;
        AudioSource b;
        bool usingA = true;
        string currentKey;
        string stickyScriptKey;
        Coroutine fadeCo;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        void Awake()
        {
            Instance = this;
            a = gameObject.AddComponent<AudioSource>();
            b = gameObject.AddComponent<AudioSource>();
            Configure(a);
            Configure(b);
            if (!MusicEnabled)
                StopAll();
        }

        static void Configure(AudioSource src)
        {
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
        }

        public void PlayForContext(string modeHint, string backgroundLabel)
        {
            if (!MusicEnabled)
            {
                StopAll();
                return;
            }
            if (!string.IsNullOrEmpty(stickyScriptKey))
            {
                Play(stickyScriptKey);
                return;
            }
            Play(ResolveKey(modeHint, backgroundLabel));
        }

        /// <summary>Script cue e.g. 编辑部日常_01（循环） / 淡出.</summary>
        public void PlayScriptLabel(string label)
        {
            if (!MusicEnabled)
            {
                StopAll();
                return;
            }
            if (string.IsNullOrEmpty(label)) return;

            var raw = label.Replace("　", "").Replace(" ", "").Trim();
            if (raw.Contains("淡出") || raw.Contains("停止") || raw.Contains("fade"))
            {
                stickyScriptKey = null;
                FadeOut();
                return;
            }

            var key = ResolveScriptLabel(raw);
            if (string.IsNullOrEmpty(key)) return;
            stickyScriptKey = key;
            Play(key);
        }

        public void ClearScriptSticky() => stickyScriptKey = null;

        public void StopAll()
        {
            if (fadeCo != null)
            {
                StopCoroutine(fadeCo);
                fadeCo = null;
            }
            if (a != null) { a.Stop(); a.volume = 0f; a.clip = null; }
            if (b != null) { b.Stop(); b.volume = 0f; b.clip = null; }
            currentKey = null;
        }

        public void FadeOut()
        {
            if (fadeCo != null)
                StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(FadeOutCo());
            currentKey = null;
        }

        IEnumerator FadeOutCo()
        {
            float t = 0f;
            float a0 = a != null ? a.volume : 0f;
            float b0 = b != null ? b.volume : 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / FadeSeconds);
                u = u * u * (3f - 2f * u);
                if (a != null) a.volume = Mathf.Lerp(a0, 0f, u);
                if (b != null) b.volume = Mathf.Lerp(b0, 0f, u);
                yield return null;
            }
            StopAll();
            fadeCo = null;
        }

        public void Play(string key)
        {
            if (!MusicEnabled)
            {
                StopAll();
                return;
            }
            if (string.IsNullOrEmpty(key) || key == currentKey)
                return;

            var clip = Load(key);
            if (clip == null)
            {
                Debug.LogWarning("[Bgm] Missing clip Audio/Bgm/" + key);
                return;
            }

            currentKey = key;
            var incoming = usingA ? b : a;
            var outgoing = usingA ? a : b;
            usingA = !usingA;

            incoming.clip = clip;
            incoming.volume = 0f;
            if (!incoming.isPlaying)
                incoming.Play();

            if (fadeCo != null)
                StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(Crossfade(outgoing, incoming));
        }

        IEnumerator Crossfade(AudioSource from, AudioSource to)
        {
            float t = 0f;
            float fromStart = from != null && from.isPlaying ? from.volume : 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / FadeSeconds);
                u = u * u * (3f - 2f * u);
                if (to != null) to.volume = Mathf.Lerp(0f, TargetVolume, u);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, u);
                yield return null;
            }
            if (to != null) to.volume = TargetVolume;
            if (from != null)
            {
                from.Stop();
                from.volume = 0f;
                from.clip = null;
            }
            fadeCo = null;
        }

        AudioClip Load(string key)
        {
            if (cache.TryGetValue(key, out var c) && c != null)
                return c;
            c = Resources.Load<AudioClip>("Audio/Bgm/" + key);
            if (c != null)
                cache[key] = c;
            return c;
        }

        public static string ResolveScriptLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return null;
            var s = label.Replace("（循环）", "").Replace("(循环)", "").Replace("循环", "")
                .Replace("　", "").Replace(" ", "").Trim();
            if (s.StartsWith("bgm_")) return s;

            if (s.Contains("主菜单") || s.Contains("Title")) return "bgm_title";
            if (s.Contains("专题结束") || s.Contains("epilogue")) return "bgm_epilogue";
            if (s.Contains("咖啡馆")) return "bgm_cafe";
            if (s.Contains("大福")) return "bgm_dafu";
            if (s.Contains("社区傍晚") || (s.Contains("傍晚") && s.Contains("社区"))) return "bgm_community_dusk";
            if (s.Contains("社区午后") || (s.Contains("午后") && s.Contains("社区"))) return "bgm_community_afternoon";
            if (s.Contains("沈禾")) return "bgm_shenhe_office";
            if (s.Contains("编辑部日常_02") || s.Contains("编辑部日常02") || s.Contains("日常_02"))
                return "bgm_editorial_02";
            if (s.Contains("编辑部")) return "bgm_editorial_01";

            // Legacy keys
            if (s.Contains("interview") || s.Contains("采访")) return "bgm_interview";
            if (s.Contains("writing") || s.Contains("写稿")) return "bgm_writing";
            if (s.Contains("community") || s.Contains("社区")) return "bgm_community_afternoon";
            if (s.Contains("magazine") || s.Contains("杂志")) return "bgm_editorial_01";
            return null;
        }

        /// <summary>
        /// modeHint: Title / Dialogue / Investigate / Interview / Writing / Notebook / Epilogue / Talk
        /// </summary>
        public static string ResolveKey(string modeHint, string backgroundLabel)
        {
            var mode = modeHint ?? "";
            var label = (backgroundLabel ?? "").Replace("　", "").Replace(" ", "").Replace("_", "");

            if (mode == "Title")
                return "bgm_title";

            if (mode == "Interview" || label.Contains("采访") || label.Contains("咖啡"))
            {
                if (label.Contains("林") || label.Contains("咖啡"))
                    return "bgm_cafe";
                return "bgm_dafu";
            }

            if (mode == "Writing" || mode == "Notebook" ||
                label.Contains("写稿") || label.Contains("笔记") || label.Contains("工位"))
                return "bgm_editorial_02";

            if (mode == "Epilogue" || label.Contains("后日谈") || label.Contains("几天后") || label.Contains("文章发布"))
                return "bgm_epilogue";

            if (label.Contains("沈禾") && label.Contains("办公"))
                return "bgm_shenhe_office";

            if (label.Contains("保安亭") && (label.Contains("傍晚") || label.Contains("黄昏")))
                return "bgm_community_dusk";

            if (mode == "Investigate" || mode == "Talk" ||
                label.Contains("槐安") || label.Contains("社区") || label.Contains("保安亭") ||
                label.Contains("午后") || label.Contains("傍晚"))
                return "bgm_community_afternoon";

            if (label.Contains("编辑") || label.Contains("杂志"))
                return "bgm_editorial_01";

            return "bgm_editorial_01";
        }
    }
}
