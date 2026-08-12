/**
 * Artist-facing UI art requirement rows for build-ui-art-refs-xlsx.mjs
 * Annotations written for illustrators who have never played the game.
 */
import fs from "fs";
import path from "path";

/** Normalized rect {x,y,w,h} in 0..1 */
export function nr(x, y, w, h) {
  return { x, y, w, h };
}

/**
 * Mockup regions on free_interview_mockup_ref.png (full-bleed UI mock).
 * Tuned to three-column scrapbook: left status+portrait / center chat / right inspire+toolbar.
 */
export const MOCKUP_REGIONS = {
  status: nr(0.02, 0.05, 0.175, 0.30),
  portrait: nr(0.02, 0.36, 0.175, 0.52),
  chat_paper: nr(0.205, 0.04, 0.555, 0.91),
  bubbles: nr(0.24, 0.18, 0.48, 0.52),
  avatars: nr(0.225, 0.20, 0.055, 0.12),
  input: nr(0.235, 0.82, 0.42, 0.09),
  send: nr(0.67, 0.83, 0.065, 0.075),
  inspire: nr(0.78, 0.05, 0.20, 0.58),
  toolbar: nr(0.78, 0.70, 0.20, 0.22),
};

/**
 * @param {{ TITLE: string, UI: string, SOCIAL: string, KEYART: string, BG: string, INTERVIEW_MOCKUP: string }} dirs
 */
