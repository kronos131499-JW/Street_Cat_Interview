# 系统实现对照（与代码同步）

## 运行入口

- 打开任意场景（如 `Assets/Scenes/SampleScene`）后进入 Play。
- `GameBootstrap` 会自动创建：`SceneDirector`、`InvestigationService`、`ReporterNotebook`、`InterviewController`、`GameUI`、`ChapterFlowController`。

## 模块

| 模块 | 脚本 | 说明 |
|------|------|------|
| 流程 | `ChapterFlowController` | SC 场景跳转、采访/写稿/后日谈切换 |
| 存档 | `SaveSystem` | `persistentDataPath/streetcat_ch1_save.json` |
| 剧本 | `SceneDirector` + `BuiltInScripts` | SC-01～06、SC-08 |
| 调查 | `InvestigationService` | 热点调查 + 保安交谈 |
| 笔记 | `ReporterNotebook` | 主题状态与待确认问题 |
| 采访 | `DafuRuleEngine` / `LinRuleEngine` | 规则层意图与知识边界；`LlmClient` 可选润色 |
| 采访提示 | `IInterviewHintProvider` / `RuleBasedInterviewHintProvider` | 自由采访教练提示 + 提问芯片；默认纯规则，可替换 Provider 做 LLM 辅助 |
| 写稿 | `MaterialCatalog` + `ArticleAssembler` | M01–M16 固定卡 + 两立意模板 + 规则审核 |
| 写稿辅助 | `IWritingAiAssist` / `RuleBasedWritingAiAssist` | 编辑器调试用（`街角专访/写稿 AI 辅助`）；素材卡界面不显示 AI 入口 |

## LLM（可选）

默认不启用。若要启用 OpenAI 兼容接口：

1. 环境变量或 PlayerPrefs 键：`STREETCAT_LLM_API_KEY`
2. `LlmClient` 默认 endpoint 为 DeepSeek；可在 Inspector 修改
3. 菜单：`StreetCat/LLM/Paste API Key From Clipboard`；采访润色与编辑器写稿调试共用同一 Key
4. Editor：`街角专访/写稿 AI 辅助` 可在 Play 时 dump 规则建议（玩家素材板无此按钮）

规则引擎始终是情报解锁与禁词的权威来源。写稿辅助**不会**凭空生成新素材卡或情报，只基于已解锁卡排序与模板拼接。

自由采访底部芯片与教练提示由 `InterviewHintService` 生成（默认 `RuleBasedInterviewHintProvider`：根据信任/压力/专注、上一句回复、已问意图、笔记未完成主题排序）。若要接 AI 辅助提问，实现 `IInterviewHintProvider` 并赋值 `InterviewHintService.Provider`；Play Mode 默认不依赖联网 LLM。

写稿 `WritingAiAssistService` 仍保留供编辑器/日后接入；玩家素材卡库界面已去掉「AI 建议」。

## 第一章竖切路径

新游戏 → 杂志社剧情 → 槐安社区调查（投喂点+快递柜）→ 保安交谈（出没时间）→ 等待大福 → 采访大福 → 问保安找林姐 → 采访林女士 → 写稿发布 → 后日谈。
