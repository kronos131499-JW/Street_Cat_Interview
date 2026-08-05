using System.Collections.Generic;
using StreetCat.Data;

namespace StreetCat.Narrative
{
    /// <summary>Embedded Chapter 1 script spine so the project runs without external JSON.</summary>
    public static class BuiltInScripts
    {
        public static ScriptDatabase Create()
        {
            var db = new ScriptDatabase();
            db.scenes.Add(Sc01());
            db.scenes.Add(Sc02());
            db.scenes.Add(Sc03());
            db.scenes.Add(Sc04());
            db.scenes.Add(Sc05());
            db.scenes.Add(Sc06());
            db.scenes.Add(Sc08());
            return db;
        }

        static ScriptLine L(string name, string text, LineSpeaker sp = LineSpeaker.Character) =>
            new ScriptLine { speakerName = name, text = text, speaker = sp };

        static ScriptLine Inner(string text) =>
            new ScriptLine { speakerName = "小凌", text = text, speaker = LineSpeaker.Inner };

        static ScriptLine Sys(string text) =>
            new ScriptLine { speakerName = "系统", text = text, speaker = LineSpeaker.System };

        static ScriptScene Sc01()
        {
            var s = new ScriptScene { id = SceneIds.SC01, title = "周五下班前", backgroundLabel = "此间杂志社_傍晚" };
            s.lines.Add(L("旁白", "编辑部已经空了大半。窗外是傍晚。", LineSpeaker.Narration));
            s.lines.Add(Inner("唉……怎么改都觉得不对味……"));
            s.lines.Add(Inner("我叫小凌，今年25岁，在《此间》杂志做记者兼编辑。"));
            s.lines.Add(Inner("工作内容包括采访、约稿、改稿、催稿，以及尽量不让作者发现他的稿子被我删掉了三分之一。"));
            s.lines.Add(L("沈禾", "来一下我办公室。"));
            s.lines.Add(Inner("周五。下午五点四十七。主编。办公室。"));
            s.lines.Add(Inner("肯定不会是为了祝我周末愉快。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "选项",
                text = "前往沈禾办公室",
                choices = new List<ScriptChoice>
                {
                    new ScriptChoice { label = "前往沈禾办公室", nextSceneId = SceneIds.SC02 }
                }
            });
            return s;
        }

        static ScriptScene Sc02()
        {
            var s = new ScriptScene { id = SceneIds.SC02, title = "喵语翻译器", backgroundLabel = "沈禾办公室" };
            s.lines.Add(L("旁白", "沈禾把一个白色设备盒推到桌边。", LineSpeaker.Narration));
            s.lines.Add(L("小凌", "这什么？"));
            s.lines.Add(L("沈禾", "喵语翻译器。"));
            s.lines.Add(L("小凌", "……啥？"));
            s.lines.Add(L("沈禾", "老板投资的项目。测试版，号称能通过叫声、动作和环境信息推测猫想表达什么。"));
            s.lines.Add(L("小凌", "听起来很像老板需要我们证明他的钱没白花。"));
            s.lines.Add(L("沈禾", "理解得很准确。"));
            s.lines.Add(L("沈禾", "编辑部准备拿它试一期报道。你去找一只流浪猫，采访它。"));
            s.lines.Add(L("小凌", "有现成的采访对象吗？"));
            s.lines.Add(L("沈禾", "没有，你自己找。找一只有故事的。最好周围还有认识它的人，猫说不清楚的东西，可以找人核实。"));
            s.lines.Add(L("小凌", "所以翻译结果也不能直接当事实。"));
            s.lines.Add(L("沈禾", "人说的话都不能，何况猫。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "获得道具「喵语翻译器」。任务更新：寻找合适的流浪猫采访对象。",
                setFlag = FlagIds.HasTranslator,
                setObjective = "寻找合适的流浪猫采访对象。",
                nextSceneId = SceneIds.SC03
            });
            return s;
        }

