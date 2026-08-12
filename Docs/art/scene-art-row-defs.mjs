/**
 * Scene / background art rows for artist workbook.
 * Base list: 18 user-specified BGs + optional article-published placeholder.
 */
import fs from "fs";
import path from "path";

function nr(x, y, w, h) {
  return { x, y, w, h };
}

function statusFor(bgDir, file) {
  const png = path.join(bgDir, file);
  const meta = png + ".meta";
  if (fs.existsSync(png)) return "已有资源（可替换）";
  if (fs.existsSync(meta)) return "缺图（仅.meta）";
  return "缺图";
}

/**
 * @param {{ BG: string }} dirs
 */
export function defineSceneRows(dirs) {
  const b = (name) => path.join(dirs.BG, name);
  const st = (file) => statusFor(dirs.BG, file);

  /** Helper: asset annotate if exists, else composition wire placeholder */
  const bgGen = (file, calloutRegion) => {
    const src = b(file);
    if (fs.existsSync(src)) {
      return { kind: "asset", src };
    }
    return {
      kind: "bg-placeholder",
      title: file,
      region: calloutRegion || nr(0.1, 0.15, 0.8, 0.65),
    };
  };

  const rows = [
    {
      id: 1,
      name: "编辑部 · 傍晚",
      where: "SC-01 开场",
      status: st("bg_editorial_dusk.png"),
      urgency: "P0",
      deliverable: "bg_editorial_dusk.png",
      notes: "VnArt/Backgrounds；杂志社大空间",
      callout: "编辑部傍晚全屏",
      elementLabel:
        "剧情开场全屏背景——杂志社编辑部内部，傍晚暖黄灯光。要能看出工位/过道氛围，方便立绘站在前景。不要在画面上叠 UI 按钮或对白框。",
      imageId: "bg01_editorial_dusk",
      gen: bgGen("bg_editorial_dusk.png"),
    },
    {
      id: 2,
      name: "沈禾办公室 · 傍晚",
      where: "SC-02",
      status: st("bg_shenhe_office_dusk.png"),
      urgency: "P0",
      deliverable: "bg_shenhe_office_dusk.png",
      notes: "主编办公室傍晚",
      callout: "沈禾办公室傍晚",
      elementLabel:
        "沈禾（主编）办公室全屏背景，傍晚。书桌、窗光、略正式的杂志社领导办公室感。立绘会叠在前景；不要画死角色。",
      imageId: "bg02_shenhe_office_dusk",
      gen: bgGen("bg_shenhe_office_dusk.png"),
    },
    {
      id: 3,
      name: "编辑部工位 · 傍晚",
      where: "SC-03 刷社交/选题",
      status: st("bg_editorial_desk_dusk.png"),
      urgency: "P0",
      deliverable: "bg_editorial_desk_dusk.png",
      notes: "小凌工位；手机社交叠层盖在上面",
      callout: "工位傍晚（刷手机）",
      elementLabel:
        "小凌个人工位特写/近景，傍晚。桌上可有电脑、杂物；玩家会在此打开手机看社交帖。不要把手机 UI 画进背景里。",
      imageId: "bg03_editorial_desk_dusk",
      gen: bgGen("bg_editorial_desk_dusk.png"),
    },
    {
      id: 4,
      name: "编辑部工位 · 上午",
      where: "SC-10 写稿",
      status: st("bg_editorial_desk_morning.png"),
      urgency: "P0",
      deliverable: "bg_editorial_desk_morning.png",
      notes: "写稿日上午光",
      callout: "工位上午（写稿）",
      elementLabel:
        "同一工位的上午版本——日光更亮、更清醒。写稿台 UI 会叠在前景，背景保持桌面/窗光即可，不要画成软木板素材库。",
      imageId: "bg04_editorial_desk_morning",
      gen: bgGen("bg_editorial_desk_morning.png"),
    },
    {
      id: 5,
      name: "沈禾办公室 · 上午",
      where: "SC-10 审稿",
      status: st("bg_shenhe_office_morning.png"),
      urgency: "P0",
      deliverable: "bg_shenhe_office_morning.png",
      notes: "交稿后审稿对白",
      callout: "沈禾办公室上午",
      elementLabel:
        "沈禾办公室上午光版本，用于交稿后听主编反馈。与傍晚版同一房间不同时段；不要画审核面板 UI。",
      imageId: "bg05_shenhe_office_morning",
      gen: bgGen("bg_shenhe_office_morning.png"),
    },
    {
      id: 6,
      name: "槐安社区 · 午后",
      where: "SC-04；后日谈暂用",
      status: st("bg_huaian_afternoon.png"),
      urgency: "P0",
      deliverable: "bg_huaian_afternoon.png",
      notes: "社区外景；亦被后日谈「文章发布页」占位",
      callout: "槐安社区午后外景",
      elementLabel:
        "槐安社区室外全景，午后阳光。调查与剧情都会用。注意：后日谈「文章发布页」现在暂时也借用这张——那是占位，不是本图的正确用途。",
      imageId: "bg06_huaian_afternoon",
      gen: bgGen("bg_huaian_afternoon.png"),
    },
    {
      id: 7,
      name: "槐安社区平面图",
      where: "调查地图选点",
      status: st("bg_huaian_map.png"),
      urgency: "P0",
      deliverable: "bg_huaian_map.png",
      notes: "图上要能辨认猫屋、贩卖机、长椅等位置",
      callout: "调查用地图平面图",
      elementLabel:
        "调查模式整屏平面示意图。必须能一眼认出可点热点区位：猫屋/投喂点、贩卖机、长椅、快递柜、保安亭等剪影或标注感。不要画成写实照片街景；程序会在热点上叠绿色✓。",
      imageId: "bg07_huaian_map",
      gen: bgGen("bg_huaian_map.png", nr(0.15, 0.2, 0.7, 0.55)),
    },
    {
      id: 8,
      name: "流浪猫投喂点",
      where: "调查：猫屋/碗",
      status: st("bg_feeding_spot.png"),
      urgency: "P1",
      deliverable: "bg_feeding_spot.png",
      notes: "近景调查",
      callout: "投喂点近景",
      elementLabel:
        "点进「猫屋/投喂点」后的近景全屏：猫屋、食碗、周边环境。午后社区光。不要把告示牌特写塞进同一张（告示牌另有一张）。",
      imageId: "bg08_feeding_spot",
      gen: bgGen("bg_feeding_spot.png"),
    },
    {
      id: 9,
      name: "投喂点告示牌特写",
      where: "调查：挂牌",
      status: st("bg_feeding_sign.png"),
      urgency: "P1",
      deliverable: "bg_feeding_sign.png",
      notes: "挂牌可读感；正文可用模糊/示意",
      callout: "告示牌特写",
      elementLabel:
        "投喂点挂着的告示牌特写全屏。要让人看出「这是一张通告」，文字可示意或略模糊（程序也可能叠字）。不要画成整片投喂点远景。",
      imageId: "bg09_feeding_sign",
      gen: bgGen("bg_feeding_sign.png"),
    },
    {
      id: 10,
      name: "晒太阳的猫 · 放松",
      where: "调查狸花",
      status: st("bg_cat_relax.png"),
      urgency: "P1",
      deliverable: "bg_cat_relax.png",
      notes: "场景角色图，非对话立绘",
      callout: "狸花猫·放松",
      elementLabel:
        "调查「晒太阳的猫」——狸花猫放松趴着的场景图。猫是画面主体，环境是社区一角。这是场景角色图，不是对话半身立绘。",
      imageId: "bg10_cat_relax",
      gen: bgGen("bg_cat_relax.png"),
    },
    {
      id: 11,
      name: "晒太阳的猫 · 警惕",
      where: "调查狸花",
      status: st("bg_cat_alert.png"),
      urgency: "P1",
      deliverable: "bg_cat_alert.png",
      notes: "同一地点不同姿态",
      callout: "狸花猫·警惕",
      elementLabel:
        "同一只晒太阳的狸花猫，警惕姿态（抬头/竖耳）。与放松/躲藏同场景不同情绪，方便调查状态切换。",
      imageId: "bg11_cat_alert",
      gen: bgGen("bg_cat_alert.png"),
    },
    {
      id: 12,
      name: "晒太阳的猫 · 躲藏",
      where: "调查狸花",
      status: st("bg_cat_hide.png"),
      urgency: "P1",
      deliverable: "bg_cat_hide.png",
      notes: "脚本「躲闪」同 key",
      callout: "狸花猫·躲藏",
      elementLabel:
        "狸花猫躲藏/躲闪姿态的场景图。仍是场景角色图；不要画成空白背景+立绘。",
      imageId: "bg12_cat_hide",
      gen: bgGen("bg_cat_hide.png"),
    },
    {
      id: 13,
      name: "自动贩卖机",
      where: "调查",
      status: st("bg_vending.png"),
      urgency: "P1",
      deliverable: "bg_vending.png",
      notes: "贩卖机近景",
      callout: "贩卖机近景",
      elementLabel:
        "调查自动贩卖机时的近景全屏。机身、按钮、社区墙角即可；不要塞进平面地图。",
      imageId: "bg13_vending",
      gen: bgGen("bg_vending.png"),
    },
    {
      id: 14,
      name: "木质长椅",
      where: "调查",
      status: st("bg_bench.png"),
      urgency: "P1",
      deliverable: "bg_bench.png",
      notes: "长椅近景",
      callout: "木质长椅近景",
      elementLabel:
        "调查木质长椅时的近景全屏。午后社区公园/路边感；可留前景给立绘，但主角不要画死在背景里。",
      imageId: "bg14_bench",
      gen: bgGen("bg_bench.png"),
    },
    {
      id: 15,
      name: "快递柜",
      where: "调查；后日谈可复用",
      status: st("bg_locker.png"),
      urgency: "P1",
      deliverable: "bg_locker.png",
      notes: "快递柜近景",
      callout: "快递柜近景",
      elementLabel:
        "调查快递柜时的近景全屏。柜门网格要能辨认；后日谈叙事也可能提到，保持生活化社区设施感。",
      imageId: "bg15_locker",
      gen: bgGen("bg_locker.png"),
    },
    {
      id: 16,
      name: "保安亭 · 午后",
      where: "调查/解锁",
      status: st("bg_guard_afternoon.png"),
      urgency: "P0",
      deliverable: "bg_guard_afternoon.png",
      notes: "保安亭外/旁午后",
      callout: "保安亭午后",
      elementLabel:
        "保安亭及周边，午后光。调查解锁与见面用。大福采访主要用傍晚版；本张偏白天。",
      imageId: "bg16_guard_afternoon",
      gen: bgGen("bg_guard_afternoon.png"),
    },
    {
      id: 17,
      name: "保安亭 · 傍晚",
      where: "采访大福等",
      status: st("bg_guard_dusk.png"),
      urgency: "P0",
      deliverable: "bg_guard_dusk.png",
      notes: "自由采访大福主场景",
      callout: "保安亭傍晚（采访）",
      elementLabel:
        "保安亭傍晚——采访大福时的主背景。暖黄昏/灯光；自由采访三栏 UI 会贴在前景纸片上，背景保持可读的亭子与夜色即可。",
      imageId: "bg17_guard_dusk",
      gen: bgGen("bg_guard_dusk.png"),
    },
    {
      id: 18,
      name: "咖啡馆 · 午后",
      where: "采访林女士",
      status: st("bg_cafe_afternoon.png"),
      urgency: "P0",
      deliverable: "bg_cafe_afternoon.png",
      notes: "林女士自由采访",
      callout: "咖啡馆午后（采访）",
      elementLabel:
        "咖啡馆内午后——采访林女士时的主背景。座位、窗光、咖啡店氛围；采访 UI 纸片叠在上面，不要把对话气泡画进背景。",
      imageId: "bg18_cafe_afternoon",
      gen: bgGen("bg_cafe_afternoon.png"),
    },
    // Optional — confirmed placeholder in VnArt / ShowEpilogue
    {
      id: 19,
      name: "后日谈 · 文章发布页",
      where: "Mode.Epilogue 开场",
      status: "占位复用（现用 bg_huaian_afternoon）",
      urgency: "P1",
      deliverable: "bg_article_published.png（建议）",
      notes: "代码键「文章发布页面」现映射 bg_huaian_afternoon；需专栏/网页感专用 BG",
      callout: "文章发布页（缺专用）",
      elementLabel:
        "后日谈开头全屏——应像「文章已上网/专栏发布」的网页或杂志成品页氛围。现在错误地借用槐安社区午后外景顶替；请新画专用发布页，不要再交社区外景当这一项。",
      imageId: "bg19_article_published",
      gen: {
        kind: "asset-annotate-placeholder",
        src: b("bg_huaian_afternoon.png"),
        note: "现占位槐安午后 → 需 bg_article_published",
      },
    },
  ];

  return rows;
}
