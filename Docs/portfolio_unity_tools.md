# 《街角专访》Unity 开发工具：实现说明

本文整理三套 Play 模式可视化编辑工具：

1. 运行时 UI 布局编辑器（Runtime UI）
2. 调查热点编辑器
3. 角色立绘实时编辑器

三者遵循同一个思路：**画面由代码在运行时生成，编辑在 Play 模式的 Game 视图里直接进行，松手后写入数据资产，下次运行由代码读取数据还原。**

文中代码均摘自项目源码，只删去了与说明无关的行。

---

## 共同背景

项目的界面几乎全部由 C# 在运行时创建，例如：

```csharp
var go = new GameObject("Spot_" + id, typeof(RectTransform), typeof(Image), typeof(Button));
go.transform.SetParent(investigateHotspotLayer, false);
```

这样做的好处是界面和游戏状态（当前场景、已调查的点、采访对象、写稿进度）联动方便。代价是：

- 场景里没有可以拖的预制体，Inspector 里看到的位置一退出 Play 就丢失；
- 只能改代码里的坐标数字，再重新进 Play、走到对应剧情才能看到效果；
- 美术和策划没法自己调。

所以三套工具都要解决同一件事：**在不放弃代码生成界面的前提下，让画面可以所见即所得地调，并且调整结果能保存、能在正式运行时生效。**

三套工具的数据分开存放：

| 工具 | 数据资产 | 负责的对象 |
|---|---|---|
| 运行时 UI 布局编辑器 | `Resources/UILayoutOverrides.asset` | 一般界面控件的位置、尺寸、显隐、文字样式 |
| 调查热点编辑器 | `Resources/InvestigateHotspotLayout.asset` | 社区地图上可点击区域 |
| 立绘实时编辑器 | `Resources/PortraitLayout.asset` | 剧情立绘所在区域与缩放 |

数据放在 `Resources` 下，是因为正式构建也要读取；编辑功能本身只在编辑器里编译（`#if UNITY_EDITOR`），不会进入玩家版本。

---

## 一、运行时 UI 布局编辑器

### 1. 遇到的问题

- 界面控件数量多，而且是按需创建的：对话框、按钮、笔记、写稿台、社交手机等，会在不同时刻生成、销毁、重建。
- 同名控件很多（例如多个 `Label`、`Btn`），无法只靠名字定位。
- 很多控件处在 `LayoutGroup` 下，手动改位置会在下一次布局计算时被冲掉。
- 屏幕上叠着全屏遮罩、点击推进对话的接收层，鼠标一点往往点到的是这些大层，而不是想改的小控件。
- 有些界面已经有专门的布局数据（社交手机、调查热点、立绘、自由采访），通用工具如果也去存，就会出现两套数据互相覆盖。

### 2. 工具的意义

- 任意运行时 UI 都能在 Game 视图里点选、拖动、四角缩放、方向键微调。
- 支持隐藏（删除）控件、恢复、撤销上一步，以及单独修改某段文字的字体、字号、字距、粗细、颜色。
- 调整结果写成数据，后续新创建的同一个控件会自动套用，不需要回去改代码。
- 对已经有专属数据的系统做隔离，避免通用工具误改。

### 3. 如何实现

#### 3.1 整体结构

| 类 | 职责 |
|---|---|
| `UILayoutEditController` | 编辑器中的选择、拖拽、缩放、删除、撤销、绘制选框（仅编辑器） |
| `UILayoutPointerSurface` | 顶层透明输入面，把鼠标事件转给控制器 |
| `UILayoutOverrides` | 路径生成、保存、应用、路径保护规则 |
| `UILayoutOverrideData` | `ScriptableObject` 数据资产，保存每个控件的覆盖记录 |
| `UILayoutOverrideRuntime` | 运行时自动把已保存的覆盖套到新生成的控件上（正式版也运行） |
| `UILayoutEditMode` | 编辑开关、网格吸附、锁定选择等设置，存在 `EditorPrefs` |
| `UILayoutEditorWindow` | 菜单窗口：开关、撤销、恢复、文字样式面板 |

#### 3.2 自动安装，不需要手动挂脚本

