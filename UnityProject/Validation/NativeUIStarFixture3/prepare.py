from pathlib import Path
import json,hashlib,datetime
batch=Path(__file__).resolve().parent;project=batch.parents[1]
relative=Path('Assets/Editor/RecoveredTools/FlowWaterStarAnimationValidation.cs')
base=project/'Validation/NativeUIStarFixture2'/relative
text=base.read_text(encoding='utf-8-sig')
text=text.replace('''                var visibleScales = visibleStars.Select(x => x.transform.localScale).ToArray();''','''                // anim_starx2 binds child paths, not the Animator-bearing sta3_N.
                var boundStars = visibleStars.Select(x => x.transform.Find("star")).ToArray();
                var boundImages = visibleStars.Select(x => x.transform.Find("star/star1/st1")).Select(x => x == null ? null : x.GetComponent<UnityEngine.UI.Image>()).ToArray();
                CheckState(id + ":original-star-curve-targets-exist", boundStars.Length == 3 && boundStars.All(x => x != null) && boundImages.All(x => x != null),
                    "anim_starx2 paths star and star/star1/st1; no path remapping");
                var visibleScales = boundStars.Select(x => x == null ? Vector3.zero : x.localScale).ToArray();
                var visibleAlpha = boundImages.Select(x => x == null ? -1f : x.color.a).ToArray();''')
text=text.replace('''                CheckState(id + ":visible-star-scale-curves-render", visibleStars.Select((x, i) => (x.transform.localScale - visibleScales[i]).sqrMagnitude > .000001f).Any(x => x),
                    "At least one actual visible star transform changes across captured scaled time");''','''                CheckState(id + ":visible-star-scale-curves-render", boundStars.Select((x, i) => x != null && (x.localScale - visibleScales[i]).sqrMagnitude > .000001f).Any(x => x),
                    "Original bound child star scales=" + String.Join(";", boundStars.Select((x, i) => visibleScales[i] + "->" + (x == null ? Vector3.zero : x.localScale)).ToArray()));
                CheckState(id + ":visible-star-alpha-curves-render", boundImages.Select((x, i) => x != null && Mathf.Abs(x.color.a - visibleAlpha[i]) > .000001f).Any(x => x),
                    "Original Image alpha star/star1/st1=" + String.Join(";", boundImages.Select((x, i) => visibleAlpha[i] + "->" + (x == null ? -1f : x.color.a)).ToArray()));''')
target=batch/relative;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text,encoding='utf-8')
edit=(project/'Validation/ui-current-editor.rsp').read_text(encoding='utf-8')
edit=edit.replace('-out:"Validation/editor-compile-check.dll"','-out:"Validation/NativeUIStarFixture3/editor.dll"').replace('Validation/ui-current-runtime.dll','Validation/NativeUIStarFixture2/runtime.dll')
if 'FlowWaterStarAnimationValidation.cs' in edit:edit=edit.replace('"'+relative.as_posix()+'"','"'+target.relative_to(project).as_posix()+'"')
else:edit+='\n"'+target.relative_to(project).as_posix()+'"\n'
(batch/'editor.rsp').write_text(edit,encoding='utf-8')
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'assetsModified':False,'files':[{'target':relative.as_posix(),'baseSha256':hashlib.sha256(base.read_bytes()).hexdigest(),'staged':target.relative_to(project).as_posix(),'stagedSha256':hashlib.sha256(target.read_bytes()).hexdigest()}],
          'preservedMetaGuid':'30adb11ee41347e3b36fb69d61260e92','editorCompile':'pending','testEntry':'Recovered.FlowWater.Editor.FlowWaterStarAnimationValidation.Run',
          'fix':'Sample actual original anim_starx2 binding targets: star localScale, star/star1/st1 Image alpha. Animator owner sta3_N scale is not animated. No production/runtime changes; original B isolated tests and all baseline state/time checks retained.',
          'evidence':'Assets/AnimationClip/anim_starx2.anim: scale paths star / star/Star_D; Image alpha paths star/star1/st1..3, original m_StopTime2.05.'}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8');print(manifest['files'])
