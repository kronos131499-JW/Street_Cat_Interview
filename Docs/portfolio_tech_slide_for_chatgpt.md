# 《街角专访》作品集 · 技术实现页（给 ChatGPT）

## 任务

请帮我设计作品集 PPT 中**一页**「技术实现」幻灯片。只做这一页，不要生成项目介绍、玩法介绍或其他页面。

要求：

- 16:9 横版，一页内放完，信息分区清楚，适合面试官 30 秒内看懂。
- 内容包括三部分：核心系统实现逻辑、代码展示、Unity 开发工具。
- 只使用本文中的事实，不要编造数据、性能指标、团队规模或上线成绩。
- 正文每条尽量不超过 20 个字；代码只展示关键几行，不要整段铺满。
- 中文为主，类名、方法名、文件名保留英文。
- 输出：页面标题、版式分区、每个区块的最终文案、选用的代码片段、配图建议，以及一份约 40 秒的口述讲解稿。

## 这一页要传达的核心观点

**规则系统决定事实，界面与可选大模型只负责表达。**

玩家可以自由提问和自由选材，但情报解锁、素材卡、审核结果都由离线规则决定，可复现、可追溯，不依赖联网。

建议页面标题（可润色）：

> 技术实现：可追溯的采访 → 素材 → 成稿管线

## 建议版式

| 区域 | 内容 | 占比 |
|---|---|---|
| 顶部 | 标题 + 一句核心观点 | 约 10% |
| 左侧 | 系统管线流程图 + 三个系统要点 | 约 40% |
| 中间 | 1～2 段关键代码 | 约 30% |
| 右侧 | Unity 开发工具列表 + 工具截图位 | 约 20% |

## 一、系统实现逻辑（左侧）

### 管线流程图

```text
玩家自由提问
  → 问题归一（中英）→ 意图分类
  → 角色知识边界 + 重复提问判定
  → 信任 / 压力 / 专注结算
  → 解锁情报 → 解锁素材卡 → 写入笔记
  → 选材 + 写作方向 → 拼稿 → 规则审核
```

### 系统 1：自由采访规则引擎

- 基类 `InterviewRuleEngine`，大福、林女士各自实现分类与回答。
- 先归一问题，再按意图和角色知识边界作答。
- 重复问同一意图：不再给情报，压力上升、专注下降。
- 大福的食物台词限额：最近 5 句最多提 1 次，超出自动替换。
- `LlmClient` 可选，只润色规则给出的台词，不能新增情报。

### 系统 2：素材卡写稿与规则审核

- 16 张固定素材卡（M01～M16），分事实 / 细节 / 情感。
- 每张卡对应两种写作方向的成稿表述。
- 四段结构，每段至少 1 张卡，总数最多 10 张。
- `ApplyRuleReview` 按选材分布判断是否偏题，70 分以上通过。

### 系统 3：记者笔记与提问提示

- 6 个采访主题，情报到达后更新完成状态。
- 提示器按三项数值、上一句意图、未完成主题给出最多 3 个提问。
- 提示通过 `IInterviewHintProvider` 接口隔离，可替换实现。

## 二、代码展示（中间，建议选 1～2 段）

以下都是项目中的真实代码，已删去无关行。请从中选择最适合一页展示的片段，可以进一步截短，但不要改变逻辑。

### 片段 A（首选）：采访处理主流程

文件：`Assets/Scripts/Interview/InterviewModels.cs`

```csharp
public InterviewReply Process(string rawInput)
{
    var classifyInput = InterviewLoc.CanonicalQuestion(input);

    if (IsHostile(classifyInput) || IsHostile(input))
    {
        var hostile = HandleHostile();
        stats.Apply(hostile);
        return hostile;
    }

    var intent = IsShortConfirmation(input) ? "followup" : Classify(classifyInput);
    var reply = BuildReply(input, intent);
    var finalIntent = reply.intent ?? intent;

    if (!IsNonStickyIntent(finalIntent)
        && askedIntents.Contains(finalIntent)
        && !reply.cognitiveBoundary)
    {
        reply.isRepeat = true;
        reply.replyLines = GetRepeatLines(finalIntent);
        reply.stressChange += 2;
        reply.attentionChange -= 3;
        reply.unlockedIntel.Clear();   // 重复提问不再解锁情报
    }
    else if (!IsNonStickyIntent(finalIntent))
    {
        askedIntents.Add(finalIntent);
    }

    stats.Apply(reply);
    return PostProcessReply(reply);
}

protected abstract string Classify(string input);
protected abstract InterviewReply BuildReply(string input, string intent);
```

可配说明：模板方法模式。基类固定处理顺序，子类只负责「怎么分类、怎么回答」。

### 片段 B：写稿前的段落覆盖校验

文件：`Assets/Scripts/Writing/ArticleAssembler.cs`

