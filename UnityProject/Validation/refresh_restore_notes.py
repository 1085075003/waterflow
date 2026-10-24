"""Refresh user notes and the recovery inventory from actual reports, including partial runs."""
import json
import runpy
import time
from datetime import datetime, timezone, timedelta
from pathlib import Path

validation = Path(__file__).resolve().parent
project = validation.parent
workspace = project.parent
runpy.run_path(str(validation / "refresh_evidence_summary.py"))
summary = json.loads((validation / "validation-summary.json").read_text(encoding="utf-8"))
reports = summary["reports"]

def read(relative):
    for attempt in range(5):
        try:
            return json.loads((validation / relative).read_text(encoding="utf-8-sig"))
        except (OSError, ValueError):
            if attempt == 4:
                return {}
            time.sleep(.1)

def line(kind, title):
    item = reports.get(kind, {})
    if not item.get("exists"):
        result = "待验证"
    elif not item.get("finishedUtc"):
        result = "进行中"
    else:
        result = "通过" if item.get("passed") else "未通过"
    detail = f"{item.get('checks', 0)}项检查" if item.get("checks") else ""
    if kind == "normalGameplay":
        detail = f"真实拖动通过{item.get('levelsPassed', 0)}/{item.get('plannedLevels', 800)}关，失败{len(item.get('levelsFailed', []))}关"
        if item.get("aggregatedVerification"):
            detail += "；两个独立400关及明确复测的汇总，各次原始记录保留"
    elif kind == "allLayouts":
        full = read(item.get("path", "FullContent/all-runtime-validation.json"))
        if not any(x.get("structuralVerticesChecked") for x in full.get("levels", [])):
            result = "旧快照，严格网格与新效果全量复验待完成"
        detail = f"已完成{item.get('completed', 0)}/{item.get('requested', 1350)}关，失败{len(item.get('levelsFailed', []))}关"
    elif kind == "nativeRecipeRegression":
        detail = f"几何修复影响范围及历史失败独立回归{item.get('levelsPassed', 0)}/169关，2616条原始步骤；不替代800关主覆盖"
    elif kind == "windowsBuild":
        detail = f"Windows64实际构建，{item.get('outputFiles', 0)}个文件，{item.get('outputBytes', 0)}字节；未据此宣称独立播放器交互通过"
    elif kind == "media":
        detail = f"{item.get('assetsChecked', 0)}项媒体资源，失败{item.get('failedAssets', 0)}"
    elif kind == "visualReview":
        detail = f"逐帧复核{item.get('framesReviewed', 0)}/{item.get('expectedFrames', 2700)}张；与本轮报告及图片SHA绑定"
    return f"{title}：{result}；{detail}；Validation/{item.get('path', kind)}；结束UTC {item.get('finishedUtc') or '尚未结束'}"

date = datetime.now(timezone(timedelta(hours=8))).strftime("%Y-%m-%d %H:%M:%S Asia/Shanghai")
complete = summary["allRequestedSuitesPassed"]
state = "所列自动验证套件全部通过" if complete else "本轮验证仍在进行；未完成项不计为通过"
data = read("data-validation.json").get("dataReplay", {})
evidence = "\n".join(line(kind, title) for kind, title in (
    ("assets", "场景与资源引用"), ("data", "全部关卡数据"), ("media", "模型、贴图、音频、材质与动画资源"),
    ("normalGameplay", "800关普通游玩"), ("allLayouts", "1350关起始与解答实际渲染"),
    ("nativeRecipeRegression", "原几何修复后的169关普通游玩"),
    ("nativeGeometryPose", "原几何坐标与预览代理边界"), ("windowsBuild", "Windows构建"),
    ("hudFraming", "桌面原HUD可见性与首次进关标题"),
    ("visualReview", "本轮全部实际画面复核"),
    ("ui", "原界面交互"), ("interaction", "真实输入与撤销"), ("cameraTutorial", "原相机与教程"),
    ("springs", "弹簧机构"), ("springModes", "机构模式"), ("springConstraints", "机构碰撞约束"),
    ("waterPropagation", "水流传播与恢复"), ("bottomMarker", "底口与瀑布几何接水"),
    ("pfuenteMarker", "喷泉遮挡与独立接水"), ("standaloneFountain", "独立喷泉标记与颜色"),
    ("jetLifetime", "喷口复用与实际粒子快照"),
    ("fountainAnimation", "原喷泉动画"), ("nativeStepPlayback", "连续解答动画"),
    ("fountainDispatch", "喷泉类型、灯与粒子颜色"), ("decorativePits", "装饰水柱动画"),
    ("waterUV", "原水纹UV与渲染时钟"),
    ("uiAnimations", "原结算与星星动画"), ("localization", "原语言与真实字形"),
    ("nativeStepMechanisms", "解答机构等待"), ("nativeStepLifts", "解答抬升例外"),
    ("nativeStepMissing", "原后续拾取记录"), ("nativeCaptureLifetime", "空拾取状态生命周期"),
    ("categoryPreview", "分类水景与机构演示"), ("aspect", "窗口比例")))
