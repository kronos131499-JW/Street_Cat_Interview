using System;
using System.Collections.Generic;
using System.Text;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Interview;
using UnityEngine;

namespace StreetCat.Notebook
{
    [Serializable]
    public class NotebookNote
    {
        public string id;
        public string text;
        public string source;
    }

    [Serializable]
    public class NotebookQaEntry
    {
        public string topicId;
        public string question;
        public string answerSummary;
        public string speaker;
    }

    [Serializable]
    public class NotebookTopic
    {
        public string id;
        public string title;
        public TopicStatus status = TopicStatus.Untouched;
        public List<NotebookNote> notes = new List<NotebookNote>();
        public List<string> bullets = new List<string>();
        public string inspiration;
        public bool inspirationIsInvestigate;
        public string hintQuestion;
    }

    /// <summary>
    /// Chapter 1 reporter notebook: 6 interview topics, ○/◐/● status,
    /// single inspiration prompt, and interview Q&amp;A association.
    /// </summary>
    public class ReporterNotebook : MonoBehaviour
    {
        public const int FormatVersion = 2;

        public static ReporterNotebook Instance { get; private set; }

        public List<NotebookTopic> Topics { get; private set; }
        public List<NotebookQaEntry> QaLog { get; private set; }

        static readonly string[] TopicOrder =
        {
            "community", "past", "neck", "rescuer", "after", "return"
        };

        void Awake()
        {
            Instance = this;
            InitEmpty();
            GameState.Ensure();
            GameState.Instance.OnIntelGained += OnIntel;
            RefreshFromState();
        }

        void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnIntelGained -= OnIntel;
        }

        void InitEmpty()
        {
            Topics = new List<NotebookTopic>
            {
                new NotebookTopic { id = "community", title = "大福的社区生活", hintQuestion = "大福平时一般什么时候会来这里？" },
                new NotebookTopic { id = "past", title = "过去的大福", hintQuestion = "你以前也会来保安亭这边吗？" },
                new NotebookTopic { id = "neck", title = "脖子上的伤", hintQuestion = "你脖子以前是不是受过伤？" },
                new NotebookTopic { id = "rescuer", title = "大福的救助者", hintQuestion = "她来过很多次吗？" },
                new NotebookTopic { id = "after", title = "被带走以后", hintQuestion = "被带走以后，你去了哪里？" },
                new NotebookTopic { id = "return", title = "大福的回归", hintQuestion = "是谁把你带回这里的？" },
            };
            QaLog = new List<NotebookQaEntry>();
        }

        public void ResetNotebook()
        {
            InitEmpty();
            Persist();
        }

        public void LoadFromSave()
        {
            InitEmpty();
            var data = GameState.Instance?.Data;
            if (data == null)
            {
                RefreshFromState();
                return;
            }

            if (data.notebookQa != null)
                QaLog = new List<NotebookQaEntry>(data.notebookQa);

            bool legacy = data.notebookFormat < FormatVersion;
            if (data.topics != null)
            {
                foreach (var saved in data.topics)
                {
                    if (saved == null || string.IsNullOrEmpty(saved.id)) continue;
                    var t = Topics.Find(x => x.id == saved.id);
                    if (t == null) continue;
                    if (!string.IsNullOrEmpty(saved.title))
                        t.title = saved.title;
                    int st = saved.status;
                    if (legacy)
                    {
                        // Old: 0 Untouched, 1 Partial, 2 Deep
                        if (st == 1) st = (int)TopicStatus.Open;
                        else if (st == 2) st = (int)TopicStatus.Complete;
                    }
                    if (st < 0) st = 0;
                    if (st > (int)TopicStatus.Complete) st = (int)TopicStatus.Complete;
                    t.status = (TopicStatus)st;
                    if (saved.noteIds != null && saved.noteIds.Count > 0 && saved.bullets != null)
                    {
                        for (int i = 0; i < saved.noteIds.Count && i < saved.bullets.Count; i++)
                        {
                            string src = (saved.sources != null && i < saved.sources.Count) ? saved.sources[i] : "";
                            AddNoteRaw(t, saved.noteIds[i], saved.bullets[i], src);
                        }
                    }
                    else if (saved.bullets != null)
                    {
                        for (int i = 0; i < saved.bullets.Count; i++)
                            AddNoteRaw(t, "legacy_" + i, saved.bullets[i], "");
                    }
                }
            }

            RefreshFromState();
        }

        void OnIntel(string id) => RefreshFromState();

