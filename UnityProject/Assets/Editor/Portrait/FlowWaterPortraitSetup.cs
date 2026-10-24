#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterPortraitSetup
    {
        [Serializable] sealed class PreviewSize { public int width=720, height=1280; }
        static string Root { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Validation/Portrait"); } }
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Configure the portrait preview from Edit mode.");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait=true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;
            PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.defaultScreenWidth=720;
            PlayerSettings.defaultScreenHeight=1280;
            PlayerSettings.defaultWebScreenWidth=720;
            PlayerSettings.defaultWebScreenHeight=1280;
            PlayerSettings.defaultIsNativeResolution=false;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            IncludeBackdropShader();
            AssetDatabase.SaveAssets();
            SetPreview();
        }
        public static void Capture() { SetPreview(); }
        static void IncludeBackdropShader()
        {
            Shader shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/FlowWater_PortraitBackdrop.shader");
            if(shader==null) throw new InvalidOperationException("The portrait backdrop shader must be imported before applying setup.");
            var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if(settings.Length==0) throw new InvalidOperationException("Graphics settings could not be loaded.");
            var serialized=new SerializedObject(settings[0]);
            var shaders=serialized.FindProperty("m_AlwaysIncludedShaders");
            for(int i=0;i<shaders.arraySize;i++) if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader) return;
            int index=shaders.arraySize;
            shaders.InsertArrayElementAtIndex(index);
            shaders.GetArrayElementAtIndex(index).objectReferenceValue=shader;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void Export()
        {
            Directory.CreateDirectory(Root);
            Type sizes=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes",true);
            Type view=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView",true);
            File.WriteAllText(Path.Combine(Root,"gameview-api.txt"),string.Join("\n",sizes.GetMembers(Flags).Select(x=>x.ToString()))+"\nGAMEVIEW\n"+string.Join("\n",view.GetMembers(Flags).Where(x=>x.Name.IndexOf("Size",StringComparison.OrdinalIgnoreCase)>=0).Select(x=>x.ToString())));
        }
        static void SetPreview()
        {
            Directory.CreateDirectory(Root);
            string path=Path.Combine(Root,"preview-size.json");
            PreviewSize size=File.Exists(path)?JsonUtility.FromJson<PreviewSize>(File.ReadAllText(path)):new PreviewSize();
            if(size==null || size.width<320 || size.width>2000 || size.height<size.width || size.height>3000) throw new InvalidDataException("Portrait size must be 320..2000 wide, no wider than its height, and <=3000 high.");
            Assembly editor=typeof(UnityEditor.Editor).Assembly;
            Type sizes=editor.GetType("UnityEditor.GameViewSizes",true);
            Type singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizes);
            object instance=singleton.GetProperty("instance",Flags).GetValue(null,null);
            MethodInfo getGroup=sizes.GetMethod("GetGroup",Flags);
            object group=getGroup.Invoke(instance,new[]{Enum.Parse(getGroup.GetParameters()[0].ParameterType,"Standalone")});
            Type groupType=group.GetType();
            int builtins=(int)groupType.GetMethod("GetBuiltinCount",Flags).Invoke(group,null);
            int custom=(int)groupType.GetMethod("GetCustomCount",Flags).Invoke(group,null);
            string name="Flow Water Portrait "+size.width+"x"+size.height;
            int chosen=-1;
            MethodInfo getSize=groupType.GetMethod("GetGameViewSize",Flags);
            for(int i=0;i<builtins+custom;i++)
            {
                object item=getSize.Invoke(group,new object[]{i});
                Type type=item.GetType();
                if(Convert.ToInt32(type.GetProperty("width",Flags).GetValue(item,null))==size.width && Convert.ToInt32(type.GetProperty("height",Flags).GetValue(item,null))==size.height) { chosen=i; break; }
            }
            if(chosen<0)
            {
                Type type=editor.GetType("UnityEditor.GameViewSize",true), mode=editor.GetType("UnityEditor.GameViewSizeType",true);
                object item=Activator.CreateInstance(type,Flags,null,new object[]{Enum.Parse(mode,"FixedResolution"),size.width,size.height,name},null);
                groupType.GetMethod("AddCustomSize",Flags).Invoke(group,new[]{item}); chosen=builtins+custom;
            }
            Type viewType=editor.GetType("UnityEditor.GameView",true);
            EditorWindow window=EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex",Flags).SetValue(window,chosen,null);
            window.Focus(); window.Repaint();
            File.WriteAllText(Path.Combine(Root,"preview-selected.json"),JsonUtility.ToJson(size,true));
        }
    }
}
#endif

