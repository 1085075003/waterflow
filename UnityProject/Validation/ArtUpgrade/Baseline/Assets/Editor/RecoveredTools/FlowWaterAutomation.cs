#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater.Editor
{
    // Local file commands allow the existing Editor to perform validation without
    // opening another process against the same project. No network endpoint.
    [InitializeOnLoad]
    public static class FlowWaterAutomation
    {
        [Serializable] sealed class Request { public string id, action, argument; }
        [Serializable] sealed class Response { public string id, status, detail, scene; public bool playing, compiling; }
        static double nextPoll;
        static string DirectoryPath { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "automation"); } }
        static FlowWaterAutomation() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            Directory.CreateDirectory(DirectoryPath);
            Write("state.json", new Response { status = "ready", scene = SceneManager.GetActiveScene().path,
                playing = EditorApplication.isPlaying, compiling = EditorApplication.isCompiling });
            string path = Path.Combine(DirectoryPath, "request.json");
            if (!File.Exists(path) || EditorApplication.isCompiling) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); }
            catch { return; }
            if (request == null || string.IsNullOrEmpty(request.id) || request.id == SessionState.GetString("FlowWater.CommandId", "")) return;
            try
            {
                if (request.action == "stop") EditorApplication.isPlaying = false;
                else if (request.action == "open")
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
                    if (string.IsNullOrEmpty(request.argument) || !request.argument.StartsWith("Assets/", StringComparison.Ordinal) || !request.argument.EndsWith(".unity", StringComparison.Ordinal))
                        throw new ArgumentException("Only project scene asset paths are accepted.");
                    EditorSceneManager.OpenScene(request.argument, OpenSceneMode.Single);
                }
                else if (request.action == "play") EditorApplication.isPlaying = true;
                else if (request.action == "refresh") AssetDatabase.Refresh();
                else if (request.action == "close")
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("An edited scene is unsaved; close was withheld.");
                    SessionState.SetString("FlowWater.CommandId", request.id);
                    Write("response.json", new Response { id = request.id, status = "closing" });
                    File.Delete(path);
                    EditorApplication.Exit(0); return;
                }
                else throw new ArgumentException("Unknown local Editor command: " + request.action);
                SessionState.SetString("FlowWater.CommandId", request.id);
                Write("response.json", new Response { id = request.id, status = "ok", scene = SceneManager.GetActiveScene().path, playing = EditorApplication.isPlaying });
                File.Delete(path);
            }
            catch (Exception exception)
            {
                SessionState.SetString("FlowWater.CommandId", request.id);
                Write("response.json", new Response { id = request.id, status = "error", detail = exception.ToString() });
                File.Delete(path);
            }
        }

        static void Write(string name, Response response)
        {
            File.WriteAllText(Path.Combine(DirectoryPath, name), JsonUtility.ToJson(response, true));
        }
    }
}
#endif