        /// <summary>Recompute visible notes / status / inspiration from intel &amp; flags.</summary>
        public void RefreshFromState()
        {
            var gs = GameState.Instance;
            if (gs == null) return;

            bool found = gs.HasFlag(FlagIds.FoundDafu) || gs.HasIntel(IntelIds.DafuWasRescued);

            // --- community ---
            if (found)
            {
                UnlockNote("community", "c_social_1", "每天下午会出现在社区门口附近。", "社交媒体", TopicStatus.New);
                UnlockNote("community", "c_social_2", "居民叫它「编外保安」。", "社交媒体", TopicStatus.New);
                UnlockNote("community", "c_social_3", "现在有人给它换水、喂饭、搭窝。", "社交媒体", TopicStatus.New);
            }
            if (gs.HasIntel(IntelIds.FixedFeedingPoint))
                UnlockNote("community", "c_feed", "社区有长期维护的投喂点和猫屋。", "现场调查", TopicStatus.Open);
            if (gs.HasIntel(IntelIds.DafuRestSpot))
                UnlockNote("community", "c_locker", "快递柜顶留有橘色猫毛，是大福常待的位置。", "现场调查", TopicStatus.Open);
            if (gs.HasIntel(IntelIds.DafuAppearTime))
                UnlockNote("community", "c_time", "大福通常下午四五点出现。", "保安叔叔", TopicStatus.Open);
            if (gs.HasIntel(IntelIds.DafuNearGuard) || gs.HasIntel(IntelIds.DafuAppearTime))
                UnlockNote("community", "c_rest", "天气好时会在快递柜上休息。", "保安叔叔", TopicStatus.Open);
            if (gs.HasIntel(IntelIds.DafuNoOwner) || gs.HasIntel(IntelIds.CommunityCare))
                UnlockNote("community", "c_owner", "没有固定主人，多名居民会投喂、换水；保安也会顺手照看。", "保安叔叔", TopicStatus.Open);
            if (gs.HasIntel(IntelIds.TabbyPartner))
                UnlockNote("community", "c_tabby", "大福经常和一只狸花猫一起吃饭、休息和活动。", "大福", TopicStatus.Open);

            bool communityArea = gs.HasIntel(IntelIds.DafuRestSpot) || gs.HasIntel(IntelIds.DafuAppearTime) || gs.HasIntel(IntelIds.DafuNearGuard);
            if (communityArea && gs.HasIntel(IntelIds.DafuNoOwner) && gs.HasIntel(IntelIds.CommunityCare))
                SetStatusMin("community", TopicStatus.Complete);

            // --- past ---
            if (gs.HasIntel(IntelIds.DafuBecameGuardCat) || gs.HasIntel(IntelIds.PastAfraid))
            {
                UnlockNote("past", "p_guard_1", "大福从医院回来以前很怕人。", "保安叔叔", TopicStatus.New);
                UnlockNote("past", "p_guard_2", "陌生人靠近时会立刻躲开。", "保安叔叔", TopicStatus.New);
            }
            if (gs.HasIntel(IntelIds.PastAfraid))
            {
                UnlockNote("past", "p_dafu_1", "以前很怕人，人靠近就会跑。", "大福", TopicStatus.Open);
                UnlockNote("past", "p_dafu_2", "那时候不会像现在一样主动靠近保安亭。", "大福", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.FeedFourDays) || (gs.HasIntel(IntelIds.PastAfraid) && gs.HasIntel(IntelIds.LinIdentity)))
            {
                UnlockNote("past", "p_lin", "林女士第一次发现大福时，它非常警惕，完全无法直接靠近。", "林女士", TopicStatus.Complete);
                SetStatusMin("past", TopicStatus.Complete);
            }

            // --- neck ---
            if (found)
            {
                UnlockNote("neck", "n_social_1", "大福以前脖子受过很严重的伤。", "社交媒体", TopicStatus.New);
                UnlockNote("neck", "n_social_2", "具体情况帖子里没有写清楚。", "社交媒体", TopicStatus.New);
            }
            if (gs.HasIntel(IntelIds.DafuBecameGuardCat) || gs.HasIntel(IntelIds.DafuWasRescued))
                UnlockNote("neck", "n_guard", "保安只知道它因此被人送去治疗，具体情况不清楚。", "保安叔叔", TopicStatus.New);
            if (gs.HasIntel(IntelIds.NeckPain) || gs.HasIntel(IntelIds.NeckObject) ||
                gs.HasIntel(IntelIds.NeckObjectTight) || gs.HasIntel(IntelIds.NeckLongTermPain))
            {
                UnlockNote("neck", "n_dafu_1", "大福记得脖子曾经长期很疼。", "大福", TopicStatus.Open);
                UnlockNote("neck", "n_dafu_2", "有某种很紧的东西一直勒着它。", "大福", TopicStatus.Open);
                UnlockNote("neck", "n_dafu_3", "自己怎么也弄不掉。", "大福", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.FeedFourDays) || gs.HasIntel(IntelIds.CaptureSuccess))
            {
                UnlockNote("neck", "n_lin_see_1", "林女士看到大福脖子上缠着一根较粗的麻绳，并且已经出血。", "林女士", TopicStatus.Open);
                UnlockNote("neck", "n_lin_see_2", "几天内伤口持续恶化。", "林女士", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.RopeEmbedded))
            {
                UnlockNote("neck", "n_hospital_1", "医院确认粗麻绳已经嵌入颈部组织。", "林女士（转述医院）", TopicStatus.Complete);
                UnlockNote("neck", "n_hospital_2", "伤口存在坏死和严重感染，需要尽快处理。", "林女士（转述医院）", TopicStatus.Complete);
                SetStatusMin("neck", TopicStatus.Complete);
            }
            if (gs.HasIntel(IntelIds.CauseUnknown))
                UnlockNote("neck", "n_cause", "没有人看到麻绳最初是如何套上去的，目前无法确认是否存在人为伤害。", "林女士", TopicStatus.Complete);

            // --- rescuer ---
            if (gs.HasIntel(IntelIds.RepeatedFeeding) || gs.HasIntel(IntelIds.WomanClue))
                UnlockNote("rescuer", "r_woman", "大福记得有一个女人曾经给它带来食物。", "大福", TopicStatus.New);
            if (gs.HasIntel(IntelIds.RepeatedFeeding) &&
                (gs.HasIntel(IntelIds.TakenAway) || gs.HasIntel(IntelIds.CaptureParticipant) || gs.HasIntel(IntelIds.ReturnedDafu)))
            {
                UnlockNote("rescuer", "r_multi_1", "那个女人连续很多次带来食物。", "大福", TopicStatus.Open);
                UnlockNote("rescuer", "r_multi_2", "她总会把食物放下，再退远。", "大福", TopicStatus.Open);
                if (gs.HasIntel(IntelIds.TakenAway) || gs.HasIntel(IntelIds.CaptureParticipant))
                    UnlockNote("rescuer", "r_multi_3", "后来她和其他人一起抓住大福。", "大福", TopicStatus.Open);
                if (gs.HasIntel(IntelIds.ReturnedDafu))
                    UnlockNote("rescuer", "r_multi_4", "大福恢复后，也是她把大福带回社区。", "大福", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.LinIdentity))
            {
                SetTitle("rescuer", "林女士");
                UnlockNote("rescuer", "r_id_1", "当年把大福送去治疗的人是社区居民林女士。", "保安叔叔", TopicStatus.Open);
                UnlockNote("rescuer", "r_id_2", "保安确认，大福康复后也是由她送回社区。", "保安叔叔", TopicStatus.Open);
            }
            else
            {
                SetTitle("rescuer", "大福的救助者");
            }
            if (gs.HasIntel(IntelIds.FeedFourDays) && gs.HasIntel(IntelIds.CaptureSuccess))
            {
                UnlockNote("rescuer", "r_lin_1", "林女士连续四晚带食物寻找大福，每次放下后主动退远。", "林女士", TopicStatus.Complete);
                UnlockNote("rescuer", "r_lin_2", "她一边投喂一边观察伤势。", "林女士", TopicStatus.Complete);
                UnlockNote("rescuer", "r_lin_3", "发现伤势持续恶化后，她联系有救助经验的人协助抓捕。", "林女士", TopicStatus.Complete);
                SetStatusMin("rescuer", TopicStatus.Complete);
            }

            // --- after ---
            if (gs.HasIntel(IntelIds.TakenAway) || gs.HasIntel(IntelIds.CaptureParticipant))
                UnlockNote("after", "a_taken", "大福记得自己曾经被人抓住，并被带离熟悉的社区。", "大福", TopicStatus.New);
            if (gs.HasIntel(IntelIds.BrightStrangePlace))
            {
                UnlockNote("after", "a_place_1", "那里很亮，气味很重。", "大福", TopicStatus.Open);
                UnlockNote("after", "a_place_2", "周围有很多陌生的人和动物。", "大福", TopicStatus.Open);
                UnlockNote("after", "a_place_3", "有人碰过它的脖子。", "大福", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.Sleep) || gs.HasIntel(IntelIds.ObjectGone))
            {
                UnlockNote("after", "a_sleep_1", "大福曾经睡着很长一段时间。", "大福", TopicStatus.Open);
                UnlockNote("after", "a_sleep_2", "醒来后，原本勒住脖子的东西已经不见了。", "大福", TopicStatus.Open);
                UnlockNote("after", "a_sleep_3", "后来又有一段时间没有力气、不想吃东西。", "大福", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.PanleukopeniaDay3) || gs.HasIntel(IntelIds.RopeEmbedded))
            {
                UnlockNote("after", "a_hosp_1", "大福被送往宠物医院。", "林女士", TopicStatus.Open);
                UnlockNote("after", "a_hosp_2", "医院检查后认为需要尽快进行颈部手术。", "林女士（转述医院）", TopicStatus.Open);
                UnlockNote("after", "a_hosp_3", "手术本身较为顺利。", "林女士", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.PanleukopeniaDay3) && gs.HasIntel(IntelIds.TotalCost))
            {
                UnlockNote("after", "a_cost_1", "大福住院第三天被确诊猫瘟。", "林女士（转述医院）", TopicStatus.Complete);
                UnlockNote("after", "a_cost_2", "手术、住院和后续治疗总费用接近一万元。", "林女士", TopicStatus.Complete);
                UnlockNote("after", "a_cost_3", "大福最终恢复。", "林女士", TopicStatus.Complete);
                SetStatusMin("after", TopicStatus.Complete);
            }
            if (gs.HasIntel(IntelIds.LinHesitated))
                UnlockNote("after", "a_hesitate", "面对费用和不确定的结果，林女士承认自己曾犹豫过是否继续治疗，但最后仍选择继续。", "林女士", TopicStatus.Complete);

            // --- return ---
            if (found)
            {
                UnlockNote("return", "ret_social_1", "大福治好以后又回到了原来的社区。", "社交媒体", TopicStatus.New);
                UnlockNote("return", "ret_social_2", "帖子没有说明为什么。", "社交媒体", TopicStatus.New);
            }
            if (gs.HasIntel(IntelIds.DafuBecameGuardCat) || gs.HasIntel(IntelIds.DafuNearGuard))
            {
                UnlockNote("return", "ret_guard_1", "大福从医院回来后，起初还是躲着人。", "保安叔叔", TopicStatus.Open);
                UnlockNote("return", "ret_guard_2", "后来逐渐靠近固定投喂点和保安亭。", "保安叔叔", TopicStatus.Open);
                UnlockNote("return", "ret_guard_3", "大家慢慢开始认识它。", "保安叔叔", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.ReturnedDafu))
            {
                UnlockNote("return", "ret_dafu_1", "大福记得最后是那个反复送食物的女人把它带回社区。", "大福", TopicStatus.Open);
                UnlockNote("return", "ret_dafu_2", "它没有长期住进女人家里，但不知道原因。", "大福", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.FourCatsHome) || gs.HasIntel(IntelIds.CannotFifth))
            {
                UnlockNote("return", "ret_lin_1", "林女士家中已经有四只猫，还需要照顾女儿。", "林女士", TopicStatus.Open);
                UnlockNote("return", "ret_lin_2", "她认为自己的空间、精力和经济能力无法长期承担第五只猫。", "林女士", TopicStatus.Open);
                UnlockNote("return", "ret_lin_3", "大福原本就在槐安社区活动，社区也已有固定投喂。", "林女士", TopicStatus.Open);
            }
            if (gs.HasIntel(IntelIds.ReturnOriginalArea) &&
                (gs.HasIntel(IntelIds.CommunityCare) || gs.HasIntel(IntelIds.FourCatsHome)))
            {
                UnlockNote("return", "ret_done_1", "林女士确认社区有人持续照看后，将大福送回原活动区域。", "林女士", TopicStatus.Complete);
                UnlockNote("return", "ret_done_2", "大福没有离开，逐渐固定在保安亭附近生活。", "林女士", TopicStatus.Complete);
                UnlockNote("return", "ret_done_3", "后来有居民换水、添粮、搭猫屋，保安也会照看它。", "现场调查", TopicStatus.Complete);
                SetStatusMin("return", TopicStatus.Complete);
            }

            RecomputeInspirations();
            SyncBullets();
            Persist();
        }

