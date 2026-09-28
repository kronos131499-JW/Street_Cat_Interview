#!/usr/bin/env node
/**
 * Extract all in-game Chinese text needing English translation → HTML + PDF.
 * Usage: node Tools/export_chinese_loc_pdf.mjs
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { execSync } from "child_process";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "..");
const OUT_DIR = path.join(ROOT, "Docs", "loc");
const HTML_PATH = path.join(OUT_DIR, "Chinese_Text_For_Translation.html");
const PDF_REPO = path.join(OUT_DIR, "Chinese_Text_For_Translation.pdf");
const PDF_DOWNLOADS = path.join(
  process.env.USERPROFILE || "",
  "Downloads",
  "街角专访_待翻译中文.pdf"
);

const CJK = /[\u4e00-\u9fff\u3400-\u4dbf\uf900-\ufaff]/;

function readJson(rel) {
  const p = path.join(ROOT, rel);
  if (!fs.existsSync(p)) return null;
  return JSON.parse(fs.readFileSync(p, "utf8"));
}

function walkDir(dir, filter) {
  const out = [];
  if (!fs.existsSync(dir)) return out;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) {
      if (["Editor", "Library", "Temp", "obj", "bin"].includes(ent.name)) continue;
      out.push(...walkDir(full, filter));
    } else if (filter(full)) out.push(full);
  }
  return out;
}

function stripComments(code) {
  return code
    .replace(/\/\/[^\n]*/g, "")
    .replace(/\/\*[\s\S]*?\*\//g, "");
}

function extractCSharpStrings(filePath) {
  const rel = path.relative(ROOT, filePath).replace(/\\/g, "/");
  const raw = fs.readFileSync(filePath, "utf8");
  const code = stripComments(raw);
  const results = [];
  const re = /"(?:[^"\\]|\\.)*"/g;
  let m;
  while ((m = re.exec(code)) !== null) {
    const s = m[0].slice(1, -1).replace(/\\n/g, "\n").replace(/\\"/g, '"').replace(/\\\\/g, "\\");
    if (!CJK.test(s)) continue;
    const line = code.slice(0, m.index).split("\n").length;
    results.push({ file: rel, line, text: s });
  }
  return results;
}

function categorizeFile(file) {
  if (file.startsWith("Assets/Resources/Loc/")) return "UI 本地化";
  if (file.includes("BuiltInScripts.cs")) return "剧本对白";
  if (file.includes("Investigation/")) return "社区调查";
  if (file.includes("Interview/DafuRuleEngine") || file.includes("Interview/LinRuleEngine")) return "自由采访（规则台词）";
  if (file.includes("Interview/RuleBasedInterviewHintProvider")) return "采访提示/芯片";
  if (file.includes("Interview/InterviewController")) return "采访系统";
  if (file.includes("Interview/InterviewMaterialExtractor")) return "采访素材";
  if (file.includes("Notebook/")) return "记者笔记";
  if (file.includes("Writing/ArticleAssembler")) return "写稿成稿";
  if (file.includes("Writing/ArticleDraftAi") || file.includes("Writing/ArticleReviewAi")) return "写稿 AI";
  if (file.includes("Writing/RuleBasedWritingAiAssist")) return "写稿建议";
  if (file.includes("UI/GameUI")) return "界面（硬编码）";
  if (file.includes("Loc/ScriptLoc")) return "剧本/目标（ScriptLoc）";
  if (file.includes("Core/ChapterFlowController")) return "章节流程";
  if (file.includes("Core/GameState")) return "游戏状态";
  if (file.includes("Core/SaveSystem")) return "存档";
  if (file.includes("Investigation/InvestigateHotspot")) return "调查热点";
  if (file.includes("UI/TitleMenuLayout")) return "标题菜单布局";
  if (file.includes("UI/VnArt")) return "背景标签（内部）";
  if (file.includes("UI/BgmController") || file.includes("UI/SfxController")) return "音频标签（内部）";
  if (file.includes("Editor/")) return null; // skip editor
  if (file.includes("Interview/Llm") || file.includes("Writing/Llm")) return null; // LLM stubs mostly EN prompts
  return "其他代码";
}

