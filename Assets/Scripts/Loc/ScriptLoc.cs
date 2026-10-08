using System;
using System.Collections.Generic;
using StreetCat.Narrative;
using UnityEngine;

namespace StreetCat.Loc
{
    /// <summary>
    /// Per-language dialogue text, keyed sceneId:lineIndex.
    /// Chinese source in C# stays the base. English base is Resources/Loc/scripts_en.json.
    /// Play-mode edits land only in the active language:
    /// scripts_overrides_zh.json or scripts_overrides_en.json.
    /// </summary>
    public static class ScriptLoc
    {
        public sealed class LineEditSnapshot
        {
            public string key;
            public string text;
            public string speakerName;
            public string[] choiceLabels;
            public bool hasUserOverride;
            public string referenceText;
            public string referenceSpeaker;
            public bool editable;
        }

        [Serializable]
        class File
        {
            public List<LineEntry> lines = new List<LineEntry>();
        }

        [Serializable]
        class LineEntry
        {
            public string key;
            public string text;
            public string speakerName;
            public string[] choices;
        }

        static readonly Dictionary<string, LineEntry> enBase = new Dictionary<string, LineEntry>();
        static readonly Dictionary<string, LineEntry> enUser = new Dictionary<string, LineEntry>();
        static readonly Dictionary<string, LineEntry> zhUser = new Dictionary<string, LineEntry>();
        static readonly Dictionary<string, LineEntry> sessionEn = new Dictionary<string, LineEntry>();
        static readonly Dictionary<string, LineEntry> sessionZh = new Dictionary<string, LineEntry>();
        static readonly HashSet<string> tombEn = new HashSet<string>();
        static readonly HashSet<string> tombZh = new HashSet<string>();
        static readonly HashSet<string> deletedLines = new HashSet<string>();
        static bool loaded;

        static readonly Dictionary<string, string> SpeakerEn = new Dictionary<string, string>
        {
            { "小凌", "Ling" },
            { "沈禾", "Shen He" },
            { "保安叔叔", "Security Guard" },
            { "大福", "Dafu" },
            { "林女士", "Ms. Lin" },
            { "林敏", "Lin Min" },
            { "系统", "System" },
            { "选项", "Choice" },
            { "旁白", "" },
        };

        static readonly Dictionary<string, string> ObjectiveEn = new Dictionary<string, string>
        {
            { "寻找合适的流浪猫采访对象。", "Find a suitable stray cat to interview." },
            { "前往槐安社区寻找大福。", "Go to Huai'an Community and find Dafu." },
            { "在社区内寻找大福的线索。", "Search the community for leads on Dafu." },
            { "向保安询问大福的情况。", "Ask the guard about Dafu." },
            { "向保安询问大福记忆中的女人。", "Ask the guard about the woman in Dafu's memory." },
            { "等待大福出现。", "Wait for Dafu to appear." },
            { "采访林女士，核实大福的救助经过。", "Interview Ms. Lin and verify Dafu's rescue." },
            { "明天下午15:00前往咖啡馆采访林女士。", "Interview Ms. Lin at the café tomorrow at 15:00." },
            { "等待林女士回复。", "Wait for Ms. Lin's reply." },
            { "明天下午三点，在槐安社区南门外的咖啡馆见林敏。", "Meet Lin Min at the café outside Huai’an Community’s south gate tomorrow at 3 p.m." },
            { "整理素材，完成报道。", "Organize materials and finish the article." },
            { "完成周五的工作。", "Finish Friday's work." },
            { "补充采访大福，补齐写稿所需素材。", "Re-interview Dafu to fill in the materials the article needs." },
            { "补充采访林女士，补齐写稿所需素材。", "Re-interview Ms. Lin to fill in the materials the article needs." },
            { "采访大福，了解它的过去。", "Interview Dafu and learn about his past." },
            { "采访林女士，核实救助经过。", "Interview Ms. Lin and verify the rescue." },
            { "等待大福上班，试着用翻译器对话。", "Wait for Dafu to clock in, then try talking through the translator." },
            { "前往咖啡馆见林女士。", "Go to the café to meet Ms. Lin." },
            { "去沈禾办公室一趟。", "Stop by Shen He's office." },
            { "向保安打听救助者的线索。", "Ask the guard for leads on the rescuer." },
            { "找到编辑部的保安猫。", "Find the guard cat for the editorial team." },
        };

        /// <summary>Segments of background labels such as「槐安社区_午后」, shown in the scene-title toast.</summary>
        static readonly Dictionary<string, string> LocationEn = new Dictionary<string, string>
        {
            { "编辑部", "Editorial Office" },
            { "编辑部工位", "Editorial Desk" },
            { "工位", "Desk" },
            { "沈禾办公室", "Shen He's Office" },
            { "槐安社区", "Huai'an Community" },
            { "社区平面图", "Community Map" },
            { "保安亭", "Guard Booth" },
            { "咖啡馆", "Café" },
            { "上午", "Morning" },
            { "午后", "Afternoon" },
            { "傍晚", "Evening" },
        };

