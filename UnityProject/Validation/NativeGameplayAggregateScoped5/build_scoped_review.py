"""Read existing closed Save1 evidence; never runs Unity or changes source assets."""
from pathlib import Path
import datetime as dt
import hashlib
import json
import re
import difflib

HERE = Path(__file__).resolve().parent
VALIDATION = HERE.parent
DRAFT = VALIDATION / "FinalGameplay/AggregationDraft"
UV_PATH = "Assets/Editor/RecoveredTools/FlowWaterWaterUVValidation.cs"
UV_SHA = "3d7abadae2d7f79faa5818b22fa39736ac45f32f15594b0f8e5bbbfa75b1f028"
TIME_SHA = "7c2edc431551c3116fefb9cae01833ec826126bfc3aca54a1121adaf12db83d8"


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def ref(path):
    path = Path(path).resolve()
    return {"path": str(path), "sha256": digest(path)}


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def write(name, data):
    path = HERE / name
    assert not path.exists(), "Immutable evidence already exists: " + str(path)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return ref(path)


def stamp(value):
    return dt.datetime.fromisoformat(value.replace("Z", "+00:00"))


source_path = DRAFT / "ExactSourceReviews/3d7abadae2d7-FlowWaterWaterUVValidation.cs/after.txt"
source = source_path.read_text(encoding="utf-8")
assert digest(source_path) == UV_SHA
assert 'if(busy || !SessionState.GetBool(Key,false)' in source
assert 'if(!capturing || rendered!=camera || originals==null' in source
assert 'static bool busy, locked, capturing;' in source
assert 'if (state == PlayModeStateChange.ExitingPlayMode) { Cleanup(); Unlock(); }' in source
assert 'capturing=false;Time.timeScale=originalTimeScale;' in source
assert source.index('SessionState.SetBool(Key,true)') > source.index('public static void Run()')
time_path = VALIDATION.parent / "ProjectSettings/TimeManager.asset"
assert digest(time_path) == TIME_SHA
assert re.search(r"^  m_TimeScale: 4\s*$", time_path.read_text(), re.M)

sources_by_sha = {}
for path in (DRAFT / "ExactSourceReviews").glob("*FlowWaterSave1GameplayValidation.cs/*.txt"):
    sources_by_sha[digest(path)] = path

runs = []
for item in load(DRAFT / "compatibility.draft.json")["runs"]:
    report_path = Path(item["reportPath"])
    assert digest(report_path) == item["reportSha256"]
    queue_path = Path(item["queueEvidence"]["path"])
    assert digest(queue_path) == item["queueEvidence"]["sha256"]
    queue = load(queue_path)
    assert len(queue["jobs"]) == 1
    job = queue["jobs"][0]
    before, after = load(job["beforeSnapshot"]["path"]), load(job["afterSnapshot"]["path"])
    if before["files"].get(UV_PATH) != UV_SHA:
        continue
    for key in ("beforeSnapshot", "afterSnapshot"):
        assert digest(job[key]["path"]) == job[key]["sha256"]
    assert before["files"] == after["files"] and job["changedSources"] == []
    assert before["files"]["ProjectSettings/TimeManager.asset"] == TIME_SHA
    assert after["files"]["ProjectSettings/TimeManager.asset"] == TIME_SHA
    report = load(report_path)
    assert report["finishedUtc"] and report["unityVersion"] == "2022.3.28f1"
    assert report["originalTimeScale"] == 4 and report["preferencesRestored"] is True
    assert report["runtimeErrors"] == []
    assert report["inputStrategy"] == "production-pointer-drag"
    assert stamp(report["finishedUtc"]) < stamp(job["finishedUtc"])
    assert job["exitCode"] == (0 if report["passed"] else 1)
    editor_sha = before["files"]["Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs"]
    editor_path = sources_by_sha[editor_sha]
    editor = editor_path.read_text(encoding="utf-8")
    finish = editor[editor.index("        static void Finish()") :]
    assert finish.index("Time.timeScale = report.originalTimeScale;") < finish.index("report.finishedUtc =")
    assert finish.index("Persist(); WriteNativeProgress(true);") < finish.index("EditorApplication.isPlaying = false;")
    assert "RestoreScenes(); Time.timeScale = SessionState.GetFloat(TimeKey, 1);" in editor
    assert "if (Application.isBatchMode && exit >= 0) EditorApplication.Exit(exit);" in editor
    log_path = Path(job["logPath"])
    lines = log_path.read_text(encoding="utf-8", errors="replace").splitlines()
    execute_lines = [n for n, line in enumerate(lines) if line == "-executeMethod"]
    assert len(execute_lines) == 1 and lines[execute_lines[0] + 1] == job["method"]
    assert job["method"].startswith("Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.")
    assert "FlowWaterWaterUVValidation.Run" not in "\n".join(lines)
    finish_lines = [n for n, line in enumerate(lines) if line.startswith("Save1 normal gameplay validation ")]
    assert len(finish_lines) == 1
    finish_line = finish_lines[0]
    restore_lines = [n for n, line in enumerate(lines) if n > finish_line and "Loaded scene 'Temp/__Backupscenes/" in line]
    time_lines = [n for n, line in enumerate(lines) if n > finish_line and "Start importing ProjectSettings/TimeManager.asset" in line]
    assert restore_lines and time_lines
    runs.append({
        "name": report_path.parent.name,
        "report": ref(report_path), "queue": ref(queue_path), "log": ref(log_path),
        "beforeSnapshot": ref(job["beforeSnapshot"]["path"]),
        "afterSnapshot": ref(job["afterSnapshot"]["path"]),
        "save1EditorSource": ref(editor_path),
        "startedUtc": report["startedUtc"], "finishedUtc": report["finishedUtc"],
        "processObservedFinishedUtc": job["finishedUtc"], "processExitCode": job["exitCode"],
        "reportPassed": report["passed"], "originalTimeScale": 4,
        "preferencesRestored": True, "runtimeErrors": [],
        "beforeAfterSourcesIdentical": True, "serializedTimeManagerBeforeAfterSha256": TIME_SHA,
        "logLines": {"executeMethod": execute_lines[0] + 2, "save1Finished": finish_line + 1,
                     "subsequentBackupSceneRestore": restore_lines[0] + 1,
                     "subsequentTimeManagerImport": time_lines[0] + 1},
    })