function parseBuiltInScripts() {
  const file = path.join(ROOT, "Assets/Scripts/Narrative/BuiltInScripts.cs");
  const src = fs.readFileSync(file, "utf8");
  const entries = [];
  const sceneRe = /static ScriptScene (Sc\d+)\(\)\s*\{([\s\S]*?)\n\s*\}\s*(?=\n\s*static|\n\s*\})/g;
  const scNum = { Sc01: "SC-01", Sc02: "SC-02", Sc03: "SC-03", Sc04: "SC-04", Sc05: "SC-05", Sc06: "SC-06", Sc08: "SC-08", Sc09: "SC-09", Sc10: "SC-10" };

  let sm;
  while ((sm = sceneRe.exec(src)) !== null) {
    const fn = sm[1];
    const sceneId = scNum[fn] || fn;
    const body = sm[2];
    const titleM = body.match(/title\s*=\s*"([^"]+)"/);
    const sceneTitle = titleM ? titleM[1] : "";
    if (sceneTitle && CJK.test(sceneTitle)) {
      entries.push({ key: `title:${sceneId}`, category: "剧本对白", speaker: "", zh: sceneTitle, en: "", status: "待查" });
    }

    const adds = body.match(/s\.lines\.Add\([\s\S]*?\);/g) || [];
    adds.forEach((add, idx) => {
      const key = `${sceneId}:${idx}`;
      let zh = "";
      let speaker = "";

      const lM = add.match(/L\(\s*"([^"]+)"\s*,\s*"((?:[^"\\]|\\.)*)"/);
      const nM = add.match(/\bN\(\s*"((?:[^"\\]|\\.)*)"/);
      const innerM = add.match(/\bInner\(\s*"((?:[^"\\]|\\.)*)"/);
      const sysM = add.match(/\bSys\(\s*"((?:[^"\\]|\\.)*)"/);
      const socialSysM = add.match(/SocialSys\(\s*"((?:[^"\\]|\\.)*)"/);
      const bgmM = add.match(/Bgm\(\s*"((?:[^"\\]|\\.)*)"/);
      const sfxM = add.match(/Sfx\(\s*"((?:[^"\\]|\\.)*)"/);
      const textM = add.match(/text\s*=\s*"((?:[^"\\]|\\.)*)"/);
      const choiceM = add.match(/label\s*=\s*"((?:[^"\\]|\\.)*)"/);
      const objM = add.match(/setObjective\s*=\s*"((?:[^"\\]|\\.)*)"/);
      const spM = add.match(/speakerName\s*=\s*"((?:[^"\\]|\\.)*)"/);

      if (lM) { speaker = lM[1]; zh = lM[2]; }
      else if (innerM) { speaker = "小凌（内心）"; zh = innerM[1]; }
      else if (nM) { speaker = "旁白"; zh = nM[1]; }
      else if (sysM) { speaker = "系统"; zh = sysM[1]; }
      else if (socialSysM) { speaker = "系统"; zh = socialSysM[1]; }
      else if (bgmM) { speaker = "演出"; zh = `[BGM] ${bgmM[1]}`; }
      else if (sfxM) { speaker = "演出"; zh = `[SFX] ${sfxM[1]}`; }
      else if (textM && CJK.test(textM[1])) { zh = textM[1]; speaker = spM ? spM[1] : ""; }
      else if (choiceM) { speaker = "选项"; zh = choiceM[1]; }
      else if (objM) { speaker = "目标"; zh = objM[1]; }

      zh = zh.replace(/\\n/g, "\n").replace(/\\"/g, '"');
      if (!zh || !CJK.test(zh)) return;
      entries.push({ key, category: "剧本对白", speaker, zh, en: "", status: "待查" });
    });
  }
  return entries;
}

function loadUiEntries() {
  const zh = readJson("Assets/Resources/Loc/ui_zh.json");
  const en = readJson("Assets/Resources/Loc/ui_en.json");
  const enMap = new Map((en?.entries || []).map((e) => [e.key, e.value]));
  return (zh?.entries || []).map((e) => {
    const enVal = enMap.get(e.key) || "";
    const hasEn = enVal.length > 0;
    return {
      key: e.key,
      category: "UI 本地化",
      speaker: "",
      zh: e.value,
      en: enVal,
      status: hasEn ? "已有翻译" : "缺翻译",
    };
  });
}

function loadScriptEnMap() {
  const en = readJson("Assets/Resources/Loc/scripts_en.json");
  const map = new Map();
  for (const line of en?.lines || []) {
    map.set(line.key, { text: line.text || "", speaker: line.speakerName || "", choices: line.choices || [] });
  }
  return map;
}

