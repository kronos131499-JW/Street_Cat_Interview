# -*- coding: utf-8 -*-
from pathlib import Path

inv = Path(r"D:\Street_Cat_Interview\github\Street_Cat_Interview\Assets\Scripts\Investigation\InvestigationService.cs")
text = inv.read_text(encoding="utf-8")

# Insert InspectBeat class after HotspotData fields - replace HotspotData class
old_hs = '''    [Serializable]
    public class HotspotData
    {
        public string id;
        public string title;
        public string description;
        public string grantIntel;
        public string noteLine;
        public bool once = true;
        public bool inspected;
    }'''

new_hs = '''    [Serializable]
    public class InspectBeat
    {
        /// <summary>true = 旁白（无名牌）；false = 小凌对白。</summary>
        public bool narration;
        public string text;
    }

    [Serializable]
    public class HotspotData
    {
        public string id;
        public string title;
        public string description;
        public List<InspectBeat> beats = new List<InspectBeat>();
        public string grantIntel;
        public string noteLine;
        public bool once = true;
        public bool inspected;
    }'''

if old_hs not in text:
    raise SystemExit("HotspotData not found")
text = text.replace(old_hs, new_hs, 1)

# Replace BuildDefaults hotspots block - from Hotspots = new to GuardTopics =
start = text.find("            Hotspots = new List<HotspotData>")
end = text.find("            GuardTopics = new List<TalkTopic>")
if start < 0 or end < 0:
    raise SystemExit("hotspot/guard markers not found")

