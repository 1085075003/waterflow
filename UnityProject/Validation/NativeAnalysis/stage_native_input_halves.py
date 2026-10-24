from pathlib import Path
import hashlib
import json
from datetime import datetime, timezone

project=Path(__file__).resolve().parents[2]
base=project/'Validation/NativeLandingTimingBatch2/Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs'
base_sha='22707476b03ad527a2fa3939a0febe223e7b107148e0390e01e7da6d8a01804a'
assert hashlib.sha256(base.read_bytes()).hexdigest()==base_sha
text=base.read_text('utf-8')

def patch(before,after):
    global text
    assert text.count(before)==1,(before[:100],text.count(before))
    text=text.replace(before,after)

patch('        const string RetestSourceKey = "FlowWater.Save1GameplayValidation.RetestSource";',
'''        const string RetestSourceKey = "FlowWater.Save1GameplayValidation.RetestSource";
        const string CatalogKey = "FlowWater.Save1GameplayValidation.ExpectedCatalog";''')
patch('        [Serializable] public sealed class RetestSource\n', '''        [Serializable] public sealed class CatalogLevel
        {
            public int id, recordedSteps;
            public string layoutSha256, solutionSha256;
        }
        [Serializable] public sealed class ExpectedCatalog
        {
            public int originalLevelCount, originalRecordedSteps;
            public string catalogSha256;
            public List<CatalogLevel> levels=new List<CatalogLevel>();
        }
        [Serializable] public sealed class RetestSource
''')
patch('            public RetestSource retestSource;', '''            public RetestSource retestSource;
            public ExpectedCatalog expectedCatalog;''')
patch('        public static void RunNativeAll()\n', '''        public static void RunNativeHalf1() { RunNativeHalf(false); }
        public static void RunNativeHalf2() { RunNativeHalf(true); }
        static void RunNativeHalf(bool second)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start native-input validation from Edit mode.");
            RecoveryCatalogBuilder.Build();
            ExpectedCatalog expected=BuildExpectedCatalog();
            int[] ids=expected.levels.Select(x=>x.id).ToArray();
            Begin((second?ids.Skip(400):ids.Take(400)).ToArray(),true);
        }
        static string TextSha256(string value)
        {
            using(var hash=System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();
        }
        static ExpectedCatalog BuildExpectedCatalog()
        {
            var expected=new ExpectedCatalog();
            foreach(RecoveryLevelEntry entry in RecoveryCatalog.Load().levels.OrderBy(x=>x.assetId))
            {
                if(entry.solution==null) continue;
                RecoverySave solution=JsonUtility.FromJson<RecoveryEnvelope>(entry.solution.text).data;
                if(!solution.IsValidSteps()) continue;
                if(entry.layout==null) throw new InvalidOperationException("Original layout missing for gm"+entry.assetId);
                expected.levels.Add(new CatalogLevel { id=entry.assetId,recordedSteps=solution.move.Count,
                    layoutSha256=TextSha256(entry.layout.text),solutionSha256=TextSha256(entry.solution.text) });
            }
            expected.originalLevelCount=expected.levels.Count;
            expected.originalRecordedSteps=expected.levels.Sum(x=>x.recordedSteps);
            if(expected.originalLevelCount!=800 || expected.originalRecordedSteps!=11919 || expected.levels.Select(x=>x.id).Distinct().Count()!=800)
                throw new InvalidOperationException("Original Save1 catalog must be800 unique levels and11919 recorded entries.");
            string canonical=String.Join("\\n",expected.levels.Select(x=>x.id+"|"+x.recordedSteps+"|"+x.layoutSha256+"|"+x.solutionSha256));
            expected.catalogSha256=TextSha256(canonical);
            return expected;
        }
        public static void RunNativeAll()
''')
patch('            SessionState.SetBool(InputModeKey,nativeInput);', '''            ExpectedCatalog expected=nativeInput?BuildExpectedCatalog():null;
            if(expected!=null && cases.Any(id=>!expected.levels.Any(entry=>entry.id==id))) throw new InvalidOperationException("Requested native case is absent from the unchanged original Save1 catalog.");
            SessionState.SetString(CatalogKey,expected==null?"":JsonUtility.ToJson(expected));
            if(expected!=null) File.WriteAllText(Path.Combine(Output,"expected-catalog.json"),JsonUtility.ToJson(expected,true));
            SessionState.SetBool(InputModeKey,nativeInput);''')
patch('                    string retestJson=SessionState.GetString(RetestSourceKey,"");', '''                    string catalogJson=SessionState.GetString(CatalogKey,"");
                    if(!String.IsNullOrEmpty(catalogJson)) report.expectedCatalog=JsonUtility.FromJson<ExpectedCatalog>(catalogJson);
                    if(NativeReplay && report.expectedCatalog==null) throw new InvalidOperationException("Original expected catalog did not survive the Play-mode/domain transition.");
                    if(report.expectedCatalog!=null) report.notes.Add("Expected original catalog800 unique ids/11919 recorded entries, identity="+report.expectedCatalog.catalogSha256+". This run executes"+Cases.Length+" actual cases; a400-case run alone cannot qualify as800-level coverage. Text SHA256 values hash the actual unchanged Unity TextAsset.text as UTF8; canonical inventory rows=id|recordedSteps|layoutSha256|solutionSha256 joined by newline, sorted byid.");
                    string retestJson=SessionState.GetString(RetestSourceKey,"");''')

batch=project/'Validation/NativeInputHalvesBatch1'
target=batch/'Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs'
target.parent.mkdir(parents=True,exist_ok=True)
target.write_text(text,'utf-8')
manifest={'createdUtc':datetime.now(timezone.utc).isoformat(),
    'scope':'Editor-only staging. Actions/timing/real input and production criteria unchanged from LandingTimingBatch2 diagnostic Editor22707476. No Assets changes/no Unity launch.',
    'files':[{'path':'Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs','source':target.relative_to(project).as_posix(),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'baseSha256':base_sha}],
    'entrypoints':['Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeHalf1','Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeHalf2'],
    'retainedEntrypoints':['RunNativeAll','RunNativeFailed','RunNativeFocused','RunNativeLandingRegression'],
    'selection':'Original valid Save1 catalog sorted by assetId: first400 and last400, never an arbitrary generatedid range. Reports plannedLevels400 honestly; no claim of800 from onehalf.',
    'expectedCatalog':'All800 originalids and individualmove.Count plus UTF8SHA256 of actual layout/solution TextAsset.text. Canonical rows=id|recordedSteps|layoutSha256|solutionSha256 newline-joined sortedid. Count800,total11919 anduniqueids required before starting. Both Report.expectedCatalog and Output/expected-catalog.json include this inventory.',
    'compile':'Pending Roslyn Editor. Root launches actual Unity after physical LandingTimingBatch2 validation.'}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),'utf-8')
print(target.relative_to(project),manifest['files'][0]['sha256'])