        void RecomputeInspirations()
        {
            var gs = GameState.Instance;
            foreach (var t in Topics)
            {
                t.inspiration = null;
                t.inspirationIsInvestigate = false;
                t.hintQuestion = null;
                if (t.status == TopicStatus.Untouched || t.status == TopicStatus.Complete)
                    continue;

                switch (t.id)
                {
                    case "community":
                        if (t.status == TopicStatus.New)
                        {
                            t.inspiration = "去社区看看大福平时在哪里活动。";
                            t.inspirationIsInvestigate = true;
                        }
                        else
                        {
                            t.inspiration = "大福平时一般什么时候会来这里？";
                            t.hintQuestion = t.inspiration;
                        }
                        break;
                    case "past":
                        if (t.status == TopicStatus.New)
                        {
                            t.inspiration = "采访大福时，可以问问它以前会不会靠近这里的人。";
                            t.hintQuestion = "你以前也会来保安亭这边吗？";
                        }
                        else
                        {
                            t.inspiration = "你以前也会来保安亭这边吗？";
                            t.hintQuestion = t.inspiration;
                        }
                        break;
                    case "neck":
                        if (gs.HasIntel(IntelIds.NeckObject) || gs.HasIntel(IntelIds.NeckPain))
                        {
                            if (gs.HasIntel(IntelIds.FeedFourDays) || gs.HasIntel(IntelIds.CaptureSuccess))
                            {
                                t.inspiration = "送到医院以后，医生怎么说它的伤？";
                                t.hintQuestion = t.inspiration;
                            }
                            else
                            {
                                t.inspiration = "勒着你的东西是什么感觉？你还记得吗？";
                                t.hintQuestion = t.inspiration;
                            }
                        }
                        else
                        {
                            t.inspiration = "你脖子以前是不是受过伤？";
                            t.hintQuestion = t.inspiration;
                        }
                        break;
                    case "rescuer":
                        if (gs.HasIntel(IntelIds.LinIdentity))
                        {
                            t.inspiration = "您当时为什么连续几天给大福送吃的？";
                            t.hintQuestion = t.inspiration;
                        }
                        else if (t.status == TopicStatus.Open)
                        {
                            t.inspiration = "大福不知道她是谁。也许可以拿这些特征去问认识大福的人。";
                            t.inspirationIsInvestigate = true;
                        }
                        else
                        {
                            t.inspiration = "她来过很多次吗？";
                            t.hintQuestion = t.inspiration;
                        }
                        break;
                    case "after":
                        if (gs.HasIntel(IntelIds.Sleep) || gs.HasIntel(IntelIds.ObjectGone))
                        {
                            if (gs.HasIntel(IntelIds.LinIdentity) || gs.HasFlag(FlagIds.LinUnlocked))
                            {
                                t.inspiration = "手术以后，大福恢复得怎么样？";
                                t.hintQuestion = t.inspiration;
                            }
                            else
                            {
                                t.inspiration = "大福只能记得身体感受，无法解释发生了什么。需要询问当时参与救助的人。";
                                t.inspirationIsInvestigate = true;
                            }
                        }
                        else if (gs.HasIntel(IntelIds.BrightStrangePlace))
                        {
                            t.inspiration = "你在那里后来发生了什么？";
                            t.hintQuestion = t.inspiration;
                        }
                        else
                        {
                            t.inspiration = "被带走以后，你去了哪里？";
                            t.hintQuestion = t.inspiration;
                        }
                        break;
                    case "return":
                        if (gs.HasIntel(IntelIds.FourCatsHome) || gs.HasIntel(IntelIds.CannotFifth))
                        {
                            t.inspiration = "决定把大福送回社区之前，您考虑了哪些情况？";
                            t.hintQuestion = t.inspiration;
                        }
                        else if (gs.HasIntel(IntelIds.ReturnedDafu))
                        {
                            t.inspiration = "大福不知道为什么自己会被送回来，需要询问林女士。";
                            t.inspirationIsInvestigate = true;
                        }
                        else if (t.status == TopicStatus.Open)
                        {
                            t.inspiration = "是谁把你带回这里的？";
                            t.hintQuestion = t.inspiration;
                        }
                        else
                        {
                            t.inspiration = "先确认它回到社区以后是什么状态。";
                            t.inspirationIsInvestigate = true;
                        }
                        break;
                }
            }
        }

