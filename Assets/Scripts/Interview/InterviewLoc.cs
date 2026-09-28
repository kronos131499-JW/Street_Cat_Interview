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
            { "送到医院以后，医生怎么说它的伤？", "After the hospital, what did the vet say about its injury?" },
            { "勒着你的东西是什么感觉？你还记得吗？", "What did whatever was around your neck feel like? Do you remember?" },
            { "您当时为什么连续几天给大福送吃的？", "Why did you keep bringing Dafu food for several days?" },
            { "手术以后，大福恢复得怎么样？", "How was Dafu's recovery after the surgery?" },
            { "你在那里后来发生了什么？", "What happened to you there afterward?" },
            { "决定把大福送回社区之前，您考虑了哪些情况？", "Before deciding to return Dafu to the community, what did you weigh?" },
            { "您是怎么注意到大福的？", "How did you first notice Dafu?" },
            { "为什么连续几天给它送吃的？", "Why did you feed it for several days in a row?" },
            { "送到医院以后怎么样？", "What happened after you got it to the hospital?" },
            { "为什么又把它送回社区？", "Why bring it back to the community afterward?" },
            { "你平时一般什么时候会来这里？", "When do you usually come here?" },
            { "有没有人经常来找你？", "Does anyone come to see you often?" },
            { "那个很亮、味道很重的地方，后来怎样了？", "What happened later in that bright, heavy-smelling place?" },
            { "有没有人经常给你送吃的？", "Does anyone bring you food often?" },
            { "后来有人把你装进笼子带走了吗？", "Did someone put you in a cage and take you away later?" },
            { "治疗大概花了多少？", "About how much did the treatment cost?" },
            { "您有没有犹豫过？", "Did you ever hesitate?" },
            { "为什么康复后没有继续收养它？", "Why not keep fostering it after it recovered?" },
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
                sb.AppendLine("- Dafu is orange, not tabby; a tabby often keeps it company. You already have four cats and a daughter—cannot keep a fifth long-term.");
                sb.AppendLine("- Discovery: ~Jan 2024, near dumpsters; a scavenger fed roast chicken to two cats; you stopped because rope was embedded in Dafu's neck with black necrotic tissue and blood.");
                sb.AppendLine("- Injury: rope embedded; necrosis; severe infection; surgery required; Dafu did NOT rub the rope off; don't move the wound site.");
                sb.AppendLine("- Feeding: it feared people; you tapped cans and left food for four evenings; when worse, you contacted rescue to trap and send it to hospital.");
                sb.AppendLine("- Hospital: surgery ~" + LinRuleEngine.SurgeryCostApprox
                             + "; panleuk on day 3; total ~" + LinRuleEngine.TotalCostApprox
                             + "; financial strain but treatment continued. Don't invent wildly wrong costs.");
                sb.AppendLine("- Release: returned to original community (limited capacity, not cold abandonment); it stayed near the guard booth; neighbors/guard helped; tabby companion later.");
                sb.AppendLine("- Names OK: Dafu, Lin Min / Ms. Lin; don't rename home cats or tabby; never call Dafu a tabby.");
                sb.AppendLine("[Rules]");
                sb.AppendLine("1. Helping ≠ must adopt; release was a capacity choice.");
                sb.AppendLine("2. You may be guarded if accused, but don't attack the reporter; no sermon or inspirational speech.");
                sb.AppendLine("3. No new plot twists that contradict canon; say \"I don't quite remember\" when unsure.");
                sb.AppendLine("4. One sentence per line, usually 1–4; Ms. Lin's lines only—no narration or name prefix.");
                sb.AppendLine("5. Respond in natural English only.");
                if (reply != null && reply.isRepeat)
                    sb.AppendLine("6. Don't repeat a fact sheet; brief reply or ask them to rephrase.");
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