```csharp
public bool CanAssemble(WritingDirection dir, List<string> selected, out string error)
{
    if (selected == null || selected.Count == 0) { /* 至少选一张 */ return false; }
    if (selected.Count > 10)                      { /* 最多十张 */  return false; }

    // 不强制指定某张卡，只要求四个段落各至少一张
    CountParagraphCoverage(selected, out int p1, out int p2, out int p3, out int p4);
    if (p1 < 1) { error = MissingPara("ui.writing.para_01", "段落 01  现在的大福"); return false; }
    if (p2 < 1) { error = MissingPara("ui.writing.para_02", "段落 02  受伤与救助"); return false; }
    if (p3 < 1) { error = MissingPara("ui.writing.para_03", "段落 03  治疗与抉择"); return false; }
    if (p4 < 1) { error = MissingPara("ui.writing.para_04", "段落 04  回到社区"); return false; }
    return true;
}
```

可配说明：只约束结构，不规定答案，玩家的选材本身就是玩法。

### 片段 C：角色专属后处理（大福的食物限额）

文件：`Assets/Scripts/Interview/DafuRuleEngine.cs`

```csharp
InterviewReply ApplyFoodQuota(InterviewReply reply)
{
    if (reply.intent == "hungry") return reply;   // 玩家主动问吃的，保留原台词

    if (!CanMentionFood)                          // 最近 5 句里已经提过食物
    {
        var scrubbed = new List<string>();
        for (int i = 0; i < reply.replyLines.Count; i++)
        {
            var line = reply.replyLines[i];
            scrubbed.Add(TextMentionsFood(line) ? NonFoodSubstitute(reply.intent, i) : line);
        }
        reply.replyLines = scrubbed;
    }
    return reply;
}
```

可配说明：用滑动窗口控制角色口癖出现频率，保留个性又避免重复。

### 片段 D：Play 模式布局编辑器的组件锁定

文件：`Assets/Scripts/UI/UILayoutEditController.cs`

```csharp
public void PointerDown(PointerEventData eventData)
{
    // 锁定后只改当前组件，点到其他组件不会切换选择
    if (UILayoutEditMode.LockSelection && _selected != null && !UILayoutEditMode.TextFocus)
    {
        if (ContainsScreenPoint(_selected, eventData.position))
            BeginDragOnSelected(eventData);
        else
            SetStatus(true, "已锁定当前组件 — 只能改它。按 L 解锁后再选别的。");
        return;
    }

    var picked = UILayoutEditMode.TextFocus ? PickText(eventData.position)
                                            : PickBest(eventData.position);
    // ...解析可编辑目标，Alt 选父级...
    if (_selected != picked)
        UILayoutEditMode.LockSelection = true;    // 新选中的组件自动锁定
    _selected = picked;
    BeginDragOnSelected(eventData);
}
```

可配说明：适合放在工具区旁边，说明编辑器如何防止误操作。

## 三、Unity 开发工具（右侧）

项目大部分运行时 UI 由代码生成，没有依赖预制体摆位。为了让布局和文本可以在运行中直接调整，我做了一套只在 Play 模式生效的可视化编辑工具：在 Game 视图中操作，松手后自动保存到对应数据资产。

建议只列 4～5 项：

| 工具 | 一句话说明 |
|---|---|
| 通用 UI 布局编辑器 | Game 视图里拖动、缩放、微调、撤销；选中即锁定防误触；保存到 `UILayoutOverrides.asset` |
| 调查热点编辑器 | 在社区地图上拖拽可点击区域，保存到 `InvestigateHotspotLayout.asset` |
| 对白文本编辑器 | 按 `sceneId:lineIndex` 修改当前语言对白，写入中 / 英覆盖 JSON |
| 图片清晰度体检 | 批量检查压缩、尺寸上限、平台覆盖、过滤模式，可一键修复 |
| 调试跳转与字体工具 | 一键跳到调查 / 采访 / 写稿；烘焙 TMP 字体；清理失效的字号覆盖 |

可补一句设计取舍：

- 不同系统的布局数据分开存放；通用编辑器会跳过自由采访、调查热点、社交帖子、系统遮罩等路径，避免多套数据互相覆盖。

## 四、配图建议

- 管线流程图：横向箭头，从「自由提问」到「规则审核」。
- 自由采访界面截图：标注信任 / 压力 / 专注和提问芯片。【待补充截图】
- 素材卡库截图：标注四段结构和素材卡。【待补充截图】
- 布局编辑器截图：Game 视图中带橙色锁定框的组件。【待补充截图】
- 代码区使用深色等宽字体卡片，只高亮 2～3 行关键逻辑，例如：
  - `reply.unlockedIntel.Clear();`
  - `Classify` / `BuildReply` 两个抽象方法；
  - `if (p1 < 1) ...` 段落覆盖校验。

## 五、不要写进这一页

- 逐句剧情推进、存档、全屏 Canvas 等常规视觉小说功能；
- `GameUI` 拆成多个 partial 文件；
- 单纯替换美术素材；
- 把大模型说成采访核心（它只是可选润色层）。