        void UnlockNote(string topicId, string noteId, string text, string source, TopicStatus minStatus)
        {
            var t = Topics.Find(x => x.id == topicId);
            if (t == null) return;
            if (t.status == TopicStatus.Untouched)
                t.status = minStatus == TopicStatus.Complete ? TopicStatus.New : minStatus;
            AddNoteRaw(t, noteId, text, source);
            SetStatusMin(topicId, minStatus);
        }

        static void AddNoteRaw(NotebookTopic t, string noteId, string text, string source)
        {
            if (t.notes.Exists(n => n.id == noteId))
                return;
            if (t.notes.Exists(n => n.text == text))
                return;
            t.notes.Add(new NotebookNote { id = noteId, text = text, source = source ?? "" });
        }

        void SetStatusMin(string topicId, TopicStatus min)
        {
            var t = Topics.Find(x => x.id == topicId);
            if (t == null || t.status == TopicStatus.Untouched) return;
            if ((int)t.status < (int)min)
                t.status = min;
        }

        void SetTitle(string topicId, string title)
        {
            var t = Topics.Find(x => x.id == topicId);
            if (t != null) t.title = title;
        }

        void SyncBullets()
        {
            foreach (var t in Topics)
            {
                t.bullets.Clear();
                foreach (var n in t.notes)
                    t.bullets.Add(n.text);
            }
        }