        public static void Reload()
        {
            loaded = false;
            Load();
        }

        public static ScriptLine Resolve(string sceneId, int lineIndex, ScriptLine src)
        {
            if (src == null) return null;
            if (!GameSettings.IsEnglish)
                return ResolveLanguage(sceneId, lineIndex, src, english: false);
            return ResolveLanguage(sceneId, lineIndex, src, english: true);
        }

        /// <summary>
        /// Investigation beats are not script lines. Key is stable, e.g. inv:cat_house:0.
        /// Base text is the Chinese source; English starts from the hard-text table.
        /// </summary>
        public static LineEditSnapshot DescribeFree(string key, string baseText, string baseSpeaker, bool english)
        {
            return DescribeFree(key, baseText, baseSpeaker, english, null);
        }

        public static LineEditSnapshot DescribeFree(string key, string baseText, string baseSpeaker, bool english, string[] baseChoices)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            Load();
            var shown = Effective(english, key, out var user);
            var other = Effective(!english, key, out _);
            string shownText = shown != null && shown.text != null ? shown.text : BaseText(baseText, english);
            string shownSpeaker = shown != null && !string.IsNullOrEmpty(shown.speakerName)
                ? shown.speakerName
                : SpeakerFor(baseSpeaker, english);
            string otherText = other != null && other.text != null ? other.text : BaseText(baseText, !english);
            string otherSpeaker = other != null && !string.IsNullOrEmpty(other.speakerName)
                ? other.speakerName
                : SpeakerFor(baseSpeaker, !english);

            return new LineEditSnapshot
            {
                key = key,
                text = shownText ?? "",
                speakerName = shownSpeaker ?? "",
                choiceLabels = ResolveChoiceLabels(shown, baseChoices, english),
                hasUserOverride = user && shown != null,
                referenceText = otherText ?? "",
                referenceSpeaker = otherSpeaker ?? "",
                editable = !string.IsNullOrEmpty(baseText) || (baseChoices != null && baseChoices.Length > 0)
            };
        }

        /// <summary>Player-facing override for one free line. False keeps the Chinese source.</summary>
        public static bool TryGetUserText(string key, out string text, out string speaker)
        {
            text = null;
            speaker = null;
            if (string.IsNullOrEmpty(key)) return false;
            Load();
            var entry = Effective(GameSettings.IsEnglish, key, out _);
            if (entry == null) return false;
            text = entry.text;
            speaker = entry.speakerName;
            return text != null;
        }

        public static string DisplayChoice(string key, int index, string source)
        {
            Load();
            var entry = Effective(GameSettings.IsEnglish, key, out _);
            if (entry?.choices != null && index >= 0 && index < entry.choices.Length
                && !string.IsNullOrEmpty(entry.choices[index]))
                return entry.choices[index];
            return source ?? "";
        }

        static string[] ResolveChoiceLabels(LineEntry shown, string[] baseChoices, bool english)
        {
            if (baseChoices == null || baseChoices.Length == 0)
                return Array.Empty<string>();
            var labels = new string[baseChoices.Length];
            for (int i = 0; i < baseChoices.Length; i++)
            {
                if (shown?.choices != null && i < shown.choices.Length && !string.IsNullOrEmpty(shown.choices[i]))
                    labels[i] = shown.choices[i];
                else
                    labels[i] = BaseText(baseChoices[i], english);
            }
            return labels;
        }

        static string BaseText(string zh, bool english)
        {
            if (!english || string.IsNullOrEmpty(zh)) return zh ?? "";
            return HardTextLoc.TryEnglish(zh, out var en) ? en : zh;
        }

        static string SpeakerFor(string name, bool english)
        {
            if (!english || string.IsNullOrEmpty(name)) return name ?? "";
            return SpeakerEn.TryGetValue(name, out var en) ? en : name;
        }

        /// <summary>What the dialogue editor should show for the active language of this line.</summary>
        public static LineEditSnapshot Describe(string sceneId, int lineIndex, ScriptLine src, bool english)
        {
            if (src == null || string.IsNullOrEmpty(sceneId) || lineIndex < 0)
                return null;

            Load();
            var key = sceneId + ":" + lineIndex;
            var entry = Effective(english, key, out var user);
            var shown = ResolveLanguage(sceneId, lineIndex, src, english);
            var other = ResolveLanguage(sceneId, lineIndex, src, !english);
            var labels = shown.choices == null
                ? Array.Empty<string>()
                : shown.choices.ConvertAll(c => c != null ? c.label ?? "" : "").ToArray();

            return new LineEditSnapshot
            {
                key = key,
                text = shown.text ?? "",
                speakerName = shown.speakerName ?? "",
                choiceLabels = labels,
                hasUserOverride = user && entry != null,
                referenceText = other.text ?? "",
                referenceSpeaker = other.speakerName ?? "",
                editable = IsEditableSource(src)
            };
        }

