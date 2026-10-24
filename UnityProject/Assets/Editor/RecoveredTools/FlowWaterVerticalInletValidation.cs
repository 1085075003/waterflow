#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Runs inside the existing gm407 play session; leaves its legal receiving move visible.</summary>
    public static class FlowWaterVerticalInletValidation
    {
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Sample
        {
            public string stage, name, parent, activeRenderers, animatorState;
            public int frame, color, incoming;
            public float time, normalizedTime;
            public Vector3 localPosition, localScale, localEulerAngles, worldPosition;
        }
        [Serializable] public sealed class Report
        {
            public string startedUtc, finishedUtc, unityVersion, error, output;
            public bool passed, receivingMoveKept;
            public int level, movedPiece;
            public Vector3 moveFrom, moveTo;
            public List<Check> checks = new List<Check>();
            public List<Sample> samples = new List<Sample>();
        }
        public static Report LastReport { get; private set; }
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "UserReportedIssues"); } }

        public static IEnumerator Run(RecoveredFlowGame game, Action<bool, string> completed = null)
        {
            var report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
                level = game == null ? 0 : game.CurrentLevelId, output = Output, movedPiece = -1 };
            var temporary = new List<GameObject>();
            IEnumerator exercise = Exercise(game, report, temporary);
            try
            {
                while (true)
                {
                    bool more;
                    try { more = exercise.MoveNext(); }
                    catch (Exception ex) { report.error = ex.ToString(); break; }
                    if (!more) break;
                    yield return exercise.Current;
                }
            }
            finally
            {
                (exercise as IDisposable)?.Dispose();
                foreach (GameObject item in temporary) if (item != null) UnityEngine.Object.DestroyImmediate(item);
                report.finishedUtc = DateTime.UtcNow.ToString("o");
                report.passed = String.IsNullOrEmpty(report.error) && report.checks.Count > 0 && report.checks.All(x => x.passed);
                LastReport = report;
                Directory.CreateDirectory(Output);
                File.WriteAllText(Path.Combine(Output, "vertical-inlet-validation.json"), JsonUtility.ToJson(report, true));
                completed?.Invoke(report.passed, report.passed ? "gm407 vertical inlet passed; legal receiving layout retained. " + Output : report.error);
            }
        }

        static IEnumerator Exercise(RecoveredFlowGame game, Report report, List<GameObject> temporary)
        {
            Require(report, "existing-gm407-play-session", Application.isPlaying && game != null && game.CurrentLevelId == 407 && game.IsLevelActive,
                "No scene or level is switched by this helper.");
            Require(report, "running-scaled-clock", Time.timeScale > 0, "Animation samples require an unpaused play session.");
            RecoveryPiece source = game.Pieces.FirstOrDefault(x => x.BasePrefab == "Bloque3FuenteAzul");
            RecoveryPiece receiver = game.Pieces.FirstOrDefault(x => x.Index == 9 && x.BasePrefab == "Recta1j");
            Require(report, "original-source-and-receiver", source != null && receiver != null, "gm407 source and movable X-axis channel index9.");
            var sourceController = source.GetComponentInChildren<ControlPiezaFuente>(true);
            Require(report, "original-east-facing-source", sourceController != null && sourceController.cascada3,
                "Bloque3FuenteAzul.cascada3 sends the fall twenty units east.");
            Vector3 target = source.transform.position + Vector3.right * 20;
            target.y = receiver.InitialPosition.y;
            report.movedPiece = receiver.Index; report.moveFrom = receiver.transform.position; report.moveTo = target;
            bool alreadyThere = (receiver.transform.position - target).sqrMagnitude < .01f;
            Require(report, "legal-receiving-move", alreadyThere || game.TryMove(receiver.Index, target), game.Status + " Target=" + target);
            report.receivingMoveKept = true;
            Transform inlet = null;
            float deadline = Time.realtimeSinceStartup + 12;
            while (inlet == null && Time.realtimeSinceStartup < deadline)
            {
                inlet = receiver.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Original Fuente5Color1" && x.gameObject.activeInHierarchy);
                if (inlet == null) yield return null;
            }
            Require(report, "actual-original-inlet-created", inlet != null, "FT1 Origen5 must create Original Fuente5Color1 beneath the real receiving piece.");
            float animationReady = Time.realtimeSinceStartup + .15f;
            while (Time.realtimeSinceStartup < animationReady) yield return null;
            var origin = inlet.parent.GetComponent<FuenteTest1>();
            Require(report, "actual-ft1-vertical5", origin != null && origin.origen == 5, Hierarchy(inlet));
            RequirePose(report, "actual-gm407", inlet, origin.transform, 5);
            Animator animator = inlet.GetComponentInChildren<Animator>(true);
            Require(report, "actual-animator-enabled", animator != null && animator.enabled && animator.runtimeAnimatorController != null, animator == null ? "missing" : animator.runtimeAnimatorController.name);
            Require(report, "actual-water-motion-driver", inlet.GetComponentInParent<RecoveredWaterMotion>() != null, "Production RecoveredWaterMotion owns the restored inlet.");
            Capture(game.GameCamera, Path.Combine(Output, "gm407-inlet-sample-start.png"));
            for (int i = 0; i < 24; i++)
            {
                Sample sample = Observe(inlet, "actual-gm407", 5, 1); report.samples.Add(sample);
                Require(report, "actual-active-renderer-" + i, !String.IsNullOrEmpty(sample.activeRenderers), sample.activeRenderers);
                float until = Time.realtimeSinceStartup + .047f;
                while (Time.realtimeSinceStartup < until) yield return null;
            }
            var live = report.samples.Where(x => x.stage == "actual-gm407").ToArray();
            Require(report, "actual-rendered-mesh-animation-changes", live.Select(x => x.activeRenderers).Distinct().Count() > 1,
                "Distinct authored active-renderer sets=" + live.Select(x => x.activeRenderers).Distinct().Count());
            Require(report, "actual-animator-time-advances", live.Select(x => x.animatorState + ":" + x.normalizedTime.ToString("F4")).Distinct().Count() > 1,
                "Observed automatic Animator playback across " + (live.Last().time - live.First().time) + " scaled seconds.");
            Capture(game.GameCamera, Path.Combine(Output, "gm407-inlet-sample-end.png"));

            GameObject fixture = new GameObject("Temporary vertical inlet validation"); temporary.Add(fixture);
            fixture.hideFlags = HideFlags.HideAndDontSave; fixture.transform.position = new Vector3(10000, 10000, 10000);
            for (int incoming = 5; incoming <= 6; incoming++)
                for (int color = 1; color <= 4; color++)
                {
                    GameObject water = CloneWater("AguaControl5", fixture.transform, Quaternion.Euler(0, 90, 0));
                    FuenteTest1 input = water.GetComponentsInChildren<FuenteTest1>(true).First(x => x.origen == incoming);
                    var created = new List<GameObject>();
                    RecoveredNativeWaterAssets.RestoreFlow(input, incoming, color, (prefab, parent) => Spawn(prefab, parent, created));
                    string label = "isolated-ft1-" + incoming + "-color" + color;
                    Require(report, label + "-one-original-prefab", created.Count == 1 && created[0].name == "Original Fuente5Color" + color, "created=" + String.Join(",", created.Select(x => x.name)));
                    RequirePose(report, label, created[0].transform, input.transform, incoming);
                    water.AddComponent<RecoveredWaterMotion>().RestoreAuthoredMotion(.3f);
                    Sample sample = Observe(created[0].transform, label, incoming, color); report.samples.Add(sample);
                    Require(report, label + "-active-authored-renderer", !String.IsNullOrEmpty(sample.activeRenderers), sample.activeRenderers);
                    UnityEngine.Object.DestroyImmediate(water);
                }
            GameObject complexWater = CloneWater("AguaControl5C", fixture.transform, Quaternion.identity);
            FuenteTest3 complex = complexWater.GetComponentsInChildren<FuenteTest3>(true).First(x => x.origen == 5);
            var complexCreated = new List<GameObject>();
            RecoveredNativeWaterAssets.RestoreFlow(complex, 5, 1, (prefab, parent) => Spawn(prefab, parent, complexCreated), 10);
            Require(report, "ft3-retains-two-phase-flow", complexCreated.Count == 2, "FT3 vertical inlet keeps its existing A/B recipes; count=" + complexCreated.Count);
            Require(report, "ft3-retains-native-A-rotation", Quaternion.Angle(complexCreated[0].transform.rotation, complex.transform.rotation * Quaternion.Euler(0, 180, 0)) < .01f,
                "FT3 A remains 180 degrees, distinct from FT1's 270-degree inlet.");
            report.samples.AddRange(complexCreated.Select(x => Observe(x.transform, "isolated-ft3-vertical5", 5, 1)));
            UnityEngine.Object.DestroyImmediate(complexWater);
        }

        static GameObject CloneWater(string name, Transform parent, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
            GameObject clone = UnityEngine.Object.Instantiate(prefab, parent, false); clone.transform.rotation = rotation;
            foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            return clone;
        }
        static GameObject Spawn(GameObject prefab, Transform parent, List<GameObject> created)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false); instance.name = "Original " + prefab.name;
            for (Transform t = instance.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            created.Add(instance); return instance;
        }
        static void RequirePose(Report report, string label, Transform inlet, Transform origin, int incoming)
        {
            Quaternion expectedRotation = Quaternion.Euler(0, -90, 0);
            if (incoming == 5) expectedRotation = origin.rotation * expectedRotation;
            Require(report, label + "-native-pose", (inlet.localPosition - new Vector3(8.64f, -1.35f, 0)).sqrMagnitude < .000001f &&
                (inlet.localScale - new Vector3(2.31f, 2.1f, 1.5f)).sqrMagnitude < .000001f &&
                Quaternion.Angle(inlet.rotation, expectedRotation) < .01f,
                "localPosition=" + inlet.localPosition + ", localScale=" + inlet.localScale + ", localEuler=" + inlet.localEulerAngles);
        }
        static Sample Observe(Transform inlet, string stage, int incoming, int color)
        {
            Animator animator = inlet.GetComponentInChildren<Animator>(true);
            AnimatorStateInfo state = animator == null ? default(AnimatorStateInfo) : animator.GetCurrentAnimatorStateInfo(0);
            return new Sample { stage = stage, name = inlet.name, parent = Hierarchy(inlet.parent), frame = Time.frameCount, time = Time.time,
                incoming = incoming, color = color, localPosition = inlet.localPosition, localScale = inlet.localScale,
                localEulerAngles = inlet.localEulerAngles, worldPosition = inlet.position, animatorState = state.fullPathHash.ToString(), normalizedTime = state.normalizedTime,
                activeRenderers = String.Join("|", inlet.GetComponentsInChildren<Renderer>(false).Where(x => x.enabled && x.sharedMaterial != null).Select(x => x.name).OrderBy(x => x)) };
        }
        static string Hierarchy(Transform item) { return item == null ? "" : Hierarchy(item.parent) + "/" + item.name; }
        static void Require(Report report, string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (!passed) throw new InvalidOperationException(name + ": " + detail);
        }
        static void Capture(Camera camera, string path)
        {
            Directory.CreateDirectory(Output);
            RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
            float oldAspect = camera.aspect;
            int width = Mathf.Clamp(camera.pixelWidth, 512, 1920), height = Mathf.Max(1, Mathf.RoundToInt(width / Mathf.Max(.1f, oldAspect)));
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.aspect = oldAspect; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { camera.targetTexture = oldTarget; camera.aspect = oldAspect; RenderTexture.active = oldActive; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
#endif
