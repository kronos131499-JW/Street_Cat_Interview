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

        public InterviewSubject Subject => subject;
        public InterviewerStats Stats => engine?.Stats;
        public IReadOnlyList<string> Log => log;
        public bool IsReinterviewFromWriting => returnToWritingAfterEnd;

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
            crossChecks = GameState.Instance.Data.crossChecksCompleted;
            SaveSystem.Autosave();

            if (who == InterviewSubject.Dafu)
            {
                engine = new DafuRuleEngine();
                log.Add(returnToWritingAfter
                    ? "系统：补充采访开始。已获得的情报与素材会保留，可继续追问未覆盖的方向。"
                    : "系统：第一次自由采访开始。可询问现在的生活、过去的伤、认识的人。");
            }
            else
            {
                engine = new LinRuleEngine();
                log.Add(returnToWritingAfter
                    ? "系统：补充采访开始。已获得的情报与素材会保留，请继续追问并注意事实边界。"
                    : "系统：第二次自由采访开始。请根据已有线索追问，并注意事实边界。");
            }
        }

        public InterviewReply Ask(string question)
        {
            if (engine == null)
                return null;

            log.Add("小凌：" + question);
            var reply = engine.Process(question);

            if (!string.IsNullOrEmpty(reply.behavior))
                log.Add("（" + reply.behavior + "）");
            foreach (var line in reply.replyLines)
                log.Add((subject == InterviewSubject.Dafu ? "大福：" : "林女士：") + line);

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

            if (!string.IsNullOrEmpty(reply.newQuestion))
                ReporterNotebook.Instance?.AddGap(reply.newQuestion);

            OnReply?.Invoke(reply);

            if (reply.shouldEnd)
                End(false);

            return reply;
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
            ChapterFlowController.Instance.ReturnToWritingFromReinterview();
        }
    }
}
