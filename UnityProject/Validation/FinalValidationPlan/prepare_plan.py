"""Prepare final queue JSONs only. Never launches Unity, edits Assets or moves results."""
import datetime
import hashlib
import importlib.util
import json
import pathlib
import re

OUT = pathlib.Path(__file__).resolve().parent
PROJECT = OUT.parents[1]
WORKSPACE = PROJECT.parent
STAMP = 'final-' + datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
ARCHIVE = 'FinalValidationRuns/' + STAMP
PYTHON = 'C:/Users/10850/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
PREFIX = 'Recovered.FlowWater.Editor.'
PROJECTS = {'Main': PROJECT, 'Half2': WORKSPACE/'.validation-project', 'Visual': WORKSPACE/'.validation-visual-project'}
RELATIVE_PROJECTS = {'Main': '../..', 'Half2': '../../../.validation-project', 'Visual': '../../../.validation-visual-project'}
VERSION = '2022.3.28f1'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def job(name, method, directory, report, destination, timeout=1800, **metadata):
    return dict(name=name, method=PREFIX+method, reportDirectory='Validation/'+directory,
                reportFile=report, archiveDestination=ARCHIVE+'/'+destination,
                timeoutSeconds=timeout, **metadata)

def queue(project, jobs, keep_going=True, **metadata):
    return dict(schemaVersion=1, planId=STAMP, project=RELATIVE_PROJECTS[project], unityVersion=VERSION,
                continueOnFailure=keep_going, jobs=jobs,
                executionStatus='Prepared only; no Unity process launched by this plan.', **metadata)

# Check counts are reference inventories, never substitutes for all actual assertions passing.
SUITES = [
 ('01-native-preview-positive-609-622','FlowWaterNativePreviewEvidenceValidation.Run','Save1Gameplay','save1-gameplay-validation.json',1800,'normal mode0 pointer positive targets for zero-target saved previews',[609,622],None),
 ('02-full1350','FlowWaterFullValidation.RunAll','FullContent','all-runtime-validation.json',21600,'all1350 instantiation, original presentation, structural vertices and saved-preview water; zero goals need prior normal proof',[],1350),
 ('03-ui','FlowWaterUIValidation.Run','UIFlows','ui-flow-validation.json',1800,'actual original control/menu flows',[],762),
 ('04-input-precheck','FlowWaterInteractionValidation.Run','Interaction','interaction-validation.json',900,'precheck only; final independent Input61 still required after all retests',[],61),
 ('05-camera','FlowWaterCameraTutorialValidation.Run','CameraTutorial','camera-tutorial-validation.json',900,'native rotation/tutorial',[],17),
 ('06-springs','FlowWaterSpringValidation.Run','Springs','spring-validation.json',1200,'real lift/rotation/plate/water/Undo',[],44),
 ('07-spring-modes','FlowWaterSpringModeValidation.Run','SpringModes','spring-mode-validation.json',1200,'controlled native mode2/3 busy and phase constraints',[],73),
 ('08-spring-constraints','FlowWaterSpringConstraintValidation.Run','SpringConstraints','spring-constraint-validation.json',1200,'native negative collision/channel constraints',[],27),
 ('09-propagation','FlowWaterPropagationValidation.Run','WaterPropagation','water-propagation-validation.json',1500,'front delays and snapshot/gates',[],27),
 ('10-preview','FlowWaterPreviewValidation.Run','CategoryPreview','category-preview-validation.json',1500,'six original animated category previews',[],48),
 ('11-aspect','FlowWaterAspectValidation.Run','Aspect','aspect-validation.json',1500,'supported1280x720 and885x630 structural geometry/UI',[],39),
 ('12-fountain-animation','FlowWaterFountainAnimationValidation.Run','FountainAnimation','fountain-animation-validation.json',1200,'original source/model Animator parameters',[],13),
 ('13-fountain-dispatch','FlowWaterFountainDispatchValidation.Run','FountainDispatch','fountain-dispatch-validation.json',1200,'native palette/model/fountain dispatch',[],31),
 ('14-decorative-pits','FlowWaterDecorativePitValidation.Run','DecorativePits','decorative-pit-validation.json',1500,'original pits and flow Animator',[],59),
 ('15-bottom-markers','FlowWaterBottomMarkerValidation.Run','BottomMarker','bottom-marker-validation.json',1200,'actual C bottom/intake marker geometry',[],12),
 ('16-jet-normal-gameplay','FlowWaterNativeJetRotationValidation.Run','Save1Gameplay','save1-gameplay-validation.json',1800,'latest world-basis and actual real-pointer three-case proof',[1696,1656,1660],None),
 ('17-jet-lifetime-snapshots','FlowWaterJetLifetimeValidation.Run','JetLifetime','jet-lifetime-validation.json',1500,'latest paused/new-root and playing/same-root exact particle snapshots',[],20),
 ('18-upward-pfuente','FlowWaterPfuenteOcclusionValidation.Run','PfuenteOcclusion','pfuente-occlusion-validation.json',1200,'upward P native blocking and snapshots',[],16),
 ('19-standalone-c3','FlowWaterStandaloneFountainValidation.Run','StandaloneFountain','standalone-fountain-validation.json',1200,'latest C3 Start, real red gm519 record1, same/new-root lifecycle',[],18),
 ('20-stars','FlowWaterStarAnimationValidation.Run','UIAnimations','native-ui-animation-validation.json',1500,'real native completion/star curves and disabled alternate branch',[],None),
 ('21-language','FlowWaterNativeLanguageValidation.Run','NativeLanguage','native-language-validation.json',1800,'21 codes, original glyphs/animations/control callbacks, actual positive water completion',[],2288),
 ('22-step-automatic','FlowWaterStepPlaybackValidation.Run','NativeStepPlayback','native-step-playback-validation.json',1800,'native three-phase preview, snapshot/Undo/Exit',[601,1851,1129],None),
 ('23-step-mechanism','FlowWaterStepPlaybackValidation.RunMechanisms','NativeStepPlayback','native-step-playback-validation.json',1800,'strict original automatic-w wait',[1656],None),
 ('24-step-lifts','FlowWaterStepPlaybackValidation.RunLifts','NativeStepPlayback','native-step-playback-validation.json',1800,'native lift preview and phase completion',[1129,1754,1961],None),
 ('25-step-missing-capture','FlowWaterStepPlaybackValidation.RunMissingCapture','NativeStepPlayback','native-step-playback-validation.json',1800,'actual original1225 full capture/actor trace',[1225],None),
 ('26-step-reference-lifetime','FlowWaterStepPlaybackValidation.RunCaptureLifetime','NativeStepPlayback','native-step-playback-validation.json',1800,'controlled stale-reference/first-miss boundary; not normal gameplay victory',[0],None),
]

