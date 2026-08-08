# 剧本目录（Docs/Scripts）

本文件夹存放《街角专访》第一章剧本源文件与导入结果。

## 文件

| 文件 | 说明 |
|------|------|
| `chapter1_dialogue_sc10.docx` | 最新对话整理稿（SC10 审核补全统一格式版）原件 |
| `chapter1_dialogue_plain.txt` | 从 docx 抽出的纯文本，供解析 |
| `hotspot_descriptions_generated.txt` | 调查点文案导出（对照用） |

## 如何重新导入

1. 替换 / 更新本目录中的 docx  
2. 重新导出 plain（或运行解析脚本内嵌的抽取）  
3. 运行：

```text
python Tools/import_chapter1_script.py
```

会覆盖 `Assets/Scripts/Narrative/BuiltInScripts.cs` 中的固定剧情（SC-01～06、SC-08、SC-10 写稿开场）。

调查点与保安交谈文案在 `InvestigationService.cs` 中维护，与剧本调查段对齐；导入后可对照 `hotspot_descriptions_generated.txt` 手工同步。

## 约定

- **旁白** / **小凌（旁白）** → `LineSpeaker.Narration`，不显示角色名牌  
- **小凌（内心独白）** → `LineSpeaker.Inner`（无「内心」名牌，仍可显示立绘；标注「无立绘」则隐藏）  
- **系统 / UI / 画面文本** → `LineSpeaker.System`  
- **立绘标注** `角色 · 立绘：惊讶` → 写入 `ScriptLine.portrait`；`工作软件消息（无立绘）` → `portrait = 无立绘`  
- **SC-07 / SC-09** 自由采访仍由规则引擎驱动，剧本示例回答不写入固定树  
- **SC-10** 开场对白进桌面写稿 UI；沈禾审核分支由 `ArticleAssembler` 按关键表述与选材判定  
- **SC-11** 后日谈由 `GameUI.ShowEpilogue` 按写作方向展示  
