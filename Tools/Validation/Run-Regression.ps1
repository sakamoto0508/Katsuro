param(
    [string]$UnityPath = 'C:/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot 'Temp/RefactorRegression'
$testLog = Join-Path $projectRoot 'Temp/refactor-regression.log'
New-Item -ItemType Directory -Force "$testRoot/Assets/Editor", "$testRoot/Packages", "$testRoot/ProjectSettings" | Out-Null
'{"dependencies":{"com.unity.modules.physics":"1.0.0"}}' | Set-Content "$testRoot/Packages/manifest.json"
Copy-Item "$projectRoot/ProjectSettings/ProjectVersion.txt" "$testRoot/ProjectSettings/ProjectVersion.txt"
$testSources = @(
    'Enemy/EnemyAI/EnemyDecisionMaker.cs',
    'Enemy/EnemyConfig/EnemyDecisionConfig.cs',
    'Enemy/EnemyAction/EnemyActionType.cs',
    'Enemy/EnemyWeapon.cs',
    'Player/WeaponHitboxRelay.cs', 'Feedback/SwordTrail.cs'
)
foreach ($source in $testSources) {
    Copy-Item "$projectRoot/Assets/Mock/Scripts/$source" "$testRoot/Assets"
}
Copy-Item "$PSScriptRoot/RegressionRunner.cs" "$testRoot/Assets/Editor"
$arguments = '-batchmode -nographics -projectPath "{0}" -executeMethod RegressionRunner.Run -logFile "{1}"' -f $testRoot, $testLog
$testProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$testProcess.WaitForExit()
Get-Content $testLog | Select-String 'PASS:|REGRESSION_RESULT:|error CS|Exception'
if ($testProcess.ExitCode -ne 0) { throw "Unity regression tests failed. See $testLog" }
if (!(Select-String -Path $testLog -Pattern 'REGRESSION_RESULT: 14 passed' -Quiet)) {
    throw "Unity did not report a completed regression run. See $testLog"
}

