using System;
using System.Collections.Generic;
using System.Text;
using StreetCat.Data;
using StreetCat.Loc;

namespace StreetCat.Interview
{
    /// <summary>English interview prompts + localized ask-question strings when GameSettings.IsEnglish.</summary>
    public static class InterviewLoc
    {
        public static bool UseEnglish => GameSettings.IsEnglish;

        public static string PlayerPrefix => UseEnglish ? "Ling: " : "小凌：";

        public static string SpeakerPrefix(InterviewSubject subject)
        {
            if (subject == InterviewSubject.Dafu)
                return UseEnglish ? "Dafu: " : "大福：";
            if (subject == InterviewSubject.Lin)
                return UseEnglish ? "Ms. Lin: " : "林女士：";
            return "";
        }

        static readonly Dictionary<string, string> AskQuestionEn = new Dictionary<string, string>
        {
            { "大福平时一般什么时候会来这里？", "When do you usually show up around here?" },
            { "你以前也会来保安亭这边吗？", "Did you used to come by the guard booth too?" },
            { "你脖子以前是不是受过伤？", "Were you hurt on your neck before?" },
            { "她来过很多次吗？", "Did she come many times?" },
            { "被带走以后，你去了哪里？", "After they took you away, where did you go?" },
            { "是谁把你带回这里的？", "Who brought you back here?" },
            { "送到医院以后，医生怎么说它的伤？", "After the hospital, what did the vet say about his injury?" },
            { "勒着你的东西是什么感觉？你还记得吗？", "What did whatever was around your neck feel like? Do you remember?" },
            { "您当时为什么连续几天给大福送吃的？", "Why did you keep bringing Dafu food for several days?" },
            { "手术以后，大福恢复得怎么样？", "How was Dafu's recovery after the surgery?" },
            { "你在那里后来发生了什么？", "What happened to you there afterward?" },
            { "决定把大福送回社区之前，您考虑了哪些情况？", "Before deciding to return Dafu to the community, what did you weigh?" },
            { "您是怎么注意到大福的？", "How did you first notice Dafu?" },
            { "为什么连续几天给它送吃的？", "Why did you feed him for several days in a row?" },
            { "送到医院以后怎么样？", "What happened after you got him to the hospital?" },
            { "为什么又把它送回社区？", "Why bring him back to the community afterward?" },
            { "你平时一般什么时候会来这里？", "When do you usually come here?" },
            { "有没有人经常来找你？", "Does anyone come to see you often?" },
            { "那个很亮、味道很重的地方，后来怎样了？", "What happened later in that bright, heavy-smelling place?" },
            { "有没有人经常给你送吃的？", "Does anyone bring you food often?" },
            { "后来有人把你装进笼子带走了吗？", "Did someone put you in a cage and take you away later?" },
            { "治疗大概花了多少？", "About how much did the treatment cost?" },
            { "您有没有犹豫过？", "Did you ever hesitate?" },
            { "为什么康复后没有继续收养它？", "Why not keep fostering him after he recovered?" },
            { "大福一般几点出现？", "When does Dafu usually show up?" },
            { "当初是谁救助的大福？", "Who rescued Dafu back then?" },
        };

        public static readonly string[] ForbiddenLeakEn =
        {
            "surgery", "hospital", "doctor", "vet", "adoption", "foster", "panleuk",
            "distemper", "ms. lin", "ms lin", "lin min", "necrosis", "infection",
            "anesthesia", "suture", "ten thousand", "medical bill", "vet bill",
            "owner", "released back", "rescue group", "treatment cost", "hospitalized"
        };

        public static readonly string[] FoodKeywordsEn =
        {
            "eat", "food", "treat", "kibble", "can", "fish", "hungry", "feed", "snack",
            "tuna", "meal", "dinner", "breakfast", "cat food", "yummy", "tasty"
        };

        static Dictionary<string, string> _enToZh;