assert len(runs) == 7
review = write("uv-editor-save1-scoped-review.json", {
    "schemaVersion": 1, "reviewedUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
    "decision": "COMPATIBLE_ONLY_FOR_LISTED_CLOSED_MODE0_SAVE1_REPORTS",
    "sourcePair": {"path": UV_PATH, "baseSha256": None, "otherSha256": UV_SHA},
    "uvEditorSource": ref(source_path), "timeManagerContent": ref(time_path),
    "onlyReportSha256": [r["report"]["sha256"] for r in runs],
    "duringPlaySourceReasoning": [
        "Each actual Unity process command invokes only the Save1 validation method, never UV.Run.",
        "UV.Update returns while its suite key is false; UV.Run is the only code that sets its key true.",
        "UV.PostRender is not directly key-guarded: default capturing=false and originals=null prevent work. Only the own UV routine enables capture, and that routine is never started in these processes.",
        "The UV static constructor only installs callbacks. The unguarded Cleanup occurs at ExitingPlayMode, after Save1 Finish serializes all normal-mode player, target and completion verdicts.",
        "Save1 Finish restores its saved originalTimeScale=4 and preferences, serializes its closed report, then requests play exit. Its own EnteredEditMode callback restores TimeKey=4 and original scenes and calls process Exit.",
    ],
    "actualObservations": runs,
    "knownDefectRetained": {
        "description": "Old3d7 has real unconditional ExitingPlayMode Cleanup. In an unstarted UV process its static originalTimeScale defaults0, so it can transiently set Time.timeScale=0 after the Save1 report is already closed.",
        "fixedSourceSha256": "bafc6e168fa383c641e46d9e30a28d05d5ba303db15aa3a372e3c86ce010f085",
        "notClaimed": [
            "No direct runtime Time.timeScale sample was taken after ExitingPlayMode or immediately before process Exit in these seven old runs.",
            "Identical TimeManager hashes prove serialized scale4 remained intact; they alone do not measure transient in-memory scale.",
            "This is not approval of old3d7 as fully dormant or safe for unrelated suites, future reports, or future Input61.",
            "These seven reports include failed attempts; compatibility never converts their failure into a pass.",
        ],
    },
    "approvalBoundary": "Exact source pair and only the seven listed immutable report SHA256s. No future report or post-import Input61 is covered. Existing earlier withheld blanket review remains correct for its broader dormant-Editor claim.",
})
write("uv-editor-save1-scoped-approval.json", {
    "path": UV_PATH, "baseSha256": None, "otherSha256": UV_SHA,
    "mode0Unchanged": True, "onlyReportSha256": [r["report"]["sha256"] for r in runs],
    "explanation": "Exact seven closed Save1 mode0 runs only: UV own routine/capture never starts; its real unguarded Time=0 exit side effect is after all player/water/verdict data were serialized. Logs show the subsequent own scene restore and process exit, before/after source snapshots preserve serialized Time4, and every report records originalTimeScale4/preferences restored/errors0. This is not dormant-Editor or arbitrary-suite approval; see explicit in-memory sampling limitation in bound review.",
    "evidence": [review],
})
print(json.dumps({"review": review, "scopedReports": len(runs)}))