        public IEnumerable<NotebookTopic> VisibleTopics()
        {
            foreach (var id in TopicOrder)
            {
                var t = Topics.Find(x => x.id == id);
                if (t != null && t.status != TopicStatus.Untouched)
                    yield return t;
            }
        }

        public static string StatusMark(TopicStatus s)
        {
            switch (s)
            {
                case TopicStatus.Complete: return "●";
                case TopicStatus.Open: return "◐";
                case TopicStatus.New: return "○";
                default: return "·";
            }
        }

        public static string StatusLabel(TopicStatus s)
        {
            switch (s)
            {
                case TopicStatus.Complete: return "已充分了解";
                case TopicStatus.Open: return "还有疑问";
                case TopicStatus.New: return "新线索";
                default: return "未发现";
            }
        }

        public string SourcesLine(NotebookTopic t)
        {
            if (t == null || t.notes.Count == 0) return "";
            var set = new List<string>();
            foreach (var n in t.notes)
            {
                if (string.IsNullOrEmpty(n.source)) continue;
                if (!set.Contains(n.source)) set.Add(n.source);
            }
            return set.Count == 0 ? "" : "来源：" + string.Join(" / ", set.ToArray());
        }

        public List<NotebookQaEntry> QaForTopic(string topicId)
        {
            var list = new List<NotebookQaEntry>();
            if (QaLog == null) return list;
            foreach (var q in QaLog)
            {
                if (q != null && q.topicId == topicId)
                    list.Add(q);
            }
            return list;
        }

