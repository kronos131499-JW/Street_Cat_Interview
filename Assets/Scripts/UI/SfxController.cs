using System.Collections.Generic;
using UnityEngine;

namespace StreetCat.UI
{
    /// <summary>
    /// UI / scene SFX. Prefers Resources/Audio/Sfx/ clips; falls back to soft synth.
    /// <see cref="PlayUi"/> uses click_button for all UI buttons.
    /// </summary>
    public class SfxController : MonoBehaviour
    {
        public static SfxController Instance { get; private set; }
        public static bool Enabled = true;

        const float Master = 0.45f;
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

        public void PlayAdvance() => Play("advance", 0.55f);
        public void PlayChoice() => Play("choice", 0.9f);
        public void PlayUi() => Play("ui", 1f);
        public void PlayType() => Play("type", 0.25f);
        public void PlayInspect() => Play("inspect", 0.85f);

        /// <summary>Script 【SE：…】 cue (may contain comma-separated names).</summary>
        public void PlayScriptLabel(string label)
        {
            if (!Enabled || string.IsNullOrEmpty(label)) return;
            var parts = label.Replace("、", ",").Replace("，", ",").Split(',');
            bool any = false;
            foreach (var part in parts)
            {
                var key = ResolveScriptLabel(part.Trim());
                if (string.IsNullOrEmpty(key)) continue;
                Play(key, 1f);
                any = true;
            }
            // Ambient-only cues (键盘/鸟叫等) with no mapped clip: skip silently.
        }

        void Play(string key, float volScale)
        {
            if (!Enabled || src == null) return;
            var clip = GetClip(key);
            if (clip == null) return;
            src.PlayOneShot(clip, Master * volScale);
        }

        AudioClip GetClip(string key)
        {
            if (cache.TryGetValue(key, out var c) && c != null)
                return c;

            // Logical keys → resource file stems
            var resourceKey = key switch
            {
                "ui" => "sfx_click",
                "choice" => "sfx_click",
                "advance" => "sfx_click",
                "inspect" => "sfx_click",
                _ => key
            };

            c = Resources.Load<AudioClip>("Audio/Sfx/" + resourceKey);
            if (c != null)
            {
                cache[key] = c;
                return c;
            }

            // Synth fallbacks for typewriter tick etc.
            c = key switch
            {
                "type" => SynthNoiseTick(0.012f, 0.25f),
                "advance" => SynthBlip(0.045f, 660f, 420f, 0.55f),
                "choice" => SynthBlip(0.06f, 520f, 780f, 0.5f),
                "ui" => SynthBlip(0.04f, 380f, 300f, 0.45f),
                "inspect" => SynthBlip(0.07f, 300f, 540f, 0.5f),
                _ => null
            };
            if (c != null) cache[key] = c;
            return c;
        }

        public static string ResolveScriptLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return null;
            var s = label.Replace("　", "").Replace(" ", "").Trim();
            if (s.StartsWith("sfx_")) return s;

            if (s.Contains("消息提示") || s.Contains("工作软件消息"))
                return "sfx_message";
            if (s.Contains("信息发送") || s.Contains("发送"))
                return "sfx_send";
            if (s.Contains("椅子"))
                return "sfx_chair";
            if (s.Contains("灌木") || s.Contains("窸窣"))
                return "sfx_bush";
            if (s.Contains("猫叫"))
                return "sfx_meow";
            if (s.Contains("设备启动") || s.Contains("翻译提示") || s.Contains("转译"))
                return "sfx_device";
            if (s.Contains("开门") || s.Contains("保安亭开门"))
                return "sfx_door";
            if (s.Contains("点击") || s.Contains("button") || s.Contains("按钮"))
                return "sfx_click";
            return null;
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