limits = """新增运行层依据原资产、存档与IL2CPP ARM64方法证据重建，并非找回原始C#工程源码。通过列明测试不等于证明与原Android二进制逐帧完全相同。
原Shader依据包内GLSL及原材质参数重建；Windows与原移动GPU的逐像素等价性未建立。原包中的无效动画路径保留溯源，不随意映射到新对象。
桌面增加原相机沿视线后移的横屏适配，以及鼠标操作；原FOV与姿态保留。原场景竖屏不受支持，竖屏截图仅作观察。
本地开放已有的27组关卡及保存解答，解答数量显示1；这是桌面重建适配，未还原原商店、广告奖励与解答消耗经济。
广告、内购、统计及联网服务没有连接真实SDK业务。原包本身的BuildSettings仅有9个场景，没有可运行的关卡编辑器场景；已按原方法恢复设计入口面板、状态按钮及原选关路由，不虚构编辑器。
本机2022.3.28f1仅安装Windows与WebGL支持，未安装Android模块，本轮未验证Android构建。"""
notes = f"""Flow Water 2.18 — Unity 2022.3.28f1 恢复与验证
说明刷新：{date}
当前状态：{state}。具体覆盖范围以各报告结束时间、源码快照及实际结果为准。

用Unity Hub添加本目录的UnityProject，选择本机2022.3.28f1。打开Assets/Scenes/Portada.unity按Play，从原首页、六分类及50关一组的选关页进入游戏。资源gm编号与组内LEVEL编号不同。
鼠标按住可移动拼块，沿原落点引导释放。音乐、音效、重开、灯泡解答、关卡菜单及下一关使用原界面按钮。右键可旋转视角，R重开、Ctrl+Z撤销；不再使用早期替代HUD。
关卡完成依据实际目标接到正确颜色的水及原延时判定。灯泡进入保存解答；有Save1记录时提供步骤演示，退出恢复原棋盘及撤销状态。
语言按原首入系统语言、原按钮顺序和返回首页规则处理；原29个翻译方法、字号与字形另有真实界面报告。部分STORE/RATE/RESET是原英文美术图片，保留原资源。

已恢复原场景、Canvas、相机、地面、背景、光照、日夜主题、模型、纹理、材质、动画及音频资源。原9场景、529预制体、1350对gm/so数据与498别名保留。
原配置前缀、固定块、地下边墙、输入拾取/抬升/下落、弹簧与门孔/喷流通道按原ARM64方法证据重建。水流使用原水片、粒子、开口、颜色与实际延时传播前沿；不会用湿水数量或摆放正确直接冒充目标通关。
53处材质槽已追溯原同网格来源修复。静态几何重放当前通过{data.get('success', 0)}/{data.get('total', 1350)}，不支持{data.get('unsupported', 0)}，失败{data.get('failure', 0)}；这与下面的普通游玩验证分开记录。

实际证据
{evidence}

Validation/validation-summary.json汇总当前报告；FullContent/review.html提供逐关画面对照（须与最新全量报告对应）。NativeAnalysis、ShaderAnalysis保留原方法与着色器证据。旧日志、历史截图及InstantBaselineCompleted只证明当时的源码，不作为当前全量通过证明。
复验前关闭占用该工程的Unity。运行Validation/Validate-Unity.ps1 -Mode Assets、NativeGameplay、All、UI、Input、CameraTutorial、Preview或Propagation；各模式由指定2022.3.28f1实际执行并保存日志。

边界与来源
{limits}
原包使用2022.3.62f2，工作工程按要求使用2022.3.28f1；原版本备份在Validation/ProjectVersion.original.txt。
源包Flow-Water-Fountain-3D-Puzzle_2.18_apkcube.apks，SHA256 74bb3e4721de868450ecc415d3deb00e3b64d1c07c42f7aa7fa67492e3cbdd03。
raw、unity、LevelData、android_decoded与inventory保留原始来源，未因运行层重建覆盖。早期文件备份位于UnityProject/Validation/BeforeRuntimeRepair。
"""
(workspace / "恢复情况与使用说明.txt").write_text(notes, encoding="utf-8-sig")
(project / "工程恢复说明.txt").write_text(f"""Flow Water 2.18 — Unity 2022.3.28f1工作工程
说明刷新：{date}
当前状态：{state}。

打开Assets/Scenes/Portada.unity按Play，从原首页、六分类、选关进入游戏。拖动拼块并在原引导上释放，灯泡打开保存解答，菜单/重开/下一关使用原界面。
当前运行层位于Assets/Scripts/RecoveryRuntime与RecoveryUI；验证位于Assets/Editor/RecoveredTools。这些是根据原数据与ARM64方法证据重建的实现。
实际报告与逐关渲染在Validation目录，摘要为Validation/validation-summary.json，画面对照为Validation/FullContent/review.html。
普通游玩：{line('normalGameplay', '800关')}
全部画面：{line('allLayouts', '1350关')}
完整使用说明、各套件计数、来源与限制见上一级“恢复情况与使用说明.txt”。

{limits}
""", encoding="utf-8-sig")

