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

# Payment database connection string (resolved relative to repo root)
$paymentMdfPath = Join-Path (Split-Path $PSScriptRoot) "Payment\PaymentDb.mdf"
$paymentConnStr = $null
if (Test-Path $paymentMdfPath) {
    $paymentConnStr = "Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=$paymentMdfPath;Integrated Security=True"
}

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

# Track IIS Express processes (main app and TrailersWS)
$script:iisProcess = $null
$script:trailersProcess = $null

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

# --- Endpoint catalog (startup banner + 404 help) ---
$script:BridgeEndpointLines = @(
    "POST /query              - Execute SQL on Dtb.mdf (body: {`"sql`": `"SELECT ...`"})"
    "POST /query-payment      - Execute SQL on PaymentDb.mdf"
    "POST /setup-payment-db   - Run Payment/Scripts/SetupPaymentDb.ps1"
    "POST /write-env          - Write request body to repo-root .env"
    "GET  /tables             - List all tables"
    "GET  /schema?table=X     - Describe table columns"
    "GET  /ping               - Health check"
    "POST /git-pull           - Pull latest changes from remote"
    "POST /build              - Restore NuGet packages and build solution"
    "POST /start              - Start IIS Express (Shipping + TrailersWS)"
    "POST /stop               - Stop IIS Express"
    "POST /deploy             - Pull + build + restart apps"
    "GET  /app-status         - Check if apps are running"
    "POST /restart            - Restart this bridge (picks up script changes)"
)

