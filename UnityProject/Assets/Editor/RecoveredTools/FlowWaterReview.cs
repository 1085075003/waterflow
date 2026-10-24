#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterReview
    {
        [MenuItem("Tools/Flow Water/Open Verified Home")]
        public static void OpenHome()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            PlayerPrefs.SetInt("Recovery.LastLevelId", 1);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
    }
}
#endif
