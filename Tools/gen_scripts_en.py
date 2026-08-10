# -*- coding: utf-8 -*-
"""Extract BuiltInScripts lines and emit scripts_en.json with EN translations."""
import re
import json
from pathlib import Path

ROOT = Path(r"D:\Street_Cat_Interview\github\Street_Cat_Interview")
SRC = ROOT / "Assets/Scripts/Narrative/BuiltInScripts.cs"
OUT = ROOT / "Assets/Resources/Loc/scripts_en.json"

ID_MAP = {
    "Sc01": "SC-01", "Sc02": "SC-02", "Sc03": "SC-03", "Sc04": "SC-04",
    "Sc05": "SC-05", "Sc06": "SC-06", "Sc08": "SC-08", "Sc09": "SC-09", "Sc10": "SC-10",
}

SPEAKER = {
    "小凌": "Ling",
    "沈禾": "Shen He",
    "保安叔叔": "Uncle Guard",
    "大福": "Dafu",
    "林女士": "Ms. Lin",
    "系统": "System",
    "选项": "Choice",
}

# Manual EN for every player-facing Chinese string in BuiltInScripts (exact match).
TR = {
    "周五下班前": "Friday Before Quitting Time",
    "喵语翻译器": "Meow Translator",
    "保安猫大福": "Guard Cat Dafu",
    "槐安社区": "Huai'an Community",
    "保安亭": "Guard Booth",
    "上班的大福": "Dafu on Duty",
    "寻找林女士": "Looking for Ms. Lin",
    "咖啡馆采访": "Cafe Interview",
    "写稿与沈禾审核": "Drafting & Shen He's Review",

    "周五傍晚，编辑部已经空了大半。窗外的天色慢慢暗下来，小凌还坐在电脑前改稿。":
        "Friday evening. Most of the newsroom is already empty. Outside, the sky is fading; Ling is still at her desk, revising a draft.",
    "《凌晨两点，我遇见了这座城市真正的灵魂》":
        '"At 2 a.m., I Met the City\'s True Soul"',
    "电脑文档里的“真正的”被删去。":
        'In the document, the word "true" is deleted.',
    "小凌停顿片刻，又把“真正的”加了回来。":
        'Ling pauses, then puts "true" back in.',
    "唉......怎么改都觉得不对味......":
        "Sigh... no matter how I rewrite it, it still feels off...",
    "我叫小凌，今年25岁，在《此间》杂志做记者兼编辑。":
        "I'm Ling, 25. Reporter and editor at Here & Now magazine.",
    "工作内容包括采访、约稿、改稿、催稿，以及尽量不让作者发现他的稿子被我删掉了三分之一。":
        "My job is interviews, commissions, edits, chasing deadlines—and hoping writers never notice I cut a third of their piece.",
    "工作软件弹窗": "Work chat popup",
    "来一下我办公室。": "Come to my office.",
    "电脑屏幕右下角显示：18:47。": "The corner of the screen reads 18:47.",
    "周五。": "Friday.",
    "下午六点四十七。": "6:47 p.m.",
    "主编。": "Editor-in-chief.",
    "办公室。": "Office.",
    "......肯定不会是为了祝我周末愉快。": "...definitely not to wish me a nice weekend.",
    "前往沈禾办公室": "Go to Shen He's office",

    "小凌进入办公室。沈禾将一个猫咪形状的设备盒推到桌边。":
        "Ling steps into the office. Shen He slides a cat-shaped device box to the edge of the desk.",
    "这什么？": "What's this?",
    "喵语翻译器。": "A meow translator.",
    "……啥？": "...Come again?",
    "老板投资的项目。测试版，号称能通过叫声、动作和环境信息推测猫想表达什么。":
        "A project the boss invested in. Beta build—claims it can infer what a cat means from cries, motion, and context.",
    "听起来很像老板需要我们证明他的钱没白花。":
        "Sounds like the boss needs us to prove his money wasn't wasted.",
    "理解得很准确。": "You've got it exactly.",
    "沈禾将设备递给小凌。": "Shen He hands the device to Ling.",
    "编辑部准备拿它试一期报道。": "The newsroom wants to try it for one feature.",
    "你去找一只流浪猫，采访它。": "Go find a stray cat and interview it.",
    "有现成的采访对象吗？": "Got a subject lined up?",
    "没有，你自己找。": "No. Find one yourself.",
    "那范围呢？救助站、社区猫，还是随便什么猫都行？":
        "Any limits? Shelters, community cats, or just any cat?",
    "都行。重点不是找只猫来试机器，是找一个值得写的故事。":
        "Anything goes. The point isn't to test the gadget on a cat—it's to find a story worth writing.",
    "找一只有故事的。最好周围还有认识它的人，猫说不清楚的东西，可以找人核实。":
        "Find one with a story. Ideally people nearby who know it—so when the cat can't explain, you can verify.",
    "所以翻译结果也不能直接当事实。": "So the translation can't be treated as fact either.",
    "人说的话都不能，何况猫。": "Even people can't. Cats, even less.",
    "行，那我先去思考下采访对象。": "Alright. I'll start thinking about who to interview.",
    "获得道具“喵语翻译器”": 'Obtained item: "Meow Translator"',
    "任务更新——“寻找合适的流浪猫采访对象”": 'Objective updated — "Find a suitable stray to interview"',

    "小凌回到工位，打开本地社交媒体，开始寻找适合采访的流浪猫。":
        "Back at her desk, Ling opens local social media and starts hunting for a stray worth interviewing.",
    "进入“选题搜索”": 'Enter "Story Search"',
    "她快速划过几条流浪猫相关帖子。": "She skims past several stray-cat posts.",
    "帖子一：": "Post 1:",
    "“如何科学投喂流浪猫”": '"How to Feed Strays Responsibly"',
    "帖子二：": "Post 2:",
    "“超可爱狸花猫找领养”": '"Adorable Tabby Looking for a Home"',
    "帖子三：": "Post 3:",
    "“我们小区保安最近多了个同事”": '"Our Community Guard Just Got a New Coworker"',
    "第三条帖子停在屏幕中央，被小凌点开。": "The third post sits centered on screen. Ling opens it.",
    "配图：一只胖橘猫端坐在社区门口的快递柜上。":
        "Photo: a plump orange cat perched on the parcel lockers by the gate.",
    "给大家介绍一下我们小区的编外保安——大福。每天下午出现，保安叔叔上班它也上班，偶尔还会跟着巡逻。":
        "Meet our unofficial guard—Dafu. Shows up every afternoon. When Uncle Guard is on shift, so is he. Sometimes he even tags along on patrol.",
    "工资是猫粮和罐头，工作内容主要包括睡觉、晒太阳以及监督快递柜。":
        "Pay is kibble and cans. Job duties: sleeping, sunbathing, and supervising the lockers.",
    "别看它现在这么胖，大福刚来这里的时候其实特别惨，当时受了很严重的伤，后来被小区里的好心人送去医院救了回来。":
        "Don't let the chubby look fool you. When Dafu first showed up he was in rough shape—badly hurt—until a kind neighbor got him to a clinic.",
    "治好以后，大福又回到了我们小区。现在有人给它换水，有人喂饭，有人给它搭窝，门口保安叔叔更是默认多了个同事。":
        "After treatment, Dafu came back. Now someone refreshes his water, someone feeds him, someone built a shelter—and the guard at the gate quietly gained a coworker.",
    "天气好的时候它就趴在快递柜上，到了晚上还会和一只狸花猫一起出去玩，可幸福了。":
        "On nice days he sprawls on the lockers; at night he even goes out with a tabby. Living his best life.",
    "这个好像还不错。": "This one looks promising.",
    "受过重伤，被人救回来，最后又回到原来的小区。":
        "Badly hurt, rescued, then returned to the same community.",
    "而且现在还有一群长期认识它的人。": "And a whole circle of people who've known him for a while.",
    "我感觉，猫自己记得的，和这些人知道的，应该会很不一样。":
        "I have a feeling what the cat remembers and what these people know won't match.",
    "发现采访对象“大福”": 'Interview subject found: "Dafu"',
    "获得初始情报": "Initial intel acquired",
    "大福通常在下午出现在社区": "Dafu usually appears in the community in the afternoon",
    "曾经受过严重的伤": "Was once seriously injured",
    "康复后被放归原社区": "Returned to the community after recovery",
    "目前由多名社区居民共同照顾": "Currently cared for by several residents",
    "记者笔记新增——“大福”": 'Reporter notebook updated — "Dafu"',
    "小凌收藏了帖子，顺手查看发帖定位。": "Ling bookmarks the post and checks the location tag.",
    "槐安社区……": "Huai'an Community...",
    "她拿起桌边的喵语翻译器，放进包里。": "She picks up the meow translator from the desk and slips it into her bag.",
    "行。": "Alright.",
    "去看看再说！": "Let's go see.",
    "解锁地点“槐安社区”": 'Location unlocked: "Huai\'an Community"',
    "任务更新——“前往槐安社区寻找大福”": 'Objective updated — "Go to Huai\'an Community and find Dafu"',

    "第二天下午，小凌按照帖子里的定位来到槐安社区。入口旁是保安亭、快递柜，以及沿步道分布的绿化带。":
        "The next afternoon, Ling follows the post's pin to Huai'an Community. By the entrance: a guard booth, parcel lockers, and greenery along the path.",
    "哎，这里有个社区的平面图，让我瞅瞅。": "Oh—there's a community map. Let me check.",
    "首次进入调查场景，触发调查教学": "First time in an investigation scene — tutorial starts",
    "场景中的部分物件可以进行【调查】。调查环境或与人物交谈，可能获得新的情报与采访方向。":
        "Some objects in the scene can be [Investigated]. Looking around or talking to people may yield intel and interview leads.",
    "当前目标更新——“在社区内寻找大福的线索”": 'Objective updated — "Search the community for leads on Dafu"',

    "保安叔叔从岗亭里走出来，拎起窗台上的水杯。小凌走过去。":
        "Uncle Guard steps out of the booth and picks up a cup from the sill. Ling walks over.",
    "叔叔，打扰一下。": "Excuse me, Uncle.",
    "嗯？": "Hmm?",
    "我想问一下，大福是在这边吗？": "Is Dafu around here?",
    "大福？": "Dafu?",
    "保安叔叔往快递柜上看了一眼。": "Uncle Guard glances toward the lockers.",
    "还没来。": "Not here yet.",
    "它每天都会来？": "Does he come every day?",
    "差不多吧。": "More or less.",
    "你找它干吗？": "What do you want with him?",
    "我是《此间》杂志的记者，想来做个采访。": "I'm a reporter with Here & Now. I'd like to do an interview.",
    "哦。": "Oh.",
    "你要采访谁?": "Who are you interviewing?",
    "呃……": "Uh...",
    "我直接说我要采访猫，可能会被当成奇怪的人……":
        "If I say I'm interviewing a cat, he'll think I'm weird...",
    "我可以先问您一些关于大福的问题吗？": "Could I ask you a few questions about Dafu first?",
    "行啊，你问吧。": "Sure. Go ahead.",
    "解锁交谈环节。": "Talk menu unlocked.",

    "傍晚，小凌坐在长椅上，一边刷手机，一边等大福出现。":
        "Evening. Ling sits on a bench, scrolling her phone, waiting for Dafu.",
    "小姑娘，大福来了。": "Miss—Dafu's here.",
    "小凌抬起头。一只橘猫从停放的电动车之间钻出来，慢悠悠地朝保安亭走去。":
        "Ling looks up. An orange cat slips out between parked e-bikes and ambles toward the booth.",
    "大福走到保安亭前，冲着里面叫了一声。": "Dafu stops in front of the booth and meows inward.",
    "喵——": "Meow—",
    "保安叔叔从岗亭里出来，给大福添了一点猫粮。": "Uncle Guard comes out and tops up Dafu's kibble.",
    "来上班打卡了。": "Clocking in for work.",
    "小凌起身靠近。大福很快注意到她，停下吃东西的动作，警惕地往后退了一步。":
        "Ling stands and approaches. Dafu notices at once, freezes mid-bite, and edges back warily.",
    "它有点怕生，你别一下靠太近。": "He's shy with strangers. Don't get too close too fast.",
    "好。": "Okay.",
    "小凌停在原地，从包里拿出喵语翻译器并启动。":
        "Ling stays put, takes out the meow translator, and powers it on.",
    "目标识别完成": "Target identified",
    "目标：大福": "Target: Dafu",
    "当前状态：警惕": "Status: wary",
    "双向交流模式开启": "Two-way mode enabled",
    "可将人类语言转译为猫能够理解的表达，同时解析猫的叫声与行为。":
        "Translates human speech into forms a cat can grasp, and parses meows and behavior.",
    "那先试试。": "Let's try.",
    "小凌按住翻译键，尝试呼唤大福。": "Ling holds the translate key and calls to Dafu.",
    "大福？": "Dafu?",
    "设备发出经过转译的猫叫声。大福耳朵动了一下，抬头看向小凌。":
        "The device emits a translated meow. Dafu's ear twitches; he looks up at Ling.",
    "喵？": "Meow?",
    "译文——“你在叫我？”": 'Translation — "Are you calling me?"',
    "……真听懂了？": "...Did he actually understand?",
    "大福仍然没有靠近。小凌想了想，从包里拿出一根猫条。":
        "Dafu still won't come closer. Ling thinks, then pulls a cat treat stick from her bag.",
    "还好我有准备。": "Good thing I packed this.",
    "大福的视线立刻落在猫条上。": "Dafu's eyes lock onto the treat.",
    "小凌蹲下，与大福保持距离，按下翻译键。":
        "Ling crouches, keeps her distance, and presses translate.",
    "给你吃的。": "This is for you.",
    "人类 → 猫语": "Human → Cat",
    "“食物，给你。”": '"Food. For you."',
    "大福闻了闻空气，试探着向前走了两步。": "Dafu sniffs the air and takes two cautious steps forward.",
    "我不过去，你自己来。": "I won't come closer. You come.",
    "“没有危险，可以靠近。”": '"Safe. You can come closer."',
    "大福犹豫片刻，终于凑到猫条前舔了一口。很快便放下戒心，开始埋头吃起来。":
        "After a pause, Dafu leans in and licks the treat—then drops his guard and digs in.",
    "状态更新——警惕降低": "Status update — wariness down",
    "大福信任提升": "Dafu trust increased",
    "吃到一半，大福已经主动站到小凌身边。小凌伸出手，大福凑过来闻了闻，没有躲开。于是小凌轻轻摸了一下大福的脑袋。":
        "Halfway through, Dafu is already at Ling's side. She offers a hand; he sniffs and doesn't pull away. She gives his head a gentle scratch.",
    "好像可以了。": "Looks like we're good.",
    "猫条吃完。大福舔舔嘴，在小凌旁边坐下，没有离开。":
        "The treat is gone. Dafu licks his lips, sits beside Ling, and stays.",
    "喵喵。": "Meow-meow.",
    "猫语 → 人类": "Cat → Human",
    "“还有吗？”": '"Any more?"',
    "没了，就带了一根。": "That's all. I only brought one.",
    "大福盯着她手里空掉的包装看了一会儿。": "Dafu stares at the empty wrapper in her hand.",
    "喵呜。": "Mrrrow.",
    "译文——“没有了啊。”": 'Translation — "So there\'s none left."',
    "下次多带点。": "Next time I'll bring more.",
    "大福虽有些不满，但没有离开，反而在原地趴下。":
        "Annoyed, but he doesn't leave—he settles down right there.",
    "小凌看了看大福，又看向手里的喵语翻译器。":
        "Ling looks at Dafu, then at the translator in her hand.",
    "居然能听懂我说的。": "He actually understood me.",
    "我也能听懂它说的。好神奇。": "And I understood him. Wild.",
    "进入“采访模式”": 'Enter "Interview Mode"',
    "采访过程中，可直接输入想向大福询问的问题。":
        "During the interview, type questions you want to ask Dafu.",
    "当问题超出大福的认知范围时，需要尝试更换提问方式。":
        "If a question is beyond what Dafu grasps, try asking another way.",
    "大福。": "Dafu.",
    "大福抬起头看向小凌。": "Dafu looks up at Ling.",
    "我能问你一些事情吗？": "Can I ask you a few things?",
    "喵。": "Meow.",
    "译文——“可以。”": 'Translation — "Okay."',
    "采访对象“大福”已建立基础信任": 'Interview subject "Dafu": basic trust established',
    "解锁第一次自由采访": "First free interview unlocked",

    "采访结束后，大福跳上快递柜趴下。小凌收起喵语翻译器，重新走向保安亭。":
        "After the interview, Dafu hops onto the lockers and sprawls. Ling packs the translator and heads back to the booth.",
    "你刚刚在和猫说话？": "Were you just talking to the cat?",
    "呃……算是吧？我能听懂猫语。": "Uh... sort of? I can understand cat.",
    "现在的年轻人真厉害！": "Kids these days are amazing!",
    "叔叔，我回来啦。刚才您说的那个救助人，我想再确认一下。":
        "Uncle, I'm back. About that rescuer you mentioned—I'd like to double-check.",
    "嗯，你问吧。": "Mm. Ask away.",
    "根据采访线索向保安打听当年的救助者。":
        "Use interview leads to ask the guard about the rescuer from back then.",

    "第二天下午，小凌提前十分钟到了约好的咖啡馆。":
        "The next afternoon, Ling arrives at the cafe ten minutes early.",
    "她选了一个靠里的位置，把记者笔记和手机放在桌边。":
        "She picks a seat farther in and sets her notebook and phone on the table.",
    "三点刚过，林女士推门进来。小凌起身朝她招了招手。":
        "Just after three, Ms. Lin pushes the door open. Ling stands and waves.",
    "林女士？您好，我是小凌。": "Ms. Lin? Hello—I'm Ling.",
    "你好。没等很久吧？": "Hi. Hope you weren't waiting long?",
    "没有，我也刚到。谢谢您愿意抽时间过来。":
        "Not at all—I just got here. Thanks for making time.",
    "两人坐下。等服务员放下饮品离开后，小凌打开记者笔记。":
        "They sit. After the drinks arrive and the server leaves, Ling opens her notebook.",
    "我昨天先问了保安一些情况，也在社区见到了大福。":
        "Yesterday I talked with the guard, and I met Dafu in the community.",
    "今天主要想把它当时受伤、送医治疗，还有后来送回社区的经过核实清楚。":
        "Today I mainly want to verify how he was hurt, treated, and later returned.",
    "嗯，可以。你问吧。": "Sure. Go ahead.",
    "我这边方便录音吗？只用来整理采访内容。":
        "Is it okay if I record? Just for organizing the interview.",
    "可以。": "That's fine.",
    "小凌打开手机录音，将它放到桌边。": "Ling starts a phone recording and sets it on the table.",
    "如果有不方便回答的，您可以直接告诉我，我们随时换一个问题。":
        "If anything's uncomfortable, just say so—we can switch questions anytime.",
    "好。": "Alright.",
    "小凌把记者笔记翻到“大福”那一页。": 'Ling flips to the "Dafu" page in her notebook.',
    "第二次自由采访开始。可根据记者笔记，自由向林女士提问。":
        "Second free interview. Use your notebook and ask Ms. Lin freely.",

    "周一上午，小凌回到编辑部，把社区调查、保安的证词、大福的采访和林女士的录音一起摊在电脑前。":
        "Monday morning. Back at the newsroom, Ling spreads out the community notes, the guard's account, Dafu's interview, and Ms. Lin's recording.",
    "好了。该把这些东西变成一篇能发的稿子了。":
        "Alright. Time to turn this into a piece we can publish.",
    "进入【素材整理与写稿】流程。": "Enter [Materials & Drafting].",
    "玩家前期获得的情报将转化为固定素材卡；未在调查或采访中获得的内容不会出现在写稿列表中。":
        "Intel you've earned becomes fixed material cards; anything you never found won't appear in the drafting list.",
}

