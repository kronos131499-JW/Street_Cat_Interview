# 美术重出清单

整理时间：2026-09-30
适用工程：`D:\Street_Cat_Interview`（Unity 2022.3.62f3）

这份清单只收录「改导入设置救不回来、必须重新出图」的资源。所有能靠导入设置解决的模糊
（DXT 块压缩、`maxTextureSize` 截断、Point 过滤、平台覆盖）已经在 2026-09-30 批量修正完毕，
共改动 375 个 `.meta`，不在本清单范围内。

机器可读版本见同目录的 `rework-list.csv`。

---

## 判定口径

游戏 Canvas 是 `ScaleWithScreenSize`，参考分辨率 **1920×1080**。
全屏背景走 `GameUI.stageArt`，它是 `StretchFull` + `preserveAspect = false`，
也就是**不管源图多大多宽，一律拉满 1920×1080**。

所以只要源图小于 1920×1080，就是在被放大；宽高比不是 16:9，还会被拉变形。
这两件事导入设置都管不了。

---

## 需要重出的 6 张（按优先级）

### P0 — 影响最大

| # | 运行时文件 | 当前 | 目标 | 问题 |
|---|---|---|---|---|
| 1 | `Assets/Resources/VnArt/Backgrounds/bg_huaian_map.png` | 1672×941 | 1920×1080 | 放大 1.15× |
| 2 | `Assets/Resources/VnArt/KeyArt/kv_title_street_interview.png` | 1536×1024 | 1920×1080 | 放大 1.25×，且 **3:2 被压成 16:9，竖向挤扁约 18%** |
| 3 | `Assets/Resources/VnArt/Title/Menu/background.png` | 1672×941 | 1920×1080 | 放大 1.15×，主菜单常驻画面 |

`bg_huaian_map` 是 SC-04 调查场景的社区平面图，玩家会长时间盯着它找热点，糊最明显。
`kv_title_street_interview` 是标题主视觉，除了糊还有明显的变形，优先级同样很高。

### P1 — 调查场景

| # | 运行时文件 | 当前 | 目标 | 问题 |
|---|---|---|---|---|
| 4 | `Assets/Resources/VnArt/Backgrounds/bg_feeding_spot.png` | 1672×941 | 1920×1080 | 放大 1.15× |
| 5 | `Assets/Resources/VnArt/Backgrounds/bg_feeding_sign.png` | 1698×926 | 1920×1080 | 放大 1.13×，且 1.834 的宽高比被压成 1.778，竖向略挤 |
| 6 | `Assets/Resources/VnArt/Backgrounds/bg_cat_alert.png` | 1672×941 | 1920×1080 | 放大 1.15× |

---

## 每张图的来源与用法

每张图在工程里有两份：`Assets/Art/…` 是美术原始文件，`Assets/Resources/VnArt/…` 是运行时
真正加载的那份（`Resources.Load`）。**两份都要替换**，只换 `Art` 下的不会生效。

### 1. `bg_huaian_map` — 槐安社区平面图

- 美术原图：`Assets/Art/Backgrounds/正式背景图/槐安社区_社区平面图.png`
- 运行时：`Assets/Resources/VnArt/Backgrounds/bg_huaian_map.png`
- 剧本标签：`【背景：槐安社区_社区平面图】`（`BuiltInScripts.cs` SC-04）
- 用途：调查场景底图，上面叠了 9 个可点击热点（猫屋 / 猫粮碗 / 水碗 / 告示牌 / 狸花猫 / 贩卖机 / 长椅 / 快递柜 / 保安亭）
- **构图要求：热点坐标是 0–1 归一化的，只要新图保持原构图（元素位置不挪），热点不用重调。**
  如果构图有变动，需要用 `街角专访 → 调查热点编辑器` 重新框一遍。
  当前宽高比 1.7768 与 16:9（1.7778）几乎一致，直接按原构图放大到 1920×1080 重绘即可。

### 2. `kv_title_street_interview` — 标题主视觉

- 美术原图：`Assets/Art/KeyArt/kv_title_street_interview.png`（另有一份重复文件 `Assets/Art/kv_title_street_interview.png`）
- 运行时：`Assets/Resources/VnArt/KeyArt/kv_title_street_interview.png`
- 触发：剧本标签里含「标题 / Title / 街角专访」时（`VnArt.ResolveBackground`）
- **注意：当前是 3:2，被强行拉成 16:9，人和物都被竖向压扁了。**
  重出时请按 16:9 重新构图，不要简单在原图上下补白——那样主体位置会偏。

### 3. `Title/Menu/background` — 主菜单桌面底图

- 美术原图：`Assets/Art/UI/Title/Menu_Components/background.png`
- 运行时：`Assets/Resources/VnArt/Title/Menu/background.png`
- 用途：`GameUI.TryApplySplitTitleBackground`，主菜单全屏底板，中间杂志区域是透明挖空的
- **构图要求：中间的透明挖空区域位置要和现在一致**，杂志、录音机等部件是按归一化锚点叠上去的
  （见 `TitleMenuLayout.Defaults`）。挖空位置变了需要在 `街角专访 → 主菜单布局编辑器` 里重调。
