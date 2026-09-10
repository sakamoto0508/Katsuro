param([string]$UnityPath = 'C:/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $rootPath 'Temp/GameplayRegression'
$testLog = Join-Path $rootPath 'Temp/gameplay-regression.log'
New-Item -ItemType Directory -Force "$testRoot/Assets/Editor", "$testRoot/Assets/Plugins", "$testRoot/Packages", "$testRoot/ProjectSettings" | Out-Null
$manifest = Get-Content "$rootPath/Packages/manifest.json" -Raw | ConvertFrom-Json
$modules = [ordered]@{}
foreach ($entry in $manifest.dependencies.PSObject.Properties) { if ($entry.Name.StartsWith('com.unity.modules.')) { $modules[$entry.Name] = $entry.Value } }
@{dependencies=$modules} | ConvertTo-Json | Set-Content "$testRoot/Packages/manifest.json"
Copy-Item "$rootPath/ProjectSettings/ProjectVersion.txt" "$testRoot/ProjectSettings/ProjectVersion.txt"
"%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!129 &1`nPlayerSettings:`n  companyName: KatsuroValidation`n  productName: GameplayRegression" | Set-Content "$testRoot/ProjectSettings/ProjectSettings.asset"
$sources = @('Progression/GameplayRules.cs','Progression/RunSession.cs','Player/PlayerAction/PlayerGhost.cs','Player/PlayerAction/AbilityBase.cs','Player/SkillGauge.cs','Config/PlayerStatus.cs','Config/LowHpBuffTable.cs','Config/JustAvoidBuffConfig.cs','Player/PlayerAction/PlayerSelfSacrifice.cs')
foreach ($source in $sources) { Copy-Item "$rootPath/Assets/Mock/Scripts/$source" "$testRoot/Assets" }
Copy-Item "$rootPath/Library/ScriptAssemblies/UniRx.dll", "$rootPath/Library/ScriptAssemblies/UnityEngine.UI.dll" "$testRoot/Assets/Plugins"
Copy-Item "$PSScriptRoot/GameplayRunner.cs" "$testRoot/Assets/Editor"
$arguments = '-batchmode -nographics -projectPath "{0}" -executeMethod GameplayRunner.Run -logFile "{1}"' -f $testRoot, $testLog
$testProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$testProcess.WaitForExit()
Get-Content $testLog | Select-String 'PASS:|GAMEPLAY_RESULT:|error CS|Exception'
if ($testProcess.ExitCode -ne 0 -or !(Select-String -Path $testLog -Pattern 'GAMEPLAY_RESULT: 30 passed' -Quiet)) { throw "Gameplay tests failed. See $testLog" }
