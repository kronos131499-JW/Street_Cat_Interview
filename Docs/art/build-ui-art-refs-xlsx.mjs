/**
 * Artist-facing art requirements (UI + scene BGs) with embedded annotated refs.
 * Run from Docs/art:  node build-ui-art-refs-xlsx.mjs
 *
 * Outputs:
 *   Docs/art/ui-refs/*.png
 *   Docs/art/scene-refs/*.png
 *   Docs/art/美术需求清单_给画师.xlsx  (canonical)
 *   Docs/art/UI界面需求清单_给画师.xlsx (copy alias)
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import ExcelJS from "exceljs";
import { createCanvas, loadImage, GlobalFonts } from "@napi-rs/canvas";
import { defineRows as defineRowsFromModule } from "./ui-art-row-defs.mjs";
import { defineSceneRows } from "./scene-art-row-defs.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(__dirname, "../..");
const REFS = path.join(__dirname, "ui-refs");
const SCENE_REFS = path.join(__dirname, "scene-refs");
const OUT_XLSX = path.join(__dirname, "美术需求清单_给画师.xlsx");
const OUT_XLSX_ALIAS = path.join(__dirname, "UI界面需求清单_给画师.xlsx");

const TITLE = path.join(REPO, "Assets/Resources/VnArt/Title");
const UI = path.join(REPO, "Assets/Resources/VnArt/UI");
const SOCIAL = path.join(UI, "Social");
const KEYART = path.join(REPO, "Assets/Resources/VnArt/KeyArt");
const BG = path.join(REPO, "Assets/Resources/VnArt/Backgrounds");
/** Latest free-interview art mockup (three-column scrapbook). */
const INTERVIEW_MOCKUP = path.join(__dirname, "free_interview_mockup_ref.png");

const W = 960;
const H = 540;

function registerFonts() {
  const candidates = [
    "C:/Windows/Fonts/msyh.ttc",
    "C:/Windows/Fonts/msyhbd.ttc",
    "C:/Windows/Fonts/simhei.ttf",
    "C:/Windows/Fonts/simsun.ttc",
    "C:/Windows/Fonts/NotoSansCJKsc-Regular.otf",
  ];
  let ok = false;
  for (const f of candidates) {
    if (!fs.existsSync(f)) continue;
    try {
      GlobalFonts.registerFromPath(f, "UIArtCN");
      ok = true;
      break;
    } catch {
      /* try next */
    }
  }
  if (!ok) console.warn("WARN: no CJK font registered; labels may be empty boxes");
  return ok;
}

function exists(p) {
  return p && fs.existsSync(p);
}

function pad(n) {
  return String(n).padStart(2, "0");
}

/** Normalized rect {x,y,w,h} in 0..1 */
function nr(x, y, w, h) {
  return { x, y, w, h };
}

function toPx(r) {
  return {
    x: Math.round(r.x * W),
    y: Math.round(r.y * H),
    w: Math.round(r.w * W),
    h: Math.round(r.h * H),
  };
}

function roundRect(ctx, x, y, w, h, rad) {
  const r = Math.min(rad, w / 2, h / 2);
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}

function fillTextBox(ctx, text, x, y, maxW, fontSize = 16, color = "#1a1410") {
  ctx.font = `bold ${fontSize}px UIArtCN, sans-serif`;
  ctx.fillStyle = color;
  const lines = wrapText(ctx, text, maxW);
  let cy = y;
  for (const line of lines) {
    ctx.fillText(line, x, cy);
    cy += fontSize + 4;
  }
  return cy;
}

function wrapText(ctx, text, maxW) {
  const chars = [...String(text)];
  const lines = [];
  let cur = "";
  for (const ch of chars) {
    const test = cur + ch;
    if (ctx.measureText(test).width > maxW && cur) {
      lines.push(cur);
      cur = ch;
    } else cur = test;
  }
  if (cur) lines.push(cur);
  return lines.length ? lines : [""];
}

function drawCallout(ctx, rect, label, opts = {}) {
  const p = toPx(rect);
  const accent = opts.accent || "#e85d04";
  const pad = 4;
  ctx.save();
  ctx.strokeStyle = accent;
  ctx.lineWidth = 3;
  ctx.setLineDash([]);
  ctx.strokeRect(p.x - pad, p.y - pad, p.w + pad * 2, p.h + pad * 2);
  ctx.fillStyle = "rgba(232, 93, 4, 0.18)";
  ctx.fillRect(p.x - pad, p.y - pad, p.w + pad * 2, p.h + pad * 2);

  // Label bubble — prefer above, else below / side
  const fontSize = opts.fontSize || 15;
  ctx.font = `bold ${fontSize}px UIArtCN, sans-serif`;
  const lines = wrapText(ctx, label, Math.min(320, W - 40));
  const tw = Math.max(...lines.map((l) => ctx.measureText(l).width), 40);
  const th = lines.length * (fontSize + 4) + 12;
  const bw = tw + 20;
  const bh = th;

  let bx = Math.min(Math.max(8, p.x), W - 8 - bw);
  // Prefer above; else below; if both clip, place inside highlight near top
  let by = p.y - bh - 14;
  let anchor = "above";
  if (by < 8) {
    by = p.y + p.h + 14;
    anchor = "below";
  }
  if (by + bh > H - 8) {
    by = Math.max(8, p.y + 10);
    bx = Math.min(Math.max(8, p.x + 12), W - 8 - bw);
    anchor = "inside";
  }

  // Connector
  const cx = p.x + p.w / 2;
  const cy =
    anchor === "above" ? p.y - pad : anchor === "below" ? p.y + p.h + pad : p.y + pad + 4;
  const lx = bx + bw / 2;
  const ly = anchor === "above" ? by + bh : by;
  ctx.strokeStyle = accent;
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.moveTo(cx, cy);
  ctx.lineTo(lx, ly);
  ctx.stroke();

  // Arrow head
  ctx.beginPath();
  ctx.moveTo(cx, cy);
  if (anchor === "above") {
    ctx.lineTo(cx - 5, cy - 8);
    ctx.lineTo(cx + 5, cy - 8);
  } else if (anchor === "below") {
    ctx.lineTo(cx - 5, cy + 8);
    ctx.lineTo(cx + 5, cy + 8);
  } else {
    ctx.lineTo(cx - 5, cy + 8);
    ctx.lineTo(cx + 5, cy + 8);
  }
  ctx.closePath();
  ctx.fillStyle = accent;
  ctx.fill();

  roundRect(ctx, bx, by, bw, bh, 6);
  ctx.fillStyle = "rgba(255, 248, 240, 0.96)";
  ctx.fill();
  ctx.strokeStyle = accent;
  ctx.lineWidth = 2;
  ctx.stroke();

  ctx.fillStyle = "#1a1410";
  let ty = by + 8 + fontSize;
  for (const line of lines) {
    ctx.fillText(line, bx + 10, ty);
    ty += fontSize + 4;
  }
  ctx.restore();
}

function drawChromeFrame(ctx, title) {
  ctx.fillStyle = "#2a2430";
  ctx.fillRect(0, 0, W, H);
  // letterbox
  ctx.fillStyle = "#0a0a0c";
  ctx.fillRect(0, 0, W, 36);
  ctx.fillRect(0, H - 36, W, 36);
  ctx.strokeStyle = "#c9953a";
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(0, 36);
  ctx.lineTo(W, 36);
  ctx.moveTo(0, H - 36);
  ctx.lineTo(W, H - 36);
  ctx.stroke();

  // stage
  ctx.fillStyle = "#4a5568";
  ctx.fillRect(0, 36, W, H - 72);

  // watermark
  ctx.font = "12px UIArtCN, sans-serif";
  ctx.fillStyle = "rgba(255,255,255,0.35)";
  ctx.fillText("线框示意 · 非实机截图", 12, H - 12);
  if (title) {
    ctx.fillStyle = "rgba(255,255,255,0.55)";
    ctx.fillText(title, 12, 22);
  }
}

