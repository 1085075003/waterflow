from pathlib import Path
import difflib,hashlib,json,re
root=Path('UnityProject');out=root/'Validation/ArtUpgrade'
source=root/'Assets/Scripts/RecoveryUI/FlowWaterRecoveryUI.cs'
before=out/'Before/UI/Runtime/FlowWaterRecoveryUI.cs'
prior=out/'Before/UI/Runtime/FlowWaterRecoveryUI.before-title-binding.cs'
text=source.read_text(encoding='utf-8-sig');old=before.read_text(encoding='utf-8-sig');previous=prior.read_text(encoding='utf-8-sig')
def method(s,name):
 m=re.search(r'    (?:void|IEnumerator) '+name+r'\([^\n]*\)\n    \{',s)
 if not m:raise RuntimeError('Missing '+name)
 start=m.start();opening=s.index('{',m.start());depth=0
 for i in range(opening,len(s)):
  if s[i]=='{':depth+=1
  elif s[i]=='}':
   depth-=1
   if not depth:return s[start:i+1]
 raise RuntimeError('Unclosed '+name)
def strip_display(s,names):
 for name in names:s=s.replace(method(s,name),'')
 return re.sub(r'\s+',' ',s).strip()
title_block='''        string levelName = RecoveredNativeLocalization.Text("Leveltexto2$$ObtenerTexto",RecoveredNativeLocalization.Language) + " " + ((game.CurrentLevelId - 1) % 50 + 1);
        foreach (Text label in SceneComponents<Text>(SceneManager.GetActiveScene()))
        {
            for (Transform parent = label.transform.parent; parent != null; parent = parent.parent)
                if (parent.name == "Level") { label.text = levelName; break; }
        }'''
expected_refresh=method(previous,'RefreshGame').replace(title_block,'        RefreshLevelTitle();')
expected_finalize=method(previous,'FinalizeScene').replace('            StopBlockingFades(scene);','            StopBlockingFades(scene);\n            if (OriginalScenePresentation.IsGameplayScene(scene)) RefreshLevelTitle();')
expected_helper='''    void RefreshLevelTitle()
    {
        var game = RecoveredFlowGame.Instance;
        if (game == null) return;
'''+title_block+'\n    }'
binding={
 'RefreshGameOutsideTitleBlockByteEquivalent':expected_refresh==method(text,'RefreshGame'),
 'usesExistingOneFrameFinalizerOnly':expected_finalize==method(text,'FinalizeScene'),
 'helperEqualsOriginalTitleBlockWithReadOnlyGameReference':expected_helper==method(text,'RefreshLevelTitle'),
 'outsideDisplayMethodsUnchanged':strip_display(previous,['RefreshGame','FinalizeScene'])==strip_display(text,['RefreshGame','FinalizeScene','RefreshLevelTitle'])
}
assert all(binding.values()),binding
for filename,oldtext in [('UI-runtime-visual-only.diff',old),('UI-title-display-binding.diff',previous)]:
 (out/filename).write_text(''.join(difflib.unified_diff(oldtext.splitlines(True),text.splitlines(True),fromfile=str(before if oldtext==old else prior),tofile=str(source))),encoding='utf8')
manifest=json.loads((out/'gameplay-code-before.json').read_text(encoding='utf-8-sig'))
changed=[e['Path'] for e in manifest if Path(e['Path']).exists() and hashlib.sha256(Path(e['Path']).read_bytes()).hexdigest().upper()!=e['Hash']]
report={'changedRuntimeFiles':changed,'otherRuntimeFilesUnchanged':all(Path(p).name=='FlowWaterRecoveryUI.cs' for p in changed),'runtimeVisualDiff':'UI-runtime-visual-only.diff','titleBindingDiff':'UI-title-display-binding.diff','beforeBackup':str(before.relative_to(out)),'preTitleBindingBackup':str(prior.relative_to(out)),'beforeSha256':hashlib.sha256(before.read_bytes()).hexdigest(),'afterSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'visualOnlyEdits':['BuildOverlay font resource','BuildDialog backdrop and card colors','Panel factory sprite/type','MakeText font color','MakeButton surface and hover colors'],'displayBindingEdits':['Extract original Level Text assignment into RefreshLevelTitle()','RefreshGame calls extracted helper at the same location','Existing FinalizeScene after its existing yield null calls only the title helper for gameplay scenes'],'titleBindingStaticAudit':binding}
(out/'UI-runtime-change-audit.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf8')
print(json.dumps(report,indent=2,ensure_ascii=False))
