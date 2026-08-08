using System;
using System.Collections.Generic;
using System.Text;
using StreetCat.Core;
using StreetCat.Data;
using UnityEngine;

namespace StreetCat.Writing
{
    [Serializable]
    public class MaterialCard
    {
        public string id;
        public string title;
        public MaterialType type;
        public string body;
        public ArticleStage stage;
        public string textGuardCat;
        public string textRescue;
    }

    public static class MaterialCatalog
    {
        public static readonly List<MaterialCard> All = new List<MaterialCard>
        {
            C(MaterialIds.M01, "今日在岗", MaterialType.Detail, ArticleStage.A_PresentLife,
                "大福经常在下午四五点来到保安亭，吃完东西后趴在快递柜上休息。",
                "下午四点多，大福从停放的电动车之间钻出来，走到保安亭前。吃完保安递来的猫粮，它熟练地跳上旁边的快递柜，开始了居民口中一天的「值班」。",
                "大福没有固定主人。每天下午四五点，它常常出现在保安亭附近，吃过东西后，便在快递柜上找个位置趴下。"),
            C(MaterialIds.M02, "大福从前很怕人", MaterialType.Fact, ArticleStage.B_PastInjury,
                "大福被救助前非常怕人，只要有人靠近便会逃跑。",
                "居民回忆，大福刚来时极度警惕，人一靠近就会躲开。",
                "林女士第一次接近它时，大福几乎立刻逃跑——那时它还非常怕人。"),
            C(MaterialIds.M03, "大福脖子的伤", MaterialType.Detail, ArticleStage.B_PastInjury,
                "大福记得脖子长期被某种很紧的东西勒着，而且无法自行弄掉。",
                "大福只记得那段时间脖子一直疼，有东西紧紧勒着，怎么也弄不掉。",
                "在猫的记忆里，伤势首先是长期的疼痛，以及怎么也摆脱不掉的束缚感。"),
            C(MaterialIds.M04, "消失的麻绳", MaterialType.Detail, ArticleStage.C_RescueTreatment,
                "大福记得自己在陌生的地方睡了一觉，醒来后，原本勒住脖子的东西已经消失。",
                "它记得自己在一个很亮、气味很重的地方睡着，醒来后勒住脖子的东西不见了。",
                "对大福而言，治疗的过程很难用人类概念描述；它只记得醒来之后，那件勒着自己的东西消失了。"),
            C(MaterialIds.M05, "麻绳嵌进了皮肉", MaterialType.Fact, ArticleStage.B_PastInjury,
                "医院检查发现，一根粗麻绳已经深深嵌入大福颈部，伤口出现坏死和严重感染。",
                "据林女士转述，医院检查后发现粗麻绳已嵌入颈部组织，伤口坏死并严重感染。",
                "送到医院后，医生告诉林女士：那不是普通的表面外伤，麻绳已经嵌进皮肉。"),
            C(MaterialIds.M06, "连续四晚投喂", MaterialType.Fact, ArticleStage.C_RescueTreatment,
                "林女士连续四晚带着罐头寻找大福，每次放下食物后都会主动退远。",
                "为了靠近这只怕人的猫，林女士连续四个晚上带着罐头去找它，放下食物后就退开。",
                "林女士连续四晚投喂，目的不是驯服，而是让大福愿意在相对固定的位置进食，以便观察和实施救助。"),
            C(MaterialIds.M07, "抓捕与送医", MaterialType.Fact, ArticleStage.C_RescueTreatment,
                "大福伤势持续恶化后，林女士联系有救助经验的人协助抓捕，并将它送往医院。",
                "伤势恶化后，林女士联系有经验的人协助抓捕，把大福送进了医院。",
                "抓捕当天大福非常害怕。最终它被装进航空箱，送往宠物医院。"),
            C(MaterialIds.M08, "手术以后又确诊猫瘟", MaterialType.Fact, ArticleStage.C_RescueTreatment,
                "大福完成颈部手术后，在住院第三天又被确诊猫瘟，需要继续住院治疗。",
                "颈部手术后，住院第三天又确诊猫瘟，治疗被迫延长。",
                "手术并不意味着结束。住院第三天，医院告知林女士：大福确诊猫瘟。"),
            C(MaterialIds.M09, "接近一万元", MaterialType.Fact, ArticleStage.C_RescueTreatment,
                "大福的手术、住院和猫瘟治疗总费用接近一万元。",
                "整段治疗费用接近一万元。",
                "手术约五千，加上猫瘟住院，全部加起来接近一万——对林女士并不是轻松的数字。"),
            C(MaterialIds.M10, "确实犹豫过", MaterialType.Emotion, ArticleStage.C_RescueTreatment,
                "面对不断增加的费用和不确定的治疗结果，林女士承认自己曾考虑过是否继续治疗。",
                "她承认自己犹豫过，不是觉得猫不值得救，而是不确定后面的费用与结果。",
                "医院无法保证一定救活时，林女士确实犹豫过，但最终还是选择继续治疗。"),
            C(MaterialIds.M11, "家里已经有四只猫", MaterialType.Fact, ArticleStage.D_Release,
                "林女士家中已经有四只猫，还需要照顾女儿，无法长期承担第五只猫。",
                "她家里已经有四只猫，还有孩子要照顾。",
                "林女士很清楚：空间、精力和费用都已经到了极限。"),
            C(MaterialIds.M12, "救治和收养是两件事", MaterialType.Emotion, ArticleStage.D_Release,
                "林女士认为，自己有能力在当时救治大福，并不代表有能力长期收养第五只猫。",
                "在她看来，救治和收养是两件事。",
                "「我当时有能力把它的伤治好，但不代表有能力长期照顾第五只猫。」"),
            C(MaterialIds.M13, "回到槐安社区", MaterialType.Fact, ArticleStage.D_Release,
                "大福康复后，林女士将它送回原本活动的槐安社区。",
                "康复后，大福被送回槐安社区。",
                "确认社区仍有人照料后，林女士把大福送回了原来的活动区域。"),
            C(MaterialIds.M14, "没有主人的大福", MaterialType.Fact, ArticleStage.A_PresentLife,
                "大福没有固定主人，但有居民为它换水、投喂、清洗猫碗和维护猫屋。",
                "它没有法律意义上的主人，却有一群人轮流换水、添粮、搭窝。",
                "没有人单独拥有大福，但社区里有人持续照料它。"),
            C(MaterialIds.M15, "狸花猫伙伴", MaterialType.Detail, ArticleStage.E_AfterReturn,
                "大福后来经常与一只狸花猫一起吃饭、休息，并在晚上结伴活动。",
                "傍晚时分，它常和一只狸花猫一起离开。",
                "放归之后，大福与一只狸花猫渐渐成了固定搭档。"),
            C(MaterialIds.M16, "麻绳来源无法确认", MaterialType.Unconfirmed, ArticleStage.B_PastInjury,
                "没有人看到麻绳如何套上大福的脖子，目前无法确认是否存在人为伤害。",
                "至于麻绳最初如何出现，目前没有人看见，也无法确认是否存在故意伤害。",
                "林女士强调：绳子怎么套上去的，没有人能确定。"),
        };

