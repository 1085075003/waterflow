"""Read-only residual animation reachability and CRC calibration; never writes Assets."""
import pathlib, sys, json, re, zlib, collections

base = pathlib.Path('Validation/NativeAnalysis')
sys.path.insert(0, str(pathlib.Path('Validation/ShaderAnalysis/python').resolve()))
import UnityPy

env = UnityPy.load('../raw/base/assets/bin/Data/data.unity3d')
calibration = []
for obj in env.objects:
    if obj.type.name != 'AnimationClip':
        continue
    t = obj.read_typetree()
    if t.get('m_Name') != 'water5':
        continue
    bindings = t['m_ClipBindingConstant']['genericBindings']
    for path in ['water3/w18', 'water3/w19', 'water3/w20', 'water3/w21', 'water3/w22']:
        crc = zlib.crc32(path.encode()) & 0xffffffff
        matches = [b for b in bindings if b['path'] == crc]
        calibration.append({'file': obj.assets_file.name, 'pathID': obj.path_id,
                            'clip': 'water5', 'exportedPath': path,
                            'crc32': crc, 'crc32Hex': f'0x{crc:08X}',
                            'rawMatchingBindings': matches, 'pass': bool(matches)})

audit = json.loads((base/'animation-path-crc-audit.json').read_text(encoding='utf-8'))
contexts = json.loads((base/'animation-original-tree-crc-audit.json').read_text(encoding='utf-8'))['referencedContexts']
states = json.loads((base/'animation-original-state-reachability.json').read_text(encoding='utf-8'))
calls = json.loads((base/'animation-original-explicit-play-audit.json').read_text(encoding='utf-8'))['nativeAnimatorPlayCallsites']
call_strings = {
 'SaveLoadManager$$showRateUs': ['an_complete', 'an_complete2d'],
 'SaveLoadManager$$showSolution': ['an_complete', 'an_complete2d'],
 'SaveLoadManager$$botonCompleteOff': ['an_completeOff'],
 'UIManagerPortScript$$botonExitDown': ['an_complete', 'an_exit2d'],
 'UIManagerSelScript0$$botonPartDown': ['an_unlock', 'an_unlock2d'],
 'UIManagerSelScript0$$CheckMostrarPopupSpinBall': ['an_sb'],
 'UIManagerOptScript$$botonResetDown': ['an_resetpopup', 'an_resetpopup2d'],
 'UIManagerOptScript$$botonResetOk': ['an_ResetOk', 'an_ResetOk2d'],
 'Game.EnableObjectsWithDelay.EnableObjectsWithDelay$$PlayStarEndAnimationForAll': ['Star_End'],
 'ControladorJuego$$mostrarComplete': ['an_complete', 'an_complete2d'],
}
for call in calls:
    call['stateStrings'] = call_strings.get(call['caller'], [])
    call['stateCrc32'] = {s: f'0x{zlib.crc32(s.encode())&0xffffffff:08X}' for s in call['stateStrings']}

classification = {
 'agua10': ('old-water-unreachable', 'Fuente4Azul(Clone) default agua15; no incoming transition or explicit Play to agua10.'),
 'agua11': ('old-water-unreachable', 'Fuente7_1 default aguachorro12 then aguachorro13; agua11 has no incoming transition or explicit Play.'),
 'agua13_0': ('old-water-unreachable', 'Fuente4Azul(Clone) old agua13 state has no incoming transition or explicit Play.'),
 'agua14': ('old-water-unreachable', 'Fuente4Azul(Clone) old agua14 state has no incoming transition or explicit Play.'),
 'anim3': ('old-water-unreachable', 'FuentePrueba2 default anim4 then anim5; anim3 has no incoming transition or explicit Play.'),
 'an_brillo': ('old-ui-unreachable-in-audited-entrypoints', 'Complete default an_complete; only an_complete/an_completeOff/an_complete2d are explicit Play targets. an_brillo has no transition.'),
 'Pressed': ('reachable-partial-orphan-binding', 'SF Button AnyState Pressed trigger reaches this state. Background color and Z curves resolve; only one separate Z curve has an absent old child hash.'),
 'Star_B_Anim_Start': ('reachable-default-unresolved', 'Star_B_Controoller default Star_B_Anim references this clip. Four absent hashes; one FX Glow path still resolves.'),
 'Star_D_Anim_Start': ('reachable-default-unresolved', 'Star_D_Controoller default Star_D_Anim references this clip. Six absent hashes; no original Animator descendant matches them.'),
 'Star_End': ('reachable-explicit-play-unresolved', 'Original PlayStarEndAnimationForAll calls Animator.Play("Star_End"). Thirteen absent hashes; one FX Glow path still resolves.'),
 'an_estrFuente4_3': ('reachable-conditional-hardware-unresolved', 'EstrucFuente4Azul(Clone) transition parameter condition ==4 reaches this clip; EstrucFuente5Azul old state is unreachable. One absent hash; do not infer a replacement target.'),
}

entries = []
for clip in sorted({pathlib.Path(v['clip'].replace('\\','/')).stem for v in audit['unresolvedEntries']}):
    hashes = sorted({v['hash'] for v in audit['unresolvedEntries'] if pathlib.Path(v['clip'].replace('\\','/')).stem == clip})
    raw_names = {'agua13_0': {'agua13'}}.get(clip, {clip})
    clip_contexts = [c for c in contexts if c['clip']['name'] in raw_names and c['hash'] in hashes]
    unique_roots = {(c['root']['file'], c['root']['pathID'], c['rootName'], c['controller']['name']) for c in clip_contexts}
    text = pathlib.Path('Assets/AnimationClip', clip+'.anim').read_text(encoding='utf-8')
    exported_paths = sorted(set(re.findall(r'^    path: (.*)$', text, re.M)))
    entries.append({'clip': clip, 'classification': classification[clip][0], 'reason': classification[clip][1],
                    'unresolvedHashes': [f'0x{h:08X}' for h in hashes],
                    'knownExportedPaths': [p for p in exported_paths if not p.startswith('path_0x')],
                    'states': [s for s in states if any(c['name'] in raw_names for c in s['clips'])],
                    'originalRoots': [{'file':f,'pathID':p,'name':n,'controller':c} for f,p,n,c in sorted(unique_roots)],
                    'matchingOriginalDescendants': sum(len(c['matches']) for c in clip_contexts)})

result = {'crcCalibration': calibration, 'crcCalibrationPass': bool(calibration) and all(c['pass'] for c in calibration),
          'explicitGameAnimatorPlayCalls': [c for c in calls if not c['caller'].startswith('UnityEngine.')],
          'clips': entries,
          'scope': 'Raw bundle Animator trees, original controller states/edges, all native B and BL direct calls to Animator.Play wrappers, and all three Legacy Animation components. No Assets changes.',
          'limitation': 'A reachable state with absent descendants is not proof of a newly missing exported hierarchy: these hashes also have no target in the original bundled Animator trees. Dynamic descendants or obsolete clip curves need independent evidence; no guessed remapping.'}
(base/'animation-active-residuals-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('CRC calibration:', len(calibration), 'all pass:', result['crcCalibrationPass'])
print('Explicit native game Play calls:',len(result['explicitGameAnimatorPlayCalls']))
for e in entries:
    print(e['clip'], e['classification'],len(e['unresolvedHashes']),'hashes',len(e['originalRoots']),'raw roots')
