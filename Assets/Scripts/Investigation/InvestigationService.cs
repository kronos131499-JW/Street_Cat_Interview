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
        /// <summary>true = 旁白（无名牌）；false = character / 小凌 when speaker empty.</summary>
        public bool narration;
        public string text;
        /// <summary>Optional mid-inspect background switch (script 【背景】).</summary>
        public string background;
        /// <summary>Optional 【SE：…】 cue played when this beat is shown.</summary>
        public string sfx;
        /// <summary>true = 系统提示行（非旁白/角色）。</summary>
        public bool system;
        /// <summary>Character name when not narration/system (default 小凌).</summary>
        public string speaker;
        /// <summary>Portrait expression tag (常态/认真/疑惑…).</summary>
        public string portrait;
    }

    [Serializable]
    public class TalkBeat
    {
        public bool narration;
        public bool system;
        public string speakerName;
        public string portrait;
        public string text;
        /// <summary>Optional 【SE：…】 cue when this beat is shown.</summary>
        public string sfx;
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
        /// <summary>Legacy single-line reply (post-interview topics / fallback).</summary>
        public string reply;
        /// <summary>Multi-beat conversation; when non-empty, GameUI plays these then returns to menu.</summary>
        public List<TalkBeat> beats = new List<TalkBeat>();
        /// <summary>Guard portrait state while delivering legacy single reply.</summary>
        public string portrait;
        public string grantIntel;
        public string grantIntel2;
        public string noteLine;
        public string noteLine2;
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

        /// <summary>
        /// Set when player just earned both FixedFeedingPoint + DafuRestSpot;
        /// consumed after the current inspect finishes to play the guard-appear cutscene.
        /// </summary>
        bool pendingGuardAppear;

        void Awake()
        {
            Instance = this;
            BuildDefaults();
        }

        static TalkBeat TB(string speaker, string text, string portrait = null, string sfx = null) =>
            new TalkBeat { speakerName = speaker, text = text, portrait = portrait, sfx = sfx };

        static TalkBeat TN(string text, string sfx = null) =>
            new TalkBeat { narration = true, text = text, sfx = sfx };

        static TalkBeat TS(string text, string sfx = null) =>
            new TalkBeat { system = true, text = text, sfx = sfx };

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
                        new InspectBeat { narration = true, text = "狸花猫迅速起身，一头钻进旁边的灌木丛，只剩树叶轻轻晃动。", background = "晒太阳的猫_躲闪", sfx = "灌木丛窸窣声" },
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
                    grantIntel = IntelIds.DafuAppearTime,
                    noteLine = "大福通常在下午四五点出现。",
                    beats = new List<TalkBeat>
                    {
                        TB("小凌", "大福一般几点钟会来这边？", "认真"),
                        TB("保安叔叔", "四点多吧。", "回忆"),
                        TB("保安叔叔", "有时候早一点，有时候晚一点。", "回忆"),
                        TB("小凌", "基本每天都来？", "认真"),
                        TB("保安叔叔", "嗯，差不多。", "常态"),
                        TN("保安叔叔指了指快递柜。"),
                        TB("保安叔叔", "天气好就在上面睡。", "常态"),
                        TB("保安叔叔", "下雨就不知道钻哪去了，反正吃饭的时候会出来。", "常态"),
                        TB("小凌", "那我今天应该能等到。", "常态"),
                        TB("保安叔叔", "希望吧。", "苦笑"),
                        TS("获得情报——“大福通常在下午四五点出现”")
                    }
                },
                new TalkTopic
                {
                    id = "relation",
                    label = "大福和保安的关系",
                    grantIntel = IntelIds.DafuNearGuard,
                    noteLine = "大福经常在保安亭附近活动。",
                    beats = new List<TalkBeat>
                    {
                        TB("小凌", "我在网上看到，大福每天陪您上班。", "思考"),
                        TB("保安叔叔", "谁说的？", "疑惑"),
                        TB("小凌", "有人发了你们俩的照片。", "常态"),
                        TB("保安叔叔", "哦。", "苦笑"),
                        TB("保安叔叔", "可能那天它刚好趴这儿。", "常态"),
                        TB("小凌", "所以没有一起上班？", "思考"),
                        TB("保安叔叔", "它那个也叫上班啊......", "苦笑"),
                        TN("保安叔叔忍不住笑了一下。"),
                        TB("保安叔叔", "反正它有时候会过来看看我。", "常态"),
                        TB("小凌", "看您？", "思考"),
                        TB("保安叔叔", "看有没有吃的。", "苦笑"),
                        TB("小凌", "某种程度上，也挺像同事的。", "吐槽"),
                        TB("保安叔叔", "同事可不会天天找我要罐头。", "苦笑"),
                        TS("获得情报——“大福经常在保安亭附近活动”")
                    }
                },
                new TalkTopic
                {
                    id = "history",
                    label = "大福一直都住在这里吗？",
                    grantIntel = IntelIds.DafuBecameGuardCat,
                    grantIntel2 = IntelIds.DafuWasRescued,
                    noteLine = "大福原本只是附近活动的流浪猫。接受救治并被放归后，它才逐渐开始在保安亭附近长期活动。",
                    noteLine2 = "大福曾因颈部受伤被送医，但保安并不了解具体经过。",
                    beats = new List<TalkBeat>
                    {
                        TB("小凌", "大福是什么时候开始天天往这边跑的？", "认真"),
                        TB("保安叔叔", "这个啊……它从医院回来以后吧。", "回忆"),
                        TB("小凌", "就是帖子里说它受伤的那次？", "认真"),
                        TB("保安叔叔", "嗯。之前有人把它送去治过病。", "回忆"),
                        TB("保安叔叔", "后来又送回小区了。", "回忆"),
                        TB("小凌", "所以它以前不在保安亭这边？", "认真"),
                        TB("保安叔叔", "没什么印象。", "回忆"),
                        TN("保安叔叔沉思了一会儿。"),
                        TB("保安叔叔", "以前这附近猫也不少。它那时候又怕人，看见人就躲，我哪分得出来哪只是它。", "回忆"),
                        TB("小凌", "那它回来以后就一直待在这儿了？", "认真"),
                        TB("保安叔叔", "也不是一回来就这样。", "回忆"),
                        TB("保安叔叔", "最开始还是躲得远远的。", "回忆"),
                        TB("保安叔叔", "后来有人每天给它送吃的，它慢慢就往门口这边来了。", "回忆"),
                        TN("保安叔叔抬手指了指快递柜。"),
                        TB("保安叔叔", "再后来不知道什么时候，它就开始往这上面躺。", "回忆"),
                        TB("保安叔叔", "躺着躺着，大家都认识它了。", "回忆"),
                        TB("小凌", "所以“保安猫”其实是后来的事。", "思考"),
                        TB("保安叔叔", "对。", "苦笑"),
                        TB("保安叔叔", "刚开始哪有这么胖。", "苦笑"),
                        TB("小凌", "它为什么去医院，您知道吗？", "认真"),
                        TB("保安叔叔", "听说是受伤了，脖子那边。", "常态"),
                        TB("保安叔叔", "具体怎么伤的、后来怎么治的，我就不清楚了。", "常态"),
                        TB("小凌", "当时送它去医院的人，您认识吗？", "认真"),
                        TB("保安叔叔", "认识。", "常态"),
                        TB("保安叔叔", "这样吧，一会儿你先看看大福。我晚点帮你问问那个人愿不愿意接受采访。", "常态"),
                        TB("小凌", "好，麻烦您了。", "常态"),
                        TS("获得情报——“大福成为保安猫的时间”"),
                        TS("大福原本只是附近活动的流浪猫。接受救治并被放归后，它才逐渐开始在保安亭附近长期活动。"),
                        TS("获得情报——“大福曾接受救助”"),
                        TS("大福曾因颈部受伤被送医，但保安并不了解具体经过。")
                    }
                },
                new TalkTopic
                {
                    id = "shelter",
                    label = "大福的居所",
                    grantIntel = IntelIds.DafuNoOwner,
                    grantIntel2 = IntelIds.CommunityCare,
                    noteLine = "大福没有固定主人，社区中有多人照顾。",
                    beats = new List<TalkBeat>
                    {
                        TB("小凌", "那边的猫粮和猫窝，是您弄的吗？", "认真"),
                        TB("保安叔叔", "不是，我哪有空弄这些。", "常态"),
                        TN("保安叔叔往投喂点方向看了一眼。"),
                        TB("保安叔叔", "小区里有人弄的。", "常态"),
                        TB("小凌", "有固定的人负责？", "认真"),
                        TB("保安叔叔", "也没什么负责不负责的。", "常态"),
                        TB("保安叔叔", "有人过来就加点。", "常态"),
                        TB("保安叔叔", "我看水没了，有时候也给它倒一点。", "常态"),
                        TB("小凌", "大福没有主人？", "认真"),
                        TB("保安叔叔", "没有。", "常态"),
                        TB("小凌", "那它就一直住外面？", "认真"),
                        TB("保安叔叔", "嗯。", "常态"),
                        TB("保安叔叔", "不过它现在过得还挺好。", "常态"),
                        TB("保安叔叔", "比以前胖多了。", "常态"),
                        TS("获得情报——“大福没有固定主人”"),
                        TS("获得情报——“社区中有多人照顾大福”")
                    }
                },
                new TalkTopic
                {
                    id = "name",
                    label = "为什么叫大福？（可选）",
                    beats = new List<TalkBeat>
                    {
                        TB("小凌", "大福这个名字是谁起的？", "常态"),
                        TB("保安叔叔", "不知道。", "常态"),
                        TB("小凌", "您也不知道？", "惊讶"),
                        TB("保安叔叔", "我认识它的时候已经叫大福了。", "常态"),
                        TB("小凌", "它知道自己叫大福吗？", "思考"),
                        TB("保安叔叔", "你喊它试试。", "常态"),
                        TB("小凌", "大福——", "常态"),
                        TN("没有任何反应。"),
                        TB("保安叔叔", "你手上又没吃的。", "常态"),
                        TB("小凌", "……", "局促"),
                        TB("小凌", "有道理。", "吐槽"),
                    }
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

        /// <summary>【结束交谈】after 大福出没时间 — wait-for-Dafu outro → SC-06.</summary>
        public static List<TalkBeat> BuildWaitForDafuEndBeats()
        {
            return new List<TalkBeat>
            {
                TS("已获得“大福出没时间”"),
                TB("小凌", "我差不多了解啦，谢谢叔叔。", "常态"),
                TB("保安叔叔", "没事。", "常态"),
                TB("小凌", "那我在附近等它一会儿。", "常态"),
                TB("保安叔叔", "行。", "常态"),
                TN("保安叔叔抬头看了眼快递柜，又看了一眼时间。"),
                TB("保安叔叔", "你坐那边等吧。", "常态"),
                TN("他指向步道旁的长椅。"),
                TB("保安叔叔", "它来了基本先在保安亭门口转一圈。", "常态"),
                TB("保安叔叔", "要是没看见，就去猫窝那边找找。", "常态"),
                TB("小凌", "好。", "常态"),
                TB("小凌", "那我先过去了，谢谢您！", "常态"),
                TB("保安叔叔", "哦对了。", "常态"),
                TB("小凌", "嗯？", "惊讶"),
                TB("保安叔叔", "你别一看见它就过去抓啊。", "常态"),
                TB("小凌", "我不抓它。", "常态"),
                TB("保安叔叔", "那就行。以前胆子小，现在好多了，但不认识的人靠太近，它还是会躲着你。", "常态"),
                TB("小凌", "明白。", "认真"),
                TS("当前目标更新——“等待大福出现”"),
                TN("阳光渐渐西斜。保安叔叔回到岗亭，小凌坐到附近的长椅上等大福。")
            };
        }

        /// <summary>Lin WeChat-style friend-request chat before SC-09 café.</summary>
        public static List<TalkBeat> BuildLinContactBeats()
        {
            return new List<TalkBeat>
            {
                TN("十几分钟后，小凌的手机收到一条新的好友申请。", "消息提示音"),
                TS("新的联系人——“林女士”"),
                TB("林女士", "你好，我是林敏。", "常态"),
                TB("林女士", "保安跟我说，你想了解大福以前的事。", "常态"),
                TB("小凌", "您好，我是《此间》的记者小凌。", "常态"),
                TB("小凌", "我今天已经在社区看过大福，也跟保安了解了一些情况。还有些它当时受伤、治疗和后来送回社区的细节，想跟您核实一下。", "常态"),
                TB("小凌", "请问您明天下午方便接受一个短采访吗？", "常态"),
                TB("林女士", "可以。", "常态"),
                TB("林女士", "明天下午三点左右我有空。小区南门外有家咖啡馆，就约在那里吧，人少一点，方便说话。", "常态"),
                TB("小凌", "好，麻烦您把定位发我一下。", "常态"),
                TS("位置共享——槐安社区南门外·咖啡馆"),
                TB("林女士", "就是这家。", "常态"),
                TB("小凌", "收到。那明天下午三点见，谢谢您。", "常态"),
                TB("林女士", "好，明天见。", "常态"),
                TS("发现采访对象——“林女士”"),
                TS("解锁人物档案——“林女士”"),
                TS("任务更新——“明天下午15:00前往咖啡馆采访林女士”")
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
                pendingGuardAppear = true;
            }
        }

        /// <summary>True once after both required intel are granted; starts the door-SE cutscene.</summary>
        public bool ConsumePendingGuardAppear()
        {
            if (!pendingGuardAppear) return false;
            pendingGuardAppear = false;
            return true;
        }

        /// <summary>
        /// Script beat after 固定投喂点 + 大福的固定休息点:
        /// 【背景：保安亭_午后】【SE：远处保安亭开门声】→ 出场 → unlock map hotspot (not auto SC-05).
        /// </summary>
        public static List<InspectBeat> BuildGuardAppearBeats()
        {
            return new List<InspectBeat>
            {
                new InspectBeat
                {
                    narration = true,
                    background = "保安亭_午后",
                    sfx = "远处保安亭开门声",
                    text = "忽然，一名穿着保安制服的中年男人从保安亭内走出，将保温杯放在窗台上。"
                },
                new InspectBeat
                {
                    narration = false,
                    speaker = "小凌",
                    portrait = "常态",
                    text = "是帖子里和大福一起出现的保安。"
                },
                new InspectBeat
                {
                    narration = false,
                    speaker = "小凌",
                    portrait = "常态",
                    text = "他应该知道大福什么时候会来。"
                },
                new InspectBeat
                {
                    system = true,
                    text = "解锁交谈对象——“保安叔叔”"
                },
                new InspectBeat
                {
                    system = true,
                    text = "当前目标更新——“向保安询问大福的情况”"
                },
                new InspectBeat
                {
                    system = true,
                    text = "解锁地点“保安亭”"
                }
            };
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
                GameState.Instance.GrantIntel(topic.grantIntel2, topic.noteLine2);
            if (!string.IsNullOrEmpty(topic.setObjective))
                GameState.Instance.SetObjective(topic.setObjective);
            if (topic.unlocksLinFlow)
                GameState.Instance.SetFlag(FlagIds.LinUnlocked, false); // identity only; contact later

            return topic.reply ?? "";
        }

        public bool CanWaitForDafu()
        {
            return GameState.Instance.HasIntel(IntelIds.DafuAppearTime);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Clear hotspot/talk topic progress so debug jumps don't inherit a stuck map/talk state.
        /// (Beat queues live on GameUI; those are cleared via GameUI.DebugCloseOverlays.)
        /// </summary>
        public void ResetForDebugJump()
        {
            pendingGuardAppear = false;
            if (Hotspots != null)
            {
                foreach (var h in Hotspots)
                    h.inspected = false;
            }
            if (GuardTopics != null)
            {
                foreach (var t in GuardTopics)
                    t.done = false;
            }
            if (PostInterviewTopics != null)
            {
                foreach (var t in PostInterviewTopics)
                    t.done = false;
            }
        }
#endif
    }
}
