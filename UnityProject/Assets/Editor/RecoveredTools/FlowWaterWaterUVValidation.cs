#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    [InitializeOnLoad]
    public static class FlowWaterWaterUVValidation
    {
        const string Key = "FlowWater.NativeWaterUVValidation";
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Sample
        {
            public string stage, prefab, renderer, material;
            public int frame, tipoOffset;
            public float time, timeScale;
            public Vector2 materialOffset, initial, speed;
            public Vector4 actualST, expectedST;
        }
        [Serializable] public sealed class Report
        {
            public string startedUtc, finishedUtc, unityVersion, runtimeSourceSha256;
            public bool passed, timeScaleRestored, preferencesRestored, enteredEditMode;
            public float playModeRestoredTimeScale, serializedTimeManagerScale;
            public float originalTimeScale, restoredTimeScale;
            public List<Check> checks = new List<Check>();
            public List<Sample> samples = new List<Sample>();
            public List<string> errors = new List<string>();
        }
        static Report report;
        static IEnumerator routine;
        static bool busy, locked, capturing;
        static double timeout;
        static GameObject fixture;
        static Camera camera;
        static RenderTexture texture;
        static scrollTest1[] originals;
        static string stage, prefabName;
        static int framesCaptured, lastFrame = -1;
        static float originalTimeScale;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Validation","WaterUV"); } }
        static FlowWaterWaterUVValidation()
        {
            EditorApplication.update += Update;
            Camera.onPostRender += PostRender;
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(Key,false)) { Cleanup(); Unlock(); }
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+".Finished",false)) {
                    float savedScale=SessionState.GetFloat(Key+".Scale",1);
                    // Mirror PropagationTimeRestoreBatch2: the play-mode exit
                    // reset runs before this callback. Apply again on delayCall.
                    Time.timeScale=savedScale;
                    EditorApplication.delayCall+=()=>CompleteInEditMode(savedScale);
                }
            };
            Application.logMessageReceived += (message, stack, type) => {
                if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(message+"\n"+stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Native Water UV")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start UV validation from Edit mode.");
            SessionState.SetFloat(Key+".Scale",Time.timeScale);
            SessionState.EraseBool(Key+".Finished");
            SessionState.SetString(Key+".Language",PlayerPrefs.GetString("language",""));
            SessionState.SetBool(Key+".LanguageExists",PlayerPrefs.HasKey("language"));
            var prefs=new FlowWaterUIValidation.Preferences();
            foreach(string name in new[] {"Recovery.LastLevelId","esTablet","OptionSelLev","Soundlevel","MusicaON","primeraVez","helpRotation"})
                prefs.entries.Add(new FlowWaterUIValidation.Preference {key=name,existed=PlayerPrefs.HasKey(name),integer=PlayerPrefs.GetInt(name)});
            foreach(string name in new[] {"Conf_music","Conf_sound"})
                prefs.entries.Add(new FlowWaterUIValidation.Preference {key=name,existed=PlayerPrefs.HasKey(name),floating=true,number=PlayerPrefs.GetFloat(name)});
            SessionState.SetString(Key+".Prefs",JsonUtility.ToJson(prefs));
            SessionState.SetBool(Key,true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if(busy || !SessionState.GetBool(Key,false) || SessionState.GetBool(Key+".Finished",false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            busy=true;
            try {
                if(report==null) {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked=true;
                    originalTimeScale=SessionState.GetFloat(Key+".Scale",Time.timeScale);
                    string path=Path.Combine(Application.dataPath,"Scripts/RecoveryRuntime/RecoveredWaterMotion.cs");
                    string sha;
                    using(var algorithm=SHA256.Create()) sha=BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
                    report=new Report { startedUtc=DateTime.UtcNow.ToString("o"),unityVersion=Application.unityVersion,runtimeSourceSha256=sha,originalTimeScale=originalTimeScale };
                    timeout=EditorApplication.timeSinceStartup+90; routine=Exercise();
                }
                if(EditorApplication.timeSinceStartup>timeout) throw new TimeoutException("Native UV fixture90second deadline");
                if(!routine.MoveNext()) Finish();
            } catch(Exception error) { if(report==null) report=new Report(); report.errors.Add(error.ToString()); Finish(); }
            finally { busy=false; }
        }
        static IEnumerator Exercise()
        {
            CheckState("unity-2022.3.28f1",Application.unityVersion=="2022.3.28f1",Application.unityVersion);
            Time.timeScale=1;
            GameObject view=new GameObject("Native UV validation capture camera");
            camera=view.AddComponent<Camera>(); camera.transform.position=new Vector3(10000,100,10000);
            camera.transform.rotation=Quaternion.Euler(90,0,0); texture=new RenderTexture(32,32,16); camera.targetTexture=texture;
            foreach(string name in new[] {"AguaControl5","AguaControl5B","Cascada1Z","Cascada1Ro"}) {
                prefabName=name;
                GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/"+name+".prefab");
                CheckState(name+":original-prefab",prefab!=null,"Original exported GameObject asset");
                if(prefab==null) continue;
                fixture=UnityEngine.Object.Instantiate(prefab,new Vector3(10000,0,10000),Quaternion.identity);
                foreach(MonoBehaviour behaviour in fixture.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled=false;
                originals=fixture.GetComponentsInChildren<scrollTest1>(true).Where(x=>x.rend!=null && x.rend.enabled && x.rend.sharedMaterial!=null).ToArray();
                CheckState(name+":actual-original-scroll-components",originals.Length>0,"Count="+originals.Length);
                var driver=fixture.AddComponent<RecoveredWaterMotion>(); driver.RestoreAuthoredMotion();
                stage="original-fields"; framesCaptured=0; lastFrame=-1; capturing=true;
                while(framesCaptured<3) yield return null;
                capturing=false;
                Time.timeScale=4;stage="scaled-time4";framesCaptured=0;lastFrame=-1;capturing=true;
                while(framesCaptured<3) yield return null;
                capturing=false;Time.timeScale=1;
                if(name=="AguaControl5" && originals.Length>=2) {
                    // Controlled Start branches on existing original components;
                    // shipped prefabs all serialize tipoOffset0.
                    originals[0].tipoOffset=1; originals[0].offsetInicialY=.73f;
                    originals[1].tipoOffset=2; originals[1].offsetInicialY=.19f;
                    driver.RestoreAuthoredMotion();
                    CheckState("controlled-tipoOffset1-native-Start",originals[0].offsetInicialY==0,"Native Start overwrites Y with0");
                    CheckState("controlled-tipoOffset2-native-Start",originals[1].offsetInicialY==.5f,"Native Start overwrites Y with.5");
                    stage="controlled-native-Start-branches";framesCaptured=0;lastFrame=-1;capturing=true;
                    while(framesCaptured<3) yield return null;
                    capturing=false;
                }
                UnityEngine.Object.Destroy(fixture); fixture=null; originals=null; yield return null;
            }
            CheckState("MatGr16-half-period-material-offset-ignored",report.samples.Any(x=>x.material=="MatGr16" && Mathf.Abs(x.materialOffset.y-.51f)<.0001f),"Actual original material starts atY.51; production UV follows native initial+speed*time");
            CheckState("MatGr16_2-half-period-material-offset-ignored",report.samples.Any(x=>x.material=="MatGr16_2" && Mathf.Abs(x.materialOffset.x-.5f)<.0001f),"Actual original material starts atX.5; production UV follows native initial+speed*time");
            CheckState("multiple-rendered-frames-observed",report.samples.Select(x=>x.frame).Distinct().Count()>=6,"Actual automatic camera post-render samples, no manual LateUpdate invocation");
            CheckState("native-scaled-Time.time-at-scales1-and4",report.samples.Any(x=>x.timeScale==1) && report.samples.Any(x=>x.timeScale==4),"Both scaled clocks exercised against actual production MPB");
        }
        static void PostRender(Camera rendered)
        {
            if(!capturing || rendered!=camera || originals==null || lastFrame==Time.frameCount) return;
            lastFrame=Time.frameCount;
            var block=new MaterialPropertyBlock();
            foreach(scrollTest1 source in originals) {
                Renderer renderer=source.rend; Material material=renderer.sharedMaterial;
                renderer.GetPropertyBlock(block);Vector4 actual=block.GetVector("_MainTex_ST");
                Vector2 initial=new Vector2(source.offsetInicialX,source.offsetInicialY), speed=new Vector2(source.scrollSpeed_X,source.scrollSpeed_Y);
                Vector2 offset=initial+speed*Time.time,scale=material.mainTextureScale;
                Vector4 expected=new Vector4(scale.x,scale.y,offset.x,offset.y);
                report.samples.Add(new Sample {stage=stage,prefab=prefabName,renderer=renderer.name,material=material.name,frame=Time.frameCount,time=Time.time,timeScale=Time.timeScale,
                    tipoOffset=source.tipoOffset,materialOffset=material.mainTextureOffset,initial=initial,speed=speed,actualST=actual,expectedST=expected});
                CheckState(prefabName+":"+stage+":"+renderer.name+":frame"+framesCaptured,(actual-expected).sqrMagnitude<.00000001f,"actual="+actual.ToString("F6")+",native="+expected.ToString("F6")+",Time.time="+Time.time);
            }
            framesCaptured++;
        }
        static void CheckState(string name,bool passed,string detail) { report.checks.Add(new Check {name=name,passed=passed,detail=detail}); }
        static void Cleanup()
        {
            capturing=false;Time.timeScale=originalTimeScale;
            if(fixture!=null) UnityEngine.Object.DestroyImmediate(fixture);
            if(camera!=null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
            if(texture!=null) {texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
            fixture=null;camera=null;texture=null;
        }
        static void Unlock() {if(!locked)return;EditorApplication.UnlockReloadAssemblies();AssetDatabase.AllowAutoRefresh();locked=false;}
        static void Finish()
        {
            Cleanup();report.restoredTimeScale=Time.timeScale;report.timeScaleRestored=Time.timeScale==originalTimeScale;
            var prefs=JsonUtility.FromJson<FlowWaterUIValidation.Preferences>(SessionState.GetString(Key+".Prefs","{}"));
            foreach(var pref in prefs.entries) {
                if(!pref.existed)PlayerPrefs.DeleteKey(pref.key);else if(pref.floating)PlayerPrefs.SetFloat(pref.key,pref.number);else PlayerPrefs.SetInt(pref.key,pref.integer);
            }
            if(SessionState.GetBool(Key+".LanguageExists",false))PlayerPrefs.SetString("language",SessionState.GetString(Key+".Language",""));else PlayerPrefs.DeleteKey("language");
            PlayerPrefs.Save();report.preferencesRestored=prefs.entries.All(x=>PlayerPrefs.HasKey(x.key)==x.existed && (!x.existed || (x.floating?PlayerPrefs.GetFloat(x.key)==x.number:PlayerPrefs.GetInt(x.key)==x.integer))) &&
                PlayerPrefs.HasKey("language")==SessionState.GetBool(Key+".LanguageExists",false) && (!PlayerPrefs.HasKey("language") || PlayerPrefs.GetString("language")==SessionState.GetString(Key+".Language",""));
            report.finishedUtc=DateTime.UtcNow.ToString("o");report.passed=report.errors.Count==0 && report.checks.Count>0 && report.checks.All(x=>x.passed) && report.timeScaleRestored && report.preferencesRestored;
            Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"native-water-uv-validation.json"),JsonUtility.ToJson(report,true));
            report.playModeRestoredTimeScale=Time.timeScale;
            // Keep the report pending until the real EnteredEditMode boundary.
            report.finishedUtc=""; SessionState.SetString(Key+".PendingReport",JsonUtility.ToJson(report));
            SessionState.SetBool(Key+".Finished",true);routine=null;
            Unlock();EditorApplication.ExitPlaymode();
        }
        static void CompleteInEditMode(float savedScale)
        {
            Time.timeScale=savedScale;
            Report result=JsonUtility.FromJson<Report>(SessionState.GetString(Key+".PendingReport","{}"));
            result.enteredEditMode=!EditorApplication.isPlaying;
            result.restoredTimeScale=Time.timeScale;
            result.timeScaleRestored=result.enteredEditMode && result.restoredTimeScale==result.originalTimeScale;
            UnityEngine.Object[] settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
            result.serializedTimeManagerScale=-1;
            if(settings.Length>0) {
                var serialized=new SerializedObject(settings[0]);serialized.Update();
                SerializedProperty scale=serialized.FindProperty("m_TimeScale");
                if(scale!=null)result.serializedTimeManagerScale=scale.floatValue;
            }
            result.checks.Add(new Check { name="time-scale-restored-after-EnteredEditMode",passed=result.timeScaleRestored && result.serializedTimeManagerScale==savedScale,
                detail="saved="+savedScale+",actual="+Time.timeScale+",serializedTimeManager="+result.serializedTimeManagerScale });
            result.passed=result.errors.Count==0 && result.checks.Count>0 && result.checks.All(x=>x.passed) && result.timeScaleRestored && result.preferencesRestored;
            result.finishedUtc=DateTime.UtcNow.ToString("o");Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output,"native-water-uv-validation.json"),JsonUtility.ToJson(result,true));
            SessionState.EraseBool(Key);SessionState.EraseBool(Key+".Finished");SessionState.EraseString(Key+".PendingReport");
            Time.timeScale=savedScale;
            if(Application.isBatchMode)EditorApplication.Exit(result.passed?0:1);
        }
    }
}
#endif
