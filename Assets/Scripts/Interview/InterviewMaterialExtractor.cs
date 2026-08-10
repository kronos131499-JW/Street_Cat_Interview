using System.Collections.Generic;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Loc;
using StreetCat.Writing;

namespace StreetCat.Interview
{
    /// <summary>
    /// Turns newly unlocked intel into material-card unlocks (via <see cref="MaterialUnlockTable"/>)
    /// and player-facing extraction notes. Offline / rule-based; LLM can later enrich notes only.
    /// </summary>
    public static class InterviewMaterialExtractor
    {
        /// <summary>
        /// After intel grants + material unlocks: write confirmed notes and a transcript line.
        /// </summary>
        public static void ApplyExtraction(
            InterviewSubject subject,
            InterviewReply reply,
            IReadOnlyList<string> newMaterialIds,
            List<string> interviewLog)
        {
            if (newMaterialIds == null || newMaterialIds.Count == 0)
                return;

            var gs = GameState.Instance;
            var titles = new List<string>();
            foreach (var id in newMaterialIds)
            {
                var card = MaterialCatalog.Get(id);
                var title = card != null ? card.title : id;
                titles.Add(title);

                var note = BuildNote(subject, reply, id, title);
                if (gs != null && !string.IsNullOrEmpty(note) && !gs.Data.confirmedNotes.Contains(note))
                    gs.Data.confirmedNotes.Add(note);
            }

            if (interviewLog == null || titles.Count == 0)
                return;

            var joined = string.Join("、", titles);
            var line = string.Format(
                UiLoc.T("ui.interview.extract_materials", "【素材】已整理进素材卡：{0}"),
                joined);
            interviewLog.Add(line);
            gs?.Notify();
        }

        static string BuildNote(InterviewSubject subject, InterviewReply reply, string matId, string title)
        {
            var who = subject == InterviewSubject.Dafu ? "大福" : "林女士";
            var quote = FirstUsefulLine(reply);
            if (!string.IsNullOrEmpty(quote))
                return $"[{matId}] {title}（采访{who}）「{quote}」";
            return $"[{matId}] {title}（采访{who}）";
        }

        static string FirstUsefulLine(InterviewReply reply)
        {
            if (reply?.replyLines == null) return null;
            foreach (var line in reply.replyLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var t = line.Trim();
                if (t == "……" || t == "？" || t.Length < 2) continue;
                if (t.Length > 48) t = t.Substring(0, 47) + "…";
                return t;
            }
            return null;
        }
    }
}
