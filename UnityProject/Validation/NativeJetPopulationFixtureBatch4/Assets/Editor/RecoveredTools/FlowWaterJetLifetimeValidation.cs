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
    public static class FlowWaterJetLifetimeValidation
    {
        const string Key = "FlowWater.JetLifetimeValidation.";
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
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "JetLifetime"); } }
        static FlowWaterJetLifetimeValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Unlock();
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + "active", false)) return;
                float restoredScale = SessionState.GetFloat(Key + "scale", Time.timeScale);
                Restore(); SessionState.EraseBool(Key + "active");
                int exit = SessionState.GetInt(Key + "exit", -1); SessionState.EraseInt(Key + "exit");
                // Restoring the original empty Editor scene resets TimeManager.
                Time.timeScale = restoredScale;
                if (Application.isBatchMode && exit >= 0)
                    EditorApplication.delayCall += () => { Time.timeScale = restoredScale; EditorApplication.Exit(exit); };
            };
            Application.logMessageReceived += (text, stack, type) =>
            {
                if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(text + "\n" + stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Original Jet Lifetime")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start camera validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.1656", "Recovery.Completed.gm1656", "Recovery.Complete.518", "Recovery.Completed.gm518", "Recovery.Complete.549", "Recovery.Completed.gm549", "Recovery.Complete.1058", "Recovery.Completed.gm1058", "Recovery.Complete.2001", "Recovery.Completed.gm2001", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
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
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Camera/tutorial validation exceeded180 seconds.");
                if (!routine.MoveNext()) { Finish(); return; }
                nextFrame = Time.frameCount + 1;
            }
            catch (Exception ex) { report.errors.Add(ex.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            Require("unity-2022.3.28f1", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
            while (FlowWaterRecoveryUI.Instance == null) yield return null;
            FlowWaterRecoveryUI.Instance.StartGame(1656);
            while (RecoveredFlowGame.Instance == null || RecoveredFlowGame.Instance.CurrentLevelId != 1656 || RecoveredFlowGame.Instance.PieceRoot == null) yield return null;
            RecoveredFlowGame game = RecoveredFlowGame.Instance;
            foreach (object step in DismissTutorial()) yield return step;
            game.Solve(); RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance; water.Rebuild();
            float until = Time.time + 180;
            while (water.HasPendingPropagation && Time.time < until) yield return null;
            Require("actual1656-full-colored-flow", !water.HasPendingPropagation && water.RequiredFountainCount > 0 && water.ActiveFountainCount >= water.RequiredFountainCount,
                water.ActiveFountainCount + "/" + water.RequiredFountainCount + " original preview targets; normal pointer replay is tested separately");
            Require("actual1656-native-cache-wrapper-active", game.PieceRoot.GetComponentsInChildren<RecoveredNativeJetHandle>(true).Any(h => h.Born && h.gameObject.activeInHierarchy),
                "actual production water arrival uses original cached routing objects");
            GameObject actor = null, originalWater = null;
            try
            {
                ControlPiezaC control; RecoveryPiece piece;
                actor = MakeActor(out control, out piece); actor.transform.rotation = Quaternion.Euler(0,90,0);
                originalWater = MakeWater(control); FuenteTest3 origin = OriginalOrigin(originalWater);
                var effects = new List<GameObject>();
                RecoveredWaterJets.Restore(origin, 1, RecoveredWaterJets.ReadRoutes(control), (prefab,parent) => Spawn(prefab,parent,effects), effects.Add);
                RecoveredNativeJetHandle handle = originalWater.GetComponentInChildren<RecoveredNativeJetHandle>(true);
                Require("isolated-original-route-wrapper-created", handle != null && !handle.Born, "original actual1696 pit4100, delayed FT3 callback has not fired");
                RecoveredWaterArrival.ActivateAfter(originalWater.transform, handle.gameObject, .2f);
                actor.transform.rotation = Quaternion.Euler(0,180,0);
                while (!handle.Born) yield return null;
                Require("first-birth-after-parent-turn-native-world-zero", Quaternion.Angle(handle.transform.rotation,Quaternion.identity)<.01f && Near(RecoveredWaterJets.ReadRoutes(control)[0].displacement,Vector3.left*20),
                    "native world Instantiate is sampled at the actual callback, after the parent turned during the native wait");
                int wrapperId = handle.GetInstanceID(), modelId = handle.Model.GetInstanceID();
                actor.transform.rotation = Quaternion.Euler(0,270,0); yield return null;
                Require("already-born-wrapper-follows-parent-turn", handle.GetInstanceID()==wrapperId && handle.Model.GetInstanceID()==modelId && Quaternion.Angle(handle.transform.rotation,Quaternion.Euler(0,90,0))<.01f && Near(RecoveredWaterJets.ReadRoutes(control)[0].displacement,Vector3.forward*20),
                    "same original obj13 and model now follow their original parent; no world-basis restart");
                ParticleSystem ps = handle.Model.GetComponentInChildren<ParticleSystem>(true);
                until=Time.time+.3f;while(Time.time<until)yield return null; uint seed=ps.randomSeed;float age=ps.time;
                RecoveredNativeJetCache.Park(originalWater.transform,false); UnityEngine.Object.Destroy(originalWater); originalWater=null;
                Require("visual-rebuild-keeps-wet-wrapper-active", handle!=null && handle.gameObject.activeInHierarchy && handle.Model.GetInstanceID()==modelId,"original cache is not disabled for an unrelated visual rebuild");
                originalWater=MakeWater(control);origin=OriginalOrigin(originalWater);effects.Clear();
                RecoveredWaterJets.Restore(origin,1,RecoveredWaterJets.ReadRoutes(control),(prefab,parent)=>Spawn(prefab,parent,effects),effects.Add);
                foreach(GameObject effect in effects)RecoveredWaterArrival.ActivateAfter(originalWater.transform,effect,0);
                yield return null;
                Require("visual-rebuild-keeps-native-identity-and-particle-phase",handle.GetInstanceID()==wrapperId && handle.Model.GetInstanceID()==modelId && ps.randomSeed==seed && ps.time>=age,
                    "same wet original wrapper/model and continuous original particle clock, without Rebind/Simulate reset");
                RecoveredNativeJetCache.Park(originalWater.transform,true);UnityEngine.Object.Destroy(originalWater);originalWater=null;
                Require("real-exit-native-wrapper-disabled-not-destroyed",handle!=null && !handle.gameObject.activeInHierarchy && handle.Model.GetInstanceID()==modelId,"native doExit SetActive(false) preserves obj13 and its existing model");
                actor.transform.rotation=Quaternion.identity;yield return null;
                originalWater=MakeWater(control);origin=OriginalOrigin(originalWater);effects.Clear();
                RecoveredWaterJets.Restore(origin,1,RecoveredWaterJets.ReadRoutes(control),(prefab,parent)=>Spawn(prefab,parent,effects),effects.Add);
                foreach(GameObject effect in effects)RecoveredWaterArrival.ActivateAfter(originalWater.transform,effect,0);
                Require("real-reentry-reuses-original-world-basis",handle.GetInstanceID()==wrapperId && handle.Model.GetInstanceID()==modelId && handle.gameObject.activeInHierarchy && Near(RecoveredWaterJets.ReadRoutes(control)[0].displacement,Vector3.right*20),
                    "same original cached object follows parent even while inactive, then SetActive(true) reuses it");
                until=Time.time+.3f;while(Time.time<until)yield return null;
                ParticleSystem[] beforeParticles=handle.Model.GetComponentsInChildren<ParticleSystem>(true);
                Require("snapshot-captures-aged-particle-phase",beforeParticles.Length>0 && beforeParticles.Any(p=>p.time>.1f),"snapshot follows actual emission, rather than a zero-time newly activated model");
                // A paused first system and the other live systems exercise both
                // saved play states without changing the production activation path.
                beforeParticles[0].Pause(false);
                uint[] savedSeeds=beforeParticles.Select(p=>p.randomSeed).ToArray();
                float[] savedTimes=beforeParticles.Select(p=>p.time).ToArray();
                int[] savedCounts=beforeParticles.Select(p=>p.particleCount).ToArray();
                Require("snapshot-captures-real-emitted-particles",savedCounts.Any(n=>n>0),"original emitted counts="+string.Join(",",savedCounts));
                bool[] savedPlaying=beforeParticles.Select(p=>p.isPlaying).ToArray();
                bool[] savedAutoSeed=beforeParticles.Select(p=>p.useAutoRandomSeed).ToArray();
                object saved=RecoveredNativeJetCache.Capture(new[]{piece});Quaternion savedBasis=handle.transform.rotation;
                RecoveredNativeJetCache.Park(originalWater.transform,true);UnityEngine.Object.Destroy(actor);actor=null;originalWater=null;yield return null;
                actor=MakeActor(out control,out piece);actor.transform.rotation=Quaternion.identity;
                RecoveredNativeJetCache.Restore(saved,new[]{piece});
                Require("snapshot-new-root-restores-native-route-before-spawn",Near(RecoveredWaterJets.ReadRoutes(control)[0].displacement,Vector3.right*20),"stable piece/controller/origin/slot snapshot restores born basis before graph routing");
                originalWater=MakeWater(control);origin=OriginalOrigin(originalWater);effects.Clear();
                RecoveredWaterJets.Restore(origin,1,RecoveredWaterJets.ReadRoutes(control),(prefab,parent)=>Spawn(prefab,parent,effects),effects.Add);
                foreach(GameObject effect in effects)RecoveredWaterArrival.ActivateAfter(originalWater.transform,effect,0);
                handle=originalWater.GetComponentInChildren<RecoveredNativeJetHandle>(true);
                Require("snapshot-new-root-restores-born-wrapper-basis",handle.Born && Quaternion.Angle(handle.transform.rotation,savedBasis)<.01f,"reconstructed Undo root retains prior birth frame, rather than treating its old wrapper as newly born world0");
                ParticleSystem[] afterParticles=handle.Model.GetComponentsInChildren<ParticleSystem>(true);
                Require("snapshot-restores-exact-particle-seeds-and-auto-flags",afterParticles.Length==savedSeeds.Length && afterParticles.Select((p,i)=>p.randomSeed==savedSeeds[i] && p.useAutoRandomSeed==savedAutoSeed[i]).All(v=>v),
                    "all original child systems retain saved seeds and authored auto-seed flags");
                Require("snapshot-restores-aged-clock-and-play-pause-state",afterParticles.Length==savedTimes.Length && afterParticles.Select((p,i)=>Mathf.Abs(p.time-savedTimes[i])<.06f && p.isPlaying==savedPlaying[i]).All(v=>v),
                    "all original child systems retain saved simulation time and playing/paused state; repeated same-frame activation does not restart the paused system");
                Require("snapshot-restores-actual-emitted-population",afterParticles.Length==savedCounts.Length && afterParticles.Select((p,i)=>savedCounts[i]==0 || p.particleCount>0).All(v=>v),
                    "actual original emitted counts before="+string.Join(",",savedCounts)+" after="+string.Join(",",afterParticles.Select(p=>p.particleCount).ToArray()));
            }
            finally { if(actor!=null)UnityEngine.Object.Destroy(actor); }
        }
        static GameObject MakeActor(out ControlPiezaC control,out RecoveryPiece piece)
        {
            GameObject root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BloqueChorro1.prefab"));
            root.name="Isolated original routing lifecycle component fixture";root.transform.position=new Vector3(10000,0,0);
            foreach(Collider c in root.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(MonoBehaviour b in root.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)b.enabled=false;
            piece=root.AddComponent<RecoveryPiece>();control=root.GetComponentInChildren<ControlPiezaC>(true);
            // Original actual1696 record29 immutable route fields, not a fabricated level.
            control.tipoPieza=9;control.configFuente=1;control.configFuenteB=0;control.configPit1="4100";control.configPit2=control.configPit3=control.configPit4="";control.configRes1=control.configRes2="";
            return root;
        }
        static GameObject MakeWater(ControlPiezaC control)
        {
            GameObject water=UnityEngine.Object.Instantiate(control.prefabAguaControl,control.transform,false);water.transform.localPosition=Vector3.zero;water.transform.localScale=Vector3.one*.1f;water.transform.rotation=control.transform.rotation;
            foreach(Collider c in water.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(MonoBehaviour b in water.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)b.enabled=false;
            return water;
        }
        static FuenteTest3 OriginalOrigin(GameObject water){return water.GetComponentsInChildren<FuenteTest3>(true).First(f=>f.gameObject.name=="Origen1");}
        static GameObject Spawn(GameObject prefab,Transform parent,List<GameObject> effects)
        {
            GameObject result=UnityEngine.Object.Instantiate(prefab,parent,false);result.SetActive(true);
            foreach(Collider c in result.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(MonoBehaviour b in result.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)b.enabled=false;
            effects.Add(result);return result;
        }
        static bool Near(Vector3 a,Vector3 b){return Vector3.Distance(a,b)<.01f;}
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
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "jet-lifetime-validation.json"), JsonUtility.ToJson(report, true));
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
