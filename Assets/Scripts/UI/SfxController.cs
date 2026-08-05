using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// Soft procedural UI SFX (no external audio assets). Toggle with <see cref="Enabled"/>.
    /// </summary>
    public class SfxController : MonoBehaviour
    {
        public static SfxController Instance { get; private set; }
        public static bool Enabled = true;

        const float Master = 0.22f;
        AudioSource src;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        void Awake()
        {
            Instance = this;
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = false;
        }

        public void PlayAdvance() => Play("advance", 0.9f);
        public void PlayChoice() => Play("choice", 1f);
        public void PlayUi() => Play("ui", 0.85f);
        public void PlayType() => Play("type", 0.35f);
        public void PlayInspect() => Play("inspect", 0.95f);

        void Play(string key, float volScale)
        {
            if (!Enabled || src == null) return;
            var clip = GetOrCreate(key);
            if (clip == null) return;
            src.PlayOneShot(clip, Master * volScale);
        }

        AudioClip GetOrCreate(string key)
        {
            if (cache.TryGetValue(key, out var c) && c != null)
                return c;
            c = key switch
            {
                "advance" => SynthBlip(0.045f, 660f, 420f, 0.55f),
                "choice" => SynthBlip(0.06f, 520f, 780f, 0.5f),
                "ui" => SynthBlip(0.04f, 380f, 300f, 0.45f),
                "type" => SynthNoiseTick(0.012f, 0.25f),
                "inspect" => SynthBlip(0.07f, 300f, 540f, 0.5f),
                _ => SynthBlip(0.04f, 440f, 440f, 0.4f)
            };
            if (c != null) cache[key] = c;
            return c;
        }

        static AudioClip SynthBlip(float seconds, float f0, float f1, float amp)
        {
            int n = Mathf.Max(32, (int)(44100 * seconds));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / 44100f;
                float u = i / (float)(n - 1);
                float f = Mathf.Lerp(f0, f1, u);
                float env = Mathf.Exp(-u * 6.5f) * (1f - u * 0.15f);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * amp;
            }
            var clip = AudioClip.Create("sfx_" + f0, n, 1, 44100, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip SynthNoiseTick(float seconds, float amp)
        {
            int n = Mathf.Max(16, (int)(44100 * seconds));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float env = Mathf.Exp(-u * 14f);
                data[i] = (Random.value * 2f - 1f) * env * amp * 0.35f;
            }
            var clip = AudioClip.Create("sfx_type", n, 1, 44100, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
