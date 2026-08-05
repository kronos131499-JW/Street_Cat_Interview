using System;
using System.Collections.Generic;
using System.IO;
using StreetCat.Data;
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
        }

        static string BuildTitle(GameSaveData data)
        {
            var scene = string.IsNullOrEmpty(data.currentSceneId) ? SceneIds.SC01 : data.currentSceneId;
            var obj = string.IsNullOrEmpty(data.currentObjective) ? "进行中" : data.currentObjective;
            if (obj.Length > 22) obj = obj.Substring(0, 22) + "…";
            return $"{scene}　{obj}";
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
                info.label = slot == AutoSlot ? "自动存档　（空）" : $"存档位 {slot + 1}　（空）";
                info.detail = "空";
                return info;
            }

            info.empty = false;
            var time = data.savedAtUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(data.savedAtUnix).LocalDateTime.ToString("yyyy-MM-dd HH:mm")
                : "未知时间";
            var prefix = slot == AutoSlot ? "自动存档" : $"存档位 {slot + 1}";
            info.label = $"{prefix}　{time}";
            info.detail = string.IsNullOrEmpty(data.saveTitle) ? BuildTitle(data) : data.saveTitle;
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