TITLE_ONLY = {
    "SC-01": "周五下班前",
    "SC-02": "喵语翻译器",
    "SC-03": "保安猫大福",
    "SC-04": "槐安社区",
    "SC-05": "保安亭",
    "SC-06": "上班的大福",
    "SC-08": "寻找林女士",
    "SC-09": "咖啡馆采访",
    "SC-10": "写稿与沈禾审核",
}


def unescape_cs(s: str) -> str:
    return s.replace('\\"', '"').replace("\\n", "\n")


def parse_string_args(chunk: str):
    """Pull double-quoted C# string literals in order."""
    return [unescape_cs(m.group(1)) for m in re.finditer(r'"((?:\\.|[^"\\])*)"', chunk)]


def extract_parts(body: str):
    parts = []
    i = 0
    while True:
        m = re.search(r"s\.lines\.Add\(", body[i:])
        if not m:
            break
        start = i + m.end()
        depth = 1
        j = start
        while j < len(body) and depth:
            c = body[j]
            if c == "(":
                depth += 1
            elif c == ")":
                depth -= 1
            j += 1
        parts.append(body[start : j - 1].strip())
        i = j
    return parts


def classify(chunk: str):
    """Return dict with text, speakerName, choices if any."""
    if chunk.startswith("Bgm(") or chunk.startswith("Sfx(") or chunk.startswith("Bg("):
        return {"text": "", "speakerName": "", "choices": None, "skip": True}
    if chunk.startswith("Prop(") or chunk.startswith("PropHide("):
        return {"text": "", "speakerName": "", "choices": None, "skip": True}

    if chunk.startswith("N("):
        args = parse_string_args(chunk)
        return {"text": args[0] if args else "", "speakerName": "", "choices": None}
    if chunk.startswith("Sys("):
        args = parse_string_args(chunk)
        return {"text": args[0] if args else "", "speakerName": "系统", "choices": None}
    if chunk.startswith("Inner("):
        args = parse_string_args(chunk)
        return {"text": args[0] if args else "", "speakerName": "小凌", "choices": None}
    if chunk.startswith("L("):
        args = parse_string_args(chunk)
        return {
            "text": args[1] if len(args) > 1 else (args[0] if args else ""),
            "speakerName": args[0] if args else "",
            "choices": None,
        }
    if chunk.startswith("new ScriptLine"):
        text_m = re.search(r'text\s*=\s*"((?:\\.|[^"\\])*)"', chunk)
        name_m = re.search(r'speakerName\s*=\s*"((?:\\.|[^"\\])*)"', chunk)
        text = unescape_cs(text_m.group(1)) if text_m else ""
        name = unescape_cs(name_m.group(1)) if name_m else ""
        choices = []
        for cm in re.finditer(r'label\s*=\s*"((?:\\.|[^"\\])*)"', chunk):
            choices.append(unescape_cs(cm.group(1)))
        return {"text": text, "speakerName": name, "choices": choices or None}
    # unknown
    args = parse_string_args(chunk)
    return {"text": args[-1] if args else "", "speakerName": "", "choices": None}


