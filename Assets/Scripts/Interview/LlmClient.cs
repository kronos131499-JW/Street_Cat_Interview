using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace StreetCat.Interview
{
    /// <summary>Optional OpenAI-compatible client. Rule engine remains source of truth; LLM only rephrases.</summary>
    public class LlmClient : MonoBehaviour
    {
        public static LlmClient Instance { get; private set; }

        [SerializeField] string endpoint = "https://api.deepseek.com/v1/chat/completions";
        [SerializeField] string model = "deepseek-chat";
        [Tooltip("Leave empty to disable LLM and use rule-engine lines only.")]
        [SerializeField] string apiKeyEnvVar = "STREETCAT_LLM_API_KEY";

        string apiKey;

        void Awake()
        {
            Instance = this;
            apiKey = Environment.GetEnvironmentVariable(apiKeyEnvVar);
            if (string.IsNullOrEmpty(apiKey))
                apiKey = PlayerPrefs.GetString("STREETCAT_LLM_API_KEY", "");
        }

        public bool IsConfigured => !string.IsNullOrEmpty(apiKey);

        public void SetApiKey(string key)
        {
            apiKey = key;
            PlayerPrefs.SetString("STREETCAT_LLM_API_KEY", key ?? "");
        }

        public IEnumerator RephraseCoroutine(string stylePrompt, string factsBlock, string playerQuestion, Action<string> onDone)
        {
            if (!IsConfigured)
            {
                onDone?.Invoke(null);
                yield break;
            }

            var body = new ChatRequest
            {
                model = model,
                temperature = 0.4f,
                messages = new[]
                {
                    new ChatMessage { role = "system", content = stylePrompt },
                    new ChatMessage { role = "user", content = "可用事实：\n" + factsBlock + "\n\n玩家问题：" + playerQuestion + "\n只输出角色回答，不要解释。" }
                }
            };

            var json = JsonUtility.ToJson(body);
            using (var req = new UnityWebRequest(endpoint, "POST"))
            {
                byte[] raw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(raw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[LlmClient] " + req.error);
                    onDone?.Invoke(null);
                    yield break;
                }
                try
                {
                    var resp = JsonUtility.FromJson<ChatResponse>(req.downloadHandler.text);
                    var text = resp?.choices != null && resp.choices.Length > 0
                        ? resp.choices[0].message?.content
                        : null;
                    onDone?.Invoke(text);
                }
                catch
                {
                    onDone?.Invoke(null);
                }
            }
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