        static MaterialCard C(string id, string title, MaterialType type, ArticleStage stage,
            string body, string guard, string rescue) => new MaterialCard
        {
            id = id,
            title = title,
            type = type,
            stage = stage,
            body = body,
            textGuardCat = guard,
            textRescue = rescue
        };

        public static MaterialCard Get(string id) => All.Find(m => m.id == id);
    }

    public class ArticleAssembler
    {
        public string Title { get; private set; }
        public string Body { get; private set; }
        public int Score { get; private set; }
        public string ReviewText { get; private set; }
        /// <summary>A=pass, B=theme, C=missing facts, D=speculation, E=dumping wording, F=source mix.</summary>
        public string ReviewBranch { get; private set; } = "A";

        public bool CanAssemble(WritingDirection dir, List<string> selected, out string error)
        {
            error = null;
            if (selected == null || selected.Count < 8 || selected.Count > 10)
            {
                error = "请选择 8～10 张素材卡。";
                return false;
            }
            if (!selected.Contains(MaterialIds.M13))
            {
                error = "必须包含素材「回到槐安社区」。";
                return false;
            }

            int a = 0, b = 0, c = 0, d = 0;
            foreach (var id in selected)
            {
                var m = MaterialCatalog.Get(id);
                if (m == null) continue;
                if (m.stage == ArticleStage.A_PresentLife || m.stage == ArticleStage.E_AfterReturn) a++;
                if (m.stage == ArticleStage.B_PastInjury) b++;
                if (m.stage == ArticleStage.C_RescueTreatment) c++;
                if (m.stage == ArticleStage.D_Release) d++;
            }
            if (a < 1) { error = "至少需要 1 张「现在的生活」相关素材。"; return false; }
            if (b < 1) { error = "至少需要 1 张「过去与伤势」相关素材。"; return false; }
            if (c < 2) { error = "至少需要 2 张「救助与治疗」相关素材。"; return false; }
            if (d < 1) { error = "至少需要 1 张「未收养与放归」相关素材。"; return false; }
            return true;
        }

