/**
 * Artist-facing UI art requirements with embedded annotated reference images.
 * Run from Docs/art:  node build-ui-art-refs-xlsx.mjs
 *
 * Outputs:
 *   Docs/art/ui-refs/NN_english_id.png
 *   Docs/art/UI界面需求清单_给画师.xlsx
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import ExcelJS from "exceljs";
import { createCanvas, loadImage, GlobalFonts } from "@napi-rs/canvas";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(__dirname, "../..");
const REFS = path.join(__dirname, "ui-refs");
const OUT_XLSX = path.join(__dirname, "UI界面需求清单_给画师.xlsx");

const TITLE = path.join(REPO, "Assets/Resources/VnArt/Title");
const UI = path.join(REPO, "Assets/Resources/VnArt/UI");
const SOCIAL = path.join(UI, "Social");
const KEYART = path.join(REPO, "Assets/Resources/VnArt/KeyArt");
const BG = path.join(REPO, "Assets/Resources/VnArt/Backgrounds");

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
      // hotspots
      [[0.25, 0.35], [0.45, 0.5], [0.65, 0.4], [0.55, 0.65]].forEach(([x, y], i) => {
        const p = toPx(nr(x, y, 0.06, 0.08));
        ctx.strokeStyle = "rgba(255,220,80,0.8)";
        ctx.lineWidth = 2;
        ctx.strokeRect(p.x, p.y, p.w, p.h);
        ctx.fillStyle = "rgba(255,220,80,0.3)";
        ctx.fillRect(p.x, p.y, p.w, p.h);
        ctx.fillStyle = "#fff";
        ctx.font = "11px UIArtCN, sans-serif";
        ctx.fillText(String(i + 1), p.x + 8, p.y + 20);
      });
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
      hotspot: nr(0.45, 0.5, 0.06, 0.08),
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
      drawChromeFrame(ctx, "Mode.Interview");
      // meters
      for (let i = 0; i < 3; i++) {
        const m = toPx(nr(0.72, 0.1 + i * 0.08, 0.24, 0.06));
        ctx.fillStyle = "#f5e6c8";
        roundRect(ctx, m.x, m.y, m.w, m.h, 3);
        ctx.fill();
        ctx.fillStyle = "#5a4030";
        ctx.font = "11px UIArtCN, sans-serif";
        ctx.fillText(["信任", "压力", "专注"][i], m.x + 8, m.y + 20);
      }
      // portrait
      ctx.fillStyle = "rgba(180,160,140,0.55)";
      ctx.fillRect(W * 0.35, H * 0.12, W * 0.3, H * 0.45);
      ctx.fillStyle = "#fff";
      ctx.font = "13px UIArtCN, sans-serif";
      ctx.fillText("受访者立绘", W * 0.42, H * 0.35);
      // companion
      const cp = toPx(nr(0.04, 0.55, 0.14, 0.28));
      ctx.fillStyle = "rgba(160,140,120,0.6)";
      ctx.fillRect(cp.x, cp.y, cp.w, cp.h);
      ctx.fillStyle = "#fff";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("伴宠", cp.x + 20, cp.y + cp.h / 2);
      // notepad
      const np = toPx(nr(0.2, 0.62, 0.6, 0.22));
      ctx.fillStyle = "#2a2218";
      roundRect(ctx, np.x, np.y, np.w, np.h, 4);
      ctx.fill();
      ctx.fillStyle = "#c8b898";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("采访便签本 / 输入区", np.x + 16, np.y + 36);
      // chips
      for (let i = 0; i < 3; i++) {
        const c = toPx(nr(0.22 + i * 0.2, 0.88, 0.16, 0.05));
        ctx.fillStyle = "rgba(50,40,30,0.9)";
        roundRect(ctx, c.x, c.y, c.w, c.h, 4);
        ctx.fill();
        ctx.fillStyle = "#e8d8c0";
        ctx.font = "11px UIArtCN, sans-serif";
        ctx.fillText(`提问${i + 1}`, c.x + 20, c.y + 20);
      }
      // action btns
      const ab = toPx(nr(0.82, 0.72, 0.14, 0.1));
      ctx.fillStyle = "#8a6040";
      roundRect(ctx, ab.x, ab.y, ab.w, ab.h, 4);
      ctx.fill();
      ctx.fillStyle = "#fff";
      ctx.font = "11px UIArtCN, sans-serif";
      ctx.fillText("发送/结束", ab.x + 12, ab.y + 30);
    },
    regions: {
      notepad: nr(0.2, 0.62, 0.6, 0.22),
      meters: nr(0.72, 0.1, 0.24, 0.22),
      chips: nr(0.22, 0.88, 0.56, 0.05),
      actions: nr(0.82, 0.72, 0.14, 0.1),
      companion: nr(0.04, 0.55, 0.14, 0.28),
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
      // left paper
      const paper = toPx(nr(0.03, 0.05, 0.62, 0.9));
      ctx.fillStyle = "#e8e0d0";
      ctx.fillRect(paper.x, paper.y, paper.w, paper.h);
      ctx.fillStyle = "#3a3020";
      ctx.font = "16px UIArtCN, sans-serif";
      ctx.fillText("槐安社区特稿（字体）", paper.x + 20, paper.y + 36);
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillStyle = "#5a5040";
      ctx.fillText("成稿滚动编辑区", paper.x + 20, paper.y + 80);
      // draft area
      const draft = toPx(nr(0.05, 0.22, 0.55, 0.55));
      ctx.strokeStyle = "#a09070";
      ctx.strokeRect(draft.x, draft.y, draft.w, draft.h);
      // right panel
      const right = toPx(nr(0.68, 0.05, 0.29, 0.9));
      ctx.fillStyle = "#243040";
      ctx.fillRect(right.x, right.y, right.w, right.h);
      ctx.fillStyle = "#c8d0d8";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("素材列表 / 状态", right.x + 12, right.y + 30);
      // buttons
      for (let i = 0; i < 3; i++) {
        const b = toPx(nr(0.7, 0.7 + i * 0.08, 0.24, 0.06));
        ctx.fillStyle = "#c8a060";
        roundRect(ctx, b.x, b.y, b.w, b.h, 3);
        ctx.fill();
      }
      ctx.fillStyle = "rgba(255,255,255,0.4)";
      ctx.font = "12px UIArtCN, sans-serif";
      ctx.fillText("WritingDeskOverlay", 12, H - 12);
    },
    regions: {
      paper: nr(0.03, 0.05, 0.62, 0.9),
      draft: nr(0.05, 0.22, 0.55, 0.55),
      right: nr(0.68, 0.05, 0.29, 0.9),
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

/**
 * Row definition:
 * id, name, where, status, urgency, deliverable, notes, elementLabel, imageId,
 * gen — generation recipe
 */
