using System;
using StreetCat.Interview;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public static class StreetCatEditorMenus
    {
        const string LlmApiKeyPrefs = "STREETCAT_LLM_API_KEY";

        [MenuItem("StreetCat/Play Chapter1 From SampleScene")]
        static void Play()
        {
            if (!EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;
        }

        [MenuItem("StreetCat/Log Persistent Save Path")]
        static void LogSavePath()
        {
            Debug.Log(Application.persistentDataPath);
        }

        [MenuItem("StreetCat/Investigate Hotspot Editor")]
        static void OpenHotspotEditor()
        {
            InvestigateHotspotEditorWindow.Open();
        }

        [MenuItem("StreetCat/LLM/Paste API Key From Clipboard")]
        static void PasteLlmApiKeyFromClipboard()
        {
            var key = (GUIUtility.systemCopyBuffer ?? "").Trim();
            if (string.IsNullOrEmpty(key))
            {
                EditorUtility.DisplayDialog(
                    "StreetCat LLM",
                    "剪贴板为空。请先复制 DeepSeek API Key（platform.deepseek.com），再运行本菜单。\n\n密钥只会写入本机 PlayerPrefs，不会进工程文件或 git。",
                    "OK");
                return;
            }

            // Default provider is DeepSeek; keep endpoint/model on DeepSeek when pasting a key.
            PlayerPrefs.SetString(LlmClient.PrefsEndpoint, "https://api.deepseek.com/v1/chat/completions");
            PlayerPrefs.SetString(LlmClient.PrefsModel, "deepseek-chat");
            PlayerPrefs.SetString(LlmApiKeyPrefs, key);
            PlayerPrefs.Save();

            if (Application.isPlaying && LlmClient.Instance != null)
            {
                LlmClient.Instance.SetEndpoint("https://api.deepseek.com/v1/chat/completions");
                LlmClient.Instance.SetModel("deepseek-chat");
                LlmClient.Instance.SetApiKey(key);
            }

            var preview = key.Length <= 8 ? "(短密钥)" : key.Substring(0, 7) + "…" + key.Substring(key.Length - 4);
            EditorUtility.DisplayDialog(
                "StreetCat LLM",
                "已切换到 DeepSeek（deepseek-chat），并保存 Key（" + preview + "）。\n停 Play 再进一次最稳妥。\n\n也可用环境变量 STREETCAT_LLM_API_KEY。",
                "OK");
            Debug.Log("[StreetCat] LLM -> DeepSeek deepseek-chat; API key saved (value not logged).");
        }

        [MenuItem("StreetCat/LLM/Clear API Key")]
        static void ClearLlmApiKey()
        {
            PlayerPrefs.DeleteKey(LlmApiKeyPrefs);
            PlayerPrefs.Save();
            if (Application.isPlaying && LlmClient.Instance != null)
                LlmClient.Instance.SetApiKey("");
            EditorUtility.DisplayDialog("StreetCat LLM", "已清除本机 PlayerPrefs 中的 LLM API Key。", "OK");
        }

        [MenuItem("StreetCat/LLM/Paste Endpoint From Clipboard")]
        static void PasteLlmEndpointFromClipboard()
        {
            var url = (GUIUtility.systemCopyBuffer ?? "").Trim();
            if (string.IsNullOrEmpty(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    "StreetCat LLM",
                    "请先复制完整的 chat/completions 地址到剪贴板，例如：\n"
                    + "https://api.openai.com/v1/chat/completions\n"
                    + "或你的兼容转发地址。\n\n"
                    + "直连失败（SSL）时，可改用本机/云端代理地址。",
                    "OK");
                return;
            }

            PlayerPrefs.SetString(LlmClient.PrefsEndpoint, url);
            PlayerPrefs.Save();
            if (Application.isPlaying && LlmClient.Instance != null)
                LlmClient.Instance.SetEndpoint(url);

            EditorUtility.DisplayDialog("StreetCat LLM", "已设置 Endpoint：\n" + url, "OK");
            Debug.Log("[StreetCat] LLM endpoint -> " + url);
        }

        [MenuItem("StreetCat/LLM/Reset Endpoint To DeepSeek Default")]
        static void ResetLlmEndpoint()
        {
            PlayerPrefs.SetString(LlmClient.PrefsEndpoint, "https://api.deepseek.com/v1/chat/completions");
            PlayerPrefs.SetString(LlmClient.PrefsModel, "deepseek-chat");
            PlayerPrefs.Save();
            if (Application.isPlaying && LlmClient.Instance != null)
            {
                LlmClient.Instance.SetEndpoint("https://api.deepseek.com/v1/chat/completions");
                LlmClient.Instance.SetModel("deepseek-chat");
            }
            EditorUtility.DisplayDialog(
                "StreetCat LLM",
                "已恢复 DeepSeek 默认：\nhttps://api.deepseek.com/v1/chat/completions\nmodel=deepseek-chat",
                "OK");
        }

        [MenuItem("StreetCat/LLM/Log Current LLM Config")]
        static void LogLlmConfig()
        {
            var key = PlayerPrefs.GetString(LlmApiKeyPrefs, "");
            var ep = PlayerPrefs.GetString(LlmClient.PrefsEndpoint, "");
            var model = PlayerPrefs.GetString(LlmClient.PrefsModel, "");
            Debug.Log("[StreetCat] LLM key set=" + (!string.IsNullOrEmpty(key))
                      + " endpoint=" + (string.IsNullOrEmpty(ep) ? "(default openai)" : ep)
                      + " model=" + (string.IsNullOrEmpty(model) ? "(default gpt-4o-mini)" : model));
        }

        [MenuItem("StreetCat/LLM/Use OpenAI gpt-4o-mini（当前默认·较快）")]
        static void UseOpenAiMini()
        {
            ApplyProvider(
                "https://api.openai.com/v1/chat/completions",
                "gpt-4o-mini",
                "已切到 OpenAI gpt-4o-mini。\n请继续使用你的 OpenAI API Key。\n\n说明：Cursor 聊天里的模型不能给游戏用；游戏只能走你自己的 API。");
        }

        [MenuItem("StreetCat/LLM/Use OpenAI gpt-4.1-nano（更快更便宜）")]
        static void UseOpenAiNano()
        {
            ApplyProvider(
                "https://api.openai.com/v1/chat/completions",
                "gpt-4.1-nano",
                "已切到 OpenAI gpt-4.1-nano（通常比 mini 更轻、限流更宽松）。\n若报 404，说明账号侧还没有该模型，再切回 gpt-4o-mini。\nKey 仍用 OpenAI。");
        }

        [MenuItem("StreetCat/LLM/Use DeepSeek deepseek-chat（国内友好·额度通常更宽）")]
        static void UseDeepSeek()
        {
            ApplyProvider(
                "https://api.deepseek.com/v1/chat/completions",
                "deepseek-chat",
                "已切到 DeepSeek。\n\n下一步：\n1. 打开 https://platform.deepseek.com 创建 API Key\n2. 复制 Key → StreetCat/LLM/Paste API Key From Clipboard\n（DeepSeek Key 与 OpenAI Key 不通用）\n\nDeepSeek 国内访问通常更稳，限流也相对少。");
        }

        static void ApplyProvider(string endpoint, string model, string message)
        {
            PlayerPrefs.SetString(LlmClient.PrefsEndpoint, endpoint);
            PlayerPrefs.SetString(LlmClient.PrefsModel, model);
            PlayerPrefs.Save();
            if (Application.isPlaying && LlmClient.Instance != null)
            {
                LlmClient.Instance.SetEndpoint(endpoint);
                LlmClient.Instance.SetModel(model);
            }
            EditorUtility.DisplayDialog("StreetCat LLM", message, "OK");
            Debug.Log("[StreetCat] LLM -> " + model + " @ " + endpoint);
        }
    }
}
