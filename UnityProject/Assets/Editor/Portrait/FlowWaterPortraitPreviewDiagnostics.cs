#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Read-only live particle inventory for the original category miniature instances.</summary>
    public static class FlowWaterPortraitPreviewDiagnostics
    {
        [Serializable] public sealed class CurveValue
        {
            public string mode; public float constantMin,constantMax,multiplier;
            public CurveValue(ParticleSystem.MinMaxCurve curve)
            { mode=curve.mode.ToString();constantMin=curve.constantMin;constantMax=curve.constantMax;multiplier=curve.curveMultiplier; }
        }
        [Serializable] public sealed class ParticleInfo
        {
            public string path,preview,parentPreview,scalingMode,simulationSpace;
            public int scalingModeValue,aliveParticles;
            public bool isPlaying,rendererEnabled,startSize3D;
            public Vector3 localScale,lossyScale,worldPosition;
            public CurveValue startSize,startSizeX,startSizeY,startSizeZ,startSpeed;
        }
        [Serializable] public sealed class PreviewInfo
        {
            public int tab;public string path,sourcePrefab;
            public Vector3 localScale,lossyScale;
            public bool active;
            public List<ParticleInfo> particles=new List<ParticleInfo>();
        }
        [Serializable] public sealed class Report
        {
            public string generatedUtc,scene,unityVersion;public int selectedTab,width,height,activeParticles,visibleParticleSystems;
            public List<PreviewInfo> previews=new List<PreviewInfo>();
        }
        [MenuItem("Tools/Flow Water/Portrait/Export Live Category Particle Diagnostics")]
        public static void Export()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Particle diagnostics require the running category scene in Play mode.");
            Scene scene=SceneManager.GetActiveScene();if(scene.name!="SelLevel0")throw new InvalidOperationException("Open category selection before exporting preview particles.");
            var report=new Report {generatedUtc=DateTime.UtcNow.ToString("o"),scene=scene.name,unityVersion=Application.unityVersion,width=Screen.width,height=Screen.height,
                selectedTab=FlowWaterRecoveryUI.Instance==null?0:FlowWaterRecoveryUI.Instance.SelectedTab};
            foreach(UIManagerSelScript0 selector in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UIManagerSelScript0>(true)))
                for(int n=1;n<=6;n++)
                {
                    var field=typeof(UIManagerSelScript0).GetField("grupo"+n,BindingFlags.Public|BindingFlags.Instance);
                    var sourceField=typeof(UIManagerSelScript0).GetField("prefabGrupo"+n,BindingFlags.Public|BindingFlags.Instance);
                    var preview=field==null?null:field.GetValue(selector) as GameObject;
                    var source=sourceField==null?null:sourceField.GetValue(selector) as GameObject;
                    if(preview==null)continue;
                    var info=new PreviewInfo {tab=n,path=PathOf(preview.transform),sourcePrefab=source==null?"":source.name,active=preview.activeInHierarchy,
                        localScale=preview.transform.localScale,lossyScale=preview.transform.lossyScale};
                    foreach(ParticleSystem particle in preview.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        if(!particle.gameObject.activeInHierarchy)continue;
                        ParticleSystem.MainModule main=particle.main;ParticleSystemRenderer renderer=particle.GetComponent<ParticleSystemRenderer>();
                        var item=new ParticleInfo {path=PathOf(particle.transform),preview=PathOf(preview.transform),parentPreview=PathOf(preview.transform.parent),
                            scalingMode=main.scalingMode.ToString(),scalingModeValue=(int)main.scalingMode,simulationSpace=main.simulationSpace.ToString(),
                            aliveParticles=particle.particleCount,isPlaying=particle.isPlaying,rendererEnabled=renderer!=null&&renderer.enabled,startSize3D=main.startSize3D,
                            localScale=particle.transform.localScale,lossyScale=particle.transform.lossyScale,worldPosition=particle.transform.position,
                            startSize=new CurveValue(main.startSize),startSizeX=new CurveValue(main.startSizeX),startSizeY=new CurveValue(main.startSizeY),startSizeZ=new CurveValue(main.startSizeZ),startSpeed=new CurveValue(main.startSpeed)};
                        info.particles.Add(item);report.activeParticles++;if(item.rendererEnabled)report.visibleParticleSystems++;
                    }
                    report.previews.Add(info);
                }
            string output=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Validation","Portrait");Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"preview-particles.json"),JsonUtility.ToJson(report,true));
            Debug.Log("Read-only category particle diagnostics: "+report.activeParticles+" active systems, "+report.visibleParticleSystems+" enabled renderers.");
        }
        static string PathOf(Transform item) {return item==null?"":item.parent==null?item.name:PathOf(item.parent)+"/"+item.name;}
    }
}
#endif
