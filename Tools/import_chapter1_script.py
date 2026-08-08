#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Parse Docs/Scripts/chapter1_dialogue_plain.txt → BuiltInScripts.cs (+ investigate dump)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLAIN = ROOT / "Docs" / "Scripts" / "chapter1_dialogue_plain.txt"
OUT_CS = ROOT / "Assets" / "Scripts" / "Narrative" / "BuiltInScripts.cs"


def esc(s: str) -> str:
    return (
        s.replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("\r", "")
        .replace("\n", "\\n")
    )


SKIP_PREFIXES = (
    "【BGM",
    "【SE",
    "【停顿",
    "【CG",
    "【画面",
    "【角色",
    "【返回",
    "【交谈】",
    "【演出",
    "【UI",
    "结构说明",
    "使用原则",
    "标注方式",
    "立绘说明",
    "整理",
    "原始文件",
    "含旁白",
    "说明：",
    "说明｜",
    "写作方向",
    "素材选择",
    "关键表述选择",
    "生成与提交",
    "退回后的流程",
    "审核通过后的流程",
    "触发条件",
    "审核分支",
    "对话情境",
    "动作与译文",
    "动作不全部",
    "颈部旧伤",
    "抽象医疗",
    "为什么亲近",
    "一次输入",
    "问题过于",
    "错误前提",
    "玩家直接",
    "追问精确",
    "询问医疗",
    "越界或游戏",
    "敌意表达",
    "重复提问",
    "模糊代词",
    "输入过长",
    "压力恢复",
    "提前结束",
    "正常结束",
    "适用于询问",
    "方向A",
    "方向B",
    "通用结尾",
    "8. 本稿",
    "SC-07 大福",
    "SC-09 林女士",
    "SC-10 写稿",
)


def is_skip(line: str) -> bool:
    if not line.strip():
        return True
    if line.startswith(SKIP_PREFIXES):
        return True
    if re.match(r"^\d+\.\s", line):
        return True
    if "立绘需求" in line or line in ("角色", "基础形式", "建议状态", "使用场景"):
        return True
    if line.startswith("半身立绘") or line.startswith("猫咪角色"):
        return True
    if line.startswith("玩家输入"):
        return True
    if line.startswith("固定标题"):
        return True
    return False


# Speaker + optional annotation after ·
SPEAKER_RE = re.compile(
    r"^(旁白|小凌（内心独白）|小凌（旁白）|小凌|沈禾|保安叔叔|大福|林女士|系统|UI|画面文本|正文)"
    r"(?:\s*·\s*(.+))?$"
)
PIPE_RE = re.compile(r"^(旁白|系统|UI|画面文本)｜(.*)$")
BG_RE = re.compile(r"^【背景[:：](.+?)】")


def clean_portrait(annotation: str | None) -> str:
    if not annotation:
        return ""
    a = annotation.strip()
    if "无立绘" in a or "仅文本" in a or "工作软件消息" in a:
        return "无立绘"
    m = re.match(r"立绘[:：]\s*(.+)$", a)
    if m:
        return re.sub(r"[（(].*$", "", m.group(1)).strip()
    m = re.match(r"角色图[:：]\s*(.+)$", a)
    if m:
        return re.sub(r"[（(].*$", "", m.group(1)).strip()
    return ""


def split_scenes(lines: list[str]) -> dict[str, list[str]]:
    scenes: dict[str, list[str]] = {}
    cur = None
    buf: list[str] = []
    header = re.compile(r"^(?:\d+\.\s*)?SC-(\d{2})\s*｜")
    scene_tag = re.compile(r"^【场景：SC-(\d{2})")

    def flush():
        nonlocal buf, cur
        if cur and buf:
            scenes[cur] = buf
        buf = []

    for line in lines:
        m = header.match(line.strip())
        if m:
            flush()
            cur = f"SC-{m.group(1)}"
            continue
        m2 = scene_tag.match(line.strip())
        if m2:
            sid = f"SC-{m2.group(1)}"
            if cur != sid:
                flush()
                cur = sid
            continue
        if cur:
            buf.append(line)
    flush()
    return scenes