function loadScriptLocObjectives() {
  const file = path.join(ROOT, "Assets/Scripts/Loc/ScriptLoc.cs");
  const src = fs.readFileSync(file, "utf8");
  const entries = [];
  const re = /\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}/g;
  let m;
  while ((m = re.exec(src)) !== null) {
    if (!CJK.test(m[1])) continue;
    entries.push({
      key: `objective:${m[1].slice(0, 20)}`,
      category: "剧本/目标（ScriptLoc）",
      speaker: "",
      zh: m[1],
      en: m[2],
      status: m[2] ? "已有翻译" : "缺翻译",
    });
  }
  return entries;
}

function dedupeEntries(list) {
  const seen = new Set();
  const out = [];
  for (const e of list) {
    const sig = `${e.category}|${e.key}|${e.zh}`;
    if (seen.has(sig)) continue;
    seen.add(sig);
    out.push(e);
  }
  return out;
}

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/\n/g, "<br>");
}

function buildHtml(allEntries, stats) {
  const byCat = {};
  for (const e of allEntries) {
    (byCat[e.category] ||= []).push(e);
  }
  const cats = Object.keys(byCat).sort();

  let body = "";
  for (const cat of cats) {
    const rows = byCat[cat];
    body += `<h2>${escapeHtml(cat)} <span class="count">(${rows.length})</span></h2>\n<table>\n`;
    body += `<thead><tr><th>#</th><th>Key / 来源</th><th>说话人</th><th>中文</th><th>英文（已有）</th><th>状态</th></tr></thead><tbody>\n`;
    rows.forEach((e, i) => {
      const statusClass = e.status === "缺翻译" ? "missing" : e.status === "硬编码" ? "hardcoded" : "ok";
      body += `<tr><td>${i + 1}</td><td class="key">${escapeHtml(e.key || e.file || "")}</td>`;
      body += `<td>${escapeHtml(e.speaker || "")}</td>`;
      body += `<td class="zh">${escapeHtml(e.zh)}</td>`;
      body += `<td class="en">${escapeHtml(e.en || "")}</td>`;
      body += `<td class="${statusClass}">${escapeHtml(e.status)}</td></tr>\n`;
    });
    body += "</tbody></table>\n";
  }

  return `<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<title>街角专访 — 待翻译中文文本</title>
<style>
  @page { margin: 18mm 14mm; size: A4; }
  body { font-family: "Microsoft YaHei", "SimHei", sans-serif; font-size: 10pt; color: #222; line-height: 1.45; }
  h1 { font-size: 18pt; border-bottom: 2px solid #333; padding-bottom: 8px; }
  h2 { font-size: 13pt; margin-top: 24px; color: #1a5276; page-break-after: avoid; }
  .count { font-size: 10pt; color: #666; font-weight: normal; }
  .meta { background: #f4f6f7; padding: 12px 16px; border-radius: 6px; margin: 16px 0; }
  table { width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 9pt; }
  th { background: #2c3e50; color: #fff; text-align: left; padding: 6px 8px; }
  td { border: 1px solid #ddd; padding: 5px 8px; vertical-align: top; }
  tr:nth-child(even) { background: #fafafa; }
  .key { font-family: Consolas, monospace; font-size: 8pt; color: #555; max-width: 140px; word-break: break-all; }
  .zh { min-width: 180px; }
  .en { color: #196f3d; min-width: 160px; }
  .missing { color: #c0392b; font-weight: bold; }
  .hardcoded { color: #d35400; font-weight: bold; }
  .ok { color: #27ae60; }
  .summary li { margin: 4px 0; }
</style>
</head>
<body>
<h1>《街角专访》待翻译中文文本清单</h1>
<div class="meta">
  <p><strong>生成时间：</strong>${new Date().toISOString().slice(0, 19).replace("T", " ")}</p>
  <p><strong>项目路径：</strong>${escapeHtml(ROOT)}</p>
  <p><strong>合计条目：</strong>${allEntries.length}</p>
  <ul class="summary">
    ${stats.map((s) => `<li><strong>${escapeHtml(s.cat)}</strong>：${s.total} 条（已有翻译 ${s.hasEn}，缺翻译 ${s.missing}，硬编码 ${s.hardcoded}）</li>`).join("\n    ")}
  </ul>
  <p><strong>说明：</strong>「已有翻译」指 ui_en.json / scripts_en.json / ScriptLoc 中已有对应英文。「硬编码」指 C# 源码中尚未接入本地化文件的玩家可见文本。</p>
</div>
${body}
</body>
</html>`;
}

