"""Sequential Unity2022.3.28f1 validation jobs; never imports/promotes source.

  python Validation/run_unity_queue.py --project PROJECT --queue QUEUE.json
  python Validation/run_unity_queue.py --project PROJECT --queue QUEUE.json --dry-run

QUEUE: {"continueOnFailure":true,"jobs":[{"name":"capture1225",
"method":"Recovered.FlowWater.Editor.FlowWaterStepPlaybackValidation.RunMissingCapture",
"reportDirectory":"Validation/NativeStepPlayback",
"reportFile":"native-step-playback-validation.json",
"archiveDestination":"QueueArchives/capture1225","timeoutSeconds":1200}]}

reportDirectory is relative to PROJECT. archiveDestination is relative to this
script's MainValidation directory (or an absolute path beneath it). Each job
gets before/after C#/ResourcesJSON/ProjectSettings snapshots and an immutable
report archive. Only reports started after that job's launch and actually
finished count. A timeout NEVER kills Unity and NEVER starts the next job while
that process is alive, even with continueOnFailure. A retained queue lock records
that live process. The script launches only when explicitly executed without
--dry-run; merely reading/compiling it never launches Unity.
"""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time
import uuid


UNITY = Path("D:/UnityHub/Editor/2022.3.28f1/Editor/Unity.exe")
MAIN_VALIDATION = Path(__file__).resolve().parent
WORKSPACE = MAIN_VALIDATION.parent.parent
VERSION = "2022.3.28f1"
NAME = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]{0,100}\Z")
METHOD = re.compile(r"[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)+\Z", re.ASCII)


class QueueError(ValueError):
    pass


def require(ok, message):
    if not ok:
        raise QueueError(message)


def utc():
    return datetime.now(timezone.utc)


def iso():
    return utc().isoformat()


def timestamp(value):
    require(isinstance(value, str) and value, "Missing startedUtc/finishedUtc")
    result = datetime.fromisoformat(value.replace("Z", "+00:00"))
    require(result.tzinfo is not None, "Report timestamp lacks timezone")
    return result.astimezone(timezone.utc)


def inside(path, root):
    return path.resolve().is_relative_to(root.resolve())


def related(a, b):
    return inside(a, b) or inside(b, a)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path, value):
    require(inside(path, MAIN_VALIDATION), f"Write outside MainValidation rejected: {path}")
    path.parent.mkdir(parents=True, exist_ok=True)
    pending = path.with_name(path.name + ".pending")
    pending.write_text(json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + "\n", "utf-8")
    pending.replace(path)


def hidden_options():
    if os.name != "nt":
        return {}
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    return {"startupinfo": startup, "creationflags": subprocess.CREATE_NO_WINDOW}


def existing_unity(project):
    """Read command lines, never terminate or modify any process."""
    require(os.name == "nt", "Unity queue execution is supported on this Windows workstation only")
    command = ("$ErrorActionPreference='Stop'; [Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); @("  # No user text is interpolated into shell code.
               "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | "
               "Select-Object ProcessId,CommandLine) | ConvertTo-Json -Compress")
    query = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", command],
                           capture_output=True, text=True, encoding="utf-8", errors="replace",
                           timeout=30, **hidden_options())
    require(query.returncode == 0, "Cannot inspect existing Unity projects; no Unity launched")
    text = query.stdout.strip()
    rows = json.loads(text) if text else []
    rows = [] if rows is None else rows
    rows = rows if isinstance(rows, list) else [rows]
    busy = []
    for row in rows:
        require(isinstance(row, dict), "Cannot inspect an existing Unity process; no launch")
        line = row.get("CommandLine")
        require(isinstance(line, str) and line, "An existing Unity process has an unreadable command line; no launch")
        match = re.search(r'(?:^|\s)-projectPath\s+(?:"([^"]+)"|(\S+))', line, re.IGNORECASE)
        require(match is not None, "An existing Unity project could not be identified; no launch")
        current = Path(match.group(1) or match.group(2)).resolve()
        if current == project.resolve():
            busy.append(row["ProcessId"])
    return busy


def check_project_lock(project):
    """A stale lock file is okay only if Windows permits an exclusive read."""
    path = project / "Temp/UnityLockfile"
    if not path.exists() or os.name != "nt":
        return
    import ctypes
    from ctypes import wintypes
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.CreateFileW.argtypes = [wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD,
                                  wintypes.LPVOID, wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE]
    kernel.CreateFileW.restype = wintypes.HANDLE
    kernel.CloseHandle.argtypes = [wintypes.HANDLE]
    handle = kernel.CreateFileW(str(path), 0x80000000, 0, None, 3, 0x80, None)
    require(handle != wintypes.HANDLE(-1).value,
            f"Project UnityLockfile is locked/unreadable; no second Unity for {project}")
    kernel.CloseHandle(handle)


