using System;
using System.IO;
using System.Text;
using StreetCat.Data;
using UnityEngine;

namespace StreetCat.Interview
{
    /// <summary>
    /// Append-only interview Q&amp;A log for debugging AI replies.
    /// Path: Application.persistentDataPath/interview_session_log.txt
    /// </summary>
    public static class InterviewDebugLog
    {
        static string Path =>
            System.IO.Path.Combine(Application.persistentDataPath, "interview_session_log.txt");

        public static string LogPath => Path;

        public static void SessionStart(InterviewSubject subject)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("======== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                          + " | " + subject + " ========");
            Append(sb.ToString());
            Debug.Log("[InterviewLog] Writing to " + Path);
        }

        public static void Exchange(
            string question,
            string intent,
            bool freeMode,
            string ruleText,
            string aiText,
            string outcome,
            string detail = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine("Q: " + (question ?? ""));
            sb.AppendLine("intent: " + (intent ?? "?") + " | mode: " + (freeMode ? "free" : "rephrase")
                          + " | outcome: " + (outcome ?? "?"));
            if (!string.IsNullOrEmpty(detail))
                sb.AppendLine("detail: " + detail);
            if (!string.IsNullOrEmpty(ruleText))
                sb.AppendLine("RULE:\n" + ruleText.TrimEnd());
            if (!string.IsNullOrEmpty(aiText))
                sb.AppendLine("AI:\n" + aiText.TrimEnd());
            Append(sb.ToString());
            Debug.Log("[InterviewLog] " + (outcome ?? "?") + " | " + (intent ?? "?") + " | "
                      + Truncate(question, 40));
        }

        static void Append(string text)
        {
            try
            {
                File.AppendAllText(Path, text + "\n", Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[InterviewLog] write failed: " + e.Message);
            }
        }

        static string Truncate(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ");
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }
    }
}
