#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    [InitializeOnLoad]
    public static class FlowWaterPfuenteOcclusionValidation
    {
        const string Key = "FlowWater.PfuenteOcclusionValidation.";
        [Serializable] sealed class Pref { public string key, text; public bool existed, isString; public int value; }
        [Serializable] sealed class Prefs { public List<Pref> values = new List<Pref>(); }
        [Serializable] sealed class SavedScene { public string path; public bool loaded, active; }
        [Serializable] sealed class Scenes { public SavedScene[] values; }
        [Serializable] sealed class Check { public string name, detail; public bool passed; }
        [Serializable] sealed class Report
        {
            public string unityVersion, startedUtc, finishedUtc;
            public bool passed, preferencesRestored;
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
        }
        static Report report;
        static IEnumerator routine;
        static bool busy, locked;
        static int nextFrame;
        static double deadline;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "PfuenteOcclusion"); } }
        static FlowWaterPfuenteOcclusionValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Unlock();
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + "active", false)) return;
                float restoredScale = SessionState.GetFloat(Key + "scale", Time.timeScale);
                Restore(); SessionState.EraseBool(Key + "active");
                int exit = SessionState.GetInt(Key + "exit", -1); SessionState.EraseInt(Key + "exit");
                Time.timeScale = restoredScale;
                if (Application.isBatchMode && exit >= 0)
                    EditorApplication.delayCall += () => { Time.timeScale = restoredScale; EditorApplication.Exit(exit); };
            };
            Application.logMessageReceived += (text, stack, type) =>
            {
                if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(text + "\n" + stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Original Upward Fountain Occlusion")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start upward fountain validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.1901", "Recovery.Completed.gm1901", "Recovery.Complete.518", "Recovery.Completed.gm518", "Recovery.Complete.549", "Recovery.Completed.gm549", "Recovery.Complete.1058", "Recovery.Completed.gm1058", "Recovery.Complete.2001", "Recovery.Completed.gm2001", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
                prefs.values.Add(new Pref { key = key, existed = PlayerPrefs.HasKey(key), isString = key == "language", value = PlayerPrefs.GetInt(key), text = PlayerPrefs.GetString(key) });
            SessionState.SetString(Key + "prefs", JsonUtility.ToJson(prefs));
            SessionState.SetString(Key + "scenes", JsonUtility.ToJson(new Scenes { values = scenes.Select(s => new SavedScene { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray() }));
            SessionState.SetFloat(Key + "scale", Time.timeScale); SessionState.SetBool(Key + "active", true);
            RecoveryCatalogBuilder.Build(); EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (busy || !SessionState.GetBool(Key + "active", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || Time.frameCount < nextFrame) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked = true;
                    report = new Report { unityVersion = Application.unityVersion, startedUtc = DateTime.UtcNow.ToString("o") };
                    Time.timeScale = 4; deadline = EditorApplication.timeSinceStartup + 180; routine = Exercise();
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Upward fountain validation exceeded180 seconds.");
                if (!routine.MoveNext()) { Finish(); return; }
                nextFrame = Time.frameCount + 1;
            }
            catch (Exception ex) { report.errors.Add(ex.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            Require("unity-2022.3.28f1",Application.unityVersion=="2022.3.28f1",Application.unityVersion);
            while(FlowWaterRecoveryUI.Instance==null)yield return null;
            FlowWaterRecoveryUI.Instance.StartGame(1901);
            while(RecoveredFlowGame.Instance==null || RecoveredFlowGame.Instance.CurrentLevelId!=1901 || RecoveredFlowGame.Instance.PieceRoot==null)yield return null;
            var game=RecoveredFlowGame.Instance;foreach(object step in DismissTutorial())yield return step;
            game.Solve();var water=RecoveredWaterNetwork.Instance;water.Rebuild();
            float until=Time.time+180;while(water.HasPendingPropagation && Time.time<until)yield return null;
            Require("actual1901-original-all-targets",water.RequiredFountainCount==8 && water.ActiveFountainCount==8,"actual saved original preview8/8 before isolated reversible occlusion perturbation");
            RecoveryPiece source=game.Pieces.First(p=>p.SourceRecordIndex==7),target=game.Pieces.First(p=>p.SourceRecordIndex==9);
            Require("actual1901-original-upward-pair",water.IsWet(source)&&water.IsWet(target),"original PiedraFuente5_3 source7 to PiedraFuente5_5 receiver9");
            Require("actual1901-unobstructed-native-font-visible",FontVisible(source,water.GetWaterColor(source)),"original CascadaTest1 visual exists while unblocked");
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            object snapshot=typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot",flags).Invoke(game,null);
            Transform originalRoot=game.PieceRoot;
            try
            {
                RecoveryPiece blocker=null;SphereCollider solid=null;
                foreach(RecoveryPiece piece in game.Pieces)
                {
                    if(piece==source || piece==target || piece.BasePrefab==null || !piece.BasePrefab.Contains("BloqueFijo") || piece.OriginalCaptureColliders==null)continue;
                    solid=piece.OriginalCaptureColliders.OfType<SphereCollider>().FirstOrDefault(c=>c!=null && c.gameObject.layer==9 && c.gameObject.activeInHierarchy);
                    if(solid!=null){blocker=piece;break;}
                }
                Require("actual1901-original-layer9-blocker-available",blocker!=null,"actual original recorded layer9 solid, not an invented BoxCollider");
                Vector3 originalPosition=blocker.transform.position;
                CascadaTest1 emitter=source.GetComponentsInChildren<CascadaTest1>(true).First(c=>c.gameObject.activeInHierarchy);
                Vector3 probe=emitter.transform.position+Vector3.up*emitter.distan_y;
                blocker.transform.position+=probe-solid.transform.TransformPoint(solid.center);Physics.SyncTransforms();water.Rebuild();yield return null;
                Require("actual-native-upward-probe-blocked",UpwardBlocked(water,source),"same original radius2/layer9 CheckSphere, actual collider "+blocker.SourceRecordIndex+":"+solid.name);
                Require("already-wet-independent-marker-still-supplies",water.IsWet(source)&&water.IsWet(target),"original persistent output marker survives visual Font destruction");
                Require("blocked-original-font-suppressed",!FontVisible(source,water.GetWaterColor(source)),"native FD3A58..64 destroys only the Font visual");
                FuenteCubeta2 intake=source.GetComponentsInChildren<FuenteCubeta2>(true).First(f=>f.gameObject.activeInHierarchy&&f.gameObject.name=="Origen3");
                FuenteCubeta2 upper=target.GetComponentsInChildren<FuenteCubeta2>(true).First(f=>f.gameObject.activeInHierarchy&&f.gameObject.name=="Origen6");
                Require("blocked-original-marker-remains-active",intake.esferaFuente1.activeInHierarchy,"native1098ACC enables independent AguaControlPFuente.esferaFuente; blocker does not turn it off");
                Require("blocked-original-marker-input-contact",OriginalContact(intake.esferaFuente1.GetComponent<SphereCollider>(),upper.GetComponent<SphereCollider>()),"actual original outputY4.21/r1 and upper bottom-centerY-3.2/r.5 remain in contact across7.6");
                water.SetPortEnabled(source,3,false);water.Rebuild();yield return null;
                Require("real-intake-close-removes-upward-arrival",!water.IsWet(source)&&!water.IsWet(target),"real missing input delivery still recursively invalidates already-arrived downstream water");
                water.SetPortEnabled(source,3,true);water.Rebuild();until=Time.time+60;
                while(water.HasPendingPropagation&&Time.time<until)yield return null;
                Require("fresh-intake-under-occlusion-still-supplies",water.IsWet(source)&&water.IsWet(target)&&UpwardBlocked(water,source),"native independent marker conducts after a genuine fresh inlet callback even while Font remains visually suppressed");
                Require("fresh-occluded-font-stays-suppressed",!FontVisible(source,water.GetWaterColor(source)),"conduction does not fabricate visible spray through the obstructing solid");
                blocker.transform.position=originalPosition;Physics.SyncTransforms();water.Rebuild();yield return null;
                Require("unblocking-restores-only-native-font",!UpwardBlocked(water,source)&&FontVisible(source,water.GetWaterColor(source))&&water.IsWet(target),"native recreate Font when clear; surviving contact keeps target wet");
            }
            finally {typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot",flags).Invoke(game,new[]{snapshot});Physics.SyncTransforms();water.Rebuild();}
            until=Time.time+60;while(water.HasPendingPropagation&&Time.time<until)yield return null;
            Require("production-snapshot-restores-original-root-and-water",game.PieceRoot!=originalRoot&&water.CurrentPieceRoot==game.PieceRoot&&water.ActiveFountainCount==8&&water.RequiredFountainCount==8,"production Snapshot restores original board, overrides, frontier and count after the explicit non-player component perturbation");
        }
        static bool UpwardBlocked(RecoveredWaterNetwork water,RecoveryPiece source)
        {
            var nodes=(System.Collections.IEnumerable)typeof(RecoveredWaterNetwork).GetField("nodes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(water);
            foreach(object node in nodes)if((RecoveryPiece)node.GetType().GetField("piece").GetValue(node)==source)return (bool)node.GetType().GetField("upwardBlocked").GetValue(node);
            return false;
        }
        static bool FontVisible(RecoveryPiece source,int color)
        {
            var emitter=source.GetComponentsInChildren<CascadaTest1>(true).FirstOrDefault(c=>c.gameObject.activeInHierarchy);if(emitter==null)return false;
            var prefab=(GameObject)typeof(CascadaTest1).GetField("prefabFuente"+Mathf.Clamp(color,1,4)).GetValue(emitter);
            return prefab!=null&&source.GetComponentsInChildren<Transform>(true).Any(t=>t.gameObject.activeInHierarchy&&t.name=="Original "+prefab.name);
        }
        static bool OriginalContact(SphereCollider a,SphereCollider b)
        {
            GameObject aa=null,bb=null;
            try
            {
                aa=UnityEngine.Object.Instantiate(a.gameObject);bb=UnityEngine.Object.Instantiate(b.gameObject);
                foreach(var pair in new[]{new{copy=aa,original=a},new{copy=bb,original=b}})
                {pair.copy.transform.position=pair.original.transform.position;pair.copy.transform.rotation=pair.original.transform.rotation;pair.copy.transform.localScale=pair.original.transform.lossyScale;pair.copy.layer=2;pair.copy.SetActive(true);
                 foreach(MonoBehaviour script in pair.copy.GetComponents<MonoBehaviour>())if(script!=null)script.enabled=false;foreach(Renderer r in pair.copy.GetComponents<Renderer>())r.enabled=false;pair.copy.GetComponent<SphereCollider>().enabled=true;}
                Physics.SyncTransforms();Vector3 direction;float distance;
                return Physics.ComputePenetration(aa.GetComponent<SphereCollider>(),aa.transform.position,aa.transform.rotation,bb.GetComponent<SphereCollider>(),bb.transform.position,bb.transform.rotation,out direction,out distance);
            }
            finally {if(aa!=null)UnityEngine.Object.Destroy(aa);if(bb!=null)UnityEngine.Object.Destroy(bb);}
        }
        static IEnumerable DismissTutorial()
        {
            HelpRotate help = FindHelp(); if (help == null || !help.gameObject.activeInHierarchy) yield break;
            SwipeHandler2 swipe = RecoveredNativeCamera.CurrentSwipe;
            if (swipe == null) throw new InvalidOperationException("Visible native tutorial has no actual swipe controller.");
            float yaw = swipe.EsferaCamera.eulerAngles.y; swipe.GirarDerecha();
            while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw + 90)) > .25f) yield return null;
            swipe.GirarIzquierda(); while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw)) > .25f) yield return null;
            float until = Time.time + 1; while (Time.time < until) yield return null;
        }
        static string MotionSignature(Transform root)
        {
            var values = new System.Text.StringBuilder();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                values.Append(child.localPosition.ToString("F4")).Append(child.localRotation.ToString("F4")).Append(child.localScale.ToString("F4"));
            foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var velocity = particles.velocityOverLifetime;
                values.Append(velocity.x.constant.ToString("F4")).Append(velocity.y.constant.ToString("F4")).Append(velocity.z.constant.ToString("F4"));
            }
            return values.ToString();
        }
        static HelpRotate FindHelp()
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            { HelpRotate result = root.GetComponentInChildren<HelpRotate>(true); if (result != null) return result; }
            return null;
        }
        static void Require(string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (!passed) throw new InvalidOperationException(name + ": " + detail);
        }
        static void Finish()
        {
            report.finishedUtc = DateTime.UtcNow.ToString("o");
            report.passed = report.errors.Count == 0 && report.checks.All(c => c.passed);
            RestorePreferences(); report.preferencesRestored = true;
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "pfuente-occlusion-validation.json"), JsonUtility.ToJson(report, true));
            SessionState.SetInt(Key + "exit", report.passed ? 0 : 2); Unlock(); EditorApplication.ExitPlaymode();
        }
        static void RestorePreferences()
        {
            Prefs prefs = JsonUtility.FromJson<Prefs>(SessionState.GetString(Key + "prefs", ""));
            if (prefs == null) return;
            foreach (Pref p in prefs.values)
                if (!p.existed) PlayerPrefs.DeleteKey(p.key); else if (p.isString) PlayerPrefs.SetString(p.key, p.text); else PlayerPrefs.SetInt(p.key, p.value);
            PlayerPrefs.Save(); Time.timeScale = SessionState.GetFloat(Key + "scale", 1);
        }
        static void Restore()
        {
            RestorePreferences();
            Scenes scenes = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + "scenes", ""));
            if (scenes != null && scenes.values != null && scenes.values.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(scenes.values.Select(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
        }
        static void Unlock()
        {
            if (!locked) return; locked = false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh();
        }
    }
}
#endif
