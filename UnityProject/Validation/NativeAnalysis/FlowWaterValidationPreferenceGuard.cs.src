#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    // Extra native tutorial state is shared by all play-mode validation suites.
    // Existing suites retain their own progress/audio/scene backups.
    [InitializeOnLoad]
    public static class FlowWaterValidationPreferenceGuard
    {
        const string Active = "FlowWater.Validation.HelpPreferenceGuard.Active";
        const string Exists = "FlowWater.Validation.HelpPreferenceGuard.Exists";
        const string Value = "FlowWater.Validation.HelpPreferenceGuard.Value";
        static FlowWaterValidationPreferenceGuard()
        {
            if (!Application.isBatchMode) return;
            string[] args = Environment.GetCommandLineArgs();
            bool validation = false;
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-executeMethod" && args[i + 1].StartsWith("Recovered.FlowWater.Editor.", StringComparison.Ordinal))
                    validation = true;
            if (!validation) return;
            if (!SessionState.GetBool(Active, false))
            {
                SessionState.SetBool(Exists, PlayerPrefs.HasKey("helpRotation"));
                SessionState.SetInt(Value, PlayerPrefs.GetInt("helpRotation", 0));
                SessionState.SetBool(Active, true);
            }
            EditorApplication.quitting += Restore;
        }
        static void Restore()
        {
            if (!SessionState.GetBool(Active, false)) return;
            bool existed = SessionState.GetBool(Exists, false);
            int value = SessionState.GetInt(Value, 0);
            if (existed) PlayerPrefs.SetInt("helpRotation", value);
            else PlayerPrefs.DeleteKey("helpRotation");
            PlayerPrefs.Save();
            bool restored = PlayerPrefs.HasKey("helpRotation") == existed &&
                (!existed || PlayerPrefs.GetInt("helpRotation", -1) == value);
            string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "rotation-help-preference-restoration.json"),
                JsonUtility.ToJson(new Result { finishedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
                    productName = Application.productName, existedBefore = existed, valueBefore = value, restored = restored }, true));
            SessionState.EraseBool(Active);
        }
        [Serializable] sealed class Result
        {
            public string finishedUtc, unityVersion, productName;
            public bool existedBefore, restored;
            public int valueBefore;
        }
    }
}
#endif