def snapshot(project, path, basis):
    candidates = list((project / "Assets").rglob("*.cs"))
    candidates += list((project / "Assets/Resources").rglob("*.json"))
    candidates += list((project / "ProjectSettings").glob("*.asset"))
    candidates += [project / "ProjectSettings/ProjectVersion.txt"]
    files = {}
    for source in sorted(set(candidates)):
        require(inside(source, project) and not source.is_symlink(), f"Source escaped project via link: {source}")
        files[source.relative_to(project).as_posix()] = digest(source)
    shaders = {}
    for pattern in ("*.shader", "*.cginc", "*.hlsl"):
        for source in sorted((project / "Assets").rglob(pattern)):
            require(inside(source, project) and not source.is_symlink(), f"Shader source escaped project: {source}")
            shaders[source.relative_to(project).as_posix()] = digest(source)
    value = {"schemaVersion": 1, "capturedUtc": iso(), "projectRoot": str(project),
             "captureBasis": basis, "files": files, "shaderFiles": shaders,
             "scope": "All Assets C#, Resources JSON, all ProjectSettings .asset files and ProjectVersion in files; all shader/cginc/hlsl sources in separate shaderFiles. No result is inferred from file identity."}
    write_json(path, value)
    return value, digest(path)


def source_changes(before, after):
    first = dict(before["files"], **before.get("shaderFiles", {}))
    second = dict(after["files"], **after.get("shaderFiles", {}))
    return [key for key in sorted(first.keys() | second.keys()) if first.get(key) != second.get(key)]


def validate_plan(project, queue, run_directory):
    require(inside(project, WORKSPACE), "Project must stay within the shared workspace")
    require(all((project / key).is_dir() for key in ("Assets", "Packages", "ProjectSettings")), "Not a Unity project")
    version = (project / "ProjectSettings/ProjectVersion.txt").read_text("utf-8-sig")
    require(re.search(r"^m_EditorVersion:\s*2022\.3\.28f1\s*$", version, re.MULTILINE) is not None,
            "Project is not set to the required Unity2022.3.28f1")
    require(UNITY.is_file(), f"Fixed Unity2022.3.28f1 executable missing: {UNITY}")
    require(inside(run_directory, MAIN_VALIDATION) and not run_directory.exists(), "Run directory must be new and below MainValidation")
    jobs = queue.get("jobs")
    require(isinstance(jobs, list) and jobs, "Queue needs a nonempty jobs array")
    require(type(queue.get("continueOnFailure", False)) is bool, "continueOnFailure must be boolean")
    planned, names, destinations = [], set(), set()
    for number, raw in enumerate(jobs, 1):
        require(isinstance(raw, dict), "Invalid job object")
        name, method = raw.get("name"), raw.get("method")
        require(isinstance(name, str) and NAME.fullmatch(name) is not None and name not in names, "Job names must be unique safe labels")
        require(isinstance(method, str) and METHOD.fullmatch(method) is not None, f"Invalid executeMethod:{name}")
        require(isinstance(raw.get("reportDirectory"), str) and isinstance(raw.get("archiveDestination"), str), f"Missing report/archive paths:{name}")
        report = (project / raw["reportDirectory"]).resolve()
        archive = (MAIN_VALIDATION / raw["archiveDestination"]).resolve()
        require(inside(report, project / "Validation"), f"Report directory must be within project Validation:{name}")
        require(inside(archive, MAIN_VALIDATION) and archive not in destinations and not archive.exists(), f"Archive must be new, unique and below MainValidation:{name}")
        require(not related(report, archive) and not related(report, run_directory), f"Nested report/archive/log copying rejected:{name}")
        require(not any(related(archive, previous) for previous in destinations), "Archive destinations cannot contain one another")
        timeout = raw.get("timeoutSeconds", 1800)
        require(type(timeout) in (int, float) and 0 < timeout <= 86400, f"Invalid timeoutSeconds:{name}")
        report_file = raw.get("reportFile")
        if report_file is not None:
            require(isinstance(report_file, str) and inside(report / report_file, report), f"reportFile escapes directory:{name}")
        names.add(name)
        destinations.add(archive)
        planned.append({"number": number, "name": name, "method": method,
                        "reportDirectory": str(report), "reportFile": report_file,
                        "archiveDestination": str(archive), "timeoutSeconds": timeout})
    return planned