def find_background(raw_lines: list[str], default: str) -> str:
    for line in raw_lines:
        m = BG_RE.match(line.strip())
        if m:
            return m.group(1).strip()
    return default


def parse_dialogue_block(raw_lines: list[str], stop_at_investigate: bool = False):
    """Yield (kind, name, text, portrait)."""
    pending_speaker = None
    pending_kind = None
    pending_portrait = ""
    i = 0
    lines = raw_lines
    while i < len(lines):
        line = lines[i].strip()
        i += 1
        if is_skip(line):
            continue
        if stop_at_investigate and (
            line.startswith("调查点") or line.startswith("【调查")
        ):
            break
        if line.startswith("【跳转"):
            yield ("jump", "", line, "")
            continue
        bm = BG_RE.match(line)
        if bm:
            yield ("background", "", bm.group(1).strip(), "")
            continue
        if line.startswith("UI｜选项") or line.startswith("UI｜选择"):
            label = "前往沈禾办公室"
            m = re.search(r"[AB]\.\s*(.+)", line)
            if m:
                label = m.group(1).strip()
            yield ("choice", "", label, "")
            continue

        pm = PIPE_RE.match(line)
        if pm:
            tag, text = pm.group(1), pm.group(2).strip()
            if not text:
                continue
            if tag == "旁白":
                yield ("narration", "", text, "")
            elif tag in ("系统", "UI", "画面文本"):
                yield ("system", "系统", text, "")
            continue

        sm = SPEAKER_RE.match(line)
        if sm:
            name = sm.group(1)
            pending_portrait = clean_portrait(sm.group(2) if sm.lastindex >= 2 else None)
            if name in ("旁白", "小凌（旁白）"):
                pending_speaker, pending_kind = "", "narration"
                pending_portrait = ""
            elif name == "小凌（内心独白）":
                pending_speaker, pending_kind = "小凌", "inner"
            elif name == "系统":
                pending_speaker, pending_kind = "系统", "system"
                pending_portrait = ""
            elif name in ("UI", "画面文本", "正文"):
                pending_speaker, pending_kind = "系统", "system"
                pending_portrait = ""
            else:
                pending_speaker, pending_kind = name, "character"
            continue

        if pending_kind is None:
            if line.startswith("【") or re.match(r"^[①②③④⑤]", line):
                continue
            if "玩家输入" in line or line.startswith("回答") or "示例" in line:
                continue
            continue

        kind = pending_kind
        name = pending_speaker
        portrait = pending_portrait
        pending_portrait = ""
        yield (kind, name, line, portrait)


def emit_line_cs(kind: str, name: str, text: str, portrait: str = "") -> str:
    t = esc(text)
    p = esc(portrait) if portrait else ""
    if kind == "background":
        return f'            s.lines.Add(Bg("{esc(text)}"));'
    if kind == "narration":
        return f'            s.lines.Add(N("{t}"));'
    if kind == "inner":
        if p:
            return f'            s.lines.Add(Inner("{t}", "{p}"));'
        return f'            s.lines.Add(Inner("{t}"));'
    if kind == "system":
        return f'            s.lines.Add(Sys("{t}"));'
    if p:
        return f'            s.lines.Add(L("{esc(name)}", "{t}", "{p}"));'
    return f'            s.lines.Add(L("{esc(name)}", "{t}"));'


def build_sc01(lines: list[str]) -> str:
    bg = find_background(lines, "编辑部_傍晚")
    out = [
        "        static ScriptScene Sc01()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC01, title = "周五下班前", backgroundLabel = "{esc(bg)}" }};',
    ]
    for kind, name, text, portrait in parse_dialogue_block(lines):
        if kind == "jump":
            continue
        if kind == "background":
            # Scene already has initial backgroundLabel; skip duplicate first tag
            if text == bg:
                continue
            out.append(emit_line_cs(kind, name, text, portrait))
            continue
        if kind == "choice":
            out.append("            s.lines.Add(new ScriptLine")
            out.append("            {")
            out.append("                speaker = LineSpeaker.System,")
            out.append('                speakerName = "选项",')
            out.append(f'                text = "{esc(text)}",')
            out.append("                choices = new List<ScriptChoice>")
            out.append("                {")
            out.append(
                f'                    new ScriptChoice {{ label = "{esc(text)}", nextSceneId = SceneIds.SC02 }}'
            )
            out.append("                }")
            out.append("            });")
            break
        out.append(emit_line_cs(kind, name, text, portrait))
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


