$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$expectedRemote = 'https://github.com/klsdxf/English-Vocabulary-Memorization.git'

try {
    Set-Location -LiteralPath $project

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw '未找到 Git。请先安装 Git for Windows。'
    }

    if (-not (Test-Path -LiteralPath (Join-Path $project '.git'))) {
        throw '项目尚未完成首次 GitHub 连接。'
    }

    $repoRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $repoRoot) {
        throw '项目尚未完成首次 GitHub 连接。'
    }
    if ([IO.Path]::GetFullPath($repoRoot).TrimEnd('\', '/') -ine [IO.Path]::GetFullPath($project).TrimEnd('\', '/')) {
        throw '当前 Git 仓库根目录与此项目文件夹不一致，已停止推送。'
    }

    $remote = (& git remote get-url origin 2>$null)
    if ($LASTEXITCODE -ne 0 -or $remote -notin @($expectedRemote, 'https://github.com/klsdxf/English-Vocabulary-Memorization', 'git@github.com:klsdxf/English-Vocabulary-Memorization.git')) {
        throw "origin 不是预期的 GitHub 仓库。当前值：$remote"
    }

    & git add --all
    if ($LASTEXITCODE -ne 0) { throw '添加文件失败。' }

    $staged = @(& git -c core.quotepath=false diff --cached --name-only)
    if ($LASTEXITCODE -ne 0) { throw '读取待提交文件失败。' }
    foreach ($file in $staged) {
        $name = [IO.Path]::GetFileName($file)
        if ($name -match '^(?:\.env(?:\..*)?|state\.json|speech-key\.bin|id_rsa|id_ed25519|credentials.*|secrets.*)$|\.(?:pem|key|pfx|p12|kdbx|cet6backup|sqlite3?|db)$') {
            throw "待提交文件可能包含私人数据：$file"
        }
    }

    & git diff --cached --check
    if ($LASTEXITCODE -ne 0) { throw '待提交文件检查失败，请修正后重试。' }

    & git grep --cached -I -q -E 'ghp_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA |OPENSSH |EC )?PRIVATE KEY-----' -- .
    if ($LASTEXITCODE -eq 0) { throw '检测到疑似密钥，已停止推送。' }
    if ($LASTEXITCODE -ne 1) { throw '密钥检查未能完成，已停止推送。' }

    & git diff --cached --quiet
    if ($LASTEXITCODE -eq 1) {
        & git commit -m 'Update CET6 vocabulary app'
        if ($LASTEXITCODE -ne 0) { throw '提交失败。请检查 Git 用户名和邮箱设置。' }
    } elseif ($LASTEXITCODE -ne 0) {
        throw '检查待提交改动失败。'
    }

    if (-not $env:HTTPS_PROXY -and -not $env:https_proxy) {
        $proxySettings = Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' -ErrorAction SilentlyContinue
        if ($proxySettings -and $proxySettings.ProxyEnable -and $proxySettings.ProxyServer) {
            $proxyEntries = [string]$proxySettings.ProxyServer
            $proxy = $null
            if ($proxyEntries -match '(?:^|;)https=([^;]+)') {
                $proxy = $Matches[1]
            } elseif ($proxyEntries -match '(?:^|;)http=([^;]+)') {
                $proxy = $Matches[1]
            } elseif ($proxyEntries -notmatch '=') {
                $proxy = $proxyEntries
            }
            if ($proxy) {
                if ($proxy -notmatch '^[a-zA-Z][a-zA-Z0-9+.-]*://') { $proxy = 'http://' + $proxy }
                $env:HTTPS_PROXY = $proxy
            }
        }
    }

    & git push --set-upstream origin HEAD
    if ($LASTEXITCODE -ne 0) { throw '推送失败。请检查网络、GitHub 登录状态或仓库分支。' }
    Write-Host '已成功推送到 GitHub。' -ForegroundColor Green
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
