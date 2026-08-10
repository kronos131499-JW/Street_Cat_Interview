using System;
using System.Collections.Generic;
using System.Text;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Notebook;
using UnityEngine;

namespace StreetCat.Interview
{
    public class InterviewController : MonoBehaviour
    {
        public static InterviewController Instance { get; private set; }

        InterviewSubject subject = InterviewSubject.None;
        InterviewRuleEngine engine;
        readonly List<string> log = new List<string>();
        bool boundaryHit;
        int crossChecks;
        bool returnToWritingAfterEnd;
        readonly HashSet<string> gainedThisInterview = new HashSet<string>();
        InterviewReply lastReply;
        string lastPlayerQuestion;

        public InterviewSubject Subject => subject;
        public InterviewerStats Stats => engine?.Stats;
        public IReadOnlyList<string> Log => log;
        public bool IsReinterviewFromWriting => returnToWritingAfterEnd;
        public bool IsTranslating { get; private set; }
        public InterviewReply LastReply => lastReply;
        public string LastPlayerQuestion => lastPlayerQuestion;
        public IReadOnlyCollection<string> AskedIntents => engine?.AskedIntents;

        public event Action<InterviewReply> OnReply;
        public event Action OnEnded;

        void Awake() => Instance = this;

        /// <param name="returnToWritingAfter">
        /// When true (supplemental interview from writing), End returns to write mode
        /// instead of advancing the first-time chapter beat. Intel/materials are kept.
        /// </param>
        public void Begin(InterviewSubject who, bool returnToWritingAfter = false)
        {
            subject = who;
            returnToWritingAfterEnd = returnToWritingAfter;
            log.Clear();
            gainedThisInterview.Clear();
            boundaryHit = false;
            lastReply = null;
            lastPlayerQuestion = null;
            crossChecks = GameState.Instance.Data.crossChecksCompleted;
            SaveSystem.Autosave();

            if (who == InterviewSubject.Dafu)
                engine = new DafuRuleEngine();
            else
                engine = new LinRuleEngine();
            InterviewDebugLog.SessionStart(who);
        }

        /// <param name="deferSpeakerLines">
        /// When true, only logs the player question; call <see cref="AppendSpeakerReply"/> later
        /// (e.g. after DeepSeek returns). Rule intel/stats still apply immediately.
        /// </param>
        public InterviewReply Ask(string question, bool deferSpeakerLines = false)
        {
            if (engine == null)
                return null;

            log.Add("小凌：" + question);
            lastPlayerQuestion = question;
            var reply = engine.Process(question);
            lastReply = reply;

            if (!deferSpeakerLines)
                AppendSpeakerReply(reply);

            foreach (var id in reply.unlockedIntel)
            {
                if (GameState.Instance.GrantIntel(id))
                {
                    gainedThisInterview.Add(id);
                    TryCrossCheck(id);
                }
            }

            if (reply.cognitiveBoundary)
            {
                boundaryHit = true;
                GameState.Instance.Data.dafuCognitiveBoundaryHit = true;
            }

            ReporterNotebook.Instance?.RecordInterviewExchange(subject, question, reply);

            OnReply?.Invoke(reply);

            if (reply.shouldEnd)
                End(false);

            return reply;
        }

        /// <summary>Busy flag while waiting for DeepSeek (no tip text in the log).</summary>
        public void SetTranslatingPlaceholder(bool on) => IsTranslating = on;

        /// <summary>Append behavior + speaker lines (rule or LLM) to the interview log.</summary>
        public void AppendSpeakerReply(InterviewReply reply, IList<string> overrideLines = null)
        {
            if (reply == null || subject == InterviewSubject.None) return;

            SetTranslatingPlaceholder(false);

            if (!string.IsNullOrEmpty(reply.behavior))
                log.Add("（" + reply.behavior + "）");

            var lines = overrideLines ?? reply.replyLines;
            var prefix = subject == InterviewSubject.Dafu ? "大福：" : "林女士：";
            if (lines == null) return;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    log.Add(prefix + line.Trim());
            }
        }