        static ScriptScene Sc03()
        {
            var s = new ScriptScene { id = SceneIds.SC03, title = "保安猫大福", backgroundLabel = "工位" };
            s.lines.Add(L("旁白", "小凌回到工位，打开本地社交媒体，划过几条流浪猫相关帖子。", LineSpeaker.Narration));
            s.lines.Add(Sys("帖子：「我们小区保安最近多了个同事」"));
            s.lines.Add(L("旁白", "配图是一只胖橘猫端坐在社区门口的快递柜上。正文提到大福曾受重伤，被好心人送医后又回到小区，如今由多人照料。", LineSpeaker.Narration));
            s.lines.Add(L("小凌", "这个好像还不错。受过重伤，被人救回来，最后又回到原来的小区。而且现在还有一群长期认识它的人。"));
            s.lines.Add(L("小凌", "我感觉，猫自己记得的，和这些人知道的，应该会很不一样。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "发现采访对象「大福」。解锁地点「槐安社区」。",
                setFlag = FlagIds.FoundDafu,
                grantIntel = IntelIds.DafuWasRescued,
                noteLine = "大福曾受过严重的伤，康复后被放归原社区。",
                setObjective = "前往槐安社区寻找大福。"
            });
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.Character,
                speakerName = "小凌",
                text = "槐安社区……去看看再说！",
                setFlag = FlagIds.UnlockedHuaiAn,
                nextSceneId = SceneIds.SC04
            });
            return s;
        }

        static ScriptScene Sc04()
        {
            var s = new ScriptScene { id = SceneIds.SC04, title = "槐安社区", backgroundLabel = "槐安社区_午后" };
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "首次进入调查场景。场景中的物件可以【调查】，也可与人物交谈以获取情报。",
                setFlag = FlagIds.InvestigateTutorialShown,
                setObjective = "在社区内寻找大福的线索。",
                openInvestigation = true
            });
            s.lines.Add(L("旁白", "小凌站在社区入口附近。前方是保安亭、快递柜和绿化带。", LineSpeaker.Narration));
            return s;
        }

        static ScriptScene Sc05()
        {
            var s = new ScriptScene { id = SceneIds.SC05, title = "保安亭", backgroundLabel = "保安亭_午后" };
            s.lines.Add(L("小凌", "叔叔，打扰一下。我想问一下，大福是在这边吗？"));
            s.lines.Add(L("保安叔叔", "还没来。你找它干吗？"));
            s.lines.Add(L("小凌", "我是《此间》杂志的记者，想来做个采访。我可以先问您一些关于大福的问题吗？"));
            s.lines.Add(L("保安叔叔", "行啊，你问吧。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "解锁交谈环节。",
                openTalkMenu = true
            });
            return s;
        }

        static ScriptScene Sc06()
        {
            var s = new ScriptScene { id = SceneIds.SC06, title = "上班的大福", backgroundLabel = "保安亭_傍晚" };
            s.lines.Add(L("保安叔叔", "小姑娘，大福来了。"));
            s.lines.Add(L("旁白", "一只橘猫从电动车之间钻出来，慢悠悠走向保安亭。", LineSpeaker.Narration));
            s.lines.Add(Inner("来上班打卡了。"));
            s.lines.Add(L("保安叔叔", "它有点怕生，你别一下靠太近。"));
            s.lines.Add(L("小凌", "好。"));
            s.lines.Add(Sys("启动喵语翻译器。目标：大福。当前状态：警惕。"));
            s.lines.Add(L("小凌", "大福？"));
            s.lines.Add(Sys("译文——「你在叫我？」"));
            s.lines.Add(L("旁白", "小凌撕开猫条。大福试探靠近，埋头吃起来。警惕降低。", LineSpeaker.Narration));
            s.lines.Add(Sys("译文——「还有吗？」"));
            s.lines.Add(L("小凌", "没了，就带了一根。下次多带点。"));
            s.lines.Add(Inner("能听懂我说的。我也能听懂它说的。好神奇。"));
            s.lines.Add(L("小凌", "大福，我能问你一些事情吗？"));
            s.lines.Add(Sys("译文——「可以。」进入采访模式。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "解锁第一次自由采访。",
                nextSceneId = SceneIds.SC07
            });
            return s;
        }

        static ScriptScene Sc08()
        {
            var s = new ScriptScene { id = SceneIds.SC08, title = "寻找林女士", backgroundLabel = "保安亭_傍晚" };
            s.lines.Add(L("保安叔叔", "你刚刚在和猫说话？"));
            s.lines.Add(L("小凌", "呃…算是吧？对了，我可以再问您一些问题吗？"));
            s.lines.Add(L("保安叔叔", "可以啊，你问。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "根据采访线索向保安打听当年的救助者。",
                openTalkMenu = true,
                setObjective = "向保安询问大福记忆中的女人。"
            });
            return s;
        }
    }
}