控制器和运行时套用器都在场景加载后自动创建，并隐藏在 Hierarchy 中：

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void Install()
{
    if (FindObjectOfType<UILayoutEditController>() != null) return;
    var host = new GameObject("UILayoutEditController");
    host.hideFlags = HideFlags.HideInHierarchy;
    DontDestroyOnLoad(host);
    host.AddComponent<UILayoutEditController>();
}
```

#### 3.3 用「路径」给运行时控件定身份

运行时控件每次重建，InstanceID 都会变化，不能当主键。工具用「从根 Canvas 到控件的名字链 + 同名兄弟序号」作为稳定 ID，例如 `VnCanvas/WritingDeskOverlay#0/Paper#0/LeftColumn#0/Headline#0`：

```csharp
public static string GetPath(Canvas canvas, RectTransform target)
{
    var builder = new StringBuilder(128);
    builder.Append(Escape(canvas.name));
    var chain = new List<Transform>();
    for (var current = target.transform; current != null && current != root; current = current.parent)
        chain.Add(current);
    for (var i = chain.Count - 1; i >= 0; i--)
    {
        builder.Append('/');
        builder.Append(Escape(chain[i].name));
        builder.Append('#');
        builder.Append(GetSameNameIndex(chain[i]));   // 同名兄弟中的第几个
    }
    return builder.ToString();
}
```

`#序号` 解决了同名控件的歧义；名字里的 `/`、`#` 会被转义，保证路径能可靠解析。

#### 3.4 输入：用一层透明面接管 Game 视图

编辑模式开启时，在最上层创建一个几乎透明、但能接收射线的全屏面，并把排序设到最高。这样点击不会触发游戏本身的按钮或推进对话，全部交给编辑器处理：

```csharp
var canvas = _captureRoot.GetComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = short.MaxValue - 2;

var image = capture.GetComponent<Image>();
image.color = new Color(0f, 0f, 0f, 0.001f);
image.raycastTarget = true;
```

```csharp
public sealed class UILayoutPointerSurface : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public UILayoutEditController Owner;
    public void OnPointerDown(PointerEventData e) => Owner?.PointerDown(e);
    public void OnDrag(PointerEventData e)        => Owner?.PointerDrag(e);
    public void OnPointerUp(PointerEventData e)   => Owner?.PointerUp(e);
}
```

#### 3.5 选中：优先「更深、更小」的控件，跳过遮罩

鼠标下面往往同时叠着十几层 RectTransform。工具给每个命中的对象打分：层级越深、面积越小，分数越高；名字里带 `Catcher`、`Dimmer`、`Blocker`、`Backdrop` 等的遮罩层直接跳过；接近全屏（超过画布 82%）的对象默认也跳过：

```csharp
if (UILayoutEditMode.SkipFullscreenCatchers && canvasArea > 1f &&
    area >= canvasArea * FullscreenAreaRatio)
    continue;

// 更深 + 更小者胜出，层级权重大于面积差
var depth = 0;
for (var t = target.transform; t != null && t != _targetCanvas.transform; t = t.parent)
    depth++;
var score = depth * 100000f - area;
if (score > bestScore) { best = target; bestScore = score; }
```

命中后还会做一次「意图修正」（`ResolveEditableTarget`）：

- 点到按钮里铺满的文字时，改为选中按钮本身；
- 对话框顶部的三个工具按钮作为一个整体移动，保持等宽等距；
- 手机帖子图层改为选中手机外框；
- 按住 Alt 选父级。

#### 3.6 LayoutGroup 下的控件也能单独拖

`LayoutGroup` 每一帧都会重排子物体。工具在选中时给控件挂 `LayoutElement` 并设置 `ignoreLayout = true`，让它脱离自动布局；这个标记也会被保存，下次运行照样生效：

```csharp
if (releaseFromLayout && IsControlledByLayout(picked))
{
    var layoutElement = picked.GetComponent<LayoutElement>();
    if (layoutElement == null) layoutElement = picked.gameObject.AddComponent<LayoutElement>();
    layoutElement.ignoreLayout = true;
    Canvas.ForceUpdateCanvases();
}
```

#### 3.7 选中即锁定，防止误触其他控件

