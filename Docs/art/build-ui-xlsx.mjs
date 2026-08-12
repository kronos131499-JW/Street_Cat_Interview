/**
 * UI 界面需求清单.xlsx — one row per UI scene / art deliverable surface.
 * Run: node build-ui-xlsx.mjs
 *
 * Cross-checked against GameUI* catalog, Docs/art/美术需求清单.md,
 * Assets/Resources/VnArt/UI|Title, Assets/Art/UI (2026-08-11 repo scan).
 */
import fs from "fs";
import path from "path";
import zlib from "zlib";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const outPath = path.join(__dirname, "UI界面需求清单.xlsx");

const H = ["序号", "场景名称", "用在哪里", "当前状态", "紧急程度", "交付文件名（英文）", "参考/说明"];

// Status vocabulary (accurate to current repo — do not invent shipped art):
// 已有资源 / 已有占位 / 缺图回退纯色 / 程序色块 / 仅开发 / 文档标缺 / 过时勿交 / 占位复用背景

const rows = [
  // —— 标题 / 主菜单 ——
  ["1", "标题·木桌全屏底", "标题/主菜单（Mode.Title）", "已有资源", "高", "title_desk_bg.png", "VnArt/Title；ShowTitle 全屏桌面"],
  ["2", "标题·展开杂志本体", "标题/主菜单中央", "已有资源", "高", "title_magazine_open.png", "左品牌页+右目录页底板"],
  ["3", "标题·杂志阴影", "标题/主菜单", "已有资源", "中", "title_magazine_shadow.png", "半透明叠在杂志下"],
  ["4", "标题·左页插画", "标题杂志左页", "已有资源", "高", "title_feature_art.png", "品牌插画区"],
  ["5", "标题·中文 Logo", "标题品牌", "已有资源", "高", "title_logo_cn.png", "英文化时隐藏，改用字体「街角专访」"],
  ["6", "标题·英文 Logo 条", "标题品牌", "已有资源", "中", "title_logo_en.png", "VnArt/Title"],
  ["7", "标题·左页引语框", "标题杂志左页", "已有资源", "中", "title_quote_box_l.png", "框内文案用字体+Loc，勿画字"],
  ["8", "标题·右页目录页眉", "标题杂志右页", "已有资源", "中", "title_contents_header.png", "「CONTENTS/目录」用字体"],
  ["9", "标题·引语装饰", "标题杂志左页", "已有资源", "低", "title_blurb_deco.png", "可选装饰"],
  ["10", "标题·胶带主按钮底", "标题菜单主操作", "已有资源", "高", "btn_tape_primary_idle.png / btn_tape_primary_hover.png", "pressed 复用 hover；按钮字用字体"],
  ["11", "标题·胶带次按钮底", "标题/笔记/写稿多处复用", "已有资源", "高", "btn_tape_idle.png / btn_tape_hover.png / btn_tape_pressed.png", "全游戏 scrapbook 风按钮底"],
  ["12", "标题·功能图标组", "标题按钮旁图标", "已有资源", "高", "icon_play / icon_cassette / icon_doc / icon_map / icon_gear / icon_exit", "新游戏/继续/读档/清档/设置/退出"],
  ["13", "标题·回形针装饰", "标题/笔记/采访/写稿", "已有资源", "中", "deco_paperclip.png", "多处复用"],
  ["14", "标题桌面·采访本道具", "主菜单桌面；可点开笔记", "已有资源", "中", "prop_field_notes.png", "VnArt/Title 桌面道具"],
  ["15", "标题桌面·翻译器等散件", "主菜单桌面装饰", "已有资源", "中", "prop_translator / prop_polaroid_a / prop_polaroid_b / prop_scraps", "装饰道具组"],
  ["16", "标题品牌 KeyArt", "标题/品牌全屏解析目标", "已有资源", "中", "kv_title_street_interview.png", "VnArt/KeyArt；现行主菜单仍以杂志拼贴为主，KeyArt 已入库"],
  ["17", "过时标题文字图", "（旧方案，勿再交）", "过时勿交", "低", "title_txt_* / title_btn_*", "已改字体+Loc；Art 仍残留 PNG"],

  // —— VN 对白 / 通用 chrome ——
  ["18", "深色纸纹纹理", "对白盒/笔记/采访便签/写稿纸面", "已有资源", "高", "tex_paper_dark.png", "VnArt/UI；GameUI/Interview/Notebook/Writing GetUi；缺时回退纯色（现已有 PNG）"],
  ["19", "VN 对话框外框", "全流程对白（Mode.Dialogue）", "程序色块", "中", "ui_dialogue_frame.png（建议）", "DialogueBox+NamePlate；VnTheme 色块，可选九宫格升级"],
  ["20", "选项条按钮底", "剧本 choices / 交谈 / 立意", "程序色块", "中", "ui_choice_btn.png（建议）", "ChoiceHost；现色块按钮，可胶带风统一"],
  ["21", "顶栏 HUD（TopBar）", "全流程（标题屏隐藏）", "程序色块", "中", "ui_topbar_chip.png（建议）", "章节 chip / 目标行 / 回看·菜单；letterbox 带内"],
  ["22", "Letterbox 黑边", "对白/调查/采访等舞台感", "程序色块", "低", "（无需图 / 或 ui_letterbox.png）", "上下黑边+琥珀细线；现程序绘制"],
  ["23", "场景名 Toast", "进场短暂提示", "程序色块", "低", "（无需图）", "Location toast；字体即可"],
  ["24", "隐藏对白按钮", "对白/交谈等", "程序色块", "低", "（无需图）", "右下角；字体按钮"],

  // —— 调查 / 交谈 ——
  ["25", "调查地图界面", "SC-04 调查（Mode.Investigate）", "已有资源", "高", "bg_huaian_map.png", "平面图 BG；透明热点点选；左上调查条/底栏芯片为程序 UI"],
  ["26", "调查热点角标（可选）", "地图已调查状态", "文档标缺", "低", "ui_hotspot_checked.png（建议）", "现仅透明点击；美术清单标 P2 可选"],
  ["27", "调查底栏动作芯片", "调查地图底栏", "程序色块", "中", "ui_investigate_chip.png（建议）", "与保安交谈/等待大福/笔记/菜单等"],
  ["28", "交谈话题菜单", "保安交谈 / 后采访核实（Mode.Talk）", "程序色块", "中", "（复用选项条）", "ShowTalkMenu；对白 chrome + AddChoice"],

  // —— 自由采访 ——
  ["29", "采访便签本底板", "自由采访（Mode.Interview）", "已有资源", "高", "tex_paper_dark.png（复用）", "InterviewOverlay 底部便签本；缺纹则纯色"],
  ["30", "采访信赖/压力条", "采访右上信任便签", "程序色块", "中", "ui_interview_trust_meter.png（建议）", "五段信任/压力/专注；文档标可后补专用图"],
  ["31", "采访提问芯片", "采访底部芯片区", "程序色块", "中", "ui_interview_chip.png（建议）", "最多 3 枚建议问法"],
  ["32", "采访发送/结束按钮", "采访动作行", "程序色块", "中", "（复用胶带钮或色块）", "发送、结束采访、返回写稿"],
  ["33", "采访伴宠立绘槽", "采访左下伴宠", "已有资源", "高", "（立绘 ch_*，非 UI）", "CompanionPortrait；UI 槽位程序布局"],

  // —— 笔记 ——
  ["34", "记者笔记桌面", "NotebookOverlay 全屏", "已有资源", "高", "tex_paper_dark.png + deco_paperclip + btn_tape_*", "深色桌面+线纹页；现组合已有资源"],
  ["35", "笔记专用封面插画", "记者笔记", "文档标缺", "低", "ui_notebook_cover.png（建议）", "美术清单：可后补；现胶带+回形针够用"],
  ["36", "笔记主题便利贴", "笔记左栏主题网格", "程序色块", "中", "ui_notebook_sticky.png（建议）", "现色块贴；可复用大福小图标作贴纸"],

  // —— 写稿 ——
  ["37", "写稿立意选择", "SC-10 写稿入口", "程序色块", "高", "（复用对白+选项）", "ShowWritingDirectionPick；沈禾说明+两大立意选项"],
  ["38", "写稿素材软木板", "WritingMaterialsOverlay", "程序色块", "高", "ui_corkboard.png（建议）", "文档标缺专用贴图；现程序软木色+素材卡"],
  ["39", "写稿素材卡面", "素材卡库网格/详情", "程序色块", "中", "ui_material_card.png（建议）", "编号/标签/锁定态；胶带+回形针装饰已有"],
  ["40", "写稿台·报纸成稿", "WritingDeskOverlay", "程序色块", "高", "ui_writing_desk_paper.png（建议）", "深蓝桌+大稿纸；栏头「槐安社区特稿」用字体"],
  ["41", "文章预览叠层", "素材板「预览文章」", "程序色块", "中", "ui_article_preview_sheet.png（建议）", "ArticlePreview；半透明遮罩+中央纸页"],
  ["42", "沈禾审核反馈屏", "提交主编后（Writing 对白态）", "程序色块", "中", "（无需专用 UI 图）", "复用对白盒；办公室 BG 已有；无独立审核面板"],
  ["43", "重新采访菜单", "写稿补访子流程", "程序色块", "中", "（复用选项条）", "ShowReInterviewMenu"],

  // —— 社交 ——
  ["44", "社交手机叠层框", "SC-03 选题（SocialOverlay）", "程序色块", "中", "ui_phone_frame.png（建议）", "现为居中矩形层承载贴图；无独立手机壳图"],
  ["45", "社交帖·信息流 01", "SC-03 手机 feed", "已有资源", "高", "social_post_01_feed.png", "VnArt/UI/Social/"],
  ["46", "社交帖·信息流 02", "SC-03 手机 feed", "已有资源", "高", "social_post_02_feed.png", "VnArt/UI/Social/"],
  ["47", "社交帖·信息流 03", "SC-03 手机 feed", "已有资源", "高", "social_post_03_feed.png", "VnArt/UI/Social/"],
  ["48", "社交帖·详情 03", "SC-03 手机 detail", "已有资源", "高", "social_post_03_detail.png", "详情略放大展示"],

  // —— 后日谈 / 菜单 overlays ——
  ["49", "后日谈·文章发布页", "Mode.Epilogue 开场", "占位复用背景", "中", "bg_article_published.png（建议）", "现占位 bg_huaian_afternoon；专栏/网页感；对白 chrome"],
  ["50", "章节结束按钮", "后日谈收束", "程序色块", "低", "（字体即可）", "「第一章 完」；勿画成图片字"],
  ["51", "暂停菜单面板", "MenuOverlay", "程序色块", "中", "ui_menu_panel.png（建议）", "dim+纸质中央板；继续/回看/存读档/笔记/设置/回标题"],
  ["52", "对话回看面板", "BacklogOverlay", "程序色块", "中", "ui_backlog_panel.png（建议）", "大纸板+滚动历史；可复用纸纹"],
  ["53", "存档/读档面板", "SaveLoadOverlay", "程序色块", "中", "ui_saveload_panel.png（建议）", "槽位列表；标题「读档」亦可从主菜单进"],
  ["54", "覆盖确认小面板", "ConfirmOverlay", "程序色块", "低", "ui_confirm_panel.png（建议）", "覆盖存档确认/取消"],
  ["55", "设置面板", "SettingsOverlay", "程序色块", "中", "ui_settings_panel.png（建议）", "语言/字体/音量/语速/全屏等；入口 icon_gear 已有"],
  ["56", "Debug 跳转面板", "仅 Editor / Development", "仅开发", "低", "（无需美术）", "DebugJumpPanel F9；非正式玩家界面"],
];

