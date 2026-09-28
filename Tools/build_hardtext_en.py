# -*- coding: utf-8 -*-
"""Extract Chinese string literals + emit hardtext_en.json skeleton / merge."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "Loc" / "hardtext_en.json"

FILES = [
    ROOT / "Assets/Scripts/Investigation/InvestigationService.cs",
    ROOT / "Assets/Scripts/Notebook/ReporterNotebook.cs",
]

# C# string literal: "...." with escapes
STR_RE = re.compile(r'"((?:\\.|[^"\\])*)"')
ZH_RE = re.compile(r"[\u4e00-\u9fff]")


def unescape(s: str) -> str:
    return (
        s.replace(r"\n", "\n")
        .replace(r"\t", "\t")
        .replace(r"\"", '"')
        .replace(r"\\", "\\")
    )


def collect() -> list[str]:
    found: dict[str, None] = {}
    for path in FILES:
        text = path.read_text(encoding="utf-8")
        for m in STR_RE.finditer(text):
            s = unescape(m.group(1))
            if ZH_RE.search(s) and s.strip():
                found[s] = None

    # GameUI investigate / talk chrome (not all GameUI — too noisy)
    extra = [
        "槐安社区　·　调查",
        "点击场景中的物件调查",
        "点击场景物件调查　·　已获情报　",
        "已获情报　",
        "　·　可继续调查",
        "午后 · 调查",
        "槐安社区",
        "与保安交谈",
        "等待大福",
        "继续采访大福",
        "继续采访林女士",
        "打听救助者",
        "回看",
        "笔记",
        "菜单",
        "保安亭",
        "想向保安叔叔了解什么？",
        "选择一个话题",
        "结束交谈",
        "交谈",
        "消息",
        "核实线索",
        "点击返回话题",
        "返回调查",
        "等待回复",
        "用大福提供的线索，向保安打听当年的救助者。",
        "（还缺少相关线索。先问清当初是谁救助的大福。）",
        "没什么特别的。",
        "（已经看过了。）",
        "（还缺少相关线索。）",
        "（现在还不能问这个。）",
    ]
    for s in extra:
        found[s] = None
    return sorted(found.keys(), key=lambda x: (len(x), x))


# Hand-authored EN for investigation / guard / notebook (authoritative for this pass).
EN: dict[str, str] = {
    # Hotspot titles
    "猫屋": "Cat house",
    "猫粮碗": "Food bowls",
    "水碗": "Water bowl",
    "投喂点小挂牌": "Feeding-spot sign",
    "灌木旁的狸花猫": "Tabby by the bushes",
    "自动贩卖机": "Vending machine",
    "木质长椅": "Wooden bench",
    "快递柜": "Parcel lockers",
    "保安亭": "Guard booth",
    # Descriptions / notes
    "塑料收纳箱改造的猫屋，比出租屋还精致。": "A plastic storage bin remade into a cat house—nicer than a studio apartment.",
    "几个猫碗并排放着，碗底很干净。": "Several cat bowls in a row; the bottoms are clean.",
    "水碗里装着大半碗清水，上面飘着几根猫毛。": "The water bowl is half full; a few cat hairs float on top.",
    "挂牌提醒不要倒剩饭，并补了一行：奶茶不算水。": "The sign says no leftovers—and adds: milk tea is not water.",
    "狸花猫晒太阳，靠近后钻进灌木丛。": "A tabby sunbathing; it slips into the bushes when you approach.",
    "咖啡只卖六块，公司楼下要十八。": "Coffee is only ¥6 here; downstairs at work it's ¥18.",
    "老式木质长椅，看上去至少服役十年了。": "An old wooden bench that looks like it's served at least ten years.",
    "柜顶有橘色猫毛，大福常趴在这里。": "Orange fur on the locker top—Dafu often lounges here.",
    "社区内设有长期维护的投喂点，附近居民可能了解流浪猫的情况。": "There's a long-maintained feeding spot; nearby residents may know about the strays.",
    "大福经常趴在社区入口的快递柜上，但当前并不在附近。": "Dafu often lies on the parcel lockers at the gate, but isn't nearby right now.",
    "大福通常在下午四五点出现。": "Dafu usually shows up around 4–5 p.m.",
    "大福经常在保安亭附近活动。": "Dafu often hangs around the guard booth.",
    "大福原本只是附近活动的流浪猫。接受救治并被放归后，它才逐渐开始在保安亭附近长期活动。": "Dafu used to just roam nearby. After treatment and release, it slowly settled around the guard booth.",
    "大福曾因颈部受伤被送医，但保安并不了解具体经过。": "Dafu was taken to a vet for a neck injury, but the guard doesn't know the details.",
    "大福没有固定主人，社区中有多人照顾。": "Dafu has no fixed owner; several people in the community look after it.",
    "救助者是小区住户「林姐」。": "The rescuer is a resident everyone calls Sister Lin.",
    "等待林女士回复。": "Wait for Ms. Lin's reply.",
    "向保安询问大福的情况。": "Ask the guard about Dafu.",
    "等待大福出现。": "Wait for Dafu to appear.",
    "明天下午15:00前往咖啡馆采访林女士。": "Interview Ms. Lin at the café tomorrow at 15:00.",
    # Guard topic labels (question options)
    "大福一般几点出现？": "When does Dafu usually show up?",
    "大福和保安的关系": "Dafu and the guard",
    "大福一直都住在这里吗？": "Has Dafu always lived here?",
    "大福的居所": "Where Dafu lives",
    "为什么叫大福？（可选）": "Why is it called Dafu? (optional)",
    "当初是谁救助的大福？": "Who rescued Dafu back then?",
    "林姐的信息": "Sister Lin's contact",
    # UI chrome
    "槐安社区　·　调查": "Huai'an Community · Investigation",
    "点击场景中的物件调查": "Tap objects in the scene to investigate",
    "午后 · 调查": "Afternoon · Investigate",
    "槐安社区": "Huai'an Community",
    "与保安交谈": "Talk to the guard",
    "等待大福": "Wait for Dafu",
    "继续采访大福": "Continue interviewing Dafu",
    "继续采访林女士": "Continue interviewing Ms. Lin",
    "打听救助者": "Ask about the rescuer",
    "回看": "Backlog",
    "笔记": "Notes",
    "菜单": "Menu",
    "想向保安叔叔了解什么？": "What do you want to ask Uncle Guard?",
    "选择一个话题": "Choose a topic",
    "结束交谈": "End conversation",
    "交谈": "Talk",
    "消息": "Messages",
    "核实线索": "Verify leads",
    "点击返回话题": "Click to return to topics",
    "返回调查": "Back to investigation",
    "等待回复": "Waiting for a reply",
    "用大福提供的线索，向保安打听当年的救助者。": "Use Dafu's clues to ask the guard about who rescued it.",
    "（还缺少相关线索。先问清当初是谁救助的大福。）": "(Not enough leads yet. First find out who rescued Dafu.)",
    "（还缺少相关线索。）": "(Not enough leads yet.)",
    "（现在还不能问这个。）": "(You can't ask that yet.)",
    "没什么特别的。": "Nothing special.",
    "（已经看过了。）": "(You've already looked at this.)",
    # Notebook topics
    "大福的社区生活": "Dafu's community life",
    "过去的大福": "Dafu in the past",
    "脖子上的伤": "The neck injury",
    "大福的救助者": "Dafu's rescuer",
    "被带走以后": "After being taken away",
    "大福的回归": "Dafu's return",
    "社交媒体": "Social media",
    "现场调查": "Field investigation",
    "保安叔叔": "Uncle Guard",
    "大福": "Dafu",
    "林女士": "Ms. Lin",
    "小凌": "Ling",
    "系统": "System",
}


def main() -> None:
    zh_list = collect()
    # Merge any existing file
    existing: dict[str, str] = {}
    if OUT.exists():
        try:
            data = json.loads(OUT.read_text(encoding="utf-8"))
            for e in data.get("entries") or []:
                if e.get("zh") and e.get("en"):
                    existing[e["zh"]] = e["en"]
        except Exception:
            pass

    entries = []
    missing = []
    for zh in zh_list:
        en = EN.get(zh) or existing.get(zh)
        if en:
            entries.append({"zh": zh, "en": en})
        else:
            missing.append(zh)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(
        json.dumps({"entries": entries}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    miss_path = ROOT / "Tools" / "_hardtext_missing.json"
    miss_path.write_text(json.dumps(missing, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"wrote {len(entries)} entries; {len(missing)} still missing -> {miss_path.name}")


if __name__ == "__main__":
    main()