调整一个控件时，很容易不小心点到旁边的控件导致选择被切走。工具在选中新控件时自动锁定：锁定期间可以继续拖动、缩放当前控件，但点击其他位置不会改变选择，按 L 才解除：

```csharp
public void PointerDown(PointerEventData eventData)
{
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
    // ……意图修正、Alt 选父级……
    if (_selected != picked)
        UILayoutEditMode.LockSelection = true;   // 新选中的控件自动锁定
    _selected = picked;
    BeginDragOnSelected(eventData);
}
```

#### 3.8 拖动与缩放：在父节点局部坐标里计算

按下位置离四角 22 像素以内是缩放，否则是移动。拖动量统一换算到父节点的局部坐标，再改 `anchoredPosition` 和尺寸。缩放时根据 pivot 修正位置，保证拖的是哪个角，对角就保持不动：

```csharp
if (right)
{
    size.x = Mathf.Max(8f, _dragStartSize.x + delta.x);
    position.x = _dragStartPosition.x + (size.x - _dragStartSize.x) * _selected.pivot.x;
}
else if (left)
{
    size.x = Mathf.Max(8f, _dragStartSize.x - delta.x);
    position.x = _dragStartPosition.x + (_dragStartSize.x - size.x) * (1f - _selected.pivot.x);
}
// 纵向同理
if (UILayoutEditMode.SnapEnabled) size = Snap(size);
_selected.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
_selected.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
_selected.anchoredPosition = position;
```

一个特殊情况：滚动区里的短对白被 `ScrollRect` 钉在顶部，往下拖时负的 `anchoredPosition` 会被吞掉。工具改为调整文字的顶部 margin，这样文字下移后能保留。

#### 3.9 保存：只存布局，不意外锁死字号

松手时保存。保存前先检查路径归属：

```csharp
public static bool Save(Canvas canvas, RectTransform target, bool captureFontSize = false)
{
    if (target != null && IsProtectedSystemOverlay(target.name))
        return false;                                         // 场景淡入淡出、对话推进层等系统层

    var path = GetPath(canvas, target);
    if (IsSocialOwnedPath(path))
        return SocialLayout.TrySaveFromRect(target);          // 改道到社交手机自己的数据

    if (IsInvestigateHotspotPath(path) || IsInvestigateHotspotSpot(target))
    {
        if (InvestigateHotspotLayout.TrySaveFromRect(target)) // 改道到调查热点数据
        {
            DropStaleHotspotOverride(path);                   // 清掉通用表里的旧记录
            return true;
        }
        return false;
    }

    var asset = EnsureAsset();
    UnityEditor.Undo.RecordObject(asset, "Save UI Layout");
    asset.Set(path, target, captureFontSize);
    UnityEditor.EditorUtility.SetDirty(asset);
    UnityEditor.AssetDatabase.SaveAssets();
    _revision++;                                              // 通知运行时重新套用
    return true;
}
```

一条覆盖记录保存的内容：

```csharp
public sealed class UILayoutOverrideEntry
{
    public string path;
    public bool deleted;              // 隐藏该控件
    public bool ignoreParentLayout;   // 脱离 LayoutGroup
    public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta;
    public float fontSize;            // 0 = 跟随设置里的字号
    public string fontId;             // 空 = 跟随全局字体
    public bool overrideLetterSpacing; public float letterSpacing;
    public int fontWeight;
    public bool overrideColor; public Color textColor;
    public float textMarginTop;
    public bool textOnly;             // 只改文字样式，不改位置
}
```

这里踩过一个坑：早期每次保存位置时，都会把当前字号一起写进去。结果只要某段文字被挪过一次，它的字号就被固定，玩家在设置里调字号对它不再生效。后来改为保存位置时沿用已有的文字设置，`fontSize` 为 0 表示跟随设置；只有笔记文字的缩放把手才会主动记录字号。另外提供了一个菜单命令，批量清除旧数据里被锁死的字号。

#### 3.10 运行时套用：新控件出现的那一帧就生效