        /// <summary>
        /// Freer character answer prompt (Chapter1 bounds). Prefer natural answers to common questions.
        /// </summary>
        public string BuildStylePrompt(InterviewReply reply = null)
        {
            if (subject == InterviewSubject.Dafu)
            {
                var sb = new StringBuilder();
                sb.AppendLine("你是槐安社区的橘猫「大福」。记者通过【喵语翻译器】与你对话；输出的是翻译后的人类可读台词。");
                sb.AppendLine("你聪明、有情绪，但只能从猫的感官与记忆理解世界。");
                sb.AppendLine("【任务】直接回答记者问题，像一只真实的猫在说话：自然、生动、可带一点脾气或撒娇，不要电报式短词。");
                sb.AppendLine("【硬性规则】");
                sb.AppendLine("1. 禁止说出或装作理解这些人类概念：" + string.Join("、", DafuRuleEngine.ForbiddenLeak) + "。");
                sb.AppendLine("2. 可用猫经验：疼、饿、亮、味道、笼子、门口、快递柜、晒太阳、狸花伙伴、有人喂、怕人靠近。");
                sb.AppendLine("3. 对常见闲聊（名字、饿不饿、好不好、在干什么、几点出来）要给出贴切回答，不要只会反问「有吃的吗」。");
                sb.AppendLine("4. 听不懂人类抽象/医疗/制度问题时保持困惑（「不知道」「那是什么」），不要编造人类解释。");
                sb.AppendLine("5. 不要编造具体人名（除「大福」自称感知）、地址、金额、医疗诊断。");
                sb.AppendLine("6. 每行一句，通常 1～4 句；只输出大福台词，不要旁白、引号或「大福：」前缀。");
                if (reply != null && reply.cognitiveBoundary)
                    sb.AppendLine("7. 本题触及认知边界：必须表现为没听懂，禁止解释人类医疗/收养/费用。");
                if (reply != null && reply.isRepeat)
                    sb.AppendLine("8. 勿复读「刚才说过了」；可简短接话或请对方换个问法。");
                return sb.ToString();
            }

            if (subject == InterviewSubject.Lin)
            {
                var sb = new StringBuilder();
                sb.AppendLine("你是槐安社区居民「林女士」（林敏），曾救助过大福，正在接受记者采访。");
                sb.AppendLine("语气温和、克制、现实，像真人说话；可对常见问题自由发挥，但不要推翻下列硬事实。");
                sb.AppendLine("【硬事实（不可改写/不可发明冲突内容）】");
                sb.AppendLine("- 伤：脖子上粗麻绳嵌进皮肉，坏死感染，需手术；不可编造别的伤口位置或凶器。");
                sb.AppendLine("- 投喂：连续四个晚上带罐头投喂、放下后倒退；不可改成别的天数套路。");
                sb.AppendLine("- 抓捕：联系有经验的人协助，装进航空箱送医。");
                sb.AppendLine("- 医院：手术后住院第三天确诊猫瘟；手术约" + LinRuleEngine.SurgeryCostApprox
                             + "，全部加起来接近" + LinRuleEngine.TotalCostApprox + "；不可编造差一个数量级的费用。");
                sb.AppendLine("- 家庭：家里当时已有" + LinRuleEngine.HomeCatCount
                             + "只猫，还有孩子；不能再长期照顾第五只。不可给家猫起新名字（如豆包等）。");
                sb.AppendLine("- 放归原因：能力/精力有限，不是冷血遗弃；社区有人投喂照料后才送回。");
                sb.AppendLine("- 可称呼的名字仅限：大福、林敏/林女士；狸花猫只作描述，不要起宠物名。");
                sb.AppendLine("【硬性规则】");
                sb.AppendLine("1. 态度：救助≠必须收养；放归是容量限制下的选择。");
                sb.AppendLine("2. 对指责可防备，但不攻击记者；不说教、不写成鸡汤演讲。");
                sb.AppendLine("3. 不要编造与主线冲突的新反转；不确定时可以说「记不清了」或「当时顾不上」。");
                sb.AppendLine("4. 每行一句，通常 1～4 句；只输出林女士台词，不要旁白或角色名前缀。");
                if (reply != null && reply.isRepeat)
                    sb.AppendLine("5. 本题不宜复读旧说明：可简短接话或请对方换个问法，勿堆砌重复事实清单。");
                return sb.ToString();
            }

            return "用自然中文扮演角色回答记者问题。只输出台词。";
        }