def fresh_reports(job, launched):
    root = Path(job["reportDirectory"])
    require(root.is_dir(), f"Report directory missing after Unity exit:{root}")
    paths = [root / job["reportFile"]] if job["reportFile"] else sorted(root.rglob("*.json"))
    accepted, ignored = [], []
    for path in paths:
        require(inside(path, root), "Report link escaped its declared directory")
        document = json.loads(path.read_text("utf-8-sig"))
        if not isinstance(document, dict) or "startedUtc" not in document:
            if job["reportFile"]:
                raise QueueError(f"Primary report lacks startedUtc:{path}")
            continue
        started = timestamp(document.get("startedUtc"))
        if started < launched:
            ignored.append(str(path.relative_to(root)))
            continue
        finished = timestamp(document.get("finishedUtc"))
        require(started <= finished <= utc(), f"Invalid/unclosed fresh report timestamps:{path}")
        passed = document.get("passed") is True
        errors = document.get("runtimeErrors", document.get("errors", []))
        passed = passed and errors == []
        require(document.get("unityVersion") == VERSION, f"Fresh report has wrong/missing Unity version:{path}")
        accepted.append({"path": str(path), "relativePath": path.relative_to(root).as_posix(),
                         "sha256": digest(path), "startedUtc": document["startedUtc"],
                         "finishedUtc": document["finishedUtc"], "passed": passed,
                         "reportedPassed": document.get("passed"), "runtimeErrors": errors})
    require(bool(accepted), "No newly started AND finished report from this Unity job; stale reports never count")
    return accepted, ignored


def archive_reports(job):
    source, target = Path(job["reportDirectory"]), Path(job["archiveDestination"])
    require(source.is_dir() and not target.exists() and not related(source, target), "Unsafe/existing archive target")
    for item in source.rglob("*"):
        attributes = getattr(item.lstat(), "st_file_attributes", 0)
        require(not item.is_symlink() and not attributes & 0x400, f"Report archive contains a reparse/symlink:{item}")
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copytree(source, target)
    inventory = {path.relative_to(target).as_posix(): digest(path) for path in sorted(target.rglob("*")) if path.is_file()}
    return {"path": str(target), "files": inventory}