const sheets = [
  {
    name: "UI界面需求",
    headers: H,
    rows,
  },
  {
    name: "说明",
    headers: ["说明"],
    rows: [
      ["《街角专访》第一章 · UI 界面需求清单"],
      [""],
      ["列含义"],
      ["序号：交付项编号"],
      ["场景名称：玩家可见界面或可交付美术面"],
      ["用在哪里：流程/Mode/overlay"],
      ["当前状态：已有资源 / 程序色块 / 占位复用背景 / 文档标缺 / 过时勿交 / 仅开发"],
      ["紧急程度：高=核心玩法环；中=体验与 overlay 打磨；低=可选/开发/过时"],
      ["交付文件名（英文）：仓库已有名优先；缺图给建议名"],
      [""],
      ["依据"],
      ["GameUI 界面目录（Title/Dialogue/Investigate/Talk/Interview/Notebook/Writing/Social/Epilogue/Overlays）"],
      ["Docs/art/美术需求清单.md §4–5"],
      ["Assets/Resources/VnArt/UI、Title、KeyArt；Assets/Art/UI 实勘（含 social_post_*、tex_paper_dark、kv_title）"],
      [""],
      ["注意"],
      ["【画师主表】请以 Docs/art/美术需求清单_给画师.xlsx 为准（含 01_UI界面 + 02_场景背景 + 嵌入参考图）。本 xlsx 为无图摘要，可能滞后。"],
      ["自由采访已改为三栏：左状态+立绘 / 中聊天+输入 / 右灵感+工具栏；无预览文章按钮；写稿台正文可编辑。"],
      ["tex_paper_dark / kv_title_street_interview 盘上已有 PNG。后日谈「文章发布页面」仍占位 bg_huaian_afternoon。"],
    ],
  },
];

