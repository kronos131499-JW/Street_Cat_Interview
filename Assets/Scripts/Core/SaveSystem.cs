using System;
using System.Collections.Generic;
using System.IO;
using StreetCat.Data;
using StreetCat.Loc;
using StreetCat.UI;
using UnityEngine;

namespace StreetCat.Core
{
    [Serializable]
    public class HistoryLineSave
    {
        public string speaker;
        public string text;
        public string kind;
    }

    // Extended on GameSaveData via partial-like additions — keep fields on GameSaveData in GameState.cs
    public static class SaveSystem
    {
        public const int ManualSlotCount = 6;
        public const int AutoSlot = -1;

        static string Dir => Application.persistentDataPath;

        static string SlotPath(int slot)
        {
            if (slot == AutoSlot)
                return Path.Combine(Dir, "streetcat_ch1_auto.json");
            return Path.Combine(Dir, $"streetcat_ch1_slot{slot}.json");
        }

        /// <summary>Legacy single-file path used by early builds.</summary>
        static string LegacyPath => Path.Combine(Dir, "streetcat_ch1_save.json");

        public static void CaptureRuntimeInto(GameSaveData data)
        {
            if (data == null) return;
            data.savedAtUnix = DateTimeOffset.Now.ToUnixTimeSeconds();
            data.saveTitle = BuildTitle(data);
            if (DialogueHistory.Instance != null)
                data.historyLines = DialogueHistory.Instance.ExportSaves();
        }

        public static void ApplyRuntimeFrom(GameSaveData data)
        {
            if (data == null) return;
            if (DialogueHistory.Instance != null)
                DialogueHistory.Instance.ImportSaves(data.historyLines);
            if (StreetCat.Notebook.ReporterNotebook.Instance != null)
                StreetCat.Notebook.ReporterNotebook.Instance.LoadFromSave();
        }

        static string BuildTitle(GameSaveData data)
        {
            var scene = string.IsNullOrEmpty(data.currentSceneId) ? SceneIds.SC01 : data.currentSceneId;
            var obj = string.IsNullOrEmpty(data.currentObjective)
                ? UiLoc.T("ui.save.in_progress", "进行中")
                : ScriptLoc.MapObjective(data.currentObjective);
            int max = GameSettings.IsEnglish ? 48 : 22;
            if (obj.Length > max) obj = obj.Substring(0, max) + "…";
            return scene + (GameSettings.IsEnglish ? "  " : "　") + obj;
        }

        public static string SlotName(int slot)
        {
            return slot == AutoSlot
                ? UiLoc.T("ui.save.auto_slot", "自动存档")
                : string.Format(UiLoc.T("ui.save.slot_fmt", "存档位 {0}"), slot + 1);
        }

        public static void SaveToSlot(int slot, GameSaveData data)
        {
            CaptureRuntimeInto(data);
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SlotPath(slot), json);
            // Keep legacy file in sync with auto for old Continue button
            if (slot == AutoSlot)
                File.WriteAllText(LegacyPath, json);
            Debug.Log($"[SaveSystem] Saved slot {slot} → {SlotPath(slot)}");
        }

        public static bool TryLoadSlot(int slot, out GameSaveData data)
        {
            data = null;
            var path = SlotPath(slot);
            if (!File.Exists(path) && slot == AutoSlot && File.Exists(LegacyPath))
                path = LegacyPath;
            if (!File.Exists(path))
                return false;
            try
            {
                data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
                return data != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool SlotExists(int slot)
        {
            if (File.Exists(SlotPath(slot))) return true;
            return slot == AutoSlot && File.Exists(LegacyPath);
        }

        public static SaveSlotInfo GetSlotInfo(int slot)
        {
            var info = new SaveSlotInfo { slot = slot };
            if (!TryLoadSlot(slot, out var data))
            {
                info.empty = true;
                info.label = SlotName(slot) + UiLoc.T("ui.save.slot_empty_suffix", "　（空）");
                info.detail = UiLoc.T("ui.save.slot_empty", "空");
                return info;
            }

            info.empty = false;
            var time = data.savedAtUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(data.savedAtUnix).LocalDateTime.ToString("yyyy-MM-dd HH:mm")
                : UiLoc.T("ui.save.unknown_time", "未知时间");
            info.label = SlotName(slot) + (GameSettings.IsEnglish ? "  " : "　") + time;
            // saveTitle was baked in the language active at save time; rebuild for the current one.
            info.detail = BuildTitle(data);
            info.objective = data.currentObjective ?? "";
            return info;
        }

        public static List<SaveSlotInfo> ListSlots(bool includeAuto)
        {
            var list = new List<SaveSlotInfo>();
            if (includeAuto)
                list.Add(GetSlotInfo(AutoSlot));
            for (int i = 0; i < ManualSlotCount; i++)
                list.Add(GetSlotInfo(i));
            return list;
        }

        public static void Autosave()
        {
            if (GameState.Instance == null) return;
            SaveToSlot(AutoSlot, GameState.Instance.Data);
        }

        public static void SaveManual(int slot)
        {
            if (slot < 0 || slot >= ManualSlotCount) return;
            if (GameState.Instance == null) return;
            SaveToSlot(slot, GameState.Instance.Data);
        }

        public static bool TryLoad(out GameSaveData data) => TryLoadSlot(AutoSlot, out data);

        public static void Delete()
        {
            TryDelete(AutoSlot);
            if (File.Exists(LegacyPath)) File.Delete(LegacyPath);
            for (int i = 0; i < ManualSlotCount; i++)
                TryDelete(i);
        }

        public static void TryDelete(int slot)
        {
            var p = SlotPath(slot);
            if (File.Exists(p)) File.Delete(p);
        }
    }

    [Serializable]
    public class SaveSlotInfo
    {
        public int slot;
        public bool empty;
        public string label;
        public string detail;
        public string objective;
    }
}