function drawTopBar(ctx) {
  const r = toPx(nr(0.02, 0.08, 0.96, 0.07));
  ctx.fillStyle = "rgba(30, 24, 20, 0.75)";
  roundRect(ctx, r.x, r.y, r.w, r.h, 8);
  ctx.fill();
  ctx.fillStyle = "#e8dcc8";
  ctx.font = "13px UIArtCN, sans-serif";
  ctx.fillText("章节 chip", r.x + 12, r.y + 22);
  ctx.fillText("目标行 ……", r.x + 120, r.y + 22);
  ctx.fillText("回看", r.x + r.w - 120, r.y + 22);
  ctx.fillText("菜单", r.x + r.w - 60, r.y + 22);
}

function drawDialogueBox(ctx) {
  const box = toPx(nr(0.08, 0.68, 0.84, 0.22));
  ctx.fillStyle = "rgba(42, 34, 28, 0.92)";
  roundRect(ctx, box.x, box.y, box.w, box.h, 6);
  ctx.fill();
  ctx.strokeStyle = "#8a7048";
  ctx.lineWidth = 2;
  ctx.stroke();
  ctx.fillStyle = "#d8c8b0";
  ctx.font = "14px UIArtCN, sans-serif";
  ctx.fillText("对白正文区域 ……", box.x + 16, box.y + 48);

  const name = toPx(nr(0.1, 0.62, 0.18, 0.06));
  ctx.fillStyle = "rgba(60, 48, 36, 0.95)";
  roundRect(ctx, name.x, name.y, name.w, name.h, 4);
  ctx.fill();
  ctx.fillStyle = "#f0e0c8";
  ctx.font = "13px UIArtCN, sans-serif";
  ctx.fillText("姓名牌", name.x + 12, name.y + 20);
}

function drawTitleWire(ctx) {
  // desk
  ctx.fillStyle = "#5c4030";
  ctx.fillRect(0, 0, W, H);
  // magazine
  const mag = toPx(nr(0.1, 0.08, 0.8, 0.84));
  ctx.fillStyle = "#e8dcc8";
  ctx.fillRect(mag.x, mag.y, mag.w, mag.h);
  ctx.fillStyle = "#d4c4a8";
  ctx.fillRect(mag.x, mag.y, mag.w * 0.48, mag.h);
  ctx.fillStyle = "#c8b898";
  ctx.fillRect(mag.x + mag.w * 0.52, mag.y, mag.w * 0.48, mag.h);
  ctx.strokeStyle = "#3a2a1a";
  ctx.lineWidth = 2;
  ctx.strokeRect(mag.x, mag.y, mag.w, mag.h);
  ctx.font = "12px UIArtCN, sans-serif";
  ctx.fillStyle = "#5a4030";
  ctx.fillText("左页", mag.x + 20, mag.y + 24);
  ctx.fillText("右页·目录", mag.x + mag.w * 0.55, mag.y + 24);
  // desk props hint
  ctx.fillStyle = "#8a7060";
  ctx.fillRect(20, H - 90, 70, 60);
  ctx.fillRect(100, H - 80, 50, 50);
  ctx.fillStyle = "rgba(255,255,255,0.4)";
  ctx.font = "11px UIArtCN, sans-serif";
  ctx.fillText("桌面道具", 24, H - 20);
}

