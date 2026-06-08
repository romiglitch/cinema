<#
.SYNOPSIS
    HTTP bridge for querying the Cinema SQL Server LocalDB and managing
    deployments from a remote machine.

.DESCRIPTION
    Starts an HTTP listener that accepts SQL queries via POST, returns JSON results,
    and provides deployment endpoints (git pull, build, start/stop IIS Express).
    Designed to run on the Windows machine where SQL Server LocalDB + Dtb.mdf live.
    Must be run as Administrator (HttpListener requires it for non-localhost prefixes).

.PARAMETER Port
    TCP port to listen on. Default: 8765

.PARAMETER MdfPath
    Full path to Dtb.mdf. If omitted, auto-detects from Shipping\App_Data\Dtb.mdf
    relative to the script's parent directory.

.EXAMPLE
    # Auto-detect database path:
    .\sql-bridge.ps1

    # Explicit path:
    .\sql-bridge.ps1 -MdfPath "C:\Users\Me\dev\cinema\Shipping\App_Data\Dtb.mdf"

    # Custom port:
    .\sql-bridge.ps1 -Port 9000
#>

param(
    [int]$Port = 8765,
    [string]$MdfPath
)

$ErrorActionPreference = "Stop"

# --- Resolve database path ---
if (-not $MdfPath) {
    $candidate = Join-Path (Split-Path $PSScriptRoot) "Shipping\App_Data\Dtb.mdf"
    if (Test-Path $candidate) {
        $MdfPath = (Resolve-Path $candidate).Path
    } else {
        Write-Host "ERROR: Could not find Dtb.mdf at $candidate" -ForegroundColor Red
        Write-Host "Pass the full path with: .\sql-bridge.ps1 -MdfPath 'C:\path\to\Dtb.mdf'"
        exit 1
    }
}

if (-not (Test-Path $MdfPath)) {
    Write-Host "ERROR: File not found: $MdfPath" -ForegroundColor Red
    exit 1
}

$connStr = "Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=$MdfPath;Integrated Security=True"

# --- Test database connection ---
Write-Host "Testing connection to $MdfPath ..." -ForegroundColor Cyan
try {
    $testConn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $testConn.Open()
    Write-Host "Database connection OK (SQL Server $($testConn.ServerVersion))" -ForegroundColor Green
    $testConn.Close()
} catch {
    Write-Host "ERROR: Cannot connect to database: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# --- Resolve project paths ---
$repoRoot = Split-Path $PSScriptRoot
$slnPath = Join-Path $repoRoot "Shipping.sln"

# Find MSBuild via vswhere
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuildPath = $null
if (Test-Path $vswhere) {
    $vsPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
    if ($vsPath) { $msbuildPath = $vsPath }
}
if (-not $msbuildPath) {
    $fallbacks = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )
    foreach ($fb in $fallbacks) {
        if (Test-Path $fb) { $msbuildPath = $fb; break }
    }
}

# Find IIS Express
$iisExpressPath = "${env:ProgramFiles}\IIS Express\iisexpress.exe"
if (-not (Test-Path $iisExpressPath)) {
    $iisExpressPath = "${env:ProgramFiles(x86)}\IIS Express\iisexpress.exe"
}

# Find NuGet
$nugetPath = Join-Path $repoRoot ".nuget\NuGet.exe"
if (-not (Test-Path $nugetPath)) {
    $nugetPath = (Get-Command nuget -ErrorAction SilentlyContinue).Source
}

# Track IIS Express process
$script:iisProcess = $null

Write-Host "  Repo:     $repoRoot"
if ($msbuildPath) { Write-Host "  MSBuild:  $msbuildPath" -ForegroundColor Green }
else { Write-Host "  MSBuild:  NOT FOUND" -ForegroundColor Yellow }