function esc(s) {
  return String(s ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function colName(n) {
  let s = "";
  while (n > 0) {
    n--;
    s = String.fromCharCode(65 + (n % 26)) + s;
    n = Math.floor(n / 26);
  }
  return s;
}

function sheetXml(headers, rows) {
  const all = [headers, ...rows];
  let body = "";
  for (let ri = 0; ri < all.length; ri++) {
    const rowNum = ri + 1;
    const row = all[ri];
    let cells = "";
    for (let ci = 0; ci < row.length; ci++) {
      const ref = colName(ci + 1) + rowNum;
      const style = ri === 0 ? ' s="1"' : "";
      cells += `<c r="${ref}" t="inlineStr"${style}><is><t>${esc(row[ci])}</t></is></c>`;
    }
    body += `<row r="${rowNum}">${cells}</row>`;
  }
  return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
<sheetData>${body}</sheetData>
</worksheet>`;
}

const crcTable = (() => {
  const t = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    t[n] = c >>> 0;
  }
  return t;
})();
function crc32(buf) {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) c = crcTable[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}
function u16(n) {
  const b = Buffer.alloc(2);
  b.writeUInt16LE(n, 0);
  return b;
}
function u32(n) {
  const b = Buffer.alloc(4);
  b.writeUInt32LE(n >>> 0, 0);
  return b;
}
function zipStore(entries) {
  const parts = [];
  const central = [];
  let offset = 0;
  for (const e of entries) {
    const nameBuf = Buffer.from(e.name, "utf8");
    const data = e.data;
    const compressed = zlib.deflateRawSync(data);
    const useStore = compressed.length >= data.length;
    const payload = useStore ? data : compressed;
    const method = useStore ? 0 : 8;
    const crc = crc32(data);
    const local = Buffer.concat([
      Buffer.from([0x50, 0x4b, 0x03, 0x04]),
      u16(20),
      u16(0x800),
      u16(method),
      u16(0),
      u16(0),
      u32(crc),
      u32(payload.length),
      u32(data.length),
      u16(nameBuf.length),
      u16(0),
      nameBuf,
      payload,
    ]);
    parts.push(local);
    central.push(
      Buffer.concat([
        Buffer.from([0x50, 0x4b, 0x01, 0x02]),
        u16(20),
        u16(20),
        u16(0x800),
        u16(method),
        u16(0),
        u16(0),
        u32(crc),
        u32(payload.length),
        u32(data.length),
        u16(nameBuf.length),
        u16(0),
        u16(0),
        u16(0),
        u16(0),
        u32(0),
        u32(offset),
        nameBuf,
      ])
    );
    offset += local.length;
  }
  const centralBuf = Buffer.concat(central);
  const end = Buffer.concat([
    Buffer.from([0x50, 0x4b, 0x05, 0x06]),
    u16(0),
    u16(0),
    u16(entries.length),
    u16(entries.length),
    u32(centralBuf.length),
    u32(offset),
    u16(0),
  ]);
  return Buffer.concat([...parts, centralBuf, end]);
}

const files = [];
const sheetOverrides = [];
const wbSheets = [];
const wbRels = [];

sheets.forEach((s, idx) => {
  const i = idx + 1;
  const fname = `sheet${i}.xml`;
  files.push({ name: `xl/worksheets/${fname}`, data: Buffer.from(sheetXml(s.headers, s.rows), "utf8") });
  wbSheets.push(`<sheet name="${esc(s.name)}" sheetId="${i}" r:id="rId${i}"/>`);
  wbRels.push(
    `<Relationship Id="rId${i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/${fname}"/>`
  );
  sheetOverrides.push(
    `<Override PartName="/xl/worksheets/${fname}" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>`
  );
});
wbRels.push(
  `<Relationship Id="rIdStyles" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>`
);

files.push({
  name: "xl/workbook.xml",
  data: Buffer.from(
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>${wbSheets.join("")}</sheets>
</workbook>`,
    "utf8"
  ),
});
files.push({
  name: "xl/_rels/workbook.xml.rels",
  data: Buffer.from(
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">${wbRels.join("")}</Relationships>`,
    "utf8"
  ),
});
files.push({
  name: "xl/styles.xml",
  data: Buffer.from(
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <fonts count="2">
    <font><sz val="11"/><name val="Microsoft YaHei"/></font>
    <font><b/><sz val="11"/><name val="Microsoft YaHei"/></font>
  </fonts>
  <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
  <borders count="1"><border/></borders>
  <cellStyleXfs count="1"><xf/></cellStyleXfs>
  <cellXfs count="2">
    <xf fontId="0" fillId="0" borderId="0"/>
    <xf fontId="1" fillId="0" borderId="0" applyFont="1"/>
  </cellXfs>
</styleSheet>`,
    "utf8"
  ),
});
files.push({
  name: "_rels/.rels",
  data: Buffer.from(
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>`,
    "utf8"
  ),
});
files.push({
  name: "[Content_Types].xml",
  data: Buffer.from(
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
  ${sheetOverrides.join("")}
</Types>`,
    "utf8"
  ),
});

fs.writeFileSync(outPath, zipStore(files));

const urgency = { 高: 0, 中: 0, 低: 0 };
for (const r of rows) urgency[r[4]] = (urgency[r[4]] || 0) + 1;
console.log("Wrote", outPath);
console.log("Rows:", rows.length);
console.log("Urgency:", JSON.stringify(urgency));
