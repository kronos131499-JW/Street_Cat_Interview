#!/usr/bin/env node
/**
 * Extract translations from Street_Cat_Interview_English_Localization.pdf
 * and apply to ui_en.json / scripts_en.json.
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { PDFParse } from "pdf-parse";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "..");
const args = process.argv.slice(2).filter((a) => !a.startsWith("--"));
const DRY_RUN = process.argv.includes("--dry-run");
const PDF_PATH =
  args[0] ||
  path.join(process.env.USERPROFILE || "", "Downloads", "Street_Cat_Interview_English_Localization.pdf");
const UI_EN = path.join(ROOT, "Assets", "Resources", "Loc", "ui_en.json");
const UI_ZH = path.join(ROOT, "Assets", "Resources", "Loc", "ui_zh.json");
const SCRIPTS_EN = path.join(ROOT, "Assets", "Resources", "Loc", "scripts_en.json");
const HARDCODED_OUT = path.join(ROOT, "Docs", "loc", "hardcoded_en_from_pdf.json");

const STATUS_RE = /^(Translated|New Translation|Hard-coded)$/;
const SKIP_LINE =
  /^(Street Cat Interview|Complete English|All player-facing|Total entries|Localization conventions|• |Prepared |-- \d+ of \d+ --|#\s*Source ID|UI Localization|Scripted Dialogue|Script \/ Goals|Chapter Flow|Game State|Save Slots|Free Interview|Interview Materials|Other Code|Interview Hints|Community Investigation|Reporter's Notebook|UI \(Hard-coded\)|Title Menu Layout|Drafting|Drafting AI|Writing Suggestions|\d+ entries$|Speaker English Source Status|Localization categories|Existing English|New \/ hard-coded)/;

const SPEAKERS = [
  "Stage Direction",
  "Narration",
  "Security Guard",
  "Uncle Guard",
  "Shen He",
  "Ms. Lin",
  "Choice",
  "System",
  "Ling",
  "Dafu",
];

function readJson(p) {
  return JSON.parse(fs.readFileSync(p, "utf8"));
}

function writeJson(p, obj) {
  fs.writeFileSync(p, JSON.stringify(obj, null, 2) + "\n", "utf8");
}

function normWs(s) {
  return (s || "").replace(/\s+/g, " ").trim();
}

function hasPdfBreakArtifact(s) {
  return /\w-\s+\w/.test(s);
}

function shouldApply(oldText, newText, zhText) {
  if (normWs(oldText) === normWs(newText)) return false;
  if (hasPdfBreakArtifact(newText) && !hasPdfBreakArtifact(oldText)) return false;
  if (zhText && zhText.includes("\n") && !newText.includes("\n") && oldText.includes("\n")) {
    // Keep intentional line breaks from existing EN when PDF flattened them.
    const parts = normWs(newText).split(/(?<=\.)\s+/);
    if (parts.length >= 2 && normWs(oldText) === normWs(parts.join(" "))) return false;
    return false;
  }
  return true;
}

function stripStatus(s) {
  return s.replace(/\s+(Translated|New Translation|Hard-coded)\s*$/i, "").trim();
}

function mapSpeaker(raw) {
  if (!raw || raw === "-" || raw === "Narration" || raw === "Stage Direction") return "";
  return raw;
}

function preprocessLines(lines) {
  const out = [];
  for (let i = 0; i < lines.length; i++) {
    let line = lines[i].trim();
    if (!line) continue;

    // Join wrapped file paths (Assets/Scripts/...cs:199)
    while (
      i + 1 < lines.length &&
      /Assets\/Scripts\//.test(line) &&
      !/:\d+/.test(line) &&
      !STATUS_RE.test(lines[i + 1].trim()) &&
      !/^\d+\s+/.test(lines[i + 1].trim())
    ) {
      line += lines[++i].trim();
    }

    // Join wrapped objective keys before the value line
    while (
      i + 1 < lines.length &&
      /^\d+\s+objective:/.test(line) &&
      !line.includes(" Translated") &&
      !line.includes(" Hard-coded") &&
      !STATUS_RE.test(lines[i + 1].trim()) &&
      !lines[i + 1].trim().startsWith("-") &&
      !/^\d+\s+(ui\.|SC-|title:|Assets\/)/.test(lines[i + 1].trim())
    ) {
      line += " " + lines[++i].trim();
    }

    out.push(line);
  }
  return out;
}

function parseSpeakerAndText(rest) {
  let speaker = "";
  let text = rest.trim();
  if (text.startsWith("- ")) {
    text = text.slice(2);
  } else {
    for (const sp of SPEAKERS) {
      if (text.startsWith(sp + " ")) {
        speaker = sp;
        text = text.slice(sp.length + 1);
        break;
      }
    }
  }
  text = stripStatus(text);
  return { speaker: mapSpeaker(speaker), text };
}

function parsePdfEntries(text) {
  const rawLines = text.split(/\r?\n/);
  const lines = preprocessLines(rawLines);
  const entries = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];
    if (!line || SKIP_LINE.test(line)) {
      i++;
      continue;
    }

    const uiM = line.match(/^(\d+)\s+(ui\.[a-z0-9_.]+)\s+(.*)$/);
    const scriptM = line.match(/^(\d+)\s+(SC-\d+:\d+|title:SC-\d+)\s+(.*)$/);
    const fileM = line.match(/^(\d+)\s+(Assets\/Scripts\/[^:]+:\d+)\s+(.*)$/);
    const objM = line.match(/^(\d+)\s+(objective:[^\s].*?)\s*(.*)$/);

    const m = uiM || scriptM || fileM || objM;
    if (!m) {
      i++;
      continue;
    }

    const key = m[2];
    let rest = m[3] || "";
    let status = "";
    i++;

    while (i < lines.length) {
      const cont = lines[i];
      if (STATUS_RE.test(cont)) {
        status = cont;
        i++;
        break;
      }
      const nextRow = cont.match(
        /^(\d+)\s+(ui\.[a-z0-9_.]+|SC-\d+:\d+|title:SC-\d+|Assets\/Scripts\/[^:]+:\d+|objective:)/
      );
      if (nextRow) break;
      if (SKIP_LINE.test(cont)) {
        i++;
        continue;
      }
      rest += (rest ? "\n" : "") + cont;
      i++;
    }

    const { speaker, text } = parseSpeakerAndText(rest);
    if (!text) continue;

    entries.push({
      key,
      speaker,
      text,
      status: status || (rest.match(/(Translated|New Translation|Hard-coded)\s*$/i)?.[1] ?? ""),
      hardcoded: key.startsWith("Assets/"),
    });
  }

  return entries;
}

async function extractPdfText(pdfPath) {
  const buf = fs.readFileSync(pdfPath);
  const parser = new PDFParse({ data: buf });
  const result = await parser.getText();
  await parser.destroy();
  return result.text;
}

function upsertScriptLine(scriptsEn, key, text, speakerName) {
  const lines = scriptsEn.lines || (scriptsEn.lines = []);
  let entry = lines.find((e) => e.key === key);
  if (!entry) {
    entry = { key, text: "", speakerName: "", choices: [] };
    lines.push(entry);
  }
  entry.text = text;
  if (speakerName !== undefined) entry.speakerName = speakerName;
  return !lines.some((e, idx) => e === entry && idx < lines.length - 1 && lines[lines.length - 1] === entry)
    ? entry
    : entry;
}

function sortScriptLines(scriptsEn) {
  scriptsEn.lines.sort((a, b) => {
    const titleA = a.key.startsWith("title:");
    const titleB = b.key.startsWith("title:");
    if (titleA && !titleB) return -1;
    if (!titleA && titleB) return 1;
    const [scA, liA] = a.key.replace("title:", "").split(":");
    const [scB, liB] = b.key.replace("title:", "").split(":");
    if (scA !== scB) return scA.localeCompare(scB);
    const na = liA === undefined ? -1 : parseInt(liA, 10);
    const nb = liB === undefined ? -1 : parseInt(liB, 10);
    return na - nb;
  });
}

async function main() {
  console.log("PDF:", PDF_PATH);
  if (!fs.existsSync(PDF_PATH)) {
    console.error("PDF not found");
    process.exit(1);
  }

  const text = await extractPdfText(PDF_PATH);
  const extractPath = path.join(ROOT, "Docs", "loc", "_pdf_extract.txt");
  fs.writeFileSync(extractPath, text, "utf8");
  console.log("Extracted", text.length, "chars");

  const entries = parsePdfEntries(text);
  console.log("Parsed entries:", entries.length);

  const uiEn = readJson(UI_EN);
  const uiZh = readJson(UI_ZH);
  const zhByKey = new Map((uiZh.entries || []).map((e) => [e.key, e.value || ""]));
  const scriptsEn = readJson(SCRIPTS_EN);
  const uiByKey = new Map((uiEn.entries || []).map((e) => [e.key, e]));
  const scriptByKey = new Map((scriptsEn.lines || []).map((e) => [e.key, e]));

  const stats = {
    uiUpdated: 0,
    uiAdded: 0,
    scriptsUpdated: 0,
    scriptsAdded: 0,
    hardcoded: 0,
    samples: [],
  };
  const hardcoded = [];

  for (const row of entries) {
    if (row.hardcoded) {
      stats.hardcoded++;
      hardcoded.push({ source: row.key, text: row.text, status: row.status });
      continue;
    }

    if (row.key.startsWith("ui.")) {
      const existing = uiByKey.get(row.key);
      if (!existing) continue;
      const old = (existing.value || "").trim();
      if (shouldApply(old, row.text, zhByKey.get(row.key))) {
        existing.value = row.text;
        stats.uiUpdated++;
        if (stats.samples.length < 15) stats.samples.push({ key: row.key, old, new: row.text });
      }
      continue;
    }

    if (/^(SC-\d+:\d+|title:SC-\d+)$/.test(row.key)) {
      const existing = scriptByKey.get(row.key);
      if (existing) {
        const old = (existing.text || "").trim();
        if (shouldApply(old, row.text)) {
          existing.text = row.text;
          if (row.speaker !== undefined && row.speaker !== existing.speakerName) {
            existing.speakerName = row.speaker;
          }
          stats.scriptsUpdated++;
          if (stats.samples.length < 20) stats.samples.push({ key: row.key, old, new: row.text });
        }
      } else {
        const entry = { key: row.key, text: row.text, speakerName: row.speaker || "", choices: [] };
        scriptsEn.lines.push(entry);
        scriptByKey.set(row.key, entry);
        stats.scriptsAdded++;
        if (stats.samples.length < 20) stats.samples.push({ key: row.key, old: "(missing)", new: row.text });
      }
    }
  }

  sortScriptLines(scriptsEn);

  console.log("\n=== Summary ===");
  console.log("UI updated:", stats.uiUpdated);
  console.log("Scripts updated:", stats.scriptsUpdated);
  console.log("Scripts added:", stats.scriptsAdded);
  console.log("Hardcoded entries in PDF (not wired):", stats.hardcoded);

  if (stats.samples.length) {
    console.log("\nSample changes:");
    for (const c of stats.samples) {
      console.log(`  ${c.key}`);
      if (c.old) console.log(`    - ${String(c.old).slice(0, 90)}${String(c.old).length > 90 ? "…" : ""}`);
      console.log(`    + ${String(c.new).slice(0, 90)}${String(c.new).length > 90 ? "…" : ""}`);
    }
  }

  if (!DRY_RUN) {
    if (stats.uiUpdated > 0) writeJson(UI_EN, uiEn);
    if (stats.scriptsUpdated > 0 || stats.scriptsAdded > 0) writeJson(SCRIPTS_EN, scriptsEn);
    writeJson(HARDCODED_OUT, { generated: new Date().toISOString(), entries: hardcoded });
    console.log("\nWrote loc files and", HARDCODED_OUT);
  } else {
    console.log("\n(dry-run: no files written)");
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
