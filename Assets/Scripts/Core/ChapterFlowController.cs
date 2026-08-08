using StreetCat.Data;
using StreetCat.Interview;
using StreetCat.Narrative;
using StreetCat.UI;
using UnityEngine;

namespace StreetCat.Core
{
    /// <summary>Drives chapter progression between scripted scenes and gameplay modes.</summary>
    public class ChapterFlowController : MonoBehaviour
    {
        public static ChapterFlowController Instance { get; private set; }

        [SerializeField] SceneDirector sceneDirector;
        [SerializeField] GameUI gameUi;

        void Awake()
        {
            Instance = this;
            GameState.Ensure();
            if (sceneDirector == null)
                sceneDirector = FindObjectOfType<SceneDirector>();
            if (gameUi == null)
                gameUi = FindObjectOfType<GameUI>();
        }

        public void StartNewGame()
        {
            GameState.Instance.ResetNewGame();
            if (DialogueHistory.Instance != null)
                DialogueHistory.Instance.Clear();
            GameState.Instance.SetScene(SceneIds.SC01);
            GameState.Instance.SetObjective("完成周五的工作。");
            sceneDirector.PlayScene(SceneIds.SC01);
            gameUi.ShowDialogueMode();
        }

        public void GoToTitle()
        {
            SaveSystem.Autosave();
            gameUi.ShowTitle();
        }

        public void ContinueOrNew()
        {
            if (SaveSystem.TryLoadSlot(SaveSystem.AutoSlot, out var data))
            {
                GameState.Instance.Load(data);
                SaveSystem.ApplyRuntimeFrom(data);
                ResumeFromState();
            }
            else
            {
                StartNewGame();
            }
        }

        public void LoadSlot(int slot)
        {
            if (!SaveSystem.TryLoadSlot(slot, out var data))
                return;
            GameState.Instance.Load(data);
            SaveSystem.ApplyRuntimeFrom(data);
            ResumeFromState();
        }

        public void ResumeFromState()
        {
            var data = GameState.Instance.Data;
            var id = data.currentSceneId;
            if (string.IsNullOrEmpty(id))
                id = SceneIds.SC01;

            var mode = data.uiMode ?? "";
            if (mode == "interview_dafu" || id == SceneIds.SC07)
            {
                gameUi.ShowInterview(InterviewSubject.Dafu);
                return;
            }
            if (mode == "interview_lin" || (id == SceneIds.SC09 && GameState.Instance.HasFlag(FlagIds.LinCafeIntroDone)))
            {
                gameUi.ShowInterview(InterviewSubject.Lin);
                return;
            }
            if (id == SceneIds.SC09)
            {
                sceneDirector.PlayScene(SceneIds.SC09);
                gameUi.ShowDialogueMode();
                return;
            }
            if (mode == "writing" || (id == SceneIds.SC10 && GameState.Instance.HasFlag(FlagIds.WritingDeskReady)))
            {
                gameUi.ShowWriting();
                return;
            }
            if (mode == "epilogue" || id == SceneIds.SC11)
            {
                gameUi.ShowEpilogue();
                return;
            }
            if (mode == "investigate" || id == SceneIds.SC04 || id == SceneIds.SC05 || id == SceneIds.SC08)
            {
                sceneDirector.PlayScene(id);
                gameUi.ShowInvestigationMode();
                return;
            }

            sceneDirector.PlayScene(id);
            gameUi.ShowDialogueMode();
        }