        /// <summary>Remember an edit for this play session. Disk writes stay in the editor tool.</summary>
        public static void SetUserOverride(bool english, string key, string text, string speakerName, string[] choices)
        {
            if (string.IsNullOrEmpty(key)) return;
            Load();
            var entry = new LineEntry
            {
                key = key,
                text = text ?? "",
                speakerName = speakerName ?? "",
                choices = CopyChoices(choices)
            };
            var session = english ? sessionEn : sessionZh;
            var tomb = english ? tombEn : tombZh;
            var file = english ? enUser : zhUser;
            session[key] = entry;
            file[key] = entry;
            tomb.Remove(key);
        }

        /// <summary>Drop the user override so this line falls back to the language base.</summary>
        public static void ClearUserOverride(bool english, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            Load();
            (english ? sessionEn : sessionZh).Remove(key);
            (english ? enUser : zhUser).Remove(key);
            (english ? tombEn : tombZh).Add(key);
        }

        public static string MapSpeaker(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var trimmed = name.Trim().TrimEnd('：', ':').Trim();
            if (string.IsNullOrEmpty(trimmed)) return "";
            if (!GameSettings.IsEnglish) return trimmed;
            if (SpeakerEn.TryGetValue(trimmed, out var en)) return en;
            foreach (var kv in SpeakerEn)
            {
                if (kv.Key.Length > 0 && trimmed.StartsWith(kv.Key, StringComparison.Ordinal))
                    return kv.Value;
            }
            return HardTextLoc.T(trimmed);
        }

        public static string MapObjective(string zh)
        {
            if (string.IsNullOrEmpty(zh) || !GameSettings.IsEnglish) return zh;
            return ObjectiveEn.TryGetValue(zh, out var en) ? en : HardTextLoc.T(zh);
        }

        /// <summary>Display form of a background label: segments split on '_'.</summary>
        public static string MapLocation(string label)
        {
            if (string.IsNullOrEmpty(label)) return label ?? "";
            if (!GameSettings.IsEnglish) return label.Replace("_", "　");
            var parts = label.Split('_');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = LocationEn.TryGetValue(parts[i], out var en) ? en : HardTextLoc.T(parts[i]);
            return string.Join(" · ", parts);
        }

        public static string SceneTitle(string sceneId, string zhTitle)
        {
            if (!GameSettings.IsEnglish || string.IsNullOrEmpty(sceneId)) return zhTitle;
            Load();
            var key = "title:" + sceneId;
            var e = Effective(true, key, out _);
            if (e != null && !string.IsNullOrEmpty(e.text))
                return e.text;
            return zhTitle;
        }

        static ScriptLine ResolveLanguage(string sceneId, int lineIndex, ScriptLine src, bool english)
        {
            Load();
            var key = sceneId + ":" + lineIndex;
            var entry = Effective(english, key, out _);
            if (!english && entry == null)
                return src;
            return ApplyEntry(src, entry, mapSpeakerFallback: english);
        }

        static LineEntry Effective(bool english, string key, out bool userOverride)
        {
            userOverride = false;
            var session = english ? sessionEn : sessionZh;
            var tomb = english ? tombEn : tombZh;
            var file = english ? enUser : zhUser;

            if (session.TryGetValue(key, out var live))
            {
                userOverride = true;
                return live;
            }

            if (tomb.Contains(key))
            {
                if (!english) return null;
                return enBase.TryGetValue(key, out var fallen) ? fallen : null;
            }

            if (file.TryGetValue(key, out var saved))
            {
                userOverride = true;
                return saved;
            }

            if (!english) return null;
            return enBase.TryGetValue(key, out var baseline) ? baseline : null;
        }

