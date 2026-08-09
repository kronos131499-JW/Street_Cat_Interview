using System;
using System.Collections.Generic;
using StreetCat.Core;
using StreetCat.Data;
using UnityEngine;

namespace StreetCat.Investigation
{
    [Serializable]
    public class InspectBeat
    {
        /// <summary>true = 旁白（无名牌）；false = 小凌对白。</summary>
        public bool narration;
        public string text;
        /// <summary>Optional mid-inspect background switch (script 【背景】).</summary>
        public string background;
    }

    [Serializable]
    public class HotspotData
    {
        public string id;
        public string title;
        public string description;
        /// <summary>Script 【背景】 label while inspecting this hotspot.</summary>
        public string background;
        public List<InspectBeat> beats = new List<InspectBeat>();
        public string grantIntel;
        public string noteLine;
        public bool once = true;
        public bool inspected;
    }

    [Serializable]
    public class TalkTopic
    {
        public string id;
        public string label;
        public string reply;
        /// <summary>Guard portrait state while delivering this reply (疑惑/苦笑/回忆/常态).</summary>
        public string portrait;
        public string grantIntel;
        public string grantIntel2;
        public string noteLine;
        public string setObjective;
        public string nextSceneId;
        public bool requiresIntel;
        public string requiredIntel;
        public bool requiresFlag;
        public string requiredFlag;
        public bool unlocksLinFlow;
        public bool done;
    }

    public class InvestigationService : MonoBehaviour
    {
        public static InvestigationService Instance { get; private set; }

        public List<HotspotData> Hotspots { get; private set; }
        public List<TalkTopic> GuardTopics { get; private set; }
        public List<TalkTopic> PostInterviewTopics { get; private set; }

        void Awake()
        {
            Instance = this;
            BuildDefaults();
        }

