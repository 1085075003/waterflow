#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterPortraitAndroidBuild
    {
        public static void Build()
        {
            string runtime = Path.Combine(EditorApplication.applicationContentsPath,
                "PlaybackEngines/AndroidPlayer/Variations/il2cpp/Release/Libs/arm64-v8a/libunity.so");
            string runtimeData = Encoding.ASCII.GetString(File.ReadAllBytes(runtime));
            if (!runtimeData.Contains(Application.unityVersion + "\0") ||
                runtimeData.Contains(Application.unityVersion + "c1\0"))
                throw new InvalidOperationException("Android runtime does not match editor " + Application.unityVersion);
            string root = Directory.GetParent(Application.dataPath).Parent.FullName;
            string tools = Path.Combine(root, "Builds/Android/Toolchain");
            AndroidExternalToolsSettings.sdkRootPath = "D:/AndroidSdk";
            AndroidExternalToolsSettings.jdkRootPath = Path.Combine(tools, "jdk");
            AndroidExternalToolsSettings.ndkRootPath = Path.Combine(tools, "android-ndk-r23b");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.frasinapp.flowwater.portrait");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            PlayerSettings.Android.bundleVersionCode = 1;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(root, "Builds/Android/FlowWater-Portrait.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            File.WriteAllText(Path.Combine(root, "Builds/Android/build-result.txt"),
                report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nSize: " + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Android build failed: " + report.summary.result);
        }
    }
}
#endif