/** Screen region maps for wireframes */
const SCREENS = {
  title: {
    paint: drawTitleWire,
    regions: {
      desk: nr(0, 0, 1, 1),
      magazine: nr(0.1, 0.08, 0.8, 0.84),
      magazine_shadow: nr(0.12, 0.12, 0.8, 0.84),
      feature: nr(0.14, 0.38, 0.34, 0.42),
      logo_cn: nr(0.16, 0.78, 0.3, 0.1),
      logo_en: nr(0.18, 0.68, 0.26, 0.08),
      quote: nr(0.14, 0.14, 0.34, 0.2),
      contents: nr(0.55, 0.82, 0.3, 0.08),
      blurb: nr(0.42, 0.1, 0.14, 0.14),
      tape_primary: nr(0.58, 0.55, 0.24, 0.08),
      tape: nr(0.58, 0.45, 0.24, 0.07),
      icons: nr(0.55, 0.35, 0.08, 0.35),
      paperclip: nr(0.78, 0.72, 0.06, 0.1),
      prop_notes: nr(0.02, 0.82, 0.08, 0.14),
      prop_scraps: nr(0.12, 0.85, 0.12, 0.1),
      keyart: nr(0.1, 0.08, 0.8, 0.84),
      obsolete: nr(0.55, 0.2, 0.3, 0.5),
    },
  },
  dialogue: {
    paint(ctx) {
      drawChromeFrame(ctx, "Mode.Dialogue");
      drawTopBar(ctx);
      // portrait hint
      ctx.fillStyle = "rgba(180,160,140,0.5)";
      ctx.fillRect(W * 0.55, H * 0.15, W * 0.35, H * 0.5);
      ctx.fillStyle = "#fff";
      ctx.font = "13px UIArtCN, sans-serif";
      ctx.fillText("立绘槽", W * 0.65, H * 0.4);
      drawDialogueBox(ctx);
      // choices
      for (let i = 0; i < 2; i++) {
        const c = toPx(nr(0.55, 0.48 + i * 0.08, 0.35, 0.06));
        ctx.fillStyle = "rgba(50,40,32,0.9)";
        roundRect(ctx, c.x, c.y, c.w, c.h, 4);
        ctx.fill();
        ctx.fillStyle = "#e0d0b8";
        ctx.font = "12px UIArtCN, sans-serif";
        ctx.fillText(`选项 ${i + 1}`, c.x + 12, c.y + 22);
      }
      // hide btn
      const hb = toPx(nr(0.88, 0.88, 0.08, 0.05));
      ctx.fillStyle = "rgba(40,40,40,0.8)";
      ctx.fillRect(hb.x, hb.y, hb.w, hb.h);
    },
    regions: {
      paper: nr(0.08, 0.68, 0.84, 0.22),
      frame: nr(0.08, 0.62, 0.84, 0.28),
      choice: nr(0.55, 0.48, 0.35, 0.14),
      topbar: nr(0.02, 0.08, 0.96, 0.07),
      letterbox: nr(0, 0, 1, 0.07),
      toast: nr(0.35, 0.12, 0.3, 0.06),
      hide: nr(0.88, 0.88, 0.08, 0.05),
      nameplate: nr(0.1, 0.62, 0.18, 0.06),
    },
  },
  investigate: {
    paint(ctx) {
      drawChromeFrame(ctx, "Mode.Investigate");
      ctx.fillStyle = "#6a8a6a";
      ctx.fillRect(40, 50, W - 80, H - 140);
      ctx.fillStyle = "#fff";
      ctx.font = "14px UIArtCN, sans-serif";
      ctx.fillText("调查地图 BG", 60, 80);
      // hotspots — one shown as investigated (green fill + ✓ badge)
      [[0.25, 0.35, false], [0.45, 0.5, true], [0.65, 0.4, false], [0.55, 0.65, false]].forEach(
        ([x, y, done], i) => {
          const p = toPx(nr(x, y, 0.08, 0.1));
          if (done) {
            ctx.fillStyle = "rgba(55, 110, 70, 0.35)";
            ctx.fillRect(p.x, p.y, p.w, p.h);
            ctx.strokeStyle = "rgba(90, 160, 100, 0.85)";
            ctx.lineWidth = 2;
            ctx.strokeRect(p.x, p.y, p.w, p.h);
            // green ✓ badge top-right of hotspot
            const bx = p.x + p.w - 22;
            const by = p.y + 3;
            ctx.fillStyle = "rgba(35, 85, 55, 0.95)";
            roundRect(ctx, bx, by, 18, 18, 3);
            ctx.fill();
            ctx.fillStyle = "#c8ecc8";
            ctx.font = "bold 13px UIArtCN, sans-serif";
            ctx.fillText("✓", bx + 3, by + 14);
          } else {
            ctx.strokeStyle = "rgba(255,220,80,0.55)";
            ctx.lineWidth = 1;
            ctx.setLineDash([4, 3]);
            ctx.strokeRect(p.x, p.y, p.w, p.h);
            ctx.setLineDash([]);
            ctx.fillStyle = "rgba(255,255,255,0.06)";
            ctx.fillRect(p.x, p.y, p.w, p.h);
            ctx.fillStyle = "#fff";
            ctx.font = "11px UIArtCN, sans-serif";
            ctx.fillText(String(i + 1), p.x + 8, p.y + 20);
          }
        }
      );
      // top strip
      const ts = toPx(nr(0.02, 0.09, 0.4, 0.06));
      ctx.fillStyle = "rgba(20,20,20,0.7)";
      ctx.fillRect(ts.x, ts.y, ts.w, ts.h);
      ctx.fillStyle = "#eee";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("调查条", ts.x + 10, ts.y + 22);
      // bottom chips
      for (let i = 0; i < 4; i++) {
        const c = toPx(nr(0.1 + i * 0.2, 0.88, 0.16, 0.06));
        ctx.fillStyle = "rgba(40,32,24,0.9)";
        roundRect(ctx, c.x, c.y, c.w, c.h, 4);
        ctx.fill();
        ctx.fillStyle = "#e8d8c0";
        ctx.font = "11px UIArtCN, sans-serif";
        ctx.fillText(["交谈", "等待", "笔记", "菜单"][i], c.x + 18, c.y + 22);
      }
    },
    regions: {
      map: nr(0.04, 0.09, 0.92, 0.72),
      hotspot: nr(0.45, 0.5, 0.08, 0.1),
      chip: nr(0.1, 0.88, 0.76, 0.06),
      strip: nr(0.02, 0.09, 0.4, 0.06),
    },
  },
  talk: {
    paint(ctx) {
      drawChromeFrame(ctx, "Mode.Talk");
      drawTopBar(ctx);
      drawDialogueBox(ctx);
      for (let i = 0; i < 3; i++) {
        const c = toPx(nr(0.55, 0.42 + i * 0.07, 0.35, 0.06));
        ctx.fillStyle = "rgba(50,40,32,0.9)";
        roundRect(ctx, c.x, c.y, c.w, c.h, 4);
        ctx.fill();
        ctx.fillStyle = "#e0d0b8";
        ctx.font = "12px UIArtCN, sans-serif";
        ctx.fillText(`话题 ${i + 1}`, c.x + 12, c.y + 22);
      }
    },
    regions: {
      topics: nr(0.55, 0.42, 0.35, 0.2),
    },
  },
  interview: {
    paint(ctx) {
      // Dim night scene behind paper UI (scrapbook free-interview)
      ctx.fillStyle = "#2a2430";
      ctx.fillRect(0, 0, W, H);
      ctx.fillStyle = "#1a2830";
      ctx.fillRect(0, 0, W, H);
      // soft BG hint
      ctx.fillStyle = "rgba(60,70,80,0.45)";
      ctx.fillRect(0, H * 0.35, W, H * 0.65);
      ctx.fillStyle = "rgba(255,255,255,0.35)";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("自由采访 · 三栏 scrapbook（线框）", 12, 18);

      // —— Left column: status + portrait ——
      const status = toPx(nr(0.02, 0.06, 0.16, 0.28));
      ctx.fillStyle = "#f2ead8";
      roundRect(ctx, status.x, status.y, status.w, status.h, 3);
      ctx.fill();
      ctx.fillStyle = "#4a3828";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("受访者状态", status.x + 8, status.y + 18);
      ["信任", "压力", "专注"].forEach((t, i) => {
        const by = status.y + 36 + i * 28;
        ctx.fillStyle = "#6a5848";
        ctx.font = "10px UIArtCN, sans-serif";
        ctx.fillText(t, status.x + 8, by);
        ctx.fillStyle = "#d8d0c0";
        ctx.fillRect(status.x + 8, by + 4, status.w - 16, 8);
        ctx.fillStyle = ["#4a9ad8", "#c05050", "#d4a020"][i];
        ctx.fillRect(status.x + 8, by + 4, (status.w - 16) * [0.7, 0.35, 0.55][i], 8);
      });

      const portrait = toPx(nr(0.02, 0.38, 0.16, 0.52));
      ctx.fillStyle = "#ebe2d0";
      roundRect(ctx, portrait.x, portrait.y, portrait.w, portrait.h, 3);
      ctx.fill();
      ctx.fillStyle = "rgba(160,140,120,0.65)";
      ctx.fillRect(portrait.x + 8, portrait.y + 10, portrait.w - 16, portrait.h - 48);
      ctx.fillStyle = "#fff";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("受访者立绘", portrait.x + 18, portrait.y + portrait.h * 0.45);
      // name plate
      const plate = toPx(nr(0.035, 0.84, 0.13, 0.05));
      ctx.fillStyle = "#f8f0e0";
      ctx.fillRect(plate.x, plate.y, plate.w, plate.h);
      ctx.fillStyle = "#3a2a1a";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("姓名条", plate.x + 28, plate.y + 18);

      // —— Center column: chat paper ——
      const paper = toPx(nr(0.20, 0.05, 0.56, 0.9));
      ctx.fillStyle = "rgba(30,24,20,0.35)";
      ctx.fillRect(paper.x + 4, paper.y + 5, paper.w, paper.h);
      ctx.fillStyle = "#f4eee0";
      ctx.fillRect(paper.x, paper.y, paper.w, paper.h);
      ctx.fillStyle = "#2a2010";
      ctx.font = "bold 16px UIArtCN, sans-serif";
      ctx.fillText("自由采访", paper.x + paper.w / 2 - 36, paper.y + 28);
      ctx.fillStyle = "#8a7a68";
      ctx.font = "10px UIArtCN, sans-serif";
      ctx.fillText("INTERVIEW", paper.x + paper.w / 2 - 28, paper.y + 44);

      // NPC bubble (left)
      const nb = toPx(nr(0.28, 0.22, 0.28, 0.12));
      ctx.beginPath();
      ctx.arc(paper.x + 28, nb.y + 20, 16, 0, Math.PI * 2);
      ctx.fillStyle = "#c8b8a0";
      ctx.fill();
      ctx.fillStyle = "#e8e4dc";
      roundRect(ctx, nb.x, nb.y, nb.w, nb.h, 10);
      ctx.fill();
      ctx.fillStyle = "#5a4a3a";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("对方气泡…", nb.x + 12, nb.y + 28);

      // Player bubble (right)
      const pb = toPx(nr(0.48, 0.4, 0.24, 0.1));
      ctx.fillStyle = "#d8ecd0";
      roundRect(ctx, pb.x, pb.y, pb.w, pb.h, 10);
      ctx.fill();
      ctx.beginPath();
      ctx.arc(pb.x + pb.w + 22, pb.y + 18, 16, 0, Math.PI * 2);
      ctx.fillStyle = "#b0a090";
      ctx.fill();
      ctx.fillStyle = "#3a4a30";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("我方气泡…", pb.x + 12, pb.y + 26);

      // Input bar + send
      const input = toPx(nr(0.23, 0.84, 0.42, 0.08));
      ctx.fillStyle = "#fff";
      ctx.strokeStyle = "#c8b898";
      ctx.lineWidth = 1;
      roundRect(ctx, input.x, input.y, input.w, input.h, 4);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = "#a09080";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("输入你的问题…", input.x + 10, input.y + 28);
      const send = toPx(nr(0.67, 0.845, 0.06, 0.07));
      ctx.fillStyle = "#8a3028";
      roundRect(ctx, send.x, send.y, send.w, send.h, 4);
      ctx.fill();
      ctx.fillStyle = "#fff";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("发", send.x + 14, send.y + 26);

      // —— Right column: inspire + toolbar ——
      const inspire = toPx(nr(0.78, 0.06, 0.2, 0.58));
      ctx.fillStyle = "#f2ead8";
      roundRect(ctx, inspire.x, inspire.y, inspire.w, inspire.h, 3);
      ctx.fill();
      ctx.fillStyle = "#4a3828";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("提问灵感", inspire.x + 12, inspire.y + 22);
      for (let i = 0; i < 3; i++) {
        const c = toPx(nr(0.795, 0.16 + i * 0.15, 0.17, 0.12));
        ctx.fillStyle = "#fff8ec";
        ctx.fillRect(c.x, c.y, c.w, c.h);
        ctx.strokeStyle = "#d0c0a8";
        ctx.strokeRect(c.x, c.y, c.w, c.h);
        ctx.fillStyle = "#6a5848";
        ctx.font = "10px UIArtCN, sans-serif";
        ctx.fillText(`灵感卡 ${i + 1}`, c.x + 8, c.y + 28);
      }

      const toolbar = toPx(nr(0.78, 0.72, 0.2, 0.2));
      ctx.fillStyle = "#f2ead8";
      roundRect(ctx, toolbar.x, toolbar.y, toolbar.w, toolbar.h, 3);
      ctx.fill();
      ["回顾", "笔记", "菜单"].forEach((t, i) => {
        const col = i % 2;
        const row = Math.floor(i / 2);
        ctx.fillStyle = "#e8e0d0";
        const bx = toolbar.x + 10 + col * 85;
        const by = toolbar.y + 16 + row * 42;
        roundRect(ctx, bx, by, 72, 34, 3);
        ctx.fill();
        ctx.fillStyle = "#3a2a1a";
        ctx.font = "11px UIArtCN, sans-serif";
        ctx.fillText(t, bx + 18, by + 22);
      });

      ctx.fillStyle = "rgba(255,255,255,0.4)";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("线框示意 · 非实机截图", 12, H - 12);
    },
    regions: {
      status: nr(0.02, 0.06, 0.16, 0.28),
      portrait: nr(0.02, 0.38, 0.16, 0.52),
      nameplate: nr(0.035, 0.84, 0.13, 0.05),
      chat_paper: nr(0.2, 0.05, 0.56, 0.9),
      bubbles: nr(0.26, 0.2, 0.46, 0.35),
      avatars: nr(0.22, 0.22, 0.08, 0.12),
      input: nr(0.23, 0.84, 0.42, 0.08),
      send: nr(0.67, 0.845, 0.06, 0.07),
      inspire: nr(0.78, 0.06, 0.2, 0.58),
      toolbar: nr(0.78, 0.72, 0.2, 0.2),
    },
  },
  notebook: {
    paint(ctx) {
      ctx.fillStyle = "#1e1814";
      ctx.fillRect(0, 0, W, H);
      ctx.fillStyle = "#3a3028";
      ctx.fillRect(40, 30, W - 80, H - 60);
      // page lines
      ctx.strokeStyle = "rgba(200,180,150,0.15)";
      for (let y = 80; y < H - 50; y += 22) {
        ctx.beginPath();
        ctx.moveTo(80, y);
        ctx.lineTo(W - 80, y);
        ctx.stroke();
      }
      // stickies left
      for (let i = 0; i < 6; i++) {
        const col = i % 2;
        const row = Math.floor(i / 2);
        const s = toPx(nr(0.08 + col * 0.14, 0.2 + row * 0.18, 0.12, 0.14));
        ctx.fillStyle = ["#f5e6a0", "#f0c8c8", "#c8e0f0"][i % 3];
        ctx.fillRect(s.x, s.y, s.w, s.h);
      }
      // paperclip
      ctx.fillStyle = "#888";
      ctx.fillRect(W * 0.7, 50, 18, 50);
      // tape btn
      const tb = toPx(nr(0.72, 0.85, 0.18, 0.07));
      ctx.fillStyle = "#c8a060";
      roundRect(ctx, tb.x, tb.y, tb.w, tb.h, 3);
      ctx.fill();
      ctx.fillStyle = "rgba(255,255,255,0.4)";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("线框 · NotebookOverlay", 12, H - 12);
    },
    regions: {
      desk: nr(0.04, 0.05, 0.92, 0.9),
      cover: nr(0.55, 0.15, 0.35, 0.5),
      sticky: nr(0.08, 0.2, 0.26, 0.5),
      paperclip: nr(0.7, 0.08, 0.04, 0.12),
    },
  },
  writing_pick: {
    paint(ctx) {
      drawChromeFrame(ctx, "写稿立意");
      drawTopBar(ctx);
      drawDialogueBox(ctx);
      for (let i = 0; i < 2; i++) {
        const c = toPx(nr(0.5, 0.42 + i * 0.1, 0.4, 0.08));
        ctx.fillStyle = "rgba(50,40,32,0.9)";
        roundRect(ctx, c.x, c.y, c.w, c.h, 4);
        ctx.fill();
        ctx.fillStyle = "#e0d0b8";
        ctx.font = "13px UIArtCN, sans-serif";
        ctx.fillText(`立意选项 ${i + 1}`, c.x + 16, c.y + 28);
      }
    },
    regions: {
      choices: nr(0.5, 0.42, 0.4, 0.18),
    },
  },
  corkboard: {
    paint(ctx) {
      ctx.fillStyle = "#8b6914";
      ctx.fillRect(0, 0, W, H);
      // cork texture hint
      for (let i = 0; i < 40; i++) {
        ctx.fillStyle = `rgba(0,0,0,${0.03 + (i % 5) * 0.01})`;
        ctx.fillRect((i * 97) % W, (i * 53) % H, 40, 30);
      }
      // cards
      for (let i = 0; i < 8; i++) {
        const col = i % 4;
        const row = Math.floor(i / 4);
        const c = toPx(nr(0.08 + col * 0.22, 0.15 + row * 0.35, 0.18, 0.28));
        ctx.fillStyle = "#f5edd8";
        ctx.fillRect(c.x, c.y, c.w, c.h);
        ctx.fillStyle = "#c8a060";
        ctx.fillRect(c.x + 10, c.y - 6, c.w - 20, 12);
        ctx.fillStyle = "#5a4030";
        ctx.font = "11px UIArtCN, sans-serif";
        ctx.fillText(`素材 ${i + 1}`, c.x + 16, c.y + 40);
      }
      ctx.fillStyle = "rgba(255,255,255,0.5)";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("WritingMaterialsOverlay · 软木板", 12, H - 12);
    },
    regions: {
      board: nr(0, 0, 1, 1),
      card: nr(0.08, 0.15, 0.18, 0.28),
    },
  },
  writing_desk: {
    paint(ctx) {
      ctx.fillStyle = "#1a2838";
      ctx.fillRect(0, 0, W, H);
      // left paper — editable manuscript
      const paper = toPx(nr(0.03, 0.05, 0.62, 0.82));
      ctx.fillStyle = "#e8e0d0";
      ctx.fillRect(paper.x, paper.y, paper.w, paper.h);
      ctx.fillStyle = "#3a3020";
      ctx.font = "16px UIArtCN, sans-serif";
      ctx.fillText("槐安社区特稿（字体）", paper.x + 20, paper.y + 36);
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillStyle = "#5a5040";
      ctx.fillText("可编辑正文（滚动）……", paper.x + 20, paper.y + 80);
      // draft area
      const draft = toPx(nr(0.05, 0.22, 0.55, 0.55));
      ctx.strokeStyle = "#a09070";
      ctx.strokeRect(draft.x, draft.y, draft.w, draft.h);
      ctx.fillStyle = "#8a7a60";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("玩家可直接改字 · 无「预览文章」按钮", draft.x + 8, draft.y + 24);
      // right panel
      const right = toPx(nr(0.68, 0.05, 0.29, 0.82));
      ctx.fillStyle = "#243040";
      ctx.fillRect(right.x, right.y, right.w, right.h);
      ctx.fillStyle = "#c8d0d8";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("立意 / 素材列表 / 状态", right.x + 12, right.y + 30);
      // bottom action bar: 返回素材 · AI优化 · 提交
      const bar = toPx(nr(0.04, 0.9, 0.92, 0.07));
      ctx.fillStyle = "#152030";
      ctx.fillRect(bar.x, bar.y, bar.w, bar.h);
      const labels = ["返回修改素材", "AI 优化", "提交主编审核"];
      labels.forEach((t, i) => {
        const b = toPx(nr(0.06 + i * 0.3, 0.91, 0.26, 0.05));
        ctx.fillStyle = i === 2 ? "#c87838" : "#2a4058";
        roundRect(ctx, b.x, b.y, b.w, b.h, 3);
        ctx.fill();
        ctx.fillStyle = "#eee";
        ctx.font = "11px UIArtCN, sans-serif";
        ctx.fillText(t, b.x + 16, b.y + 20);
      });
      ctx.fillStyle = "rgba(255,255,255,0.4)";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("WritingDeskOverlay · 可编辑成稿", 12, H - 12);
    },
    regions: {
      paper: nr(0.03, 0.05, 0.62, 0.82),
      draft: nr(0.05, 0.22, 0.55, 0.55),
      right: nr(0.68, 0.05, 0.29, 0.82),
      actions: nr(0.04, 0.9, 0.92, 0.07),
    },
  },
  article_preview: {
    paint(ctx) {
      ctx.fillStyle = "rgba(10,10,15,0.7)";
      ctx.fillRect(0, 0, W, H);
      const sheet = toPx(nr(0.2, 0.1, 0.6, 0.8));
      ctx.fillStyle = "#f5f0e4";
      ctx.fillRect(sheet.x, sheet.y, sheet.w, sheet.h);
      ctx.fillStyle = "#3a3020";
      ctx.font = "18px UIArtCN, sans-serif";
      ctx.fillText("文章预览纸页", sheet.x + 40, sheet.y + 50);
      ctx.font = "13px UIArtCN, sans-serif";
      ctx.fillStyle = "#6a6050";
      ctx.fillText("正文预览 ……", sheet.x + 40, sheet.y + 100);
    },
    regions: {
      sheet: nr(0.2, 0.1, 0.6, 0.8),
    },
  },
  social: {
    paint(ctx) {
      drawChromeFrame(ctx, "SC-03 Social");
      // phone frame
      const ph = toPx(nr(0.32, 0.08, 0.36, 0.84));
      ctx.fillStyle = "#1a1a1e";
      roundRect(ctx, ph.x - 12, ph.y - 12, ph.w + 24, ph.h + 24, 28);
      ctx.fill();
      ctx.fillStyle = "#0e0e12";
      roundRect(ctx, ph.x, ph.y, ph.w, ph.h, 8);
      ctx.fill();
      // feed cards
      for (let i = 0; i < 3; i++) {
        const c = toPx(nr(0.34, 0.12 + i * 0.25, 0.32, 0.22));
        ctx.fillStyle = "#2a2a30";
        ctx.fillRect(c.x, c.y, c.w, c.h);
        ctx.fillStyle = "#888";
        ctx.font = "11px UIArtCN, sans-serif";
        ctx.fillText(`帖子 ${i + 1}`, c.x + 12, c.y + 24);
      }
      ctx.fillStyle = "rgba(255,255,255,0.45)";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("SocialOverlay · 手机外框 + feed", 12, H - 12);
    },
    regions: {
      phone: nr(0.32, 0.06, 0.36, 0.88),
      feed: nr(0.34, 0.12, 0.32, 0.7),
      detail: nr(0.34, 0.12, 0.32, 0.7),
    },
  },
  epilogue: {
    paint(ctx) {
      drawChromeFrame(ctx, "Mode.Epilogue");
      ctx.fillStyle = "#5a6a7a";
      ctx.fillRect(40, 50, W - 80, H - 140);
      ctx.fillStyle = "#fff";
      ctx.font = "16px UIArtCN, sans-serif";
      ctx.fillText("文章发布页 BG（现占位）", 60, 100);
      drawDialogueBox(ctx);
      const btn = toPx(nr(0.4, 0.88, 0.2, 0.06));
      ctx.fillStyle = "#4a4038";
      roundRect(ctx, btn.x, btn.y, btn.w, btn.h, 4);
      ctx.fill();
      ctx.fillStyle = "#eee";
      ctx.font = "13px UIArtCN, sans-serif";
      ctx.fillText("第一章 完", btn.x + 30, btn.y + 22);
    },
    regions: {
      bg: nr(0.04, 0.09, 0.92, 0.55),
      endbtn: nr(0.4, 0.88, 0.2, 0.06),
    },
  },
  menu: {
    paint(ctx) {
      ctx.fillStyle = "rgba(10,10,15,0.65)";
      ctx.fillRect(0, 0, W, H);
      const panel = toPx(nr(0.28, 0.15, 0.44, 0.7));
      ctx.fillStyle = "#e8dcc8";
      roundRect(ctx, panel.x, panel.y, panel.w, panel.h, 8);
      ctx.fill();
      ctx.fillStyle = "#3a3020";
      ctx.font = "18px UIArtCN, sans-serif";
      ctx.fillText("暂停菜单", panel.x + 40, panel.y + 40);
      ["继续", "回看", "存档", "读档", "笔记", "设置", "回标题"].forEach((t, i) => {
        const b = toPx(nr(0.35, 0.28 + i * 0.07, 0.3, 0.055));
        ctx.fillStyle = "#c8a060";
        roundRect(ctx, b.x, b.y, b.w, b.h, 3);
        ctx.fill();
        ctx.fillStyle = "#2a2010";
        ctx.font = "13px UIArtCN, sans-serif";
        ctx.fillText(t, b.x + 20, b.y + 22);
      });
    },
    regions: {
      panel: nr(0.28, 0.15, 0.44, 0.7),
    },
  },
  backlog: {
    paint(ctx) {
      ctx.fillStyle = "rgba(10,10,15,0.65)";
      ctx.fillRect(0, 0, W, H);
      const panel = toPx(nr(0.12, 0.08, 0.76, 0.84));
      ctx.fillStyle = "#e8dcc8";
      ctx.fillRect(panel.x, panel.y, panel.w, panel.h);
      ctx.fillStyle = "#3a3020";
      ctx.font = "16px UIArtCN, sans-serif";
      ctx.fillText("对话回看", panel.x + 24, panel.y + 36);
      for (let i = 0; i < 6; i++) {
        ctx.fillStyle = "#6a6050";
        ctx.font = "12px UIArtCN, sans-serif";
        ctx.fillText(`历史对白行 ${i + 1} ……`, panel.x + 24, panel.y + 80 + i * 40);
      }
    },
    regions: {
      panel: nr(0.12, 0.08, 0.76, 0.84),
    },
  },
  saveload: {
    paint(ctx) {
      ctx.fillStyle = "rgba(10,10,15,0.65)";
      ctx.fillRect(0, 0, W, H);
      const panel = toPx(nr(0.2, 0.1, 0.6, 0.8));
      ctx.fillStyle = "#e8dcc8";
      ctx.fillRect(panel.x, panel.y, panel.w, panel.h);
      ctx.fillStyle = "#3a3020";
      ctx.font = "16px UIArtCN, sans-serif";
      ctx.fillText("存档 / 读档", panel.x + 24, panel.y + 36);
      for (let i = 0; i < 5; i++) {
        const s = toPx(nr(0.25, 0.22 + i * 0.12, 0.5, 0.1));
        ctx.fillStyle = "#d8d0c0";
        ctx.fillRect(s.x, s.y, s.w, s.h);
        ctx.fillStyle = "#5a5040";
        ctx.font = "12px UIArtCN, sans-serif";
        ctx.fillText(`槽位 ${i + 1}`, s.x + 16, s.y + 28);
      }
    },
    regions: {
      panel: nr(0.2, 0.1, 0.6, 0.8),
    },
  },
  confirm: {
    paint(ctx) {
      ctx.fillStyle = "rgba(10,10,15,0.55)";
      ctx.fillRect(0, 0, W, H);
      const panel = toPx(nr(0.32, 0.35, 0.36, 0.3));
      ctx.fillStyle = "#e8dcc8";
      roundRect(ctx, panel.x, panel.y, panel.w, panel.h, 8);
      ctx.fill();
      ctx.fillStyle = "#3a3020";
      ctx.font = "15px UIArtCN, sans-serif";
      ctx.fillText("覆盖存档？", panel.x + 40, panel.y + 50);
      ctx.fillStyle = "#c8a060";
      roundRect(ctx, panel.x + 30, panel.y + 90, 100, 36, 4);
      ctx.fill();
      roundRect(ctx, panel.x + panel.w - 130, panel.y + 90, 100, 36, 4);
      ctx.fill();
    },
    regions: {
      panel: nr(0.32, 0.35, 0.36, 0.3),
    },
  },
  settings: {
    paint(ctx) {
      ctx.fillStyle = "rgba(10,10,15,0.65)";
      ctx.fillRect(0, 0, W, H);
      const panel = toPx(nr(0.22, 0.1, 0.56, 0.8));
      ctx.fillStyle = "#e8dcc8";
      ctx.fillRect(panel.x, panel.y, panel.w, panel.h);
      ctx.fillStyle = "#3a3020";
      ctx.font = "16px UIArtCN, sans-serif";
      ctx.fillText("设置", panel.x + 24, panel.y + 36);
      ["语言", "字体", "音量", "语速", "全屏"].forEach((t, i) => {
        ctx.fillStyle = "#5a5040";
        ctx.font = "13px UIArtCN, sans-serif";
        ctx.fillText(t, panel.x + 40, panel.y + 90 + i * 50);
        ctx.strokeStyle = "#a09070";
        ctx.strokeRect(panel.x + 160, panel.y + 70 + i * 50, 280, 28);
      });
    },
    regions: {
      panel: nr(0.22, 0.1, 0.56, 0.8),
    },
  },
  debug: {
    paint(ctx) {
      ctx.fillStyle = "#1a2030";
      ctx.fillRect(0, 0, W, H);
      ctx.fillStyle = "#80ff80";
      ctx.font = "16px UIArtCN, sans-serif";
      ctx.fillText("DebugJumpPanel (F9) · 仅开发 · 无需美术", 40, 80);
      for (let i = 0; i < 8; i++) {
        ctx.fillStyle = "#304050";
        ctx.fillRect(40, 120 + i * 40, 400, 32);
        ctx.fillStyle = "#a0c0a0";
        ctx.font = "12px UIArtCN, sans-serif";
        ctx.fillText(`跳转项 ${i + 1}`, 56, 142 + i * 40);
      }
    },
    regions: {
      panel: nr(0.04, 0.15, 0.45, 0.7),
    },
  },
  paper_swatch: {
    paint(ctx) {
      ctx.fillStyle = "#1a1612";
      ctx.fillRect(0, 0, W, H);
      ctx.fillStyle = "#2a2218";
      ctx.fillRect(80, 60, W - 160, H - 120);
      ctx.fillStyle = "#c8b898";
      ctx.font = "18px UIArtCN, sans-serif";
      ctx.fillText("深色纸纹纹理用途示意", 120, 120);
      ctx.font = "13px UIArtCN, sans-serif";
      ctx.fillStyle = "#a09070";
      ["对白盒底板", "笔记页", "采访便签", "写稿纸面"].forEach((t, i) => {
        ctx.fillRect(140, 160 + i * 70, 280, 50);
        ctx.fillStyle = "#e8dcc8";
        ctx.fillText(t, 160, 190 + i * 70);
        ctx.fillStyle = "#a09070";
      });
    },
    regions: {
      texture: nr(0.08, 0.11, 0.84, 0.78),
    },
  },
};

