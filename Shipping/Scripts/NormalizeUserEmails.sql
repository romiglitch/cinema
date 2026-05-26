-- להריץ מול מסד הנתונים של האפליקציה (Shipping/App_Data/Dtb.mdf).
--
-- SSMS / Azure Data Studio:
--   1. התחברות ל-(LocalDB)\MSSQLLocalDB
--   2. הפעלת אתר Shipping פעם אחת (F5) כדי ש-Dtb.mdf יצורף, או צירוף Dtb.mdf ידנית
--   3. בחירת מסד "Dtb" בתפריט (לא master)
--   4. הרצת הסקריפט
--
-- אופציונלי: לבטל הערה ולעדכן אם שם הקטלוג שונה מ-Dtb
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

-- מחרוזות ריקות הופכות ל-NULL כדי שלא יתנגשו באינדקס הייחודי
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
    -- אינדקס ייחודי מסונן: ייחודיות לאימיילים קיימים, עם אפשרות ל-NULL ישנים
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email) WHERE Email IS NOT NULL;
END;

SELECT @nullEmailCount = COUNT(*) FROM dbo.Users WHERE Email IS NULL;

PRINT N'Email normalization completed on database: ' + @db;

IF @nullEmailCount > 0
BEGIN
    PRINT CAST(@nullEmailCount AS nvarchar(10)) + N' user(s) have no email and cannot log in until Email is set.';
    PRINT N'Example: UPDATE dbo.Users SET Email = N''user@example.com'' WHERE UserId = 1;';
END;
