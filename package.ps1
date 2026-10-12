param([string]$Destination)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
if (-not $Destination) { $Destination = Split-Path -Parent $taskRoot }
$taskDestination = [IO.Path]::GetFullPath($Destination)
New-Item -ItemType Directory -Force -Path $taskDestination | Out-Null
& (Join-Path $taskRoot 'build.ps1')
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskPortableFiles = @('六级词汇.exe','六级词汇.exe.config','README.md','使用说明.txt','LICENSE.txt','data\词库导入模板.csv','data\六级备考精选.csv')
$taskPortableFiles += Get-ChildItem -LiteralPath (Join-Path $taskRoot 'docs') -File | ForEach-Object { 'docs\' + $_.Name }
$taskSourceFiles = Get-ChildItem -LiteralPath $taskRoot -File -Recurse | Where-Object { $_.Extension -ne '.exe' -and $_.Extension -ne '.zip' } | ForEach-Object { $_.FullName.Substring($taskRoot.Length + 1) }
function Write-ProjectArchive([string]$taskPath,[string[]]$taskFiles) {
 $taskStream = [IO.File]::Open($taskPath,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
 $taskArchive = [IO.Compression.ZipArchive]::new($taskStream,[IO.Compression.ZipArchiveMode]::Create)
 try {
  foreach ($taskFile in ($taskFiles | Sort-Object)) {
   $taskEntry = 'CET6Word/' + $taskFile.Replace('\','/')
   [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskArchive,(Join-Path $taskRoot $taskFile),$taskEntry,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
  }
 } finally { $taskArchive.Dispose(); $taskStream.Dispose() }
}
$taskPortableZip = Join-Path $taskDestination 'CET6Word-V1.7-Windows.zip'
$taskSourceZip = Join-Path $taskDestination 'CET6Word-V1.7-Source.zip'
Write-ProjectArchive $taskPortableZip $taskPortableFiles
Write-ProjectArchive $taskSourceZip $taskSourceFiles
$taskChecksums = @((Get-FileHash -LiteralPath $taskPortableZip -Algorithm SHA256), (Get-FileHash -LiteralPath $taskSourceZip -Algorithm SHA256), (Get-FileHash -LiteralPath (Join-Path $taskRoot '六级词汇.exe') -Algorithm SHA256)) | ForEach-Object { $_.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_.Path) }
[IO.File]::WriteAllLines((Join-Path $taskDestination 'CET6Word-V1.7-SHA256.txt'),$taskChecksums,[Text.UTF8Encoding]::new($false))
Write-Output 'Portable and source archives created.'