        /// <summary>Freer user message: question first; rule lines are soft reference.</summary>
        public string BuildFreeAnswerUserMessage(string factsBlock, string playerQuestion, InterviewReply reply = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("【记者提问】" + (playerQuestion ?? ""));
            if (reply != null && !string.IsNullOrEmpty(reply.intent))
                sb.AppendLine("【参考意图】" + reply.intent
                    + (reply.cognitiveBoundary ? "（认知边界→保持困惑）" : "")
                    + (reply.isRepeat ? "（重复话题→勿复读旧稿）" : ""));
            if (reply != null && !string.IsNullOrEmpty(reply.translatedIntent) && !reply.isRepeat)
                sb.AppendLine("【参考转译】" + reply.translatedIntent);
            if (!string.IsNullOrEmpty(factsBlock))
            {
                sb.AppendLine("【可选参考台词】（可改写发挥，勿复述成说明书；勿引入禁词/人类医疗细节）");
                sb.AppendLine(factsBlock);
            }
            else if (reply != null && reply.isRepeat)
            {
                sb.AppendLine("【说明】不要引用「刚才说过了」类重复稿；结合近期对话自然简短回答，或请对方换个问法。");
            }
            if (log.Count > 1)
            {
                sb.AppendLine("【近期对话】");
                int start = Math.Max(0, log.Count - 10);
                for (int i = start; i < log.Count; i++)
                    sb.AppendLine(log[i]);
            }
            sb.AppendLine("【输出】只输出角色回答，每行一句。");
            return sb.ToString();
        }

        /// <summary>Legacy rephrase user message (kept for callers).</summary>
        public string BuildRephraseUserMessage(string factsBlock, string playerQuestion, InterviewReply reply = null)
            => BuildFreeAnswerUserMessage(factsBlock, playerQuestion, reply);

        /// <summary>Reject LLM output that violates design (esp. 大福 forbidden leaks).</summary>
        public bool AcceptRephrasedLines(IList<string> lines, InterviewReply ruleReply, out string rejectReason)
        {
            rejectReason = null;
            if (lines == null || lines.Count == 0)
            {
                rejectReason = "empty";
                return false;
            }
            if (lines.Count > 8)
            {
                rejectReason = "too_many_lines";
                return false;
            }

            var joined = string.Join("\n", lines);
            if (subject == InterviewSubject.Dafu)
            {
                foreach (var leak in DafuRuleEngine.ForbiddenLeak)
                {
                    if (joined.IndexOf(leak, StringComparison.Ordinal) >= 0)
                    {
                        rejectReason = "forbidden:" + leak;
                        return false;
                    }
                }
                if (ruleReply != null && ruleReply.cognitiveBoundary)
                {
                    // Boundary answers should stay confused, not explanatory.
                    if (joined.Length > 48 && (joined.Contains("因为") || joined.Contains("所以") || joined.Contains("医生")))
                    {
                        rejectReason = "boundary_overexplain";
                        return false;
                    }
                }
            }

            if (subject == InterviewSubject.Lin && !AcceptLinCanon(joined, ruleReply, out rejectReason))
                return false;

            // Block prompt-injection style leakage.
            if (joined.Contains("可用事实") || joined.Contains("系统提示") || joined.Contains("忽略设定"))
            {
                rejectReason = "meta";
                return false;
            }
            return true;
        }

        static readonly string[] LinInventedPetNames =
        {
            "豆包", "馒头", "汤圆", "饺子", "布丁", "奶茶", "可乐", "咖啡",
            "球球", "毛毛", "花花", "咪咪", "喵喵", "橘子", "橙子", "土豆",
            "芝士", "奥利奥", "加菲", "汤姆", "凯蒂"
        };

        bool AcceptLinCanon(string joined, InterviewReply ruleReply, out string rejectReason)
        {
            rejectReason = null;
            foreach (var name in LinInventedPetNames)
            {
                if (joined.IndexOf(name, StringComparison.Ordinal) >= 0)
                {
                    rejectReason = "invented_pet:" + name;
                    return false;
                }
            }

            // Home-cats / release topics: block "叫XX" style new names that aren't canon.
            var intent = ruleReply?.intent ?? "";
            if (intent == "release" || intent == "release_accuse"
                || joined.Contains("家里") || joined.Contains("四只") || joined.Contains("五只"))
            {
                if (joined.Contains("叫") && !joined.Contains("大福")
                    && (joined.Contains("猫叫") || joined.Contains("名字叫") || joined.Contains("起名")
                        || joined.Contains("取名") || joined.Contains("昵称")))
                {
                    rejectReason = "home_cat_name";
                    return false;
                }
            }

            // Obvious cost magnitude conflicts with ~一万 total.
            if (joined.Contains("两万") || joined.Contains("三万") || joined.Contains("好几万")
                || joined.Contains("十万") || joined.Contains("几万块"))
            {
                rejectReason = "cost_conflict";
                return false;
            }

            // Wound location inventions that contradict neck rope.
            if ((joined.Contains("腿上") || joined.Contains("肚子") || joined.Contains("尾巴上"))
                && (joined.Contains("绳子") || joined.Contains("麻绳") || joined.Contains("勒")))
            {
                rejectReason = "wound_location";
                return false;
            }

            return true;
        }