```csharp
void LateUpdate()
{
    if (_seenRevision != UILayoutOverrides.Revision)   // 数据变化后全部重新套用
    {
        _seenRevision = UILayoutOverrides.Revision;
        _applied.Clear();
    }

    var data = UILayoutOverrides.Asset;
    if (data == null || data.entries == null || data.entries.Count == 0) return;
    foreach (var canvas in FindObjectsOfType<Canvas>())
    {
        if (canvas == null || !canvas.isRootCanvas) continue;
        _rects.Clear();
        canvas.GetComponentsInChildren(true, _rects);
        foreach (var target in _rects)
        {
            var id = target.GetInstanceID();
            if (_applied.Contains(id)) continue;          // 已套用过的不重复处理
            UILayoutOverrides.TryApply(canvas, target);
            _applied.Add(id);
        }
    }
}
```

最初的做法是定时扫描，新生成的控件会先以代码里的尺寸出现，最长 1.5 秒后才跳到保存的位置。现在在 `LateUpdate` 里每帧检查，用 InstanceID 记录已处理的对象，新控件出现的那一帧就会被套用，已套用的不会重复写入。

套用时再次检查路径保护，下面这些都不会被通用数据改写：

```csharp
if (IsSocialOwnedPath(path) || IsInvestigateHotspotPath(path) || IsEphemeralControlPath(path)
    || IsInterviewMeterPath(path) || IsInterviewChatBubblePath(path)
    || IsFreeInterviewPath(path) || IsStagePortraitPath(path))
    return false;
```

| 被保护的路径 | 原因 |
|---|---|
| 社交手机层 | 由 `SocialLayout.asset` 负责 |
| 调查热点层 | 由 `InvestigateHotspotLayout.asset` 负责 |
| 每屏临时生成的操作按钮 | 旧的「删除」或绝对位置会把「确认发布」等按钮藏掉或停在错误位置 |
| 采访的信任 / 压力 / 专注条 | 由代码按底板布局，旧的拖动会盖住数字 |
| 采访聊天气泡 | 每句话都会重建，保存的位置会把气泡和头像拉出遮罩 |
| 自由采访界面 | 按美术底板在代码中精确布局 |
| 舞台立绘 | 由 `PortraitLayout.asset` 负责 |

#### 3.11 撤销、删除与恢复

- 每次开始拖动前记录一个快照（是否原本已有保存记录 + 当时的完整状态），保存成功后才写入撤销栈。
- 撤销时同时恢复数据资产和画面上的控件。
- 删除会弹确认框，删除后可以通过「恢复上次删除」还原。

### 4. 最终效果

- 开启编辑模式后，在 Game 视图中可以直接选中任意运行时生成的界面元素，进行移动、缩放、微调、隐藏、改字体字号。
- 调整后松手即保存，无需改代码、无需重进 Play。
- 正式运行时，即使界面是之后才动态生成的，也会在出现的同一帧套用已保存的布局。
- 不同系统的布局数据互不干扰，通用工具不会覆盖专用工具的结果。

【待补充：截图 — Game 视图中橙色锁定框选中某个按钮，以及编辑器窗口】

---

## 二、调查热点编辑器

### 1. 遇到的问题

- 调查玩法是在一张社区平面图上点击投喂点、狸花猫、贩卖机、长椅、快递柜、保安亭等区域。
- 这些可点击区域是运行时生成的透明按钮，看不见边界，只能靠代码里的数字猜位置。
- 底图换过美术后，所有热点都要重新对齐；不同分辨率下，用像素写死的位置也会偏移。
- 编辑时如果点一下就触发调查剧情，会打断调整过程。

### 2. 工具的意义

- 让每个热点在编辑模式下变成可见的橙色半透明框，直接在底图上拖动、缩放对齐。
- 松手即保存，下次运行自动读取。
- 坐标使用相对底图的 0～1 比例，换分辨率不需要重新调。

### 3. 如何实现

#### 3.1 数据结构：归一化矩形

每个热点保存一个 `Vector4(xMin, yMin, xMax, yMax)`，取值 0～1，原点在左下角，相对热点层：

