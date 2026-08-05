using System.Collections.Generic;
using System.Text;
using StreetCat.Core;
using StreetCat.Data;
using UnityEngine;

namespace StreetCat.Notebook
{
    [System.Serializable]
    public class NotebookTopic
    {
        public string id;
        public string title;
        public TopicStatus status;
        public List<string> bullets = new List<string>();
        public string hintQuestion;
    }

    public class ReporterNotebook : MonoBehaviour
    {
        public static ReporterNotebook Instance { get; private set; }

        public List<NotebookTopic> Topics { get; private set; }

        void Awake()
        {
            Instance = this;
            Topics = new List<NotebookTopic>
            {
                new NotebookTopic { id = "who", title = "大福是谁", hintQuestion = "你平时都在哪里活动？" },
                new NotebookTopic { id = "daily", title = "现在的生活", hintQuestion = "谁会给你吃的？" },
                new NotebookTopic { id = "fear", title = "以前怕人", hintQuestion = "你以前也这样不怕人吗？" },
                new NotebookTopic { id = "neck", title = "脖子上的伤", hintQuestion = "你的脖子以前疼吗？" },
                new NotebookTopic { id = "woman", title = "送食物的女人", hintQuestion = "有没有人连续很多天给你带吃的？" },
                new NotebookTopic { id = "taken", title = "被带走的那天", hintQuestion = "后来有人把你带走了吗？" },
                new NotebookTopic { id = "place", title = "陌生的地方", hintQuestion = "那个地方亮不亮？味道重不重？" },
                new NotebookTopic { id = "return", title = "回到社区", hintQuestion = "最后是谁把你送回来的？" },
                new NotebookTopic { id = "gaps", title = "仍需确认", hintQuestion = "" },
            };
            GameState.Ensure();
            GameState.Instance.OnIntelGained += OnIntel;
        }

        void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnIntelGained -= OnIntel;
        }

        void OnIntel(string id)
        {
            switch (id)
            {
                case IntelIds.DafuAppearTime:
                case IntelIds.DafuRestSpot:
                case IntelIds.DafuNearGuard:
                    Touch("daily", "大福常在保安亭 / 快递柜附近活动。", TopicStatus.Partial);
                    break;
                case IntelIds.CommunityCare:
                case IntelIds.DafuNoOwner:
                    Touch("daily", "没有固定主人，多人共同照料。", TopicStatus.Deep);
                    break;
                case IntelIds.PastAfraid:
                    Touch("fear", "大福以前非常怕人。", TopicStatus.Deep);
                    break;
                case IntelIds.NeckPain:
                case IntelIds.NeckObject:
                case IntelIds.NeckObjectTight:
                case IntelIds.NeckLongTermPain:
                    Touch("neck", "脖子曾被某种很紧的东西长期勒住。", TopicStatus.Partial);
                    break;
                case IntelIds.RepeatedFeeding:
                    Touch("woman", "一名女性曾多次带来食物。", TopicStatus.Partial);
                    break;
                case IntelIds.TakenAway:
                case IntelIds.CaptureParticipant:
                    Touch("taken", "后来被抓走并带走。", TopicStatus.Partial);
                    break;
                case IntelIds.BrightStrangePlace:
                case IntelIds.Sleep:
                case IntelIds.ObjectGone:
                    Touch("place", "曾在很亮、气味重的地方停留；醒来后勒住脖子的东西消失。", TopicStatus.Partial);
                    break;
                case IntelIds.ReturnedDafu:
                    Touch("return", "康复后被带回槐安社区。", TopicStatus.Deep);
                    break;
                case IntelIds.CognitiveBoundary:
                    AddGap("大福无法解释手术 / 疾病 / 费用等人类概念。");
                    break;
                case IntelIds.WomanClue:
                    AddGap("送食物并参与带走的女人是谁？");
                    Touch("woman", "需要向社区居民核实身份。", TopicStatus.Partial);
                    break;
                case IntelIds.RopeEmbedded:
                    Touch("neck", "医院确认麻绳嵌入皮肉，严重感染。", TopicStatus.Deep);
                    break;
                case IntelIds.FeedFourDays:
                    Touch("woman", "林女士连续四晚投喂并保持距离。", TopicStatus.Deep);
                    break;
                case IntelIds.CaptureSuccess:
                    Touch("taken", "联系他人协助抓捕并送医。", TopicStatus.Deep);
                    break;
                case IntelIds.PanleukopeniaDay3:
                case IntelIds.TotalCost:
                    Touch("place", "术后确诊猫瘟，总费用接近一万元。", TopicStatus.Deep);
                    break;
                case IntelIds.FourCatsHome:
                case IntelIds.CannotFifth:
                case IntelIds.ReturnOriginalArea:
                    Touch("return", "因家中已有四只猫而放归；放归后留在社区。", TopicStatus.Deep);
                    break;
                case IntelIds.LinIdentity:
                    Touch("woman", "救助者是林女士（林敏）。", TopicStatus.Deep);
                    break;
            }
        }

        public void Touch(string topicId, string bullet, TopicStatus minStatus)
        {
            var t = Topics.Find(x => x.id == topicId);
            if (t == null) return;
            if (!t.bullets.Contains(bullet))
                t.bullets.Add(bullet);
            if ((int)t.status < (int)minStatus)
                t.status = minStatus;
            Persist();
        }

        public void AddGap(string q)
        {
            GameState.Instance.AddPendingQuestion(q);
            Touch("gaps", q, TopicStatus.Partial);
        }

        public string BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("【记者笔记】");
            foreach (var t in Topics)
            {
                if (t.status == TopicStatus.Untouched && t.bullets.Count == 0)
                    continue;
                var mark = t.status == TopicStatus.Deep ? "●" : t.status == TopicStatus.Partial ? "◐" : "○";
                sb.AppendLine($"{mark} {t.title}");
                foreach (var b in t.bullets)
                    sb.AppendLine("  - " + b);
            }
            if (GameState.Instance.Data.pendingQuestions.Count > 0)
            {
                sb.AppendLine("待确认：");
                foreach (var q in GameState.Instance.Data.pendingQuestions)
                    sb.AppendLine("  ? " + q);
            }
            return sb.ToString();
        }

        void Persist()
        {
            var list = new List<NotebookTopicSave>();
            foreach (var t in Topics)
            {
                list.Add(new NotebookTopicSave
                {
                    id = t.id,
                    title = t.title,
                    status = (int)t.status,
                    bullets = new List<string>(t.bullets)
                });
            }
            GameState.Instance.Data.topics = list;
            GameState.Instance.Notify();
        }
    }
}
