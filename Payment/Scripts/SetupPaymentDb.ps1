# Setup script for PaymentDb.mdf - creates the database file in Payment folder
# Run this once from the repository root: .\Payment\Scripts\SetupPaymentDb.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path (Split-Path $PSScriptRoot)
$paymentPath = Join-Path $repoRoot "Payment"
$mdfPath = Join-Path $paymentPath "PaymentDb.mdf"
$ldfPath = Join-Path $paymentPath "PaymentDb_log.ldf"

Write-Host "Setting up PaymentDb in $paymentPath" -ForegroundColor Cyan

# Remove existing files if any
if (Test-Path $mdfPath) {
    Write-Host "Removing existing PaymentDb.mdf..." -ForegroundColor Yellow
    Remove-Item $mdfPath -Force
}
if (Test-Path $ldfPath) {
    Remove-Item $ldfPath -Force
}

# Drop catalog-based database if it exists
$dropDb = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'PaymentDb')
BEGIN
    ALTER DATABASE PaymentDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PaymentDb;
END
"@

try {
    sqlcmd -S "(LocalDB)\MSSQLLocalDB" -Q $dropDb 2>$null
    Write-Host "Dropped old catalog-based PaymentDb" -ForegroundColor Yellow
} catch {
    # Database might not exist, that's fine
}

# Create the database with file-based storage
$createDb = @"
CREATE DATABASE PaymentDb
ON PRIMARY (
    NAME = PaymentDb_Data,
    FILENAME = '$($mdfPath -replace '\\', '\\')',
    SIZE = 10MB,
    FILEGROWTH = 5MB
)
LOG ON (
    NAME = PaymentDb_Log,
    FILENAME = '$($ldfPath -replace '\\', '\\')',
    SIZE = 5MB,
    FILEGROWTH = 5MB
);
"@

Write-Host "Creating PaymentDb.mdf..." -ForegroundColor Cyan
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -Q $createDb

# Create the DebitCards table
$createTable = @"
USE PaymentDb;

CREATE TABLE DebitCards (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    HolderName NVARCHAR(100) NOT NULL,
    CardNumber NVARCHAR(16) NOT NULL,
    ExpirationDate NVARCHAR(7) NOT NULL,
    CVC NVARCHAR(3) NOT NULL,
    Balance DECIMAL(10,2) NOT NULL DEFAULT 1000.00
);

INSERT INTO DebitCards (HolderName, CardNumber, ExpirationDate, CVC, Balance) VALUES
('ISRAELI ISRAELI', '1234567890123456', '12/2027', '123', 1000.00),
('RACHEL COHEN',   '9876543210987654', '06/2026', '456', 1000.00),
('DAVID LEVY',     '1111222233334444', '03/2028', '789', 1000.00),
('MICHAL GOLAN',   '5555666677778888', '09/2027', '321', 1000.00);
"@

Write-Host "Creating DebitCards table and adding test data..." -ForegroundColor Cyan
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -d PaymentDb -Q $createTable

# Detach the database to make it portable
Write-Host "Detaching database..." -ForegroundColor Cyan
$detachDb = "EXEC sp_detach_db 'PaymentDb', 'true';"
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -Q $detachDb

Write-Host "`nPaymentDb setup complete!" -ForegroundColor Green
Write-Host "Database file: $mdfPath" -ForegroundColor Gray
Write-Host "4 test cards created with 1000 balance each" -ForegroundColor Gray
Write-Host "`nThe database will be attached automatically when the app starts." -ForegroundColor Gray