def run_queue(project, queue_path, run_directory, dry_run=False, continue_override=False):
    queue = json.loads(queue_path.read_text("utf-8-sig"))
    require(isinstance(queue, dict), "Queue must be a JSON object")
    if queue.get("project") is not None:
        require((queue_path.parent / queue["project"]).resolve() == project, "CLI project differs from queue project")
    jobs = validate_plan(project, queue, run_directory)
    record = {"schemaVersion": 1, "queuePath": str(queue_path), "queueSha256": digest(queue_path),
              "project": str(project), "unityExecutable": str(UNITY), "unityVersion": VERSION,
              "preparedUtc": iso(), "startedUtc": "", "finishedUtc": "", "passed": False,
              "dryRun": dry_run, "continueOnFailure": continue_override or queue.get("continueOnFailure", False),
              "jobs": jobs, "scope": "Serial jobs on one real project, with per-job immutable source snapshots and fresh report archives. No runtime/source promotion or report truth rewriting."}
    run_directory.mkdir(parents=True, exist_ok=False)
    state_path = run_directory / "queue-run.json"
    write_json(state_path, record)
    if dry_run:
        print(f"Validated queue only; no Unity launched: {state_path}")
        return 0
    lock_directory = MAIN_VALIDATION / "UnityQueueLocks"
    lock_directory.mkdir(exist_ok=True)
    lock_path = lock_directory / (hashlib.sha256(str(project).casefold().encode()).hexdigest() + ".json")
    token = uuid.uuid4().hex
    lock = os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    with os.fdopen(lock, "w", encoding="utf-8") as handle:
        json.dump({"runnerPid": os.getpid(), "project": str(project), "token": token, "createdUtc": iso()}, handle)
    record["startedUtc"] = iso()
    live_process = None
    try:
        for job in jobs:
            job["startedUtc"] = iso()
            try:
                busy = existing_unity(project)
                require(not busy, f"Unity already uses this exact project; PIDs={busy}")
                check_project_lock(project)
                folder = run_directory / (f"{job['number']:02d}-" + job["name"])
                folder.mkdir()
                before, before_hash = snapshot(project, folder / "sources-before.json", "Immediately before this queued Unity launch")
                job["beforeSnapshot"] = {"path": str(folder / "sources-before.json"), "sha256": before_hash}
                log = folder / "unity-2022.3.28f1.log"
                arguments = [str(UNITY), "-batchmode", "-projectPath", str(project), "-logFile", str(log), "-executeMethod", job["method"]]
                launched = utc()
                job["unityLaunchedUtc"] = launched.isoformat()
                job["logPath"] = str(log)
                # Do not use -quit: tests leave Play mode, restore prefs/scenes,
                # and call EditorApplication.Exit themselves after completion.
                live_process = subprocess.Popen(arguments, cwd=str(project), **hidden_options())
                job["unityPid"] = live_process.pid
                write_json(state_path, record)
                try:
                    job["exitCode"] = live_process.wait(timeout=job["timeoutSeconds"])
                except subprocess.TimeoutExpired:
                    job.update(passed=False, timedOut=True, liveUnityPid=live_process.pid,
                               failure="Timeout: Unity is still running; no kill issued and no next job launched.")
                    record["liveUnityPid"] = live_process.pid
                    write_json(state_path, record)
                    print(f"TIMEOUT, Unity left alive PID{live_process.pid}; retained lock:{lock_path}", flush=True)
                    return 3
                live_process = None
                after, after_hash = snapshot(project, folder / "sources-after.json", "After Unity self-exit, before report archival")
                job["afterSnapshot"] = {"path": str(folder / "sources-after.json"), "sha256": after_hash}
                job["changedSources"] = source_changes(before, after)
                try:
                    job["freshReports"], job["ignoredStaleReports"] = fresh_reports(job, launched)
                    job["passed"] = job["exitCode"] == 0 and not job["changedSources"] and all(item["passed"] for item in job["freshReports"])
                finally:
                    if Path(job["reportDirectory"]).is_dir():
                        job["archive"] = archive_reports(job)
            except Exception as error:
                job["passed"] = False
                job["failure"] = f"{type(error).__name__}: {error}"
                if live_process is not None and live_process.poll() is None:
                    job["liveUnityPid"] = record["liveUnityPid"] = live_process.pid
                    write_json(state_path, record)
                    print(f"Job error with Unity still alive PID{live_process.pid}; no kill/no next job. {state_path}", flush=True)
                    return 3
                if Path(job["reportDirectory"]).is_dir() and "archive" not in job and not Path(job["archiveDestination"]).exists():
                    try:
                        job["archive"] = archive_reports(job)
                    except Exception as archive_error:
                        job["archiveFailure"] = f"{type(archive_error).__name__}: {archive_error}"
            job["finishedUtc"] = iso()
            write_json(state_path, record)
            print(f"{'PASS' if job.get('passed') else 'FAIL'} {job['name']}; archive={job.get('archiveDestination')}; {state_path}", flush=True)
            if not job.get("passed") and not record["continueOnFailure"]:
                break
        record["finishedUtc"] = iso()
        record["passed"] = all(job.get("passed") is True for job in jobs)
        write_json(state_path, record)
        return 0 if record["passed"] else 1
    except KeyboardInterrupt:
        record["interruptedUtc"] = iso()
        if live_process is not None and live_process.poll() is None:
            record["liveUnityPid"] = live_process.pid
        write_json(state_path, record)
        print(f"Interrupted; no Unity terminated. Live PID={record.get('liveUnityPid')}; {state_path}", flush=True)
        return 130
    finally:
        # Never clear a protection lock while our Unity child is still alive.
        if live_process is None or live_process.poll() is not None:
            if lock_path.exists():
                saved = json.loads(lock_path.read_text("utf-8"))
                if saved.get("token") == token:
                    require(inside(lock_path, MAIN_VALIDATION / "UnityQueueLocks"), "Lock path escaped its intended directory")
                    lock_path.unlink()


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--queue", required=True, type=Path)
    parser.add_argument("--run-directory", type=Path)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--continue-on-failure", action="store_true")
    args = parser.parse_args(argv)
    project, queue = args.project.resolve(), args.queue.resolve()
    run_directory = (args.run_directory or MAIN_VALIDATION / "UnityQueueRuns" / (utc().strftime("%Y%m%dT%H%M%S.%fZ-") + queue.stem)).resolve()
    try:
        require(inside(queue, WORKSPACE), "Queue JSON must remain within workspace")
        return run_queue(project, queue, run_directory, args.dry_run, args.continue_on_failure)
    except (QueueError, OSError, ValueError, TypeError, KeyError) as error:
        print(f"Queue rejected: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