def build_sc02(lines: list[str]) -> str:
    bg = find_background(lines, "沈禾办公室_傍晚")
    out = [
        "        static ScriptScene Sc02()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC02, title = "喵语翻译器", backgroundLabel = "{esc(bg)}" }};',
    ]
    buffered_sys = []
    for kind, name, text, portrait in parse_dialogue_block(lines):
        if kind == "jump":
            break
        if kind == "background":
            if text != bg:
                out.append(emit_line_cs(kind, name, text, portrait))
            continue
        if kind == "system" and ("获得道具" in text or "任务更新" in text):
            buffered_sys.append(text)
            continue
        out.append(emit_line_cs(kind, name, text, portrait))
    joined = "\\n".join(esc(x) for x in buffered_sys) if buffered_sys else esc("获得道具「喵语翻译器」。")
    out.append("            s.lines.Add(new ScriptLine")
    out.append("            {")
    out.append("                speaker = LineSpeaker.System,")
    out.append('                speakerName = "系统",')
    out.append(f'                text = "{joined}",')
    out.append("                setFlag = FlagIds.HasTranslator,")
    out.append('                setObjective = "寻找合适的流浪猫采访对象。",')
    out.append("                nextSceneId = SceneIds.SC03")
    out.append("            });")
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


def build_sc03(lines: list[str]) -> str:
    bg = find_background(lines, "编辑部_工位_傍晚")
    out = [
        "        static ScriptScene Sc03()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC03, title = "保安猫大福", backgroundLabel = "{esc(bg)}" }};',
    ]
    pending_before_flags = []
    for kind, name, text, portrait in parse_dialogue_block(lines):
        if kind == "jump":
            break
        if kind == "background":
            if text != bg:
                out.append(emit_line_cs(kind, name, text, portrait))
            continue
        if kind == "system" and (
            "发现采访对象" in text
            or "获得初始情报" in text
            or "记者笔记" in text
            or "解锁地点" in text
            or "任务更新" in text
            or text.startswith("大福通常")
            or text.startswith("曾经受过")
            or text.startswith("康复后")
            or text.startswith("目前由")
        ):
            pending_before_flags.append(text)
            continue
        if kind == "character" and name == "小凌" and ("去看看" in text or "槐安社区" in text):
            pending_before_flags.append(("line", kind, name, text, portrait))
            continue
        if isinstance(text, str) and pending_before_flags and kind == "narration" and "收藏" in text:
            out.append(emit_line_cs(kind, name, text, portrait))
            continue
        out.append(emit_line_cs(kind, name, text, portrait))

    sys_parts = [x for x in pending_before_flags if isinstance(x, str)]
    char_lines = [x for x in pending_before_flags if isinstance(x, tuple)]
    sys_text = "\\n".join(esc(x) for x in sys_parts[:6]) if sys_parts else "发现采访对象「大福」。"
    out.append("            s.lines.Add(new ScriptLine")
    out.append("            {")
    out.append("                speaker = LineSpeaker.System,")
    out.append('                speakerName = "系统",')
    out.append(f'                text = "{sys_text}",')
    out.append("                setFlag = FlagIds.FoundDafu,")
    out.append("                grantIntel = IntelIds.DafuWasRescued,")
    out.append('                noteLine = "大福曾受过严重的伤，康复后被放归原社区。",')
    out.append('                setObjective = "前往槐安社区寻找大福。"')
    out.append("            });")
    for item in char_lines:
        _, k, n, t, p = item
        if "去看看" in t or "槐安" in t:
            out.append("            s.lines.Add(new ScriptLine")
            out.append("            {")
            out.append("                speaker = LineSpeaker.Character,")
            out.append(f'                speakerName = "{esc(n)}",')
            out.append(f'                text = "{esc(t)}",')
            if p:
                out.append(f'                portrait = "{esc(p)}",')
            out.append("                setFlag = FlagIds.UnlockedHuaiAn,")
            out.append("                nextSceneId = SceneIds.SC04")
            out.append("            });")
        else:
            out.append(emit_line_cs(k, n, t, p))
    if not any(isinstance(x, tuple) and ("去看看" in x[3] or "槐安" in x[3]) for x in pending_before_flags):
        out.append("            s.lines.Add(new ScriptLine")
        out.append("            {")
        out.append("                speaker = LineSpeaker.Character,")
        out.append('                speakerName = "小凌",')
        out.append('                text = "行。去看看再说！",')
        out.append('                portrait = "认真",')
        out.append("                setFlag = FlagIds.UnlockedHuaiAn,")
        out.append("                nextSceneId = SceneIds.SC04")
        out.append("            });")
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


