#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Reflows the retained gameplay HUD without rebuilding its hierarchy or events.</summary>
    public static class FlowWaterPortraitGameplay
    {
        static readonly string[] Scenes={"Assets/Scenes/Levely5z/Levely5.unity","Assets/Scenes/Levely5_basic.unity","Assets/Scenes/Levely5_6x6.unity","Assets/Scenes/Levely5_7x7.unity"};
        [Serializable] public sealed class SceneResult { public string scene, protectedBefore, protectedAfter; public bool passed; public int rects, textLayouts; public List<string> animatedRectBindings=new List<string>(); }
        [Serializable] public sealed class Report { public string generatedUtc; public bool passed=true; public string reference="720x1280, width matched; header 144, tools and hints 240"; public List<SceneResult> scenes=new List<SceneResult>(); }
        static SceneResult current;
        static string Project { get { return Directory.GetParent(Application.dataPath).FullName; } }

        [MenuItem("Tools/Flow Water/Portrait/Apply Gameplay Layout")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply portrait layout in Edit mode.");
            var setup=EditorSceneManager.GetSceneManagerSetup(); var report=new Report { generatedUtc=DateTime.UtcNow.ToString("o") };
            try
            {
                foreach(string path in Scenes)
                {
                    Backup(path); Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    current=new SceneResult { scene=path, protectedBefore=ProtectedFingerprint(scene) };
                    Reflow(scene); current.protectedAfter=ProtectedFingerprint(scene); current.passed=current.protectedBefore==current.protectedAfter;
                    if(!current.passed) throw new InvalidOperationException("Gameplay hierarchy, events or protected serialized properties changed: "+path);
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); report.scenes.Add(current);
                }
                AssetDatabase.SaveAssets(); Write("gameplay-layout-apply.json",report);
            }
            finally { current=null; EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        public static void Audit()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup(); var report=new Report { generatedUtc=DateTime.UtcNow.ToString("o") };
            try
            {
                foreach(string path in Scenes)
                {
                    Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    var result=new SceneResult { scene=path,protectedAfter=ProtectedFingerprint(scene),passed=true };
                    foreach(Animator animator in Components<Animator>(scene))
                    {
                        if(animator.runtimeAnimatorController==null) continue;
                        foreach(AnimationClip clip in animator.runtimeAnimatorController.animationClips.Distinct())
                            foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                                if(binding.type==typeof(RectTransform) && (binding.propertyName.Contains("m_AnchoredPosition")||binding.propertyName.Contains("m_SizeDelta")||binding.propertyName.Contains("m_Anchor")))
                                    result.animatedRectBindings.Add(PathOf(animator.transform)+"/"+binding.path+":"+binding.propertyName);
                    }
                    Canvas canvas=Components<Canvas>(scene).FirstOrDefault(c=>c.name=="Canvas"&&c.transform.parent==null);
                    CanvasScaler scaler=canvas==null?null:canvas.GetComponent<CanvasScaler>();
                    result.passed=scaler!=null && scaler.referenceResolution==new Vector2(720,1280) && Mathf.Approximately(scaler.matchWidthOrHeight,0);
                    report.passed&=result.passed; report.scenes.Add(result);
                }
                Write("gameplay-layout-audit.json",report);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        static void Reflow(Scene scene)
        {
            Canvas canvas=Components<Canvas>(scene).First(c=>c.name=="Canvas"&&c.transform.parent==null);
            Camera camera=Components<Camera>(scene).First(c=>c.CompareTag("MainCamera"));
            canvas.planeDistance=camera.nearClipPlane+30;
            CanvasScaler scaler=canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(720,1280);
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight=0;
            EditorUtility.SetDirty(scaler);
            Transform ui=canvas.transform;
            Stretch(Find(ui,"PanelW1b"));
            Fixed(Find(ui,"PanelW1b/panel1"),new Vector2(.5f,1),new Vector2(0,-80),new Vector2(656,96));
            Fixed(Find(ui,"PanelW1b/panel1/panel1a"),new Vector2(0,.5f),new Vector2(44,0),new Vector2(80,80));
            Fixed(Find(ui,"PanelW1b/panel1/panel1b"),new Vector2(.5f,.5f),new Vector2(8,0),new Vector2(360,76));
            Stretch(Find(ui,"PanelW1b/panel1/panel1a/MenuButton"));
            RectTransform level=Find(ui,"PanelW1b/panel1/panel1b/Level"); Stretch(level);
            foreach(RectTransform rect in level.GetComponentsInChildren<RectTransform>(true)) if(rect!=level) Stretch(rect);
            foreach(Text text in level.GetComponentsInChildren<Text>(true)) TextLayout(text,32,24,TextAnchor.MiddleCenter);
            Fixed(Find(ui,"PanelW1b/panel1/panel1a/MenuButton/icon2"),new Vector2(.5f,.5f),Vector2.zero,new Vector2(34,34));

            Stretch(Find(ui,"PanelW2b"));
            RectTransform bar=Find(ui,"PanelW2b/panel2"); Fixed(bar,new Vector2(.5f,0),new Vector2(0,96),new Vector2(620,116));
            Stretch(Find(ui,"PanelW2b/panel2/cuadro"));
            string[] groups={"panel2a","panel2b","panel2c","panel2d"};
            for(int i=0;i<groups.Length;i++) Fixed(Find(bar,groups[i]),new Vector2((i+.5f)/4,.5f),Vector2.zero,new Vector2(92,92));
            foreach(SaveLoadManager loader in Components<SaveLoadManager>(scene))
            {
                Stretch(Rect(loader.MenuButton)); Stretch(Rect(loader.MusicButton)); Stretch(Rect(loader.SoundButton)); Stretch(Rect(loader.RestartButton));
                Stretch(Rect(loader.solutionButton)); Stretch(Rect(loader.backButton)); Stretch(Rect(loader.NextButton));
                Icon(loader.MusicButton,"icon",40); Icon(loader.SoundButton,"icon",40); Icon(loader.RestartButton,"icon",42);
                if(loader.solutionButton!=null)
                {
                    Icon(loader.solutionButton,"lightBulb",42);
                    RectTransform ring=Find(loader.solutionButton.transform,"aro",false); if(ring!=null) Stretch(ring);
                }
                if(loader.botonNumSol!=null)
                {
                    Fixed(Rect(loader.botonNumSol),new Vector2(1,0),new Vector2(-8,8),new Vector2(28,28));
                    foreach(Text text in loader.botonNumSol.GetComponentsInChildren<Text>(true)) { Stretch(text.rectTransform); TextLayout(text,18,14,TextAnchor.MiddleCenter); }
                    foreach(RectTransform child in loader.botonNumSol.GetComponentsInChildren<RectTransform>(true)) if(child!=Rect(loader.botonNumSol)) Stretch(child);
                }
                Icon(loader.NextButton,"flecha",38);
                if(loader.backButton!=null)
                {
                    RectTransform p=Find(loader.backButton.transform,"Panel1",false); if(p!=null) Stretch(p);
                    Icon(loader.backButton,"Panel1/flecha1",36);
                }
                if(loader.panel3!=null)
                {
                    Stretch(Rect(loader.panel3));
                    RectTransform stepsBar=Find(loader.panel3.transform,"panel2"); Fixed(stepsBar,new Vector2(.5f,0),new Vector2(0,96),new Vector2(620,116));
                    Stretch(Find(stepsBar,"cuadro",false));
                    Fixed(Find(stepsBar,"panel2d",false),new Vector2(.30f,.5f),Vector2.zero,new Vector2(140,92));
                    Fixed(Find(stepsBar,"panel3d",false),new Vector2(.70f,.5f),Vector2.zero,new Vector2(140,92));
                    Stretch(Rect(loader.backButton1)); Stretch(Rect(loader.stepsButton));
                    if(loader.backButton1!=null) { Stretch(Find(loader.backButton1.transform,"Panel1",false)); Icon(loader.backButton1,"Panel1/flecha1",36); }
                    if(loader.stepsButton!=null) { Stretch(Find(loader.stepsButton.transform,"Panel1",false)); Icon(loader.stepsButton,"Panel1/steps",36); }
                }
                foreach(GameObject popup in new[]{loader.cajaSolution,loader.cajaNoMore,loader.cajaComplete,loader.cajaRate})
                    if(popup!=null) ReflowPopup(popup.transform);
            }
            ReflowRotation(Find(ui,"PanelRotation",false));
            // The landscape export used negative local Z as an extra zoom on
            // individual controls. Portrait layout uses actual pixel sizes.
            foreach(RectTransform rect in canvas.GetComponentsInChildren<RectTransform>(true))
                if(rect!=canvas.transform)
                { Vector3 position=rect.localPosition; position.z=0; rect.localPosition=position; EditorUtility.SetDirty(rect); current.rects++; }
        }
        static void ReflowRotation(RectTransform root)
        {
            if(root==null) return;
            Fixed(root,new Vector2(.5f,0),new Vector2(0,208),new Vector2(620,72));
            for(int i=1;i<=2;i++)
            {
                RectTransform window=Find(root,"Window"+i,false); if(window==null) continue;
                Fixed(window,new Vector2(i==1?.25f:.75f,.5f),Vector2.zero,new Vector2(270,64));
                foreach(string name in new[]{"rotateBoard"+i,"rotateBoardSombra"+i})
                    Fixed(Find(window,name,false),new Vector2(0,.5f),new Vector2(24,0),new Vector2(38,38));
                RectTransform label=Find(window,"RotateLabel"+i,false); if(label==null) continue;
                Fixed(label,new Vector2(1,.5f),new Vector2(-104,0),new Vector2(196,58));
                foreach(Text text in label.GetComponentsInChildren<Text>(true)) TextLayout(text,17,13,TextAnchor.MiddleCenter);
            }
        }
        static void ReflowPopup(Transform popup)
        {
            // Native completion/solution animations own the popup root scale.
            // Scale its unanimated inner window instead of fighting that clip.
            foreach(string name in new[]{"Window1","Window2"})
            {
                RectTransform window=Find(popup,name,false); if(window==null) continue;
                window.localScale=new Vector3(1.45f,1.45f,1); window.localRotation=Quaternion.identity;
                EditorUtility.SetDirty(window); current.rects++;
            }
        }
        static void Icon(GameObject target,string path,float size)
        { if(target!=null) Fixed(Find(target.transform,path,false),new Vector2(.5f,.5f),Vector2.zero,new Vector2(size,size)); }
        static RectTransform Rect(GameObject go) { return go==null?null:go.transform as RectTransform; }
        static RectTransform Find(Transform root,string path,bool required=true)
        {
            Transform child=root==null?null:root.Find(path); RectTransform result=child as RectTransform;
            if(result==null&&required) throw new InvalidOperationException("Missing HUD RectTransform: "+(root==null?"null":PathOf(root))+"/"+path);
            return result;
        }
        static void Stretch(RectTransform rect)
        {
            if(rect==null) return; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=Vector2.zero; rect.sizeDelta=Vector2.zero; rect.localScale=Vector3.one; rect.localRotation=Quaternion.identity;
            EditorUtility.SetDirty(rect); if(current!=null) current.rects++;
        }
        static void Fixed(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size)
        {
            if(rect==null) return; rect.anchorMin=rect.anchorMax=anchor; rect.pivot=new Vector2(.5f,.5f); rect.anchoredPosition=position; rect.sizeDelta=size;
            rect.localScale=Vector3.one; rect.localRotation=Quaternion.identity; EditorUtility.SetDirty(rect); if(current!=null) current.rects++;
        }
        static void TextLayout(Text text,int max,int min,TextAnchor alignment)
        {
            text.fontSize=max; text.resizeTextForBestFit=true; text.resizeTextMinSize=min; text.resizeTextMaxSize=max;
            text.alignment=alignment; text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate;
            EditorUtility.SetDirty(text); if(current!=null) current.textLayouts++;
        }
        static IEnumerable<T> Components<T>(Scene scene) where T:Component
        { return scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<T>(true)); }
        static string PathOf(Transform transform) { return transform.parent==null?transform.name:PathOf(transform.parent)+"/"+transform.name; }
        static string ProtectedFingerprint(Scene scene)
        {
            var value=new StringBuilder();
            foreach(Transform transform in Components<Transform>(scene))
            {
                GameObject go=transform.gameObject;
                value.Append(PathOf(transform)).Append('|').Append(transform.GetSiblingIndex()).Append('|').Append(go.activeSelf).Append('|').Append(go.layer).Append('|').Append(go.tag).AppendLine();
                foreach(Component component in go.GetComponents<Component>())
                {
                    value.Append(component==null?"missing":component.GetType().FullName).Append('|');
                    if(component==null||component is RectTransform) { value.AppendLine(); continue; }
                    string json=EditorJsonUtility.ToJson(component);
                    if(component is CanvasScaler)
                        json=Regex.Replace(json,"\\\"(?:m_UiScaleMode|m_ReferenceResolution|m_ScreenMatchMode|m_MatchWidthOrHeight)\\\":(?:\\{[^}]*\\}|[^,}]*),?","");
                    if(component is Canvas)
                        json=Regex.Replace(json,"\\\"m_PlaneDistance\\\":[^,}]*,?","");
                    if(component is Text)
                        json=Regex.Replace(json,"\\\"(?:m_FontSize|m_BestFit|m_MinSize|m_MaxSize|m_Alignment|m_HorizontalOverflow|m_VerticalOverflow)\\\":[^,}]*,?","");
                    value.Append(json).AppendLine();
                }
            }
            using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()))).Replace("-","");
        }
        static void Backup(string asset)
        {
            string copy=Path.Combine(Project,"Validation/Portrait/Before",asset); Directory.CreateDirectory(Path.GetDirectoryName(copy));
            if(!File.Exists(copy)) File.Copy(Path.Combine(Project,asset),copy);
        }
        static void Write(string name,Report report)
        { string output=Path.Combine(Project,"Validation/Portrait"); Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output,name),JsonUtility.ToJson(report,true)); }
    }
}
#endif