function defineRows() {
  const t = (name) => path.join(TITLE, name);
  const u = (name) => path.join(UI, name);
  const s = (name) => path.join(SOCIAL, name);
  const b = (name) => path.join(BG, name);
  const k = (name) => path.join(KEYART, name);

  return [
    // —— 标题 / 主菜单 ——
    {
      id: 1, name: "标题·木桌全屏底", where: "标题/主菜单（Mode.Title）", status: "已有资源", urgency: "高",
      deliverable: "title_desk_bg.png", notes: "VnArt/Title；ShowTitle 全屏桌面",
      elementLabel: "标题屏最底层全屏木桌背景，杂志与桌面道具叠在其上",
      imageId: "01_title_desk_bg",
      gen: { kind: "asset", src: t("title_desk_bg.png") },
    },
    {
      id: 2, name: "标题·展开杂志本体", where: "标题/主菜单中央", status: "已有资源", urgency: "高",
      deliverable: "title_magazine_open.png", notes: "左品牌页+右目录页底板",
      elementLabel: "中央展开杂志本体底板（左右页纸面），不含左页插画与按钮",
      imageId: "02_title_magazine_open",
      gen: { kind: "asset", src: t("title_magazine_open.png") },
    },
    {
      id: 3, name: "标题·杂志阴影", where: "标题/主菜单", status: "已有资源", urgency: "中",
      deliverable: "title_magazine_shadow.png", notes: "半透明叠在杂志下",
      elementLabel: "杂志下方半透明投影层，略偏移叠在桌面与杂志之间",
      imageId: "03_title_magazine_shadow",
      gen: { kind: "asset", src: t("title_magazine_shadow.png") },
    },
    {
      id: 4, name: "标题·左页插画", where: "标题杂志左页", status: "已有资源", urgency: "高",
      deliverable: "title_feature_art.png", notes: "品牌插画区",
      elementLabel: "杂志左页中上部品牌插画区，非 Logo、非引语框",
      imageId: "04_title_feature_art",
      gen: { kind: "asset-region", src: t("title_magazine_open.png"), region: nr(0.05, 0.35, 0.42, 0.5),
        fallbackSrc: t("title_feature_art.png") },
    },
    {
      id: 5, name: "标题·中文 Logo", where: "标题品牌", status: "已有资源", urgency: "高",
      deliverable: "title_logo_cn.png", notes: "英文化时隐藏，改用字体「街角专访」",
      elementLabel: "左页顶部中文 Logo 图形条，非正文引语",
      imageId: "05_title_logo_cn",
      gen: { kind: "asset", src: t("title_logo_cn.png") },
    },
    {
      id: 6, name: "标题·英文 Logo 条", where: "标题品牌", status: "已有资源", urgency: "中",
      deliverable: "title_logo_en.png", notes: "VnArt/Title",
      elementLabel: "左页中文 Logo 下方的英文 Logo 条",
      imageId: "06_title_logo_en",
      gen: { kind: "asset", src: t("title_logo_en.png") },
    },
    {
      id: 7, name: "标题·左页引语框", where: "标题杂志左页", status: "已有资源", urgency: "中",
      deliverable: "title_quote_box_l.png", notes: "框内文案用字体+Loc，勿画字",
      elementLabel: "左页下部引语装饰外框，框内文字用字体，勿预渲染文案",
      imageId: "07_title_quote_box_l",
      gen: { kind: "asset", src: t("title_quote_box_l.png") },
    },
    {
      id: 8, name: "标题·右页目录页眉", where: "标题杂志右页", status: "已有资源", urgency: "中",
      deliverable: "title_contents_header.png", notes: "「CONTENTS/目录」用字体",
      elementLabel: "右页顶部 CONTENTS/目录页眉装饰线，标题字用字体",
      imageId: "08_title_contents_header",
      gen: { kind: "asset", src: t("title_contents_header.png") },
    },
    {
      id: 9, name: "标题·引语装饰", where: "标题杂志左页", status: "已有资源", urgency: "低",
      deliverable: "title_blurb_deco.png", notes: "可选装饰",
      elementLabel: "左页引语旁小块装饰贴图，非主插画",
      imageId: "09_title_blurb_deco",
      gen: { kind: "asset", src: t("title_blurb_deco.png") },
    },
    {
      id: 10, name: "标题·胶带主按钮底", where: "标题菜单主操作", status: "已有资源", urgency: "高",
      deliverable: "btn_tape_primary_idle.png / btn_tape_primary_hover.png", notes: "pressed 复用 hover；按钮字用字体",
      elementLabel: "右页主操作胶带按钮底板（idle/hover），不含按钮文字",
      imageId: "10_btn_tape_primary",
      gen: { kind: "collage", srcs: [t("btn_tape_primary_idle.png"), t("btn_tape_primary_hover.png")] },
    },
    {
      id: 11, name: "标题·胶带次按钮底", where: "标题/笔记/写稿多处复用", status: "已有资源", urgency: "高",
      deliverable: "btn_tape_idle.png / btn_tape_hover.png / btn_tape_pressed.png", notes: "全游戏 scrapbook 风按钮底",
      elementLabel: "全游戏复用的次级胶带按钮底板三态，不含文字",
      imageId: "11_btn_tape",
      gen: { kind: "collage", srcs: [t("btn_tape_idle.png"), t("btn_tape_hover.png"), t("btn_tape_pressed.png")] },
    },
    {
      id: 12, name: "标题·功能图标组", where: "标题按钮旁图标", status: "已有资源", urgency: "高",
      deliverable: "icon_play / icon_cassette / icon_doc / icon_map / icon_gear / icon_exit", notes: "新游戏/继续/读档/清档/设置/退出",
      elementLabel: "标题菜单胶带按钮左侧功能小图标组（非按钮底）",
      imageId: "12_title_icons",
      gen: {
        kind: "collage",
        srcs: ["icon_play", "icon_cassette", "icon_doc", "icon_map", "icon_gear", "icon_exit"].map((n) => t(`${n}.png`)),
      },
    },
    {
      id: 13, name: "标题·回形针装饰", where: "标题/笔记/采访/写稿", status: "已有资源", urgency: "中",
      deliverable: "deco_paperclip.png", notes: "多处复用",
      elementLabel: "回形针装饰贴图，笔记/采访/写稿边角复用",
      imageId: "13_deco_paperclip",
      gen: { kind: "asset", src: t("deco_paperclip.png") },
    },
    {
      id: 14, name: "标题桌面·采访本道具", where: "主菜单桌面；可点开笔记", status: "已有资源", urgency: "中",
      deliverable: "prop_field_notes.png", notes: "VnArt/Title 桌面道具",
      elementLabel: "标题木桌上可点击的采访本道具，非杂志本体",
      imageId: "14_prop_field_notes",
      gen: { kind: "asset", src: t("prop_field_notes.png") },
    },
    {
      id: 15, name: "标题桌面·翻译器等散件", where: "主菜单桌面装饰", status: "已有资源", urgency: "中",
      deliverable: "prop_translator / prop_polaroid_a / prop_polaroid_b / prop_scraps", notes: "装饰道具组",
      elementLabel: "标题木桌装饰散件组（翻译器/拍立得/散页），非可交互杂志",
      imageId: "15_title_desk_props",
      gen: {
        kind: "collage",
        srcs: [t("prop_translator.png"), t("prop_polaroid_a.png"), t("prop_polaroid_b.png"), t("prop_scraps.png")],
      },
    },
    {
      id: 16, name: "标题品牌 KeyArt", where: "标题/品牌全屏解析目标", status: "已有资源", urgency: "中",
      deliverable: "kv_title_street_interview.png", notes: "VnArt/KeyArt；现行主菜单仍以杂志拼贴为主",
      elementLabel: "品牌全屏主视觉 KeyArt，非标题杂志拼贴层",
      imageId: "16_kv_title_street_interview",
      gen: { kind: "asset", src: k("kv_title_street_interview.png") },
    },
    {
      id: 17, name: "过时标题文字图", where: "（旧方案，勿再交）", status: "过时勿交", urgency: "低",
      deliverable: "title_txt_* / title_btn_*", notes: "已改字体+Loc；Art 仍残留 PNG",
      elementLabel: "过时预渲染标题文字/按钮字图——请勿再交付，仅作反例标注",
      imageId: "17_obsolete_title_txt",
      gen: {
        kind: "collage",
        srcs: [t("title_txt_contents.png"), t("title_txt_subtitle.png"), t("title_btn_01_newgame.png")],
      },
    },

    // —— VN 对白 / 通用 chrome ——
    {
      id: 18, name: "深色纸纹纹理", where: "对白盒/笔记/采访便签/写稿纸面", status: "已有资源", urgency: "高",
      deliverable: "tex_paper_dark.png", notes: "VnArt/UI；缺时回退纯色",
      elementLabel: "深色可平铺纸纹——对白盒/笔记/采访便签/写稿纸面底纹，非姓名牌",
      imageId: "18_tex_paper_dark",
      gen: { kind: "asset", src: u("tex_paper_dark.png") },
    },
    {
      id: 19, name: "VN 对话框外框", where: "全流程对白（Mode.Dialogue）", status: "程序色块", urgency: "中",
      deliverable: "ui_dialogue_frame.png（建议）", notes: "DialogueBox+NamePlate；可选九宫格升级",
      elementLabel: "对白盒外框+姓名牌整体 chrome，非对白盒内部纸纹填充",
      imageId: "19_ui_dialogue_frame",
      gen: { kind: "wire", screen: "dialogue", region: "frame" },
    },
    {
      id: 20, name: "选项条按钮底", where: "剧本 choices / 交谈 / 立意", status: "程序色块", urgency: "中",
      deliverable: "ui_choice_btn.png（建议）", notes: "ChoiceHost；可胶带风统一",
      elementLabel: "对白右侧/上方选项条按钮底板，非对白正文区",
      imageId: "20_ui_choice_btn",
      gen: { kind: "wire", screen: "dialogue", region: "choice" },
    },
    {
      id: 21, name: "顶栏 HUD（TopBar）", where: "全流程（标题屏隐藏）", status: "程序色块", urgency: "中",
      deliverable: "ui_topbar_chip.png（建议）", notes: "章节 chip / 目标行 / 回看·菜单",
      elementLabel: "舞台上方 TopBar：章节 chip、目标行、回看与菜单按钮带",
      imageId: "21_ui_topbar",
      gen: { kind: "wire", screen: "dialogue", region: "topbar" },
    },
    {
      id: 22, name: "Letterbox 黑边", where: "对白/调查/采访等舞台感", status: "程序色块", urgency: "低",
      deliverable: "（无需图 / 或 ui_letterbox.png）", notes: "上下黑边+琥珀细线；现程序绘制",
      elementLabel: "画面上下 Letterbox 黑边与琥珀细线，非 TopBar 内容",
      imageId: "22_ui_letterbox",
      gen: { kind: "wire", screen: "dialogue", region: "letterbox" },
    },
    {
      id: 23, name: "场景名 Toast", where: "进场短暂提示", status: "程序色块", urgency: "低",
      deliverable: "（无需图）", notes: "Location toast；字体即可",
      elementLabel: "进场短暂场景名 Toast 条，位于 TopBar 下方中央",
      imageId: "23_location_toast",
      gen: { kind: "wire", screen: "dialogue", region: "toast" },
    },
    {
      id: 24, name: "隐藏对白按钮", where: "对白/交谈等", status: "程序色块", urgency: "低",
      deliverable: "（无需图）", notes: "右下角；字体按钮",
      elementLabel: "右下角「隐藏对白」小按钮，非选项条",
      imageId: "24_hide_dialogue_btn",
      gen: { kind: "wire", screen: "dialogue", region: "hide" },
    },

    // —— 调查 / 交谈 ——
    {
      id: 25, name: "调查地图界面", where: "SC-04 调查（Mode.Investigate）", status: "已有资源", urgency: "高",
      deliverable: "bg_huaian_map.png", notes: "平面图 BG；透明热点点选",
      elementLabel: "调查全屏地图背景平面图，不含底栏动作芯片与程序条",
      imageId: "25_bg_huaian_map",
      gen: { kind: "asset", src: b("bg_huaian_map.png") },
    },
    {
      id: 26, name: "调查热点角标（可选）", where: "地图已调查状态", status: "文档标缺", urgency: "低",
      deliverable: "ui_hotspot_checked.png（建议）", notes: "现仅透明点击；P2 可选",
      elementLabel: "地图热点「已调查」角标小图标，叠在透明点击区角上",
      imageId: "26_ui_hotspot_checked",
      gen: { kind: "wire", screen: "investigate", region: "hotspot" },
    },
    {
      id: 27, name: "调查底栏动作芯片", where: "调查地图底栏", status: "程序色块", urgency: "中",
      deliverable: "ui_investigate_chip.png（建议）", notes: "与保安交谈/等待大福/笔记/菜单等",
      elementLabel: "调查地图底部动作芯片条（交谈/等待/笔记/菜单）",
      imageId: "27_ui_investigate_chip",
      gen: { kind: "wire", screen: "investigate", region: "chip" },
    },
    {
      id: 28, name: "交谈话题菜单", where: "保安交谈 / 后采访核实（Mode.Talk）", status: "程序色块", urgency: "中",
      deliverable: "（复用选项条）", notes: "ShowTalkMenu；对白 chrome + AddChoice",
      elementLabel: "交谈模式话题选项列表区，复用选项条样式",
      imageId: "28_talk_topics",
      gen: { kind: "wire", screen: "talk", region: "topics" },
    },

    // —— 自由采访 ——
    {
      id: 29, name: "采访便签本底板", where: "自由采访（Mode.Interview）", status: "已有资源", urgency: "高",
      deliverable: "tex_paper_dark.png（复用）", notes: "InterviewOverlay 底部便签本",
      elementLabel: "采访界面底部便签本/输入区纸面底板，非右上角 meter",
      imageId: "29_interview_notepad",
      gen: { kind: "wire", screen: "interview", region: "notepad" },
    },
    {
      id: 30, name: "采访信赖/压力条", where: "采访右上信任便签", status: "程序色块", urgency: "中",
      deliverable: "ui_interview_trust_meter.png（建议）", notes: "五段信任/压力/专注",
      elementLabel: "采访右上角信任/压力/专注便签条 meter，非底部提问芯片",
      imageId: "30_interview_meters",
      gen: { kind: "wire", screen: "interview", region: "meters" },
    },
    {
      id: 31, name: "采访提问芯片", where: "采访底部芯片区", status: "程序色块", urgency: "中",
      deliverable: "ui_interview_chip.png（建议）", notes: "最多 3 枚建议问法",
      elementLabel: "采访底部最多三枚建议提问芯片，非发送按钮",
      imageId: "31_interview_chips",
      gen: { kind: "wire", screen: "interview", region: "chips" },
    },
    {
      id: 32, name: "采访发送/结束按钮", where: "采访动作行", status: "程序色块", urgency: "中",
      deliverable: "（复用胶带钮或色块）", notes: "发送、结束采访、返回写稿",
      elementLabel: "采访动作行发送/结束/返回写稿按钮区",
      imageId: "32_interview_actions",
      gen: { kind: "wire", screen: "interview", region: "actions" },
    },
    {
      id: 33, name: "采访伴宠立绘槽", where: "采访左下伴宠", status: "已有资源", urgency: "高",
      deliverable: "（立绘 ch_*，非 UI）", notes: "CompanionPortrait；UI 槽位程序布局",
      elementLabel: "采访左下伴宠立绘槽位（占位框），交付物为角色立绘非 UI 框",
      imageId: "33_interview_companion",
      gen: { kind: "wire", screen: "interview", region: "companion" },
    },

    // —— 笔记 ——
    {
      id: 34, name: "记者笔记桌面", where: "NotebookOverlay 全屏", status: "已有资源", urgency: "高",
      deliverable: "tex_paper_dark.png + deco_paperclip + btn_tape_*", notes: "深色桌面+线纹页",
      elementLabel: "记者笔记全屏桌面与线纹页组合，非单张便利贴",
      imageId: "34_notebook_desk",
      gen: { kind: "wire", screen: "notebook", region: "desk" },
    },
    {
      id: 35, name: "笔记专用封面插画", where: "记者笔记", status: "文档标缺", urgency: "低",
      deliverable: "ui_notebook_cover.png（建议）", notes: "可后补；现胶带+回形针够用",
      elementLabel: "笔记可选封面/页眉插画装饰区，非主题便利贴网格",
      imageId: "35_notebook_cover",
      gen: { kind: "wire", screen: "notebook", region: "cover" },
    },
    {
      id: 36, name: "笔记主题便利贴", where: "笔记左栏主题网格", status: "程序色块", urgency: "中",
      deliverable: "ui_notebook_sticky.png（建议）", notes: "现色块贴；可复用大福小图标",
      elementLabel: "笔记左栏主题便利贴网格卡片，非右侧正文页",
      imageId: "36_notebook_sticky",
      gen: { kind: "wire", screen: "notebook", region: "sticky" },
    },

    // —— 写稿 ——
    {
      id: 37, name: "写稿立意选择", where: "SC-10 写稿入口", status: "程序色块", urgency: "高",
      deliverable: "（复用对白+选项）", notes: "ShowWritingDirectionPick",
      elementLabel: "写稿入口两大立意选项条，复用对白+选项 chrome",
      imageId: "37_writing_direction",
      gen: { kind: "wire", screen: "writing_pick", region: "choices" },
    },
    {
      id: 38, name: "写稿素材软木板", where: "WritingMaterialsOverlay", status: "程序色块", urgency: "高",
      deliverable: "ui_corkboard.png（建议）", notes: "文档标缺专用贴图；现程序软木色",
      elementLabel: "写稿素材库全屏软木板背景，不含单张素材卡面",
      imageId: "38_ui_corkboard",
      gen: { kind: "wire", screen: "corkboard", region: "board" },
    },
    {
      id: 39, name: "写稿素材卡面", where: "素材卡库网格/详情", status: "程序色块", urgency: "中",
      deliverable: "ui_material_card.png（建议）", notes: "编号/标签/锁定态",
      elementLabel: "软木板上单张素材卡面（编号/标签/锁定），非整板背景",
      imageId: "39_ui_material_card",
      gen: { kind: "wire", screen: "corkboard", region: "card" },
    },
    {
      id: 40, name: "写稿台·报纸成稿", where: "WritingDeskOverlay", status: "程序色块", urgency: "高",
      deliverable: "ui_writing_desk_paper.png（建议）", notes: "深蓝桌+大稿纸；栏头用字体",
      elementLabel: "写稿台左侧成稿滚动区纸张，非右侧素材列表栏",
      imageId: "40_writing_desk_paper",
      gen: { kind: "wire", screen: "writing_desk", region: "draft" },
    },
    {
      id: 41, name: "文章预览叠层", where: "素材板「预览文章」", status: "程序色块", urgency: "中",
      deliverable: "ui_article_preview_sheet.png（建议）", notes: "半透明遮罩+中央纸页",
      elementLabel: "文章预览叠层中央纸页，非写稿台编辑区",
      imageId: "41_article_preview",
      gen: { kind: "wire", screen: "article_preview", region: "sheet" },
    },
    {
      id: 42, name: "沈禾审核反馈屏", where: "提交主编后（Writing 对白态）", status: "程序色块", urgency: "中",
      deliverable: "（无需专用 UI 图）", notes: "复用对白盒；办公室 BG 已有",
      elementLabel: "审稿反馈复用对白盒+办公室背景，无独立审核面板",
      imageId: "42_review_feedback",
      gen: { kind: "wire", screen: "dialogue", region: "frame" },
    },
    {
      id: 43, name: "重新采访菜单", where: "写稿补访子流程", status: "程序色块", urgency: "中",
      deliverable: "（复用选项条）", notes: "ShowReInterviewMenu",
      elementLabel: "写稿补访「重新采访」选项菜单，复用选项条",
      imageId: "43_reinterview_menu",
      gen: { kind: "wire", screen: "dialogue", region: "choice" },
    },

    // —— 社交 ——
    {
      id: 44, name: "社交手机叠层框", where: "SC-03 选题（SocialOverlay）", status: "程序色块", urgency: "中",
      deliverable: "ui_phone_frame.png（建议）", notes: "现为居中矩形层；无独立手机壳图",
      elementLabel: "SC-03 舞台中央手机外框，不含帖子内容图",
      imageId: "44_ui_phone_frame",
      gen: { kind: "wire", screen: "social", region: "phone" },
    },
    {
      id: 45, name: "社交帖·信息流 01", where: "SC-03 手机 feed", status: "已有资源", urgency: "高",
      deliverable: "social_post_01_feed.png", notes: "VnArt/UI/Social/",
      elementLabel: "手机信息流内第 1 条帖子整卡内容图，非手机外框",
      imageId: "45_social_post_01_feed",
      gen: { kind: "asset", src: s("social_post_01_feed.png") },
    },
    {
      id: 46, name: "社交帖·信息流 02", where: "SC-03 手机 feed", status: "已有资源", urgency: "高",
      deliverable: "social_post_02_feed.png", notes: "VnArt/UI/Social/",
      elementLabel: "手机信息流内第 2 条帖子整卡内容图，非手机外框",
      imageId: "46_social_post_02_feed",
      gen: { kind: "asset", src: s("social_post_02_feed.png") },
    },
    {
      id: 47, name: "社交帖·信息流 03", where: "SC-03 手机 feed", status: "已有资源", urgency: "高",
      deliverable: "social_post_03_feed.png", notes: "VnArt/UI/Social/",
      elementLabel: "手机信息流内第 3 条帖子整卡内容图，非手机外框",
      imageId: "47_social_post_03_feed",
      gen: { kind: "asset", src: s("social_post_03_feed.png") },
    },
    {
      id: 48, name: "社交帖·详情 03", where: "SC-03 手机 detail", status: "已有资源", urgency: "高",
      deliverable: "social_post_03_detail.png", notes: "详情略放大展示",
      elementLabel: "帖子 03 详情放大页内容图，非 feed 缩略卡、非手机壳",
      imageId: "48_social_post_03_detail",
      gen: { kind: "asset", src: s("social_post_03_detail.png") },
    },

    // —— 后日谈 / 菜单 overlays ——
    {
      id: 49, name: "后日谈·文章发布页", where: "Mode.Epilogue 开场", status: "占位复用背景", urgency: "中",
      deliverable: "bg_article_published.png（建议）", notes: "现占位 bg_huaian_afternoon",
      elementLabel: "后日谈开场「文章发布页」全屏背景（专栏/网页感），非对白盒",
      imageId: "49_bg_article_published",
      gen: {
        kind: "asset-annotate-placeholder",
        src: b("bg_huaian_afternoon.png"),
        note: "现占位：槐安午后 → 需换专栏发布页",
      },
    },
    {
      id: 50, name: "章节结束按钮", where: "后日谈收束", status: "程序色块", urgency: "低",
      deliverable: "（字体即可）", notes: "「第一章 完」；勿画成图片字",
      elementLabel: "后日谈收束「第一章 完」字体按钮，勿画成图片字",
      imageId: "50_chapter_end_btn",
      gen: { kind: "wire", screen: "epilogue", region: "endbtn" },
    },
    {
      id: 51, name: "暂停菜单面板", where: "MenuOverlay", status: "程序色块", urgency: "中",
      deliverable: "ui_menu_panel.png（建议）", notes: "dim+纸质中央板",
      elementLabel: "暂停菜单中央纸质面板（继续/回看/存读档等），非设置页",
      imageId: "51_ui_menu_panel",
      gen: { kind: "wire", screen: "menu", region: "panel" },
    },
    {
      id: 52, name: "对话回看面板", where: "BacklogOverlay", status: "程序色块", urgency: "中",
      deliverable: "ui_backlog_panel.png（建议）", notes: "大纸板+滚动历史",
      elementLabel: "对话回看大纸板滚动历史面板，非暂停菜单",
      imageId: "52_ui_backlog_panel",
      gen: { kind: "wire", screen: "backlog", region: "panel" },
    },
    {
      id: 53, name: "存档/读档面板", where: "SaveLoadOverlay", status: "程序色块", urgency: "中",
      deliverable: "ui_saveload_panel.png（建议）", notes: "槽位列表",
      elementLabel: "存档/读档槽位列表面板，非覆盖确认小窗",
      imageId: "53_ui_saveload_panel",
      gen: { kind: "wire", screen: "saveload", region: "panel" },
    },
    {
      id: 54, name: "覆盖确认小面板", where: "ConfirmOverlay", status: "程序色块", urgency: "低",
      deliverable: "ui_confirm_panel.png（建议）", notes: "覆盖存档确认/取消",
      elementLabel: "覆盖存档确认/取消小面板，非完整存档列表",
      imageId: "54_ui_confirm_panel",
      gen: { kind: "wire", screen: "confirm", region: "panel" },
    },
    {
      id: 55, name: "设置面板", where: "SettingsOverlay", status: "程序色块", urgency: "中",
      deliverable: "ui_settings_panel.png（建议）", notes: "语言/字体/音量等；入口 icon_gear 已有",
      elementLabel: "设置面板整体（语言/字体/音量/语速/全屏），非齿轮入口图标",
      imageId: "55_ui_settings_panel",
      gen: { kind: "wire", screen: "settings", region: "panel" },
    },
    {
      id: 56, name: "Debug 跳转面板", where: "仅 Editor / Development", status: "仅开发", urgency: "低",
      deliverable: "（无需美术）", notes: "DebugJumpPanel F9；非正式玩家界面",
      elementLabel: "仅开发用 Debug 跳转面板（F9），无需美术交付",
      imageId: "56_debug_jump_panel",
      gen: { kind: "wire", screen: "debug", region: "panel" },
    },
  ];
}