        public void GoToScene(string sceneId)
        {
            GameState.Instance.SetScene(sceneId);
            // Autosave before entering major beats (incl. interviews)
            SaveSystem.Autosave();

            switch (sceneId)
            {
                case SceneIds.SC07:
                    GameState.Instance.Data.uiMode = "interview_dafu";
                    gameUi.ShowInterview(InterviewSubject.Dafu);
                    break;
                case SceneIds.SC09:
                    if (GameState.Instance.HasFlag(FlagIds.LinCafeIntroDone))
                    {
                        GameState.Instance.Data.uiMode = "interview_lin";
                        gameUi.ShowInterview(InterviewSubject.Lin);
                    }
                    else
                    {
                        GameState.Instance.Data.uiMode = "dialogue";
                        sceneDirector.PlayScene(SceneIds.SC09);
                        gameUi.ShowDialogueMode();
                    }
                    break;
                case SceneIds.SC10:
                    GameState.Instance.SetFlag(FlagIds.WritingUnlocked);
                    if (GameState.Instance.HasFlag(FlagIds.WritingDeskReady))
                    {
                        GameState.Instance.Data.uiMode = "writing";
                        gameUi.ShowWriting();
                    }
                    else
                    {
                        GameState.Instance.Data.uiMode = "dialogue";
                        sceneDirector.PlayScene(SceneIds.SC10);
                        gameUi.ShowDialogueMode();
                    }
                    break;
                case SceneIds.SC11:
                    GameState.Instance.Data.uiMode = "epilogue";
                    gameUi.ShowEpilogue();
                    break;
                case SceneIds.SC04:
                    GameState.Instance.Data.uiMode = "investigate";
                    sceneDirector.PlayScene(sceneId);
                    gameUi.ShowInvestigationMode();
                    break;
                case SceneIds.SC05:
                case SceneIds.SC08:
                    // Guard-booth dialogue / talk — do not force community map investigate UI.
                    GameState.Instance.Data.uiMode = "dialogue";
                    sceneDirector.PlayScene(sceneId);
                    gameUi.ShowDialogueMode();
                    break;
                default:
                    GameState.Instance.Data.uiMode = "dialogue";
                    sceneDirector.PlayScene(sceneId);
                    gameUi.ShowDialogueMode();
                    break;
            }
        }

        public void OnDafuInterviewFinished()
        {
            GameState.Instance.SetFlag(FlagIds.DafuInterviewDone);
            GameState.Instance.SetObjective("向保安询问大福记忆中的女人。");
            GoToScene(SceneIds.SC08);
        }

        public void OnLinInterviewFinished()
        {
            GameState.Instance.SetFlag(FlagIds.LinInterviewDone);
            GameState.Instance.SetFlag(FlagIds.WritingUnlocked);
            GameState.Instance.SetObjective("整理素材，完成报道。");
            GoToScene(SceneIds.SC10);
        }

        /// <summary>Re-enter an interview from writing without wiping intel/materials.</summary>
        public void BeginReInterview(InterviewSubject subject)
        {
            var sceneId = subject == InterviewSubject.Lin ? SceneIds.SC09 : SceneIds.SC07;
            GameState.Instance.SetScene(sceneId);
            GameState.Instance.Data.uiMode =
                subject == InterviewSubject.Lin ? "interview_lin" : "interview_dafu";
            GameState.Instance.SetObjective(
                subject == InterviewSubject.Lin
                    ? "补充采访林女士，补齐写稿所需素材。"
                    : "补充采访大福，补齐写稿所需素材。");
            SaveSystem.Autosave();
            gameUi.ShowInterview(subject, returnToWritingAfter: true);
        }

        /// <summary>After a supplemental interview ends, resume the writing flow.</summary>
        public void ReturnToWritingFromReinterview()
        {
            GameState.Instance.SetFlag(FlagIds.WritingUnlocked);
            GameState.Instance.SetFlag(FlagIds.WritingDeskReady);
            GameState.Instance.SetScene(SceneIds.SC10);
            GameState.Instance.Data.uiMode = "writing";
            GameState.Instance.SetObjective("整理素材，完成报道。");
            SaveSystem.Autosave();
            gameUi.ShowWriting();
        }

        /// <summary>Called when SC-10 intro dialogue opens the writing desk.</summary>
        public void OpenWritingDeskFromScript()
        {
            GameState.Instance.SetFlag(FlagIds.WritingUnlocked);
            GameState.Instance.SetFlag(FlagIds.WritingDeskReady);
            GameState.Instance.SetScene(SceneIds.SC10);
            GameState.Instance.Data.uiMode = "writing";
            SaveSystem.Autosave();
            gameUi.ShowWriting();
        }

        public void OnArticlePublished()
        {
            GameState.Instance.SetFlag(FlagIds.ArticlePublished);
            GoToScene(SceneIds.SC11);
        }

        public void OnChapterComplete()
        {
            GameState.Instance.SetFlag(FlagIds.Chapter1Complete);
            SaveSystem.Autosave();
            gameUi.ShowTitle();
        }
    }
}