def build_sc04(lines: list[str]) -> str:
    bg = find_background(lines, "槐安社区_午后")
    out = [
        "        static ScriptScene Sc04()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC04, title = "槐安社区", backgroundLabel = "{esc(bg)}" }};',
    ]
    for kind, name, text, portrait in parse_dialogue_block(lines, stop_at_investigate=True):
        if kind == "jump":
            break
        if kind == "background":
            if text != bg:
                out.append(emit_line_cs(kind, name, text, portrait))
            continue
        if kind == "system" and ("调查" in text or "目标更新" in text or "首次进入" in text):
            continue
        out.append(emit_line_cs(kind, name, text, portrait))
    out.append("            s.lines.Add(new ScriptLine")
    out.append("            {")
    out.append("                speaker = LineSpeaker.System,")
    out.append('                speakerName = "系统",')
    out.append(
        '                text = "首次进入调查场景。场景中的物件可以【调查】，也可与人物交谈以获取情报。",'
    )
    out.append("                setFlag = FlagIds.InvestigateTutorialShown,")
    out.append('                setObjective = "在社区内寻找大福的线索。",')
    out.append("                openInvestigation = true")
    out.append("            });")
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


def build_sc05_intro(lines: list[str]) -> str:
    bg = find_background(lines, "保安亭_午后")
    out = [
        "        static ScriptScene Sc05()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC05, title = "保安亭", backgroundLabel = "{esc(bg)}" }};',
    ]
    for kind, name, text, portrait in parse_dialogue_block(lines):
        if kind == "jump":
            break
        if kind == "background":
            if text != bg:
                out.append(emit_line_cs(kind, name, text, portrait))
            continue
        if text.startswith("①") or "交谈环节" in text or text.startswith("大福一般几点"):
            break
        if kind == "system" and "解锁交谈" in text:
            break
        if re.match(r"^[①②③④⑤]", text):
            break
        out.append(emit_line_cs(kind, name, text, portrait))
    out.append("            s.lines.Add(new ScriptLine")
    out.append("            {")
    out.append("                speaker = LineSpeaker.System,")
    out.append('                speakerName = "系统",')
    out.append('                text = "解锁交谈环节。",')
    out.append("                openTalkMenu = true")
    out.append("            });")
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


def build_sc06(lines: list[str]) -> str:
    bg = find_background(lines, "保安亭_傍晚")
    out = [
        "        static ScriptScene Sc06()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC06, title = "上班的大福", backgroundLabel = "{esc(bg)}" }};',
    ]
    for kind, name, text, portrait in parse_dialogue_block(lines):
        if kind == "jump":
            break
        if kind == "background":
            if text != bg:
                out.append(emit_line_cs(kind, name, text, portrait))
            continue
        if "想问大福什么" in text or "动态对白" in text or "第一次自由采访" in text:
            if kind == "system" and "解锁第一次" in text:
                continue
            if "自由采访" in text and kind != "system":
                break
            if kind == "system" and ("解锁第一次" in text or "已建立基础信任" in text):
                continue
            if "想问大福什么" in text:
                break
        if kind == "system" and ("解锁第一次" in text or "已建立基础信任" in text):
            continue
        out.append(emit_line_cs(kind, name, text, portrait))
    out.append("            s.lines.Add(new ScriptLine")
    out.append("            {")
    out.append("                speaker = LineSpeaker.System,")
    out.append('                speakerName = "系统",')
    out.append('                text = "解锁第一次自由采访。",')
    out.append("                nextSceneId = SceneIds.SC07")
    out.append("            });")
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


