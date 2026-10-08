using System;
using StreetCat.Interview;
using StreetCat.Investigation;
using StreetCat.UI;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    public static class StreetCatEditorMenus
    {
        const string LlmApiKeyPrefs = "STREETCAT_LLM_API_KEY";

        [MenuItem("街角专访/关闭所有编辑器", priority = 1)]
        [MenuItem("StreetCat/Turn Off All Editors", priority = 1)]
        public static void DisableAllPlayEditors()
        {
            UILayoutEditMode.Enabled = false;
            UILayoutEditMode.TextFocus = false;
            UILayoutEditMode.LockSelection = false;
            TitleMenuEditMode.Enabled = false;
            PortraitEditMode.Enabled = false;
            SocialEditMode.Enabled = false;
            InvestigateHotspotEditMode.Enabled = false;
            EditorApplication.delayCall += CloseEditorWindows;
            Debug.Log(ToolLang.T("[StreetCat] 已关闭所有编辑器，Game 视图点击恢复为正常游玩。",
                "[StreetCat] All editors turned off. Game-view clicks are back to normal play."));
        }

        const string ToolLangMenuZh = "街角专访/工具界面英文 (Tool UI in English)";
        const string ToolLangMenuEn = "StreetCat/Tool UI in English";

        [MenuItem(ToolLangMenuZh, priority = 0)]
        [MenuItem(ToolLangMenuEn, priority = 0)]
        static void ToggleToolLanguage()
        {
            ToolLang.English = !ToolLang.English;
            Debug.Log(ToolLang.English
                ? "[StreetCat] Dev tool UI language: English"
                : "[StreetCat] 开发工具界面语言：中文");
        }

        [MenuItem(ToolLangMenuZh, true)]
        [MenuItem(ToolLangMenuEn, true)]
        static bool ToggleToolLanguageValidate()
        {
            Menu.SetChecked(ToolLangMenuZh, ToolLang.English);
            Menu.SetChecked(ToolLangMenuEn, ToolLang.English);
            return true;
        }

        static void CloseEditorWindows()
        {
            CloseIfOpen<UILayoutEditorWindow>();
            CloseIfOpen<TitleMenuEditorWindow>();
            CloseIfOpen<PortraitLayoutEditorWindow>();
            CloseIfOpen<SocialLayoutEditorWindow>();
            CloseIfOpen<InvestigateHotspotEditorWindow>();
            CloseIfOpen<DialogueFontColorEditorWindow>();
            CloseIfOpen<DialogueTextEditorWindow>();
        }

        static void CloseIfOpen<T>() where T : EditorWindow
        {
            var windows = Resources.FindObjectsOfTypeAll<T>();
            for (var i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                    windows[i].Close();
            }
        }

        /// <summary>
        /// Older saves snapshotted the live font size on every pose save, so any element that was
        /// ever nudged stopped following the settings size/weight/spacing sliders. Layout is kept.
        /// </summary>
        [MenuItem("街角专访/清除已锁死的文字尺寸覆盖", priority = 2)]
        [MenuItem("StreetCat/Clear Pinned Text Size Overrides", priority = 2)]
        static void ClearPinnedTextStyleOverrides()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UILayoutOverrideData>(UILayoutOverrides.AssetDiskPath);
            if (asset == null || asset.entries == null)
            {
                EditorUtility.DisplayDialog("UI Layout", ToolLang.T("找不到 ", "Not found: ") + UILayoutOverrides.AssetDiskPath, "OK");
                return;
            }

            var cleared = 0;
            var report = new System.Text.StringBuilder();
            foreach (var entry in asset.entries)
            {
                if (entry == null) continue;
                if (entry.fontSize <= 1f && entry.fontWeight < 100 && !entry.overrideLetterSpacing && !entry.overrideFontStyle)
                    continue;
                report.AppendLine("  " + entry.path + "  (size " + entry.fontSize + ", weight " + entry.fontWeight + ")");
                entry.fontSize = 0f;
                entry.fontWeight = 0;
                entry.overrideLetterSpacing = false;
                entry.overrideFontStyle = false;
                cleared++;
            }

            if (cleared == 0)
            {
                EditorUtility.DisplayDialog("UI Layout", ToolLang.T("没有被锁死的文字尺寸覆盖。", "No pinned text size overrides found."), "OK");
                return;
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log(ToolLang.T("[UI Layout] 已清除 " + cleared + " 条文字样式锁定：\n",
                "[UI Layout] Cleared " + cleared + " pinned text style override(s):\n") + report);
            EditorUtility.DisplayDialog(
                "UI Layout",
                ToolLang.T("已清除 " + cleared + " 条锁死的文字尺寸/字重/字间距覆盖。\n位置与大小保持不变。\n明细见 Console。",
                    "Cleared " + cleared + " pinned text size / weight / spacing override(s).\nPositions and sizes are unchanged.\nSee the Console for details."),
                "OK");
        }

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

        [MenuItem("StreetCat/Portrait Layout Editor")]
        static void OpenPortraitLayoutEditor()
        {
            PortraitLayoutEditorWindow.Open();
        }

        [MenuItem("StreetCat/Portrait Debug Picker (F11)")]
        static void LogPortraitDebugPicker()
        {
            Debug.Log(ToolLang.T("[StreetCat] 立绘调试：Play 后按 F11；点击立绘仅预览，点「确认本句立绘」才生效，且只影响当前这一句。",
                "[StreetCat] Portrait debug: press F11 in Play Mode. Clicking a portrait only previews it; \"Confirm for this line\" applies it to the current line only."));
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
