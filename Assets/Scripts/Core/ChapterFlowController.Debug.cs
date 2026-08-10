#if UNITY_EDITOR || DEVELOPMENT_BUILD
using StreetCat.Data;
using StreetCat.Investigation;
using StreetCat.Interview;
using StreetCat.Narrative;
using StreetCat.Notebook;
using StreetCat.UI;
using UnityEngine;

namespace StreetCat.Core
{
    public partial class ChapterFlowController
    {
        public readonly struct DebugJumpTarget
        {
            public readonly string Id;
            public readonly string Label;

            public DebugJumpTarget(string id, string label)
            {
                Id = id;
                Label = label;
            }
        }

        /// <summary>Stable target ids for editor menus / F9 panel.</summary>
        public static readonly DebugJumpTarget[] DebugJumpTargets =
        {
            new DebugJumpTarget("title", "标题画面"),
            new DebugJumpTarget(SceneIds.SC01, "SC-01 周五下班前"),
            new DebugJumpTarget(SceneIds.SC02, "SC-02 喵语翻译器"),
            new DebugJumpTarget(SceneIds.SC03, "SC-03 保安猫大福"),
            new DebugJumpTarget(SceneIds.SC04, "SC-04 槐安社区（剧本）"),
            new DebugJumpTarget("investigate", "调查地图（槐安社区）"),
            new DebugJumpTarget(SceneIds.SC05, "SC-05 保安亭（剧本）"),
            new DebugJumpTarget("talk", "保安交谈菜单"),
            new DebugJumpTarget(SceneIds.SC06, "SC-06 上班的大福"),
            new DebugJumpTarget(SceneIds.SC07, "采访大福（SC-07）"),
            new DebugJumpTarget(SceneIds.SC08, "SC-08 寻找林女士"),
            new DebugJumpTarget(SceneIds.SC09, "SC-09 咖啡馆（见面剧本）"),
            new DebugJumpTarget("interview_lin", "采访林女士"),
            new DebugJumpTarget(SceneIds.SC10, "SC-10 写稿开场（剧本）"),
            new DebugJumpTarget("writing", "写稿桌 / 素材板"),
            new DebugJumpTarget("notebook", "记者笔记"),
            new DebugJumpTarget("epilogue", "后日谈（SC-11）"),
        };

        /// <summary>
        /// Instantly jump to a chapter beat for testing. Resets save state then seeds
        /// prerequisites appropriate to the target (not every flag for early scenes).
        /// </summary>
        public void DebugJumpTo(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                Debug.LogWarning("[DebugJump] empty target");
                return;
            }

            if (gameUi == null)
                gameUi = FindObjectOfType<GameUI>();
            if (sceneDirector == null)
                sceneDirector = FindObjectOfType<SceneDirector>();
            if (gameUi == null || sceneDirector == null)
            {
                Debug.LogWarning("[DebugJump] GameUI / SceneDirector missing — enter Play Mode first.");
                return;
            }

            var id = targetId.Trim();
            Debug.Log("[DebugJump] → " + id);

            gameUi.DebugCloseOverlays();
            PrepareFreshDebugState();