export function defineRows(dirs) {
  const t = (name) => path.join(dirs.TITLE, name);
  const u = (name) => path.join(dirs.UI, name);
  const s = (name) => path.join(dirs.SOCIAL, name);
  const b = (name) => path.join(dirs.BG, name);
  const k = (name) => path.join(dirs.KEYART, name);
  const mock = dirs.INTERVIEW_MOCKUP;

  return [
    // —— 标题 / 主菜单 ——
    {
      id: 1, name: "标题·木桌全屏底", where: "标题/主菜单", status: "已有资源", urgency: "高",
      deliverable: "title_desk_bg.png", notes: "VnArt/Title；全屏最底层",
      callout: "全屏木桌底",
      elementLabel:
        "标题主菜单——整屏最底层的木桌桌面背景。杂志、胶带按钮、采访本等道具都叠在这张桌子上。只要桌面材质与透视，不要画杂志或按钮。",
      imageId: "01_title_desk_bg",
      gen: { kind: "asset", src: t("title_desk_bg.png") },
    },
    {
      id: 2, name: "标题·展开杂志本体", where: "标题/主菜单中央", status: "已有资源", urgency: "高",
      deliverable: "title_magazine_open.png", notes: "左右页纸面底板",
      callout: "中央展开杂志",
      elementLabel:
        "标题主菜单——屏幕正中间摊开的大本杂志。左页是品牌插画区，右页是目录按钮区；这里交付的是杂志纸面底板。不要把左页插画、Logo、胶带按钮画死在同一张成品里（它们是分图层）。",
      imageId: "02_title_magazine_open",
      gen: { kind: "asset", src: t("title_magazine_open.png") },
    },
    {
      id: 3, name: "标题·杂志阴影", where: "标题/主菜单", status: "已有资源", urgency: "中",
      deliverable: "title_magazine_shadow.png", notes: "半透明叠在杂志下",
      callout: "杂志投影",
      elementLabel:
        "标题主菜单——摊开杂志底下那一层淡淡的投影。略偏一点压在木桌上，让杂志「浮」起来。只要阴影，不要杂志内容。",
      imageId: "03_title_magazine_shadow",
      gen: { kind: "asset", src: t("title_magazine_shadow.png") },
    },
    {
      id: 4, name: "标题·左页插画", where: "标题杂志左页", status: "已有资源", urgency: "高",
      deliverable: "title_feature_art.png", notes: "品牌插画区",
      callout: "左页品牌插画",
      elementLabel:
        "标题杂志左页——中上部那块最大的品牌插画（猫/街角氛围）。不是顶部 Logo，也不是下面引语框。画完整插画即可，文字用程序叠。",
      imageId: "04_title_feature_art",
      gen: {
        kind: "asset-region",
        src: t("title_magazine_open.png"),
        region: nr(0.05, 0.35, 0.42, 0.5),
        fallbackSrc: t("title_feature_art.png"),
      },
    },
    {
      id: 5, name: "标题·中文 Logo", where: "标题品牌", status: "已有资源", urgency: "高",
      deliverable: "title_logo_cn.png", notes: "英文化时隐藏，改用字体",
      callout: "中文 Logo 条",
      elementLabel:
        "标题杂志左页——靠上的中文游戏名 Logo 图形条（街角专访）。不是插画，不是引语。英文界面会隐藏这张，改用字体。",
      imageId: "05_title_logo_cn",
      gen: { kind: "asset", src: t("title_logo_cn.png") },
    },
    {
      id: 6, name: "标题·英文 Logo 条", where: "标题品牌", status: "已有资源", urgency: "中",
      deliverable: "title_logo_en.png", notes: "VnArt/Title",
      callout: "英文 Logo 条",
      elementLabel:
        "标题杂志左页——中文 Logo 正下方那条英文副标题/Logo 条。小一号，贴在插画上方或下方的品牌区，不要画进插画本体。",
      imageId: "06_title_logo_en",
      gen: { kind: "asset", src: t("title_logo_en.png") },
    },
    {
      id: 7, name: "标题·左页引语框", where: "标题杂志左页", status: "已有资源", urgency: "中",
      deliverable: "title_quote_box_l.png", notes: "框内文案用字体+Loc，勿画字",
      callout: "左页引语外框",
      elementLabel:
        "标题杂志左页下部——像便签/引语卡一样的装饰外框。框里的句子用程序字体填，请不要把文案画死在图里。",
      imageId: "07_title_quote_box_l",
      gen: { kind: "asset", src: t("title_quote_box_l.png") },
    },
    {
      id: 8, name: "标题·右页目录页眉", where: "标题杂志右页", status: "已有资源", urgency: "中",
      deliverable: "title_contents_header.png", notes: "CONTENTS/目录用字体",
      callout: "右页目录页眉",
      elementLabel:
        "标题杂志右页顶部——「CONTENTS / 目录」那条装饰页眉线。标题字用字体，图只提供线条/胶带感装饰。",
      imageId: "08_title_contents_header",
      gen: { kind: "asset", src: t("title_contents_header.png") },
    },
    {
      id: 9, name: "标题·引语装饰", where: "标题杂志左页", status: "已有资源", urgency: "低",
      deliverable: "title_blurb_deco.png", notes: "可选装饰",
      callout: "引语旁小装饰",
      elementLabel:
        "标题杂志左页——引语框旁边的一小块贴纸/墨点装饰。可有可无的润色件，不要盖住主插画。",
      imageId: "09_title_blurb_deco",
      gen: { kind: "asset", src: t("title_blurb_deco.png") },
    },
    {
      id: 10, name: "标题·胶带主按钮底", where: "标题菜单主操作", status: "已有资源", urgency: "高",
      deliverable: "btn_tape_primary_idle.png / btn_tape_primary_hover.png", notes: "字用字体",
      callout: "主操作胶带钮底",
      elementLabel:
        "标题杂志右页——最醒目的那条「主操作」胶带按钮底板（常态/悬停）。按钮上的字用字体叠，图里不要写「新游戏」。",
      imageId: "10_btn_tape_primary",
      gen: { kind: "collage", srcs: [t("btn_tape_primary_idle.png"), t("btn_tape_primary_hover.png")] },
    },
    {
      id: 11, name: "标题·胶带次按钮底", where: "标题/笔记/写稿多处复用", status: "已有资源", urgency: "高",
      deliverable: "btn_tape_idle.png / hover / pressed", notes: "全游戏 scrapbook 钮底",
      callout: "次级胶带钮三态",
      elementLabel:
        "全游戏复用的次级胶带按钮底板（常态/悬停/按下）。标题菜单、笔记、写稿底栏都会用。只要胶带外形，不要写死按钮文字。",
      imageId: "11_btn_tape",
      gen: {
        kind: "collage",
        srcs: [t("btn_tape_idle.png"), t("btn_tape_hover.png"), t("btn_tape_pressed.png")],
      },
    },
    {
      id: 12, name: "标题·功能图标组", where: "标题按钮旁图标", status: "已有资源", urgency: "高",
      deliverable: "icon_play / cassette / doc / map / gear / exit", notes: "菜单旁小图标",
      callout: "菜单功能小图标",
      elementLabel:
        "标题菜单胶带按钮左侧那组小功能图标（开始/磁带/文档/地图/齿轮/退出）。是图标，不是整条按钮底板。",
      imageId: "12_title_icons",
      gen: {
        kind: "collage",
        srcs: ["icon_play", "icon_cassette", "icon_doc", "icon_map", "icon_gear", "icon_exit"].map(
          (n) => t(`${n}.png`)
        ),
      },
    },
    {
      id: 13, name: "标题·回形针装饰", where: "标题/笔记/采访/写稿", status: "已有资源", urgency: "中",
      deliverable: "deco_paperclip.png", notes: "多处复用",
      callout: "回形针装饰",
      elementLabel:
        "金属回形针小装饰贴图。标题、记者笔记、自由采访纸页角、写稿素材卡都会夹在纸角上。只要回形针，不要夹着纸的内容。",
      imageId: "13_deco_paperclip",
      gen: { kind: "asset", src: t("deco_paperclip.png") },
    },
    {
      id: 14, name: "标题桌面·采访本道具", where: "主菜单桌面；可点开笔记", status: "已有资源", urgency: "中",
      deliverable: "prop_field_notes.png", notes: "可点击进笔记",
      callout: "桌面采访本道具",
      elementLabel:
        "标题木桌左下/边缘——一本可点击的采访笔记本道具。点开会进记者笔记，不是中央那本杂志。",
      imageId: "14_prop_field_notes",
      gen: { kind: "asset", src: t("prop_field_notes.png") },
    },
    {
      id: 15, name: "标题桌面·翻译器等散件", where: "主菜单桌面装饰", status: "已有资源", urgency: "中",
      deliverable: "prop_translator / polaroid / scraps", notes: "装饰道具组",
      callout: "桌面装饰散件",
      elementLabel:
        "标题木桌上散落的翻译器、拍立得、纸屑等装饰道具。气氛件，不是中央杂志，也不要做成可点按钮。",
      imageId: "15_title_desk_props",
      gen: {
        kind: "collage",
        srcs: [t("prop_translator.png"), t("prop_polaroid_a.png"), t("prop_polaroid_b.png"), t("prop_scraps.png")],
      },
    },
    {
      id: 16, name: "标题品牌 KeyArt", where: "标题/品牌全屏", status: "已有资源", urgency: "中",
      deliverable: "kv_title_street_interview.png", notes: "VnArt/KeyArt；盘上已有 PNG",
      callout: "品牌全屏 KeyArt",
      elementLabel:
        "品牌用的全屏主视觉大图（街角专访 KeyArt）。现行主菜单仍以木桌+杂志拼贴为主；这张是品牌/宣传向全屏图，不是杂志分图层。",
      imageId: "16_kv_title_street_interview",
      gen: { kind: "asset", src: k("kv_title_street_interview.png") },
    },
    {
      id: 17, name: "过时标题文字图", where: "（旧方案，勿再交）", status: "过时勿交", urgency: "低",
      deliverable: "title_txt_* / title_btn_*", notes: "已改字体+Loc",
      callout: "过时预渲染文字（勿交）",
      elementLabel:
        "反例——旧方案把「新游戏」等字画死在图里。现行用字体+本地化，请勿再交付这类预渲染标题字/按钮字图。",
      imageId: "17_obsolete_title_txt",
      gen: {
        kind: "collage",
        srcs: [t("title_txt_contents.png"), t("title_txt_subtitle.png"), t("title_btn_01_newgame.png")],
      },
    },

    // —— VN 对白 / 通用 chrome ——
    {
      id: 18, name: "深色纸纹纹理", where: "对白盒/笔记/采访/写稿纸面", status: "已有资源", urgency: "高",
      deliverable: "tex_paper_dark.png", notes: "VnArt/UI；盘上已有 PNG",
      callout: "深色纸纹平铺",
      elementLabel:
        "可平铺的深色纸纹纹理。对白盒底板、记者笔记页、采访纸页、写稿纸面都会拿来当底纹。只要材质，不要画姓名牌或按钮。",
      imageId: "18_tex_paper_dark",
      gen: { kind: "asset", src: u("tex_paper_dark.png") },
    },
    {
      id: 19, name: "VN 对话框外框", where: "全流程对白", status: "程序色块", urgency: "中",
      deliverable: "ui_dialogue_frame.png（建议）", notes: "可选九宫格升级",
      callout: "对白盒+姓名牌外框",
      elementLabel:
        "普通剧情对白界面——屏幕下方那块最大的对白纸盒，左上角叠一小块姓名牌。只要外框/纸边，正文与姓名用字体。不要画成自由采访中间那张大聊天纸。",
      imageId: "19_ui_dialogue_frame",
      gen: { kind: "wire", screen: "dialogue", region: "frame" },
    },
    {
      id: 20, name: "选项条按钮底", where: "剧本选项 / 交谈 / 立意", status: "程序色块", urgency: "中",
      deliverable: "ui_choice_btn.png（建议）", notes: "可与胶带风统一",
      callout: "选项条底板",
      elementLabel:
        "对白界面右侧（或上方）竖排的选项条按钮底板。玩家点选剧情分支用。只要条形底板，不要把选项文字画死。",
      imageId: "20_ui_choice_btn",
      gen: { kind: "wire", screen: "dialogue", region: "choice" },
    },
    {
      id: 21, name: "顶栏 HUD", where: "全流程（标题屏隐藏）", status: "程序色块", urgency: "中",
      deliverable: "ui_topbar_chip.png（建议）", notes: "章节/目标/回看·菜单",
      callout: "顶部信息条",
      elementLabel:
        "绝大多数玩法界面——屏幕最上方一条细长信息栏：左边章节小标签、中间目标提示、右边「回看」「菜单」。不是上下黑边，也不是对白盒。",
      imageId: "21_ui_topbar",
      gen: { kind: "wire", screen: "dialogue", region: "topbar" },
    },
    {
      id: 22, name: "Letterbox 黑边", where: "对白/调查/采访等", status: "程序色块", urgency: "低",
      deliverable: "（无需图 / 或 ui_letterbox.png）", notes: "现程序绘制",
      callout: "上下黑边",
      elementLabel:
        "画面最上和最下一道宽黑边（电影感），内侧可有一条细琥珀线。不是顶栏内容本身；多数情况程序画即可。",
      imageId: "22_ui_letterbox",
      gen: { kind: "wire", screen: "dialogue", region: "letterbox" },
    },
    {
      id: 23, name: "场景名 Toast", where: "进场短暂提示", status: "程序色块", urgency: "低",
      deliverable: "（无需图）", notes: "字体即可",
      callout: "场景名提示条",
      elementLabel:
        "刚进场景时，顶栏下方中央短暂弹出的场景名小条（如「槐安社区」）。字体即可，一般不需要单独贴图。",
      imageId: "23_location_toast",
      gen: { kind: "wire", screen: "dialogue", region: "toast" },
    },
    {
      id: 24, name: "隐藏对白按钮", where: "对白/交谈等", status: "程序色块", urgency: "低",
      deliverable: "（无需图）", notes: "右下角字体钮",
      callout: "右下隐藏对白",
      elementLabel:
        "对白界面右下角很小的「隐藏对白」按钮，用来暂时收起对话框看背景。不是选项条，字体钮即可。",
      imageId: "24_hide_dialogue_btn",
      gen: { kind: "wire", screen: "dialogue", region: "hide" },
    },

    // —— 调查 / 交谈 ——
    {
      id: 25, name: "调查地图界面", where: "SC-04 调查地图", status: "已有资源", urgency: "高",
      deliverable: "bg_huaian_map.png", notes: "平面图；透明热点点选",
      callout: "调查全屏地图",
      elementLabel:
        "调查模式——整屏的槐安社区平面图背景。玩家点图上的透明热点调查物件。底栏动作钮和绿色「已调查」标记是另项，不要画进地图底图。",
      imageId: "25_bg_huaian_map",
      gen: { kind: "asset", src: b("bg_huaian_map.png") },
    },
    {
      id: 26, name: "调查热点·已完成标记", where: "地图已调查热点", status: "程序色块", urgency: "中",
      deliverable: "ui_hotspot_checked.png（建议）", notes: "现：绿色半透明区+角上✓；可升图",
      callout: "已调查绿框+✓",
      elementLabel:
        "调查地图上——已经查过的热点：整块点击区淡淡发绿，右上角再贴一个小绿底「✓」。未调查的几乎看不见。可交付角标小图标；不要画成黄色感叹号。",
      imageId: "26_ui_hotspot_checked",
      gen: { kind: "wire", screen: "investigate", region: "hotspot" },
    },
    {
      id: 27, name: "调查底栏动作芯片", where: "调查地图底栏", status: "程序色块", urgency: "中",
      deliverable: "ui_investigate_chip.png（建议）", notes: "交谈/等待/笔记/菜单",
      callout: "地图底栏动作钮",
      elementLabel:
        "调查地图屏幕最底下横排的小动作按钮（与保安交谈、等待大福、笔记、菜单等）。贴在地图下方，不要画进平面图里。",
      imageId: "27_ui_investigate_chip",
      gen: { kind: "wire", screen: "investigate", region: "chip" },
    },
    {
      id: 28, name: "交谈话题菜单", where: "保安交谈 / 核实", status: "程序色块", urgency: "中",
      deliverable: "（复用选项条）", notes: "对白 chrome + 选项",
      callout: "交谈话题列表",
      elementLabel:
        "与保安等交谈时——对白界面上列出可选话题的选项列表。样式复用普通选项条，不必单独新风格。",
      imageId: "28_talk_topics",
      gen: { kind: "wire", screen: "talk", region: "topics" },
    },

    // —— 自由采访（三栏）——
    {
      id: 29, name: "采访·左栏状态便签", where: "自由采访左上", status: "程序色块", urgency: "高",
      deliverable: "ui_interview_status_pad.png（建议）", notes: "信任/压力/专注三条",
      callout: "左上状态便签",
      elementLabel:
        "自由采访全屏——屏幕左边竖条的上半截。一张贴在夜景背景上的小便签纸，标题「受访者状态」，下面「信任/压力/专注」三条进度条（纸片+简单色条即可）。可用胶带/回形针固定。不要画成屏幕中间那张大聊天纸。",
      imageId: "29_interview_status_pad",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.status }
        : { kind: "wire", screen: "interview", region: "status" },
    },
    {
      id: 30, name: "采访·左栏立绘框+姓名条", where: "自由采访左下", status: "程序色块", urgency: "高",
      deliverable: "ui_interview_portrait_frame.png（建议）", notes: "立绘槽+姓名纸条；立绘另交",
      callout: "左栏立绘框+姓名",
      elementLabel:
        "自由采访全屏——左边竖条下半截。像拍立得/剪贴照片的竖框，里面放受访者半身立绘；框下方再贴一条写名字的小纸条。注意：小凌不要出现在左边全身立绘里；左边只显示当前受访者。立绘角色图另交，这里主要是相框纸边。",
      imageId: "30_interview_portrait_frame",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.portrait }
        : { kind: "wire", screen: "interview", region: "portrait" },
    },
    {
      id: 31, name: "采访·中央聊天大纸页", where: "自由采访正中", status: "程序色块", urgency: "高",
      deliverable: "ui_interview_chat_paper.png（建议）", notes: "最大纸页；标题用字体",
      callout: "中央聊天大纸",
      elementLabel:
        "自由采访全屏——屏幕正中间最大的一张竖向纸页。顶部居中写「自由采访」（字体），中间是可滚动聊天记录，底部是输入条。纸角可夹回形针。只要纸页底板与边角，不要把气泡字画死在纸上。",
      imageId: "31_interview_chat_paper",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.chat_paper }
        : { kind: "wire", screen: "interview", region: "chat_paper" },
    },
    {
      id: 32, name: "采访·聊天气泡底板", where: "中央纸页对话气泡", status: "程序色块", urgency: "高",
      deliverable: "ui_interview_bubble_npc.png / ui_interview_bubble_player.png（建议）",
      notes: "对方偏左灰白；我方偏右淡绿",
      callout: "左右聊天气泡",
      elementLabel:
        "自由采访中央纸页里——左右两侧的聊天气泡底板。对方消息靠左、偏灰白；玩家消息靠右、偏淡绿。只要气泡外形（可九点拉伸），气泡内文字用字体。不是整张大纸页，也不是圆形头像。",
      imageId: "32_interview_chat_bubbles",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.bubbles }
        : { kind: "wire", screen: "interview", region: "bubbles" },
    },
    {
      id: 33, name: "采访·对话头像圆框", where: "气泡旁圆形头像", status: "程序色块", urgency: "中",
      deliverable: "ui_interview_avatar_ring.png（建议）", notes: "圆遮罩框；头像切自立绘",
      callout: "气泡旁圆头像框",
      elementLabel:
        "自由采访聊天行——气泡旁边那个圆形小头像外框。对方在左气泡旁，玩家在右气泡旁。只要圆形遮罩/相框边缘；头像内容从立绘裁切，不要在框里画死某张脸。",
      imageId: "33_interview_avatars",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.avatars }
        : { kind: "wire", screen: "interview", region: "avatars" },
    },
    {
      id: 34, name: "采访·底部输入条", where: "中央纸页底部", status: "程序色块", urgency: "高",
      deliverable: "ui_interview_input_bar.png（建议）", notes: "占位「输入你的问题…」用字体",
      callout: "底部输入条",
      elementLabel:
        "自由采访中央大纸最底下——横向一条输入框，提示「输入你的问题…」。只要输入槽外形（浅底+细边），提示字用字体。右边发送钮是另项，不要画进输入条里。",
      imageId: "34_interview_input_bar",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.input }
        : { kind: "wire", screen: "interview", region: "input" },
    },
    {
      id: 35, name: "采访·发送按钮", where: "输入条右侧", status: "程序色块", urgency: "高",
      deliverable: "ui_interview_send_btn.png（建议）", notes: "纸飞机或「发」；深红小方钮",
      callout: "发送小方钮",
      elementLabel:
        "自由采访中央纸页右下——输入条右边那个深红/棕红小方按钮，上有纸飞机图标或「发」字。只画发送钮外形，不要画成旧版整条「结束采访」大按钮，也不要和右下工具栏搞混。",
      imageId: "35_interview_send_btn",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.send }
        : { kind: "wire", screen: "interview", region: "send" },
    },
    {
      id: 36, name: "采访·右栏提问灵感板", where: "自由采访右上", status: "程序色块", urgency: "高",
      deliverable: "ui_interview_inspire_pad.png（建议）", notes: "标题+最多3张灵感卡",
      callout: "右上提问灵感",
      elementLabel:
        "自由采访全屏——屏幕右边上半的竖向纸板，标题「提问灵感」。下面叠放最多三张可点的灵感小卡片（每张一行提示问法）。卡面文字用字体；不要画成旧版底部三枚横向提问芯片，也不要画玩家 AI 建议大面板。",
      imageId: "36_interview_inspire_pad",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.inspire }
        : { kind: "wire", screen: "interview", region: "inspire" },
    },
    {
      id: 37, name: "采访·右栏工具条", where: "自由采访右下", status: "程序色块", urgency: "高",
      deliverable: "ui_interview_toolbar.png（建议）", notes: "回顾/笔记/菜单等",
      callout: "右下工具纸片",
      elementLabel:
        "自由采访全屏——屏幕右下角一小条横纸，上面两到三个小按钮（回顾、笔记、菜单等），可夹回形针。这是工具入口，不是发送钮，也不是灵感卡。",
      imageId: "37_interview_toolbar",
      gen: existsMock(mock)
        ? { kind: "mockup", src: mock, region: MOCKUP_REGIONS.toolbar }
        : { kind: "wire", screen: "interview", region: "toolbar" },
    },

    // —— 笔记 ——
    {
      id: 38, name: "记者笔记桌面", where: "记者笔记全屏", status: "已有资源", urgency: "高",
      deliverable: "tex_paper_dark + deco_paperclip + btn_tape_*", notes: "深色桌+线纹页",
      callout: "笔记全屏桌面",
      elementLabel:
        "记者笔记界面——整屏像摊开笔记簿的深色桌面+线纹内页。左栏主题贴、右栏正文。回形针与胶带钮可复用已有资源；不要只交一张孤立便利贴。",
      imageId: "38_notebook_desk",
      gen: { kind: "wire", screen: "notebook", region: "desk" },
    },
    {
      id: 39, name: "笔记专用封面插画", where: "记者笔记", status: "文档标缺", urgency: "低",
      deliverable: "ui_notebook_cover.png（建议）", notes: "可后补",
      callout: "笔记封面装饰（可选）",
      elementLabel:
        "记者笔记可选的封面/页眉插画装饰区。现有胶带+回形针已够用；这是润色项，不是左栏便利贴网格。",
      imageId: "39_notebook_cover",
      gen: { kind: "wire", screen: "notebook", region: "cover" },
    },
    {
      id: 40, name: "笔记主题便利贴", where: "笔记左栏主题网格", status: "程序色块", urgency: "中",
      deliverable: "ui_notebook_sticky.png（建议）", notes: "可复用大福小图标",
      callout: "左栏主题便利贴",
      elementLabel:
        "记者笔记左边——一格格彩色便利贴主题卡片（点选切换笔记主题）。不是右侧大片正文页，也不要画成采访灵感卡。",
      imageId: "40_notebook_sticky",
      gen: { kind: "wire", screen: "notebook", region: "sticky" },
    },

    // —— 写稿 ——
    {
      id: 41, name: "写稿立意选择", where: "SC-10 写稿入口", status: "程序色块", urgency: "高",
      deliverable: "（复用对白+选项）", notes: "两大立意选项",
      callout: "写稿立意选项",
      elementLabel:
        "写稿开始前——沈禾说明后，对白界面上两大「立意」选项条。复用普通对白+选项即可，不必新面板。",
      imageId: "41_writing_direction",
      gen: { kind: "wire", screen: "writing_pick", region: "choices" },
    },
    {
      id: 42, name: "写稿素材软木板", where: "写稿素材库", status: "程序色块", urgency: "高",
      deliverable: "ui_corkboard.png（建议）", notes: "现程序软木色",
      callout: "素材库软木板底",
      elementLabel:
        "写稿选材界面——整屏软木板背景，上面钉着素材卡片。只要软木纹理底板，不要把单张素材内容画死在底图上。",
      imageId: "42_ui_corkboard",
      gen: { kind: "wire", screen: "corkboard", region: "board" },
    },
    {
      id: 43, name: "写稿素材卡面", where: "素材卡库网格/详情", status: "程序色块", urgency: "中",
      deliverable: "ui_material_card.png（建议）", notes: "编号/标签/锁定",
      callout: "单张素材卡",
      elementLabel:
        "软木板上钉着的单张素材小卡片（编号、标签、锁定态）。非整块软木背景；文字可用字体。",
      imageId: "43_ui_material_card",
      gen: { kind: "wire", screen: "corkboard", region: "card" },
    },
    {
      id: 44, name: "写稿台·可编辑成稿纸", where: "写稿台左侧大纸", status: "程序色块", urgency: "高",
      deliverable: "ui_writing_desk_paper.png（建议）", notes: "正文可编辑；无预览文章钮",
      callout: "写稿台成稿纸",
      elementLabel:
        "写稿台——深蓝桌面上左侧最大的报纸/稿纸，标题「槐安社区特稿」用字体，正文区域玩家可直接改字滚动编辑。底栏有「返回修改素材 / AI 优化 / 提交主编审核」。注意：没有「预览文章」按钮，也不要画单独预览叠层。",
      imageId: "44_writing_desk_paper",
      gen: { kind: "wire", screen: "writing_desk", region: "draft" },
    },
    {
      id: 45, name: "沈禾审核反馈屏", where: "提交主编后", status: "程序色块", urgency: "中",
      deliverable: "（无需专用 UI 图）", notes: "复用对白+办公室 BG",
      callout: "审稿复用对白盒",
      elementLabel:
        "交稿后听沈禾反馈——回到普通对白盒+办公室背景，没有单独「审核面板」要画。",
      imageId: "45_review_feedback",
      gen: { kind: "wire", screen: "dialogue", region: "frame" },
    },
    {
      id: 46, name: "重新采访菜单", where: "写稿补访", status: "程序色块", urgency: "中",
      deliverable: "（复用选项条）", notes: "补访人选选项",
      callout: "补访选项菜单",
      elementLabel:
        "写稿过程中要补访某人时——对白上的「重新采访谁」选项列表。复用选项条即可。",
      imageId: "46_reinterview_menu",
      gen: { kind: "wire", screen: "dialogue", region: "choice" },
    },

    // —— 社交 ——
    {
      id: 47, name: "社交手机外框", where: "SC-03 选题手机叠层", status: "程序色块", urgency: "中",
      deliverable: "ui_phone_frame.png（建议）", notes: "只要外壳；帖子另交",
      callout: "手机外壳（无帖）",
      elementLabel:
        "选题阶段——舞台中央竖着的手机外框（圆角、听筒、边框）。只要手机壳，不要画帖子内容；帖子是塞进屏幕里的另几张图。",
      imageId: "47_ui_phone_frame",
      gen: { kind: "wire", screen: "social", region: "phone" },
    },
    {
      id: 48, name: "社交帖·信息流 01", where: "SC-03 手机 feed", status: "已有资源", urgency: "高",
      deliverable: "social_post_01_feed.png", notes: "VnArt/UI/Social/",
      callout: "Feed 帖子 01",
      elementLabel:
        "手机屏幕信息流里的第 1 条帖子整卡内容图。塞进手机外框的屏幕区域；不要连手机壳一起画。",
      imageId: "48_social_post_01_feed",
      gen: { kind: "asset", src: s("social_post_01_feed.png") },
    },
    {
      id: 49, name: "社交帖·信息流 02", where: "SC-03 手机 feed", status: "已有资源", urgency: "高",
      deliverable: "social_post_02_feed.png", notes: "VnArt/UI/Social/",
      callout: "Feed 帖子 02",
      elementLabel:
        "手机屏幕信息流里的第 2 条帖子整卡内容图。只要帖子卡面，不要手机外框。",
      imageId: "49_social_post_02_feed",
      gen: { kind: "asset", src: s("social_post_02_feed.png") },
    },
    {
      id: 50, name: "社交帖·信息流 03", where: "SC-03 手机 feed", status: "已有资源", urgency: "高",
      deliverable: "social_post_03_feed.png", notes: "VnArt/UI/Social/",
      callout: "Feed 帖子 03",
      elementLabel:
        "手机屏幕信息流里的第 3 条帖子整卡内容图。详情放大页是另一张，不要和详情搞混。",
      imageId: "50_social_post_03_feed",
      gen: { kind: "asset", src: s("social_post_03_feed.png") },
    },
    {
      id: 51, name: "社交帖·详情 03", where: "SC-03 手机 detail", status: "已有资源", urgency: "高",
      deliverable: "social_post_03_detail.png", notes: "详情略放大",
      callout: "帖子 03 详情页",
      elementLabel:
        "点开第 3 条帖后的详情放大页内容图。比 feed 缩略更满屏；仍不要画手机壳。",
      imageId: "51_social_post_03_detail",
      gen: { kind: "asset", src: s("social_post_03_detail.png") },
    },

    // —— 后日谈 / 菜单 ——
    {
      id: 52, name: "后日谈·文章发布页", where: "后日谈开场", status: "占位复用背景", urgency: "中",
      deliverable: "bg_article_published.png（建议）", notes: "现占位槐安午后",
      callout: "文章发布页背景（缺）",
      elementLabel:
        "后日谈开头——全屏「文章已发布」网页/专栏感背景。现在暂时借用槐安午后图顶替，需要专用发布页；不要画对白盒。",
      imageId: "52_bg_article_published",
      gen: {
        kind: "asset-annotate-placeholder",
        src: b("bg_huaian_afternoon.png"),
        note: "现占位：槐安午后 → 需换专栏发布页",
      },
    },
    {
      id: 53, name: "章节结束按钮", where: "后日谈收束", status: "程序色块", urgency: "低",
      deliverable: "（字体即可）", notes: "「第一章 完」",
      callout: "第一章完按钮",
      elementLabel:
        "后日谈收束——屏幕下方「第一章 完」字体按钮。不要做成预渲染艺术字大图。",
      imageId: "53_chapter_end_btn",
      gen: { kind: "wire", screen: "epilogue", region: "endbtn" },
    },
    {
      id: 54, name: "暂停菜单面板", where: "暂停菜单", status: "程序色块", urgency: "中",
      deliverable: "ui_menu_panel.png（建议）", notes: "dim+中央纸板",
      callout: "暂停菜单纸板",
      elementLabel:
        "按菜单后——半透明遮罩中央的纸质暂停面板（继续/回看/存读档/笔记/设置/回标题）。不是设置页，也不是存档列表本身。",
      imageId: "54_ui_menu_panel",
      gen: { kind: "wire", screen: "menu", region: "panel" },
    },
    {
      id: 55, name: "对话回看面板", where: "回看历史", status: "程序色块", urgency: "中",
      deliverable: "ui_backlog_panel.png（建议）", notes: "大纸板+滚动",
      callout: "对话回看大纸",
      elementLabel:
        "回看历史对白时——几乎铺满的大纸板，上面可滚动的对白记录。比暂停菜单更大，不要画成设置页。",
      imageId: "55_ui_backlog_panel",
      gen: { kind: "wire", screen: "backlog", region: "panel" },
    },
    {
      id: 56, name: "存档/读档面板", where: "存读档", status: "程序色块", urgency: "中",
      deliverable: "ui_saveload_panel.png（建议）", notes: "槽位列表",
      callout: "存读档槽位板",
      elementLabel:
        "存档/读档——中央列出多个存档槽位的纸板。覆盖确认小窗是另项，不要画在一起。",
      imageId: "56_ui_saveload_panel",
      gen: { kind: "wire", screen: "saveload", region: "panel" },
    },
    {
      id: 57, name: "覆盖确认小面板", where: "覆盖存档确认", status: "程序色块", urgency: "低",
      deliverable: "ui_confirm_panel.png（建议）", notes: "确认/取消",
      callout: "覆盖确认小窗",
      elementLabel:
        "覆盖旧存档时弹出的小小确认窗（确认/取消）。比完整存档列表小很多。",
      imageId: "57_ui_confirm_panel",
      gen: { kind: "wire", screen: "confirm", region: "panel" },
    },
    {
      id: 58, name: "设置面板", where: "设置", status: "程序色块", urgency: "中",
      deliverable: "ui_settings_panel.png（建议）", notes: "语言/字体/音量等",
      callout: "设置整页纸板",
      elementLabel:
        "设置界面——语言、字体、音量、语速、全屏等整页纸板。齿轮只是入口图标（已有），这里要的是设置面板本身。",
      imageId: "58_ui_settings_panel",
      gen: { kind: "wire", screen: "settings", region: "panel" },
    },
    {
      id: 59, name: "Debug 跳转面板", where: "仅开发", status: "仅开发", urgency: "低",
      deliverable: "（无需美术）", notes: "F9；非正式",
      callout: "开发用跳转（无需图）",
      elementLabel:
        "仅开发人员用的关卡跳转面板（F9）。非正式玩家界面，无需美术交付。",
      imageId: "59_debug_jump_panel",
      gen: { kind: "wire", screen: "debug", region: "panel" },
    },
  ];
}

function existsMock(p) {
  return !!p && fs.existsSync(p);
}
