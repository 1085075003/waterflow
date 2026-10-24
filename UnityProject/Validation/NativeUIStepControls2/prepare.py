from pathlib import Path
import hashlib,json,datetime
batch=Path(__file__).resolve().parent;project=batch.parents[1]
relative=Path('Assets/Scripts/RecoveryUI/FlowWaterRecoveryUI.cs')
base=project/'Validation/NativeUIAnimations1'/relative
text=base.read_text(encoding='utf-8-sig')
text=text.replace('''    bool finalStepPreview;''','''    bool finalStepPreview;
    bool automaticStepPreview;
    float stepExitAvailableAt;''')
text=text.replace('''        game.EnterSolutionPreview(steps && stepPreview);
        SetSolutionControls(true, stepPreview);''','''        game.EnterSolutionPreview(steps && stepPreview);
        automaticStepPreview = steps && stepPreview;
        stepExitAvailableAt = Time.time + 6f;
        SetSolutionControls(true, stepPreview);
        RefreshStepPreviewControls();''')
text=text.replace('''        stepPreview = false;
        SetSolutionControls(false, false);''','''        stepPreview = false;
        automaticStepPreview = false;
        SetSolutionControls(false, false);
        RefreshStepPreviewControls();''')
text=text.replace('''    void RefreshGame()
''','''    void RefreshStepPreviewControls()
    {
        var game = RecoveredFlowGame.Instance;
        bool active = automaticStepPreview && game != null && game.IsSolutionPreview;
        foreach (var loader in SceneComponents<SaveLoadManager>(SceneManager.GetActiveScene()))
        {
            Button steps = loader.stepsButton == null ? null : loader.stepsButton.GetComponent<Button>();
            Button back = loader.backButton1 == null ? null : loader.backButton1.GetComponent<Button>();
            if (steps != null) steps.interactable = !active;
            if (back != null) back.interactable = !active || Time.time >= stepExitAvailableAt;
            Image arrow = loader.back1 == null ? null : loader.back1.GetComponent<Image>();
            if (arrow != null)
            {
                Color color = arrow.color;
                color.a = active && Time.time < stepExitAvailableAt ? .43921568989753723f : 1f;
                arrow.color = color;
            }
        }
    }

    void RefreshGame()
''')
text=text.replace('''    void Update()
    {
        bool escape''','''    void Update()
    {
        if (automaticStepPreview) RefreshStepPreviewControls();
        bool escape''')
target=batch/relative;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text,encoding='utf-8')
runtime=(project/'Validation/ui-current-runtime.rsp').read_text(encoding='utf-8').replace('-out:"Validation/ui-current-runtime.dll"','-out:"Validation/NativeUIStepControls2/runtime.dll"')
runtime=runtime.replace('"'+relative.as_posix()+'"','"'+target.relative_to(project).as_posix()+'"')
if 'RecoveredOriginalUIAnimation.cs' not in runtime:runtime+='\n"Validation/NativeUIAnimations1/Assets/Scripts/RecoveryUI/RecoveredOriginalUIAnimation.cs"\n'
(batch/'runtime.rsp').write_text(runtime,encoding='utf-8')
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'assetsModified':False,'files':[{'target':relative.as_posix(),'baseSha256':hashlib.sha256(base.read_bytes()).hexdigest(),'staged':target.relative_to(project).as_posix(),'stagedSha256':hashlib.sha256(target.read_bytes()).hexdigest()}],
          'runtimeCompile':'pending','scope':'Native okSteps disables steps for automatic playback; backButton1 disabled for6scaled seconds then enabled; back1 alpha0.43921568989753723 (original0x6001A0)->1. Final animBack remains native core-owned.',
          'nativeEvidence':['SaveLoadManager.okSteps1020E98:doMoveBloque2 after2s/backbutton2 after6s','SaveLoadManager.backbutton2 sets backButton1.interactable true and back1 alpha1'],
          'testEntry':'Recovered.FlowWater.Editor.FlowWaterStepPlaybackValidation.Run (already core3casesPASS; adds actual UI button state validation separately)'}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(manifest['files'])
