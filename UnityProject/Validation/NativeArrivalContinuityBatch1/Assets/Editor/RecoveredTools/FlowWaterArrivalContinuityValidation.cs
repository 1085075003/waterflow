#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterArrivalContinuityValidation
    {
        [Serializable] sealed class Result { public string unityVersion; public bool passed; public float outletRadius, inletRadius; public List<string> checks = new List<string>(); }
        [MenuItem("Tools/Flow Water/Validate Native Arrival Contact Continuity")]
        public static void Run()
        {
            foreach (SceneSetup setup in EditorSceneManager.GetSceneManagerSetup())
                if (SceneManager.GetSceneByPath(setup.path).isDirty) throw new InvalidOperationException("Save edited scenes before validation");
            Scene originalScene = SceneManager.GetActiveScene();
            Scene fixtureScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(fixtureScene);
            var result = new Result { unityVersion = Application.unityVersion };
            GameObject outlet = null, inlet = null;
            try
            {
                outlet = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/chorroCascadaMech.prefab"));
                var jet = outlet.GetComponentInChildren<CascadaTestE>(true);
                SphereCollider source = jet.esferaSalida1.GetComponent<SphereCollider>();
                outlet.transform.position = new Vector3(40, 4.95f, 40); outlet.transform.rotation = Quaternion.identity; outlet.transform.localScale = Vector3.one;
                source.transform.localPosition = new Vector3(0, 0, 40);
                ControlPieza piece = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Recta6j_4.prefab").GetComponentInChildren<ControlPieza>(true);
                inlet = UnityEngine.Object.Instantiate(piece.prefabAguaControl);
                inlet.transform.rotation = Quaternion.identity; inlet.transform.localScale = Vector3.one;
                SphereCollider target = null;
                foreach (FuenteTest1 origin in inlet.GetComponentsInChildren<FuenteTest1>(true))
                    if (origin.origen == 5) { target = origin.GetComponent<SphereCollider>(); break; }
                if (source == null || target == null) throw new InvalidOperationException("Original E output or CP center intake collider missing");
                for (Transform t = source.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
                for (Transform t = target.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
                source.enabled = target.enabled = true;
                result.outletRadius = source.radius * MaxAbs(source.transform.lossyScale);
                result.inletRadius = target.radius * MaxAbs(target.transform.lossyScale);
                if (Mathf.Abs(result.outletRadius - 1.5f) > .0001f || Mathf.Abs(result.inletRadius - .4f) > .0001f) throw new InvalidOperationException("Original authored radii differ from evidence");
                foreach (MonoBehaviour b in outlet.GetComponentsInChildren<MonoBehaviour>(true)) if (b != null) b.enabled = false;
                foreach (MonoBehaviour b in inlet.GetComponentsInChildren<MonoBehaviour>(true)) if (b != null) b.enabled = false;
                Vector3 before = new Vector3(39.99817f, 4.44134474f, 80.02793f), after = new Vector3(40, 3.95f, 80);
                for (int i = 0; i <= 24; i++)
                {
                    inlet.transform.position = Vector3.Lerp(before, after, i / 24f);
                    Physics.SyncTransforms(); Vector3 direction; float depth;
                    bool overlap = Physics.ComputePenetration(source, source.transform.position, source.transform.rotation, target, target.transform.position, target.transform.rotation, out direction, out depth);
                    if (!overlap) throw new InvalidOperationException("Original spheres lost contact at actual landing sample " + i);
                }
                result.checks.Add("25 actual original-sphere samples continuously overlap between captured gm1660 pre-snap and final pose");
                inlet.transform.position = new Vector3(40, 9, 80); Physics.SyncTransforms(); Vector3 normal; float distance;
                if (Physics.ComputePenetration(source, source.transform.position, source.transform.rotation, target, target.transform.position, target.transform.rotation, out normal, out distance)) throw new InvalidOperationException("Separated original input must not overlap");
                result.checks.Add("Original geometric disconnect negative control remains separated"); result.passed = true;
            }
            finally
            {
                if (outlet != null) UnityEngine.Object.DestroyImmediate(outlet); if (inlet != null) UnityEngine.Object.DestroyImmediate(inlet);
                EditorSceneManager.CloseScene(fixtureScene, true); SceneManager.SetActiveScene(originalScene);
                string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "WaterArrivalContinuity"); Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "geometry-validation.json"), JsonUtility.ToJson(result, true));
            }
            MethodInfo begin = typeof(FlowWaterSave1GameplayValidation).GetMethod("Begin", BindingFlags.Static | BindingFlags.NonPublic);
            ParameterInfo[] parameters = begin.GetParameters(); object[] arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) arguments[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : Type.Missing;
            arguments[0] = new[] {1660,1696,1656}; arguments[1] = true; begin.Invoke(null, arguments);
        }
        static float MaxAbs(Vector3 v) { return Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)); }
    }
}
#endif
