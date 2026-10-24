from pathlib import Path
import hashlib,json,datetime
batch=Path(__file__).resolve().parent;project=batch.parents[1]
relative=Path('Assets/Scripts/RecoveryUI/FlowWaterRecoveryUI.cs')
base=project/relative
source=(project/'Validation/ImmutableMenuContext'/relative).read_text(encoding='utf-8-sig')
source=source.replace('''            foreach (var animator in canvas.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;''','''            // Keep each authored Animator.enabled state. Blocking transition
            // fades are handled by StopBlockingFades; modal/button/star curves
            // remain available, while originally disabled Star_D stays disabled.''')
source=source.replace('''        foreach (var controller in SceneComponents<ControladorJuego>(scene))
            SetActive(controller.helpRotate, false);
''','')
source=source.replace('''            case "showSolution": SetActive(originalSolution, true); break;''','''            case "showSolution":
                SetActive(originalSolution, true);
                RecoveredOriginalUIAnimation.PlayState(originalSolution, PlayerPrefs.GetInt("esTablet", 0) == 1 ? "an_complete2d" : "an_complete");
                break;''')
source=source.replace('''        SetActive(originalComplete, completed && !completionDismissed);''','''        bool showComplete = completed && !completionDismissed;
        bool completeWasVisible = originalComplete != null && originalComplete.activeSelf;
        SetActive(originalComplete, showComplete);
        if (showComplete && !completeWasVisible) RecoveredOriginalUIAnimation.ShowComplete(originalComplete);''')
source=source.replace('''                port.panelExit.SetActive(true);
                return;''','''                port.panelExit.SetActive(true);
                RecoveredOriginalUIAnimation.PlayState(port.panelExit, PlayerPrefs.GetInt("esTablet", 0) == 1 ? "an_exit2d" : "an_complete");
                return;''')
source=source.replace('The recovered local interface uses English. Original localization code was not present in the exported project.','Language: English.')
source=source.replace('This local recovery has no connected app-store service.','The app store is unavailable.')
source=source.replace('Ads and purchases are unavailable in this local recovery. No payment is made. All recovered level assets are available through Levels.','Ads and purchases are unavailable.')
source=source.replace('The original level designer was not recovered. Open a recovered level and use Hint to inspect its saved solution.','The level editor is unavailable.')
staged=batch/relative;staged.parent.mkdir(parents=True,exist_ok=True);staged.write_text(source,encoding='utf-8')
runtime=(project/'Validation/ui-current-runtime.rsp').read_text(encoding='utf-8')
runtime=runtime.replace('-out:"Validation/ui-current-runtime.dll"','-out:"Validation/NativeUIAnimations1/runtime-ui.dll"')
runtime=runtime.replace('"'+relative.as_posix()+'"','"'+staged.relative_to(project).as_posix()+'"')
runtime+='\n"Validation/NativeUIAnimations1/Assets/Scripts/RecoveryUI/RecoveredOriginalUIAnimation.cs"\n'
(batch/'runtime.rsp').write_text(runtime,encoding='utf-8')
editor=(project/'Validation/ui-current-editor.rsp').read_text(encoding='utf-8')
editor=editor.replace('-out:"Validation/editor-compile-check.dll"','-out:"Validation/NativeUIAnimations1/editor-ui.dll"').replace('Validation/ui-current-runtime.dll','Validation/NativeUIAnimations1/runtime-ui.dll')
(batch/'editor.rsp').write_text(editor,encoding='utf-8')
files=[]
for name in [relative,Path('Assets/Scripts/RecoveryUI/RecoveredOriginalUIAnimation.cs')]:
    target=project/name;proposal=batch/name
    files.append({'target':name.as_posix(),'baseSha256':hashlib.sha256(target.read_bytes()).hexdigest() if target.exists() else None,
                  'staged':proposal.relative_to(project).as_posix(),'stagedSha256':hashlib.sha256(proposal.read_bytes()).hexdigest()})
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'assetsModified':False,'supersedes':'ImmutableMenuContext','files':files,
          'scope':['Composite menu context + authored UI Animator states + native complete/exit/solution animation entry and assigned Star_B delay + concise product notices + removal of forced helpRotate hiding; core camera ConfigureLevel hook remains root-owned.'],
          'evidence':['Methods/ControladorJuego_mostrarComplete_1074F78.asm.txt','Methods/Game.EnableObjectsWithDelay.EnableObjectsWithDelay_Start_1068038.asm.txt','Methods/Game.EnableObjectsWithDelay.EnableObjectsWithDelay._EnableObjectAfterDelay_d__4_MoveNext_1068608.asm.txt','Methods/UIManagerPortScript_botonExitDown_103789C.asm.txt','VisualReview/original-ui-animation-structure.json'],
          'runtimeCompile':'pending','editorCompile':'pending',
          'limitations':['Original orphan curve paths are preserved. No fabricated D stars or remapped orphan paths. Native completeOff root animation exit remains a separate controller-transition audit.']}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(batch)
