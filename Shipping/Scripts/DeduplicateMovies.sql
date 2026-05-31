-- Remove duplicate Movie rows and repoint foreign keys.
--
-- Duplicate rule (same as HomePage import logic):
--   1. Same TmdbId (when TmdbId IS NOT NULL and <> 0) -> keep lowest Id
--   2. Same Title among remaining rows              -> keep lowest Id
--
-- Updates:
--   Screening.MovieId
--   MovieGenres.IdMovie (merge genres, skip pairs that already exist)
--
-- Tickets reference Screening, not Movie directly — no Tickets update needed.
--
-- Run on Dtb. Review reports, then COMMIT or ROLLBACK at the bottom.

SET NOCOUNT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Select database Dtb before running this script.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Movie', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.Movie was not found.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Movie_Backup_BeforeDedup', N'U') IS NOT NULL
    DROP TABLE dbo.Movie_Backup_BeforeDedup;

SELECT *
INTO dbo.Movie_Backup_BeforeDedup
FROM dbo.Movie;

IF OBJECT_ID(N'dbo.MovieGenres_Backup_BeforeDedup', N'U') IS NOT NULL
    DROP TABLE dbo.MovieGenres_Backup_BeforeDedup;

IF OBJECT_ID(N'dbo.MovieGenres', N'U') IS NOT NULL
BEGIN
    SELECT *
    INTO dbo.MovieGenres_Backup_BeforeDedup
    FROM dbo.MovieGenres;
END;

IF OBJECT_ID(N'tempdb..#DuplicateMap') IS NOT NULL DROP TABLE #DuplicateMap;

CREATE TABLE #DuplicateMap
(
    DupId int NOT NULL PRIMARY KEY,
    KeepId int NOT NULL,
    DupReason nvarchar(20) NOT NULL
);

;WITH TmdbDupes AS
(
    SELECT
        m.Id,
        MIN(m.Id) OVER (PARTITION BY m.TmdbId) AS KeepId
    FROM dbo.Movie m
    WHERE m.TmdbId IS NOT NULL
      AND m.TmdbId <> 0
)
INSERT INTO #DuplicateMap (DupId, KeepId, DupReason)
SELECT Id, KeepId, N'TmdbId'
FROM TmdbDupes
WHERE Id <> KeepId;

;WITH TitleDupes AS
(
    SELECT
        m.Id,
        MIN(m.Id) OVER (PARTITION BY m.Title) AS KeepId
    FROM dbo.Movie m
    WHERE NOT EXISTS (SELECT 1 FROM #DuplicateMap d WHERE d.DupId = m.Id)
)
INSERT INTO #DuplicateMap (DupId, KeepId, DupReason)
SELECT Id, KeepId, N'Title'
FROM TitleDupes
WHERE Id <> KeepId;

PRINT N'--- Duplicate map (DupId -> KeepId) ---';
SELECT
    d.DupId,
    dup.Title AS DupTitle,
    d.KeepId,
    keep.Title AS KeepTitle,
    dup.TmdbId,
    d.DupReason
FROM #DuplicateMap d
JOIN dbo.Movie dup ON dup.Id = d.DupId
JOIN dbo.Movie keep ON keep.Id = d.KeepId
ORDER BY d.KeepId, d.DupId;

IF NOT EXISTS (SELECT 1 FROM #DuplicateMap)
BEGIN
    PRINT N'No duplicate movies found.';
    RETURN;
END;

PRINT N'--- Screenings that will be repointed ---';
SELECT
    s.ScreeningId,
    s.MovieId AS OldMovieId,
    d.KeepId AS NewMovieId,
    s.Hall,
    s.StartTime,
    s.EndTime
FROM dbo.Screening s
JOIN #DuplicateMap d ON d.DupId = s.MovieId
ORDER BY d.KeepId, s.StartTime;

PRINT N'--- Possible screening conflicts after merge (same hall + overlapping time) ---';
SELECT
    s1.ScreeningId AS ScreeningId_Existing,
    s2.ScreeningId AS ScreeningId_Moved,
    s1.MovieId AS KeepMovieId,
    d.DupId AS DupMovieId,
    s1.Hall,
    s1.StartTime AS ExistingStart,
    s2.StartTime AS MovedStart
FROM dbo.Screening s1
JOIN dbo.Screening s2
    ON s1.MovieId = s2.MovieId
   AND s1.Hall = s2.Hall
   AND s1.StartTime < s2.EndTime
   AND s2.StartTime < s1.EndTime
   AND s1.ScreeningId <> s2.ScreeningId
JOIN #DuplicateMap d ON d.KeepId = s1.MovieId AND d.DupId = s2.MovieId
ORDER BY s1.StartTime, s1.Hall;

BEGIN TRANSACTION;

UPDATE s
SET s.MovieId = d.KeepId
FROM dbo.Screening s
JOIN #DuplicateMap d ON d.DupId = s.MovieId;

IF OBJECT_ID(N'dbo.MovieGenres', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.MovieGenres (IdMovie, IdGenre)
    SELECT DISTINCT d.KeepId, mg.IdGenre
    FROM dbo.MovieGenres mg
    JOIN #DuplicateMap d ON d.DupId = mg.IdMovie
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.MovieGenres keepMg
        WHERE keepMg.IdMovie = d.KeepId
          AND keepMg.IdGenre = mg.IdGenre
    );

    DELETE mg
    FROM dbo.MovieGenres mg
    JOIN #DuplicateMap d ON d.DupId = mg.IdMovie;
END;

DELETE m
FROM dbo.Movie m
JOIN #DuplicateMap d ON d.DupId = m.Id;

PRINT N'--- Summary ---';
SELECT DupReason, COUNT(*) AS RemovedCount
FROM #DuplicateMap
GROUP BY DupReason;

SELECT COUNT(*) AS RemainingMovies FROM dbo.Movie;

PRINT N'--- Remaining movies ---';
SELECT Id, Title, Duration, Age, TmdbId
FROM dbo.Movie
ORDER BY Title, Id;

-- Dry run by default. When satisfied, comment ROLLBACK and uncomment COMMIT.
ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;

PRINT N'Done. Transaction rolled back (dry run). Uncomment COMMIT and comment ROLLBACK to apply.';
