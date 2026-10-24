from pathlib import Path
import json,hashlib,datetime
batch=Path(__file__).resolve().parent;project=batch.parents[1]
helper=Path('Assets/Scripts/RecoveryUI/RecoveredOriginalUIAnimation.cs')
source=(project/helper).read_text(encoding='utf-8-sig')
needle='if (original == null || original.GetType().FullName != "Game.EnableObjectsWithDelay.EnableObjectsWithDelay") continue;'
assert source.count(needle)==2
source=source.replace(needle,'if (original == null || !original.isActiveAndEnabled || original.GetType().FullName != "Game.EnableObjectsWithDelay.EnableObjectsWithDelay") continue;')
source=source.replace('''        // Source scenes assign Star_B(.2); the other two entries are null.''','''        // Source scenes assign Star_B(.2), but its StarCollection_B1 branch
        // is authored inactive. Native Start/Update never run for that branch
        // unless it is explicitly enabled. Visible stars use their own star3
        // controllers, not this inactive alternate effect.''')
target=batch/helper;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(source,encoding='utf-8')
editor=Path('Assets/Editor/RecoveredTools/FlowWaterStarAnimationValidation.cs')
source=(project/'Validation/NativeUIStarFixture1'/editor).read_text(encoding='utf-8-sig')
source=source.replace('''CheckState(id + ":serialized-star-delay-target", driver != null && driver.StarTargetCount == 1, "Original list contains one assigned Star_B(.2s), two null slots");''','''CheckState(id + ":inactive-alternate-star-delay-not-run", driver != null && driver.StarTargetCount == 0 && driver.StarsEnabledCount == 0,
                    "Original StarCollection_B1 is inactive; native Start does not run there");
                var visibleStars = panel.GetComponentsInChildren<Animator>(true).Where(x => x.gameObject.name.StartsWith("sta3_", StringComparison.Ordinal)).ToArray();
                var visibleTimes = visibleStars.Select(x => x.GetCurrentAnimatorStateInfo(0).normalizedTime).ToArray();
                var visibleScales = visibleStars.Select(x => x.transform.localScale).ToArray();''')
old='''                CheckState(id + ":original-star-b-enabled-after-delay", driver != null && driver.StarsEnabledCount == 1 && starB != null && starB.gameObject.activeInHierarchy && starB.enabled,
                    "Only the original configured target is activated");'''
new='''                var alternate = panel.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "StarCollection_B1");
                CheckState(id + ":authored-alternate-b-remains-inactive", alternate != null && !alternate.gameObject.activeSelf && starB != null && !starB.gameObject.activeInHierarchy,
                    "No hidden original branch is activated as a fabricated victory effect");
                CheckState(id + ":three-visible-star-controllers-advance", visibleStars.Length == 3 && visibleStars.Select((x, i) =>
                    x.enabled && x.gameObject.activeInHierarchy && x.GetCurrentAnimatorStateInfo(0).normalizedTime > visibleTimes[i]).All(x => x),
                    "Actual stars/sta3_1..3 original star3.controller time advances");
                CheckState(id + ":visible-star-scale-curves-render", visibleStars.Select((x, i) => (x.transform.localScale - visibleScales[i]).sqrMagnitude > .000001f).Any(x => x),
                    "At least one actual visible star transform changes across captured scaled time");'''
assert old in source;source=source.replace(old,new)
start=source.index('''                float bTime = starB == null ? 0 : starB.GetCurrentAnimatorStateInfo(0).normalizedTime;''')
end=source.index('''            }
        }
        static void CheckState''',start)
