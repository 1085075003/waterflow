param(
    [ValidateSet('Import', 'Audit', 'Data', 'Play', 'Catalog', 'Media', 'Representative', 'All', 'Assets', 'UI', 'Spring', 'SpringModes', 'SpringConstraints', 'Aspect', 'Input', 'Preview', 'Propagation', 'Gameplay', 'GameplayFocused', 'GameplayRegressions', 'NativeGameplay', 'NativeFocused', 'CameraTutorial', 'FountainAnimation', 'Failures')]
    [string]$Mode = 'Assets',
    [string]$UnityPath = 'D:\UnityHub\Editor\2022.3.28f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity 2022.3.28f1 was not found: $UnityPath"
}
$logPath = Join-Path $PSScriptRoot ($Mode.ToLowerInvariant() + '-2022.3.28f1.log')
$arguments = @('-batchmode', '-projectPath', ('"' + $projectPath + '"'), '-logFile', ('"' + $logPath + '"'))
switch ($Mode) {
    'Import' { $arguments += '-quit' }
    'Catalog' { $arguments += @('-quit', '-executeMethod', 'Recovered.FlowWater.RecoveryCatalogBuilder.Build') }
    'Audit' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterValidation.RunAudit') }
    'Data' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterValidation.RunDataValidation') }
    'Play' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterValidation.RunPlayValidation') }
    'Media' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterMediaAudit.Run') }
    'Representative' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterFullValidation.RunRepresentative') }
    'All' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterFullValidation.RunAll') }
    'Assets' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterValidation.RunAssetSuite') }
    'UI' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterUIValidation.Run') }
    'Spring' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterSpringValidation.Run') }
    'SpringModes' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterSpringModeValidation.Run') }
    'SpringConstraints' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterSpringConstraintValidation.Run') }
    'Aspect' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterAspectValidation.Run') }
    'Input' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterInteractionValidation.Run') }
    'Preview' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterPreviewValidation.Run') }
    'Propagation' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterPropagationValidation.Run') }
    'Gameplay' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunAll') }
    'GameplayFocused' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunFocused') }
    'GameplayRegressions' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunFailuresAndFocused') }
    'NativeGameplay' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeAll') }
    'NativeFocused' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeFocused') }
    'CameraTutorial' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterCameraTutorialValidation.Run') }
    'FountainAnimation' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterFountainAnimationValidation.Run') }
    'Failures' { $arguments += @('-executeMethod', 'Recovered.FlowWater.Editor.FlowWaterFullValidation.RunFailures') }
}
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
Write-Output "Unity $Mode exited with code $($process.ExitCode). Log: $logPath"
exit $process.ExitCode