        static ScriptLine ApplyEntry(ScriptLine src, LineEntry entry, bool mapSpeakerFallback)
        {
            var copy = CloneShallow(src);
            if (entry != null)
            {
                if (entry.text != null
                    && !ScriptMetaCue.IsProductionMetaText(entry.text)
                    && !ScriptMetaCue.IsStructuredCueSource(src))
                    copy.text = entry.text;
                if (!string.IsNullOrEmpty(entry.speakerName))
                    copy.speakerName = entry.speakerName;
                else if (mapSpeakerFallback && !string.IsNullOrEmpty(src.speakerName))
                    copy.speakerName = MapSpeaker(src.speakerName);

                if (src.choices != null && src.choices.Count > 0)
                {
                    copy.choices = new List<ScriptChoice>(src.choices.Count);
                    for (int i = 0; i < src.choices.Count; i++)
                    {
                        var c = src.choices[i];
                        var nc = new ScriptChoice
                        {
                            label = c.label,
                            nextSceneId = c.nextSceneId,
                            setFlag = c.setFlag,
                            grantIntel = c.grantIntel,
                            setObjective = c.setObjective
                        };
                        if (entry.choices != null && i < entry.choices.Length && !string.IsNullOrEmpty(entry.choices[i]))
                            nc.label = entry.choices[i];
                        copy.choices.Add(nc);
                    }
                }
            }
            else if (mapSpeakerFallback && !string.IsNullOrEmpty(copy.speakerName))
            {
                copy.speakerName = MapSpeaker(copy.speakerName);
            }

            return copy;
        }

        static bool IsEditableSource(ScriptLine src)
        {
            if (src == null || ScriptMetaCue.IsStructuredCueSource(src))
                return false;
            if (!string.IsNullOrEmpty(ScriptMetaCue.SanitizeDisplayText(src.text)))
                return true;
            return src.choices != null && src.choices.Count > 0;
        }

        static string[] CopyChoices(string[] choices)
        {
            if (choices == null || choices.Length == 0)
                return Array.Empty<string>();
            var copy = new string[choices.Length];
            for (int i = 0; i < choices.Length; i++)
                copy[i] = choices[i] ?? "";
            return copy;
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            enBase.Clear();
            enUser.Clear();
            zhUser.Clear();
            LoadFile("Loc/scripts_en", enBase, required: true);
            LoadFile("Loc/scripts_overrides_en", enUser, required: false);
            LoadFile("Loc/scripts_overrides_zh", zhUser, required: false);
            LoadDeleted();
        }

        public static bool IsLineDeleted(string sceneId, int lineIndex)
        {
            if (string.IsNullOrEmpty(sceneId) || lineIndex < 0) return false;
            Load();
            return deletedLines.Contains(sceneId + ":" + lineIndex);
        }

        public static void SetLineDeleted(string sceneId, int lineIndex, bool deleted)
        {
            if (string.IsNullOrEmpty(sceneId) || lineIndex < 0) return;
            Load();
            var key = sceneId + ":" + lineIndex;
            if (deleted) deletedLines.Add(key);
            else deletedLines.Remove(key);
        }

        public static string[] DeletedLineKeys()
        {
            Load();
            var keys = new string[deletedLines.Count];
            deletedLines.CopyTo(keys);
            return keys;
        }

        [Serializable]
        class DeletedFile
        {
            public List<string> keys = new List<string>();
        }

        static void LoadDeleted()
        {
            deletedLines.Clear();
            var asset = Resources.Load<TextAsset>("Loc/scripts_deleted");
            if (asset == null || string.IsNullOrEmpty(asset.text)) return;
            try
            {
                var file = JsonUtility.FromJson<DeletedFile>(asset.text);
                if (file?.keys == null) return;
                for (int i = 0; i < file.keys.Count; i++)
                {
                    if (!string.IsNullOrEmpty(file.keys[i]))
                        deletedLines.Add(file.keys[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ScriptLoc] scripts_deleted.json unreadable: " + ex.Message);
            }
        }

        static void LoadFile(string resourcePath, Dictionary<string, LineEntry> into, bool required)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                if (required)
                    Debug.LogWarning("[ScriptLoc] Missing Resources/" + resourcePath + ".json");
                return;
            }

            try
            {
                var file = JsonUtility.FromJson<File>(asset.text);
                if (file?.lines == null) return;
                foreach (var e in file.lines)
                {
                    if (e == null || string.IsNullOrEmpty(e.key)) continue;
                    into[e.key] = e;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[ScriptLoc] Parse failed for " + resourcePath + ": " + ex.Message);
            }
        }

        static ScriptLine CloneShallow(ScriptLine src)
        {
            return new ScriptLine
            {
                speaker = src.speaker,
                speakerName = src.speakerName,
                text = src.text,
                portrait = src.portrait,
                background = src.background,
                bgm = src.bgm,
                sfx = src.sfx,
                prop = src.prop,
                hideProp = src.hideProp,
                social = src.social,
                setFlag = src.setFlag,
                grantIntel = src.grantIntel,
                noteLine = src.noteLine,
                setObjective = src.setObjective,
                nextSceneId = src.nextSceneId,
                openInvestigation = src.openInvestigation,
                openTalkMenu = src.openTalkMenu,
                openWriting = src.openWriting,
                openInterview = src.openInterview,
                choices = src.choices
            };
        }
    }
}