replacement='''                Capture(id + "-complete-stars.png");
                if (driver != null) driver.PlayConfiguredStarEnd();
                CheckState(id + ":baseline-e-keeps-inactive-b-hidden", alternate != null && !alternate.gameObject.activeSelf,
                    "Original inactive Delay.Update does not route E to the alternate B effect");

                // Explicit test-only activation: validate the original assigned
                // B branch independently, without calling it the gameplay star.
                if (id == 1 && alternate != null)
                {
                    bool oldActive = alternate.gameObject.activeSelf;
                    alternate.gameObject.SetActive(true);
                    driver = RecoveredOriginalUIAnimation.ShowComplete(panel);
                    CheckState("isolated-b:configured-delay-start", driver.StarTargetCount == 1 && starB != null && !starB.gameObject.activeSelf,
                        "Controlled original branch activation only; target disabled before native .2s delay");
                    settle = Time.time + .3f;
                    while (Time.time < settle) yield return null;
                    CheckState("isolated-b:delay-enables-authored-target", driver.StarsEnabledCount == 1 && starB != null && starB.enabled && starB.gameObject.activeInHierarchy,
                        "Original .2scaled-second activation completed");
                    float bTime = starB == null ? 0 : starB.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    settle = Time.time + .2f;
                    while (Time.time < settle) yield return null;
                    CheckState("isolated-b:controller-time-advances", starB != null && starB.GetCurrentAnimatorStateInfo(0).normalizedTime > bTime,
                        "Original B default controller advances; orphan curves remain untouched");
                    driver.PlayConfiguredStarEnd();
                    CheckState("isolated-b:original-e-star-end-state", starB != null && starB.GetCurrentAnimatorStateInfo(0).IsName("Star_End"),
                        "Same assigned target list as native E-key PlayStarEndAnimationForAll106834C");
                    Capture("isolated-original-b-effect.png");
                    alternate.gameObject.SetActive(oldActive);
                }
'''
source=source[:start]+replacement+source[end:]
target=batch/editor;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(source,encoding='utf-8')
runtime=(project/'Validation/ui-current-runtime.rsp').read_text(encoding='utf-8')
runtime=runtime.replace('-out:"Validation/ui-current-runtime.dll"','-out:"Validation/NativeUIStarFixture2/runtime.dll"')
runtime=runtime.replace('"'+helper.as_posix()+'"','"'+(batch/helper).relative_to(project).as_posix()+'"')
(batch/'runtime.rsp').write_text(runtime,encoding='utf-8')
edit=(project/'Validation/ui-current-editor.rsp').read_text(encoding='utf-8')
edit=edit.replace('-out:"Validation/editor-compile-check.dll"','-out:"Validation/NativeUIStarFixture2/editor.dll"').replace('Validation/ui-current-runtime.dll','Validation/NativeUIStarFixture2/runtime.dll')
if 'FlowWaterStarAnimationValidation.cs' in edit:edit=edit.replace('"'+editor.as_posix()+'"','"'+(batch/editor).relative_to(project).as_posix()+'"')
else:edit+='\n"'+(batch/editor).relative_to(project).as_posix()+'"\n'
(batch/'editor.rsp').write_text(edit,encoding='utf-8')
files=[]
for path in [helper,editor]:
    current=project/path;staged=batch/path
    if not current.exists():current=project/'Validation/NativeUIStarFixture1'/path
    files.append({'target':path.as_posix(),'baseSha256':hashlib.sha256(current.read_bytes()).hexdigest(),'staged':staged.relative_to(project).as_posix(),'stagedSha256':hashlib.sha256(staged.read_bytes()).hexdigest()})
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'assetsModified':False,'files':files,
          'preservedMetaGuids':{'RecoveredOriginalUIAnimation.cs':'aa7c8cb594379ee42b4b13088c169560','FlowWaterStarAnimationValidation.cs':'30adb11ee41347e3b36fb69d61260e92'},
          'runtimeCompile':'pending','editorCompile':'pending','testEntry':'Recovered.FlowWater.Editor.FlowWaterStarAnimationValidation.Run',
          'productionFix':'Native Delay Start/Update run only when original component.isActiveAndEnabled. StarCollection_B1 is authored inactive in all4scenes; actual visible stars use sta3_1..3 parent star3 controllers, keeping D Animator disabled.',
          'fixture':'Natural4scene baseline validates root curves and actual3star controllers; separate explicitly activated B case retains .2s/E state tests without misrepresenting unused B as gameplay.',
          'evidence':'VisualReview/original-ui-animation-structure.json now resolves original target paths; all4scene StarCollection_B1 m_IsActive0; original Start/Update/WaitForSeconds asm unchanged.'}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(files)
