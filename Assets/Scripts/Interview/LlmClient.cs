using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StreetCat.Interview
{
    /// <summary>Optional OpenAI-compatible client. Rule engine remains source of truth; LLM only rephrases.</summary>
    public class LlmClient : MonoBehaviour
    {
        public static LlmClient Instance { get; private set; }

        public const string PrefsApiKey = "STREETCAT_LLM_API_KEY";
        public const string PrefsEndpoint = "STREETCAT_LLM_ENDPOINT";
        public const string PrefsModel = "STREETCAT_LLM_MODEL";

        const string DefaultEndpoint = "https://api.deepseek.com/v1/chat/completions";
        const string DefaultModel = "deepseek-chat";

        [SerializeField] string endpoint = DefaultEndpoint;
        [SerializeField] string model = DefaultModel;
        [Tooltip("Env var name; leave key empty in inspector. Falls back to PlayerPrefs.")]
        [SerializeField] string apiKeyEnvVar = "STREETCAT_LLM_API_KEY";
        [SerializeField] float minSecondsBetweenCalls = 2.5f;
        [SerializeField] float rateLimitCooldownSeconds = 45f;

        string apiKey;
        float nextAllowedCallTime;
        bool requestInFlight;
        static HttpClient http;

        /// <summary>Last failure reason for UI (rate limit / network / etc.). Cleared on success.</summary>
        public string LastError { get; private set; }

        public bool IsConfigured => !string.IsNullOrEmpty(apiKey);

        public bool IsCoolingDown => Time.realtimeSinceStartup < nextAllowedCallTime;

        public float SecondsUntilReady => Mathf.Max(0f, nextAllowedCallTime - Time.realtimeSinceStartup);

        public bool IsBusy => requestInFlight;

        void Awake()
        {
            Instance = this;
            TryEnableTls();
            ReloadApiKey();
        }

        static void TryEnableTls()
        {
            try
            {
                // Prefer modern TLS; some Windows/.NET setups default too low for api.openai.com.
                ServicePointManager.SecurityProtocol |=
                    SecurityProtocolType.Tls12 | (SecurityProtocolType)3072 /* Tls13 if available */;
            }
            catch
            {
                try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
                catch { /* ignore */ }
            }
        }

        static HttpClient Http
        {
            get
            {
                if (http != null) return http;
                var handler = new HttpClientHandler
                {
                    // Use system / env proxy (HTTP_PROXY / HTTPS_PROXY / Windows proxy).
                    UseProxy = true,
                    Proxy = WebRequest.DefaultWebProxy,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                if (handler.Proxy != null)
                    handler.Proxy.Credentials = CredentialCache.DefaultCredentials;

                http = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(45)
                };
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                http.DefaultRequestHeaders.UserAgent.ParseAdd("StreetCatInterview/1.0");
                return http;
            }
        }

        public void ReloadApiKey()
        {
            apiKey = Environment.GetEnvironmentVariable(apiKeyEnvVar);
            if (string.IsNullOrEmpty(apiKey))
                apiKey = PlayerPrefs.GetString(PrefsApiKey, "");
        }

        public void SetApiKey(string key)
        {
            apiKey = (key ?? "").Trim();
            PlayerPrefs.SetString(PrefsApiKey, apiKey);
            PlayerPrefs.Save();
            LastError = null;
        }

        public void SetEndpoint(string url)
        {
            endpoint = string.IsNullOrWhiteSpace(url) ? DefaultEndpoint : url.Trim();
            // Always persist explicit endpoint so old OpenAI PlayerPrefs don't linger as empty→default confusion.
            PlayerPrefs.SetString(PrefsEndpoint, endpoint);
            PlayerPrefs.Save();
        }

        public void SetModel(string modelName)
        {
            model = string.IsNullOrWhiteSpace(modelName) ? DefaultModel : modelName.Trim();
            PlayerPrefs.SetString(PrefsModel, model);
            PlayerPrefs.Save();
        }

        string ResolveEndpoint()
        {
            var env = Environment.GetEnvironmentVariable("STREETCAT_LLM_ENDPOINT");
            if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
            var pref = PlayerPrefs.GetString(PrefsEndpoint, "");
            if (!string.IsNullOrWhiteSpace(pref)) return pref.Trim();
            return string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint.Trim();
        }

        string ResolveModel()
        {
            var env = Environment.GetEnvironmentVariable("STREETCAT_LLM_MODEL");
            if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
            var pref = PlayerPrefs.GetString(PrefsModel, "");
            if (!string.IsNullOrWhiteSpace(pref)) return pref.Trim();
            return string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
        }

        public IEnumerator RephraseCoroutine(string stylePrompt, string factsBlock, string playerQuestion, Action<string> onDone)
        {
            LastError = null;
            if (!IsConfigured)
            {
                onDone?.Invoke(null);
                yield break;
            }

            if (requestInFlight)
            {
                LastError = "上一次 AI 请求仍在进行，已跳过。";
                onDone?.Invoke(null);
                yield break;
            }

            // Caller (PreferLlmInterviewReplyCo) waits out cooldown; keep a tiny safety wait here
            // instead of failing immediately (which used to surface rule lines as "first" reply).
            while (IsCoolingDown)
                yield return null;

            // factsBlock is the full user message (authoritative facts + question + output rules).
            // playerQuestion kept for API compatibility; ignored when factsBlock already includes it.
            var userContent = !string.IsNullOrEmpty(factsBlock) && factsBlock.IndexOf("【权威台词", StringComparison.Ordinal) >= 0
                ? factsBlock
                : "【权威台词/事实】\n" + factsBlock + "\n\n【记者原问】" + playerQuestion
                  + "\n只改写事实，禁止新增信息。只输出角色台词。";

            var body = new ChatRequest
            {
                model = ResolveModel(),
                temperature = 0.7f,
                messages = new[]
                {
                    new ChatMessage { role = "system", content = stylePrompt },
                    new ChatMessage { role = "user", content = userContent }
                }
            };

            var json = JsonUtility.ToJson(body);
            var url = ResolveEndpoint();
            requestInFlight = true;
            // Min interval starts after we actually send, not when the UI question was clicked.
            nextAllowedCallTime = Time.realtimeSinceStartup + minSecondsBetweenCalls;

            // HttpClient uses Windows Schannel + system proxy; more reliable than UnityWebRequest TLS here.
            var task = SendAsync(url, json, apiKey);
            while (!task.IsCompleted)
                yield return null;

            requestInFlight = false;

            if (task.IsFaulted)
            {
                var msg = task.Exception?.GetBaseException()?.Message ?? "unknown";
                LastError = DescribeTransportError(msg, url);
                Debug.LogWarning("[LlmClient] " + LastError);
                onDone?.Invoke(null);
                yield break;
            }

            var (code, rawBody, transportError) = task.Result;
            if (!string.IsNullOrEmpty(transportError))
            {
                LastError = DescribeTransportError(transportError, url);
                Debug.LogWarning("[LlmClient] " + LastError);
                onDone?.Invoke(null);
                yield break;
            }

            if (code == 429 || IsTooManyRequests(null, rawBody))
            {
                nextAllowedCallTime = Time.realtimeSinceStartup + rateLimitCooldownSeconds;
                LastError = "OpenAI 请求过多（429）。已改用规则回复，请稍后再问。";
                Debug.LogWarning("[LlmClient] 429. Body: " + Truncate(rawBody, 300));
                onDone?.Invoke(null);
                yield break;
            }

            if (code < 200 || code >= 300)
            {
                LastError = DescribeHttpError(code, null, rawBody);
                Debug.LogWarning("[LlmClient] " + LastError + " | " + Truncate(rawBody, 300));
                onDone?.Invoke(null);
                yield break;
            }

            try
            {
                var resp = JsonUtility.FromJson<ChatResponse>(rawBody);
                var text = resp?.choices != null && resp.choices.Length > 0
                    ? resp.choices[0].message?.content
                    : null;
                if (string.IsNullOrWhiteSpace(text))
                    LastError = "AI 返回为空，已用规则回复。";
                else
                    LastError = null;
                onDone?.Invoke(text);
            }
            catch (Exception e)
            {
                LastError = "AI 响应解析失败，已用规则回复。";
                Debug.LogWarning("[LlmClient] parse: " + e.Message);
                onDone?.Invoke(null);
            }
        }

        static async Task<(long code, string body, string error)> SendAsync(string url, string json, string key)
        {
            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    var resp = await Http.SendAsync(req).ConfigureAwait(false);
                    var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return ((long)resp.StatusCode, body ?? "", null);
                }
            }
            catch (Exception e)
            {
                return (0, "", e.GetBaseException().Message);
            }
        }

        static bool IsTooManyRequests(string error, string body)
        {
            var s = ((error ?? "") + " " + (body ?? "")).ToLowerInvariant();
            return s.Contains("too many requests") || s.Contains("rate_limit") || s.Contains("rate limit");
        }

        static string DescribeTransportError(string error, string url)
        {
            var s = (error ?? "").ToLowerInvariant();
            if (s.Contains("ssl") || s.Contains("tls") || s.Contains("secure channel") ||
                s.Contains("authentication failed") || s.Contains("connection was closed") ||
                s.Contains("unable to complete ssl"))
            {
                return "无法完成 HTTPS/SSL 连接（常见于网络拦截或无法直连 OpenAI）。"
                       + "请开系统代理/VPN，或把兼容转发地址设到 STREETCAT_LLM_ENDPOINT。"
                       + " 当前：" + url;
            }
            if (s.Contains("timed out") || s.Contains("timeout"))
                return "连接 OpenAI 超时。请检查网络/代理。";
            if (s.Contains("name or service") || s.Contains("resolve") || s.Contains("dns"))
                return "无法解析 API 域名。请检查 DNS/网络。";
            return "网络错误：" + error;
        }

        static string DescribeHttpError(long code, string error, string body)
        {
            if (code == 401) return "API Key 无效或未授权（401）。请重新粘贴 Key。";
            if (code == 403) return "API Key 无权限（403）。";
            if (code == 404) return "接口地址或模型不存在（404）。可检查 Endpoint/Model。";
            if (code == 429) return "OpenAI 请求过多（429）。";
            if (!string.IsNullOrEmpty(body) && body.IndexOf("insufficient_quota", StringComparison.OrdinalIgnoreCase) >= 0)
                return "OpenAI 额度不足，请检查 Billing。";
            if (!string.IsNullOrEmpty(error)) return error;
            return "AI 请求失败（HTTP " + code + "）。";
        }

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max) + "…";
        }

        [Serializable] class ChatRequest
        {
            public string model;
            public float temperature;
            public ChatMessage[] messages;
        }

        [Serializable] class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable] class ChatResponse
        {
            public Choice[] choices;
        }

        [Serializable] class Choice
        {
            public ChatMessage message;
        }
    }
}
