param([switch]$Test,[string]$TestOutput)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$taskSdk = (& dotnet --list-sdks | Select-Object -Last 1)
if (-not $taskSdk) { throw 'Rebuilding requires the .NET SDK. Running the app does not require the SDK.' }
$taskVersion = ($taskSdk -split ' ')[0]
$taskSdkBase = [regex]::Match($taskSdk, '\[(.+)\]').Groups[1].Value
$taskCompiler = Join-Path $taskSdkBase "$taskVersion\Roslyn\bincore\csc.dll"
$taskRefs = @('mscorlib','System','System.Core','System.Xml','System.Xml.Linq','System.Web.Extensions','System.Security','System.IO.Compression','System.IO.Compression.FileSystem') | ForEach-Object { '/reference:' + (Join-Path $taskFramework ($_.ToString()+'.dll')) }
$taskRefs += @('PresentationCore','PresentationFramework','WindowsBase','System.Xaml','System.Speech') | ForEach-Object { $taskAssembly = Join-Path $taskFramework ('WPF\'+$_.ToString()+'.dll'); if (-not (Test-Path -LiteralPath $taskAssembly)) { $taskAssembly = Join-Path $taskFramework ($_.ToString()+'.dll') }; '/reference:' + $taskAssembly }
$taskSourceFiles = @('Core.cs','Storage.cs','Migration.cs','Imports.cs','Speech.cs','Program.cs','UI.cs','PaginationUI.cs','Animation.cs','AssemblyInfo.cs') | ForEach-Object { Join-Path $taskRoot ('src\'+$_) }
$taskCommon = @('/nologo','/nostdlib+','/langversion:latest','/optimize+','/utf8output','/platform:anycpu',('/resource:'+(Join-Path $taskRoot 'data\builtin.json')+',builtin.json'),('/resource:'+(Join-Path $taskRoot 'src\MainWindow.xaml')+',MainWindow.xaml'))
if ($Test) {
 $taskWork = if ($TestOutput) { [IO.Path]::GetFullPath($TestOutput) } else { Join-Path ([IO.Path]::GetTempPath()) ('CET6Word-tests-'+[guid]::NewGuid().ToString('N')) }
 New-Item -ItemType Directory -Force -Path $taskWork | Out-Null
 $taskTestExe = Join-Path $taskWork 'CET6Word.Tests.exe'
 & dotnet $taskCompiler @taskCommon @taskRefs @taskSourceFiles (Join-Path $taskRoot 'tests\Tests.cs') (Join-Path $taskRoot 'tests\UITests.cs') (Join-Path $taskRoot 'tests\V2Tests.cs') (Join-Path $taskRoot 'tests\V2UITests.cs') ('/resource:'+(Join-Path $taskRoot 'tests\fixtures\v1-builtin.json')+',v1-builtin.json') '/target:exe' '/main:CET6Word.Tests' ('/out:'+$taskTestExe)
 if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
 & $taskTestExe (Join-Path $taskWork 'automated-tests')
 if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
} else {
 & dotnet $taskCompiler @taskCommon @taskRefs @taskSourceFiles '/target:winexe' ('/win32manifest:'+(Join-Path $taskRoot 'src\app.manifest')) ('/out:'+(Join-Path $taskRoot '六级词汇.exe'))
 if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}