        public void Assemble(WritingDirection dir, List<string> selected, int phrasingA, int phrasingB)
        {
            Title = dir == WritingDirection.GuardCatToday ? "《大福今天也在上班》" : "《救下一只猫以后》";
            var sb = new StringBuilder();
            sb.AppendLine(Title);
            sb.AppendLine();

            if (dir == WritingDirection.GuardCatToday)
            {
                sb.AppendLine("【现在的大福】");
                AppendStage(sb, selected, dir, ArticleStage.A_PresentLife, ArticleStage.E_AfterReturn);
                sb.AppendLine();
                sb.AppendLine("【过去】");
                AppendStage(sb, selected, dir, ArticleStage.B_PastInjury);
                sb.AppendLine();
                sb.AppendLine("【救助】");
                AppendStage(sb, selected, dir, ArticleStage.C_RescueTreatment);
                sb.AppendLine();
                sb.AppendLine("【放归之后】");
                AppendStage(sb, selected, dir, ArticleStage.D_Release);
                AppendStage(sb, selected, dir, ArticleStage.E_AfterReturn);
            }
            else
            {
                sb.AppendLine("【发现】");
                AppendStage(sb, selected, dir, ArticleStage.B_PastInjury);
                sb.AppendLine();
                sb.AppendLine("【接近与治疗】");
                AppendStage(sb, selected, dir, ArticleStage.C_RescueTreatment);
                sb.AppendLine();
                sb.AppendLine("【为什么没有收养】");
                AppendStage(sb, selected, dir, ArticleStage.D_Release);
                sb.AppendLine();
                sb.AppendLine("【回到社区】");
                AppendStage(sb, selected, dir, ArticleStage.A_PresentLife, ArticleStage.E_AfterReturn);
            }

            // phrasingA: 0 = 推测写成事实(退回D), 1 = 无法确认(通过)
            // phrasingB: 0 = 扔回外面(退回E), 1 = 送回社区(通过)
            if (phrasingA == 0)
                sb.AppendLine("\n（表述）大福疑似遭到人为虐待，一根麻绳被故意勒在它的脖子上。");
            else
                sb.AppendLine("\n（表述）没人看到麻绳是如何套上大福脖子的，目前无法确认是否存在人为伤害。");

            if (phrasingB == 0)
                sb.AppendLine("（表述）治疗结束后，林女士最终还是把大福扔回了外面。");
            else
                sb.AppendLine("（表述）大福康复后，林女士将它送回原本活动的槐安社区；当时社区已有固定投喂点，也有人继续照料。");

            Body = sb.ToString();
            ScoreAndReview(dir, selected, phrasingA, phrasingB);
        }

        void AppendStage(StringBuilder sb, List<string> selected, WritingDirection dir, params ArticleStage[] stages)
        {
            foreach (var id in selected)
            {
                var m = MaterialCatalog.Get(id);
                if (m == null) continue;
                bool match = false;
                foreach (var st in stages)
                    if (m.stage == st) match = true;
                // M01 can appear in A and E
                if (!match && m.id == MaterialIds.M01)
                    foreach (var st in stages)
                        if (st == ArticleStage.A_PresentLife || st == ArticleStage.E_AfterReturn) match = true;
                if (!match) continue;
                sb.AppendLine(dir == WritingDirection.GuardCatToday ? m.textGuardCat : m.textRescue);
            }
        }

