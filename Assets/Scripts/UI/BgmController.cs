using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Crossfading ambient BGM driven by scene / UI mode.
    /// Clips live under Resources/Audio/Bgm/ (procedural loops).
    /// </summary>
    public class BgmController : MonoBehaviour
    {
        public static BgmController Instance { get; private set; }

        /// <summary>Procedural loops are muted until replaced with better tracks.</summary>
        public static bool MusicEnabled = false;

        const float FadeSeconds = 1.4f;
        const float TargetVolume = 0.38f;

        AudioSource a;
        AudioSource b;
        bool usingA = true;
        string currentKey;
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
            var key = ResolveKey(modeHint, backgroundLabel);
            Play(key);
        }

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
                // smoothstep
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

        /// <summary>
        /// modeHint: Title / Dialogue / Investigate / Interview / Writing / Notebook / Epilogue / Talk
        /// </summary>
        public static string ResolveKey(string modeHint, string backgroundLabel)
        {
            var mode = modeHint ?? "";
            var label = (backgroundLabel ?? "").Replace("　", "").Replace(" ", "").Replace("_", "");

            if (mode == "Interview" || label.Contains("采访"))
                return "bgm_interview";

            if (mode == "Writing" || mode == "Notebook" ||
                label.Contains("写稿") || label.Contains("笔记"))
                return "bgm_writing";

            if (mode == "Epilogue" || label.Contains("后日谈") || label.Contains("几天后"))
                return "bgm_community";

            if (mode == "Investigate" || mode == "Talk" ||
                label.Contains("槐安") || label.Contains("社区") || label.Contains("保安亭") ||
                label.Contains("午后") || label.Contains("傍晚"))
                return "bgm_community";

            // Title, magazine, office, workstation
            return "bgm_magazine";
        }
    }
}