```csharp
[Serializable]
public class HotspotRectEntry
{
    public string id;
    public Vector4 rect;   // xMin, yMin, xMax, yMax（0–1，左下角为原点）
}

public class InvestigateHotspotLayoutData : ScriptableObject
{
    public List<HotspotRectEntry> entries = new List<HotspotRectEntry>();

    public static Vector4 ClampRect(Vector4 r)
    {
        float xMin = Mathf.Clamp01(Mathf.Min(r.x, r.z));
        float xMax = Mathf.Clamp01(Mathf.Max(r.x, r.z));
        float yMin = Mathf.Clamp01(Mathf.Min(r.y, r.w));
        float yMax = Mathf.Clamp01(Mathf.Max(r.y, r.w));
        if (xMax - xMin < 0.02f) xMax = Mathf.Min(1f, xMin + 0.02f);   // 最小尺寸
        if (yMax - yMin < 0.02f) yMax = Mathf.Min(1f, yMin + 0.02f);
        return new Vector4(xMin, yMin, xMax, yMax);
    }
}
```

`ClampRect` 保证拖出边界、左右拖反或拖得太小时，仍然得到一个合法的矩形。

#### 3.2 读取顺序：数据资产优先，内置默认值兜底

```csharp
public static bool TryGet(string hotspotId, string backgroundKey, out Vector4 rect)
{
    var asset = Asset;
    if (asset != null && asset.TryGet(hotspotId, out rect))
        return true;
    return DefaultHuaianMap.TryGetValue(hotspotId, out rect);   // 代码内置的默认布局
}
```

第一次保存时，资产会以内置默认值初始化，避免只保存了一个点，其他点全部丢失。

#### 3.3 生成热点时直接用比例锚点

热点按钮的 `anchorMin / anchorMax` 就是保存的比例，`offset` 清零。按钮会随底图一起缩放，任何分辨率下都贴在同一位置：

```csharp
if (!InvestigateHotspotLayout.TryGet(id, layoutKey, out var rect))
    rect = new Vector4(0.1f, 0.4f, 0.25f, 0.55f);   // 缺数据时仍保留可点区域

var go = new GameObject("Spot_" + id, typeof(RectTransform), typeof(Image), typeof(Button));
var rt = go.GetComponent<RectTransform>();
rt.anchorMin = new Vector2(rect.x, rect.y);
rt.anchorMax = new Vector2(rect.z, rect.w);
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
```

#### 3.4 编辑模式：点击不触发调查，热点变成可见框

同一个按钮在编辑模式下拦截原有的调查行为，并额外挂上拖拽组件：

```csharp
btn.onClick.AddListener(() =>
{
#if UNITY_EDITOR
    if (InvestigateHotspotEditMode.Enabled) return;   // 编辑时不进入调查
#endif
    action();
});

#if UNITY_EDITOR
var drag = go.AddComponent<DraggableInvestigateHotspot>();
drag.HotspotId = id;
drag.Title = title;
#endif
```

`DraggableInvestigateHotspot` 每帧检查编辑开关：开启时把热点涂成橙色半透明，关闭时恢复原本的颜色；并在框的左上角用 `OnGUI` 写出热点名称，方便辨认。

#### 3.5 拖动：直接改比例锚点

拖拽量先转换为热点层的局部坐标，再除以热点层尺寸，得到锚点的变化量。移动时四边一起动，拖角时只改对应的两条边：

```csharp
var size = _layer.rect.size;
var delta = localNow - localStart;
var dAnchor = new Vector2(delta.x / size.x, delta.y / size.y);

var min = _startMin;
var max = _startMax;
switch (_mode)
{
    case Mode.Move:     min += dAnchor; max += dAnchor; break;
    case Mode.ResizeBL: min += dAnchor;                 break;
    case Mode.ResizeBR: max.x += dAnchor.x; min.y += dAnchor.y; break;
    case Mode.ResizeTL: min.x += dAnchor.x; max.y += dAnchor.y; break;
    case Mode.ResizeTR: max += dAnchor;                 break;
}

var clamped = InvestigateHotspotLayoutData.ClampRect(new Vector4(min.x, min.y, max.x, max.y));
_rt.anchorMin = new Vector2(clamped.x, clamped.y);
_rt.anchorMax = new Vector2(clamped.z, clamped.w);
_rt.offsetMin = Vector2.zero;
_rt.offsetMax = Vector2.zero;
```

判断移动还是缩放：按下位置落在框四角各 22% 范围内是缩放，否则是移动。

