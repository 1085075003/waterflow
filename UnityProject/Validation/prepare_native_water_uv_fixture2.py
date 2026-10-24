import datetime,hashlib,json,pathlib,shutil,difflib
project=pathlib.Path(__file__).resolve().parent.parent
previous=project/'Validation/NativeWaterUV1'
batch=project/'Validation/NativeWaterUVFixture2'
target='Assets/Editor/RecoveredTools/FlowWaterWaterUVValidation.cs'
old=(previous/target).read_text(encoding='utf-8')
new=old.replace('public bool passed, timeScaleRestored, preferencesRestored;',
                'public bool passed, timeScaleRestored, preferencesRestored, enteredEditMode;\n            public float playModeRestoredTimeScale, serializedTimeManagerScale;')
new=new.replace('if (state == PlayModeStateChange.EnteredEditMode) SessionState.EraseBool(Key);',
'''if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+".Finished",false)) {
                    float savedScale=SessionState.GetFloat(Key+".Scale",1);
                    // Mirror PropagationTimeRestoreBatch2: the play-mode exit
                    // reset runs before this callback. Apply again on delayCall.
                    Time.timeScale=savedScale;
                    EditorApplication.delayCall+=()=>CompleteInEditMode(savedScale);
                }''')
new=new.replace('SessionState.SetString(Key+".Language",PlayerPrefs.GetString("language",""));',
'''SessionState.SetFloat(Key+".Scale",Time.timeScale);
            SessionState.EraseBool(Key+".Finished");
            SessionState.SetString(Key+".Language",PlayerPrefs.GetString("language",""));''')
new=new.replace('if(busy || !SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;',
'''if(busy || !SessionState.GetBool(Key,false) || SessionState.GetBool(Key+".Finished",false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;''')
new=new.replace('originalTimeScale=Time.timeScale;','originalTimeScale=SessionState.GetFloat(Key+".Scale",Time.timeScale);')
new=new.replace('new[] {"AguaControl5","Cascada1Z","Cascada1Ro"}',
                'new[] {"AguaControl5","AguaControl5B","Cascada1Z","Cascada1Ro"}')
needle='''            SessionState.EraseBool(Key);Unlock();EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(report.passed?0:1);'''
replacement='''            report.playModeRestoredTimeScale=Time.timeScale;
            // Keep the report pending until the real EnteredEditMode boundary.
            report.finishedUtc=""; SessionState.SetString(Key+".PendingReport",JsonUtility.ToJson(report));
            SessionState.SetBool(Key+".Finished",true);routine=null;
            Unlock();EditorApplication.ExitPlaymode();
        }
        static void CompleteInEditMode(float savedScale)
        {
            Time.timeScale=savedScale;
            Report result=JsonUtility.FromJson<Report>(SessionState.GetString(Key+".PendingReport","{}"));
            result.enteredEditMode=!EditorApplication.isPlaying;
            result.restoredTimeScale=Time.timeScale;
            result.timeScaleRestored=result.enteredEditMode && result.restoredTimeScale==result.originalTimeScale;
            UnityEngine.Object[] settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
            result.serializedTimeManagerScale=-1;
            if(settings.Length>0) {
                var serialized=new SerializedObject(settings[0]);serialized.Update();
                SerializedProperty scale=serialized.FindProperty("m_TimeScale");
                if(scale!=null)result.serializedTimeManagerScale=scale.floatValue;
            }
            result.checks.Add(new Check { name="time-scale-restored-after-EnteredEditMode",passed=result.timeScaleRestored && result.serializedTimeManagerScale==savedScale,
                detail="saved="+savedScale+",actual="+Time.timeScale+",serializedTimeManager="+result.serializedTimeManagerScale });
            result.passed=result.errors.Count==0 && result.checks.Count>0 && result.checks.All(x=>x.passed) && result.timeScaleRestored && result.preferencesRestored;
            result.finishedUtc=DateTime.UtcNow.ToString("o");Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output,"native-water-uv-validation.json"),JsonUtility.ToJson(result,true));
            SessionState.EraseBool(Key);SessionState.EraseBool(Key+".Finished");SessionState.EraseString(Key+".PendingReport");
            Time.timeScale=savedScale;
            if(Application.isBatchMode)EditorApplication.Exit(result.passed?0:1);'''
assert old.count(needle)==1
new=new.replace(needle,replacement)
out=batch/target;out.parent.mkdir(parents=True,exist_ok=True);out.write_text(new,encoding='utf-8')
shutil.copyfile(previous/(target+'.meta'),batch/(target+'.meta'))
(batch/'editor.diff').write_text(''.join(difflib.unified_diff(old.splitlines(True),new.splitlines(True),fromfile=target+' UV1',tofile=target+' fixture2')),encoding='utf-8')
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'assetsModified':False,
          'runtimeUnchangedSha256':'e73aaf4d9964cb9e63e8cbd62bd98b3a13b80f9ef6775a897b53b3dd65088a05',
          'files':[{'target':target,'baseSha256':hashlib.sha256((previous/target).read_bytes()).hexdigest(),
                    'staged':str(out.relative_to(project)).replace('\\','/'),'stagedSha256':hashlib.sha256(out.read_bytes()).hexdigest()},
                   {'target':target+'.meta','baseSha256':hashlib.sha256((previous/(target+'.meta')).read_bytes()).hexdigest(),
                    'staged':str((batch/(target+'.meta')).relative_to(project)).replace('\\','/'),'stagedSha256':hashlib.sha256((batch/(target+'.meta')).read_bytes()).hexdigest()}],
          'compile':'pending','testEntry':'Recovered.FlowWater.Editor.FlowWaterWaterUVValidation.Run',
          'report':'Validation/WaterUV/native-water-uv-validation.json',
          'fix':'Original AguaControl5B has enabled MatGr16_2 renderers; retain strict required coverage. Save scale before scene/play manipulation and finalize report+restore actual/serialized TimeManager after EnteredEditMode and delayed callback, before selfExit.'}
(batch/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
rsp=(previous/'editor.rsp').read_text(encoding='utf-8').replace('-out:"Validation/NativeWaterUV1/editor.dll"','-out:"Validation/NativeWaterUVFixture2/editor.dll"').replace('"Validation/NativeWaterUV1/'+target+'"','"Validation/NativeWaterUVFixture2/'+target+'"')
(batch/'editor.rsp').write_text(rsp,encoding='utf-8')
print(json.dumps(manifest,indent=2))