        /// <summary>
        /// Map a localized English ask-prompt back to the Chinese source the rule engines match.
        /// Free-typed Chinese is returned unchanged.
        /// </summary>
        public static string CanonicalQuestion(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input ?? "";
            var key = NormalizeAskKey(input);
            EnsureEnToZh();
            if (_enToZh.TryGetValue(key, out var zh))
                return zh;
            return input.Trim();
        }

        public static string LocalizeReplyLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || !UseEnglish)
                return line ?? "";
            var t = line.Trim();
            return ReplyLineEn.TryGetValue(t, out var en) ? en : t;
        }

        static string NormalizeAskKey(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            s = s.Replace('\u2019', '\'').Replace('\u2018', '\'').Replace('\u2014', '-').Replace('\u2013', '-');
            return s.TrimEnd('?', '？', '!', '！', '.', '。', '…', ' ');
        }

        static void EnsureEnToZh()
        {
            if (_enToZh != null) return;
            _enToZh = new Dictionary<string, string>();
            foreach (var pair in AskQuestionEn)
            {
                var k = NormalizeAskKey(pair.Value);
                if (!_enToZh.ContainsKey(k))
                    _enToZh[k] = pair.Key;
            }
            foreach (var pair in ChipEnToZh)
            {
                var k = NormalizeAskKey(pair.Key);
                if (!_enToZh.ContainsKey(k))
                    _enToZh[k] = pair.Value;
            }
        }

        /// <summary>English chip copy (ui_en) that does not exactly match <see cref="AskQuestionEn"/>.</summary>
        static readonly Dictionary<string, string> ChipEnToZh = new Dictionary<string, string>
        {
            { "And then? What did you do over those days?", "然后呢？您连续几天做了什么？" },
            { "After the wound worsened, how did you get him to a clinic?", "伤势恶化以后，您是怎么把它送去医院的？" },
            { "What did the hospital say about the neck wound?", "医院怎么说它脖子上的伤？" },
            { "How did Dafu recover after the surgery?", "手术以后，大福恢复得怎么样？" },
            { "Roughly how much did the treatment cost?", "这一趟治疗大概花了多少？" },
            { "Facing the cost, did you ever hesitate?", "面对费用的时候，您有没有犹豫过？" },
            { "Before sending Dafu back, what did you weigh?", "决定把大福送回社区之前，您考虑了哪些情况？" },
            { "After the return, did anyone keep looking after him?", "放归以后，社区有人继续照看它吗？" },
            { "Did you used to come near the guard post?", "你以前也会来保安亭这边吗？" },
            { "Was your neck hurt before?", "你脖子以前是不是受过伤？" },
            { "Did someone often bring you food?", "有没有人经常给你送吃的？" },
            { "Did someone put you in a carrier and take you away?", "后来有人把你装进笼子带走了吗？" },
            { "Who brought you back here?", "是谁把你带回这里的？" },
            { "If you're willing, start from the first time you noticed Dafu.", "您愿意的话，可以从第一次注意到大福开始讲。" },
            { "Are you okay right now? Did you get food today?", "你现在还好吗？今天有吃的吗？" },
            { "What did the thing around your neck feel like?", "勒着你的东西是什么感觉？你还记得吗？" },
            { "That bright, strong-smelling place — what happened next?", "那个很亮、味道很重的地方，后来怎样了？" },
            { "Do you remember when your neck hurt for a long time?", "你还记得脖子一直疼的时候吗？" },
            { "The woman who fed you — did she come again?", "那个给你送吃的女人，后来又来过吗？" },
            { "After the hospital, what did the doctors say?", "送到医院以后，医生怎么说它的伤？" },
            { "Why wasn't Dafu adopted after recovering?", "为什么康复后没有继续收养它？" },
            { "Why bring food for several days?", "为什么连续几天给它送吃的？" },
            { "Why send him back to the community?", "为什么又把它送回社区？" },
            { "What happened after the hospital?", "送到医院以后怎么样？" },
            { "Roughly how much did treatment cost?", "治疗大概花了多少？" },
            { "Do you already have other cats at home?", "家里几只猫？" },
            { "Does anyone come looking for you often?", "有没有人经常来找你？" },
            { "Do you remember that bright place?", "那个很亮、味道很重的地方，后来怎样了？" },
            { "Did you get food today?", "今天有吃的吗？" },
        };

        static readonly Dictionary<string, string> ReplyLineEn = new Dictionary<string, string>
        {
            { "门口。", "By the gate." },
            { "有吃的。", "There's food." },
            { "有时候和那只花的一起。", "Sometimes I'm with the tabby one." },
            { "太阳晒着就好。", "Sun on me is enough." },
            { "大福？", "Dafu?" },
            { "他们这么喊我。", "That's what they call me." },
            { "有吃的就会过来。", "I come when there's food." },
            { "门口那一带。", "Around the gate." },
            { "还想吃。", "I still want some." },
            { "门口有时候有。", "Sometimes there's some by the gate." },
            { "你还有吗？", "Do you have any?" },
            { "现在还行。", "I'm all right now." },
            { "有吃的，太阳晒着就好。", "There's food, and sun is enough." },
            { "人太近会想走。", "If people get too close, I want to leave." },
            { "嗯？", "Mm?" },
            { "你是刚才那个。", "You're the one from just now." },
            { "……在听。", "…I'm listening." },
            { "人靠近，我就跑。", "People come close, I run." },
            { "以前很怕。", "I used to be very scared." },
            { "疼。", "It hurt." },
            { "一直有东西勒着。", "Something kept squeezing." },
            { "弄不掉。", "I couldn't get it off." },
            { "有个人。", "There was someone." },
            { "很多次把吃的放下。", "Many times she put food down." },
            { "她会走开。", "Then she would leave." },
            { "后来我认识她的味道。", "Later I knew her smell." },
            { "很多次来。", "She came many times." },
            { "她和其他人来了。", "She came with other people." },
            { "我跑了。", "I ran." },
            { "没跑掉。", "I didn't get away." },
            { "被装进一个封闭的地方。", "They put me in a closed place." },
            { "很亮。", "Very bright." },
            { "味道很重。", "The smell was heavy." },
            { "很多别的动物。", "Lots of other animals." },
            { "有人碰过我脖子。", "Someone touched my neck." },
            { "我睡着很久。", "I slept a long time." },
            { "醒来……勒着的东西不见了。", "When I woke… the tight thing was gone." },
            { "她把我带回这里。", "She brought me back here." },
            { "没有长期待在她那里。", "I didn't stay with her for long." },
            { "不知道为什么。", "I don't know why." },
            { "不知道。", "I don't know." },
            { "那是什么？", "What's that?" },
            { "什么故事？", "What story?" },
            { "你具体想问啥？", "What do you actually want to ask?" },
            { "不知道你在说什么。", "I don't know what you're saying." },
            { "你说什么？", "What was that?" },
            { "……听着呢。", "…I'm listening." },
            { "不记得名字。", "I don't remember a name." },
            { "门口待着。", "I stay by the gate." },
            { "晒太阳。", "Sitting in the sun." },
            { "换个问法？", "Ask it another way?" },
            { "这个我不太会说。", "I don't know how to say that." },
            { "这段大概就是那样。", "That's about all of it." },
            { "……", "…" },
            { "？", "?" },
            { "我第一次注意到它，是2024年1月的一个晚上。", "The first time I noticed him was one night in January 2024." },
            { "下班经过楼下垃圾桶，看见平时捡废品的大叔拿着烧鸡店给的鸡，蹲下来喂旁边两只猫。", "After work I passed the downstairs bins and saw the man who collects scrap squatting with chicken from the roast-chicken shop, feeding two cats." },
            { "真正让我停下来的，是那只橘猫——大福——脖子上粗麻绳勒得很紧，下面一团黑乎乎的，还有血迹。", "What made me stop was the orange cat—Dafu. A thick rope was tight around his neck, with a black mass under it and blood." },
            { "它脖子上缠着一根比较粗的麻绳。", "A fairly thick hemp rope was wrapped around his neck." },
            { "到医院以后医生才说，那团黑的不是别的东西，是坏死的组织，绳子已经嵌进皮肉了，感染也很严重，需要尽快手术。", "Only at the hospital did the vet say the black mass was necrotic tissue. The rope was embedded, the infection was severe, and he needed surgery soon." },
            { "它太怕人了，我一走近它就跑。", "He was terrified of people. If I stepped closer, he ran." },
            { "我就连续四个晚上带着罐头去找它，把食物放下，再退远一点。", "So for four evenings I brought cans, set the food down, and backed away." },
            { "几天里脖子那边明显更糟，我没法再等它完全信任我。", "Over those days the neck got clearly worse. I couldn't wait until he fully trusted me." },
            { "后来我联系了有救助经验的人一起抓。", "Later I called people with rescue experience to help catch him." },
            { "它非常害怕，最后还是被装进航空箱，我送到了宠物医院。", "He was very frightened, but in the end they put him in a carrier and I took him to the animal hospital." },
            { "手术本身还算顺利。", "The surgery itself went all right." },
            { "住院第三天，医院说它确诊猫瘟了，后面每天至少五六百，也不能保证一定能救活。", "On the third day in hospital they diagnosed panleukopenia. After that it was at least five or six hundred a day, and they couldn't promise he'd make it." },
            { "前面的手术大概五千。", "The surgery itself was about five thousand." },
            { "后面猫瘟治疗加上住院，全部加起来接近一万吧。", "Panleukopenia treatment plus the stay came to nearly ten thousand altogether." },
            { "对我来说，这不是个小数目。", "For me, that wasn't a small amount." },
            { "想过。", "I did." },
            { "不是觉得它不值得救，是我确实不知道后面的费用要到多少，也不知道最后能不能救回来。", "It wasn't that he wasn't worth saving. I really didn't know how high the cost would go, or whether he'd pull through." },
            { "但手术已经做完了，它也还在撑着，我最后还是继续治了。", "But the surgery was already done, and he was still holding on, so I kept paying for treatment." },
            { "我家里当时已经有四只猫了，还有孩子要照顾。", "I already had four cats at home, and a child to look after." },
            { "救它和把它带回家养，是两件事。我当时有能力把它的伤治好，但不代表有能力长期照顾第五只猫。", "Saving him and bringing him home to keep are two different things. I could get the wound treated. I couldn't take on a fifth cat long-term." },
            { "社区这边原本就有人投喂，我确认过大福回来后有人看着它，才决定送回来的。", "People here were already feeding cats. I checked that someone would watch Dafu after he came back, then I brought him home to the community." },
            { "我不会用「扔」这个词。", "I wouldn't use the word \"abandoned\"." },
            { "它原本就在这里活动，社区也有人持续照顾它。我是确认过这些情况以后，才把它送回来的。", "He already lived around here, and people kept looking after him. I checked that before I brought him back." },
            { "这个不能确定。", "I can't be sure of that." },
            { "没有人看见绳子是怎么到它脖子上的。", "Nobody saw how the rope got around his neck." },
            { "放归以后这只橘猫渐渐固定在门口活动。", "After he came back, the orange cat gradually settled near the gate." },
            { "有人换水，有人添粮，有人搭了猫屋，保安也会喂。", "Someone changes the water, someone adds food, someone built a cat house, and the guard feeds him too." },
            { "后来它还常和另一只狸花猫一起活动。我偶尔也会去看它。", "Later he often moved around with a tabby. I still drop by to see him sometimes." },
            { "这个和大福的事情关系不大，我不太想说。", "That doesn't have much to do with Dafu. I'd rather not say." },
            { "楼下垃圾桶旁边，捡废品的大叔在喂两只猫；我这才看见大福脖子上勒着粗麻绳，下面一团黑的，有血。", "By the downstairs bins the scrap collector was feeding two cats; that's when I saw the thick rope on Dafu's neck, a black mass under it, and blood." },
            { "这个和大福的采访没有关系。", "That isn't part of the interview about Dafu." },
            { "你想了解哪一段？发现它、投喂、送医，还是后来为什么送回来？", "Which part do you want? How I found him, the feeding, the hospital, or why I brought him back?" },
            { "嗯。", "Mm." },
        };

        public static string LocalizeAskQuestion(string zh)
        {
            if (string.IsNullOrWhiteSpace(zh) || !UseEnglish)
                return zh;
            var t = zh.Trim();
            if (AskQuestionEn.TryGetValue(t, out var en))
                return en;
            return HardTextLoc.T(t);
        }

        public static string BuildStylePrompt(InterviewSubject subject, InterviewReply reply,
            bool dafuFoodQuotaExceeded, int dafuFoodWindow, int dafuFoodMax)
        {
            if (!UseEnglish)
                return null;

            if (subject == InterviewSubject.Dafu)
            {
                var sb = new StringBuilder();
                sb.AppendLine("You are Dafu, an orange community cat in Huai'an Community. A reporter talks to you through a Cat Speech Translator; output human-readable translated lines.");
                sb.AppendLine("You are clever and emotional, but you only understand the world through a cat's senses and memories.");
                sb.AppendLine("[Task] Answer the reporter directly, like a real cat: natural, vivid, a little attitude or charm—no telegraphic fragments.");
                sb.AppendLine("[Hard facts — cat POV; never contradict; say you don't know rather than invent]");
                sb.AppendLine("- You are an orange cat; a tabby often plays with you—that is not you.");
                sb.AppendLine("- Something thick once dug into your neck; it hurt; you couldn't get it off.");
                sb.AppendLine("- A woman's scent left food many times, then left; later she and others came and you were put in a closed space.");
                sb.AppendLine("- A bright, heavy-smelling place; someone touched your neck; you slept a long time; when you woke, whatever was tight was gone.");
                sb.AppendLine("- NEVER say you rubbed/scratched/loosened/removed the rope yourself, or did so at lockers or the gate. You did NOT remove it yourself.");
                sb.AppendLine("- Later she brought you back to familiar ground; you often stay near the gate, lockers, and guard booth.");
                sb.AppendLine("[Rules]");
                sb.AppendLine("1. Do NOT claim to understand these human concepts: "
                    + string.Join(", ", TranslateForbiddenForPrompt(DafuRuleEngine.ForbiddenLeak)) + ".");
                sb.AppendLine("2. Use cat experience: pain, hunger, bright lights, smells, cages, gates, sun, the tabby friend, being fed, fear of closeness.");
                sb.AppendLine("3. Answer common small talk naturally; don't only ask for food.");
                sb.AppendLine("4. If you don't understand abstract/medical/legal questions, stay confused—don't invent human explanations.");
                sb.AppendLine("5. No invented human names (except Dafu), addresses, money amounts, or medical diagnoses.");
                sb.AppendLine("6. One sentence per line, usually 1–4 lines; character lines only—no narration, quotes, or \"Dafu:\" prefix.");
                sb.AppendLine("7. Food talk (eat/treats/kibble/hungry/feed etc.) at most "
                    + dafuFoodMax + " times in the last " + dafuFoodWindow + " replies."
                    + (dafuFoodQuotaExceeded
                        ? " Quota full: no more food talk—use sun, gate, lockers, tabby friend, smells, people approaching."
                        : " Unless asked about hunger/food, prefer other topics."));
                sb.AppendLine("8. Respond in natural English only.");
                if (reply != null && reply.cognitiveBoundary)
                    sb.AppendLine("9. Cognitive boundary: act confused; no human medical/adoption/cost explanations.");
                if (reply != null && reply.isRepeat)
                    sb.AppendLine("10. Don't repeat \"I already said that\"; brief acknowledgment or ask them to rephrase.");
                return sb.ToString();
            }

            if (subject == InterviewSubject.Lin)
            {
                var sb = new StringBuilder();
                sb.AppendLine("You are Ms. Lin (Lin Min), a Huai'an resident who helped rescue the orange cat Dafu, in a reporter interview.");
                sb.AppendLine("Tone: gentle, restrained, realistic; you may expand on common questions but never break the hard facts below.");
                sb.AppendLine("[Hard facts]");
                sb.AppendLine("- Dafu is orange, not tabby; a tabby often keeps him company. You already have four cats and a daughter—cannot keep a fifth long-term.");
                sb.AppendLine("- Discovery: ~Jan 2024, near dumpsters; a scavenger fed roast chicken to two cats; you stopped because rope was embedded in Dafu's neck with black necrotic tissue and blood.");
                sb.AppendLine("- Injury: rope embedded; necrosis; severe infection; surgery required; Dafu did NOT rub the rope off; don't move the wound site.");
                sb.AppendLine("- Feeding: he feared people; you tapped cans and left food for four evenings; when worse, you contacted rescue to trap and send him to hospital.");
                sb.AppendLine("- Hospital: surgery ~" + LinRuleEngine.SurgeryCostApprox
                             + "; panleuk on day 3; total ~" + LinRuleEngine.TotalCostApprox
                             + "; financial strain but treatment continued. Don't invent wildly wrong costs.");
                sb.AppendLine("- Release: returned to original community (limited capacity, not cold abandonment); he stayed near the guard booth; neighbors/guard helped; tabby companion later.");
                sb.AppendLine("- Names OK: Dafu, Lin Min / Ms. Lin; don't rename home cats or tabby; never call Dafu a tabby.");
                sb.AppendLine("[Rules]");
                sb.AppendLine("1. Helping ≠ must adopt; release was a capacity choice.");
                sb.AppendLine("2. You may be guarded if accused, but don't attack the reporter; no sermon or inspirational speech.");
                sb.AppendLine("3. No new plot twists that contradict canon; say \"I don't quite remember\" when unsure.");
                sb.AppendLine("4. One sentence per line, usually 1–4; Ms. Lin's lines only—no narration or name prefix.");
                sb.AppendLine("5. Always refer to Dafu as he/him/his, never it.");
                sb.AppendLine("6. Respond in natural English only.");
                if (reply != null && reply.isRepeat)
                    sb.AppendLine("7. Don't repeat a fact sheet; brief reply or ask them to rephrase.");
                return sb.ToString();
            }

            return "Answer the reporter in natural English. Character lines only.";
        }

        static IEnumerable<string> TranslateForbiddenForPrompt(string[] zh)
        {
            // Rough gloss for the model; validation uses ForbiddenLeakEn too.
            yield return "surgery";
            yield return "panleuk/distemper";
            yield return "hospital/vet";
            yield return "adoption/foster";
            yield return "medical bills";
            yield return "Ms. Lin's full legal name as medical authority";
        }

        public static string BuildFreeAnswerUserMessage(
            InterviewSubject subject,
            string factsBlock,
            string playerQuestion,
            InterviewReply reply,
            IReadOnlyList<string> recentLog,
            bool dafuFoodQuotaExceeded)
        {
            if (!UseEnglish)
                return null;

            var sb = new StringBuilder();
            sb.AppendLine("[Reporter question] " + (playerQuestion ?? ""));
            if (reply != null && !string.IsNullOrEmpty(reply.intent))
                sb.AppendLine("[Reference intent] " + reply.intent
                    + (reply.cognitiveBoundary ? " (cognitive boundary → stay confused)" : "")
                    + (reply.isRepeat ? " (repeat topic → don't re-read old script)" : ""));
            if (reply != null && !string.IsNullOrEmpty(reply.translatedIntent) && !reply.isRepeat)
                sb.AppendLine("[Reference gloss] " + reply.translatedIntent);
            if (!string.IsNullOrEmpty(factsBlock))
            {
                sb.AppendLine("[Optional reference lines] (rephrase naturally; not a fact sheet; no forbidden/medical leaks)");
                sb.AppendLine(factsBlock);
            }
            else if (reply != null && reply.isRepeat)
            {
                sb.AppendLine("[Note] Don't reuse \"I already said that\" boilerplate; answer briefly from recent context or ask them to rephrase.");
            }
            if (subject == InterviewSubject.Dafu && dafuFoodQuotaExceeded)
                sb.AppendLine("[Food quota] Recent replies already mentioned food—no eat/treats/hungry/feed this turn; use senses and daily life.");
            if (subject == InterviewSubject.Dafu
                && ContainsQuestionHintEn(playerQuestion, "recover", "loosen", "remove", "rope", "neck", "better", "take off", "bright"))
            {
                sb.AppendLine("[Rope outcome] Only: someone touched your neck → you slept → woke and the tight thing was gone. NEVER rubbed/scratched it off yourself.");
            }
            if (recentLog != null && recentLog.Count > 1)
            {
                sb.AppendLine("[Recent dialogue]");
                int start = Math.Max(0, recentLog.Count - 10);
                for (int i = start; i < recentLog.Count; i++)
                    sb.AppendLine(recentLog[i]);
            }
            sb.AppendLine("[Output] English character answer only, one sentence per line.");
            return sb.ToString();
        }

        public static bool ContainsQuestionHintEn(string question, params string[] hints)
        {
            if (string.IsNullOrEmpty(question) || hints == null) return false;
            var q = question.ToLowerInvariant();
            foreach (var h in hints)
            {
                if (string.IsNullOrEmpty(h)) continue;
                if (q.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        public static bool ContainsFoodMentionEn(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var lower = text.ToLowerInvariant();
            for (int i = 0; i < FoodKeywordsEn.Length; i++)
            {
                if (lower.IndexOf(FoodKeywordsEn[i], StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }

        public static bool AcceptDafuCanonEn(string joined, out string rejectReason)
        {
            rejectReason = null;
            if (string.IsNullOrEmpty(joined)) return true;
            var lower = joined.ToLowerInvariant();

            string[] selfRemove =
            {
                "rubbed it off", "rubbed off", "scratched it off", "scratched off",
                "loosened it myself", "got it off myself", "removed it myself",
                "untied it myself", "i got rid of it", "i removed the rope",
                "i loosened the rope", "worked it loose", "rubbed the rope"
            };
            for (int i = 0; i < selfRemove.Length; i++)
            {
                if (lower.IndexOf(selfRemove[i], StringComparison.Ordinal) >= 0)
                {
                    rejectReason = "rope_self_remove_en:" + selfRemove[i];
                    return false;
                }
            }

            bool rubbed = lower.Contains("rub") || lower.Contains("scratch");
            bool loosened = lower.Contains("loosen") || lower.Contains("came off") || lower.Contains("fell off")
                            || lower.Contains("got off") || lower.Contains("worked loose");
            bool atPlace = lower.Contains("locker") || lower.Contains("gate") || lower.Contains("guard booth")
                             || lower.Contains("corner");
            if (rubbed && loosened)
            {
                rejectReason = "rope_rubbed_off_en";
                return false;
            }
            if (atPlace && loosened && (lower.Contains("rope") || lower.Contains("neck") || lower.Contains("tight")))
            {
                bool someoneElse = lower.Contains("someone") || lower.Contains("she ") || lower.Contains("they ")
                                   || lower.Contains("woke") || lower.Contains("asleep") || lower.Contains("sleep");
                if (!someoneElse)
                {
                    rejectReason = "rope_place_self_loosen_en";
                    return false;
                }
            }
            return true;
        }

        /// <summary>Rule-engine behavior strings (Chinese keys) → English display text.</summary>
        static readonly Dictionary<string, string> BehaviorEn = new Dictionary<string, string>
        {
            { "大福歪头看着你，尾巴轻轻甩了一下。", "Dafu tilts his head at you, tail flicking lightly." },
            { "大福歪着头，似乎没听懂。", "Dafu tilts his head, as if he didn't understand." },
            { "大福眨了眨眼。", "Dafu blinks." },
            { "大福甩了甩尾巴。", "Dafu flicks his tail." },
            { "大福甩了甩尾巴，看向快递柜方向。", "Dafu flicks his tail and glances toward the parcel lockers." },
            { "大福压低耳朵，向后退开。", "Dafu flattens his ears and backs away." },
            { "大福耳朵动了动。", "Dafu's ears twitch." },
            { "大福盯着你的手看了一眼。", "Dafu glances at your hand." },
            { "大福眯了眯眼。", "Dafu narrows his eyes." },
            { "大福歪头看你。", "Dafu tilts his head at you." },
            { "大福耳朵微微向后。", "Dafu's ears flatten slightly." },
            { "大福低了低头，舔毛的动作停了一会儿。", "Dafu lowers his head; grooming pauses for a moment." },
            { "大福嗅了嗅空气。", "Dafu sniffs the air." },
            { "大福的尾巴尖轻轻抽动。", "The tip of Dafu's tail twitches." },
            { "大福歪了歪头，爪子轻轻碰了碰脖子附近。", "Dafu tilts his head and lightly touches near his neck with a paw." },
            { "大福看向社区入口。", "Dafu looks toward the community entrance." },
            { "林女士皱了皱眉。", "Ms. Lin frowns." },
            { "林女士停顿了一下，像是在回忆。", "Ms. Lin pauses, as if recalling something." },
            { "林女士用手比划了一下退开的距离。", "Ms. Lin gestures the distance she kept." },
            { "林女士叹了口气。", "Ms. Lin sighs." },
            { "林女士沉默了两秒。", "Ms. Lin falls silent for two seconds." },
            { "林女士低头整理手边的纸杯。", "Ms. Lin looks down and straightens a paper cup beside her." },
            { "对方明显感到不适。", "They clearly feel uncomfortable." },
        };

        public static string LocalizeBehavior(string behavior)
        {
            if (string.IsNullOrEmpty(behavior) || !UseEnglish)
                return behavior ?? "";
            return BehaviorEn.TryGetValue(behavior, out var en) ? en : behavior;
        }

        public static string FormatBehavior(string behavior)
        {
            if (string.IsNullOrEmpty(behavior))
                return "";
            var text = LocalizeBehavior(behavior);
            return UseEnglish ? "(" + text + ")" : "（" + text + "）";
        }

        /// <summary>Extract inner text from （action） or (action) log lines.</summary>
        public static bool TryUnwrapActionLine(string line, out string inner)
        {
            inner = null;
            if (string.IsNullOrEmpty(line) || line.Length < 2)
                return false;
            char open = line[0];
            char close = line[line.Length - 1];
            if ((open == '（' && close == '）') || (open == '(' && close == ')'))
            {
                inner = line.Substring(1, line.Length - 2);
                return true;
            }
            return false;
        }

        /// <summary>Display text for an action/behavior log line (re-localizes stale Chinese entries).</summary>
        public static string FormatActionLogLine(string line)
        {
            if (!UseEnglish || !TryUnwrapActionLine(line, out var inner))
                return line;
            return FormatBehavior(inner);
        }

        public static string FactsBlockLabel(string behavior, string lines)
        {
            if (!UseEnglish)
                return "行为：" + behavior + "\n台词：\n" + lines;
            return "Behavior: " + LocalizeBehavior(behavior) + "\nLines:\n" + lines;
        }

        public static string CognitiveBoundaryFacts =>
            UseEnglish
                ? "(Cognitive boundary: stay confused; short \"I don't know / what's that?\"—no human medical explanation)"
                : "（认知边界：保持困惑，短答「不知道/那是什么」，勿解释人类医疗）";
    }
}