松手时保存：

```csharp
public void OnEndDrag(PointerEventData eventData)
{
    if (!InvestigateHotspotEditMode.Enabled) return;
    _mode = Mode.None;
    InvestigateHotspotLayout.SaveRectFromTransform(HotspotId, _rt);
}
```

#### 3.6 和通用 UI 编辑器的衔接

如果开发者用通用布局编辑器去拖热点，通用编辑器改的是 `anchoredPosition`，只读锚点会漏掉这部分偏移。所以保存时按热点的四个实际角点反推比例矩形：

```csharp
static bool TryReadNormalizedRect(RectTransform rt, out Vector4 rect)
{
    var parent = rt.parent as RectTransform;
    var bounds = parent.rect;
    var corners = new Vector3[4];
    rt.GetWorldCorners(corners);
    var bl = parent.InverseTransformPoint(corners[0]);
    var tr = parent.InverseTransformPoint(corners[2]);
    rect = new Vector4(
        Mathf.InverseLerp(bounds.xMin, bounds.xMax, bl.x),
        Mathf.InverseLerp(bounds.yMin, bounds.yMax, bl.y),
        Mathf.InverseLerp(bounds.xMin, bounds.xMax, tr.x),
        Mathf.InverseLerp(bounds.yMin, bounds.yMax, tr.y));
    return true;
}
```

这样无论用哪种工具拖，最终都只写进 `InvestigateHotspotLayout.asset`，通用布局表里的旧记录会被删除，热点不会被两套数据拉来拉去。

### 4. 最终效果

- 打开「街角专访 → 调查热点编辑器」并开启编辑模式，进入槐安社区调查界面后，所有热点显示为带名字的橙色框。
- 中间拖动整体移动，四角拖动缩放，松手自动保存。
- 编辑过程中点击热点不会进入调查剧情。
- 坐标为相对底图的比例，在不同窗口尺寸下保持对齐。

【待补充：截图 — 社区平面图上的橙色热点框】

---

## 三、角色立绘实时编辑器

### 1. 遇到的问题

- 剧情对话中，立绘的位置和大小不是固定写死的，而是代码根据「立绘区域」和每张图的宽高比实时计算出来的。不同角色、不同表情的立绘尺寸比例都不一样。
- 如果直接拖立绘图片本身，下一句换表情时代码重新计算，拖动结果会被覆盖。
- 编辑时一点击画面，就会触发「点击推进对话」，剧情往下走，正在调的立绘就消失了。
- 需要分别检查沈禾、大福、林女士、保安、小凌等角色在同一套参数下是否都合适。
- 通用 UI 编辑器早期曾保存过旧的立绘位置，会把所有角色拉回旧位置。

### 2. 工具的意义

- 调整的不是某一张图，而是「立绘区域 + 适配规则」，一次调整对所有角色和表情都生效。
- 在真实剧情画面里实时看效果，拖动和滑条变化立即反映到画面。
- 编辑期间屏蔽对话推进，方便专心调整。
- 可以一键切换预览角色，检查不同体型的立绘。

### 3. 如何实现

#### 3.1 参数化：区域 + 适配规则

```csharp
public class PortraitLayoutData : ScriptableObject
{
    // 立绘区域（屏幕锚点 0–1，左下角为原点）
    public float slotLeft = 0.72f;
    public float slotRight = 0.99f;
    public float slotTop = 0.98f;
    public float slotBottom = 0.34f;   // 低于对话框顶部时，立绘可以压在对话框上

    // 区域内的适配方式
    public float heightScale = 1.10f;  // 按比例适配后的额外缩放
    [Range(0f, 1f)] public float centerBias = 0.58f;     // 0.5 居中，大于 0.5 偏右
    [Range(-0.35f, 0.35f)] public float offsetY = 0f;    // 垂直微调
}
```

`Clamp()` 会限制最小宽高、底边下限和各项取值范围，保证编辑出来的参数始终合法。没有数据资产时，使用 `VnTheme` 里的默认值。

#### 3.2 立绘位置由参数和图片宽高比计算

每次换立绘都会调用 `LayoutPortraitRect`：先按区域高度和图片宽高比适配，宽度超出就改按宽度适配；再乘以缩放系数；按 `centerBias` 决定水平中心；最后加上垂直微调：

