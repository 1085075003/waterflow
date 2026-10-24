using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Desktop-only background depth adaptation. Original UI geometry and artwork stay intact.</summary>
public static class RecoveredBackdropDepth
{
    public const string ShaderName="Recovered/UI/BackdropFarDepth";
    public sealed class Binding
    {
        public Image Image {get;private set;}
        public Material OriginalMaterial {get;private set;}
        public Material BackgroundMaterial {get;private set;}
        internal Binding(Image image,Material original,Material background)
        {Image=image;OriginalMaterial=original;BackgroundMaterial=background;}
    }
    static readonly List<Binding> bindings=new List<Binding>();
    public static IReadOnlyList<Binding> Bindings {get{return bindings;}}
    static int sceneHandle=-1;
    static Shader shader;
    static RecoveredBackdropDepth()
    {
        SceneManager.sceneUnloaded+=scene=>{if(scene.handle==sceneHandle)Clear();};
    }
    public static void Apply(Scene scene)
    {
        if(!OriginalScenePresentation.IsGameplayScene(scene))return;
        if(sceneHandle!=scene.handle){Clear();sceneHandle=scene.handle;}
        if(shader==null)shader=Resources.Load<Shader>("RecoveredBackdropDepth");
        if(shader==null){Debug.LogError("RecoveredBackdropDepth shader resource is missing.");return;}
        foreach(UIManagerScript manager in OriginalScenePresentation.Components<UIManagerScript>(scene))
            foreach(FieldInfo field in typeof(UIManagerScript).GetFields(BindingFlags.Instance|BindingFlags.Public))
            {
                if(field.FieldType!=typeof(GameObject) || !field.Name.StartsWith("fondo",StringComparison.Ordinal))continue;
                GameObject source=field.GetValue(manager) as GameObject;
                Image image=source==null?null:source.GetComponent<Image>();
                if(image==null || bindings.Exists(x=>x.Image==image))continue;
                Material original=image.material;
                if(original==null || original.shader==null || original.shader.name!="UI/Default")
                {Debug.LogError("Original backdrop requires its UI/Default material: "+source.name);continue;}
                var local=new Material(original){shader=shader,name="Backdrop depth / "+source.name,hideFlags=HideFlags.HideAndDontSave};
                image.material=local;
                bindings.Add(new Binding(image,original,local));
            }
    }
    static void Clear()
    {
        foreach(Binding binding in bindings)
        {
            if(binding.Image!=null && binding.Image.material==binding.BackgroundMaterial)binding.Image.material=binding.OriginalMaterial;
            if(binding.BackgroundMaterial!=null)
            {
                if(Application.isPlaying)UnityEngine.Object.Destroy(binding.BackgroundMaterial);
                else UnityEngine.Object.DestroyImmediate(binding.BackgroundMaterial);
            }
        }
        bindings.Clear();sceneHandle=-1;
    }
}
