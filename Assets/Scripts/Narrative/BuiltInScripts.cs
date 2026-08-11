using System.Collections.Generic;
using StreetCat.Data;

namespace StreetCat.Narrative
{
    /// <summary>
    /// Chapter 1 fixed dialogue imported from Docs/Scripts (SC10 审核补全统一格式版).
    /// 旁白使用 LineSpeaker.Narration（不显示角色名）。
    /// Regenerate via: Tools/import_chapter1_script.py
    /// </summary>
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
            db.scenes.Add(Sc09());
            db.scenes.Add(Sc10());
            return db;
        }

        static ScriptLine L(string name, string text, string portrait = null, LineSpeaker sp = LineSpeaker.Character) =>
            new ScriptLine { speakerName = name, text = text, speaker = sp, portrait = portrait };

        static ScriptLine N(string text) =>
            new ScriptLine { speakerName = "", text = text, speaker = LineSpeaker.Narration };

        static ScriptLine Inner(string text, string portrait = null) =>
            new ScriptLine { speakerName = "小凌", text = text, speaker = LineSpeaker.Inner, portrait = portrait };

        static ScriptLine Sys(string text) =>
            new ScriptLine { speakerName = "系统", text = text, speaker = LineSpeaker.System };

        static ScriptLine Bg(string background) =>
            new ScriptLine { speakerName = "", text = "", speaker = LineSpeaker.Narration, background = background };

        static ScriptLine Bgm(string bgm) =>
            new ScriptLine { speakerName = "", text = "", speaker = LineSpeaker.Narration, bgm = bgm };

        static ScriptLine Sfx(string sfx) =>
            new ScriptLine { speakerName = "", text = "", speaker = LineSpeaker.Narration, sfx = sfx };

        /// <summary>【演出】center prop; click to continue. Key under Resources/VnArt/Props/.</summary>
        static ScriptLine Prop(string propKey) =>
            new ScriptLine { speakerName = "", text = "", speaker = LineSpeaker.Narration, prop = propKey };

        /// <summary>【演出】hide sticky center prop (auto-advances like other cue-only beats).</summary>
        static ScriptLine PropHide() =>
            new ScriptLine { speakerName = "", text = "", speaker = LineSpeaker.Narration, hideProp = true };

        /// <summary>【演出】social phone overlay cue; empty text waits for click (except hide).</summary>
        static ScriptLine Social(string cue) =>
            new ScriptLine { speakerName = "", text = "", speaker = LineSpeaker.Narration, social = cue };

        static ScriptLine SocialSys(string text, string cue) =>
            new ScriptLine { speakerName = "系统", text = text, speaker = LineSpeaker.System, social = cue };

        static ScriptLine SocialHide() =>
            new ScriptLine { speakerName = "", text = "", speaker = LineSpeaker.Narration, social = "hide" };

        static ScriptScene Sc01()
        {
            var s = new ScriptScene { id = SceneIds.SC01, title = "周五下班前", backgroundLabel = "编辑部_傍晚" };
            s.lines.Add(Bgm("编辑部日常_01（循环）"));
            s.lines.Add(Sfx("键盘声、远处打印机声、零散办公环境音"));
            s.lines.Add(N("周五傍晚，编辑部已经空了大半。窗外的天色慢慢暗下来，小凌还坐在电脑前改稿。"));
            s.lines.Add(Sys("《凌晨两点，我遇见了这座城市真正的灵魂》"));
            s.lines.Add(Sfx("键盘删除声"));
            s.lines.Add(N("电脑文档里的“真正的”被删去。"));
            s.lines.Add(N("小凌停顿片刻，又把“真正的”加了回来。"));
            s.lines.Add(L("小凌", "唉......怎么改都觉得不对味......", "认真"));
            s.lines.Add(N("我叫小凌，今年25岁，在《此间》杂志做记者兼编辑。"));
            s.lines.Add(N("工作内容包括采访、约稿、改稿、催稿，以及尽量不让作者发现他的稿子被我删掉了三分之一。"));
            s.lines.Add(Sfx("消息提示音"));
            s.lines.Add(Sys("工作软件弹窗"));
            s.lines.Add(L("沈禾", "来一下我办公室。", "无立绘"));
            s.lines.Add(N("电脑屏幕右下角显示：18:47。"));
            s.lines.Add(N("周五。"));
            s.lines.Add(N("下午六点四十七。"));
            s.lines.Add(N("主编。"));
            s.lines.Add(N("办公室。"));
            s.lines.Add(N("......肯定不会是为了祝我周末愉快。"));
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
            var s = new ScriptScene { id = SceneIds.SC02, title = "喵语翻译器", backgroundLabel = "沈禾办公室_傍晚" };
            s.lines.Add(Sfx("椅子移动声"));
            s.lines.Add(Bgm("沈禾办公室"));
            s.lines.Add(N("小凌进入办公室。沈禾将一个猫咪形状的设备盒推到桌边。"));
            // 【演出：喵语翻译器-关机状态的图片出现在画面中央】（sticky until PropHide）
            s.lines.Add(Prop("prop_translator_off"));
            s.lines.Add(L("小凌", "这什么？", "思考"));
            s.lines.Add(L("沈禾", "喵语翻译器。", "平静"));
            s.lines.Add(L("小凌", "……啥？", "惊讶"));
            s.lines.Add(L("沈禾", "老板投资的项目。测试版，号称能通过叫声、动作和环境信息推测猫想表达什么。", "平静"));
            s.lines.Add(L("小凌", "听起来很像老板需要我们证明他的钱没白花。", "吐槽"));
            s.lines.Add(L("沈禾", "理解得很准确。", "平静"));
            s.lines.Add(N("沈禾将设备递给小凌。"));
            s.lines.Add(L("沈禾", "编辑部准备拿它试一期报道。", "平静"));
            s.lines.Add(L("沈禾", "你去找一只流浪猫，采访它。", "平静"));
            s.lines.Add(L("小凌", "有现成的采访对象吗？", "常态"));
            s.lines.Add(L("沈禾", "没有，你自己找。", "无奈"));
            s.lines.Add(L("小凌", "那范围呢？救助站、社区猫，还是随便什么猫都行？", "思考"));
            s.lines.Add(L("沈禾", "都行。重点不是找只猫来试机器，是找一个值得写的故事。", "认真"));
            // 【停顿】via player click between lines (no timed pause support)
            s.lines.Add(L("沈禾", "找一只有故事的。最好周围还有认识它的人，猫说不清楚的东西，可以找人核实。", "认真"));
            s.lines.Add(L("小凌", "所以翻译结果也不能直接当事实。", "思考"));
            s.lines.Add(L("沈禾", "人说的话都不能，何况猫。", "无奈"));
            s.lines.Add(L("小凌", "行，那我先去思考下采访对象。", "认真"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "获得道具“喵语翻译器”",
                setFlag = FlagIds.HasTranslator
            });
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "任务更新——“寻找合适的流浪猫采访对象”",
                setObjective = "寻找合适的流浪猫采访对象。"
            });
            // 【演出：喵语翻译器-关机状态的图片从画面中央消失】→ SC-03
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.Narration,
                speakerName = "",
                text = "",
                hideProp = true,
                nextSceneId = SceneIds.SC03
            });
            return s;
        }

        static ScriptScene Sc03()
        {
            var s = new ScriptScene { id = SceneIds.SC03, title = "保安猫大福", backgroundLabel = "编辑部_工位_傍晚" };
            // Keep SC-02 沈禾办公室 BGM (sticky); do not switch to 编辑部日常_01.
            s.lines.Add(N("小凌回到工位，打开本地社交媒体，开始寻找适合采访的流浪猫。"));
            s.lines.Add(SocialSys("进入“选题搜索”", "enter"));
            s.lines.Add(N("她快速划过几条流浪猫相关帖子。"));
            s.lines.Add(SocialSys("帖子一：“如何科学投喂流浪猫”", "post1"));
            s.lines.Add(SocialSys("帖子二：“超可爱狸花猫找领养”", "post2"));
            s.lines.Add(SocialSys("帖子三：“我们小区保安最近多了个同事”", "post3"));
            s.lines.Add(N("第三条帖子停在屏幕中央，被小凌点开。"));
            s.lines.Add(Social("detail"));
            s.lines.Add(Sys("配图：一只胖橘猫端坐在社区门口的快递柜上。"));
            s.lines.Add(Sys("给大家介绍一下我们小区的编外保安——大福。"));
            s.lines.Add(Sys("每天下午出现，保安叔叔上班它也上班，偶尔还会跟着巡逻。工资是猫粮和罐头，工作内容主要包括睡觉、晒太阳以及监督快递柜。"));
            s.lines.Add(Sys("别看它现在这么胖，大福刚来这里的时候其实特别惨，当时受了很严重的伤，后来被小区里的好心人送去医院救了回来。"));
            s.lines.Add(Sys("治好以后，大福又回到了我们小区。现在有人给它换水，有人喂饭，有人给它搭窝，门口保安叔叔更是默认多了个同事。天气好的时候它就趴在快递柜上，到了晚上还会和一只狸花猫一起出去玩，可幸福了。"));
            s.lines.Add(L("小凌", "这个好像还不错。", "思考"));
            s.lines.Add(L("小凌", "受过重伤，被人救回来，最后又回到原来的小区。而且现在还有一群长期认识它的人。", "思考"));
            s.lines.Add(N("")); // 【停顿】
            s.lines.Add(L("小凌", "我感觉，猫自己记得的，和这些人知道的，应该会很不一样。", "思考"));
            s.lines.Add(SocialHide());
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "发现采访对象“大福”",
                setFlag = FlagIds.FoundDafu
            });

            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "记者笔记新增——“大福”",
                grantIntel = IntelIds.DafuWasRescued,
                noteLine = "大福曾受过严重的伤，康复后被放归原社区。"
            });
            s.lines.Add(N("小凌收藏了帖子，顺手查看发帖定位。"));
            s.lines.Add(L("小凌", "槐安社区……", "思考"));
            // 【演出：喵语翻译器-关机状态的图片出现在画面中央】
            s.lines.Add(Prop("prop_translator_off"));
            s.lines.Add(N("她拿起桌边的喵语翻译器，放进包里。"));
            s.lines.Add(L("小凌", "行。", "常态"));
            s.lines.Add(L("小凌", "去看看再说！", "常态"));
            // 【演出：喵语翻译器-关机状态的图片从画面消失】
            s.lines.Add(PropHide());
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "解锁地点“槐安社区”",
                setFlag = FlagIds.UnlockedHuaiAn
            });
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "任务更新——“前往槐安社区寻找大福”",
                setObjective = "前往槐安社区寻找大福。"
            });
            // Keep sticky 沈禾办公室 until fade; do not force 编辑部日常_01.
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.Narration,
                speakerName = "",
                text = "",
                bgm = "淡出",
                nextSceneId = SceneIds.SC04
            });
            return s;
        }

        static ScriptScene Sc04()
        {
            var s = new ScriptScene { id = SceneIds.SC04, title = "槐安社区", backgroundLabel = "槐安社区_午后" };
            s.lines.Add(Bgm("社区午后_01（循环）"));
            s.lines.Add(Sfx("鸟叫、树叶摩擦声、远处车辆声"));
            s.lines.Add(N("第二天下午，小凌按照帖子里的定位来到槐安社区。入口旁是保安亭、快递柜，以及沿步道分布的绿化带。"));
            s.lines.Add(L("小凌", "哎，这里有个社区的平面图，让我瞅瞅。", "常态"));
            s.lines.Add(Bg("槐安社区_社区平面图"));
            s.lines.Add(Sys("首次进入调查场景，触发调查教学"));
            s.lines.Add(Sys("场景中的部分物件可以进行【调查】。调查环境或与人物交谈，可能获得新的情报与采访方向。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "当前目标更新——“在社区内寻找大福的线索”",
                setFlag = FlagIds.InvestigateTutorialShown,
                setObjective = "在社区内寻找大福的线索。",
                openInvestigation = true
            });
            return s;
        }

        static ScriptScene Sc05()
        {
            var s = new ScriptScene { id = SceneIds.SC05, title = "保安亭", backgroundLabel = "保安亭_午后" };
            s.lines.Add(Bgm("保安亭_01（循环）"));
            s.lines.Add(N("保安叔叔从岗亭里走出来，拎起窗台上的水杯。小凌走过去。"));
            s.lines.Add(L("小凌", "叔叔，打扰一下。", "常态"));
            s.lines.Add(L("保安叔叔", "嗯？", "疑惑"));
            s.lines.Add(L("小凌", "我想问一下，大福是在这边吗？", "常态"));
            s.lines.Add(L("保安叔叔", "大福？", "疑惑"));
            s.lines.Add(N("保安叔叔往快递柜上看了一眼。"));
            s.lines.Add(L("保安叔叔", "还没来。", "常态"));
            s.lines.Add(L("小凌", "它每天都会来？", "思考"));
            s.lines.Add(L("保安叔叔", "差不多吧。", "常态"));
            s.lines.Add(L("保安叔叔", "你找它干吗？", "疑惑"));
            s.lines.Add(L("小凌", "我是《此间》杂志的记者，想来做个采访。", "认真"));
            s.lines.Add(L("保安叔叔", "哦。", "常态"));
            s.lines.Add(L("保安叔叔", "你要采访谁?", "疑惑"));
            s.lines.Add(L("小凌", "呃……", "局促"));
            s.lines.Add(Inner("我直接说我要采访猫，可能会被当成奇怪的人……", "局促"));
            s.lines.Add(L("小凌", "我可以先问您一些关于大福的问题吗？", "常态"));
            s.lines.Add(L("保安叔叔", "行啊，你问吧。", "常态"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "解锁交谈环节。",
                setFlag = FlagIds.GuardIntroDone,
                openTalkMenu = true
            });
            return s;
        }

        static ScriptScene Sc06()
        {
            var s = new ScriptScene { id = SceneIds.SC06, title = "上班的大福", backgroundLabel = "保安亭_傍晚" };
            s.lines.Add(Bgm("大福的出现（循环）"));
            s.lines.Add(Sfx("树叶摩擦声、远处自行车铃声"));
            s.lines.Add(N("傍晚，小凌坐在长椅上，一边刷手机，一边等大福出现。"));
            s.lines.Add(L("保安叔叔", "小姑娘，大福来了。", "常态"));
            s.lines.Add(N("小凌抬起头。一只橘猫从停放的电动车之间钻出来，慢悠悠地朝保安亭走去。"));
            s.lines.Add(N("大福走到保安亭前，冲着里面叫了一声。"));
            s.lines.Add(Sfx("猫叫声"));
            s.lines.Add(L("大福", "喵——", "放松"));
            s.lines.Add(N("保安叔叔从岗亭里出来，给大福添了一点猫粮。"));
            s.lines.Add(L("小凌", "来上班打卡了。", "常态"));
            s.lines.Add(N("小凌起身靠近。大福很快注意到她，停下吃东西的动作，警惕地往后退了一步。"));
            s.lines.Add(L("保安叔叔", "它有点怕生，你别一下靠太近。", "常态"));
            s.lines.Add(L("小凌", "好。", "常态"));
            s.lines.Add(N("小凌停在原地，从包里拿出喵语翻译器并启动。"));
            s.lines.Add(Sfx("设备启动提示音"));
            s.lines.Add(Sys("目标识别完成"));
            s.lines.Add(Sys("目标：大福"));
            s.lines.Add(Sys("当前状态：警惕"));
            s.lines.Add(Sys("双向交流模式开启"));
            s.lines.Add(Sys("可将人类语言转译为猫能够理解的表达，同时解析猫的叫声与行为。"));
            s.lines.Add(L("小凌", "那先试试。", "思考"));
            s.lines.Add(N("小凌按住翻译键，尝试呼唤大福。"));
            s.lines.Add(L("小凌", "大福？", "思考"));
            s.lines.Add(Sfx("转译音"));
            s.lines.Add(N("设备发出经过转译的猫叫声。大福耳朵动了一下，抬头看向小凌。"));
            s.lines.Add(L("大福", "喵？", "警惕"));
            s.lines.Add(Sfx("喵叫声_04"));
            s.lines.Add(Sys("译文——“你在叫我？”"));
            s.lines.Add(L("小凌", "……真听懂了？", "惊讶"));
            s.lines.Add(N("大福仍然没有靠近。小凌想了想，从包里拿出一根猫条。"));
            s.lines.Add(L("小凌", "还好我有准备。", "常态"));
            s.lines.Add(Sfx("猫条包装撕开声"));
            s.lines.Add(N("大福的视线立刻落在猫条上。"));
            s.lines.Add(N("小凌蹲下，与大福保持距离，按下翻译键。"));
            s.lines.Add(L("小凌", "给你吃的。", "常态"));
            s.lines.Add(Sfx("转译音"));
            s.lines.Add(Sys("人类 → 猫语"));
            s.lines.Add(L("小凌", "“食物，给你。”"));
            s.lines.Add(N("大福闻了闻空气，试探着向前走了两步。"));
            s.lines.Add(L("小凌", "我不过去，你自己来。", "常态"));
            s.lines.Add(Sys("人类 → 猫语"));
            s.lines.Add(L("小凌", "“没有危险，可以靠近。”"));
            s.lines.Add(N("大福犹豫片刻，终于凑到猫条前舔了一口。很快便放下戒心，开始埋头吃起来。"));
            s.lines.Add(Sys("状态更新——警惕降低"));
            s.lines.Add(Sys("大福信任提升"));
            s.lines.Add(N("吃到一半，大福已经主动站到小凌身边。小凌伸出手，大福凑过来闻了闻，没有躲开。于是小凌轻轻摸了一下大福的脑袋。"));
            s.lines.Add(Inner("好像可以了。", "惊讶"));
            s.lines.Add(N("猫条吃完。大福舔舔嘴，在小凌旁边坐下，没有离开。"));
            s.lines.Add(Sfx("喵叫声_02"));
            s.lines.Add(L("大福", "喵喵。", "放松"));
            s.lines.Add(Sfx("翻译提示音"));
            s.lines.Add(Sys("猫语 → 人类"));
            s.lines.Add(L("大福", "“还有吗？”"));
            s.lines.Add(L("小凌", "没了，就带了一根。", "常态"));
            s.lines.Add(Sfx("转译音"));
            s.lines.Add(N("大福盯着她手里空掉的包装看了一会儿。"));
            s.lines.Add(L("大福", "喵呜。", "不满"));
            s.lines.Add(Sfx("喵叫声_03"));
            s.lines.Add(Sys("译文——“没有了啊。”"));
            s.lines.Add(L("小凌", "下次多带点。", "常态"));
            s.lines.Add(Sfx("转译音"));
            s.lines.Add(N("大福虽有些不满，但没有离开，反而在原地趴下。"));
            s.lines.Add(N("小凌看了看大福，又看向手里的喵语翻译器。"));
            s.lines.Add(Inner("居然能听懂我说的。", "惊讶"));
            s.lines.Add(Inner("我也能听懂它说的。好神奇。", "惊讶"));
            s.lines.Add(Sys("进入“采访模式”"));
            s.lines.Add(Sys("采访过程中，可直接输入想向大福询问的问题。"));
            s.lines.Add(Sys("当问题超出大福的认知范围时，需要尝试更换提问方式。"));
            s.lines.Add(L("小凌", "大福。", "认真"));
            s.lines.Add(N("大福抬起头看向小凌。"));
            s.lines.Add(L("小凌", "我能问你一些事情吗？", "认真"));
            s.lines.Add(Sfx("转译音"));
            s.lines.Add(L("大福", "喵。", "放松"));
            s.lines.Add(Sfx("喵叫声_05"));
            s.lines.Add(Sys("译文——“可以。”"));
            s.lines.Add(Sys("采访对象“大福”已建立基础信任"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "解锁第一次自由采访",
                nextSceneId = SceneIds.SC07
            });
            return s;
        }

        static ScriptScene Sc08()
        {
            var s = new ScriptScene { id = SceneIds.SC08, title = "寻找林女士", backgroundLabel = "保安亭_傍晚" };
            s.lines.Add(Bgm("社区傍晚_01（循环）"));
            s.lines.Add(N("采访结束后，大福跳上快递柜趴下。小凌收起喵语翻译器，重新走向保安亭。"));
            s.lines.Add(L("保安叔叔", "你刚刚在和猫说话？", "疑惑"));
            s.lines.Add(L("小凌", "呃……算是吧？我能听懂猫语。", "局促"));
            s.lines.Add(L("保安叔叔", "现在的年轻人真厉害！", "常态"));
            s.lines.Add(L("小凌", "刚才您说的那个救助人，我想再确认一下。", "认真"));
            s.lines.Add(L("保安叔叔", "嗯，你问吧。", "常态"));
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

        static ScriptScene Sc09()
        {
            var s = new ScriptScene { id = SceneIds.SC09, title = "咖啡馆采访", backgroundLabel = "咖啡馆_午后" };
            s.lines.Add(Bgm("咖啡馆日常_01（循环）"));
            s.lines.Add(Sfx("低声交谈、咖啡机蒸汽声、偶尔的杯碟轻响"));
            s.lines.Add(N("第二天下午，小凌提前十分钟到了约好的咖啡馆。"));
            s.lines.Add(N("她选了一个靠里的位置，把记者笔记和手机放在桌边。"));
            s.lines.Add(N("三点刚过，林女士推门进来。小凌起身朝她招了招手。"));
            s.lines.Add(L("小凌", "林女士？您好，我是小凌。", "常态"));
            s.lines.Add(L("林女士", "你好。没等很久吧？", "常态"));
            s.lines.Add(L("小凌", "没有，我也刚到。谢谢您愿意抽时间过来。", "常态"));
            s.lines.Add(N("两人坐下。等服务员放下饮品离开后，小凌打开记者笔记。"));
            s.lines.Add(L("小凌", "我昨天先问了保安一些情况，也在社区见到了大福。", "认真"));
            s.lines.Add(L("小凌", "今天主要想把它当时受伤、送医治疗，还有后来送回社区的经过核实清楚。", "认真"));
            s.lines.Add(L("林女士", "嗯，可以。你问吧。", "常态"));
            s.lines.Add(L("小凌", "我这边方便录音吗？只用来整理采访内容。", "认真"));
            s.lines.Add(L("林女士", "可以。", "常态"));
            s.lines.Add(N("小凌打开手机录音，将它放到桌边。"));
            s.lines.Add(L("小凌", "如果有不方便回答的，您可以直接告诉我，我们随时换一个问题。", "认真"));
            s.lines.Add(L("林女士", "好。", "常态"));
            s.lines.Add(N("小凌把记者笔记翻到“大福”那一页。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "第二次自由采访开始。可根据记者笔记，自由向林女士提问。",
                setFlag = FlagIds.LinCafeIntroDone,
                setObjective = "采访林女士，核实大福的救助经过。",
                openInterview = true
            });
            return s;
        }

        static ScriptScene Sc10()
        {
            var s = new ScriptScene { id = SceneIds.SC10, title = "写稿与沈禾审核", backgroundLabel = "编辑部工位_上午" };
            s.lines.Add(Bgm("编辑部日常_02（循环）"));
            s.lines.Add(Sfx("键盘声、鼠标点击声、零散办公环境音"));
            s.lines.Add(N("周一上午，小凌回到编辑部，把社区调查、保安的证词、大福的采访和林女士的录音一起摊在电脑前。"));
            s.lines.Add(L("小凌", "好了。该把这些东西变成一篇能发的稿子了。", "认真"));
            s.lines.Add(Sys("进入【素材整理与写稿】流程。"));
            s.lines.Add(new ScriptLine
            {
                speaker = LineSpeaker.System,
                speakerName = "系统",
                text = "玩家前期获得的情报将转化为固定素材卡；未在调查或采访中获得的内容不会出现在写稿列表中。",
                setFlag = FlagIds.WritingUnlocked,
                setObjective = "整理素材，完成报道。",
                openWriting = true
            });
            return s;
        }
    }
}
