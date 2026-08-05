using System;
using System.Collections.Generic;
using System.Text;
using StreetCat.Core;
using UnityEngine;

namespace StreetCat.UI
{
    [Serializable]
    public class HistoryEntry
    {
        public string speaker;
        public string text;
        public string kind;
        public float time;
    }

    public class DialogueHistory : MonoBehaviour
    {
        public static DialogueHistory Instance { get; private set; }

        const int MaxEntries = 400;
        readonly List<HistoryEntry> entries = new List<HistoryEntry>();

        public IReadOnlyList<HistoryEntry> Entries => entries;

        void Awake()
        {
            Instance = this;
        }

        public void Clear()
        {
            entries.Clear();
        }

        public void Add(string speaker, string text, string kind = "dialogue")
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (entries.Count > 0)
            {
                var last = entries[entries.Count - 1];
                if (last.speaker == speaker && last.text == text)
                    return;
            }

            entries.Add(new HistoryEntry
            {
                speaker = speaker ?? "",
                text = text.Trim(),
                kind = kind ?? "dialogue",
                time = Time.unscaledTime
            });

            while (entries.Count > MaxEntries)
                entries.RemoveAt(0);
        }

        public List<HistoryLineSave> ExportSaves()
        {
            var list = new List<HistoryLineSave>(entries.Count);
            foreach (var e in entries)
            {
                list.Add(new HistoryLineSave
                {
                    speaker = e.speaker,
                    text = e.text,
                    kind = e.kind
                });
            }
            return list;
        }

        public void ImportSaves(List<HistoryLineSave> lines)
        {
            entries.Clear();
            if (lines == null) return;
            foreach (var l in lines)
            {
                if (l == null || string.IsNullOrWhiteSpace(l.text)) continue;
                entries.Add(new HistoryEntry
                {
                    speaker = l.speaker ?? "",
                    text = l.text,
                    kind = l.kind ?? "dialogue",
                    time = 0
                });
            }
            while (entries.Count > MaxEntries)
                entries.RemoveAt(0);
        }

        public string BuildPlainText()
        {
            var sb = new StringBuilder();
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.speaker))
                    sb.Append(e.speaker).Append("　");
                sb.AppendLine(e.text);
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }
    }
}
