using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater
{
    /// <summary>Original level tutorial resets plus binding for retained source scene controllers.</summary>
    public static class RecoveredNativeCamera
    {
        static readonly int[] ResetTutorialLevels = { 1, 2, 7, 51, 52, 251, 401, 402, 403, 451, 452, 601, 602, 1051, 1651 };
        public static SwipeHandler2 CurrentSwipe { get; private set; }
        public static bool ForcesTutorial(int level)
        {
            foreach (int id in ResetTutorialLevels) if (id == level) return true;
            return false;
        }
        public static SwipeHandler2 ConfigureLevel(int level, GameObject tutorialRoot = null)
        {
            Scene scene = SceneManager.GetActiveScene();
            CurrentSwipe = null;
            SwipeHandler2.girado = false;
            if (ForcesTutorial(level)) PlayerPrefs.SetInt("helpRotation", 0);
            bool visible = PlayerPrefs.GetInt("helpRotation", 0) == 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (SwipeHandler2 swipe in root.GetComponentsInChildren<SwipeHandler2>(true))
                {
                    if (swipe.EsferaCamera == null) continue;
                    swipe.InitializeNativeRotation();
                    swipe.SynchronizeExternalRotation();
                    swipe.enabled = true;
                    if (CurrentSwipe == null) CurrentSwipe = swipe;
                }
                foreach (HelpRotate help in root.GetComponentsInChildren<HelpRotate>(true))
                {
                    if (tutorialRoot != null && help.gameObject != tutorialRoot && !help.transform.IsChildOf(tutorialRoot.transform)) continue;
                    help.enabled = true;
                    help.ResetTutorial();
                    help.gameObject.SetActive(visible);
                }
            }
            if (tutorialRoot != null) tutorialRoot.SetActive(visible);
            return CurrentSwipe;
        }
        // Call after desktop right-button orbit changes the original sphere.
        // Its absolute target follows that pose so the next native swipe cannot spring back.
        public static void SynchronizeExternalOrbit(bool markRotated = true)
        {
            if (CurrentSwipe != null) CurrentSwipe.SynchronizeExternalRotation(markRotated);
        }
    }
}