            switch (id)
            {
                case "title":
                    gameUi.ShowTitle();
                    return;

                case "investigate":
                    SeedThroughInvestigationMap();
                    GameState.Instance.SetScene(SceneIds.SC04);
                    GameState.Instance.Data.uiMode = "investigate";
                    GameState.Instance.SetObjective("在槐安社区调查大福的日常。");
                    sceneDirector.PlayScene(SceneIds.SC04);
                    gameUi.ShowInvestigationMode();
                    return;

                case "talk":
                case "guard_talk":
                    SeedThroughGuardTalk();
                    GameState.Instance.SetScene(SceneIds.SC05);
                    GameState.Instance.Data.uiMode = "dialogue";
                    GameState.Instance.SetObjective("向保安询问大福的情况。");
                    gameUi.ShowTalkMenu();
                    return;

                case "interview_dafu":
                case SceneIds.SC07:
                    SeedThroughDafuInterview();
                    GameState.Instance.SetScene(SceneIds.SC07);
                    GameState.Instance.Data.uiMode = "interview_dafu";
                    GameState.Instance.SetObjective("采访大福，了解它的过去。");
                    gameUi.ShowInterview(InterviewSubject.Dafu);
                    return;

                case "interview_lin":
                    SeedThroughLinInterview();
                    GameState.Instance.SetFlag(FlagIds.LinCafeIntroDone);
                    GameState.Instance.SetScene(SceneIds.SC09);
                    GameState.Instance.Data.uiMode = "interview_lin";
                    GameState.Instance.SetObjective("采访林女士，核实救助经过。");
                    gameUi.ShowInterview(InterviewSubject.Lin);
                    return;

                case "writing":
                    SeedThroughWriting();
                    OpenWritingDeskFromScript();
                    return;

                case "notebook":
                    SeedThroughInvestigationMap();
                    GameState.Instance.GrantIntel(IntelIds.FixedFeedingPoint, "固定投喂点在快递柜附近。");
                    GameState.Instance.GrantIntel(IntelIds.DafuRestSpot, "大福有固定休息点。");
                    GameState.Instance.SetScene(SceneIds.SC04);
                    GameState.Instance.Data.uiMode = "investigate";
                    sceneDirector.PlayScene(SceneIds.SC04);
                    gameUi.ShowInvestigationMode();
                    gameUi.DebugOpenNotebook();
                    return;

                case "epilogue":
                case SceneIds.SC11:
                    SeedThroughEpilogue();
                    GameState.Instance.SetScene(SceneIds.SC11);
                    GameState.Instance.Data.uiMode = "epilogue";
                    gameUi.ShowEpilogue();
                    return;

                case SceneIds.SC01:
                    GameState.Instance.SetScene(SceneIds.SC01);
                    GameState.Instance.SetObjective("完成周五的工作。");
                    EnterSceneImmediate(SceneIds.SC01);
                    return;

                case SceneIds.SC02:
                    GameState.Instance.SetScene(SceneIds.SC02);
                    GameState.Instance.SetObjective("去沈禾办公室一趟。");
                    EnterSceneImmediate(SceneIds.SC02);
                    return;

                case SceneIds.SC03:
                    SeedFlags(FlagIds.HasTranslator);
                    GameState.Instance.SetScene(SceneIds.SC03);
                    GameState.Instance.SetObjective("找到编辑部的保安猫。");
                    EnterSceneImmediate(SceneIds.SC03);
                    return;

                case SceneIds.SC04:
                    SeedThroughInvestigationMap();
                    EnterSceneImmediate(SceneIds.SC04);
                    return;

                case SceneIds.SC05:
                    SeedThroughGuardTalk(setIntroDone: false);
                    EnterSceneImmediate(SceneIds.SC05);
                    return;

                case SceneIds.SC06:
                    SeedThroughGuardTalk();
                    GameState.Instance.GrantIntel(IntelIds.DafuAppearTime, "大福通常在下午四五点出现。");
                    GameState.Instance.SetFlag(FlagIds.WaitingForDafu);
                    GameState.Instance.SetScene(SceneIds.SC06);
                    GameState.Instance.SetObjective("等待大福上班，试着用翻译器对话。");
                    EnterSceneImmediate(SceneIds.SC06);
                    return;

                case SceneIds.SC08:
                    SeedThroughDafuInterview();
                    GameState.Instance.SetFlag(FlagIds.DafuInterviewDone);
                    GameState.Instance.GrantIntel(IntelIds.WomanClue, "大福记得一名多次投喂并参与带走的女性。");
                    GameState.Instance.SetScene(SceneIds.SC08);
                    GameState.Instance.SetObjective("向保安打听救助者的线索。");
                    EnterSceneImmediate(SceneIds.SC08);
                    return;

                case SceneIds.SC09:
                    SeedThroughLinCafe();
                    EnterSceneImmediate(SceneIds.SC09);
                    return;

                case SceneIds.SC10:
                    SeedThroughWriting(deskReady: false);
                    EnterSceneImmediate(SceneIds.SC10);
                    return;

                default:
                    Debug.LogWarning("[DebugJump] unknown target: " + id);
                    return;
            }
        }

        void PrepareFreshDebugState()
        {
            GameState.Ensure();
            GameState.Instance.ResetNewGame();
            if (DialogueHistory.Instance != null)
                DialogueHistory.Instance.Clear();
            if (ReporterNotebook.Instance != null)
                ReporterNotebook.Instance.ResetNotebook();
            if (InvestigationService.Instance != null)
                InvestigationService.Instance.ResetForDebugJump();
        }

        static void SeedFlags(params string[] flags)
        {
            foreach (var f in flags)
                GameState.Instance.SetFlag(f);
        }

