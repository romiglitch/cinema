-- Run against the cinema application database (Shipping/App_Data/Dtb.mdf).
--
-- SSMS / Azure Data Studio:
--   1. Connect to (LocalDB)\MSSQLLocalDB
--   2. Start the Shipping site once (F5) so Dtb.mdf attaches, or attach Dtb.mdf manually
--   3. In the database dropdown, select "Dtb" (NOT master)
--   4. Execute this script
--
-- Optional: uncomment and set if your catalog name is not Dtb
-- USE [Dtb];
-- GO

SET NOCOUNT ON;

DECLARE @db sysname = DB_NAME();
DECLARE @nullEmailCount int;

IF @db IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(
        N'Current database is "%s". Switch to the cinema database (usually "Dtb") in the toolbar, then run this script again.',
        16, 1, @db);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    RAISERROR(
        N'dbo.Users was not found in database "%s". Attach Shipping\App_Data\Dtb.mdf, select that catalog in SSMS, then run again.',
        16, 1, @db);
    RETURN;
END;

-- Empty strings become NULL so they do not collide on the unique index
UPDATE dbo.Users
SET Email = NULL
WHERE Email IS NOT NULL AND LTRIM(RTRIM(Email)) = N'';

UPDATE dbo.Users
SET Email = LOWER(LTRIM(RTRIM(Email)))
WHERE Email IS NOT NULL;

IF EXISTS (
    SELECT Email
    FROM dbo.Users
    WHERE Email IS NOT NULL
    GROUP BY Email
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR(
        N'Duplicate non-null emails remain in dbo.Users. Run: SELECT UserId, FullName, Email FROM dbo.Users WHERE Email IN (SELECT Email FROM dbo.Users WHERE Email IS NOT NULL GROUP BY Email HAVING COUNT(*) > 1);',
        16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Users_Email'
      AND object_id = OBJECT_ID(N'dbo.Users')
      AND has_filter = 0
)
BEGIN
    DROP INDEX UX_Users_Email ON dbo.Users;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Users_Email'
      AND object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    -- Filtered index: unique emails when set; multiple NULLs allowed (legacy rows)
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email) WHERE Email IS NOT NULL;
END;

SELECT @nullEmailCount = COUNT(*) FROM dbo.Users WHERE Email IS NULL;

PRINT N'Email normalization completed on database: ' + @db;

IF @nullEmailCount > 0
BEGIN
    PRINT CAST(@nullEmailCount AS nvarchar(10)) + N' user(s) have no email and cannot log in until Email is set.';
    PRINT N'Example: UPDATE dbo.Users SET Email = N''user@example.com'' WHERE UserId = 1;';
END;
