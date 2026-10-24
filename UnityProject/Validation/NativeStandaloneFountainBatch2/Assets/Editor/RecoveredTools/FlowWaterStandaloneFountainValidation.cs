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
    public static class FlowWaterStandaloneFountainValidation
    {
        const string Key = "FlowWater.StandaloneFountainValidation.";
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
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "StandaloneFountain"); } }
        static FlowWaterStandaloneFountainValidation()
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
        [MenuItem("Tools/Flow Water/Validate Original Standalone Fountain Marker")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start standalone fountain validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.514", "Recovery.Completed.gm514", "Recovery.Complete.519", "Recovery.Completed.gm519", "Recovery.Complete.518", "Recovery.Completed.gm518", "Recovery.Complete.549", "Recovery.Completed.gm549", "Recovery.Complete.1058", "Recovery.Completed.gm1058", "Recovery.Complete.2001", "Recovery.Completed.gm2001", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
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
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Standalone fountain validation exceeded180 seconds.");
                if (!routine.MoveNext()) { Finish(); return; }
                nextFrame = Time.frameCount + 1;
            }
            catch (Exception ex) { report.errors.Add(ex.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            Require("unity-2022.3.28f1",Application.unityVersion=="2022.3.28f1",Application.unityVersion);
            foreach(string name in new[]{"Azul","Verde","Z","Ro"})
            {
                CascadaTest3 authored=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PiedraFuente7"+name+".prefab").GetComponentInChildren<CascadaTest3>(true);
                Require("original-"+name+"-standalone-marker-authored-active",authored.esferaSalida.activeSelf,"original prefab activeSelf is true before any Font; Start only changes the tag");
            }
            while(FlowWaterRecoveryUI.Instance==null)yield return null;
            FlowWaterRecoveryUI.Instance.StartGame(514);
            while(RecoveredFlowGame.Instance==null || RecoveredFlowGame.Instance.CurrentLevelId!=514 || RecoveredFlowGame.Instance.PieceRoot==null)yield return null;
            var game=RecoveredFlowGame.Instance;foreach(object step in DismissTutorial())yield return step;
            game.Solve();var water=RecoveredWaterNetwork.Instance;water.Rebuild();
            float until=Time.time+180;while(water.HasPendingPropagation && Time.time<until)yield return null;
            Require("actual514-original-all-targets",water.RequiredFountainCount==5 && water.ActiveFountainCount==5,"actual saved original preview5/5 before isolated reversible occlusion perturbation");
            RecoveryPiece source=game.Pieces.First(p=>p.SourceRecordIndex==10),target=game.Pieces.First(p=>p.SourceRecordIndex==13);
            CascadaTest3 emitter=source.GetComponentsInChildren<CascadaTest3>(true).Single();
            Require("real-font-retains-authored-independent-marker",FontVisible(source,water.GetWaterColor(source)) && emitter.esferaSalida.activeInHierarchy && emitter.esferaSalida.tag=="color1","original marker is authored active; doCrearFuente FD51CC also explicitly re-enables it");
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var capture=typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot",flags);
            var restore=typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot",flags);
            object pristine=capture.Invoke(game,null);Transform originalRoot=game.PieceRoot;
            try
            {
                RecoveryPiece blocker=null;SphereCollider solid=null;
                foreach(RecoveryPiece piece in game.Pieces)
                {
                    if(piece==source || piece==target || piece.BasePrefab==null || !piece.BasePrefab.Contains("BloqueFijo") || piece.OriginalCaptureColliders==null)continue;
                    solid=piece.OriginalCaptureColliders.OfType<SphereCollider>().FirstOrDefault(c=>c!=null && c.gameObject.layer==9 && c.gameObject.activeInHierarchy);
                    if(solid!=null){blocker=piece;break;}
                }
                Require("actual514-original-layer9-blocker",blocker!=null,"actual original recorded collider used for explicit component lifecycle perturbation, not claimed legal player motion");
                Vector3 originalPosition=blocker.transform.position;
                Vector3 probe=emitter.transform.position+Vector3.up*emitter.distan_y;
                blocker.transform.position+=probe-solid.transform.TransformPoint(solid.center);Physics.SyncTransforms();water.Rebuild();yield return null;
                Require("born-marker-survives-native-visual-occlusion",UpwardBlocked(water,source)&&!FontVisible(source,1)&&emitter.esferaSalida.activeInHierarchy&&water.IsWet(target),"original Update destroys only fuente; the already-born independent sphere still delivers water");
                object bornBlocked=capture.Invoke(game,null);
                water.SetSourceEnabled(source,false);water.Rebuild();yield return null;
                Require("real-source-off-invalidates-arrived-branch",!water.IsWet(source)&&!water.IsWet(target),"persistent marker metadata does not keep an absent source arrival alive");
                // Explicitly perturb the authored active marker to false in
                // order to exercise doCrearFuente and Snapshot's inactive case.
                // This is NOT the default state of original PiedraFuente7.
                emitter.esferaSalida.SetActive(false);
                water.SetSourceEnabled(source,true);water.Rebuild();yield return null;
                Require("initial-obstruction-does-not-create-marker-or-delivery",UpwardBlocked(water,source)&&water.IsWet(source)&&!water.IsWet(target)&&!emitter.esferaSalida.activeSelf&&!FontVisible(source,1),"explicitly disabled original marker stays disabled until real doCrearFuente; this is a component perturbation, not the default original prefab");
                object unbornBlocked=capture.Invoke(game,null);
                blocker.transform.position=originalPosition;Physics.SyncTransforms();water.Rebuild();until=Time.time+60;
                while(water.HasPendingPropagation&&Time.time<until)yield return null;
                Require("first-clear-native-creation-starts-marker-and-water",!UpwardBlocked(water,source)&&FontVisible(source,1)&&emitter.esferaSalida.activeInHierarchy&&water.IsWet(target),"first unblocked doCrearFuente creates both original visual and independent marker");
                Transform beforeUnbornRestore=game.PieceRoot;game.LoadLevel(514);
                restore.Invoke(game,new[]{unbornBlocked});Physics.SyncTransforms();water.Rebuild();yield return null;
                source=game.Pieces.First(p=>p.SourceRecordIndex==10);target=game.Pieces.First(p=>p.SourceRecordIndex==13);emitter=source.GetComponentsInChildren<CascadaTest3>(true).Single();
                Require("snapshot-new-root-retains-unborn-blocked-marker",game.PieceRoot!=beforeUnbornRestore&&UpwardBlocked(water,source)&&!emitter.esferaSalida.activeSelf&&!water.IsWet(target),"production Undo reconstructs the actual pre-birth blocked marker state");
                Transform beforeBornRestore=game.PieceRoot;game.LoadLevel(514);
                restore.Invoke(game,new[]{bornBlocked});Physics.SyncTransforms();water.Rebuild();until=Time.time+60;
                while(water.HasPendingPropagation&&Time.time<until)yield return null;
                source=game.Pieces.First(p=>p.SourceRecordIndex==10);target=game.Pieces.First(p=>p.SourceRecordIndex==13);emitter=source.GetComponentsInChildren<CascadaTest3>(true).Single();
                Require("snapshot-new-root-retains-born-blocked-marker",game.PieceRoot!=beforeBornRestore&&UpwardBlocked(water,source)&&emitter.esferaSalida.activeInHierarchy&&water.IsWet(target)&&!FontVisible(source,1),"production Undo restores the born original sphere without fabricating the obstructed Font");
                water.SetSourceEnabled(source,false);water.Rebuild();yield return null;
                water.SetSourceEnabled(source,true);water.Rebuild();until=Time.time+60;
                while(water.HasPendingPropagation&&Time.time<until)yield return null;
                Require("fresh-delivery-reuses-born-marker-under-obstruction",water.IsWet(target)&&emitter.esferaSalida.activeInHierarchy&&!FontVisible(source,1),"existing native marker conducts a genuine fresh source arrival after visual-only suppression");
            }
            finally {restore.Invoke(game,new[]{pristine});Physics.SyncTransforms();water.Rebuild();}
            until=Time.time+60;while(water.HasPendingPropagation&&Time.time<until)yield return null;
            Require("production-snapshot-restores-pristine-five-targets",water.CurrentPieceRoot==game.PieceRoot&&water.ActiveFountainCount==5&&water.RequiredFountainCount==5,"original source, target, blocker poses, marker state and complete water restored after explicit fixture perturbation");
            game.LoadLevel(519);game.Solve();
            source=game.Pieces.First(p=>p.GetComponentInChildren<CascadaTest3>(true)!=null);emitter=source.GetComponentInChildren<CascadaTest3>(true);
            water.SetSourceEnabled(source,false);water.Rebuild();
            Require("actual519-native-start-tags-red-marker-before-font",emitter.color==4&&emitter.esferaSalida.activeSelf&&emitter.esferaSalida.tag=="color4"&&!FontVisible(source,4),"actual original red source's prefab marker starts as color1; native Start changes it before any first Font, even while source delivery is independently paused");
            water.SetSourceEnabled(source,true);water.Rebuild();until=Time.time+60;
            while(water.HasPendingPropagation&&Time.time<until)yield return null;
            Require("actual519-real-red-source-complete-flow",water.RequiredFountainCount>0&&water.ActiveFountainCount==water.RequiredFountainCount,"original final preview "+water.ActiveFountainCount+"/"+water.RequiredFountainCount+" after testing exact Start marker tag");
        }
        static bool UpwardBlocked(RecoveredWaterNetwork water,RecoveryPiece source)
        {
            var nodes=(System.Collections.IEnumerable)typeof(RecoveredWaterNetwork).GetField("nodes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(water);
            foreach(object node in nodes)if((RecoveryPiece)node.GetType().GetField("piece").GetValue(node)==source)return (bool)node.GetType().GetField("upwardBlocked").GetValue(node);
            return false;
        }
        static bool FontVisible(RecoveryPiece source,int color)
        {
            var emitter=source.GetComponentsInChildren<CascadaTest3>(true).FirstOrDefault(c=>c.gameObject.activeInHierarchy);if(emitter==null)return false;
            var prefab=(GameObject)typeof(CascadaTest3).GetField("prefabFuente"+Mathf.Clamp(color,1,4)).GetValue(emitter);
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
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "standalone-fountain-validation.json"), JsonUtility.ToJson(report, true));
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