# --- Helper: run a shell command and capture output ---
function Run-Command {
    param([string]$Command, [string]$WorkingDir = $repoRoot)

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = "powershell.exe"
    $pinfo.Arguments = "-NoProfile -NonInteractive -Command `"Set-Location '$WorkingDir'; $Command`""
    $pinfo.WorkingDirectory = $WorkingDir
    $pinfo.RedirectStandardOutput = $true
    $pinfo.RedirectStandardError = $true
    $pinfo.UseShellExecute = $false
    $pinfo.CreateNoWindow = $true

    $proc = [System.Diagnostics.Process]::Start($pinfo)
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()

    return @{
        exitCode = $proc.ExitCode
        stdout   = $stdout
        stderr   = $stderr
        success  = ($proc.ExitCode -eq 0)
    }
}

# --- Helper: execute SQL and return result object ---
function Invoke-SqlQuery {
    param([string]$Sql)

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        $cmd.CommandTimeout = 30

        $trimmed = $Sql.TrimStart()
        $isSelect = $trimmed -match "(?i)^(SELECT|WITH|EXEC|EXECUTE|SP_|SHOW|DESCRIBE)"

        if ($isSelect) {
            $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
            $table = New-Object System.Data.DataTable
            [void]$adapter.Fill($table)

            $rows = [System.Collections.ArrayList]::new()
            foreach ($row in $table.Rows) {
                $obj = [ordered]@{}
                foreach ($col in $table.Columns) {
                    $val = $row[$col.ColumnName]
                    if ($val -is [System.DBNull]) { $val = $null }
                    $obj[$col.ColumnName] = $val
                }
                [void]$rows.Add($obj)
            }

            $columns = @()
            foreach ($col in $table.Columns) {
                $columns += @{ name = $col.ColumnName; type = $col.DataType.Name }
            }

            return @{
                success = $true
                type    = "resultset"
                columns = $columns
                rows    = $rows.ToArray()
                count   = $rows.Count
            }
        } else {
            $affected = $cmd.ExecuteNonQuery()
            return @{
                success       = $true
                type          = "affected"
                rowsAffected  = $affected
            }
        }
    } finally {
        $conn.Close()
    }
}

# --- Helper: send JSON response ---
function Send-Json {
    param($Context, $Data, [int]$StatusCode = 200)

    $json = $Data | ConvertTo-Json -Depth 20 -Compress
    $buffer = [System.Text.Encoding]::UTF8.GetBytes($json)
    $Context.Response.StatusCode = $StatusCode
    $Context.Response.ContentType = "application/json; charset=utf-8"
    $Context.Response.Headers.Add("Access-Control-Allow-Origin", "*")
    $Context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
    $Context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type")
    $Context.Response.ContentLength64 = $buffer.Length
    $Context.Response.OutputStream.Write($buffer, 0, $buffer.Length)
    $Context.Response.Close()
}

# --- Start HTTP listener ---
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://*:$Port/")

try {
    $listener.Start()
} catch [System.Net.HttpListenerException] {
    Write-Host ""
    Write-Host "ERROR: Cannot bind to port $Port." -ForegroundColor Red
    Write-Host "Run PowerShell as Administrator, or use:" -ForegroundColor Yellow
    Write-Host "  netsh http add urlacl url=http://*:$Port/ user=$env:USERNAME" -ForegroundColor Yellow
    exit 1
}

$localIP = (Get-NetIPAddress -AddressFamily IPv4 |
            Where-Object { $_.InterfaceAlias -notmatch "Loopback" -and $_.IPAddress -ne "127.0.0.1" } |
            Select-Object -First 1).IPAddress

Write-Host ""
Write-Host "=== Cinema SQL Bridge ===" -ForegroundColor Green
Write-Host "  Database: $MdfPath"
Write-Host "  Port:     $Port"
Write-Host "  Local:    http://localhost:$Port"
if ($localIP) {
    Write-Host "  Network:  http://${localIP}:$Port" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "SQL Endpoints:" -ForegroundColor Cyan
Write-Host "  POST /query          - Execute SQL (body: {`"sql`": `"SELECT ...`"})"
Write-Host "  GET  /tables         - List all tables"
Write-Host "  GET  /schema?table=X - Describe table columns"
Write-Host "  GET  /ping           - Health check"
Write-Host ""
Write-Host "Deploy Endpoints:" -ForegroundColor Cyan
Write-Host "  POST /git-pull       - Pull latest changes from remote"
Write-Host "  POST /build          - Restore NuGet packages and build solution"
Write-Host "  POST /start          - Start IIS Express for the Shipping app"
Write-Host "  POST /stop           - Stop IIS Express"
Write-Host "  POST /deploy         - Pull + build + restart (all in one)"
Write-Host "  GET  /app-status     - Check if the app is running"
Write-Host ""
Write-Host "Press Ctrl+C to stop." -ForegroundColor DarkGray
Write-Host ""

# --- Request loop ---
try {
    while ($listener.IsListening) {
        $ctx = $listener.GetContext()
        $req = $ctx.Request
        $path = $req.Url.LocalPath

        # CORS preflight
        if ($req.HttpMethod -eq "OPTIONS") {
            Send-Json $ctx @{ok = $true}
            continue
        }

        $timestamp = Get-Date -Format "HH:mm:ss"

        try {
            switch -Regex ($path) {
                "^/ping$" {
                    Write-Host "[$timestamp] GET /ping" -ForegroundColor DarkGray
                    Send-Json $ctx @{ status = "ok"; database = [System.IO.Path]::GetFileName($MdfPath) }
                }

                "^/tables$" {
                    Write-Host "[$timestamp] GET /tables" -ForegroundColor DarkGray
                    $result = Invoke-SqlQuery "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"
                    $names = $result.rows | ForEach-Object { $_.TABLE_NAME }
                    Send-Json $ctx @{ tables = $names }
                }

                "^/schema$" {
                    $table = $req.QueryString["table"]
                    if (-not $table) {
                        Send-Json $ctx @{ error = "Missing ?table= parameter" } 400
                        continue
                    }
                    Write-Host "[$timestamp] GET /schema?table=$table" -ForegroundColor DarkGray
                    $result = Invoke-SqlQuery @"
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = '$($table -replace "'","''")'
ORDER BY ORDINAL_POSITION
"@
                    Send-Json $ctx @{ table = $table; columns = $result.rows }
                }

                "^/query$" {
                    if ($req.HttpMethod -ne "POST") {
                        Send-Json $ctx @{ error = "Use POST" } 405
                        continue
                    }
                    $reader = New-Object System.IO.StreamReader($req.InputStream, $req.ContentEncoding)
                    $body = $reader.ReadToEnd()
                    $reader.Close()

                    $payload = $body | ConvertFrom-Json
                    $sql = $payload.sql

                    if (-not $sql) {
                        Send-Json $ctx @{ error = 'Missing "sql" field in request body' } 400
                        continue
                    }

                    $preview = if ($sql.Length -gt 80) { $sql.Substring(0, 80) + "..." } else { $sql }
                    Write-Host "[$timestamp] POST /query: $preview" -ForegroundColor White

                    $result = Invoke-SqlQuery $sql
                    Send-Json $ctx $result
                }

                "^/git-pull$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /git-pull" -ForegroundColor Magenta
                    $result = Run-Command "git pull"
                    Write-Host "  git pull exit=$($result.exitCode)" -ForegroundColor $(if ($result.success) { "Green" } else { "Red" })
                    Send-Json $ctx @{
                        success = $result.success
                        output  = ($result.stdout + $result.stderr).Trim()
                    } $(if ($result.success) { 200 } else { 500 })
                }

                "^/build$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /build" -ForegroundColor Magenta

                    if (-not $msbuildPath) {
                        Send-Json $ctx @{ success = $false; error = "MSBuild not found on this machine." } 500
                        continue
                    }

                    $steps = @()

                    # NuGet restore
                    if ($nugetPath -and (Test-Path $nugetPath)) {
                        $nugetResult = Run-Command "& '$nugetPath' restore '$slnPath'"
                        $steps += @{ step = "nuget-restore"; success = $nugetResult.success; output = ($nugetResult.stdout + $nugetResult.stderr).Trim() }
                        if (-not $nugetResult.success) {
                            Write-Host "  NuGet restore FAILED" -ForegroundColor Red
                            Send-Json $ctx @{ success = $false; steps = $steps } 500
                            continue
                        }
                    }

                    # MSBuild
                    $buildResult = Run-Command "& '$msbuildPath' '$slnPath' /p:Configuration=Debug /t:Build /v:minimal"
                    $steps += @{ step = "msbuild"; success = $buildResult.success; output = ($buildResult.stdout + $buildResult.stderr).Trim() }

                    $ok = $buildResult.success
                    Write-Host "  Build $(if ($ok) { 'OK' } else { 'FAILED' })" -ForegroundColor $(if ($ok) { "Green" } else { "Red" })
                    Send-Json $ctx @{ success = $ok; steps = $steps } $(if ($ok) { 200 } else { 500 })
                }

                "^/start$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /start" -ForegroundColor Magenta

                    if (-not (Test-Path $iisExpressPath)) {
                        Send-Json $ctx @{ success = $false; error = "IIS Express not found." } 500
                        continue
                    }

                    # Stop existing instance first
                    if ($script:iisProcess -and -not $script:iisProcess.HasExited) {
                        $script:iisProcess.Kill()
                        $script:iisProcess.WaitForExit(5000)
                    }

                    $appPath = Join-Path $repoRoot "Shipping"
                    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
                    $pinfo.FileName = $iisExpressPath
                    $pinfo.Arguments = "/path:`"$appPath`" /port:50594"
                    $pinfo.UseShellExecute = $false
                    $pinfo.CreateNoWindow = $true

                    try {
                        $script:iisProcess = [System.Diagnostics.Process]::Start($pinfo)
                        Start-Sleep -Seconds 2
                        if ($script:iisProcess.HasExited) {
                            Send-Json $ctx @{ success = $false; error = "IIS Express exited immediately with code $($script:iisProcess.ExitCode)." } 500
                        } else {
                            Write-Host "  IIS Express started (PID $($script:iisProcess.Id), port 50594)" -ForegroundColor Green
                            Send-Json $ctx @{ success = $true; pid = $script:iisProcess.Id; url = "http://localhost:50594/" }
                        }
                    } catch {
                        Send-Json $ctx @{ success = $false; error = $_.Exception.Message } 500
                    }
                }

                "^/stop$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /stop" -ForegroundColor Magenta

                    $stopped = $false
                    if ($script:iisProcess -and -not $script:iisProcess.HasExited) {
                        $script:iisProcess.Kill()
                        $script:iisProcess.WaitForExit(5000)
                        $stopped = $true
                        Write-Host "  IIS Express stopped" -ForegroundColor Yellow
                    }

                    # Also kill any other IIS Express instances for this port
                    Get-Process -Name "iisexpress" -ErrorAction SilentlyContinue | ForEach-Object {
                        $_.Kill(); $stopped = $true
                    }

                    Send-Json $ctx @{ success = $true; stopped = $stopped }
                }

                "^/deploy$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /deploy (full pipeline)" -ForegroundColor Cyan

                    $steps = @()

                    # 1. git pull
                    Write-Host "  [1/3] git pull..." -ForegroundColor White
                    $pullResult = Run-Command "git pull"
                    $steps += @{ step = "git-pull"; success = $pullResult.success; output = ($pullResult.stdout + $pullResult.stderr).Trim() }
                    if (-not $pullResult.success) {
                        Write-Host "    FAILED" -ForegroundColor Red
                        Send-Json $ctx @{ success = $false; steps = $steps; failedAt = "git-pull" } 500
                        continue
                    }

                    # 2. NuGet restore + build
                    Write-Host "  [2/3] build..." -ForegroundColor White
                    if (-not $msbuildPath) {
                        $steps += @{ step = "build"; success = $false; output = "MSBuild not found." }
                        Send-Json $ctx @{ success = $false; steps = $steps; failedAt = "build" } 500
                        continue
                    }

                    if ($nugetPath -and (Test-Path $nugetPath)) {
                        $nugetResult = Run-Command "& '$nugetPath' restore '$slnPath'"
                        $steps += @{ step = "nuget-restore"; success = $nugetResult.success; output = ($nugetResult.stdout + $nugetResult.stderr).Trim() }
                    }

                    $buildResult = Run-Command "& '$msbuildPath' '$slnPath' /p:Configuration=Debug /t:Build /v:minimal"
                    $steps += @{ step = "build"; success = $buildResult.success; output = ($buildResult.stdout + $buildResult.stderr).Trim() }
                    if (-not $buildResult.success) {
                        Write-Host "    FAILED" -ForegroundColor Red
                        Send-Json $ctx @{ success = $false; steps = $steps; failedAt = "build" } 500
                        continue
                    }

                    # 3. Restart IIS Express
                    Write-Host "  [3/3] restart app..." -ForegroundColor White
                    if ($script:iisProcess -and -not $script:iisProcess.HasExited) {
                        $script:iisProcess.Kill()
                        $script:iisProcess.WaitForExit(5000)
                    }
                    Get-Process -Name "iisexpress" -ErrorAction SilentlyContinue | ForEach-Object { $_.Kill() }

                    if (Test-Path $iisExpressPath) {
                        $appPath = Join-Path $repoRoot "Shipping"
                        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
                        $pinfo.FileName = $iisExpressPath
                        $pinfo.Arguments = "/path:`"$appPath`" /port:50594"
                        $pinfo.UseShellExecute = $false
                        $pinfo.CreateNoWindow = $true
                        $script:iisProcess = [System.Diagnostics.Process]::Start($pinfo)
                        Start-Sleep -Seconds 2

                        if ($script:iisProcess.HasExited) {
                            $steps += @{ step = "start"; success = $false; output = "IIS Express exited with code $($script:iisProcess.ExitCode)" }
                            Send-Json $ctx @{ success = $false; steps = $steps; failedAt = "start" } 500
                            continue
                        }
                        $steps += @{ step = "start"; success = $true; output = "IIS Express running on port 50594 (PID $($script:iisProcess.Id))" }
                    } else {
                        $steps += @{ step = "start"; success = $false; output = "IIS Express not found." }
                        Send-Json $ctx @{ success = $false; steps = $steps; failedAt = "start" } 500
                        continue
                    }

                    Write-Host "  Deploy complete!" -ForegroundColor Green
                    Send-Json $ctx @{ success = $true; steps = $steps; url = "http://localhost:50594/" }
                }

                "^/app-status$" {
                    Write-Host "[$timestamp] GET /app-status" -ForegroundColor DarkGray
                    $running = $false
                    $pid_val = $null
                    if ($script:iisProcess -and -not $script:iisProcess.HasExited) {
                        $running = $true
                        $pid_val = $script:iisProcess.Id
                    }
                    Send-Json $ctx @{ running = $running; pid = $pid_val; url = "http://localhost:50594/" }
                }

                default {
                    Send-Json $ctx @{
                        error = "Unknown endpoint: $path"
                        endpoints = @("/query", "/tables", "/schema?table=X", "/ping", "/git-pull", "/build", "/start", "/stop", "/deploy", "/app-status")
                    } 404
                }
            }
        } catch {
            Write-Host "[$timestamp] ERROR: $($_.Exception.Message)" -ForegroundColor Red
            Send-Json $ctx @{ success = $false; error = $_.Exception.Message } 500
        }
    }
} finally {
    $listener.Stop()
    Write-Host "Bridge stopped." -ForegroundColor Yellow
}
