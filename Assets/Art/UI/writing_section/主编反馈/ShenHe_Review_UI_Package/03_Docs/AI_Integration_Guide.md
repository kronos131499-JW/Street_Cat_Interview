# 沈禾审稿界面 UI 组件接入说明

本包包含 1 张最终效果参考图、1 张评论面板底图，以及 4 个按钮组件。
所有按钮均保持统一比例，可直接缩放使用。

## 目录说明
- 00_Final_Preview
  - 00_ShenHe_Review_Final_Mockup.png：最终效果参考图
- 01_Review_Background
  - 01_Review_Panel_Background_NoText.png：评论背景底图（无正文、无标题文字）
- 02_Buttons
  - 02_Button_Back_to_Writing.png
  - 03_Button_Open_Notebook.png
  - 04_Button_Reinterview.png
  - 05_Button_Reselect_Angle.png

## 原始尺寸
- 最终效果图：1672 × 941
- 评论背景底图：1448 × 1086
- 四个按钮：2172 × 724（统一比例）

## 推荐层级结构
建议在游戏内按以下顺序搭建：
1. 场景背景 / 办公室环境
2. 半透明暗色遮罩（建议黑色 35%–45%）
3. 评论主面板 Review_Panel_Background_NoText
4. 标题与状态文本（程序动态填充）
5. 正文评论文本（程序动态填充）
6. 四个操作按钮
7. 角色立绘、铭牌、底部提示条等外层装饰

## 面板接入建议
- 评论背景图建议作为主弹窗底图使用，居中摆放。
- 顶部区域可动态填入：
  - 主标题：例如 Shen He's Review
  - 状态标签：例如 Returned branch C
  - 正文区：支持长文本自动换行
- 由于底图已经带有装饰线、纸张纹理、回形针和浅色叶片装饰，接入时不建议再叠加过多边框。

## 按钮接入建议
四个按钮已经统一风格和长宽比，建议保持同一高度，并横向排列在评论面板底部。

推荐顺序：
1. Back to writing
2. Open notebook
3. Re-interview...
4. Reselect angle

推荐排版：
- 四按钮等高排列
- 按钮之间保留一致间距
- 建议按钮宽度可统一，也可按文字长短微调
- 若引擎支持，按钮可加轻微悬停发亮 / 阴影增强

## 文本接入建议
本次拆出的按钮仅保留“主标题文字 + icon”，没有底部副标题。
如果需要程序动态替换语言，建议：
- 将这些图作为视觉参考直接使用；
- 或在引擎内重新排版同风格文字，把图只当按钮底板参考。

评论面板上的正文建议：
- 左对齐
- 行宽不要过满，保留纸张边距
- 标题字号明显大于正文
- 状态标签使用偏红棕或柔和砖红色

## 适配建议
- 当前参考比例接近 16:9 场景弹窗。
- 若适配 1920 × 1080，可直接按中心弹窗思路缩放。
- 若适配更窄画面，优先保证主面板完整显示，再压缩按钮横向间距。

## 命名建议（程序侧）
- UI_ReviewPanel_BG
- UI_Btn_BackToWriting
- UI_Btn_OpenNotebook
- UI_Btn_ReInterview
- UI_Btn_ReselectAngle
- UI_Review_FinalMockup_Ref

## 备注
最终效果图主要用于对照整体视觉风格和组件关系；实际接入时请以拆分组件为准。
