"""Stage a real authored-level Animator state/motion fixture; no Assets writes."""
import pathlib,re
project=pathlib.Path.cwd();source=(project/'Validation/NativeCameraTutorialBatch2/Assets/Editor/RecoveredTools/FlowWaterCameraTutorialValidation.cs').read_text(encoding='utf-8')
source=source.replace('FlowWaterCameraTutorialValidation','FlowWaterFountainAnimationValidation').replace('CameraTutorialValidation','FountainAnimationValidation').replace('"CameraTutorial"','"FountainAnimation"').replace('camera-tutorial-validation.json','fountain-animation-validation.json').replace('Validate Original Camera and Rotation Tutorial','Validate Original Fountain Animator Recipes')
source=source.replace('"Recovery.Complete.401", "Recovery.Completed.gm401"','"Recovery.Complete.1101", "Recovery.Completed.gm1101", "Recovery.Complete.1193", "Recovery.Completed.gm1193"')
source=source.replace('using UnityEditor;','using UnityEditor;\nusing UnityEditor.Animations;')
start=source.index('        static IEnumerator Exercise()');end=source.index('        static HelpRotate FindHelp()',start)
exercise='''        static IEnumerator Exercise()
        {
            Require("unity-2022.3.28f1", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
            while (FlowWaterRecoveryUI.Instance == null) yield return null;
            foreach (int level in new[] { 1101, 1193 })
            {
                FlowWaterRecoveryUI.Instance.StartGame(level);
                while (RecoveredFlowGame.Instance == null || RecoveredFlowGame.Instance.CurrentLevelId != level || RecoveredFlowGame.Instance.PieceRoot == null) yield return null;
                RecoveredFlowGame game = RecoveredFlowGame.Instance;
                game.Solve(); RecoveredWaterNetwork.Instance.Rebuild();
                float until = Time.time + 180;
                while (RecoveredWaterNetwork.Instance.HasPendingPropagation && Time.time < until) yield return null;
                Require("gm" + level + "-actual-flow-settled", !RecoveredWaterNetwork.Instance.HasPendingPropagation &&
                    RecoveredWaterNetwork.Instance.ActiveFountainCount >= RecoveredWaterNetwork.Instance.RequiredFountainCount && RecoveredWaterNetwork.Instance.RequiredFountainCount > 0,
                    "actual original source to all targets before inspecting generated fountain");
                string code = level == 1101 ? "3" : "2";
                RecoveredNativeFountainAnimation[] bindings = game.PieceRoot.GetComponentsInChildren<RecoveredNativeFountainAnimation>(true)
                    .Where(b => b.NativeConfiguration == code && b.gameObject.activeInHierarchy).ToArray();
                Require("gm" + level + "-native-generated-animation-owner", bindings.Length > 0, "actual production recipe owners with configAnim=" + code + "; count=" + bindings.Length);
                bool correctParameter = false, reachedState = false, moving = false;
                var samples = new Dictionary<RecoveredNativeFountainAnimation, string>();
                until = Time.time + 8;
                while (Time.time < until && (!correctParameter || !reachedState || !moving))
                {
                    foreach (RecoveredNativeFountainAnimation binding in bindings)
                    {
                        if (binding == null) continue;
                        Animator animator = binding.GetComponent<Animator>();
                        if (animator == null || !animator.enabled) continue;
                        correctParameter |= animator.GetInteger("animcode") == Int32.Parse(code);
                        var controller = animator.runtimeAnimatorController as AnimatorController;
                        if (controller != null)
                        {
                            foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
                                foreach (AnimatorStateTransition transition in state.state.transitions)
                                    if (transition.destinationState != null && transition.conditions.Any(c => c.parameter == "animcode" && c.mode == AnimatorConditionMode.Equals && c.threshold == Int32.Parse(code)))
                                        reachedState |= animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(transition.destinationState.name);
                        }
                        string sample = MotionSignature(binding.transform);
                        string old;
                        if (samples.TryGetValue(binding, out old)) moving |= old != sample;
                        else samples[binding] = sample;
                    }
                    yield return null;
                }
                Require("gm" + level + "-native-integer-survives-rebind", correctParameter, "original animcode=" + code);
                Require("gm" + level + "-actual-controller-transition", reachedState, "actual current state matches original animcode transition destination");
                Require("gm" + level + "-actual-fountain-motion", moving, "actual native instance transforms/particle velocity changed over real Unity frames; no injected Animator.Update");
                RecoveredWaterNetwork.Instance.Rebuild(); yield return null;
                Require("gm" + level + "-unrelated-rebuild-keeps-animation-code", bindings.Any(b => b != null && b.GetComponent<Animator>().GetInteger("animcode") == Int32.Parse(code)), "native parameter remains after production unrelated graph rebuild");
            }
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
'''
source=source[:start]+exercise+source[end:]
out=project/'Validation/FountainAnimationWork/Assets/Editor/RecoveredTools/FlowWaterFountainAnimationValidation.cs';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(source,encoding='utf-8')
print('Staged original1101/1193 flow + integer/state/real motion fixture')