$script:BridgeEndpointPaths = @(
    "/query", "/query-payment", "/setup-payment-db", "/write-env",
    "/tables", "/schema?table=X", "/ping",
    "/git-pull", "/build", "/start", "/stop", "/deploy", "/app-status", "/restart"
)

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Schedules an elevated bridge relaunch after 100ms, then the caller should exit
function Schedule-BridgeRestart {
    $bridgeScript = (Resolve-Path (Join-Path $PSScriptRoot "sql-bridge.ps1")).Path
    $argList = "-NoProfile -ExecutionPolicy Bypass -File `"$bridgeScript`" -Port $Port"
    $launchLine = if (Test-IsAdministrator) {
        "Start-Process -FilePath powershell.exe -ArgumentList '$argList'"
    } else {
        "Start-Process -FilePath powershell.exe -ArgumentList '$argList' -Verb RunAs"
    }

    $launcherScript = @"
Start-Sleep -Milliseconds 100
$launchLine
"@

    $tempScript = Join-Path $env:TEMP "cinema-bridge-restart-$PID.ps1"
    Set-Content -Path $tempScript -Value $launcherScript -Encoding UTF8

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = "powershell.exe"
    $pinfo.Arguments = "-NoProfile -WindowStyle Hidden -File `"$tempScript`""
    $pinfo.UseShellExecute = $false
    $pinfo.CreateNoWindow = $true
    [void][System.Diagnostics.Process]::Start($pinfo)
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
Write-Host "Endpoints:" -ForegroundColor Cyan
foreach ($line in $script:BridgeEndpointLines) {
    Write-Host "  $line"
}
Write-Host ""
Write-Host "Press Ctrl+C to stop." -ForegroundColor DarkGray
Write-Host ""

# --- Request loop ---
try {
    :listenerLoop while ($listener.IsListening) {
        $ctx = $listener.GetContext()
        $req = $ctx.Request
        $path = $req.Url.LocalPath
        $restartBridge = $false

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

                "^/query-payment$" {
                    if ($req.HttpMethod -ne "POST") {
                        Send-Json $ctx @{ error = "Use POST" } 405
                        continue
                    }
                    if (-not $paymentConnStr) {
                        Send-Json $ctx @{ error = "PaymentDb.mdf not found at $paymentMdfPath" } 500
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
                    Write-Host "[$timestamp] POST /query-payment: $preview" -ForegroundColor White

                    $conn = New-Object System.Data.SqlClient.SqlConnection($paymentConnStr)
                    $conn.Open()
                    try {
                        $cmd = $conn.CreateCommand()
                        $cmd.CommandText = $sql
                        $cmd.CommandTimeout = 30
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
                        Send-Json $ctx @{ success = $true; rows = $rows.ToArray(); count = $rows.Count }
                    } finally {
                        $conn.Close()
                    }
                }

                "^/setup-payment-db$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /setup-payment-db" -ForegroundColor Cyan
                    $setupScript = Join-Path $repoRoot "Payment\Scripts\SetupPaymentDb.ps1"
                    if (-not (Test-Path $setupScript)) {
                        Send-Json $ctx @{ success = $false; error = "SetupPaymentDb.ps1 not found at $setupScript" } 500
                        continue
                    }
                    $result = Run-Command "& '$setupScript'"
                    Send-Json $ctx @{ success = $result.success; output = ($result.stdout + $result.stderr).Trim() }
                }

                "^/write-env$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    $reader = New-Object System.IO.StreamReader($req.InputStream, $req.ContentEncoding)
                    $body = $reader.ReadToEnd()
                    $reader.Close()
                    $envPath = Join-Path $repoRoot ".env"
                    [System.IO.File]::WriteAllText($envPath, $body, [System.Text.Encoding]::UTF8)
                    Write-Host "[$timestamp] POST /write-env - wrote $($body.Length) bytes to $envPath" -ForegroundColor Green
                    Send-Json $ctx @{ success = $true; path = $envPath }
                }

                "^/git-pull$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /git-pull" -ForegroundColor Magenta
                    $result = Run-Command "git fetch origin; if (`$LASTEXITCODE -ne 0) { exit `$LASTEXITCODE }; git reset --hard origin/main"
                    Write-Host "  git sync exit=$($result.exitCode)" -ForegroundColor $(if ($result.success) { "Green" } else { "Red" })
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

                    # Stop existing instances first
                    if ($script:iisProcess -and -not $script:iisProcess.HasExited) {
                        $script:iisProcess.Kill()
                        $script:iisProcess.WaitForExit(5000)
                    }
                    if ($script:trailersProcess -and -not $script:trailersProcess.HasExited) {
                        $script:trailersProcess.Kill()
                        $script:trailersProcess.WaitForExit(5000)
                    }

                    $appPath = Join-Path $repoRoot "Shipping"
                    $trailersPath = Join-Path $repoRoot "TrailersWS"

                    # Clean up ALL conflicting network rules for both ports
                    netsh interface portproxy delete v4tov4 listenport=50594 listenaddress=0.0.0.0 2>$null
                    netsh http delete urlacl url=http://*:50594/ 2>$null
                    netsh http delete urlacl url=http://+:50594/ 2>$null
                    netsh http delete urlacl url=http://localhost:50594/ 2>$null
                    netsh interface portproxy delete v4tov4 listenport=51730 listenaddress=0.0.0.0 2>$null
                    netsh http delete urlacl url=http://*:51730/ 2>$null
                    netsh http delete urlacl url=http://+:51730/ 2>$null
                    netsh http delete urlacl url=http://localhost:51730/ 2>$null
                    # Ensure firewall allows incoming on both ports
                    netsh advfirewall firewall delete rule name="IIS Express 50594" 2>$null
                    netsh advfirewall firewall add rule name="IIS Express 50594" dir=in action=allow protocol=tcp localport=50594 2>$null
                    netsh advfirewall firewall delete rule name="IIS Express 51730" 2>$null
                    netsh advfirewall firewall add rule name="IIS Express 51730" dir=in action=allow protocol=tcp localport=51730 2>$null

                    # Find IIS Express default config
                    $iisConfigPath = "$env:USERPROFILE\Documents\IISExpress\config\applicationhost.config"
                    if (-not (Test-Path $iisConfigPath)) {
                        $iisConfigPath = "$env:USERPROFILE\.iis\IISExpress\config\applicationhost.config"
                    }

                    $debug = @{}
                    $debug["configPath"] = $iisConfigPath
                    $debug["configExists"] = (Test-Path $iisConfigPath)

                    # Find or create site entries for both apps
                    $siteId = $null
                    $trailersSiteId = $null
                    if (Test-Path $iisConfigPath) {
                        $xml = [xml](Get-Content $iisConfigPath)
                        $sitesNode = $xml.SelectSingleNode("//sites")
                        $sites = $xml.SelectNodes("//site")

                        # Look for existing sites
                        foreach ($site in $sites) {
                            $bindings = $site.SelectNodes("bindings/binding[@protocol='http']")
                            foreach ($b in $bindings) {
                                $info = $b.GetAttribute("bindingInformation")
                                if ($info -match ":50594") {
                                    $b.SetAttribute("bindingInformation", "*:50594:")
                                    $siteId = $site.GetAttribute("id")
                                    $debug["mainSite"] = "patched existing"
                                }
                                if ($info -match ":51730") {
                                    $b.SetAttribute("bindingInformation", "*:51730:")
                                    $trailersSiteId = $site.GetAttribute("id")
                                    $debug["trailersSite"] = "patched existing"
                                }
                            }
                        }

                        # Create missing sites
                        if ((-not $siteId -or -not $trailersSiteId) -and $sitesNode) {
                            $maxId = 0
                            foreach ($s in $sites) {
                                $id = [int]$s.GetAttribute("id")
                                if ($id -gt $maxId) { $maxId = $id }
                            }

                            if (-not $siteId) {
                                $newId = $maxId + 1
                                $siteXml = @"
    <site name="CinemaRemote" id="$newId">
        <application path="/" applicationPool="Clr4IntegratedAppPool">
            <virtualDirectory path="/" physicalPath="$appPath" />
        </application>
        <bindings>
            <binding protocol="http" bindingInformation="*:50594:" />
        </bindings>
    </site>
"@
                                $fragment = $xml.CreateDocumentFragment()
                                $fragment.InnerXml = $siteXml
                                $sitesNode.AppendChild($fragment) | Out-Null
                                $siteId = $newId
                                $maxId = $newId
                                $debug["mainSite"] = "created new"
                            }

                            if (-not $trailersSiteId) {
                                $newId = $maxId + 1
                                $siteXml = @"
    <site name="TrailersWS" id="$newId">
        <application path="/" applicationPool="Clr4IntegratedAppPool">
            <virtualDirectory path="/" physicalPath="$trailersPath" />
        </application>
        <bindings>
            <binding protocol="http" bindingInformation="*:51730:" />
        </bindings>
    </site>
"@
                                $fragment = $xml.CreateDocumentFragment()
                                $fragment.InnerXml = $siteXml
                                $sitesNode.AppendChild($fragment) | Out-Null
                                $trailersSiteId = $newId
                                $debug["trailersSite"] = "created new"
                            }
                        }

                        $xml.Save($iisConfigPath)
                    }
                    $debug["siteId"] = $siteId
                    $debug["trailersSiteId"] = $trailersSiteId

                    # Start main app
                    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
                    $pinfo.FileName = $iisExpressPath
                    if ($siteId) {
                        $pinfo.Arguments = "/config:`"$iisConfigPath`" /siteid:$siteId"
                    } else {
                        $pinfo.Arguments = "/path:`"$appPath`" /port:50594"
                    }
                    $pinfo.UseShellExecute = $false
                    $pinfo.CreateNoWindow = $true
                    $debug["mainCmdLine"] = "$($pinfo.FileName) $($pinfo.Arguments)"

                    # Start TrailersWS
                    $trailersInfo = New-Object System.Diagnostics.ProcessStartInfo
                    $trailersInfo.FileName = $iisExpressPath
                    if ($trailersSiteId) {
                        $trailersInfo.Arguments = "/config:`"$iisConfigPath`" /siteid:$trailersSiteId"
                    } else {
                        $trailersInfo.Arguments = "/path:`"$trailersPath`" /port:51730"
                    }
                    $trailersInfo.UseShellExecute = $false
                    $trailersInfo.CreateNoWindow = $true
                    $debug["trailersCmdLine"] = "$($trailersInfo.FileName) $($trailersInfo.Arguments)"

                    try {
                        $script:iisProcess = [System.Diagnostics.Process]::Start($pinfo)
                        Start-Sleep -Seconds 2
                        if ($script:iisProcess.HasExited) {
                            $debug["mainExitCode"] = $script:iisProcess.ExitCode
                            Send-Json $ctx @{ success = $false; error = "Main app exited immediately with code $($script:iisProcess.ExitCode)."; debug = $debug } 500
                        } else {
                            Write-Host "  Main app started (PID $($script:iisProcess.Id), port 50594)" -ForegroundColor Green
                            
                            $script:trailersProcess = [System.Diagnostics.Process]::Start($trailersInfo)
                            Start-Sleep -Seconds 2
                            if ($script:trailersProcess.HasExited) {
                                $debug["trailersExitCode"] = $script:trailersProcess.ExitCode
                                Write-Host "  TrailersWS failed to start (exit code $($script:trailersProcess.ExitCode))" -ForegroundColor Yellow
                            } else {
                                Write-Host "  TrailersWS started (PID $($script:trailersProcess.Id), port 51730)" -ForegroundColor Green
                            }

                            Send-Json $ctx @{ 
                                success = $true
                                mainPid = $script:iisProcess.Id
                                trailersPid = if ($script:trailersProcess -and -not $script:trailersProcess.HasExited) { $script:trailersProcess.Id } else { $null }
                                url = "http://100.94.185.70:50594/"
                                trailersUrl = "http://100.94.185.70:51730/"
                                debug = $debug
                            }
                        }
                    } catch {
                        $debug["exception"] = $_.Exception.Message
                        Send-Json $ctx @{ success = $false; error = $_.Exception.Message; debug = $debug } 500
                    }
                }

                "^/stop$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /stop" -ForegroundColor Magenta

                    $stoppedMain = $false
                    $stoppedTrailers = $false
                    if ($script:iisProcess -and -not $script:iisProcess.HasExited) {
                        $script:iisProcess.Kill()
                        $script:iisProcess.WaitForExit(5000)
                        $stoppedMain = $true
                        Write-Host "  Main app stopped" -ForegroundColor Yellow
                    }
                    if ($script:trailersProcess -and -not $script:trailersProcess.HasExited) {
                        $script:trailersProcess.Kill()
                        $script:trailersProcess.WaitForExit(5000)
                        $stoppedTrailers = $true
                        Write-Host "  TrailersWS stopped" -ForegroundColor Yellow
                    }

                    # Also kill any other IIS Express instances
                    Get-Process -Name "iisexpress" -ErrorAction SilentlyContinue | ForEach-Object {
                        $_.Kill()
                    }

                    Send-Json $ctx @{ success = $true; stopped = ($stoppedMain -or $stoppedTrailers); stoppedMain = $stoppedMain; stoppedTrailers = $stoppedTrailers }
                }

                "^/deploy$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /deploy (full pipeline)" -ForegroundColor Cyan

                    $steps = @()

                    # 1. git sync
                    Write-Host "  [1/3] git sync..." -ForegroundColor White
                    $pullResult = Run-Command "git fetch origin; if (`$LASTEXITCODE -ne 0) { exit `$LASTEXITCODE }; git reset --hard origin/main"
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

                    # 3. Restart IIS Express (both apps)
                    Write-Host "  [3/3] restart apps..." -ForegroundColor White
                    if ($script:iisProcess -and -not $script:iisProcess.HasExited) {
                        $script:iisProcess.Kill()
                        $script:iisProcess.WaitForExit(5000)
                    }
                    if ($script:trailersProcess -and -not $script:trailersProcess.HasExited) {
                        $script:trailersProcess.Kill()
                        $script:trailersProcess.WaitForExit(5000)
                    }
                    Get-Process -Name "iisexpress" -ErrorAction SilentlyContinue | ForEach-Object { $_.Kill() }

                    if (Test-Path $iisExpressPath) {
                        $appPath = Join-Path $repoRoot "Shipping"
                        $trailersPath = Join-Path $repoRoot "TrailersWS"

                        # Clean up ALL conflicting network rules for both ports
                        netsh interface portproxy delete v4tov4 listenport=50594 listenaddress=0.0.0.0 2>$null
                        netsh http delete urlacl url=http://*:50594/ 2>$null
                        netsh http delete urlacl url=http://+:50594/ 2>$null
                        netsh http delete urlacl url=http://localhost:50594/ 2>$null
                        netsh interface portproxy delete v4tov4 listenport=51730 listenaddress=0.0.0.0 2>$null
                        netsh http delete urlacl url=http://*:51730/ 2>$null
                        netsh http delete urlacl url=http://+:51730/ 2>$null
                        netsh http delete urlacl url=http://localhost:51730/ 2>$null
                        netsh advfirewall firewall delete rule name="IIS Express 50594" 2>$null
                        netsh advfirewall firewall add rule name="IIS Express 50594" dir=in action=allow protocol=tcp localport=50594 2>$null
                        netsh advfirewall firewall delete rule name="IIS Express 51730" 2>$null
                        netsh advfirewall firewall add rule name="IIS Express 51730" dir=in action=allow protocol=tcp localport=51730 2>$null

                        $iisConfigPath = "$env:USERPROFILE\Documents\IISExpress\config\applicationhost.config"
                        if (-not (Test-Path $iisConfigPath)) {
                            $iisConfigPath = "$env:USERPROFILE\.iis\IISExpress\config\applicationhost.config"
                        }

                        # Find or create site entries for both apps
                        $siteId = $null
                        $trailersSiteId = $null
                        if (Test-Path $iisConfigPath) {
                            $xml = [xml](Get-Content $iisConfigPath)
                            $sitesNode = $xml.SelectSingleNode("//sites")
                            $sites = $xml.SelectNodes("//site")
                            foreach ($site in $sites) {
                                $bindings = $site.SelectNodes("bindings/binding[@protocol='http']")
                                foreach ($b in $bindings) {
                                    $info = $b.GetAttribute("bindingInformation")
                                    if ($info -match ":50594") {
                                        $b.SetAttribute("bindingInformation", "*:50594:")
                                        $siteId = $site.GetAttribute("id")
                                    }
                                    if ($info -match ":51730") {
                                        $b.SetAttribute("bindingInformation", "*:51730:")
                                        $trailersSiteId = $site.GetAttribute("id")
                                    }
                                }
                            }
                            if ((-not $siteId -or -not $trailersSiteId) -and $sitesNode) {
                                $maxId = 0
                                foreach ($s in $sites) {
                                    $id = [int]$s.GetAttribute("id")
                                    if ($id -gt $maxId) { $maxId = $id }
                                }
                                if (-not $siteId) {
                                    $newId = $maxId + 1
                                    $siteXml = @"
    <site name="CinemaRemote" id="$newId">
        <application path="/" applicationPool="Clr4IntegratedAppPool">
            <virtualDirectory path="/" physicalPath="$appPath" />
        </application>
        <bindings>
            <binding protocol="http" bindingInformation="*:50594:" />
        </bindings>
    </site>
"@
                                    $fragment = $xml.CreateDocumentFragment()
                                    $fragment.InnerXml = $siteXml
                                    $sitesNode.AppendChild($fragment) | Out-Null
                                    $siteId = $newId
                                    $maxId = $newId
                                }
                                if (-not $trailersSiteId) {
                                    $newId = $maxId + 1
                                    $siteXml = @"
    <site name="TrailersWS" id="$newId">
        <application path="/" applicationPool="Clr4IntegratedAppPool">
            <virtualDirectory path="/" physicalPath="$trailersPath" />
        </application>
        <bindings>
            <binding protocol="http" bindingInformation="*:51730:" />
        </bindings>
    </site>
"@
                                    $fragment = $xml.CreateDocumentFragment()
                                    $fragment.InnerXml = $siteXml
                                    $sitesNode.AppendChild($fragment) | Out-Null
                                    $trailersSiteId = $newId
                                }
                            }
                            $xml.Save($iisConfigPath)
                        }

                        # Start main app
                        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
                        $pinfo.FileName = $iisExpressPath
                        if ($siteId) {
                            $pinfo.Arguments = "/config:`"$iisConfigPath`" /siteid:$siteId"
                        } else {
                            $pinfo.Arguments = "/path:`"$appPath`" /port:50594"
                        }
                        $pinfo.UseShellExecute = $false
                        $pinfo.CreateNoWindow = $true
                        $script:iisProcess = [System.Diagnostics.Process]::Start($pinfo)
                        Start-Sleep -Seconds 2

                        if ($script:iisProcess.HasExited) {
                            $steps += @{ step = "start"; success = $false; output = "Main app exited with code $($script:iisProcess.ExitCode)" }
                            Send-Json $ctx @{ success = $false; steps = $steps; failedAt = "start" } 500
                            continue
                        }

                        # Start TrailersWS
                        $trailersInfo = New-Object System.Diagnostics.ProcessStartInfo
                        $trailersInfo.FileName = $iisExpressPath
                        if ($trailersSiteId) {
                            $trailersInfo.Arguments = "/config:`"$iisConfigPath`" /siteid:$trailersSiteId"
                        } else {
                            $trailersInfo.Arguments = "/path:`"$trailersPath`" /port:51730"
                        }
                        $trailersInfo.UseShellExecute = $false
                        $trailersInfo.CreateNoWindow = $true
                        $script:trailersProcess = [System.Diagnostics.Process]::Start($trailersInfo)
                        Start-Sleep -Seconds 2

                        $trailersStatus = if ($script:trailersProcess -and -not $script:trailersProcess.HasExited) {
                            "TrailersWS running on port 51730 (PID $($script:trailersProcess.Id))"
                        } else {
                            "TrailersWS failed to start"
                        }

                        $steps += @{ step = "start"; success = $true; output = "Main app running on port 50594 (PID $($script:iisProcess.Id)); $trailersStatus" }
                    } else {
                        $steps += @{ step = "start"; success = $false; output = "IIS Express not found." }
                        Send-Json $ctx @{ success = $false; steps = $steps; failedAt = "start" } 500
                        continue
                    }

                    Write-Host "  Deploy complete!" -ForegroundColor Green
                    Send-Json $ctx @{ success = $true; steps = $steps; url = "http://100.94.185.70:50594/" }
                }

                "^/app-status$" {
                    Write-Host "[$timestamp] GET /app-status" -ForegroundColor DarkGray
                    $mainRunning = $false
                    $mainPid = $null
                    $trailersRunning = $false
                    $trailersPid = $null
                    
                    if ($script:iisProcess -and -not $script:iisProcess.HasExited) {
                        $mainRunning = $true
                        $mainPid = $script:iisProcess.Id
                    }
                    if ($script:trailersProcess -and -not $script:trailersProcess.HasExited) {
                        $trailersRunning = $true
                        $trailersPid = $script:trailersProcess.Id
                    }
                    
                    Send-Json $ctx @{ 
                        mainRunning = $mainRunning
                        mainPid = $mainPid
                        mainUrl = "http://localhost:50594/"
                        trailersRunning = $trailersRunning
                        trailersPid = $trailersPid
                        trailersUrl = "http://localhost:51730/"
                    }
                }

                "^/restart$" {
                    if ($req.HttpMethod -ne "POST") { Send-Json $ctx @{ error = "Use POST" } 405; continue }
                    Write-Host "[$timestamp] POST /restart - scheduling elevated relaunch" -ForegroundColor Cyan
                    Schedule-BridgeRestart
                    Send-Json $ctx @{
                        success = $true
                        message = "Bridge restart scheduled in 100ms"
                        elevated = (-not (Test-IsAdministrator))
                    }
                    $restartBridge = $true
                }

                default {
                    Send-Json $ctx @{
                        error = "Unknown endpoint: $path"
                        endpoints = $script:BridgeEndpointPaths
                    } 404
                }
            }
        } catch {
            Write-Host "[$timestamp] ERROR: $($_.Exception.Message)" -ForegroundColor Red
            Send-Json $ctx @{ success = $false; error = $_.Exception.Message } 500
        }

        if ($restartBridge) {
            break listenerLoop
        }
    }
} finally {
    $listener.Stop()
    Write-Host "Bridge stopped." -ForegroundColor Yellow
}
