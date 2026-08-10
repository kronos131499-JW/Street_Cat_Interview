using System;
using System.Collections.Generic;
using StreetCat.Data;
using StreetCat.Notebook;
using UnityEngine;

namespace StreetCat.Core
{
    [Serializable]
    public class GameSaveData
    {
        public string currentSceneId = SceneIds.SC01;
        public string currentObjective = "完成周五的工作。";
        public string uiMode = "dialogue";
        public string saveTitle = "";
        public long savedAtUnix;
        public List<string> intel = new List<string>();
        public List<string> flags = new List<string>();
        public List<string> unlockedMaterials = new List<string>();
        public List<string> selectedMaterials = new List<string>();
        public List<string> confirmedNotes = new List<string>();
        public List<string> pendingQuestions = new List<string>();
        public List<NotebookTopicSave> topics = new List<NotebookTopicSave>();
        public List<NotebookQaEntry> notebookQa = new List<NotebookQaEntry>();
        /// <summary>0 = legacy topic status ints; 2 = New/Open/Complete.</summary>
        public int notebookFormat;
        public List<HistoryLineSave> historyLines = new List<HistoryLineSave>();
        public int writingDirection = -1;
        /// <summary>Legacy save fields; phrasing step removed — unused.</summary>
        public int phrasingChoiceA = -1;
        public int phrasingChoiceB = -1;
        public int lastReviewScore;
        public string lastArticleTitle = "";
        public string lastArticleBody = "";
        public bool dafuCognitiveBoundaryHit;
        public int crossChecksCompleted;
    }

    [Serializable]
    public class NotebookTopicSave
    {
        public string id;
        public string title;
        public int status;
        public List<string> bullets = new List<string>();
        public List<string> noteIds = new List<string>();
        public List<string> sources = new List<string>();
    }

    public class GameState
    {
        public static GameState Instance { get; private set; }

        public GameSaveData Data { get; private set; } = new GameSaveData();

        public event Action OnStateChanged;
        public event Action<string> OnIntelGained;
        public event Action<string> OnObjectiveChanged;

        public static void Ensure()
        {
            if (Instance == null)
                Instance = new GameState();
        }

        public void ResetNewGame()
        {
            Data = new GameSaveData();
            Notify();
        }

        public bool HasFlag(string id) => Data.flags.Contains(id);

        public void SetFlag(string id, bool value = true)
        {
            if (value)
            {
                if (!Data.flags.Contains(id))
                {
                    Data.flags.Add(id);
                    Notify();
                }
            }
            else if (Data.flags.Remove(id))
            {
                Notify();
            }
        }

        public bool HasIntel(string id) => Data.intel.Contains(id);

        public bool GrantIntel(string id, string noteLine = null)
        {
            if (string.IsNullOrEmpty(id) || Data.intel.Contains(id))
                return false;

            Data.intel.Add(id);
            if (!string.IsNullOrEmpty(noteLine) && !Data.confirmedNotes.Contains(noteLine))
                Data.confirmedNotes.Add(noteLine);

            MaterialUnlockTable.TryUnlockFromIntel(id);
            OnIntelGained?.Invoke(id);
            Notify();
            return true;
        }

        public void AddPendingQuestion(string q)
        {
            if (string.IsNullOrEmpty(q) || Data.pendingQuestions.Contains(q))
                return;
            Data.pendingQuestions.Add(q);
            Notify();
        }

        public void SetObjective(string objective)
        {
            Data.currentObjective = objective;
            OnObjectiveChanged?.Invoke(objective);
            Notify();
        }

        public void SetScene(string sceneId)
        {
            Data.currentSceneId = sceneId;
            Notify();
        }

        public void UnlockMaterial(string materialId)
        {
            if (string.IsNullOrEmpty(materialId) || Data.unlockedMaterials.Contains(materialId))
                return;
            Data.unlockedMaterials.Add(materialId);
            Notify();
        }

        public void Notify() => OnStateChanged?.Invoke();

        public void Load(GameSaveData data)
        {
            Data = data ?? new GameSaveData();
            Notify();
        }
    }

    public static class MaterialUnlockTable
    {
        static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>
        {
            { IntelIds.FixedFeedingPoint, new[] { MaterialIds.M01, MaterialIds.M14 } },
            { IntelIds.DafuRestSpot, new[] { MaterialIds.M01 } },
            { IntelIds.DafuAppearTime, new[] { MaterialIds.M01 } },
            { IntelIds.CommunityCare, new[] { MaterialIds.M14 } },
            { IntelIds.DafuNoOwner, new[] { MaterialIds.M14 } },
            { IntelIds.PastAfraid, new[] { MaterialIds.M02 } },
            { IntelIds.NeckPain, new[] { MaterialIds.M03 } },
            { IntelIds.NeckObject, new[] { MaterialIds.M03 } },
            { IntelIds.Sleep, new[] { MaterialIds.M04 } },
            { IntelIds.ObjectGone, new[] { MaterialIds.M04 } },
            { IntelIds.RopeEmbedded, new[] { MaterialIds.M05 } },
            { IntelIds.FeedFourDays, new[] { MaterialIds.M06 } },
            { IntelIds.CaptureSuccess, new[] { MaterialIds.M07 } },
            { IntelIds.TakenAway, new[] { MaterialIds.M07 } },
            { IntelIds.PanleukopeniaDay3, new[] { MaterialIds.M08 } },
            { IntelIds.TotalCost, new[] { MaterialIds.M09 } },
            { IntelIds.LinHesitated, new[] { MaterialIds.M10 } },
            { IntelIds.FourCatsHome, new[] { MaterialIds.M11 } },
            { IntelIds.CannotFifth, new[] { MaterialIds.M12 } },
            { IntelIds.ReturnOriginalArea, new[] { MaterialIds.M13 } },
            { IntelIds.ReturnedDafu, new[] { MaterialIds.M13 } },
            { IntelIds.TabbyPartner, new[] { MaterialIds.M15 } },
            { IntelIds.CauseUnknown, new[] { MaterialIds.M16 } },
        };

        public static void TryUnlockFromIntel(string intelId)
        {
            if (!Map.TryGetValue(intelId, out var mats))
                return;
            foreach (var m in mats)
                GameState.Instance.UnlockMaterial(m);
        }
    }
}