def tr(zh: str) -> str:
    if not zh:
        return ""
    if zh in TR:
        return TR[zh]
    print("MISSING:", zh)
    return zh  # leave Chinese so we notice


def main():
    text = SRC.read_text(encoding="utf-8")
    lines_out = []
    missing = 0

    for m in re.finditer(r"static ScriptScene (Sc\d+)\(\)\s*\{(.*?)\n            return s;", text, re.S):
        sc = m.group(1)
        body = m.group(2)
        sid = ID_MAP[sc]
        title_zh = TITLE_ONLY[sid]
        lines_out.append({
            "key": f"title:{sid}",
            "text": tr(title_zh),
            "speakerName": "",
            "choices": [],
        })
        parts = extract_parts(body)
        for idx, chunk in enumerate(parts):
            info = classify(chunk)
            if info.get("skip") and not info["text"]:
                # still emit empty entry for index alignment? ScriptLoc only needs entries with text/choices
                # Empty cue lines: omit is fine (Resolve falls back to speaker map only)
                continue
            if not info["text"] and not info.get("choices"):
                continue
            en_text = tr(info["text"]) if info["text"] else ""
            if info["text"] and en_text == info["text"] and any("\u4e00" <= c <= "\u9fff" for c in info["text"]):
                missing += 1
            entry = {
                "key": f"{sid}:{idx}",
                "text": en_text,
                "speakerName": SPEAKER.get(info["speakerName"], info["speakerName"] or ""),
                "choices": [],
            }
            if info.get("choices"):
                entry["choices"] = [tr(c) for c in info["choices"]]
            lines_out.append(entry)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps({"lines": lines_out}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {len(lines_out)} entries to {OUT}")
    print(f"Missing translations: {missing}")


if __name__ == "__main__":
    main()