- 透明通道必须保留（带 Alpha 的 PNG）。

### 4. `bg_feeding_spot` — 流浪猫投喂点

- 美术原图：`Assets/Art/Backgrounds/正式背景图/流浪猫投喂点.png`
- 运行时：`Assets/Resources/VnArt/Backgrounds/bg_feeding_spot.png`
- 用途：SC-04 调查点「猫屋」「猫粮碗」「水碗」三处的背景（`InvestigationService.cs`）

### 5. `bg_feeding_sign` — 投喂点告示牌

- 美术原图：`Assets/Art/Backgrounds/正式背景图/流浪猫投喂点_告示牌png.png`
- 运行时：`Assets/Resources/VnArt/Backgrounds/bg_feeding_sign.png`
- 用途：SC-04 调查点「投喂点小挂牌」
- 当前 1698×926 比 16:9 更宽，会被竖向压一点；重出时直接按 1920×1080 构图

### 6. `bg_cat_alert` — 狸花猫警惕

- 美术原图：`Assets/Art/Backgrounds/正式背景图/晒太阳的猫_警惕.png`
- 运行时：`Assets/Resources/VnArt/Backgrounds/bg_cat_alert.png`
- 用途：SC-04「灌木旁的狸花猫」第 3 个 beat（小凌靠近，猫抬头警惕）
- 同组的 `晒太阳的猫_放松`、`晒太阳的猫_躲藏` 已经是 1920×1080，只有「警惕」这张小，
  连着播的时候画质会突然掉一档，观感很明显

---

## 出图规范

- 尺寸：**1920×1080**，正好等于 Canvas 参考分辨率，1:1 显示不缩放
- 格式：PNG，8 bit/通道；需要透明的（主菜单底图）保留 Alpha
- **不要用 AI 放大 / Photoshop 缩放现有小图顶替。** 放大过的图细节回不来，导入设置也救不回，
  和现在的状态没有区别
- 不要加锐化预处理，导入已经是 Uncompressed + Bilinear，源图什么样屏幕上就什么样
- 文件名保持不变，直接覆盖上面列的两个路径
- 覆盖后回到 Unity 会自动重导入；导入设置（不压缩、质量 100、`maxTextureSize` 按源图）
  已经写死在 `.meta` 里，不用手动调

新加美术时可以跑一下 `街角专访 → 美术 → 图片清晰度体检（只出报告）`，
报告会输出到 `Temp/TextureAudit.csv`，能查出被压缩 / 被缩小 / Point 过滤的图。

---

## 附：不需要重出——这 10 个文件已经没在用了

排查时发现下面这些图在代码里完全取不到（`VnArt.ResolveBackground` 的精确表和模糊匹配
都不会返回这些 key），属于历史残留。**不要花时间重出，建议直接删掉**，
包括同名的 `.meta`：

| 文件 | 尺寸 | 说明 |
|---|---|---|
| `Resources/VnArt/Backgrounds/bg_epilogue_morning.png` | 1536×1024 | 无引用 |
| `Resources/VnArt/Backgrounds/bg_interview_corner.png` | 1536×1024 | 无引用 |
| `Resources/VnArt/Backgrounds/bg_magazine_office.png` | 1536×1024 | 无引用 |
| `Resources/VnArt/Backgrounds/bg_workstation.png` | 1536×1024 | 无引用 |
| `Resources/VnArt/Backgrounds/bg_writing_desk.png` | 1536×1024 | 无引用 |
| `Resources/VnArt/Backgrounds/bg_huaian_community.png` | 1920×1080 | 无引用，且疑似由 1536×1024 放大而来 |
| `Resources/VnArt/Backgrounds/bg_guard_booth_afternoon.png` | 1920×1080 | 无引用，同上（已被 `bg_guard_afternoon` 取代） |
| `Resources/VnArt/Backgrounds/bg_guard_booth_dusk.png` | 1920×1080 | 无引用，同上（已被 `bg_guard_dusk` 取代） |
| `Resources/VnArt/Backgrounds/bg_shenhe_office.png` | 1920×1080 | 无引用，同上（已被 `_dusk` / `_morning` 取代） |
| `Resources/VnArt/Title/Menu/background_new.png` | 1672×941 | 无引用，代码只读 `Menu/background` |

后 4 个在 `Assets/Art/Backgrounds/` 下有对应的 1536×1024 原图，而 `Resources` 里却是
1920×1080，说明当时是放大导出的。因为它们都没在用，所以不用管，删掉即可。

删之前建议先用 Unity 的 `Edit → Find References In Scene` 或全局搜一遍 GUID 确认，
`Resources` 目录下的资源不会被静态引用检测到。