        void SeedThroughInvestigationMap()
        {
            SeedFlags(
                FlagIds.HasTranslator,
                FlagIds.FoundDafu,
                FlagIds.UnlockedHuaiAn,
                FlagIds.InvestigateTutorialShown);
        }

        void SeedThroughGuardTalk(bool setIntroDone = true)
        {
            SeedThroughInvestigationMap();
            GameState.Instance.GrantIntel(IntelIds.FixedFeedingPoint, "固定投喂点在快递柜附近。");
            GameState.Instance.GrantIntel(IntelIds.DafuRestSpot, "大福有固定休息点。");
            SeedFlags(FlagIds.GuardUnlocked);
            if (setIntroDone)
                SeedFlags(FlagIds.GuardIntroDone);
        }

        void SeedThroughDafuInterview()
        {
            SeedThroughGuardTalk();
            GameState.Instance.GrantIntel(IntelIds.DafuAppearTime, "大福通常在下午四五点出现。");
            GameState.Instance.GrantIntel(IntelIds.DafuNearGuard, "大福常在保安亭附近晃。");
            GameState.Instance.GrantIntel(IntelIds.DafuBecameGuardCat, "大福像是社区的「保安猫」。");
            GameState.Instance.SetFlag(FlagIds.WaitingForDafu);
        }

        void SeedThroughLinCafe()
        {
            SeedThroughDafuInterview();
            SeedFlags(FlagIds.DafuInterviewDone, FlagIds.LinUnlocked);
            GameState.Instance.GrantIntel(IntelIds.WomanClue, "大福记得一名多次投喂并参与带走的女性。");
            GameState.Instance.GrantIntel(IntelIds.LinIdentity, "林女士曾参与救助大福。");
            GameState.Instance.SetObjective("前往咖啡馆见林女士。");
        }

        void SeedThroughLinInterview()
        {
            SeedThroughLinCafe();
            SeedFlags(FlagIds.LinCafeIntroDone);
        }

        void SeedThroughWriting(bool deskReady = true)
        {
            SeedThroughLinInterview();
            SeedFlags(FlagIds.LinInterviewDone, FlagIds.WritingUnlocked);
            if (deskReady)
                SeedFlags(FlagIds.WritingDeskReady);

            // Representative intel so the corkboard is not empty.
            GrantWritingSampleIntel();
            GameState.Instance.SetObjective("整理素材，完成报道。");
        }

        void SeedThroughEpilogue()
        {
            SeedThroughWriting();
            SeedFlags(FlagIds.ArticlePublished);
            var data = GameState.Instance.Data;
            data.writingDirection = (int)WritingDirection.GuardCatToday;
            data.lastReviewScore = 82;
            data.lastArticleTitle = "大福今天也在上班";
            data.lastArticleBody = "（调试跳转示例正文）";
            if (!data.selectedMaterials.Contains(MaterialIds.M01))
                data.selectedMaterials.Add(MaterialIds.M01);
            if (!data.selectedMaterials.Contains(MaterialIds.M14))
                data.selectedMaterials.Add(MaterialIds.M14);
        }

        static void GrantWritingSampleIntel()
        {
            var gs = GameState.Instance;
            void G(string intel, string note = null) => gs.GrantIntel(intel, note);

            G(IntelIds.FixedFeedingPoint);
            G(IntelIds.DafuRestSpot);
            G(IntelIds.DafuAppearTime);
            G(IntelIds.CommunityCare);
            G(IntelIds.DafuNoOwner);
            G(IntelIds.PastAfraid);
            G(IntelIds.NeckPain);
            G(IntelIds.NeckObject);
            G(IntelIds.Sleep);
            G(IntelIds.ObjectGone);
            G(IntelIds.RopeEmbedded);
            G(IntelIds.FeedFourDays);
            G(IntelIds.CaptureSuccess);
            G(IntelIds.TakenAway);
            G(IntelIds.PanleukopeniaDay3);
            G(IntelIds.TotalCost);
            G(IntelIds.LinHesitated);
            G(IntelIds.FourCatsHome);
            G(IntelIds.CannotFifth);
            G(IntelIds.ReturnOriginalArea);
            G(IntelIds.ReturnedDafu);
            G(IntelIds.TabbyPartner);
            G(IntelIds.CauseUnknown);
        }
    }
}
#endif
