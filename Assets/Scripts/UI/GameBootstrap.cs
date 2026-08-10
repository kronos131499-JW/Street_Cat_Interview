using StreetCat.Core;
using StreetCat.Investigation;
using StreetCat.Interview;
using StreetCat.Loc;
using StreetCat.Narrative;
using StreetCat.Notebook;
using StreetCat.UI;
using UnityEngine;

namespace StreetCat
{
    /// <summary>
    /// Drop this on any scene (or use menu StreetCat/Bootstrap Scene Objects).
    /// Builds the Chapter 1 playable runtime.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (FindObjectOfType<GameBootstrap>() != null)
                return;
            var go = new GameObject("StreetCat_Bootstrap");
            go.AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            GameSettings.EnsureLoaded();
            GameState.Ensure();

            Ensure<SceneDirector>();
            Ensure<InvestigationService>();
            Ensure<ReporterNotebook>();
            Ensure<InterviewController>();
            Ensure<LlmClient>();
            Ensure<SceneViewSetup>();
            Ensure<DialogueHistory>();
            Ensure<BgmController>();
            Ensure<SfxController>();
            Ensure<GameUI>();
            Ensure<ChapterFlowController>();

            // Camera clear color for VN-like backdrop
            if (Camera.main != null)
                Camera.main.backgroundColor = new Color(0.05f, 0.07f, 0.1f);
        }

        T Ensure<T>() where T : Component
        {
            var c = FindObjectOfType<T>();
            if (c != null) return c;
            return gameObject.AddComponent<T>();
        }
    }
}