        public List<string> PendingGaps()
        {
            var gs = GameState.Instance;
            return gs == null ? new List<string>() : new List<string>(gs.Data.pendingQuestions);
        }

        public void AddGap(string q)
        {
            GameState.Instance.AddPendingQuestion(q);
            Persist();
        }

        /// <summary>Associate a free-interview exchange with the matching notebook topic.</summary>
        public void RecordInterviewExchange(InterviewSubject subject, string question, InterviewReply reply)
        {
            if (reply == null || string.IsNullOrWhiteSpace(question)) return;
            string topicId = TopicIdFromIntent(subject, reply.intent);
            if (string.IsNullOrEmpty(topicId))
                topicId = TopicIdFromIntel(reply.unlockedIntel);

            if (!string.IsNullOrEmpty(topicId))
            {
                var t = Topics.Find(x => x.id == topicId);
                if (t != null && t.status == TopicStatus.Untouched)
                    SetStatusMin(topicId, TopicStatus.New);
            }

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(reply.behavior))
                sb.Append(reply.behavior);
            if (reply.replyLines != null)
            {
                foreach (var line in reply.replyLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(line.Trim());
                }
            }
            string summary = sb.ToString();
            if (summary.Length > 120)
                summary = summary.Substring(0, 117) + "…";

            var entry = new NotebookQaEntry
            {
                topicId = topicId ?? "",
                question = question.Trim(),
                answerSummary = summary,
                speaker = subject == InterviewSubject.Dafu ? "大福" : "林女士"
            };

            // Dedupe identical consecutive Q
            if (QaLog.Count > 0)
            {
                var last = QaLog[QaLog.Count - 1];
                if (last.question == entry.question && last.topicId == entry.topicId)
                {
                    last.answerSummary = entry.answerSummary;
                    Persist();
                    RefreshFromState();
                    return;
                }
            }

            QaLog.Add(entry);
            if (QaLog.Count > 40)
                QaLog.RemoveAt(0);

            if (!string.IsNullOrEmpty(reply.newQuestion))
                AddGap(reply.newQuestion);