async function ensureImage(row) {
  const outPath = path.join(REFS, `${row.imageId}.png`);
  const label = row.elementLabel;
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
  } else {
    throw new Error(`Unknown gen kind for row ${row.id}: ${g.kind}`);
  }

  if (!exists(outPath)) throw new Error(`Failed to write ${outPath}`);
  return outPath;
}

async function buildXlsx(rows, imagePaths) {
  const wb = new ExcelJS.Workbook();
  wb.creator = "Street Cat Interview";
  wb.created = new Date();

  const intro = wb.addWorksheet("00_请先看这里", {
    properties: { defaultRowHeight: 18 },
  });
  intro.getColumn(1).width = 100;
  const introLines = [
    "《街角专访》第一章 · UI 界面需求清单（给画师 · 含参考图）",
    "",
    "怎么用",
    "1. 打开「01_UI界面需求」工作表：每一行是一项 UI 交付，右侧嵌入了标注参考图。",
    "2. 「界面元素标注」用中文说明这是屏幕上的哪一块；请以红/橙框标注为准。",
    "3. 「参考图」列是嵌入 PNG；原图也在同目录 Docs/art/ui-refs/ 下，文件名与行对应。",
    "4. 线框示意 ≠ 最终美术风格，只表达布局与部位；已有资源行会贴上实机 PNG 并加标注。",
    "5. 「过时勿交」「仅开发」「无需图」行仍附参考图，方便对照，不要误交付。",
    "",
    "列含义",
    "序号 | 场景名称 | 用在哪里 | 当前状态 | 紧急程度 | 交付文件名（英文） | 参考/说明 | 界面元素标注 | 参考图",
    "",
    "紧急程度：高=核心玩法环；中=体验与 overlay；低=可选/开发/过时",
    "当前状态：已有资源 / 程序色块 / 占位复用背景 / 文档标缺 / 过时勿交 / 仅开发",
  ];
  introLines.forEach((line, i) => {
    const r = intro.getRow(i + 1);
    r.getCell(1).value = line;
    if (i === 0) r.font = { bold: true, size: 14, name: "Microsoft YaHei" };
    else r.font = { size: 11, name: "Microsoft YaHei" };
  });

  const ws = wb.addWorksheet("01_UI界面需求", {
    views: [{ state: "frozen", ySplit: 1 }],
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
  const widths = [6, 22, 28, 14, 10, 42, 36, 40, 48];
  headers.forEach((h, i) => {
    ws.getColumn(i + 1).width = widths[i];
    const cell = ws.getRow(1).getCell(i + 1);
    cell.value = h;
    cell.font = { bold: true, name: "Microsoft YaHei", size: 11, color: { argb: "FFFFFFFF" } };
    cell.fill = { type: "pattern", pattern: "solid", fgColor: { argb: "FF5C4030" } };
    cell.alignment = { vertical: "middle", wrapText: true };
  });
  ws.getRow(1).height = 28;

  const imgH = 118;
  const imgW = 210;

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
      "", // image column
    ];
    vals.forEach((v, ci) => {
      const cell = excelRow.getCell(ci + 1);
      cell.value = v;
      cell.font = { name: "Microsoft YaHei", size: 10 };
      cell.alignment = { vertical: "middle", wrapText: true };
    });
    excelRow.height = 130;

    const imgPath = imagePaths[i];
    const imageId = wb.addImage({
      filename: imgPath,
      extension: "png",
    });
    // Column I = index 8
    ws.addImage(imageId, {
      tl: { col: 8, row: i + 1 },
      ext: { width: imgW, height: imgH },
      editAs: "oneCell",
    });
  }

  const idx = wb.addWorksheet("02_参考图索引");
  ["序号", "场景名称", "本地文件名", "相对路径", "界面元素标注"].forEach((h, i) => {
    idx.getColumn(i + 1).width = [8, 24, 36, 40, 50][i];
    const cell = idx.getRow(1).getCell(i + 1);
    cell.value = h;
    cell.font = { bold: true, name: "Microsoft YaHei" };
  });
  rows.forEach((row, i) => {
    const r = idx.getRow(i + 2);
    r.getCell(1).value = row.id;
    r.getCell(2).value = row.name;
    r.getCell(3).value = `${row.imageId}.png`;
    r.getCell(4).value = `Docs/art/ui-refs/${row.imageId}.png`;
    r.getCell(5).value = row.elementLabel;
    for (let c = 1; c <= 5; c++) {
      r.getCell(c).font = { name: "Microsoft YaHei", size: 10 };
      r.getCell(c).alignment = { wrapText: true, vertical: "middle" };
    }
    r.height = 36;
  });

  await wb.xlsx.writeFile(OUT_XLSX);
}

async function main() {
  registerFonts();
  fs.mkdirSync(REFS, { recursive: true });

  const rows = defineRows();
  console.log(`Generating ${rows.length} annotated reference images…`);

  const imagePaths = [];
  for (const row of rows) {
    process.stdout.write(`  [${pad(row.id)}] ${row.imageId} … `);
    const p = await ensureImage(row);
    imagePaths.push(p);
    console.log("ok");
  }

  // Verify every row has an image
  const missing = rows.filter((_, i) => !exists(imagePaths[i]));
  if (missing.length) {
    throw new Error(`Missing images for rows: ${missing.map((r) => r.id).join(", ")}`);
  }

  console.log("Writing workbook…");
  await buildXlsx(rows, imagePaths);

  const urgency = { 高: 0, 中: 0, 低: 0 };
  for (const r of rows) urgency[r.urgency] = (urgency[r.urgency] || 0) + 1;

  console.log("");
  console.log("Wrote", OUT_XLSX);
  console.log("Images", REFS);
  console.log("Rows:", rows.length);
  console.log("Urgency:", JSON.stringify(urgency));
  console.log("Open tip: Excel / WPS 打开 xlsx；若嵌入图显示不全，可同时打开 Docs/art/ui-refs/ 对照。");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