async function savePng(canvas, outPath) {
  const buf = canvas.toBuffer("image/png");
  fs.writeFileSync(outPath, buf);
}

async function generateWireframe(outPath, screenKey, regionKey, label, subtitle) {
  const screen = SCREENS[screenKey];
  if (!screen) throw new Error(`Unknown screen ${screenKey}`);
  const canvas = createCanvas(W, H);
  const ctx = canvas.getContext("2d");
  screen.paint(ctx);
  const region = screen.regions[regionKey] || nr(0.2, 0.2, 0.6, 0.4);
  drawCallout(ctx, region, label);
  if (subtitle) {
    ctx.font = "11px UIArtCN, sans-serif";
    ctx.fillStyle = "rgba(255,255,255,0.55)";
    ctx.fillText(subtitle, 12, 18);
  }
  await savePng(canvas, outPath);
}

async function annotateAsset(outPath, assetPath, label, highlight /* optional nr */) {
  const canvas = createCanvas(W, H);
  const ctx = canvas.getContext("2d");
  ctx.fillStyle = "#1a1612";
  ctx.fillRect(0, 0, W, H);

  if (!exists(assetPath)) {
    ctx.fillStyle = "#c04040";
    ctx.font = "18px UIArtCN, sans-serif";
    ctx.fillText("资源文件缺失 — 示意标注", 40, 80);
    ctx.fillStyle = "#aaa";
    ctx.font = "13px UIArtCN, sans-serif";
    ctx.fillText(path.basename(assetPath || "(none)"), 40, 120);
    drawCallout(ctx, highlight || nr(0.2, 0.3, 0.6, 0.4), label);
    await savePng(canvas, outPath);
    return;
  }

  const img = await loadImage(assetPath);
  const scale = Math.min((W - 40) / img.width, (H - 40) / img.height);
  const dw = Math.round(img.width * scale);
  const dh = Math.round(img.height * scale);
  const dx = Math.round((W - dw) / 2);
  const dy = Math.round((H - dh) / 2);
  ctx.drawImage(img, dx, dy, dw, dh);

  // Default highlight = full drawn image area
  const hl = highlight || {
    x: dx / W,
    y: dy / H,
    w: dw / W,
    h: dh / H,
  };
  drawCallout(ctx, hl, label);
  ctx.font = "11px UIArtCN, sans-serif";
  ctx.fillStyle = "rgba(255,255,255,0.55)";
  ctx.fillText(`实机资源标注 · ${path.basename(assetPath)}`, 12, H - 10);
  await savePng(canvas, outPath);
}

