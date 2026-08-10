using System;
using System.Collections.Generic;
using StreetCat.Narrative;
using UnityEngine;

namespace StreetCat.Loc
{
    /// <summary>
    /// English overlay for BuiltInScripts lines, keyed sceneId:lineIndex.
    /// Chinese source in C# stays authoritative; EN lives in Resources/Loc/scripts_en.json.
    /// </summary>
    public static class ScriptLoc
    {
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

        static readonly Dictionary<string, LineEntry> map = new Dictionary<string, LineEntry>();
        static bool loaded;

        static readonly Dictionary<string, string> SpeakerEn = new Dictionary<string, string>
        {
            { "小凌", "Ling" },
            { "沈禾", "Shen He" },
            { "保安叔叔", "Uncle Guard" },
            { "大福", "Dafu" },
            { "林女士", "Ms. Lin" },
            { "系统", "System" },
            { "选项", "Choice" },
            { "旁白", "" },
        };

        static readonly Dictionary<string, string> ObjectiveEn = new Dictionary<string, string>
        {
            { "寻找合适的流浪猫采访对象。", "Find a suitable stray cat to interview." },
            { "前往槐安社区寻找大福。", "Go to Huai'an Community and find Dafu." },
            { "在社区内寻找大福的线索。", "Search the community for leads on Dafu." },
            { "向保安询问大福记忆中的女人。", "Ask the guard about the woman in Dafu's memory." },
            { "采访林女士，核实大福的救助经过。", "Interview Ms. Lin and verify Dafu's rescue." },
            { "整理素材，完成报道。", "Organize materials and finish the article." },
        };

        public static void Reload()
        {
            loaded = false;
            Load();
        }

        public static ScriptLine Resolve(string sceneId, int lineIndex, ScriptLine src)
        {
            if (src == null) return null;
            if (!GameSettings.IsEnglish) return src;

            Load();
            var key = sceneId + ":" + lineIndex;
            map.TryGetValue(key, out var entry);

            var copy = CloneShallow(src);
            if (entry != null)
            {
                if (entry.text != null)
                    copy.text = entry.text;
                if (!string.IsNullOrEmpty(entry.speakerName))
                    copy.speakerName = entry.speakerName;
                else if (!string.IsNullOrEmpty(src.speakerName))
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
            else
            {
                if (!string.IsNullOrEmpty(copy.speakerName))
                    copy.speakerName = MapSpeaker(copy.speakerName);
            }

            return copy;
        }

        public static string MapSpeaker(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (!GameSettings.IsEnglish) return name;
            return SpeakerEn.TryGetValue(name, out var en) ? en : name;
        }

        public static string MapObjective(string zh)
        {
            if (string.IsNullOrEmpty(zh) || !GameSettings.IsEnglish) return zh;
            return ObjectiveEn.TryGetValue(zh, out var en) ? en : zh;
        }

        public static string SceneTitle(string sceneId, string zhTitle)
        {
            if (!GameSettings.IsEnglish || string.IsNullOrEmpty(sceneId)) return zhTitle;
            Load();
            var key = "title:" + sceneId;
            if (map.TryGetValue(key, out var e) && !string.IsNullOrEmpty(e.text))
                return e.text;
            return zhTitle;
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            map.Clear();
            var asset = Resources.Load<TextAsset>("Loc/scripts_en");
            if (asset == null)
            {
                Debug.LogWarning("[ScriptLoc] Missing Resources/Loc/scripts_en.json");
                return;
            }

            try
            {
                var file = JsonUtility.FromJson<File>(asset.text);
                if (file?.lines == null) return;
                foreach (var e in file.lines)
                {
                    if (e == null || string.IsNullOrEmpty(e.key)) continue;
                    map[e.key] = e;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[ScriptLoc] Parse failed: " + ex.Message);
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