hotspots = r'''            Hotspots = new List<HotspotData>
            {
                new HotspotData
                {
                    id = "cat_house",
                    title = "猫屋",
                    description = "塑料收纳箱改造的猫屋，比出租屋还精致。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "猫屋是用塑料收纳箱改造的，外面罩着一层裁剪过的防水板，接缝处贴了好几道胶带，屋顶还压着两块砖，防止被风吹翻。猫屋里铺着一张旧毛毯，表面已经被抓得起了球。" },
                        new InspectBeat { narration = false, text = "竟然还有给猫住的地方。" },
                        new InspectBeat { narration = false, text = "看起来好精致哇......" },
                        new InspectBeat { narration = false, text = "比我的出租屋还要精致。" }
                    }
                },
                new HotspotData
                {
                    id = "food_bowl",
                    title = "猫粮碗",
                    description = "几个猫碗并排放着，碗底很干净。",
                    grantIntel = IntelIds.FixedFeedingPoint,
                    noteLine = "社区内设有长期维护的投喂点，附近居民可能了解流浪猫的情况。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "几个猫碗并排放着，其中一个还有少量猫粮。碗底很干净。" },
                        new InspectBeat { narration = false, text = "碗还挺干净。" },
                        new InspectBeat { narration = false, text = "应该有人定期过来投喂。" }
                    }
                },
                new HotspotData
                {
                    id = "water_bowl",
                    title = "水碗",
                    description = "水碗里装着大半碗清水，上面飘着几根猫毛。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "水碗里装着大半碗清水，上面飘着几根猫毛。" },
                        new InspectBeat { narration = false, text = "其实我一直很好奇，猫会不会把水里自己的毛喝下去。" }
                    }
                },
                new HotspotData
                {
                    id = "sign",
                    title = "投喂点小挂牌",
                    description = "挂牌提醒不要倒剩饭，并补了一行：奶茶不算水。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "挂牌上用记号笔写着：请不要把人类吃的剩饭倒在这里。猫粮少量添加，吃完再补，不然放久了会变质。水脏了的话麻烦帮忙换一下，谢谢。" },
                        new InspectBeat { narration = true, text = "下面还有一行明显是后来补上的：不要倒水之外的液体！！！奶茶不算水！" },
                        new InspectBeat { narration = false, text = "……" },
                        new InspectBeat { narration = false, text = "不知道为什么有点想喝奶茶了......" }
                    }
                },
                new HotspotData
                {
                    id = "tabby",
                    title = "灌木旁的狸花猫",
                    description = "狸花猫晒太阳，靠近后钻进灌木丛。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "一只狸花猫趴在灌木丛边的草地上晒太阳，前爪交叠，眯着眼睛。" },
                        new InspectBeat { narration = false, text = "那边有只猫哎。" },
                        new InspectBeat { narration = true, text = "小凌刚往前靠近两步，狸花猫立刻抬起头，警惕地看向她。" },
                        new InspectBeat { narration = false, text = "嘬嘬嘬——咪咪——" },
                        new InspectBeat { narration = true, text = "狸花猫迅速起身，一头钻进旁边的灌木丛，只剩树叶轻轻晃动。" },
                        new InspectBeat { narration = false, text = "……跑得还挺快。" },
                        new InspectBeat { narration = true, text = "小凌往灌木丛里看了一眼，但是什么都没看见。" },
                        new InspectBeat { narration = false, text = "看来这里虽然有人固定照顾它们，但不代表它们会随便亲近陌生人。" },
                        new InspectBeat { narration = false, text = "好吧，不打扰你晒太阳了。" }
                    }
                },
                new HotspotData
                {
                    id = "vending",
                    title = "自动贩卖机",
                    description = "咖啡只卖六块，公司楼下要十八。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = false, text = "什么，咖啡只卖六块？？？" },
                        new InspectBeat { narration = false, text = "公司楼下要十八。突然发现了一个值得调查的社会议题。" }
                    }
                },
                new HotspotData
                {
                    id = "bench",
                    title = "木质长椅",
                    description = "老式木质长椅，看上去至少服役十年了。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "一张老式木质长椅靠着步道摆放。绿色金属扶手已经有些掉漆，露出下面发灰的铁锈；几块木板被晒得颜色深浅不一，其中一块还微微翘起。" },
                        new InspectBeat { narration = false, text = "看上去至少服役十年了。" },
                        new InspectBeat { narration = false, text = "和《此间》的打印机差不多。" }
                    }
                },
                new HotspotData
                {
                    id = "locker",
                    title = "快递柜",
                    description = "柜顶有橘色猫毛，大福常趴在这里。",
                    grantIntel = IntelIds.DafuRestSpot,
                    noteLine = "大福经常趴在社区入口的快递柜上，但当前并不在附近。",
                    beats = new List<InspectBeat>
                    {
                        new InspectBeat { narration = true, text = "社区入口旁立着一排快递柜。柜顶铺着一块折叠纸板，上面残留着少量橘色猫毛。" },
                        new InspectBeat { narration = false, text = "帖子里的照片就是在这里拍的。" },
                        new InspectBeat { narration = true, text = "小凌抬头看向空荡荡的柜顶。" },
                        new InspectBeat { narration = false, text = "本人还没来上班。" }
                    }
                }
            };

'''

text = text[:start] + hotspots + text[end:]

# Add GetInspectBeats method before Inspect
needle = "        public string Inspect(string hotspotId)"
method = '''        public IReadOnlyList<InspectBeat> GetInspectBeats(string hotspotId)
        {
            var h = Hotspots.Find(x => x.id == hotspotId);
            if (h == null) return Array.Empty<InspectBeat>();
            if (h.beats != null && h.beats.Count > 0)
                return h.beats;
            if (!string.IsNullOrEmpty(h.description))
                return new[] { new InspectBeat { narration = true, text = h.description } };
            return Array.Empty<InspectBeat>();
        }

        public string Inspect(string hotspotId)'''
if "GetInspectBeats" not in text:
    if needle not in text:
        raise SystemExit("Inspect method not found")
    text = text.replace(needle, method, 1)

inv.write_text(text, encoding="utf-8")
print("InvestigationService patched")

# BuiltInScripts: Inner -> N
bs = Path(r"D:\Street_Cat_Interview\github\Street_Cat_Interview\Assets\Scripts\Narrative\BuiltInScripts.cs")
bt = bs.read_text(encoding="utf-8")
bt2 = bt.replace("s.lines.Add(Inner(", "s.lines.Add(N(")
# keep Inner helper for now or remove - keep for API
bs.write_text(bt2, encoding="utf-8")
print("BuiltInScripts Inner->N count", bt.count('s.lines.Add(Inner('))
