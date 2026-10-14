param([string]$DataDirectory, [string]$Backup, [switch]$Latest)
$ErrorActionPreference='Stop'
if(-not $DataDirectory){$DataDirectory=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'CET6Word'}
$taskDirectory=[IO.Path]::GetFullPath($DataDirectory)
if(-not (Test-Path -LiteralPath $taskDirectory -PathType Container)){throw '学习数据目录不存在。'}
if($Latest){$taskChoice=Get-ChildItem -LiteralPath $taskDirectory -Filter 'before-v2-*.cet6backup' -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1;if(-not $taskChoice){throw '没有找到升级前备份。请手动指定备份。'};$Backup=$taskChoice.FullName}
if(-not $Backup){throw '需要指定升级前完整备份，或使用 -Latest。'}
$taskLock=$null
try{
 $taskLock=[IO.File]::Open((Join-Path $taskDirectory 'instance.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 $taskEnvelope=Get-Content -LiteralPath $Backup -Raw -Encoding UTF8 | ConvertFrom-Json
 $taskHashProvider=[Security.Cryptography.SHA256]::Create()
 try{$taskHash=([BitConverter]::ToString($taskHashProvider.ComputeHash([Text.Encoding]::UTF8.GetBytes($taskEnvelope.Data)))).Replace('-','').ToLowerInvariant()}finally{$taskHashProvider.Dispose()}
 if($taskEnvelope.Format -ne 'CET6Word.FullBackup' -or $taskEnvelope.Version -ne 1 -or $taskHash -ne $taskEnvelope.Sha256){throw '备份校验失败，未修改数据。'}
 $taskState=$taskEnvelope.Data | ConvertFrom-Json
 if($taskState.SchemaVersion -ne 1){throw '这不是 V1.7 升级前备份，未修改数据。'}
 $taskFile=Join-Path $taskDirectory 'state.json'
 $taskSafety=Join-Path $taskDirectory ('before-rollback-'+(Get-Date -Format 'yyyyMMdd-HHmmssfff')+'.json')
 if(Test-Path -LiteralPath $taskFile){Copy-Item -LiteralPath $taskFile -Destination $taskSafety}
 $taskTemporary=Join-Path $taskDirectory ('rollback-'+[Guid]::NewGuid().ToString('N')+'.tmp')
 [IO.File]::WriteAllText($taskTemporary,$taskEnvelope.Data,[Text.UTF8Encoding]::new($false))
 if(Test-Path -LiteralPath $taskFile){[IO.File]::Replace($taskTemporary,$taskFile,$taskFile+'.previous',$true)}else{[IO.File]::Move($taskTemporary,$taskFile)}
 Write-Output ('已恢复升级前档案。升级后副本保留在：'+$taskSafety)
}finally{if($taskLock){$taskLock.Dispose()}}