            RefreshFromState();
        }

        static string TopicIdFromIntent(InterviewSubject subject, string intent)
        {
            if (string.IsNullOrEmpty(intent)) return null;
            if (subject == InterviewSubject.Dafu)
            {
                switch (intent)
                {
                    case "daily": return "community";
                    case "past_fear": return "past";
                    case "neck": return "neck";
                    case "woman": return "rescuer";
                    case "capture": return "rescuer";
                    case "strange_place": return "after";
                    case "return": return "return";
                    case "cognitive_boundary": return "after";
                }
            }
            else
            {
                switch (intent)
                {
                    case "discovery": return "past";
                    case "injury": return "neck";
                    case "feeding": return "rescuer";
                    case "capture": return "rescuer";
                    case "hospital": return "after";
                    case "cost": return "after";
                    case "hesitate": return "after";
                    case "release": return "return";
                    case "release_accuse": return "return";
                    case "community": return "community";
                    case "cause_unknown": return "neck";
                }
            }
            return null;
        }

        string TopicIdFromIntel(List<string> intel)
        {
            if (intel == null) return null;
            foreach (var id in intel)
            {
                switch (id)
                {
                    case IntelIds.DafuNearGuard:
                    case IntelIds.TabbyPartner:
                    case IntelIds.CommunityCare:
                    case IntelIds.DafuNoOwner:
                        return "community";
                    case IntelIds.PastAfraid:
                        return "past";
                    case IntelIds.NeckPain:
                    case IntelIds.NeckObject:
                    case IntelIds.NeckObjectTight:
                    case IntelIds.NeckLongTermPain:
                    case IntelIds.RopeEmbedded:
                    case IntelIds.CauseUnknown:
                        return "neck";
                    case IntelIds.RepeatedFeeding:
                    case IntelIds.WomanClue:
                    case IntelIds.CaptureParticipant:
                    case IntelIds.FeedFourDays:
                    case IntelIds.CaptureSuccess:
                    case IntelIds.LinIdentity:
                        return "rescuer";
                    case IntelIds.TakenAway:
                    case IntelIds.BrightStrangePlace:
                    case IntelIds.Sleep:
                    case IntelIds.ObjectGone:
                    case IntelIds.PanleukopeniaDay3:
                    case IntelIds.TotalCost:
                    case IntelIds.LinHesitated:
                    case IntelIds.CognitiveBoundary:
                        return "after";
                    case IntelIds.ReturnedDafu:
                    case IntelIds.FourCatsHome:
                    case IntelIds.CannotFifth:
                    case IntelIds.ReturnOriginalArea:
                        return "return";
                }
            }
            return null;
        }

        public string GetInspirationQuestion(string topicId)
        {
            var t = Topics.Find(x => x.id == topicId);
            if (t == null) return null;
            if (!string.IsNullOrEmpty(t.hintQuestion))
                return t.hintQuestion;
            if (!t.inspirationIsInvestigate)
                return t.inspiration;
            return null;
        }

        /// <summary>
        /// Short askable presets for free interview chips: incomplete notebook topics first,
        /// then a tiny static fallback. Investigate-only inspirations are skipped.
        /// Prefer <see cref="StreetCat.Interview.InterviewHintService"/> for Play Mode tips;
        /// this remains a notebook-only candidate source.
        /// </summary>
        public List<string> GetPresetAskQuestions(InterviewSubject subject, int max = 4)
        {
            return GetContextualAskQuestions(subject, null, max, includeFallbacks: true);
        }

        /// <summary>
        /// Rank incomplete notebook ask-questions for a subject. Open topics first, then New.
        /// Skips topics whose primary intent was already asked this interview when possible.
        /// </summary>
        public List<string> GetContextualAskQuestions(
            InterviewSubject subject,
            IReadOnlyCollection<string> askedIntents,
            int max = 4,
            bool includeFallbacks = false)
        {
            var list = new List<string>();
            if (Topics == null || max <= 0 || subject == InterviewSubject.None)
                return list;

            void TryAdd(string q)
            {
                if (list.Count >= max) return;
                if (string.IsNullOrWhiteSpace(q)) return;
                q = q.Trim();
                if (list.Contains(q)) return;
                if (!FitsInterviewSubject(subject, q)) return;
                list.Add(q);
            }

            // Pass 1: Open (还有疑问) — highest value "what to try next".
            foreach (var t in VisibleTopics())
            {
                if (list.Count >= max) break;
                if (t.status != TopicStatus.Open) continue;
                if (TopicLikelyAsked(subject, t.id, askedIntents)) continue;
                TryAdd(GetInspirationQuestion(t.id));
            }

            // Pass 2: New clues.
            foreach (var t in VisibleTopics())
            {
                if (list.Count >= max) break;
                if (t.status != TopicStatus.New) continue;
                if (TopicLikelyAsked(subject, t.id, askedIntents)) continue;
                TryAdd(GetInspirationQuestion(t.id));
            }

            // Pass 3: any remaining incomplete even if intent was asked (different phrasing).
            if (list.Count < max)
            {
                foreach (var t in VisibleTopics())
                {
                    if (list.Count >= max) break;
                    if (t.status == TopicStatus.Complete) continue;
                    TryAdd(GetInspirationQuestion(t.id));
                }
            }

            if (includeFallbacks && list.Count < 2)
            {
                foreach (var q in FallbackPresets(subject))
                {
                    if (list.Count >= max) break;
                    TryAdd(q);
                }
            }

            return list;
        }

        static bool TopicLikelyAsked(
            InterviewSubject subject, string topicId, IReadOnlyCollection<string> askedIntents)
        {
            if (askedIntents == null || askedIntents.Count == 0 || string.IsNullOrEmpty(topicId))
                return false;
            foreach (var intent in IntentsForTopic(subject, topicId))
            {
                foreach (var a in askedIntents)
                {
                    if (string.Equals(a, intent, StringComparison.Ordinal))
                        return true;
                }
            }
            return false;
        }

        static IEnumerable<string> IntentsForTopic(InterviewSubject subject, string topicId)
        {
            if (subject == InterviewSubject.Dafu)
            {
                switch (topicId)
                {
                    case "community": yield return "daily"; break;
                    case "past": yield return "past_fear"; break;
                    case "neck": yield return "neck"; break;
                    case "rescuer":
                        yield return "woman";
                        yield return "capture";
                        break;
                    case "after":
                        yield return "strange_place";
                        yield return "cognitive_boundary";
                        break;
                    case "return": yield return "return"; break;
                }
            }
            else
            {
                switch (topicId)
                {
                    case "community": yield return "community"; break;
                    case "past": yield return "discovery"; break;
                    case "neck":
                        yield return "injury";
                        yield return "cause_unknown";
                        break;
                    case "rescuer":
                        yield return "feeding";
                        yield return "capture";
                        break;
                    case "after":
                        yield return "hospital";
                        yield return "cost";
                        yield return "hesitate";
                        break;
                    case "return":
                        yield return "release";
                        yield return "release_accuse";
                        break;
                }
            }
        }

        static bool FitsInterviewSubject(InterviewSubject subject, string q)
        {
            bool linTone = q.IndexOf('您') >= 0
                           || q.Contains("医生")
                           || q.Contains("手术")
                           || q.Contains("决定把")
                           || q.Contains("连续几天");
            bool catTone = q.IndexOf('你') >= 0
                           || q.Contains("勒着")
                           || q.Contains("保安亭");
            if (subject == InterviewSubject.Lin)
                return linTone || (!catTone && (q.Contains("大福") || q.Contains("她")));
            return !linTone;
        }

        static IEnumerable<string> FallbackPresets(InterviewSubject subject)
        {
            if (subject == InterviewSubject.Lin)
            {
                yield return "您是怎么注意到大福的？";
                yield return "为什么连续几天给它送吃的？";
                yield return "送到医院以后怎么样？";
                yield return "为什么又把它送回社区？";
            }
            else
            {
                yield return "你平时一般什么时候会来这里？";
                yield return "你以前也会来保安亭这边吗？";
                yield return "你脖子以前是不是受过伤？";
                yield return "有没有人经常来找你？";
            }
        }

        public string BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("【记者笔记】");
            foreach (var t in VisibleTopics())
            {
                sb.AppendLine($"{StatusMark(t.status)} {t.title}　（{StatusLabel(t.status)}）");
                foreach (var n in t.notes)
                    sb.AppendLine("  · " + n.text);
                var src = SourcesLine(t);
                if (!string.IsNullOrEmpty(src))
                    sb.AppendLine("  " + src);
                if (!string.IsNullOrEmpty(t.inspiration) && t.status != TopicStatus.Complete)
                    sb.AppendLine((t.inspirationIsInvestigate ? "  🔎 " : "  ✦ ") + t.inspiration);
                sb.AppendLine();
            }
            var gaps = PendingGaps();
            if (gaps.Count > 0)
            {
                sb.AppendLine("待确认：");
                foreach (var q in gaps)
                    sb.AppendLine("  ? " + q);
            }
            return sb.ToString().TrimEnd();
        }

        void Persist()
        {
            if (GameState.Instance == null) return;
            var list = new List<NotebookTopicSave>();
            foreach (var t in Topics)
            {
                var save = new NotebookTopicSave
                {
                    id = t.id,
                    title = t.title,
                    status = (int)t.status,
                    bullets = new List<string>(),
                    noteIds = new List<string>(),
                    sources = new List<string>()
                };
                foreach (var n in t.notes)
                {
                    save.noteIds.Add(n.id);
                    save.bullets.Add(n.text);
                    save.sources.Add(n.source ?? "");
                }
                list.Add(save);
            }
            GameState.Instance.Data.topics = list;
            GameState.Instance.Data.notebookQa = new List<NotebookQaEntry>(QaLog);
            GameState.Instance.Data.notebookFormat = FormatVersion;
            GameState.Instance.Notify();
        }

        // Kept for any external callers that still use Touch / bullet style.
        public void Touch(string topicId, string bullet, TopicStatus minStatus)
        {
            UnlockNote(topicId, "touch_" + bullet.GetHashCode(), bullet, "", minStatus);
            SyncBullets();
            Persist();
        }
    }
}
