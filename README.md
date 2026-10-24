# Flow Water 2.18 恢复工程

本仓库保存当前恢复工程、原始导出资源、关卡数据和分析/验证脚本。它是依据原资源和原生方法证据重建的工程，并非原作者的完整原始 C# 源码；已知限制和历史验证结果见《恢复情况与使用说明.txt》。

## 拉取与打开

1. 安装 Git、Unity Hub 和 **Unity 2022.3.28f1**。Windows 构建需要对应 Windows Build Support，Android 构建需额外安装 Android Build Support、SDK、NDK 和 OpenJDK。
2. 执行 `git clone -c core.longpaths=true https://github.com/1085075003/waterflow.git`。
3. 在 Unity Hub 中添加仓库内的 **UnityProject** 文件夹（不要添加仓库根目录）。
4. 使用 2022.3.28f1 打开，等待 Unity 下载 manifest 中固定版本的包并重新生成 Library。首次导入需要联网。
5. 打开 `Assets/Scenes/Portada.unity`，按 Play 从首页进入关卡。

项目直接包含 Assets 及其 .meta、Packages/manifest.json、packages-lock.json 和 ProjectSettings；不使用 Git LFS 或 Git 子模块，无需另外复制本机资源。Unity 编辑器和第三方包由对应官方安装/包管理流程提供。

## 目录

- `UnityProject/`：可在 Unity Hub 打开的工作工程；Assets、Packages、ProjectSettings 为核心内容。
- `UnityProject/Validation/`：验证脚本、报告、分析依据及历史源码快照。历史报告只证明当时状态，不代表当前代码全部通过。
- `raw/`、`unity/`、`android_decoded/`：原始二进制、资源导出及 Android 解码资料。
- `LevelData/`：1350 份布局和对应解答及索引。
- `inventory/`：资源结构、组件类型、拼块映射。
- `Tools/`：辅助处理脚本。

不提交 Unity 的 Library/Temp/Logs/UserSettings、构建输出、本机 Android 工具链、重复验证工程、Python 运行时和验证截图/视频/临时编译文件。这些不是打开工程所需的源资源；原始 Assets 中的图片、音频、模型与插件仍完整保留。

## 验证

关闭占用工程的 Unity 后，在 PowerShell 执行（将 UnityPath 替换为自己的实际安装路径）：

```powershell
.\UnityProject\Validation\Validate-Unity.ps1 -Mode Assets -UnityPath 'C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Unity.exe'
```

其他模式见该脚本参数。部分历史分析脚本包含原机器的绝对路径，重跑历史分析时需按本机环境调整；正常从首页运行不依赖这些脚本。

## 来源与边界

原包版本使用 Unity 2022.3.62f2；工作工程使用 2022.3.28f1。广告、内购、统计等真实在线业务未连接。完整恢复边界见根目录说明和 UnityProject/工程恢复说明.txt。本次仓库整理不重新宣称历史验证通过，也不改变原有资源的权利归属。

拉取后可执行 pwsh -File .\Tools\Verify-Repository.ps1 核对核心文件是否全部受 Git 跟踪及 Git 对象完整性。