def build_sc08(lines: list[str]) -> str:
    bg = find_background(lines, "保安亭_傍晚")
    out = [
        "        static ScriptScene Sc08()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC08, title = "寻找林女士", backgroundLabel = "{esc(bg)}" }};',
    ]
    for kind, name, text, portrait in parse_dialogue_block(lines):
        if kind == "jump":
            break
        if kind == "background":
            if text != bg:
                out.append(emit_line_cs(kind, name, text, portrait))
            continue
        if text.startswith("①") or "根据“大福采访”" in text or "当初是谁救助" in text:
            break
        if kind == "system" and "根据" in text and "选项" in text:
            break
        out.append(emit_line_cs(kind, name, text, portrait))
    out.append("            s.lines.Add(new ScriptLine")
    out.append("            {")
    out.append("                speaker = LineSpeaker.System,")
    out.append('                speakerName = "系统",')
    out.append('                text = "根据采访线索向保安打听当年的救助者。",')
    out.append("                openTalkMenu = true,")
    out.append('                setObjective = "向保安询问大福记忆中的女人。"')
    out.append("            });")
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


def build_sc09(lines: list[str]) -> str:
    """Cafe meeting intro; free interview opens via openInterview."""
    bg = find_background(lines, "咖啡馆_午后")
    out = [
        "        static ScriptScene Sc09()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC09, title = "咖啡馆采访", backgroundLabel = "{esc(bg)}" }};',
    ]
    for kind, name, text, portrait in parse_dialogue_block(lines):
        if kind == "jump":
            break
        if kind == "background":
            if text != bg:
                out.append(emit_line_cs(kind, name, text, portrait))
            continue
        if kind == "system" and (
            "第二次自由采访开始" in text
            or "自由向林女士提问" in text
            or "采访模式" in text
        ):
            break
        if "想问林女士什么" in text or "动态回答" in text or "宽泛询问" in text:
            break
        if kind == "system" and "可根据记者笔记" in text:
            break
        out.append(emit_line_cs(kind, name, text, portrait))
    out.append("            s.lines.Add(new ScriptLine")
    out.append("            {")
    out.append("                speaker = LineSpeaker.System,")
    out.append('                speakerName = "系统",')
    out.append(
        '                text = "第二次自由采访开始。可根据记者笔记，自由向林女士提问。",'
    )
    out.append("                setFlag = FlagIds.LinCafeIntroDone,")
    out.append('                setObjective = "采访林女士，核实大福的救助经过。",')
    out.append("                openInterview = true")
    out.append("            });")
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


def build_sc10(lines: list[str]) -> str:
    """Intro into writing desk; gameplay + Shenhe review handled by writing UI."""
    bg = find_background(lines, "编辑部工位_上午")
    out = [
        "        static ScriptScene Sc10()",
        "        {",
        f'            var s = new ScriptScene {{ id = SceneIds.SC10, title = "写稿与沈禾审核", backgroundLabel = "{esc(bg)}" }};',
    ]
    for kind, name, text, portrait in parse_dialogue_block(lines):
        if kind == "jump":
            break
        if kind == "background":
            if text != bg:
                out.append(emit_line_cs(kind, name, text, portrait))
            continue
        # Stop before UI writing-direction / material pick instructions
        if kind == "system" and (
            "写作方向" in text
            or "素材" in text and "选择" in text
            or "进入【素材整理" in text
            or "进入【素材整理与写稿】" in text
            or "玩家前期获得" in text
        ):
            break
        if "选择本次报道" in text or text.startswith("A｜") or text.startswith("B｜"):
            break
        out.append(emit_line_cs(kind, name, text, portrait))
    out.append("            s.lines.Add(new ScriptLine")
    out.append("            {")
    out.append("                speaker = LineSpeaker.System,")
    out.append('                speakerName = "系统",')
    out.append(
        '                text = "进入【素材整理与写稿】流程。玩家前期获得的情报将转化为固定素材卡。",'
    )
    out.append("                setFlag = FlagIds.WritingUnlocked,")
    out.append('                setObjective = "整理素材，完成报道。",')
    out.append("                openWriting = true")
    out.append("            });")
    out.append("            return s;")
    out.append("        }")
    return "\n".join(out)


