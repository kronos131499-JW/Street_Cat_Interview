using StreetCat.Core;
using StreetCat.Data;
using UnityEditor;
using UnityEngine;

namespace StreetCat.Editor
{
    /// <summary>Play Mode test jumps via 街角专访 → 测试跳转.</summary>
    public static class StreetCatDebugJump
    {
        const string MenuRoot = "街角专访/测试跳转/";

        [MenuItem(MenuRoot + "标题画面", false, 100)]
        static void JumpTitle() => Jump("title");

        [MenuItem(MenuRoot + "SC-01 周五下班前", false, 110)]
        static void JumpSc01() => Jump(SceneIds.SC01);

        [MenuItem(MenuRoot + "SC-02 喵语翻译器", false, 111)]
        static void JumpSc02() => Jump(SceneIds.SC02);

        [MenuItem(MenuRoot + "SC-03 保安猫大福", false, 112)]
        static void JumpSc03() => Jump(SceneIds.SC03);

        [MenuItem(MenuRoot + "SC-04 槐安社区（剧本）", false, 113)]
        static void JumpSc04() => Jump(SceneIds.SC04);

        [MenuItem(MenuRoot + "调查地图（槐安社区）", false, 114)]
        static void JumpInvestigate() => Jump("investigate");

        [MenuItem(MenuRoot + "SC-05 保安亭（剧本）", false, 120)]
        static void JumpSc05() => Jump(SceneIds.SC05);

        [MenuItem(MenuRoot + "保安交谈菜单", false, 121)]
        static void JumpTalk() => Jump("talk");

        [MenuItem(MenuRoot + "SC-06 上班的大福", false, 122)]
        static void JumpSc06() => Jump(SceneIds.SC06);

        [MenuItem(MenuRoot + "采访大福（SC-07）", false, 130)]
        static void JumpInterviewDafu() => Jump(SceneIds.SC07);

        [MenuItem(MenuRoot + "SC-08 寻找林女士", false, 131)]
        static void JumpSc08() => Jump(SceneIds.SC08);

        [MenuItem(MenuRoot + "SC-09 咖啡馆（见面剧本）", false, 132)]
        static void JumpSc09() => Jump(SceneIds.SC09);

        [MenuItem(MenuRoot + "采访林女士", false, 133)]
        static void JumpInterviewLin() => Jump("interview_lin");

        [MenuItem(MenuRoot + "SC-10 写稿开场（剧本）", false, 140)]
        static void JumpSc10() => Jump(SceneIds.SC10);

        [MenuItem(MenuRoot + "写稿桌 / 素材板", false, 141)]
        static void JumpWriting() => Jump("writing");

        [MenuItem(MenuRoot + "记者笔记", false, 150)]
        static void JumpNotebook() => Jump("notebook");

        [MenuItem(MenuRoot + "后日谈（SC-11）", false, 160)]
        static void JumpEpilogue() => Jump("epilogue");

        [MenuItem(MenuRoot + "标题画面", true)]
        [MenuItem(MenuRoot + "SC-01 周五下班前", true)]
        [MenuItem(MenuRoot + "SC-02 喵语翻译器", true)]
        [MenuItem(MenuRoot + "SC-03 保安猫大福", true)]
        [MenuItem(MenuRoot + "SC-04 槐安社区（剧本）", true)]
        [MenuItem(MenuRoot + "调查地图（槐安社区）", true)]
        [MenuItem(MenuRoot + "SC-05 保安亭（剧本）", true)]
        [MenuItem(MenuRoot + "保安交谈菜单", true)]
        [MenuItem(MenuRoot + "SC-06 上班的大福", true)]
        [MenuItem(MenuRoot + "采访大福（SC-07）", true)]
        [MenuItem(MenuRoot + "SC-08 寻找林女士", true)]
        [MenuItem(MenuRoot + "SC-09 咖啡馆（见面剧本）", true)]
        [MenuItem(MenuRoot + "采访林女士", true)]
        [MenuItem(MenuRoot + "SC-10 写稿开场（剧本）", true)]
        [MenuItem(MenuRoot + "写稿桌 / 素材板", true)]
        [MenuItem(MenuRoot + "记者笔记", true)]
        [MenuItem(MenuRoot + "后日谈（SC-11）", true)]
        static bool ValidateJump() => Application.isPlaying && ChapterFlowController.Instance != null;

        static void Jump(string targetId)
        {
            if (!Application.isPlaying || ChapterFlowController.Instance == null)
            {
                EditorUtility.DisplayDialog(
                    "测试跳转",
                    "请先进入 Play Mode，并确保 SampleScene 已加载（有 ChapterFlowController）。",
                    "OK");
                return;
            }

            ChapterFlowController.Instance.DebugJumpTo(targetId);
        }
    }
}
