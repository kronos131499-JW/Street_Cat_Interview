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
        /// After intel grants + material unlocks: write confirmed notes.
        /// Does not add a chat line. The "filed into cards" strip was a toast on the interview paper.
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
            foreach (var id in newMaterialIds)
            {
                var card = MaterialCatalog.Get(id);
                var title = card != null ? card.title : id;
                var note = BuildNote(subject, reply, id, title);
                if (gs != null && !string.IsNullOrEmpty(note) && !gs.Data.confirmedNotes.Contains(note))
                    gs.Data.confirmedNotes.Add(note);
            }

            // interviewLog stays in the signature so callers don't change. Nothing is appended.
            _ = interviewLog;
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
