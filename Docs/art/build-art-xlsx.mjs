/**
 * Artist-friendly 美术需求清单.xlsx
 * Run: node build-art-xlsx.mjs
 */
import fs from "fs";
import path from "path";
import zlib from "zlib";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const outPath = path.join(__dirname, "美术需求清单_给画师.xlsx");

// Columns optimized for artists (not engineers)
const H = ["序号", "要画什么", "用在哪里", "建议尺寸", "现在有没有", "紧急程度", "交付文件名（英文）", "参考/说明"];

const sheets = [
  {
    name: "00_请先看这里",
    headers: ["说明"],
    rows: [
      ["《街角专访》第一章 · 美术需求（给画师版）"],
      [""],
      ["怎么读这张表"],
      ["1. 先看「01_还缺什么」——只列还要补的、要换新的，优先做。"],
      ["2. 再看「02～05」分类总表：立绘 / 场景背景 / 道具与调查 / 界面。"],
      ["3. 「现在有没有」：已有＝游戏里能用；还缺＝没有图；待换新＝有旧图但造型过时；可后补＝不挡流程。"],
      ["4. 「紧急程度」：马上要 / 这章该有 / 有空再画。"],
      ["5. 「交付文件名」：请按英文名出 PNG，透明底；我们会放进游戏资源目录。"],
      [""],
      ["画幅约定"],
      ["· 人物立绘：半身、透明底，建议 1024×1536（或同比例）。同一角色各表情：头顶、肩线对齐，换表情不跳身高。"],
      ["· 猫咪立绘：全身/前景姿态即可，不必做人型半身。"],
      ["· 场景背景：横版全屏 1920×1080。"],
      ["· 界面小图/图标：按现有胶带、回形针风格即可；文字一般用字体，不用再画按钮大字。"],
      [""],
      ["这章出场角色（不用加戏外人物）"],
      ["小凌（记者）、沈禾（主编）、大福（猫）、林女士、保安大叔；调查场景里有狸花猫（主要是场景图，不是对话立绘）。"],
      [""],
      ["不必再画的（已改成字体）"],
      ["标题上的「开始游戏」等按钮大字、左页长文案预渲染图——现在用程序字体显示，只保留胶带按钮底图和 Logo 图形。"],
    ],
  },
  {
    name: "01_还缺什么（优先）",
    headers: ["序号", "要画什么", "用在哪里", "建议尺寸", "紧急程度", "交付文件名", "现状", "说明"],
    rows: [
      ["1", "深色纸纹（可平铺）", "对话框、笔记本、采访纸、写稿纸的底纹", "平铺纹理，建议 ≥512×512", "马上要", "tex_paper_dark.png", "还缺", "现在只有空壳，游戏里会变成纯色块，观感差"],
      ["2", "标题/品牌全屏主视觉（KeyArt）", "标题相关全屏展示", "1920×1080", "马上要 / 这章该有", "kv_title_street_interview.png", "还缺", "品牌感强的街角采访主视觉"],
      ["3", "沈禾「淡淡认可」新造型表情", "写稿过审、最后一章认可感对白", "1024×1536 立绘", "这章该有", "ch_shenhe_amused.png", "待换新", "旧短发图已废弃；现在暂时用「平静」顶替，需要盘发+眼镜的新造型认可表情"],
      ["4", "后日谈「文章发布页」专用背景", "第一章后日谈：文章发出的画面", "1920×1080", "这章该有", "建议新文件，如 bg_article_published.png", "占位中", "现在暂用「槐安社区午后」，缺专栏/网页发布感"],
      ["5", "大福「常态」源文件命名对齐", "对白默认大福", "猫姿全身", "这章该有", "ch_dafu_default.png（源可叫 大福_常态）", "已有运行图", "游戏里已有图，请在源文件夹补明确「常态」命名，方便以后改图"],
      ["6", "狸花猫单独立绘（可选）", "若以后对白要单独叫「梨花」", "猫姿图", "有空再画", "ch_lihua_default.png", "还缺", "主流程已用场景三态 bg_cat_*，不急"],
      ["7", "电脑文档局部特写（可选）", "SC-01 电脑上看文档", "UI/文档特写", "有空再画", "协商命名", "可后补", "剧本有提到，目前用旁白，可不画"],
      ["8", "后日谈结尾静帧（可选）", "大福上快递柜 + 狸花出现", "1920×1080 或分层", "有空再画", "协商命名", "可后补", "可复用快递柜背景 + 猫"],
    ],
  },
  {
    name: "02_人物立绘",
    headers: H,
    rows: [
      ["1", "小凌 · 常态", "对白默认", "1024×1536", "已有", "马上要（已齐）", "ch_xiaoling_default", "源：小凌-常态"],
      ["2", "小凌 · 惊讶", "对白", "同上", "已有", "马上要（已齐）", "ch_xiaoling_surprised", "源：小凌-惊讶"],
      ["3", "小凌 · 思考", "对白 / 内心", "同上", "已有", "马上要（已齐）", "ch_xiaoling_thinking", "源：小凌-思考；常用"],
      ["4", "小凌 · 认真", "对白 / 采访感", "同上", "已有", "马上要（已齐）", "ch_xiaoling_serious", "源：小凌-认真"],
      ["5", "小凌 · 局促", "对白", "同上", "已有", "马上要（已齐）", "ch_xiaoling_worried", "源：小凌-局促"],
      ["6", "小凌 · 吐槽", "对白", "同上", "已有", "马上要（已齐）", "ch_xiaoling_smile", "源：小凌-吐槽"],
      ["7", "沈禾 · 平静", "对白默认；过审暂用", "1024×1536", "已有", "马上要（已齐）", "ch_shenhe_default", "新造型：盘发+眼镜"],
      ["8", "沈禾 · 无奈", "对白", "同上", "已有", "马上要（已齐）", "ch_shenhe_helpless", ""],
      ["9", "沈禾 · 认真", "对白 / 审稿", "同上", "已有", "马上要（已齐）", "ch_shenhe_serious", ""],
      ["10", "沈禾 · 淡淡认可", "写稿过审等", "同上", "待换新 / 还缺", "这章该有", "ch_shenhe_amused", "需新造型；勿交旧短发版"],
      ["11", "大福 · 默认/常态", "对白默认", "猫姿全身", "已有", "马上要（已齐）", "ch_dafu_default", "源文件夹建议补「大福_常态」名"],
      ["12", "大福 · 警惕", "对白 / 采访", "同上", "已有", "马上要（已齐）", "ch_dafu_wary", ""],
      ["13", "大福 · 不满", "对白 / 采访", "同上", "已有", "马上要（已齐）", "ch_dafu_annoyed", ""],
      ["14", "大福 · 回忆", "对白 / 采访", "同上", "已有", "马上要（已齐）", "ch_dafu_recall", ""],
      ["15", "大福 · 好奇", "对白；笔记小图标也会用", "同上", "已有", "马上要（已齐）", "ch_dafu_curious", ""],
      ["16", "大福 · 放松", "对白；写稿涂鸦也会用", "同上", "已有", "马上要（已齐）", "ch_dafu_relaxed", ""],
      ["17", "林女士 · 常态", "对白 / 自由采访", "1024×1536", "已有", "马上要（已齐）", "ch_lin_default", "采访时居中显示"],
      ["18", "林女士 · 压力", "对白 / 采访", "同上", "已有", "马上要（已齐）", "ch_lin_pressure", ""],
      ["19", "林女士 · 坚定", "对白 / 采访", "同上", "已有", "马上要（已齐）", "ch_lin_firm", ""],
      ["20", "林女士 · 疲惫", "对白 / 采访", "同上", "已有", "马上要（已齐）", "ch_lin_tired", ""],
      ["21", "林女士 · 防备", "对白 / 采访", "同上", "已有", "马上要（已齐）", "ch_lin_guarded", ""],
      ["22", "林女士 · 回忆", "对白 / 采访", "同上", "已有", "马上要（已齐）", "ch_lin_recall", ""],
      ["23", "保安大叔 · 常态", "对白", "1024×1536", "已有", "马上要（已齐）", "ch_guard_default", ""],
      ["24", "保安大叔 · 疑惑", "对白", "同上", "已有", "马上要（已齐）", "ch_guard_puzzled", ""],
      ["25", "保安大叔 · 苦笑", "对白", "同上", "已有", "马上要（已齐）", "ch_guard_wry", ""],
      ["26", "保安大叔 · 回忆", "对白", "同上", "已有", "马上要（已齐）", "ch_guard_recall", ""],
      ["27", "狸花猫 · 单独立绘", "仅当对白要单独叫名时", "猫姿", "还缺", "有空再画", "ch_lihua_default", "调查主要靠场景三态，可不画"],
    ],
  },
  {
    name: "03_场景背景",
    headers: H,
    rows: [
      ["1", "编辑部 · 傍晚", "SC-01 开场", "1920×1080", "已有", "马上要（已齐）", "bg_editorial_dusk", "源：编辑部_傍晚"],
      ["2", "沈禾办公室 · 傍晚", "SC-02", "同上", "已有", "马上要（已齐）", "bg_shenhe_office_dusk", ""],
      ["3", "编辑部工位 · 傍晚", "SC-03 刷社交/选题", "同上", "已有", "马上要（已齐）", "bg_editorial_desk_dusk", ""],
      ["4", "编辑部工位 · 上午", "SC-10 写稿", "同上", "已有", "马上要（已齐）", "bg_editorial_desk_morning", ""],
      ["5", "沈禾办公室 · 上午", "SC-10 审稿", "同上", "已有", "马上要（已齐）", "bg_shenhe_office_morning", ""],
      ["6", "槐安社区 · 午后", "SC-04；后日谈暂用", "同上", "已有", "马上要（已齐）", "bg_huaian_afternoon", "后日谈发布页在占位用它"],
      ["7", "槐安社区平面图", "调查地图选点", "同上", "已有", "马上要（已齐）", "bg_huaian_map", "图上要能辨认猫屋、贩卖机、长椅等位置"],
      ["8", "流浪猫投喂点", "调查：猫屋/碗", "同上", "已有", "马上要（已齐）", "bg_feeding_spot", ""],
      ["9", "投喂点告示牌特写", "调查：挂牌", "同上", "已有", "马上要（已齐）", "bg_feeding_sign", ""],
      ["10", "晒太阳的猫 · 放松", "调查狸花", "同上", "已有", "马上要（已齐）", "bg_cat_relax", "三态成套"],
      ["11", "晒太阳的猫 · 警惕", "调查狸花", "同上", "已有", "马上要（已齐）", "bg_cat_alert", ""],
      ["12", "晒太阳的猫 · 躲藏", "调查狸花", "同上", "已有", "马上要（已齐）", "bg_cat_hide", ""],
      ["13", "自动贩卖机", "调查", "同上", "已有", "马上要（已齐）", "bg_vending", ""],
      ["14", "木质长椅", "调查", "同上", "已有", "马上要（已齐）", "bg_bench", ""],
      ["15", "快递柜", "调查；后日谈可复用", "同上", "已有", "马上要（已齐）", "bg_locker", ""],
      ["16", "保安亭 · 午后", "调查/解锁", "同上", "已有", "马上要（已齐）", "bg_guard_afternoon", ""],
      ["17", "保安亭 · 傍晚", "采访大福等", "同上", "已有", "马上要（已齐）", "bg_guard_dusk", ""],
      ["18", "咖啡馆 · 午后", "采访林女士", "同上", "已有", "马上要（已齐）", "bg_cafe_afternoon", ""],
      ["19", "文章发布页（专栏/网页感）", "后日谈", "1920×1080", "占位中", "这章该有", "建议 bg_article_published", "别再用社区街景顶替"],
      ["20", "标题品牌 KeyArt", "标题全屏", "1920×1080", "还缺", "马上要 / 这章该有", "kv_title_street_interview", ""],
      ["21", "电脑文档特写", "SC-01（可选）", "特写", "可后补", "有空再画", "协商", ""],
    ],
  },
  {
    name: "04_道具与调查",
    headers: H,
    rows: [
      ["1", "喵语翻译器（关机）", "剧情赠送道具特写", "舞台中心约 560px 宽合适", "已有", "马上要（已齐）", "prop_translator_off", "舞台道具"],
      ["2", "标题桌面 · 翻译器", "主菜单桌面装饰", "桌面小道具", "已有", "这章该有（已齐）", "prop_translator", "标题杂志页"],
      ["3", "标题桌面 · 采访本", "主菜单；可点开笔记", "桌面小道具", "已有", "这章该有（已齐）", "prop_field_notes", ""],
      ["4", "标题桌面 · 拍立得 A", "主菜单装饰", "同上", "已有", "这章该有（已齐）", "prop_polaroid_a", ""],
      ["5", "标题桌面 · 拍立得 B", "主菜单装饰", "同上", "已有", "这章该有（已齐）", "prop_polaroid_b", ""],
      ["6", "标题桌面 · 散页", "主菜单装饰", "同上", "已有", "这章该有（已齐）", "prop_scraps", ""],
      ["7", "调查点击区", "地图上的可点位置", "不用单独出图", "已有（靠背景）", "—", "（无）", "猫屋/碗/挂牌/狸花/贩卖机/长椅/快递柜/保安亭 = 透明点击，美术保证平面图能对上即可"],
    ],
  },
  {
    name: "05_界面UI",
    headers: H,
    rows: [
      ["1", "木桌全屏底", "主菜单", "1920×1080", "已有", "马上要（已齐）", "title_desk_bg", ""],
      ["2", "展开的空白杂志", "主菜单", "按现有构图", "已有", "马上要（已齐）", "title_magazine_open", ""],
      ["3", "杂志阴影", "主菜单", "半透明", "已有", "这章该有（已齐）", "title_magazine_shadow", ""],
      ["4", "左页插画", "主菜单左页", "按现有", "已有", "马上要（已齐）", "title_feature_art", ""],
      ["5", "中文 Logo 图形", "主菜单品牌", "按现有", "已有", "马上要（已齐）", "title_logo_cn", "英文版会隐藏，改用字体"],
      ["6", "英文 Logo 条", "主菜单", "按现有", "已有", "这章该有（已齐）", "title_logo_en", ""],
      ["7", "左页引语框", "主菜单", "按现有", "已有", "这章该有（已齐）", "title_quote_box_l", "框内文字用字体，不用画字"],
      ["8", "右页目录页眉线", "主菜单", "按现有", "已有", "这章该有（已齐）", "title_contents_header", "「CONTENTS」用字体"],
      ["9", "胶带主按钮底（普通/悬停）", "主菜单按钮", "按现有胶带风", "已有", "马上要（已齐）", "btn_tape_primary_*", "按钮上的字用字体+小图标"],
      ["10", "胶带次按钮底", "主菜单", "同上", "已有", "马上要（已齐）", "btn_tape_*", ""],
      ["11", "回形针装饰", "主菜单/笔记/采访/写稿", "小装饰", "已有", "这章该有（已齐）", "deco_paperclip", "多处复用"],
      ["12", "功能小图标×6", "开始/继续/读档/清档/设置/退出", "小图标", "已有", "马上要（已齐）", "icon_play 等", "含 icon_gear 设置"],
      ["13", "深色纸纹", "对话框·笔记·采访·写稿", "可平铺", "还缺", "马上要", "tex_paper_dark", "全游戏纸质感关键"],
      ["14", "预渲染标题大字/按钮字", "（旧方案）", "—", "不用了", "—", "title_txt_* / title_btn_*", "请勿再当作要交付的文案图"],
      ["15", "对话框外框/选项框升级", "对白UI", "九宫格可选", "程序色块即可", "有空再画", "协商", "不挡流程"],
      ["16", "笔记专用封面插画", "笔记本界面", "—", "可后补", "有空再画", "协商", "现有胶带+回形针够用"],
      ["17", "写稿软木板专用贴图", "写稿桌", "—", "可后补", "有空再画", "协商", ""],
      ["18", "采访信赖条专用图", "自由采访", "—", "程序色块即可", "有空再画", "协商", ""],
    ],
  },
  {
    name: "06_后日谈与片尾",
    headers: H,
    rows: [
      ["1", "文章发布页背景", "后日谈开场画面", "1920×1080", "占位中", "这章该有", "建议新文件名", "专栏/网页发出感；正文仍是对话框旁白"],
      ["2", "结尾：大福上快递柜+狸花", "后日谈收束", "静帧或分层", "可后补", "有空再画", "协商", "可拼快递柜BG+猫"],
      ["3", "「第一章 完」", "片尾按钮", "无需图", "无需图", "—", "（字体）", "不要画成图片字"],
      ["4", "转场花字", "场景切换", "—", "不需要", "—", "—", "现有淡入淡出即可"],
    ],
  },
  {
    name: "07_交图与文件夹",
    headers: ["你从哪交源图", "我们放进游戏的位置", "备注"],
    rows: [
      ["人物立绘源文件夹（中文名也可）", "VnArt/Characters/ 下的 ch_角色_表情.png", "透明底 PNG"],
      ["正式背景图", "VnArt/Backgrounds/ 下的 bg_….png", "1920×1080"],
      ["标题杂志素材", "VnArt/Title/", "桌面、杂志、胶带、图标"],
      ["翻译器舞台图", "VnArt/Props/prop_translator_off.png", ""],
      ["品牌 KeyArt", "VnArt/KeyArt/kv_….png", "还缺，请补"],
      ["纸纹等通用UI", "VnArt/UI/tex_….png", "纸纹还缺"],
      ["字体", "不用交图", "游戏内用字体显示按钮字和标题文案"],
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
console.log("Wrote", outPath);