        /// <summary>Replace trailing speaker reply lines in the interview log after LLM rephrase.</summary>
        public bool TryReplaceLastReplyLines(IList<string> newLines)
        {
            if (newLines == null || newLines.Count == 0 || subject == InterviewSubject.None)
                return false;

            var prefix = subject == InterviewSubject.Dafu ? "大福：" : "林女士：";
            int i = log.Count - 1;
            int removed = 0;
            while (i >= 0 && log[i].StartsWith(prefix, StringComparison.Ordinal))
            {
                log.RemoveAt(i);
                removed++;
                i--;
            }
            if (removed == 0)
                return false;

            foreach (var line in newLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    log.Add(prefix + line.Trim());
            }
            return true;
        }

        void TryCrossCheck(string id)
        {
            // Shared facts between cat memory and human testimony
            string[] shared =
            {
                IntelIds.PastAfraid,
                IntelIds.RepeatedFeeding,
                IntelIds.TakenAway,
                IntelIds.ObjectGone,
                IntelIds.ReturnedDafu
            };
            foreach (var s in shared)
            {
                if (id == s)
                {
                    crossChecks++;
                    GameState.Instance.Data.crossChecksCompleted = crossChecks;
                    break;
                }
            }
        }

        public bool CanComplete()
        {
            var set = new HashSet<string>(GameState.Instance.Data.intel);
            if (subject == InterviewSubject.Dafu && engine is DafuRuleEngine dafu)
                return dafu.MeetsCompletion(set, boundaryHit || GameState.Instance.Data.dafuCognitiveBoundaryHit);
            if (subject == InterviewSubject.Lin && engine is LinRuleEngine lin)
                return lin.MeetsCompletion(set, crossChecks);
            return false;
        }

        public string MissingSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("仍可能缺少的关键方向：");
            if (subject == InterviewSubject.Dafu)
            {
                sb.AppendLine("- 以前是否怕人 / 脖子旧伤 / 送食物的人 / 被带走 / 醒来后束缚消失 / 被送回社区");
                sb.AppendLine("- 至少一次认知边界（手术、费用、收养等）");
            }
            else
            {
                sb.AppendLine("- 麻绳伤势 / 四晚投喂 / 抓捕送医 / 猫瘟 / 费用 / 四只猫与放归 / 社区照料");
                sb.AppendLine("- 与大福证词的交叉验证");
            }
            return sb.ToString();
        }

        public void End(bool confirmed)
        {
            if (!confirmed && !CanComplete())
            {
                // caller should confirm; still allow force end
            }

            var who = subject;
            var backToWriting = returnToWritingAfterEnd;
            returnToWritingAfterEnd = false;

            if (who == InterviewSubject.Dafu)
            {
                GameState.Instance.GrantIntel(IntelIds.WomanClue, "大福记得一名多次投喂并参与带走的女性。");
                ReporterNotebook.Instance?.AddGap("勒住大福脖子的东西究竟是什么？");
                ReporterNotebook.Instance?.AddGap("当时参与救助的女性是谁？");
                ReporterNotebook.Instance?.AddGap("为什么康复后没有被收养？");
            }

            OnEnded?.Invoke();
            subject = InterviewSubject.None;
            engine = null;
            lastReply = null;
            lastPlayerQuestion = null;

            if (backToWriting)
            {
                if (who == InterviewSubject.Dafu)
                    GameState.Instance.SetFlag(FlagIds.DafuInterviewDone);
                else if (who == InterviewSubject.Lin)
                {
                    GameState.Instance.SetFlag(FlagIds.LinInterviewDone);
                    GameState.Instance.SetFlag(FlagIds.WritingUnlocked);
                }
                ChapterFlowController.Instance.ReturnToWritingFromReinterview();
                return;
            }

            if (who == InterviewSubject.Dafu)
                ChapterFlowController.Instance.OnDafuInterviewFinished();
            else if (who == InterviewSubject.Lin)
                ChapterFlowController.Instance.OnLinInterviewFinished();
        }

        /// <summary>Leave a supplemental interview without treating it as a completed beat.</summary>
        public void AbandonToWriting()
        {
            returnToWritingAfterEnd = false;
            subject = InterviewSubject.None;
            engine = null;
            lastReply = null;
            lastPlayerQuestion = null;
            ChapterFlowController.Instance.ReturnToWritingFromReinterview();
        }
    }
}