HEADER = '''using System.Collections.Generic;
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
'''


def extract_hotspot_blobs(lines: list[str]) -> dict[str, str]:
    mapping_keys = {
        "猫屋": "cat_house",
        "猫粮碗": "food_bowl",
        "水碗": "water_bowl",
        "投喂点旁的小挂牌": "sign",
        "小挂牌": "sign",
        "灌木丛旁的流浪猫": "tabby",
        "自动贩卖机": "vending",
        "木质长椅": "bench",
        "快递柜": "locker",
    }
    result: dict[str, list[str]] = {v: [] for v in mapping_keys.values()}
    cur = None
    for line in lines:
        line = line.strip()
        m = re.match(r"【调查[:：](.+?)】", line)
        if m:
            title = m.group(1).strip()
            cur = None
            for k, vid in mapping_keys.items():
                if k in title or title in k:
                    cur = vid
                    break
            continue
        if line.startswith("调查点"):
            cur = None
            if "自动贩卖机" in line:
                cur = "vending"
            elif "木质长椅" in line:
                cur = "bench"
            elif "快递柜" in line:
                cur = "locker"
            continue
        if cur is None:
            continue
        if line.startswith("系统｜") and "获得情报" in line:
            continue
        if line.startswith("【") or line.startswith("调查"):
            if line.startswith("【调查"):
                pass
            else:
                if line.startswith("调查点") or line.startswith("【跳转"):
                    cur = None
                continue
        pm = PIPE_RE.match(line)
        if pm:
            result[cur].append(pm.group(2).strip())
            continue
        sm = SPEAKER_RE.match(line)
        if sm:
            continue
        if line and not line.startswith("系统"):
            result[cur].append(line)
    return {k: "\\n".join(esc(x) for x in v if x) for k, v in result.items() if v}


def main():
    text = PLAIN.read_text(encoding="utf-8")
    lines = text.splitlines()
    scenes = split_scenes(lines)
    print("scenes:", sorted(scenes.keys()), {k: len(v) for k, v in scenes.items()})

    parts = [HEADER]
    parts.append(build_sc01(scenes.get("SC-01", [])))
    parts.append("")
    parts.append(build_sc02(scenes.get("SC-02", [])))
    parts.append("")
    parts.append(build_sc03(scenes.get("SC-03", [])))
    parts.append("")
    parts.append(build_sc04(scenes.get("SC-04", [])))
    parts.append("")
    parts.append(build_sc05_intro(scenes.get("SC-05", [])))
    parts.append("")
    parts.append(build_sc06(scenes.get("SC-06", [])))
    parts.append("")
    parts.append(build_sc08(scenes.get("SC-08", [])))
    parts.append("")
    parts.append(build_sc09(scenes.get("SC-09", [])))
    parts.append("")
    parts.append(build_sc10(scenes.get("SC-10", [])))
    parts.append("    }")
    parts.append("}")
    OUT_CS.write_text("\n".join(parts) + "\n", encoding="utf-8")
    print("wrote", OUT_CS)

    blobs = extract_hotspot_blobs(scenes.get("SC-04", []))
    dump = ROOT / "Docs" / "Scripts" / "hotspot_descriptions_generated.txt"
    dump.write_text(
        "\n\n".join(f"[{k}]\n{v.replace(chr(92)+'n', chr(10))}" for k, v in blobs.items()),
        encoding="utf-8",
    )
    print("hotspots", list(blobs.keys()))


if __name__ == "__main__":
    main()