        void BuildDefaults()
        {
            Hotspots = new List<HotspotData>
            {
                new HotspotData
                {
                    id = "cat_house",
                    title = "猫屋",
                    description = "塑料收纳箱改造的猫屋，比出租屋还精致。",
                    background = "流浪猫投喂点",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "猫屋是用塑料收纳箱改造的，外面罩着一层裁剪过的防水板，接缝处贴了好几道胶带，屋顶还压着两块砖，防止被风吹翻。猫屋里铺着一张旧毛毯，表面已经被抓得起了球。" },
                        new InspectBeat { narration = false, text = "竟然还有给猫住的地方。" },
                        new InspectBeat { narration = false, text = "看起来好精致哇......" },
                        new InspectBeat { narration = false, text = "比我的出租屋还要精致。" }
                    }
                },
                new HotspotData
                {
                    id = "food_bowl",
                    title = "猫粮碗",
                    description = "几个猫碗并排放着，碗底很干净。",
                    background = "流浪猫投喂点",
                    grantIntel = IntelIds.FixedFeedingPoint,
                    noteLine = "社区内设有长期维护的投喂点，附近居民可能了解流浪猫的情况。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "几个猫碗并排放着，其中一个还有少量猫粮。碗底很干净。" },
                        new InspectBeat { narration = false, text = "碗还挺干净。" },
                        new InspectBeat { narration = false, text = "应该有人定期过来投喂。" }
                    }
                },
                new HotspotData
                {
                    id = "water_bowl",
                    title = "水碗",
                    description = "水碗里装着大半碗清水，上面飘着几根猫毛。",
                    background = "流浪猫投喂点",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "水碗里装着大半碗清水，上面飘着几根猫毛。" },
                        new InspectBeat { narration = false, text = "其实我一直很好奇，猫会不会把水里自己的毛喝下去。" }
                    }
                },
                new HotspotData
                {
                    id = "sign",
                    title = "投喂点小挂牌",
                    description = "挂牌提醒不要倒剩饭，并补了一行：奶茶不算水。",
                    background = "流浪猫投喂点_告示牌",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "挂牌上用记号笔写着：请不要把人类吃的剩饭倒在这里。猫粮少量添加，吃完再补，不然放久了会变质。水脏了的话麻烦帮忙换一下，谢谢。" },
                        new InspectBeat { narration = true, text = "下面还有一行明显是后来补上的：不要倒水之外的液体！！！奶茶不算水！" },
                        new InspectBeat { narration = false, text = "……" },
                        new InspectBeat { narration = false, text = "不知道为什么有点想喝奶茶了......" }
                    }
                },
                new HotspotData
                {
                    id = "tabby",
                    title = "灌木旁的狸花猫",
                    description = "狸花猫晒太阳，靠近后钻进灌木丛。",
                    background = "晒太阳的猫_放松",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "一只狸花猫趴在灌木丛边的草地上晒太阳，前爪交叠，眯着眼睛。" },
                        new InspectBeat { narration = false, text = "那边有只猫哎。" },
                        new InspectBeat { narration = true, text = "小凌刚往前靠近两步，狸花猫立刻抬起头，警惕地看向她。", background = "晒太阳的猫_警惕" },
                        new InspectBeat { narration = false, text = "嘬嘬嘬——咪咪——" },
                        new InspectBeat { narration = true, text = "狸花猫迅速起身，一头钻进旁边的灌木丛，只剩树叶轻轻晃动。", background = "晒太阳的猫_躲闪" },
                        new InspectBeat { narration = false, text = "……跑得还挺快。" },
                        new InspectBeat { narration = true, text = "小凌往灌木丛里看了一眼，但是什么都没看见。" },
                        new InspectBeat { narration = false, text = "看来这里虽然有人固定照顾它们，但不代表它们会随便亲近陌生人。" },
                        new InspectBeat { narration = false, text = "好吧，不打扰你晒太阳了。" }
                    }
                },
                new HotspotData
                {
                    id = "vending",
                    title = "自动贩卖机",
                    description = "咖啡只卖六块，公司楼下要十八。",
                    background = "自动贩卖机",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = false, text = "什么，咖啡只卖六块？？？" },
                        new InspectBeat { narration = false, text = "公司楼下要十八。突然发现了一个值得调查的社会议题。" }
                    }
                },
                new HotspotData
                {
                    id = "bench",
                    title = "木质长椅",
                    description = "老式木质长椅，看上去至少服役十年了。",
                    background = "木质长椅",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "一张老式木质长椅靠着步道摆放。绿色金属扶手已经有些掉漆，露出下面发灰的铁锈；几块木板被晒得颜色深浅不一，其中一块还微微翘起。" },
                        new InspectBeat { narration = false, text = "看上去至少服役十年了。" },
                        new InspectBeat { narration = false, text = "和《此间》的打印机差不多。" }
                    }
                },
                new HotspotData
                {
                    id = "locker",
                    title = "快递柜",
                    description = "柜顶有橘色猫毛，大福常趴在这里。",
                    background = "快递柜",
                    grantIntel = IntelIds.DafuRestSpot,
                    noteLine = "大福经常趴在社区入口的快递柜上，但当前并不在附近。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "社区入口旁立着一排快递柜。柜顶铺着一块折叠纸板，上面残留着少量橘色猫毛。" },
                        new InspectBeat { narration = false, text = "帖子里的照片就是在这里拍的。" },
                        new InspectBeat { narration = true, text = "小凌抬头看向空荡荡的柜顶。" },
                        new InspectBeat { narration = false, text = "本人还没来上班。" }
                    }
                }
            };

            GuardTopics = new List<TalkTopic>
            {
                new TalkTopic
                {
                    id = "appear_time",
                    label = "大福一般几点出现？",
                    reply =
                        "四点多吧。有时候早一点，有时候晚一点。天气好就在快递柜上睡，下雨就不知道钻哪去了，反正吃饭的时候会出来。",
                    portrait = "常态",
                    grantIntel = IntelIds.DafuAppearTime,
                    noteLine = "大福通常在下午四五点出现。",
                    setObjective = "等待大福出现。"
                },
                new TalkTopic
                {
                    id = "relation",
                    label = "大福和保安的关系",
                    reply =
                        "可能那天它刚好趴这儿。它那个也叫上班啊……反正有时候会过来看看我。看有没有吃的。同事可不会天天找我要罐头。",
                    portrait = "苦笑",
                    grantIntel = IntelIds.DafuNearGuard,
                    noteLine = "大福经常在保安亭附近活动。"
                },
                new TalkTopic
                {
                    id = "history",
                    label = "大福一直都住在这里吗？",
                    reply =
                        "它从医院回来以后吧。之前有人把它送去治过病，后来又送回小区了。以前它怕人，看见人就躲。刚开始哪有这么胖。听说是脖子受伤，具体你得问当时救它的人。",
                    portrait = "回忆",
                    grantIntel = IntelIds.DafuBecameGuardCat,
                    grantIntel2 = IntelIds.DafuWasRescued,
                    noteLine = "大福曾因颈部受伤被送医，放归后才逐渐成为「保安猫」。"
                },
                new TalkTopic
                {
                    id = "shelter",
                    label = "大福的居所",
                    reply =
                        "猫粮和猫窝不是我弄的，小区里有人弄的。有人过来就加点，水没了我有时候也倒一点。它没有主人，不过现在过得还挺好，比以前胖多了。",
                    portrait = "常态",
                    grantIntel = IntelIds.DafuNoOwner,
                    grantIntel2 = IntelIds.CommunityCare,
                    noteLine = "大福没有固定主人，社区中有多人照顾。"
                },
                new TalkTopic
                {
                    id = "name",
                    label = "为什么叫大福？（可选）",
                    reply = "不知道。我认识它的时候已经叫大福了。你喊它试试——手上又没吃的，它当然不理你。",
                    portrait = "苦笑"
                }
            };

            PostInterviewTopics = new List<TalkTopic>
            {
                new TalkTopic
                {
                    id = "who_rescued",
                    label = "当初是谁救助的大福？",
                    reply =
                        "林姐。我们小区的住户。大福脖子受伤的时候，是她找人抓住它，送去医院的。后来也是她把大福送回来的。具体怎么回事，你得问她。我也只知道个大概。",
                    portrait = "回忆",
                    grantIntel = IntelIds.LinIdentity,
                    noteLine = "救助者是小区住户「林姐」。",
                    unlocksLinFlow = true
                },
                new TalkTopic
                {
                    id = "lin_info",
                    label = "林姐的信息",
                    reply =
                        "她还住这儿，有时候还会过来看大福。你想采访她的话，我先帮你问一声。她愿意的话，再把联系方式给你。",
                    portrait = "常态",
                    requiresIntel = true,
                    requiredIntel = IntelIds.LinIdentity,
                    setObjective = "等待林女士回复。",
                    nextSceneId = SceneIds.SC09
                }
            };
        }

        public IReadOnlyList<InspectBeat> GetInspectBeats(string hotspotId)
        {
            var h = Hotspots.Find(x => x.id == hotspotId);
            if (h == null) return Array.Empty<InspectBeat>();
            if (h.beats != null && h.beats.Count > 0)
                return h.beats;
            if (!string.IsNullOrEmpty(h.description))
                return new[] { new InspectBeat { narration = true, text = h.description } };
            return Array.Empty<InspectBeat>();
        }

        public string Inspect(string hotspotId)
        {
            var h = Hotspots.Find(x => x.id == hotspotId);
            if (h == null) return "没什么特别的。";
            if (h.once && h.inspected) return h.description + "\n（已经看过了。）";
            h.inspected = true;
            if (!string.IsNullOrEmpty(h.grantIntel))
                GameState.Instance.GrantIntel(h.grantIntel, h.noteLine);
            TryUnlockGuard();
            return h.description;
        }

        void TryUnlockGuard()
        {
            var gs = GameState.Instance;
            if (gs.HasIntel(IntelIds.FixedFeedingPoint) && gs.HasIntel(IntelIds.DafuRestSpot) && !gs.HasFlag(FlagIds.GuardUnlocked))
            {
                gs.SetFlag(FlagIds.GuardUnlocked);
                gs.SetObjective("向保安询问大福的情况。");
            }
        }

        public string Talk(TalkTopic topic)
        {
            if (topic == null) return "";
            if (topic.requiresIntel && !GameState.Instance.HasIntel(topic.requiredIntel))
                return "（还缺少相关线索。）";
            if (topic.requiresFlag && !GameState.Instance.HasFlag(topic.requiredFlag))
                return "（现在还不能问这个。）";

            topic.done = true;
            if (!string.IsNullOrEmpty(topic.grantIntel))
                GameState.Instance.GrantIntel(topic.grantIntel, topic.noteLine);
            if (!string.IsNullOrEmpty(topic.grantIntel2))
                GameState.Instance.GrantIntel(topic.grantIntel2);
            if (!string.IsNullOrEmpty(topic.setObjective))
                GameState.Instance.SetObjective(topic.setObjective);
            if (topic.unlocksLinFlow)
                GameState.Instance.SetFlag(FlagIds.LinUnlocked, false); // identity only; contact later

            return topic.reply;
        }

        public bool CanWaitForDafu()
        {
            return GameState.Instance.HasIntel(IntelIds.DafuAppearTime);
        }
    }
}