def prepare():
    files = {}
    visual = [job(n,m,d,f,'Visual/'+n,t,scope=s,caseIds=ids,referenceCount=c)
              for n,m,d,f,t,s,ids,c in SUITES]
    files['main-half1.queue.json'] = queue('Main', [job('native-half1','FlowWaterSave1GameplayValidation.RunNativeHalf1','Save1Gameplay','save1-gameplay-validation.json','Main-Half1',21600)],False)
    files['clone-half2.queue.json'] = queue('Half2', [job('native-half2','FlowWaterSave1GameplayValidation.RunNativeHalf2','Save1Gameplay','save1-gameplay-validation.json','Clone-Half2',21600)],False)
    files['visual-regression.queue.json'] = queue('Visual',visual,True,
        orderingRequirement='Positive609622 job01 finishes first and stays in working Save1Gameplay through Full1350; all jobs sequential on this separate project.')
    files['final-input-post-retests.queue.json'] = queue('Main', [job('final-input61-after-all-retests','FlowWaterInteractionValidation.Run','Interaction','interaction-validation.json','Final-Input61-After-Retests',900)],False,
        executionGate=['Both400 base reports finished.','All selected final native retests finished on identical critical physics.','All visual/feature jobs and manual AssetSuite finished; no more source imports.','Independent final Input61 startedUtc must be later than every supplied retest finishedUtc; visual Input is only a precheck.'])
    files['native-failed-retest.template.queue.json'] = queue('Half2',[job('native-failed-retest','FlowWaterSave1GameplayValidation.RunNativeFailed','Save1Gameplay','save1-gameplay-validation.json','Retests-NativeFailed-Batch01',21600)],False,
        executionGate=['Only if actual closed final base has nonempty failed ids.','Copy that immutable original report to this project Validation/Save1Gameplay/native-retest-source.json.','For two source reports use distinct new queues/archive destinations, preserving each source provenance.','Do not skip failed movements, alter inputs/targets or combine different critical physics. Final Input61 follows every selected retest.'])
    files['native-terminal-1187.optional.queue.json'] = queue('Half2',[job('native-1187-terminal-quick-release','FlowWaterSave1GameplayValidation.RunNativeTerminalQuickRelease','Save1Gameplay','save1-gameplay-validation.json','Retest-1187-Terminal-Quick-Release',1800,
        scope='legal alternate real-pointer release timing; explicit playerReleaseStrategy and terminalQuickRelease fields')],False,
        executionGate=['Optional only if1187 actually failed in the finished final base.','Same final frozen physics as bothhalves; no use of older pre-freeze quick success.','Keep failed standard player strategy report; do not relabel it as successful.','Final Input61 follows this and all other retests.'])

    # Source results may have been reversibly relocated while this plan was prepared.
    candidates = list((PROJECTS['Half2']/'Validation/Save1Gameplay').glob('expected-catalog.json'))
    candidates += list((PROJECTS['Half2']/'Validation/History').rglob('expected-catalog.json'))
    assert candidates, 'Original800 expected catalog evidence missing'
    catpath = candidates[0]; cat=json.loads(catpath.read_text('utf-8-sig'))
    rows=sorted(cat['levels'],key=lambda x:x['id'])
    assert cat['originalLevelCount']==800 and cat['originalRecordedSteps']==11919
    assert len(rows)==len({x['id'] for x in rows})==800 and sum(x['recordedSteps'] for x in rows)==11919
    canonical='\n'.join(f"{x['id']}|{x['recordedSteps']}|{x['layoutSha256']}|{x['solutionSha256']}" for x in rows)
    assert hashlib.sha256(canonical.encode()).hexdigest()==cat['catalogSha256']=='fe00b5eb0a0baa923ee8b520661022046a1ea81cc19fbd1399c8001d47744e85'
    files['original-save1-catalog.json']={'evidenceKind':'Original expected input catalog only, not gameplay success.','source':str(catpath),'sourceSha256':sha(catpath),'expectedCatalog':cat,
        'halves':[{'method':'RunNativeHalf'+str(i+1),'ids':[x['id'] for x in part],'levels':400,'recordedSteps':sum(x['recordedSteps'] for x in part)} for i,part in enumerate([rows[:400],rows[400:]])]}

    files['asset-suite.manual.json'] = {
        'schemaVersion':1,'planId':STAMP,'manualOnly':True,'doNotPassToQueueRunner':True,'project':str(PROJECTS['Visual']),
        'reason':'RunAssetSuite writes at Validation root; runner archive of root would capture History/staging/archives and is deliberately excluded.',
        'method':PREFIX+'FlowWaterValidation.RunAssetSuite','unityVersion':VERSION,
        'argv':['D:/UnityHub/Editor/2022.3.28f1/Editor/Unity.exe','-batchmode','-projectPath',str(PROJECTS['Visual']),'-logFile',str(PROJECT/'Validation'/ARCHIVE/'Manual-AssetSuite/unity-2022.3.28f1.log'),'-executeMethod',PREFIX+'FlowWaterValidation.RunAssetSuite'],
        'launch':'Root only, hidden process, after visual queue self-exits. One Unity per exact project. Self-exit; no forced kill.',
        'snapshotTool':'Existing run_unity_queue.snapshot(project,path,basis) before/after the manual launch; output below MainValidation. This function hashes only and never launches Unity.',
        'snapshotPaths':[ARCHIVE+'/Manual-AssetSuite/sources-before.json',ARCHIVE+'/Manual-AssetSuite/sources-after.json'],
        'archiveOnlyFiles':['Validation/audit.json','Validation/data-validation.json','Validation/media-validation.json','Validation/rotation-help-preference-restoration.json'],
        'freshnessChecks':['All three primaries are newly written by this launch, Unity2022.3.28f1, passed true, zero failures/errors and self-exit0.','audit/data have started/finished newer than launch. media has only finishedUtc: require finished>=launch plus new file timestamp/SHA; do not invent startedUtc.','Source dictionaries identical before/after; bind all actual snapshot/report/log SHA.','Copy selected files only. Never copy/move/archive the entire Validation root.'],
        'executionGate':'After visual queue exits, before final independent Input61, with production frozen.'}

    all_methods=[PREFIX+'FlowWaterSave1GameplayValidation.RunNativeHalf1',PREFIX+'FlowWaterSave1GameplayValidation.RunNativeHalf2']+[PREFIX+x[1] for x in SUITES]
    readiness={'capturedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'Current read-only entrypoint readiness only; root owns final freeze/source inventory.','projects':{},
        'uiLanguageAudit':{'source':'.validation-project/Assets/Editor/RecoveredTools/FlowWaterUIValidation.cs','sha256':sha(PROJECTS['Half2']/'Assets/Editor/RecoveredTools/FlowWaterUIValidation.cs'),
            'EnglishTextAssertions':False,'InvokesHiddenLanguageOrDesignerControls':False,'fixturePatchNeededFromThisAudit':False,
            'languagePreferenceRestoration':'Current FlowWaterValidationPreferenceGuard saves language/helpRotation before executeMethod and restores on quitting; root guard report is separate from per-suite output.','screenshots':'UI follows original system locale. LanguageSuite independently has explicit English/callingScene1 baseline.'}}
    for name,p in PROJECTS.items():
        present={}
        for method in all_methods:
            cls,short=method.rsplit('.',1);cls=cls.rsplit('.',1)[1];f=p/'Assets/Editor/RecoveredTools'/(cls+'.cs')
            present[method]=bool(f.exists() and re.search(r'public\s+static\s+void\s+'+re.escape(short)+r'\s*\(',f.read_text('utf-8-sig')))
        settings=(p/'ProjectSettings/ProjectSettings.asset').read_text('utf-8-sig');time=(p/'ProjectSettings/TimeManager.asset').read_text('utf-8-sig')
        readiness['projects'][name]={'project':str(p),'companyName':re.search(r'^\s*companyName:\s*(.*)$',settings,re.M)[1],'productName':re.search(r'^\s*productName:\s*(.*)$',settings,re.M)[1],
            'timeScale':float(re.search(r'm_TimeScale:\s*(\S+)',time)[1]),'entryMethodPresentNow':present,'missingEntryMethodsNow':[k for k,v in present.items() if not v]}
    files['readiness-review.json']=readiness

    files['expected-results.json']={'planId':STAMP,'unityVersion':VERSION,
        'normalGameplay':{'bases':[ARCHIVE+'/Main-Half1/save1-gameplay-validation.json',ARCHIVE+'/Clone-Half2/save1-gameplay-validation.json'],'expectedOriginalIds':800,'expectedOriginalRecords':11919,'catalogSha256':cat['catalogSha256'],
            'standard':'normal mode0 real pointer drags, positive original required goals, matching-color water/native mission, no pending/busy, original51 polling','aggregate':'Separate native-gameplay-coverage.json only after actual finished compatible halves/retests and final Input61; aggregate evidence, never single-run800 claim.'},
        'features':[{'name':j['name'],'method':j['method'],'expectedPrimaryArchive':j['archiveDestination']+'/'+j['reportFile'],'caseIds':j['caseIds'],'referenceCount':j['referenceCount'],'scope':j['scope']} for j in visual],
        'full1350Positive609622':{'workingReference':str(PROJECTS['Visual']/'Validation/Save1Gameplay/save1-gameplay-validation.json'),'immutableArchive':ARCHIVE+'/Visual/01-native-preview-positive-609-622/save1-gameplay-validation.json',
            'binding':'Full1350 actually reads job01 normal proof before later Jet report rewrites working path. Bind final result to immutable job01 actual report SHA and snapshots.'},
        'reusedWorkingDirectories':['Save1Gameplay is reused by job16 only after job01 and Full1350 archives.','NativeStepPlayback five jobs overwrite primary JSON. Previous same-freeze images may remain; only each fresh primary defines its cases/checks. Do not relabel old images as new.'],
        'finalInputArchive':ARCHIVE+'/Final-Input61-After-Retests/interaction-validation.json','assetSuite':'Manual selected root files only; never root Validation directory.'}

    files['operator-plan.json']={'planId':STAMP,'status':'Reviewable preparation only. No Unity launched or final success inferred.','projects':{k:str(v) for k,v in PROJECTS.items()},
        'executionOrder':['Finish confirmed fixes and mirror the same final runtime/Editor/Resources to allthree projects, keeping unique product profiles and Time4.','Root reversibly moves existing report work directories to project Validation/History before first final launch. No History/FirstPass/old archive subtree may remain inside archived work directories.','MainHalf1 and cloneHalf2 use separate projects/Libraries; visual sequence uses its own third project. Never second Unity for one project.','Visual job01 positive609622 before Full1350, then all26 feature jobs serially. continueOnFailure collects honest failures, never passes them.','Manual AssetSuite only when visual queue exits; copy selected root reports only.','After bases finish, execute only needed original-id retests on same critical physics. New critical physics requires new final coverage, never mixed-version merge.','After every selected retest and feature/AssetSuite job, run independent final Input61 once.','Root binds actual report SHA/source snapshots and performs strict disjoint400+400 union800/11919 aggregation; final notes/inventory remain root-owned.'],
        'outputPreparation':{'operation':'Root-only reversible move; this plan moves/deletes nothing.','sourceDirectories':{n:sorted({'Validation/'+x[2] for x in SUITES} if n=='Visual' else {'Validation/Save1Gameplay','Validation/Interaction'}) for n in PROJECTS},
            'destinationPattern':'<project>/Validation/History/'+STAMP+'-before-final/<original-directory-name>',
            'requirements':['Verify absolute targets stay in intended workspace/project; destination new and outside source.','Move entire previous work directory once, recreate only empty working directory. Keep NativeAnalysis and Golden fixture evidence.','Clone Save1Gameplay old Jet cases are not finalHalf2; root handles their prior location.','No History/FirstPass/FocusedFront/FirstFocused archive trees left in any queue reportDirectory.']},
        'runnerRules':['Fixed installed Unity2022.3.28f1, hidden launch, before/after source/settings snapshots, actual new closed report and immutable selected-directory archive.','Timeout records live PID and stops queue without kill or next same-project job.','Source dictionaries equal before/after each job; bind actual launch snapshots, not a guessed final snapshot.','Guard root preference report lies outside most suite archives; root preserves its final actual record separately.'],
        'launchArgv':[[PYTHON,'UnityProject/Validation/run_unity_queue.py','--project',str(PROJECTS[n]),'--queue',str(OUT/f)] for n,f in [('Main','main-half1.queue.json'),('Half2','clone-half2.queue.json'),('Visual','visual-regression.queue.json')]],
        'afterAllRetestsArgv':[PYTHON,'UnityProject/Validation/run_unity_queue.py','--project',str(PROJECTS['Main']),'--queue',str(OUT/'final-input-post-retests.queue.json')],
        'noFinalCoverageCreated':True}

    assert all(not (OUT/name).exists() for name in files), 'Do not overwrite a reviewable plan'
    for name,value in files.items(): (OUT/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf-8')
    spec=importlib.util.spec_from_file_location('queue_review',PROJECT/'Validation/run_unity_queue.py');runner=importlib.util.module_from_spec(spec);spec.loader.exec_module(runner)
    review=[]
    for n,f in [('Main','main-half1.queue.json'),('Half2','clone-half2.queue.json'),('Visual','visual-regression.queue.json'),('Main','final-input-post-retests.queue.json'),('Half2','native-failed-retest.template.queue.json'),('Half2','native-terminal-1187.optional.queue.json')]:
        q=files[f];assert (OUT/q['project']).resolve()==PROJECTS[n]
        jobs=runner.validate_plan(PROJECTS[n],q,OUT/'DryRuns'/f.replace('.queue.json',''))
        review.append({'queue':f,'project':str(PROJECTS[n]),'jobs':len(jobs),'structureAccepted':True,'archiveDestinationsNewAndNotNested':True,'UnityLaunched':False})
    (OUT/'queue-structure-review.json').write_text(json.dumps({'planId':STAMP,'review':review,'noUnityLaunched':True},ensure_ascii=False,indent=2)+'\n','utf-8')
    manifest={'planId':STAMP,'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'status':'JSON prepared; existing runner validate_plan accepted allsix queues. No Unity launched; no Assets, existing scripts or result directories changed.',
        'files':[{'path':f.relative_to(PROJECT).as_posix(),'sha256':sha(f)} for f in sorted(OUT.iterdir()) if f.is_file()],
        'defaultVisualJobs':26,'manualAssetSuite':True,'independentFinalInput':True}
    (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n','utf-8')
    print(json.dumps({'planId':STAMP,'visualJobs':len(SUITES),'validatedQueues':len(review),'half1Ids':[rows[0]['id'],rows[399]['id']],'half2Ids':[rows[400]['id'],rows[-1]['id']],
        'half1Steps':sum(x['recordedSteps'] for x in rows[:400]),'half2Steps':sum(x['recordedSteps'] for x in rows[400:]),'catalog':cat['catalogSha256'],
        'missingMainEntries':len(readiness['projects']['Main']['missingEntryMethodsNow']),'missingVisualEntries':len(readiness['projects']['Visual']['missingEntryMethodsNow'])},ensure_ascii=False))

if __name__=='__main__':
    prepare()
