from pathlib import Path
import hashlib,json,datetime
project=Path(__file__).resolve().parents[2]
relative=Path('Assets/Scripts/RecoveryUI/FlowWaterRecoveryUI.cs')
original=project/relative
batch=project/'Validation/ImmutableMenuContext'
staged=batch/relative
staged.parent.mkdir(parents=True,exist_ok=True)
text=original.read_text(encoding='utf-8-sig')
needle='''        SelectedTab = Mathf.Clamp(tab, 1, 6);
        PlayerPrefs.SetInt("OptionSelLev", SelectedTab);'''
replacement='''        SelectedTab = Mathf.Clamp(tab, 1, 6);
        // Original selector OnEnable resets parte=0 (103E750), and its
        // Night branch sets subparte=1 (103E7CC). Tab 4 clears subparte
        // again in botonTabDown (1043E5C). These fields drive original
        // menu particle palettes independently of the selected level pack.
        UIManagerSelScript0.parte = 0;
        if (SelectedTab == 6) UIManagerSelScript0.subparte = 1;
        else if (SelectedTab == 4) UIManagerSelScript0.subparte = 0;
        PlayerPrefs.SetInt("OptionSelLev", SelectedTab);'''
assert text.count(needle)==1
staged.write_text(text.replace(needle,replacement),encoding='utf-8')
runtime=(project/'Validation/ui-current-runtime.rsp').read_text(encoding='utf-8')
runtime=runtime.replace('-out:"Validation/ui-current-runtime.dll"','-out:"Validation/ImmutableMenuContext/runtime-context.dll"')
runtime=runtime.replace('"'+relative.as_posix()+'"','"'+staged.relative_to(project).as_posix()+'"')
(batch/'runtime.rsp').write_text(runtime,encoding='utf-8')
editor=(project/'Validation/ui-current-editor.rsp').read_text(encoding='utf-8')
editor=editor.replace('-out:"Validation/editor-compile-check.dll"','-out:"Validation/ImmutableMenuContext/editor-context.dll"')
editor=editor.replace('Validation/ui-current-runtime.dll','Validation/ImmutableMenuContext/runtime-context.dll')
(batch/'editor.rsp').write_text(editor,encoding='utf-8')
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'assetsModified':False,
          'files':[{'target':relative.as_posix(),'baseSha256':hashlib.sha256(original.read_bytes()).hexdigest(),
                    'staged':staged.relative_to(project).as_posix(),'stagedSha256':hashlib.sha256(staged.read_bytes()).hexdigest()}],
          'nativeEvidence':['Validation/NativeAnalysis/Methods/UIManagerSelScript0_OnEnable_103E6E0.asm.txt:103E750/103E7CC',
                            'Validation/NativeAnalysis/Methods/UIManagerSelScript0_botonTabDown_10429D0.asm.txt:1043E5C',
                            'Validation/NativeAnalysis/native-category-context-writes.json'],
          'runtimeCompile':'pending','editorCompile':'pending',
          'verification':['Enter SelLevel0 from a previously played level: parte must reset to0.',
                          'Select Night: subparte1 and original changeColorP night-menu gradient branch.',
                          'Select Mech: subparte0; do not modify SelectedPart/source gm asset mapping.']}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(batch)