inventory_path = workspace / "inventory/project_restore.json"
inventory = json.loads(inventory_path.read_text(encoding="utf-8-sig"))
inventory.setdefault("initial_export_warnings", inventory.get("warnings", []))
inventory["completed_scope"] = "AssetRipper export completion only; not full original-game behavioral equivalence."
inventory["restoration_complete"] = False
inventory["local_validation_complete"] = complete
inventory["updated_local_time"] = date
inventory["warnings"] = limits.splitlines()
editor = inventory.setdefault("unity_editor_test", {})
for key in ("play_checks_passed", "play_check_count", "play_runtime_error_count"):
    editor.pop(key, None)
editor.update({"asset_audit_checks": reports.get("assets", {}).get("checks", 0),
    "asset_audit_passed": reports.get("assets", {}).get("passed", False),
    "data_geometric_replay_success": data.get("success", 0),
    "data_geometric_replay_unsupported": data.get("unsupported", 0),
    "data_geometric_replay_failure": data.get("failure", 0),
    "validation_summary": "UnityProject/Validation/validation-summary.json",
    "actual_report_results": reports, "original_gameplay_fully_restored": False})
runtime = inventory.setdefault("runtime_reconstruction", {})
runtime["unsupported_asset_ids"] = [e["assetId"] for e in data.get("entries", []) if e.get("classification") == "unsupported"]
runtime["ordinary_gameplay_800_passed"] = reports.get("normalGameplay", {}).get("all800ProductionPointerDrag", False)
runtime["all_1350_latest_runtime_passed"] = reports.get("allLayouts", {}).get("passed", False)
runtime["native_evidence"] = "UnityProject/Validation/NativeAnalysis"
inventory_path.write_text(json.dumps(inventory, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"notesUpdated": True, "allRequestedSuitesPassed": complete}, ensure_ascii=False))
