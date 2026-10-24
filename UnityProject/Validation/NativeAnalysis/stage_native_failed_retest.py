import datetime
import hashlib
import json
import pathlib

project = pathlib.Path(__file__).resolve().parents[2]
base = project / 'Validation/NativeInputSlowViewsBatch1/Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs'
base_sha = '1c1e08026b0384b63afe644f1368ffcb146916817c9fc60a5cf3939d29ffc909'
assert hashlib.sha256(base.read_bytes()).hexdigest() == base_sha
text = base.read_text('utf-8-sig')

def patch(old, new):
    global text
    assert text.count(old) == 1, (old[:100], text.count(old))
    text = text.replace(old, new)

patch('        const string InputModeKey = "FlowWater.Save1GameplayValidation.NativeInput";',
'''        const string InputModeKey = "FlowWater.Save1GameplayValidation.NativeInput";
        const string RetestSourceKey = "FlowWater.Save1GameplayValidation.RetestSource";''')
patch('        [Serializable] public sealed class Report\n',
'''        [Serializable] public sealed class RetestSource
        {
            public string path, sha256, startedUtc, finishedUtc;
            public int[] closedFailedIds;
        }
        [Serializable] public sealed class Report
''')
patch('            public string startedUtc, finishedUtc, unityVersion, inputStrategy;',
'''            public string startedUtc, finishedUtc, unityVersion, inputStrategy;
            public RetestSource retestSource;''')
patch('        public static void RunNativeViewFailures() { Begin(new[] {754,761},true); }',
'''        public static void RunNativeViewFailures() { Begin(new[] {754,761},true); }
        public static void RunNativeFailed()
        {
            // Root copies a closed snapshot here. The parent run may still be
            // active, but an unfinished level without failure is never selected.
            string path=Path.Combine(Output,"native-retest-source.json");
            byte[] bytes=File.ReadAllBytes(path);
            string json=System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\\uFEFF');
            Report previous=JsonUtility.FromJson<Report>(json);
            if(previous==null || previous.inputStrategy!="production-pointer-drag" || previous.levels==null)
                throw new InvalidOperationException("Native retest source must be a production-pointer-drag Report snapshot: "+path);
            int[] cases=previous.levels.Where(l=>l!=null && l.id>0 && !l.passed && !String.IsNullOrEmpty(l.failure)).Select(l=>l.id).Distinct().OrderBy(id=>id).ToArray();
            if(cases.Length==0) throw new InvalidOperationException("No closed failed native-input levels in "+path);
            string sha;
            using(var digest=System.Security.Cryptography.SHA256.Create()) sha=BitConverter.ToString(digest.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
            Begin(cases,true,new RetestSource { path=Path.GetFullPath(path),sha256=sha,startedUtc=previous.startedUtc,finishedUtc=previous.finishedUtc,closedFailedIds=cases });
        }''')
patch('        static void Begin(int[] cases, bool nativeInput=false)',
      '        static void Begin(int[] cases, bool nativeInput=false, RetestSource retestSource=null)')
patch('            SessionState.SetBool(InputModeKey,nativeInput);',
'''            SessionState.SetBool(InputModeKey,nativeInput);
            SessionState.SetString(RetestSourceKey,retestSource==null ? "" : JsonUtility.ToJson(retestSource));''')
patch('                    if(NativeReplay) report.notes.Add("After four occluded quarter views, v8 may use genuine gentle original mouse rotation:',
'''                    string retestJson=SessionState.GetString(RetestSourceKey,"");
                    if(!String.IsNullOrEmpty(retestJson))
                    {
                        report.retestSource=JsonUtility.FromJson<RetestSource>(retestJson);
                        report.notes.Add("Retest of closed failed production-pointer-drag levels only. Source snapshot sha256="+report.retestSource.sha256+", ids="+String.Join(",",report.retestSource.closedFailedIds)+". A source run with no finishedUtc may still have unfinished levels; those are excluded. This is a separate actual retest for aggregate evidence, not a claim that the original full run passed in one execution.");
                    }
                    if(NativeReplay) report.notes.Add("After four occluded quarter views, v8 may use genuine gentle original mouse rotation:''')

batch = project / 'Validation/NativeInputFailedRetestBatch1'
out = batch / 'Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs'
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(text, encoding='utf-8')
sha = hashlib.sha256(out.read_bytes()).hexdigest()
manifest = {
    'createdUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'scope': 'EDITOR VALIDATOR ONLY. Main/clone Assets and production physics/camera/water are unchanged. Based exactly on v8 slow native views already actual-validated for754/761.',
    'files': [{'path': 'Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs',
               'source': out.relative_to(project).as_posix(), 'sha256': sha, 'baseSha256': base_sha}],
    'entrypoint': 'Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeFailed',
    'sourceReport': 'Validation/Save1Gameplay/native-retest-source.json',
    'input': 'Copy an immutable UTF8 full Report snapshot with inputStrategy=production-pointer-drag into sourceReport before running. The full source run may still be active. Only distinct positive ids whose level.passed=false and nonempty level.failure are selected; any unfinished case is excluded.',
    'provenance': 'Retest report includes exact source path/SHA256/start/finish and selected closed-failed IDs. Missing/wrong-strategy/empty-failure-source fails before Begin changes preferences or opens a scene. Existing source report is not overwritten.',
    'changes': ['New public native-failed selector and retained provenance only; all v8 playback actions, original ray/camera routes, animation timing and positive color/native victory checks unchanged.',
                'A normal RunNativeAll/Focused clears previous retest provenance to avoid stale cross-run metadata.',
                'No global source passed/finished requirement: partial-run closed failures can be retested while main continues; results explicitly remain separate aggregate evidence.'],
    'compile': 'Pending Roslyn editor check; no Unity launched by this subagent.',
    'actualUnityValidation': 'Not run for this selector. Underlying v8 actual754/761 passed in Unity2022.3.28f1 at2026-10-16T01:57:33.0757357Z, targets16/16 and13/13. Root launches next isolated retest.'
}
(batch / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(sha)
print(out.relative_to(project))