async function annotateAssetRegion(outPath, assetPath, label, regionOnAsset /* 0..1 of asset */) {
  const canvas = createCanvas(W, H);
  const ctx = canvas.getContext("2d");
  ctx.fillStyle = "#1a1612";
  ctx.fillRect(0, 0, W, H);

  if (!exists(assetPath)) {
    await annotateAsset(outPath, assetPath, label, regionOnAsset);
    return;
  }

  const img = await loadImage(assetPath);
  const scale = Math.min((W - 40) / img.width, (H - 40) / img.height);
  const dw = Math.round(img.width * scale);
  const dh = Math.round(img.height * scale);
  const dx = Math.round((W - dw) / 2);
  const dy = Math.round((H - dh) / 2);
  ctx.drawImage(img, dx, dy, dw, dh);

  const hl = {
    x: (dx + regionOnAsset.x * dw) / W,
    y: (dy + regionOnAsset.y * dh) / H,
    w: (regionOnAsset.w * dw) / W,
    h: (regionOnAsset.h * dh) / H,
  };
  drawCallout(ctx, hl, label);
  ctx.font = "11px UIArtCN, sans-serif";
  ctx.fillStyle = "rgba(255,255,255,0.55)";
  ctx.fillText(`实机资源区域标注 · ${path.basename(assetPath)}`, 12, H - 10);
  await savePng(canvas, outPath);
}