```csharp
float slotW = slotRight - slotLeft;
float slotH = slotTop - slotBottom;

float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
float heightNorm = slotH;
float widthNorm = heightNorm * aspect;
if (widthNorm > slotW) { widthNorm = slotW; heightNorm = widthNorm / aspect; }

heightNorm *= PortraitLayout.HeightScale;
widthNorm = heightNorm * aspect;
if (widthNorm > slotW) { widthNorm = slotW; heightNorm = widthNorm / aspect; }
if (heightNorm > slotH) heightNorm = slotH;

float cx = slotLeft + slotW * PortraitLayout.CenterBias;
float left = cx - widthNorm * 0.5f;
float right = cx + widthNorm * 0.5f;
float bottom = slotBottom;
float top = bottom + heightNorm;

// offsetY 是真正的位移，不再被压回区域内，
// 否则已经占满区域的立绘无法通过滑条上移
bottom += offsetY;
top += offsetY;
```

这就是为什么工具编辑的是区域和规则，而不是图片：图片的最终位置始终由这套计算得出，换任何表情都一致。

#### 3.3 区域拖拽用 IMGUI 实现

立绘区域没有对应的 UI 对象，工具直接在屏幕上用 IMGUI 画一个青色框。用 `GUIUtility.hotControl` 管理拖拽，按下、拖动、松开三个事件完整闭环：

```csharp
var slotRect = AnchorsToGuiRect(d.slotLeft, d.slotBottom, d.slotRight, d.slotTop);
DrawSlotOutline(slotRect);

int controlId = GUIUtility.GetControlID("PortraitSlotImGui".GetHashCode(), FocusType.Passive);
var e = Event.current;

switch (e.type)
{
    case EventType.MouseDown:
        _slotDragMode = PickSlotDragMode(e.mousePosition, slotRect, handle);
        if (_slotDragMode == SlotDragNone) break;
        _slotDragStartMouse = e.mousePosition;
        _slotDragL = d.slotLeft; _slotDragR = d.slotRight;
        _slotDragB = d.slotBottom; _slotDragT = d.slotTop;
        GUIUtility.hotControl = controlId;
        e.Use();
        break;

    case EventType.MouseDrag:
        if (GUIUtility.hotControl != controlId) break;
        ApplySlotImGuiDrag(d, e.mousePosition - _slotDragStartMouse);
        RefreshPortraitLayoutFromAsset();      // 拖动时实时重算立绘
        e.Use();
        break;

    case EventType.MouseUp:
        if (GUIUtility.hotControl != controlId) break;
        GUIUtility.hotControl = 0;
        d.Clamp();
        PortraitLayout.SaveCurrent();          // 松手保存
        RefreshPortraitLayoutFromAsset();
        e.Use();
        break;
}
```

屏幕坐标和锚点坐标的换算：GUI 的 y 轴向下，锚点的 y 轴向上，需要翻转：

```csharp
static void ApplySlotImGuiDrag(PortraitLayoutData d, Vector2 deltaGui)
{
    float dx = deltaGui.x / Screen.width;
    float dy = -deltaGui.y / Screen.height;   // GUI y 向下，锚点 y 向上
    // 按拖拽模式修改对应的边……
}
```

#### 3.4 整体移动时保持区域大小

早期把区域拖到屏幕边缘时，只裁掉了超出的那条边，区域被压扁，立绘却没跟着移动。现在整体移动时，碰边会把整个区域往回推，宽高保持不变：

```csharp
static void KeepSlotOnScreen(ref float l, ref float r, ref float b, ref float t)
{
    float w = r - l;
    float h = t - b;
    if (t > 1f) { t = 1f; b = t - h; }
    if (b < VnTheme.PortraitSlotBottomMin) { b = VnTheme.PortraitSlotBottomMin; t = Mathf.Min(1f, b + h); }
    if (r > 1f) { r = 1f; l = r - w; }
    if (l < 0f) { l = 0f; r = Mathf.Min(1f, l + w); }
}
```

#### 3.5 编辑期间屏蔽对话推进

