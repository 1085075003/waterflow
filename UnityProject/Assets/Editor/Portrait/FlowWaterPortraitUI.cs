#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Portrait composition of the existing menu controls. No hierarchy or callback changes.</summary>
    public static class FlowWaterPortraitUI
    {
        static readonly string[] Scenes = { "Assets/Scenes/Portada.unity", "Assets/Scenes/SelLevel0.unity", "Assets/Scenes/SelLevel1.unity", "Assets/Settings.unity", "Assets/Store.unity" };
        static readonly Vector2 Reference = new Vector2(720, 1280);
        static readonly Vector2[] Viewports = { new Vector2(720,1280), new Vector2(1080,2400), new Vector2(768,1024) };
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "Portrait"); } }
        [Serializable] public sealed class Control { public string path, component, serializedBinding; public bool activeSelf, raycast, interactable; }
        [Serializable] public sealed class RectCheck { public string path, viewport, exclusion; public Vector2 min, max; public bool valid, inside; }
        [Serializable] public sealed class RaycastChange { public string path, graphic, parentAction, reason; public bool before, after; }
        [Serializable] public sealed class SceneReport { public string scene, beforeHash, afterHash; public int buttons, triggers, sliders; public bool bindingInvariant; public List<Control> before = new List<Control>(), after = new List<Control>(); public List<RectCheck> rectangles = new List<RectCheck>(); public List<string> animatedLayoutBindings = new List<string>(); public List<string> normalizedModalAnimations = new List<string>(); public List<RaycastChange> decorativeTabRaycastWhitelist = new List<RaycastChange>(); }
        [Serializable] public sealed class Report { public string generatedUtc, unityVersion; public bool passed = true; public List<SceneReport> scenes = new List<SceneReport>(); }

        [MenuItem("Tools/Flow Water/Portrait/Apply Menu Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply menu layout in Edit mode.");
            Directory.CreateDirectory(Output);
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in Scenes)
                {
                    string backup = Path.Combine(Output, "Before", path);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    if (!File.Exists(backup)) File.Copy(path, backup);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var sr = new SceneReport { scene = path, before = Controls(scene) };
                    sr.beforeHash = Invariant(scene);
                    Layout(scene,sr);
                    NormalizeModalAnimations(scene, sr);
                    sr.after = Controls(scene); sr.afterHash = Invariant(scene);
                    sr.bindingInvariant = sr.beforeHash == sr.afterHash;
                    if (!sr.bindingInvariant) throw new InvalidOperationException("Portrait UI modified input contracts: " + path);
                    Inspect(scene, sr);
                    if (sr.rectangles.Any(r => r.exclusion == "" && (!r.valid || !r.inside))) report.passed = false;
                    report.scenes.Add(sr);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                File.WriteAllText(Path.Combine(Output,"menu-apply.json"), JsonUtility.ToJson(report,true));
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        public static void Audit()
        {
            Directory.CreateDirectory(Output);
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var sr = new SceneReport { scene = path, after = Controls(scene), afterHash = Invariant(scene), bindingInvariant = true };
                    Inspect(scene, sr); report.scenes.Add(sr);
                    if (sr.rectangles.Any(r => r.exclusion == "" && (!r.valid || !r.inside))) report.passed = false;
                }
                File.WriteAllText(Path.Combine(Output,"menu-audit.json"), JsonUtility.ToJson(report,true));
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        static IEnumerable<T> All<T>(Scene scene) where T:Component { return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)); }
        static string P(Transform t) { return t.parent == null ? t.name : P(t.parent) + "/" + t.name; }
        static RectTransform Find(Scene scene, string path) { return All<RectTransform>(scene).FirstOrDefault(t => P(t) == path); }
        static void Normal(RectTransform t) { if(t==null)return; t.localScale=Vector3.one; t.localRotation=Quaternion.identity; }
        static void Box(RectTransform t,float x,float y,float w,float h, bool top=false)
        {
            if(t==null)return; Normal(t); t.anchorMin=t.anchorMax=new Vector2(.5f,top?1:.5f); t.pivot=Vector2.one*.5f;
            t.anchoredPosition=new Vector2(x,y); t.sizeDelta=new Vector2(w,h); EditorUtility.SetDirty(t);
        }
        static void Fill(RectTransform t,float inset=0)
        {
            if(t==null)return; Normal(t); t.anchorMin=Vector2.zero; t.anchorMax=Vector2.one; t.pivot=Vector2.one*.5f;
            t.offsetMin=Vector2.one*inset; t.offsetMax=-Vector2.one*inset; EditorUtility.SetDirty(t);
        }
        static void Box(Scene s,string path,float x,float y,float w,float h,bool top=false) { Box(Find(s,path),x,y,w,h,top); }
        static void BottomBox(Scene s,string path,float x,float bottom,float w,float h)
        {
            RectTransform rect=Find(s,path); Box(rect,x,bottom,w,h);
            if(rect!=null) { rect.anchorMin=rect.anchorMax=new Vector2(.5f,0); EditorUtility.SetDirty(rect); }
        }
        static void Fill(Scene s,string path,float inset=0) { Fill(Find(s,path),inset); }
        static void ClearDecoration(Scene s,string path)
        {
            RectTransform rect=Find(s,path);if(rect==null)return;Image image=rect.GetComponent<Image>();if(image==null)return;
            Color color=image.color;color.a=0;image.color=color;EditorUtility.SetDirty(image);
        }
        static void Texts(Transform t,int size)
        {
            if(t==null)return;
            foreach(Text text in t.GetComponentsInChildren<Text>(true))
            {
                text.fontSize=size; text.resizeTextForBestFit=true; text.resizeTextMinSize=Mathf.Max(16,size-8); text.resizeTextMaxSize=size;
                text.alignment=TextAnchor.MiddleCenter; text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate;
                Normal(text.rectTransform); EditorUtility.SetDirty(text);
            }
        }
        static void ButtonFace(RectTransform t,int size)
        {
            if(t==null)return;
            foreach(RectTransform r in t.GetComponentsInChildren<RectTransform>(true))
            {
                Normal(r);
                if(r==t)continue;
                if(r.GetComponent<Text>()!=null || r.name.StartsWith("Label",StringComparison.Ordinal) || r.name.StartsWith("Background",StringComparison.Ordinal)) Fill(r);
            }
            Texts(t,size);
        }

        static void Layout(Scene s,SceneReport report)
        {
            foreach(Canvas canvas in All<Canvas>(s))
            {
                if(canvas.transform.parent!=null || canvas.renderMode==RenderMode.WorldSpace)continue;
                Normal(canvas.transform as RectTransform);
                var scaler=canvas.GetComponent<CanvasScaler>();
                if(scaler!=null) { scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=Reference; scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand; scaler.matchWidthOrHeight=0; EditorUtility.SetDirty(scaler); }
            }
            foreach(RectTransform r in All<RectTransform>(s))
                if(r.GetComponentInParent<Canvas>()!=null && (r.name.StartsWith("Fondo",StringComparison.OrdinalIgnoreCase) || r.name.StartsWith("PanelFade",StringComparison.Ordinal))) Fill(r);
            if(s.name=="Portada") Home(s);
            else if(s.name=="SelLevel0") Categories(s,report);
            else if(s.name=="SelLevel1") Levels(s);
            else if(s.name=="Settings") Settings(s);
            else if(s.name=="Store") Store(s);
            Modals(s);
        }

        static void Logo(Scene s,string root,float top)
        {
            Fill(s,root);
            foreach(string suffix in new[]{"","/Panel1b","/Panel1b/Panel","/Panel2","/Panel2/PanelPuzzle"})ClearDecoration(s,root+suffix);
            Box(s,root+"/Panel1b",0,top,600,140,true); Fill(s,root+"/Panel1b/Panel"); Fill(s,root+"/Panel1b/Panel/Titulo1Flow1");
            Box(s,root+"/Panel2",0,top-100,380,46,true); Fill(s,root+"/Panel2/PanelPuzzle"); Fill(s,root+"/Panel2/PanelPuzzle/3D");
        }
        static void Home(Scene s)
        {
            Logo(s,"Canvas/PanelTitulo",-125);
            Fill(s,"Canvas/PanelButtons"); Fill(s,"Canvas/PanelButtons/Window (1)");
            ClearDecoration(s,"Canvas/PanelButtons"); ClearDecoration(s,"Canvas/PanelButtons/Window (1)");
            string root="Canvas/PanelButtons/Window (1)/";
            Box(s,root+"Play",0,-290,480,104); ButtonFace(Find(s,root+"Play"),36);
            Box(s,root+"Options",0,-410,420,84); ButtonFace(Find(s,root+"Options"),28);
            Box(s,root+"Exit",0,-510,300,72); ButtonFace(Find(s,root+"Exit"),25);
        }
        static void Categories(Scene s,SceneReport report)
        {
            Fill(s,"Canvas/MainMenu1"); Fill(s,"Canvas/MainMenu1/GrupoTabs"); Fill(s,"Canvas/MainMenu1/GrupoTabs/grupoTab");
            ClearDecoration(s,"Canvas/MainMenu1"); ClearDecoration(s,"Canvas/MainMenu1/GrupoTabs"); ClearDecoration(s,"Canvas/MainMenu1/GrupoTabs/grupoTab");
            string stem="Canvas/MainMenu1/GrupoTabs/grupoTab/";
            Fill(s,stem+"Panel1"); Box(s,stem+"Panel1/arriba",0,335,624,188); BottomBox(s,stem+"Panel1/abajo",0,595,620,540); Box(s,stem+"Panel1/barra",0,220,620,2);
            ClearDecoration(s,stem+"Panel1");ClearDecoration(s,stem+"Panel2");
            Fill(s,stem+"Panel2"); Box(s,stem+"Panel2/Checks",0,335,624,188);
            for(int n=1;n<=6;n++)
            {
                string check=stem+"Panel2/Checks/part"+n;
                Box(s,check,((n-1)%3-1)*208,44-((n-1)/3)*88,196,76);
                Fill(s,check+"/boton"+n);
            }
            for(int g=1;g<=6;g++)
            {
                string group=stem+"grupoTab"+g; Fill(s,group);
                ClearDecoration(s,group);
                string tabs=group+"/PanelTab"+g;
                Box(s,tabs,0,335,624,188);
                for(int n=1;n<=6;n++)
                {
                    string cell=tabs+"/part"+n;
                    Box(s,cell,((n-1)%3-1)*208,44-((n-1)/3)*88,196,76);
                    var button=Find(s,cell+"/boton"+n); Fill(button); ButtonFace(button,24);
                    foreach(RectTransform child in button==null?new RectTransform[0]:button.GetComponentsInChildren<RectTransform>(true))
                        if(child!=button && child.GetComponent<Text>()==null && child.name!="tab") Fill(child);
                    RectTransform decorative=Find(s,cell+"/boton"+n+"/tab");
                    if(decorative!=null)
                        foreach(Graphic graphic in decorative.GetComponentsInChildren<Graphic>(true))
                        {
                            report.decorativeTabRaycastWhitelist.Add(new RaycastChange {path=P(graphic.transform),graphic=graphic.GetType().Name,parentAction=P(button)+" / botonTabDown("+n+")",before=graphic.raycastTarget,after=false,reason="Overlapping decorative tab alias; all six category actions stay on their original parent buttons. Callback bindings and arguments remain unchanged."});
                            graphic.raycastTarget=false;EditorUtility.SetDirty(graphic);
                        }
                }
                string packs=group+"/PanelBotones"; BottomBox(s,packs,0,595,620,540);
                ClearDecoration(s,packs);
                var holder=Find(s,packs); if(holder==null)continue;
                var packCells=holder.Cast<Transform>().Where(t=>t.name!="Back" && ActionRects(t,"botonPartDown").Length>0).OrderBy(t=>PackOrder(t.name)).ThenBy(t=>t.name,StringComparer.Ordinal).ToArray();
                for(int i=0;i<packCells.Length;i++)
                {
                    var cell=(RectTransform)packCells[i]; Box(cell,(i%2==0?-157:157),205-(i/2)*120,296,104);
                    foreach(RectTransform control in ActionRects(cell,"botonPartDown"))
                    {
                        Fill(control,3); ButtonFace(control,27);
                        foreach(RectTransform face in control.GetComponentsInChildren<RectTransform>(true))
                            if(face.GetComponent<Image>()!=null && !face.name.StartsWith("Check",StringComparison.Ordinal) && !face.name.StartsWith("Candado",StringComparison.Ordinal) && face.GetComponentInParent<Selectable>()==control.GetComponent<Selectable>())
                            {
                                // The card image and its authored label containers occupy the full hit target.
                                if(face==control || face.name.StartsWith("Background",StringComparison.Ordinal))
                                { if(face!=control)Fill(face); Image image=face.GetComponent<Image>();image.preserveAspect=false;EditorUtility.SetDirty(image); }
                            }
                    }
                    foreach(RectTransform icon in cell.GetComponentsInChildren<RectTransform>(true))
                        if(icon.name.StartsWith("CheckOk",StringComparison.Ordinal) || icon.name.StartsWith("Candado",StringComparison.Ordinal)) Box(icon,112,-30,34,34);
                }
                foreach(Transform back in holder.Cast<Transform>().Where(t=>t.name=="Back"))
                {
                    Box(back as RectTransform,0,-535,220,72);
                    foreach(RectTransform control in ActionRects(back,"botonMenuDown")) { Fill(control); ButtonFace(control,26); }
                }
            }
            // The two tablet title variants retain their active state and original reference fields.
            Fill(s,"Canvas/PanelTitulo1"); Box(s,"Canvas/PanelTitulo1/Panel",0,-80,620,100,true);
            foreach(string suffix in new[]{"","/Panel","/Panel/Panel","/Panel/Panel/Panel0","/Panel/Panel/Panel1","/Panel/Panel/Panel1a","/Panel/Panel/Panel1b","/Panel/Panel/Panel2","/Panel1a","/Panel1a/Panel2"}) ClearDecoration(s,"Canvas/PanelTitulo1"+suffix);
            Fill(s,"Canvas/PanelTitulo1/Panel/Panel"); Box(s,"Canvas/PanelTitulo1/Panel/Panel/Panel1",-90,0,380,84);
            Box(s,"Canvas/PanelTitulo1/Panel/Panel/Panel1a",190,0,160,48); Box(s,"Canvas/PanelTitulo1/Panel/Panel/Panel1b",190,0,160,48);
            Box(s,"Canvas/PanelTitulo1/Panel/Panel/Panel2",274,0,72,72);
            var shop=Find(s,"Canvas/PanelTitulo1/Panel/Panel/Panel2/Store"); Fill(shop); ButtonFace(shop,24);
            Box(s,"Canvas/PanelTitulo1/Panel1a",0,-240,620,80,true);
            foreach(RectTransform r in All<RectTransform>(s)) if(P(r).Contains("/PanelTitulo1/Panel/Panel/Panel1") && (r.GetComponent<Image>()!=null || r.GetComponent<Text>()!=null) && r.GetComponent<EventTrigger>()==null) { if(r.GetComponent<Image>()!=null && r.GetComponent<Image>().sprite!=null && r.GetComponent<Image>().sprite.name=="wordmark") Fill(r); }
        }
        static void Levels(Scene s)
        {
            Box(s,"Canvas/Window1",0,-88,620,96,true); Box(s,"Canvas/Window1/BotonMenu",-270,0,72,72); ButtonFace(Find(s,"Canvas/Window1/BotonMenu"),24);
            Box(s,"Canvas/Window1/BotonMenu/icon2",0,0,30,30);
            Box(s,"Canvas/Window1/BotonMenu/circulo",0,0,72,72);
            Box(s,"Canvas/Window1/LabelParte",20,0,450,70); Texts(Find(s,"Canvas/Window1/LabelParte"),36);
            Box(s,"Canvas/Paneles",0,0,720,920);
            ClearDecoration(s,"Canvas/Paneles");
            var panels=Find(s,"Canvas/Paneles"); if(panels==null)return;
            int page=0;
            foreach(RectTransform p in panels.Cast<Transform>().Where(t=>t.name.StartsWith("Panel",StringComparison.Ordinal)).OrderBy(t=>((RectTransform)t).anchoredPosition.x))
            {
                Box(p,page*900,0,720,920); page++;
                var rows=p.Cast<Transform>().Where(t=>t.name.StartsWith("Panel1",StringComparison.Ordinal)).ToArray(); foreach(Transform row in rows) {Fill(row as RectTransform);ClearDecoration(s,P(row));}
                var cells=p.GetComponentsInChildren<EventTrigger>(true).Where(t=>HasMethod(t,"Level")).OrderBy(t=>PersistentInt(t,"Level")).ToArray();
                for(int i=0;i<cells.Length;i++)
                {
                    RectTransform cell=cells[i].transform as RectTransform;
                    Box(cell,i%2==0?-148:148,345-(i/2)*158,260,130); ButtonFace(cell,48);
                    foreach(RectTransform r in cell.GetComponentsInChildren<RectTransform>(true))
                    {
                        if(r.name.StartsWith("TextN",StringComparison.Ordinal)) Fill(r,10);
                        if(r.name.StartsWith("Select",StringComparison.Ordinal)) Fill(r);
                        if(r.name.StartsWith("checkOk",StringComparison.Ordinal)||r.name.StartsWith("checkSkip",StringComparison.Ordinal)||r.name.StartsWith("Candado",StringComparison.Ordinal)) Box(r,103,-40,36,36);
                    }
                }
                foreach(RectTransform arrow in p.Cast<Transform>().Where(t=>t.name.StartsWith("Flecha",StringComparison.Ordinal)))
                {
                    bool back=arrow.name.Contains("Back"); Box(arrow,back?-265:265,-475,78,72); ButtonFace(arrow,25);
                    foreach(RectTransform child in arrow.GetComponentsInChildren<RectTransform>(true)) if(child!=arrow) Fill(child,10);
                }
            }
        }
        static void Settings(Scene s)
        {
            Logo(s,"Canvas/PanelTitulo (1)",-110); Logo(s,"Canvas/PanelTitulo",-110);
            Fill(s,"Canvas/Panel"); Fill(s,"Canvas/Panel/PanelButtons1"); Fill(s,"Canvas/Panel/PanelButtons2");
            ClearDecoration(s,"Canvas/Panel"); ClearDecoration(s,"Canvas/Panel/PanelButtons1"); ClearDecoration(s,"Canvas/Panel/PanelButtons2");
            Box(s,"Canvas/Panel/PanelButtons1/Window",0,15,620,540); Box(s,"Canvas/Panel/PanelButtons2/Window",0,-370,620,240);
            string audio="Canvas/Panel/PanelButtons1/Window/";
            Box(s,audio+"Music",-145,205,260,64); ButtonFace(Find(s,audio+"Music"),29);
            Box(s,audio+"SliderMusic",0,123,520,56);
            Box(s,audio+"Sound",-145,15,260,64); ButtonFace(Find(s,audio+"Sound"),29);
            Box(s,audio+"SliderSound",0,-67,520,56);
            Box(s,audio+"Reset",0,-200,520,72); ButtonFace(Find(s,audio+"Reset"),26);
            foreach(Slider slider in All<Slider>(s))
            {
                RectTransform rect=slider.transform as RectTransform; Normal(rect);
                var bg=rect.Find("Background") as RectTransform; Box(bg,0,0,520,14);
                var handle=rect.Find("Handle Slide Area/Handle") as RectTransform; if(handle!=null) {Normal(handle); handle.sizeDelta=new Vector2(44,0);}
                var area=rect.Find("Handle Slide Area") as RectTransform; Fill(area); if(area!=null) {area.offsetMin=new Vector2(22,0);area.offsetMax=new Vector2(-22,0);}
            }
            string actions="Canvas/Panel/PanelButtons2/Window/";
            Box(s,actions+"Store",-158,55,300,76); ButtonFace(Find(s,actions+"Store"),27);
            Box(s,actions+"Rate",158,55,300,76); ButtonFace(Find(s,actions+"Rate"),27);
            Box(s,actions+"Back",0,-60,300,72); ButtonFace(Find(s,actions+"Back"),27);
        }
        static void Store(Scene s)
        {
            Box(s,"Canvas/WindowStore",0,-78,620,90,true); Texts(Find(s,"Canvas/WindowStore"),36);
            Box(s,"Canvas/Paneles",0,-25,720,950); Fill(s,"Canvas/Paneles (1)");
            ClearDecoration(s,"Canvas/Paneles"); ClearDecoration(s,"Canvas/Paneles (1)");
            Box(s,"Canvas/Paneles (1)/Panel - Footer1 (1)",0,-548,620,86); Box(s,"Canvas/Paneles (1)/Panel - Footer1 (1)/Back1",0,0,260,72); ButtonFace(Find(s,"Canvas/Paneles (1)/Panel - Footer1 (1)/Back1"),26);
            for(int n=1;n<=2;n++)
            {
                string stem="Canvas/Paneles/Panel"+n; Box(s,stem,(n-1)*900,0,720,950); Fill(s,stem+"/RecuadroAzul",12);
                Fill(s,stem+"/Scroll View",24); Fill(s,stem+"/Scroll View/Viewport"); Fill(s,stem+"/Scroll View/Viewport/Content");
                var content=Find(s,stem+"/Scroll View/Viewport/Content"); if(content==null)continue;
                var parts=content.Cast<Transform>().Where(t=>t.name.StartsWith("parte",StringComparison.Ordinal)).OrderBy(t=>t.name).ToArray();
                for(int i=0;i<parts.Length;i++)
                {
                    Box(parts[i] as RectTransform,0,367-i*141,610,130);
                    foreach(RectTransform product in parts[i].Cast<Transform>())
                    {
                        Fill(product); var all=product.Find("PanelAll") as RectTransform; Fill(all); if(all==null)continue;
                        Box(all.Find("Caja1") as RectTransform,-225,0,130,110);
                        Box(all.Find("Caja2") as RectTransform,208,34,125,36);
                        Box(all.Find("Caja3") as RectTransform,208,-24,130,50);
                        Box(all.Find("Caja4") as RectTransform,-12,0,265,110);
                        foreach(Text text in product.GetComponentsInChildren<Text>(true)) { Texts(text.transform,text.name.StartsWith("desc",StringComparison.Ordinal)?22:23); Fill(text.rectTransform,2); }
                        foreach(EventTrigger trigger in product.GetComponentsInChildren<EventTrigger>(true)) if(!trigger.name.StartsWith("BotonTest",StringComparison.Ordinal)) { Fill(trigger.transform as RectTransform); ButtonFace(trigger.transform as RectTransform,23); }
                    }
                }
                foreach(RectTransform arrow in content.Cast<Transform>().Where(t=>t.name.StartsWith("Flecha",StringComparison.Ordinal))) {Box(arrow,arrow.name.Contains("Back")?-270:270,-449,68,54); ButtonFace(arrow,24);}
            }
        }

        static bool HasMethod(EventTrigger trigger,string method) { return trigger.triggers!=null && trigger.triggers.Any(e=>e.callback!=null && Enumerable.Range(0,e.callback.GetPersistentEventCount()).Any(i=>e.callback.GetPersistentMethodName(i)==method)); }
        static bool HasMethod(Button button,string method) { return Enumerable.Range(0,button.onClick.GetPersistentEventCount()).Any(i=>button.onClick.GetPersistentMethodName(i)==method); }
        static RectTransform[] ActionRects(Transform owner,string method)
        {
            return owner.GetComponentsInChildren<EventTrigger>(true).Where(e=>HasMethod(e,method)).Select(e=>e.transform as RectTransform)
                .Concat(owner.GetComponentsInChildren<Button>(true).Where(b=>HasMethod(b,method)).Select(b=>b.transform as RectTransform)).Where(t=>t!=null).Distinct().ToArray();
        }
        static int PackOrder(string name)
        {
            string[] classic={"Basic","Easy","Hard","Mix","Master","Genius","Maniac"};
            int n=Array.IndexOf(classic,name);return n<0?100:n;
        }
        static int PersistentInt(EventTrigger trigger,string method)
        {
            var so=new SerializedObject(trigger); var entries=so.FindProperty("m_Delegates"); if(entries==null)return 0;
            for(int e=0;e<entries.arraySize;e++) {var calls=entries.GetArrayElementAtIndex(e).FindPropertyRelative("callback.m_PersistentCalls.m_Calls"); if(calls==null)continue; for(int i=0;i<calls.arraySize;i++){var call=calls.GetArrayElementAtIndex(i);if(call.FindPropertyRelative("m_MethodName").stringValue==method)return call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue;}}
            return 0;
        }
        static void Modals(Scene s)
        {
            string[] modalNames={"PanelExit1","PanelUnlock1","ResetPopup","ResetOk","watchBox","CajaError"};
            foreach(RectTransform modal in All<RectTransform>(s).Where(r=>modalNames.Contains(r.name)))
            {
                Box(modal,0,0,620,480);
                foreach(RectTransform w in modal.Cast<Transform>()) if(w.name.StartsWith("Window",StringComparison.Ordinal)||w.name.StartsWith("caja",StringComparison.Ordinal))
                {
                    Fill(w,8);
                    var controls=w.GetComponentsInChildren<EventTrigger>(true).Where(t=>!t.GetComponentsInParent<EventTrigger>(true).Any(a=>a!=t)).ToArray();
                    for(int i=0;i<controls.Length;i++) {Box(controls[i].transform as RectTransform,controls.Length==1?0:(i%2==0?-140:140),-140,116,76);ButtonFace(controls[i].transform as RectTransform,26);}
                    foreach(Text t in w.GetComponentsInChildren<Text>(true))
                    {
                        // Flatten only presentation-container rectangles; event nodes and their descendants keep their controls.
                        if(t.GetComponentInParent<EventTrigger>()!=null)continue;
                        Transform p=t.transform.parent;
                        while(p!=null && p!=w) {Fill(p as RectTransform);p=p.parent;}
                        Box(t.rectTransform,0,52,520,215); Texts(t.transform,28);
                    }
                }
            }
        }

        static void NormalizeModalAnimations(Scene s, SceneReport report)
        {
            const string folder="Assets/ArtUpgrade/Portrait/Animation";
            foreach(Animator animator in All<Animator>(s))
            {
                if(animator.GetComponentInParent<Canvas>()==null || animator.runtimeAnimatorController==null)continue;
                RuntimeAnimatorController source=animator.runtimeAnimatorController;
                if(AssetDatabase.GetAssetPath(source).StartsWith(folder+"/",StringComparison.Ordinal))continue;
                var replacements=new List<KeyValuePair<AnimationClip,AnimationClip>>();
                foreach(AnimationClip clip in source.animationClips.Distinct())
                {
                    var bindings=AnimationUtility.GetCurveBindings(clip).Where(b=>b.path=="" && b.propertyName.StartsWith("m_LocalScale.",StringComparison.Ordinal)).ToArray();
                    var normalize=new List<EditorCurveBinding>();
                    foreach(var binding in bindings)
                    {
                        AnimationCurve curve=AnimationUtility.GetEditorCurve(clip,binding);if(curve==null||curve.length==0)continue;
                        float end=curve.keys[curve.length-1].value;if(Mathf.Abs(end)>.001f && Mathf.Abs(end)<.5f)normalize.Add(binding);
                    }
                    if(normalize.Count==0)continue;
                    EnsureFolder(folder);
                    string clipPath=folder+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip))+".anim";
                    AnimationClip output=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if(output==null){output=UnityEngine.Object.Instantiate(clip);output.name=clip.name;AssetDatabase.CreateAsset(output,clipPath);}
                    else EditorUtility.CopySerialized(clip,output);
                    foreach(var binding in normalize)
                    {
                        AnimationCurve curve=AnimationUtility.GetEditorCurve(clip,binding);float end=curve.keys[curve.length-1].value;var keys=curve.keys;
                        for(int i=0;i<keys.Length;i++){keys[i].value/=end;keys[i].inTangent/=end;keys[i].outTangent/=end;}
                        curve.keys=keys;AnimationUtility.SetEditorCurve(output,binding,curve);
                    }
                    EditorUtility.SetDirty(output);replacements.Add(new KeyValuePair<AnimationClip,AnimationClip>(clip,output));
                    report.normalizedModalAnimations.Add(P(animator.transform)+" / "+clip.name+" / root scale normalized; duration and all other bindings retained");
                }
                if(replacements.Count==0)continue;
                string controllerPath=folder+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".overrideController";
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
                if(controller==null){controller=new AnimatorOverrideController(source);AssetDatabase.CreateAsset(controller,controllerPath);}
                else controller.runtimeAnimatorController=source;
                controller.ApplyOverrides(replacements);EditorUtility.SetDirty(controller);animator.runtimeAnimatorController=controller;EditorUtility.SetDirty(animator);
            }
            AssetDatabase.SaveAssets();
        }
        static void EnsureFolder(string path)
        {
            string current="Assets";foreach(string part in path.Split('/').Skip(1)){string next=current+"/"+part;if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,part);current=next;}
        }

        static List<Control> Controls(Scene s)
        {
            var list=new List<Control>();
            foreach(Transform t in All<Transform>(s))
            {
                foreach(Component c in t.GetComponents<Component>())
                {
                    if(!(c is Selectable) && !(c is EventTrigger))continue;
                    var graphic=t.GetComponent<Graphic>(); var selectable=c as Selectable;
                    list.Add(new Control {path=P(t),component=c.GetType().Name,activeSelf=t.gameObject.activeSelf,raycast=graphic!=null&&graphic.raycastTarget,interactable=selectable==null||selectable.interactable,serializedBinding=EditorJsonUtility.ToJson(c)});
                }
            }
            return list.OrderBy(x=>x.path).ThenBy(x=>x.component).ToList();
        }
        static string Invariant(Scene s)
        {
            var b=new StringBuilder();
            foreach(Transform t in All<Transform>(s))
            {
                b.Append(P(t)).Append('|').Append(t.GetSiblingIndex()).Append('|').Append(t.gameObject.activeSelf).Append('|').Append(t.gameObject.layer).Append('|').Append(t.gameObject.tag);
                foreach(Component c in t.GetComponents<Component>())
                {
                    b.Append('|').Append(c==null?"missing":c.GetType().FullName);
                    if(c is Button) {var so=new SerializedObject(c);b.Append(so.FindProperty("m_OnClick").propertyPath).Append(EventJson(so.FindProperty("m_OnClick")));}
                    else if(c is EventTrigger) b.Append(EditorJsonUtility.ToJson(c));
                    else if(c is Slider) {var sl=(Slider)c;b.Append(EventJson(new SerializedObject(sl).FindProperty("m_OnValueChanged"))).Append('|').Append(sl.minValue).Append('|').Append(sl.maxValue).Append('|').Append(sl.wholeNumbers);}
                    if(c is Graphic)
                    {
                        if(IsDecorativeTabPath(s,P(t)))b.Append("ray:portrait-decorative-tab-whitelist");
                        else b.Append("ray:").Append(((Graphic)c).raycastTarget);
                    }
                }
                b.AppendLine();
            }
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()))).Replace("-","");
        }
        static bool IsDecorativeTabPath(Scene scene,string path)
        {
            if(scene.name!="SelLevel0")return false;
            for(int g=1;g<=6;g++)for(int n=1;n<=6;n++)
            {
                string root="Canvas/MainMenu1/GrupoTabs/grupoTab/grupoTab"+g+"/PanelTab"+g+"/part"+n+"/boton"+n+"/tab";
                if(path==root||path.StartsWith(root+"/",StringComparison.Ordinal))return true;
            }
            return false;
        }
        static string EventJson(SerializedProperty property)
        {
            if(property==null)return "";var b=new StringBuilder();var it=property.Copy();var end=property.GetEndProperty();
            while(it.Next(true)&&!SerializedProperty.EqualContents(it,end))
            {
                b.Append(it.propertyPath).Append(':');
                switch(it.propertyType) { case SerializedPropertyType.Integer:b.Append(it.intValue);break;case SerializedPropertyType.Enum:b.Append(it.enumValueIndex);break;case SerializedPropertyType.String:b.Append(it.stringValue);break;case SerializedPropertyType.Boolean:b.Append(it.boolValue);break;case SerializedPropertyType.Float:b.Append(it.floatValue);break;case SerializedPropertyType.ObjectReference:b.Append(it.objectReferenceInstanceIDValue);break; }
                b.Append(';');
            }
            return b.ToString();
        }
        static Rect LogicalRect(RectTransform r,Vector2 canvasSize)
        {
            if(r.parent==null || !(r.parent is RectTransform))return new Rect(Vector2.zero,canvasSize);
            Rect parent=LogicalRect((RectTransform)r.parent,canvasSize); Vector2 size=Vector2.Scale(parent.size,r.anchorMax-r.anchorMin)+r.sizeDelta;
            Vector2 pivot=parent.min+Vector2.Scale(parent.size,r.anchorMin+Vector2.Scale(r.anchorMax-r.anchorMin,r.pivot))+r.anchoredPosition;
            Vector2 scaled=Vector2.Scale(size,new Vector2(Mathf.Abs(r.localScale.x),Mathf.Abs(r.localScale.y)));
            return new Rect(pivot-Vector2.Scale(scaled,r.pivot),scaled);
        }
        static void Inspect(Scene s,SceneReport report)
        {
            report.buttons=All<Button>(s).Count();report.triggers=All<EventTrigger>(s).Count();report.sliders=All<Slider>(s).Count();
            foreach(Animator animator in All<Animator>(s))
                if(animator.runtimeAnimatorController!=null)foreach(AnimationClip clip in animator.runtimeAnimatorController.animationClips.Distinct())
                    foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)) if(binding.propertyName.Contains("AnchoredPosition")||binding.propertyName.Contains("SizeDelta"))report.animatedLayoutBindings.Add(P(animator.transform)+" / "+clip.name+" / "+binding.path+" / "+binding.propertyName);
            foreach(Vector2 viewport in Viewports)
            {
                float scale=Mathf.Min(viewport.x/Reference.x,viewport.y/Reference.y);Vector2 logical=viewport/scale;
                foreach(RectTransform r in All<RectTransform>(s).Where(t=>t.GetComponent<Selectable>()!=null||t.GetComponent<EventTrigger>()!=null))
                {
                    string path=P(r); Rect rect=LogicalRect(r,logical); string excluded="";
                    if(r.GetComponentInParent<Canvas>()==null)excluded="not UI";
                    else if(!r.gameObject.activeInHierarchy)excluded="inactive authored control";
                    else if(path.IndexOf("Design",StringComparison.OrdinalIgnoreCase)>=0)excluded="authored debug interface hidden by recovered UI";
                    else if(IsDecorativeTabPath(s,path) && !r.GetComponentsInChildren<Graphic>(true).Any(g=>g.raycastTarget))excluded="decorative tab alias; parent retains the category action";
                    // Other pages intentionally sit outside the viewport; validate each page at its own 900-unit origin.
                    RectTransform page=r;
                    while(page!=null && page.parent!=null && page.parent.name!="Paneles")page=page.parent as RectTransform;
                    if(page!=null && page.parent!=null && page.parent.name=="Paneles")rect.x-=page.anchoredPosition.x;
                    bool valid=rect.width>1&&rect.height>1&&!float.IsNaN(rect.x)&&!float.IsNaN(rect.y);
                    bool inside=rect.xMin>=-2&&rect.yMin>=-2&&rect.xMax<=logical.x+2&&rect.yMax<=logical.y+2;
                    report.rectangles.Add(new RectCheck{path=path,viewport=viewport.x+"x"+viewport.y,min=rect.min,max=rect.max,valid=valid,inside=inside,exclusion=excluded});
                }
            }
        }
    }
}
#endif