async function collageIcons(outPath, paths, label) {
  const canvas = createCanvas(W, H);
  const ctx = canvas.getContext("2d");
  ctx.fillStyle = "#2a2218";
  ctx.fillRect(0, 0, W, H);
  const present = paths.filter(exists);
  const n = Math.max(present.length, 1);
  const cell = Math.min(140, Math.floor((W - 80) / Math.min(n, 6)));
  for (let i = 0; i < present.length; i++) {
    const img = await loadImage(present[i]);
    const col = i % 6;
    const row = Math.floor(i / 6);
    const x = 40 + col * (cell + 16);
    const y = 60 + row * (cell + 24);
    const s = Math.min(cell / img.width, cell / img.height);
    const dw = img.width * s;
    const dh = img.height * s;
    ctx.drawImage(img, x, y, dw, dh);
    ctx.fillStyle = "#c8b898";
    ctx.font = "11px UIArtCN, sans-serif";
    ctx.fillText(path.basename(present[i]).replace(/\.[^.]+$/, ""), x, y + cell + 14);
  }
  drawCallout(ctx, nr(0.04, 0.08, 0.92, 0.75), label);
  await savePng(canvas, outPath);
}

function defineRows() {
  return defineRowsFromModule({ TITLE, UI, SOCIAL, KEYART, BG, INTERVIEW_MOCKUP });
}