进入编辑模式时，记录推进层原本的状态，然后关闭它的射线检测和交互；退出编辑模式再恢复：

```csharp
void BlockInputForPortraitEdit()
{
    if (advanceCatcher != null)
    {
        advanceCatcher.raycastTarget = false;
        advanceCatcher.gameObject.SetActive(false);
        var btn = advanceCatcher.GetComponent<Button>();
        if (btn != null) btn.interactable = false;
    }
    if (dialogueClick != null)
    {
        dialogueClick.interactable = false;
        var dlgImg = dialogueClick.GetComponent<Image>();
        if (dlgImg != null) dlgImg.raycastTarget = false;
    }
    if (hideDialogueBtn != null)
        hideDialogueBtn.interactable = false;
}
```

鼠标在参数面板上操作时，也不会同时拖动区域框（`PortraitEditPanelConsumesMouse`）。

#### 3.6 参数面板与预览角色

按 F10 显示或隐藏画面左侧的参数面板，可以用滑条调整左右上下边界、缩放、水平位置、垂直微调；面板里还有「保存」「恢复默认」按钮，以及五个角色的预览按钮：

```csharp
string[] keys   = { "ch_shenhe_default", "ch_dafu_default", "ch_lin_default", "ch_guard_default", "ch_xiaoling_default" };
string[] labels = { "沈禾", "大福", "林女士", "保安", "小凌" };
for (int i = 0; i < keys.Length; i++)
{
    if (GUILayout.Button(labels[i], GUILayout.Width(56f)))
    {
        _portraitEditPreviewKey = keys[i];
        SetPortrait(keys[i]);   // 立即换成该角色，用同一套参数重新计算位置
    }
}
```

如果进入编辑模式时画面上没有立绘，工具会自动放一个预览立绘，避免空着调。编辑器窗口「街角专访 → 立绘布局编辑器」提供同样的参数滑条、资源定位和重置按钮。

#### 3.7 配套：逐句立绘表情调试（F11）

除了位置布局，还有一个按剧情行调整表情的调试面板：

- 按 F11 打开，可以给当前这句对白选择角色和表情；
- 点击是预览，确认后只对这一句生效；
- 以 `script:场景ID:行号` 为键记录，仅在当前测试会话中有效，用于快速检查剧本标注的表情是否合适。

#### 3.8 和通用 UI 编辑器的隔离

舞台立绘的路径被通用 UI 编辑器列为受保护路径（`IsStagePortraitPath`），不会被通用覆盖表改写。原因是早期通用工具保存过立绘贴着对话框的旧位置，导致所有角色都被拉低。

### 4. 最终效果

- 开启编辑模式并进入有立绘的剧情（可用 F9 跳到 SC-02），画面上出现青色立绘区域框。
- 拖动框的中间整体移动，拖四角缩放，立绘实时跟随；松手自动保存到 `PortraitLayout.asset`。
- F10 参数面板精细调整缩放、水平位置和垂直位置，一键切换五个角色检查效果。
- 编辑期间点击画面不会推进对话。
- 调整结果对所有角色、所有表情统一生效。

【待补充：截图 — 剧情画面中的青色立绘区域框与左侧参数面板】

---

## 四、三套工具的共同设计取舍

| 取舍 | 说明 |
|---|---|
| 编辑代码与运行代码分离 | 编辑交互全部放在 `#if UNITY_EDITOR` 中，玩家版本不包含；数据读取与套用在正式版中照常运行 |
| 数据驱动而不是改代码 | 位置、尺寸、样式保存为 `ScriptableObject`，代码只负责读取和计算 |
| 按系统拆分数据 | 通用 UI、调查热点、立绘、社交手机各有数据资产，并通过路径保护避免互相覆盖 |
| 比例坐标优先 | 热点和立绘使用 0～1 锚点，适配不同分辨率 |
| 编辑时屏蔽游戏输入 | 透明输入面、拦截热点点击、关闭对话推进层，保证编辑不触发剧情 |
| 松手即保存 | 降低忘记保存的风险；通用 UI 编辑器额外提供撤销和删除恢复 |
| 统一入口 | 「街角专访」菜单下集中打开各编辑器，并提供「关闭所有编辑器」一键恢复正常游玩 |
