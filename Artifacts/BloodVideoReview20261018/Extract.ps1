param([double]$Step=1,[double]$Start=0,[double]$End=-1)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null=[Windows.Storage.StorageFile,Windows.Storage,ContentType=WindowsRuntime]
$null=[Windows.Media.Editing.MediaClip,Windows.Media,ContentType=WindowsRuntime]
$null=[Windows.Media.Editing.MediaComposition,Windows.Media,ContentType=WindowsRuntime]
$null=[Windows.Media.Editing.VideoFramePrecision,Windows.Media,ContentType=WindowsRuntime]
$null=[Windows.Graphics.Imaging.ImageStream,Windows.Graphics.Imaging,ContentType=WindowsRuntime]
$taskMethod=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'} | Select-Object -First 1
function Await-WinRT($operation,[Type]$type){$task=$taskMethod.MakeGenericMethod($type).Invoke($null,@($operation));$task.GetAwaiter().GetResult()}
$file=Await-WinRT ([Windows.Storage.StorageFile]::GetFileFromPathAsync('C:\Users\sakam\Videos\画面録画\画面録画 2026-10-18 024011.mp4')) ([Windows.Storage.StorageFile])
$clip=Await-WinRT ([Windows.Media.Editing.MediaClip]::CreateFromFileAsync($file)) ([Windows.Media.Editing.MediaClip])
$composition=New-Object Windows.Media.Editing.MediaComposition
$clipsProperty=[Windows.Media.Editing.MediaComposition].GetProperty("Clips"); $clipsProperty.PropertyType.GetMethod("Insert").Invoke($clipsProperty.GetValue($composition),@([int]0,$clip))
$duration=$composition.Duration.TotalSeconds
Write-Output "DurationSeconds=$duration"
if($End -lt 0){$End=$duration}
$folder=Join-Path $PSScriptRoot 'Frames';[System.IO.Directory]::CreateDirectory($folder) | Out-Null
for($t=$Start;$t -lt [Math]::Min($End,$duration);$t+=$Step){
    $frame=Await-WinRT ($composition.GetThumbnailAsync([TimeSpan]::FromSeconds($t),960,540,[Windows.Media.Editing.VideoFramePrecision]::NearestFrame)) ([Windows.Graphics.Imaging.ImageStream])
    $stream=[System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($frame)
    $path=Join-Path $folder ('frame_{0:000000}.jpg' -f [int]($t*1000))
    $output=[System.IO.File]::Create($path);try{$stream.CopyTo($output)}finally{$output.Dispose();$stream.Dispose()}
    Write-Output $path
}