async function ensureImage(row, outDir) {
  const dirOut = outDir || REFS;
  fs.mkdirSync(dirOut, { recursive: true });
  const outPath = path.join(dirOut, `${row.imageId}.png`);
  // Prefer short callout on the PNG; keep long elementLabel for Excel only
  const label = row.callout || row.elementLabel;
  const g = row.gen;

  // Always regenerate so labels stay in sync with row definitions
  if (g.kind === "asset") {
    await annotateAsset(outPath, g.src, label);
  } else if (g.kind === "asset-region") {
    const src = exists(g.fallbackSrc) ? g.fallbackSrc : g.src;
    if (exists(g.fallbackSrc) && g.fallbackSrc !== g.src) {
      await annotateAsset(outPath, g.fallbackSrc, label);
    } else {
      await annotateAssetRegion(outPath, src, label, g.region);
    }
  } else if (g.kind === "asset-annotate-placeholder") {
    await annotateAsset(outPath, g.src, `${label}（${g.note}）`);
  } else if (g.kind === "collage") {
    await collageIcons(outPath, g.srcs, label);
  } else if (g.kind === "wire") {
    await generateWireframe(outPath, g.screen, g.region, label);
  } else if (g.kind === "mockup") {
    const src = g.src || INTERVIEW_MOCKUP;
    await annotateAssetRegion(outPath, src, label, g.region);
  } else if (g.kind === "bg-placeholder") {
    await generateBgPlaceholder(outPath, g.title || label, label, g.region);
  } else {
    throw new Error(`Unknown gen kind for row ${row.id}: ${g.kind}`);
  }

  if (!exists(outPath)) throw new Error(`Failed to write ${outPath}`);
  return outPath;
}

async function generateBgPlaceholder(outPath, title, label, region) {
  const canvas = createCanvas(W, H);
  const ctx = canvas.getContext("2d");
  const grd = ctx.createLinearGradient(0, 0, 0, H);
  grd.addColorStop(0, "#5a6a80");
  grd.addColorStop(0.55, "#3a4558");
  grd.addColorStop(1, "#1e2430");
  ctx.fillStyle = grd;
  ctx.fillRect(0, 0, W, H);
  ctx.strokeStyle = "#c9a05a";
  ctx.lineWidth = 2;
  ctx.strokeRect(40, 40, W - 80, H - 80);
  ctx.fillStyle = "#f0e6d0";
  ctx.font = "bold 20px UIArtCN, sans-serif";
  ctx.fillText("场景背景占位 · 待交稿", 60, 80);
  ctx.font = "14px UIArtCN, sans-serif";
  ctx.fillStyle = "#d0c4a8";
  fillTextBox(ctx, String(title || ""), 60, 110, W - 140, 14, "#d0c4a8");
  ctx.fillStyle = "rgba(255,255,255,0.35)";
  ctx.font = "11px UIArtCN, sans-serif";
  ctx.fillText("构图示意 · 非最终美术", 12, H - 12);
  drawCallout(ctx, region || nr(0.12, 0.25, 0.76, 0.5), label);
  await savePng(canvas, outPath);
}

