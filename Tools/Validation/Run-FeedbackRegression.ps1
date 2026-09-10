param([string]$UnityPath = 'C:/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $rootPath 'Temp/FeedbackRegression'
$testLog = Join-Path $rootPath 'Temp/feedback-regression.log'
New-Item -ItemType Directory -Force "$testRoot/Assets/Editor", "$testRoot/Packages", "$testRoot/ProjectSettings" | Out-Null
$manifest = Get-Content "$rootPath/Packages/manifest.json" -Raw | ConvertFrom-Json
$modules = [ordered]@{}
foreach ($entry in $manifest.dependencies.PSObject.Properties) {
    if ($entry.Name.StartsWith('com.unity.modules.')) { $modules[$entry.Name] = $entry.Value }
}
@{ dependencies = $modules } | ConvertTo-Json | Set-Content "$testRoot/Packages/manifest.json"
Copy-Item "$rootPath/ProjectSettings/ProjectVersion.txt" "$testRoot/ProjectSettings"
foreach ($source in @('Managers/AnimationSpeedController.cs', 'Managers/HitStopManager.cs', 'StatusEffect/StatusEffectManager.cs', 'StatusEffect/StatusEffectDef.cs', 'StatusEffect/StatusEffectInstance.cs', 'StatusEffect/IStatusEffectReceiver.cs')) {
    Copy-Item "$rootPath/Assets/Mock/Scripts/$source" "$testRoot/Assets"
}
Copy-Item "$PSScriptRoot/FeedbackRunner.cs" "$testRoot/Assets/Editor"
$arguments = '-batchmode -nographics -projectPath "{0}" -executeMethod FeedbackRunner.Run -logFile "{1}"' -f $testRoot, $testLog
$process = Start-Process $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
Get-Content $testLog | Select-String 'PASS:|FEEDBACK_RESULT:|error CS|Exception'
if ($process.ExitCode -ne 0 -or !(Select-String $testLog -Pattern 'FEEDBACK_RESULT: 10 passed' -Quiet)) { throw "Feedback tests failed: $testLog" }
