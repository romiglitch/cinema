<#
.SYNOPSIS
    HTTP bridge for querying the Cinema SQL Server LocalDB from a remote machine.

.DESCRIPTION
    Starts an HTTP listener that accepts SQL queries via POST and returns JSON results.
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
Write-Host "Endpoints:" -ForegroundColor Cyan
Write-Host "  POST /query          - Execute SQL (body: {`"sql`": `"SELECT ...`"})"
Write-Host "  GET  /tables         - List all tables"
Write-Host "  GET  /schema?table=X - Describe table columns"
Write-Host "  GET  /ping           - Health check"
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

                default {
                    Send-Json $ctx @{
                        error = "Unknown endpoint: $path"
                        endpoints = @("/query", "/tables", "/schema?table=X", "/ping")
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