function fillSheetRows(wb, ws, rows, imagePaths, imgW, imgH) {
  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    const excelRow = ws.getRow(i + 2);
    const vals = [
      row.id,
      row.name,
      row.where,
      row.status,
      row.urgency,
      row.deliverable,
      row.notes,
      row.elementLabel,
      "",
    ];
    vals.forEach((v, ci) => {
      const cell = excelRow.getCell(ci + 1);
      cell.value = v;
      cell.font = { name: "Microsoft YaHei", size: 10 };
      cell.alignment = { vertical: "middle", wrapText: true };
    });
    excelRow.height = 140;

    const imageId = wb.addImage({
      filename: imagePaths[i],
      extension: "png",
    });
    ws.addImage(imageId, {
      tl: { col: 8, row: i + 1 },
      ext: { width: imgW, height: imgH },
      editAs: "oneCell",
    });
  }
}

function styleHeaderRow(ws, headers, widths, fillArgb) {
  headers.forEach((h, i) => {
    ws.getColumn(i + 1).width = widths[i];
    const cell = ws.getRow(1).getCell(i + 1);
    cell.value = h;
    cell.font = { bold: true, name: "Microsoft YaHei", size: 11, color: { argb: "FFFFFFFF" } };
    cell.fill = { type: "pattern", pattern: "solid", fgColor: { argb: fillArgb } };
    cell.alignment = { vertical: "middle", wrapText: true };
  });
  ws.getRow(1).height = 28;
}

async function buildXlsx(uiRows, uiImages, sceneRows, sceneImages) {
  const wb = new ExcelJS.Workbook();
  wb.creator = "Street Cat Interview";
  wb.created = new Date();

  const intro = wb.addWorksheet("00_请先看这里", {
    properties: { defaultRowHeight: 18 },
  });
  intro.getColumn(1).width = 100;
  const introLines = [
    "《街角专访》第一章 · 美术需求清单（给画师 · UI + 场景背景）",
    "",
    "本表为画师主交付清单（canonical）。旧名「UI界面需求清单_给画师.xlsx」为同内容副本。",
    "",
    "怎么用",
    "1. 「01_UI界面」——界面控件/纸片/手机框等；自由采访为左状态+立绘 / 中聊天 / 右灵感+工具栏 三栏。",
    "2. 「02_场景背景」——全屏剧情/调查背景图（建议 1920×1080）。",
    "3. 「界面元素标注 / 画面构图标注」用大白话说明画什么、在屏幕哪、不要画什么；红/橙框以参考图为准。",
    "4. 参考图原文件：Docs/art/ui-refs/ 、Docs/art/scene-refs/",
    "5. 线框/标注图 ≠ 最终风格；「已有资源（可替换）」表示盘上已有 PNG，可按新风格重画替换。",
    "",
    "列含义",
    "序号 | 场景名称 | 用在哪里 | 当前状态 | 紧急程度 | 交付文件名（英文） | 参考/说明 | 界面元素标注 | 参考图",
    "",
    "UI 紧急程度：高 / 中 / 低　　场景紧急程度：P0 / P1",
    "当前状态：已有资源（可替换）/ 程序色块 / 占位复用 / 缺图 / 过时勿交 / 仅开发 等",
  ];
  introLines.forEach((line, i) => {
    const r = intro.getRow(i + 1);
    r.getCell(1).value = line;
    if (i === 0) r.font = { bold: true, size: 14, name: "Microsoft YaHei" };
    else r.font = { size: 11, name: "Microsoft YaHei" };
  });

  const headers = [
    "序号",
    "场景名称",
    "用在哪里",
    "当前状态",
    "紧急程度",
    "交付文件名（英文）",
    "参考/说明",
    "界面元素标注",
    "参考图",
  ];
  const widths = [6, 24, 28, 18, 10, 42, 40, 52, 48];
  const imgH = 118;
  const imgW = 210;

  const wsUi = wb.addWorksheet("01_UI界面", {
    views: [{ state: "frozen", ySplit: 1 }],
  });
  styleHeaderRow(wsUi, headers, widths, "FF5C4030");
  fillSheetRows(wb, wsUi, uiRows, uiImages, imgW, imgH);

  const sceneHeaders = [
    "序号",
    "场景名称",
    "用在哪里",
    "当前状态",
    "紧急程度",
    "交付文件名（英文）",
    "参考/说明",
    "画面/构图标注",
    "参考图",
  ];
  const wsSc = wb.addWorksheet("02_场景背景", {
    views: [{ state: "frozen", ySplit: 1 }],
  });
  styleHeaderRow(wsSc, sceneHeaders, widths, "FF2F4A3C");
  fillSheetRows(wb, wsSc, sceneRows, sceneImages, imgW, imgH);

  const idx = wb.addWorksheet("03_参考图索引");
  ["分册", "序号", "场景名称", "本地文件名", "相对路径", "标注摘要"].forEach((h, i) => {
    idx.getColumn(i + 1).width = [10, 8, 24, 36, 42, 50][i];
    const cell = idx.getRow(1).getCell(i + 1);
    cell.value = h;
    cell.font = { bold: true, name: "Microsoft YaHei" };
  });
  let ri = 2;
  const pushIdx = (book, rows, relDir) => {
    rows.forEach((row) => {
      const r = idx.getRow(ri++);
      r.getCell(1).value = book;
      r.getCell(2).value = row.id;
      r.getCell(3).value = row.name;
      r.getCell(4).value = `${row.imageId}.png`;
      r.getCell(5).value = `Docs/art/${relDir}/${row.imageId}.png`;
      r.getCell(6).value = row.elementLabel;
      for (let c = 1; c <= 6; c++) {
        r.getCell(c).font = { name: "Microsoft YaHei", size: 10 };
        r.getCell(c).alignment = { wrapText: true, vertical: "middle" };
      }
      r.height = 40;
    });
  };
  pushIdx("UI", uiRows, "ui-refs");
  pushIdx("场景", sceneRows, "scene-refs");

  await wb.xlsx.writeFile(OUT_XLSX);
  fs.copyFileSync(OUT_XLSX, OUT_XLSX_ALIAS);
}

async function main() {
  registerFonts();
  fs.mkdirSync(REFS, { recursive: true });
  fs.mkdirSync(SCENE_REFS, { recursive: true });

  // Clean obsolete interview wireframe filenames from prior list
  const obsolete = [
    "29_interview_notepad.png",
    "30_interview_meters.png",
    "31_interview_chips.png",
    "32_interview_actions.png",
    "33_interview_companion.png",
    "41_article_preview.png",
  ];
  for (const f of obsolete) {
    const fp = path.join(REFS, f);
    if (exists(fp)) fs.unlinkSync(fp);
  }

  const uiRows = defineRows();
  const sceneRows = defineSceneRows({ BG });

  console.log(`Generating ${uiRows.length} UI refs…`);
  const uiImages = [];
  for (const row of uiRows) {
    process.stdout.write(`  [UI ${pad(row.id)}] ${row.imageId} … `);
    uiImages.push(await ensureImage(row, REFS));
    console.log("ok");
  }

  console.log(`Generating ${sceneRows.length} scene refs…`);
  const sceneImages = [];
  for (const row of sceneRows) {
    process.stdout.write(`  [BG ${pad(row.id)}] ${row.imageId} … `);
    sceneImages.push(await ensureImage(row, SCENE_REFS));
    console.log("ok");
  }

  console.log("Writing workbook…");
  await buildXlsx(uiRows, uiImages, sceneRows, sceneImages);

  console.log("");
  console.log("Wrote", OUT_XLSX);
  console.log("Alias", OUT_XLSX_ALIAS);
  console.log("UI images", REFS, "count", uiRows.length);
  console.log("Scene images", SCENE_REFS, "count", sceneRows.length);
  console.log("Open tip: Excel / WPS → 01_UI界面 / 02_场景背景");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