function computeStats(entries) {
  const byCat = {};
  for (const e of entries) {
    const c = e.category;
    if (!byCat[c]) byCat[c] = { total: 0, hasEn: 0, missing: 0, hardcoded: 0 };
    byCat[c].total++;
    if (e.status === "已有翻译") byCat[c].hasEn++;
    else if (e.status === "硬编码") byCat[c].hardcoded++;
    else byCat[c].missing++;
  }
  return Object.entries(byCat).map(([cat, s]) => ({ cat, ...s }));
}

function tryChromePdf(htmlPath, pdfPath) {
  const browsers = [
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
  ];
  const url = "file:///" + htmlPath.replace(/\\/g, "/");
  for (const exe of browsers) {
    if (!fs.existsSync(exe)) continue;
    try {
      execSync(
        `"${exe}" --headless --disable-gpu --no-pdf-header-footer --print-to-pdf="${pdfPath}" "${url}"`,
        { stdio: "pipe", timeout: 120000 }
      );
      if (fs.existsSync(pdfPath) && fs.statSync(pdfPath).size > 1000) return true;
    } catch (_) { /* try next */ }
  }
  return false;
}

// ── Main ──
fs.mkdirSync(OUT_DIR, { recursive: true });

const scriptEn = loadScriptEnMap();
const all = [];

// 1. UI loc
all.push(...loadUiEntries());

// 2. Script dialogue
const scriptEntries = parseBuiltInScripts();
for (const e of scriptEntries) {
  const enLine = scriptEn.get(e.key);
  if (enLine) {
    e.en = enLine.text;
    if (enLine.speaker && !e.speaker) e.speaker = enLine.speaker;
    e.status = enLine.text ? "已有翻译" : "缺翻译";
  } else {
    e.status = "缺翻译";
  }
}
all.push(...scriptEntries);

// 3. ScriptLoc objectives
all.push(...loadScriptLocObjectives());

// 4. Hardcoded C# (exclude BuiltInScripts — already parsed; exclude InterviewController LLM prompts partially via file filter)
const csFiles = walkDir(path.join(ROOT, "Assets/Scripts"), (f) => f.endsWith(".cs"));
const skipFiles = new Set([
  "Assets/Scripts/Narrative/BuiltInScripts.cs",
  "Assets/Scripts/Loc/ScriptLoc.cs",
  "Assets/Scripts/Interview/InterviewController.cs", // mostly LLM system prompts
  "Assets/Scripts/Interview/LlmInterviewPortraitPickerStub.cs",
  "Assets/Scripts/Writing/LlmWritingAiAssistStub.cs",
  "Assets/Scripts/Interview/LlmClient.cs",
  "Assets/Scripts/Loc/TmpFontCatalog.cs",
  "Assets/Scripts/Loc/FontCatalog.cs",
]);

for (const file of csFiles) {
  const rel = path.relative(ROOT, file).replace(/\\/g, "/");
  if (skipFiles.has(rel)) continue;
  const cat = categorizeFile(rel);
  if (!cat || cat.includes("内部")) continue;

  for (const { line, text } of extractCSharpStrings(file)) {
    // Skip if already in ui_zh values
    all.push({
      key: `${rel}:${line}`,
      category: cat,
      speaker: "",
      zh: text,
      en: "",
      status: "硬编码",
      file: rel,
    });
  }
}

const deduped = dedupeEntries(all);
const stats = computeStats(deduped);
const html = buildHtml(deduped, stats);
fs.writeFileSync(HTML_PATH, html, "utf8");
console.log("HTML:", HTML_PATH);
console.log("Total entries:", deduped.length);
for (const s of stats) console.log(`  ${s.cat}: ${s.total}`);

let pdfOk = tryChromePdf(HTML_PATH, PDF_REPO);
if (pdfOk) {
  fs.copyFileSync(PDF_REPO, PDF_DOWNLOADS);
  console.log("PDF (repo):", PDF_REPO);
  console.log("PDF (Downloads):", PDF_DOWNLOADS);
} else {
  console.warn("PDF generation failed — open HTML in browser and Print to PDF.");
  console.warn("HTML path:", HTML_PATH);
}