        void ScoreAndReview(WritingDirection dir, List<string> selected, int phrasingA, int phrasingB)
        {
            // Priority (SC-10 script): fact/misleading first, then missing info / theme drift.
            if (phrasingA == 0)
            {
                ReviewBranch = "D";
                Score = 48;
                ReviewText = BuildRejectReview("将推测写成事实",
                    "「有人故意用麻绳勒伤它」——没人看到麻绳怎么套上去，不能把猜测换成句号。",
                    "将未确认信息恢复为「无法确认」或删除无依据的因果判断。");
                return;
            }

            if (phrasingB == 0)
            {
                ReviewBranch = "E";
                Score = 50;
                ReviewText = BuildRejectReview("放归措辞误导",
                    "「把大福扔回了外面」已经替读者下了结论。事实是送回原活动区域，且确认过有人继续投喂。",
                    "修改对「放归」的误导性措辞，保持与已确认事实一致。");
                return;
            }

            // 重要治疗 / 放归事实断层
            if (!selected.Contains(MaterialIds.M07) || !selected.Contains(MaterialIds.M08))
            {
                ReviewBranch = "C";
                Score = 55;
                ReviewText = BuildRejectReview("重要事实不足",
                    "大福从脖子受伤直接跳到回社区，中间怎么治疗几乎没交代。已经问到的补进素材；没问到的回去补访。",
                    "补充关键事实；若对应素材尚未解锁，可返回采访阶段继续核实。");
                return;
            }

            int present = 0, rescueFocus = 0;
            foreach (var id in selected)
            {
                var m = MaterialCatalog.Get(id);
                if (m == null) continue;
                if (m.stage == ArticleStage.A_PresentLife || m.stage == ArticleStage.E_AfterReturn) present++;
                if (m.stage == ArticleStage.C_RescueTreatment || m.stage == ArticleStage.D_Release) rescueFocus++;
            }

            if (dir == WritingDirection.RescueWithoutAdoption && present >= rescueFocus)
            {
                ReviewBranch = "B";
                Score = 58;
                ReviewText = BuildRejectReview("选材与立意不匹配",
                    "你选的是「救下一只猫以后」，但大半篇幅都在写大福现在怎么上班、怎么晒太阳。先想清楚要写变化还是写这场救助。",
                    "调整写作方向或重新选择更匹配的素材。");
                return;
            }

            if (dir == WritingDirection.GuardCatToday && rescueFocus >= present + 2 && !selected.Contains(MaterialIds.M01))
            {
                ReviewBranch = "B";
                Score = 58;
                ReviewText = BuildRejectReview("选材与立意不匹配",
                    "标题写的是大福的日常变化，但选材几乎都在讲救助与放归。想清楚你到底要写哪一条线。",
                    "调整写作方向或重新选择更匹配的素材。");
                return;
            }

            ReviewBranch = "A";
            Score = 88;
            if (selected.Contains(MaterialIds.M12)) Score += 4;
            if (selected.Contains(MaterialIds.M16)) Score += 3;
            Score = Mathf.Clamp(Score, 60, 100);
            var sb = new StringBuilder();
            sb.AppendLine("审核结果——通过");
            sb.AppendLine();
            sb.AppendLine("沈禾：看完了。可以发。");
            sb.AppendLine("大福记得的部分，你按它的感受写；治疗和放归，你也找到了能确认的人。");
            sb.AppendLine("该说不知道的地方，没有替它编答案。");
            sb.AppendLine("记者不是负责把故事写得更传奇，是负责别把故事写错。");
            sb.AppendLine("就这样，发吧。");
            ReviewText = sb.ToString();
        }

        static string BuildRejectReview(string title, string detail, string advice)
        {
            var sb = new StringBuilder();
            sb.AppendLine("审核结果——退回");
            sb.AppendLine("问题｜" + title);
            sb.AppendLine();
            sb.AppendLine(detail);
            sb.AppendLine();
            sb.AppendLine("编辑意见——" + advice);
            return sb.ToString();
        }

        public bool CanPublish => ReviewBranch == "A" && Score >= 60;
    }
}
