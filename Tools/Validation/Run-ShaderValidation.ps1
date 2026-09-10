param([string]$UnityPath = 'C:/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $rootPath 'Temp/ShaderValidation'
$testLog = Join-Path $rootPath 'Temp/shader-validation.log'
New-Item -ItemType Directory -Force "$testRoot/Assets/Editor", "$testRoot/Assets/Resources", "$testRoot/Packages", "$testRoot/ProjectSettings" | Out-Null
$available = @{}
Get-ChildItem "$rootPath/Library/PackageCache" -Directory | ForEach-Object {
    $jsonPath = Join-Path $_.FullName 'package.json'
    if (Test-Path $jsonPath) {
        $json = Get-Content $jsonPath -Raw | ConvertFrom-Json
        $available[$json.name] = @{ path = $_.FullName; package = $json }
    }
}
$dependencies = [ordered]@{}
function Add-Package($name) {
    if ($dependencies.Contains($name)) { return }
    if ($name.StartsWith('com.unity.modules.')) { $dependencies[$name] = '1.0.0'; return }
    if (!$available.ContainsKey($name)) { throw "Missing cached package: $name. Open the main project in Unity first." }
    $entry = $available[$name]
    $dependencies[$name] = 'file:' + $entry.path.Replace('\', '/')
    foreach ($dep in $entry.package.dependencies.PSObject.Properties) { Add-Package $dep.Name }
}
Add-Package 'com.unity.render-pipelines.high-definition'
@{ dependencies = $dependencies } | ConvertTo-Json | Set-Content "$testRoot/Packages/manifest.json"
Copy-Item "$rootPath/ProjectSettings/ProjectVersion.txt" "$testRoot/ProjectSettings"
Copy-Item "$rootPath/Assets/Mock/Resources/CombatGlow.shader" "$testRoot/Assets/Resources"
Copy-Item "$PSScriptRoot/ShaderRunner.cs" "$testRoot/Assets/Editor"
$arguments = '-batchmode -projectPath "{0}" -executeMethod ShaderRunner.Run -logFile "{1}"' -f $testRoot, $testLog
$process = Start-Process $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
Get-Content $testLog | Select-String 'SHADER_RESULT:|error CS|Shader error|Exception'
if ($process.ExitCode -ne 0 -or !(Select-String $testLog -Pattern 'SHADER_RESULT: passed' -Quiet)) { throw "Shader validation failed: $testLog" }
