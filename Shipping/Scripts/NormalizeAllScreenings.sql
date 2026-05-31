-- Snap every Screening to the booking grid used by ScreeningsEditor.aspx.cs.
--
-- Per movie:
--   slotMinutes = Movie.Duration rounded up to 5 minutes + 35
--   daily slots start at 09:00, back-to-back, until the slot would end after midnight
--
-- For each screening (chronological order):
--   1. Try valid slots on the screening day (and previous day if start is before 09:00)
--   2. Prefer the slot nearest the original StartTime
--   3. Keep the same Hall when possible
--   4. UPDATE when a free slot is found; DELETE when none exists
--      (screenings with sold tickets are kept unchanged and reported)
--
-- Set @MovieIdFilter to a single Movie.Id to normalize one title only (e.g. 1594 for מייקל).
-- Run on Dtb. Review reports, then COMMIT or ROLLBACK at the bottom.

SET NOCOUNT ON;

DECLARE @MovieIdFilter int = NULL;  -- e.g. 1594 for מייקל only; NULL = all movies

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Select database Dtb before running this script.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Screening', N'U') IS NULL OR OBJECT_ID(N'dbo.Movie', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.Screening or dbo.Movie was not found.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Screening_Backup_BeforeNormalize', N'U') IS NOT NULL
    DROP TABLE dbo.Screening_Backup_BeforeNormalize;

SELECT s.*
INTO dbo.Screening_Backup_BeforeNormalize
FROM dbo.Screening s
WHERE @MovieIdFilter IS NULL OR s.MovieId = @MovieIdFilter;

IF NOT EXISTS (SELECT 1 FROM dbo.Screening_Backup_BeforeNormalize)
BEGIN
    PRINT N'No screenings matched the filter.';
    RETURN;
END;

IF OBJECT_ID(N'tempdb..#NormMovieSlot') IS NOT NULL DROP TABLE #NormMovieSlot;
IF OBJECT_ID(N'tempdb..#NormDays') IS NOT NULL DROP TABLE #NormDays;
IF OBJECT_ID(N'tempdb..#NormSlots') IS NOT NULL DROP TABLE #NormSlots;
IF OBJECT_ID(N'tempdb..#NormPlan') IS NOT NULL DROP TABLE #NormPlan;
IF OBJECT_ID(N'tempdb..#NormAssigned') IS NOT NULL DROP TABLE #NormAssigned;

SELECT
    m.Id AS MovieId,
    m.Title,
    m.Duration,
    m.Duration
        + CASE WHEN m.Duration % 5 = 0 THEN 0 ELSE (5 - m.Duration % 5) END
        + 35 AS SlotMinutes
INTO #NormMovieSlot
FROM dbo.Movie m
WHERE @MovieIdFilter IS NULL OR m.Id = @MovieIdFilter;

PRINT N'--- Slot length per movie (Duration rounded up to 5 + 35) ---';
SELECT MovieId, Title, Duration, SlotMinutes
FROM #NormMovieSlot
ORDER BY Title;

DECLARE @MinDay date;
DECLARE @MaxDay date;

SELECT
    @MinDay = DATEADD(day, -1, MIN(CAST(s.StartTime AS date))),
    @MaxDay = MAX(CAST(s.StartTime AS date))
FROM dbo.Screening_Backup_BeforeNormalize s;

;WITH DaySeries AS
(
    SELECT @MinDay AS DayDate
    UNION ALL
    SELECT DATEADD(day, 1, DayDate)
    FROM DaySeries
    WHERE DayDate < @MaxDay
)
SELECT DayDate
INTO #NormDays
FROM DaySeries
OPTION (MAXRECURSION 400);

;WITH MovieDays AS
(
    SELECT
        ms.MovieId,
        ms.SlotMinutes,
        d.DayDate
    FROM #NormMovieSlot ms
    CROSS JOIN #NormDays d
),
SlotSeries AS
(
    SELECT
        md.MovieId,
        md.SlotMinutes,
        md.DayDate,
        0 AS SlotIndex,
        DATEADD(minute, 9 * 60, CAST(md.DayDate AS datetime)) AS SlotStart
    FROM MovieDays md

    UNION ALL

    SELECT
        ss.MovieId,
        ss.SlotMinutes,
        ss.DayDate,
        ss.SlotIndex + 1,
        DATEADD(minute, ss.SlotMinutes, ss.SlotStart)
    FROM SlotSeries ss
    WHERE DATEADD(minute, ss.SlotMinutes, ss.SlotStart)
          <= DATEADD(day, 1, CAST(ss.DayDate AS datetime))
)
SELECT
    ss.MovieId,
    ss.SlotMinutes,
    ss.DayDate,
    ss.SlotIndex,
    ss.SlotStart,
    DATEADD(minute, ss.SlotMinutes, ss.SlotStart) AS SlotEnd
INTO #NormSlots
FROM SlotSeries ss
OPTION (MAXRECURSION 32767);

CREATE INDEX IX_NormSlots_Lookup ON #NormSlots (MovieId, DayDate, SlotStart);

SELECT
    s.ScreeningId,
    s.MovieId,
    m.Title AS MovieTitle,
    s.Hall,
    s.StartTime AS OrigStart,
    s.EndTime AS OrigEnd,
    CAST(NULL AS datetime) AS NewStart,
    CAST(NULL AS datetime) AS NewEnd,
    CAST(N'PENDING' AS nvarchar(20)) AS Action,
    CAST(NULL AS nvarchar(200)) AS Note
INTO #NormPlan
FROM dbo.Screening_Backup_BeforeNormalize s
JOIN dbo.Movie m ON m.Id = s.MovieId;

CREATE TABLE #NormAssigned
(
    ScreeningId int NOT NULL PRIMARY KEY,
    MovieId int NOT NULL,
    Hall int NOT NULL,
    NewStart datetime NOT NULL,
    NewEnd datetime NOT NULL
);

DECLARE @ScrId int;
DECLARE @ScrMovieId int;
DECLARE @ScrTitle nvarchar(200);
DECLARE @ScrHall int;
DECLARE @ScrOrigStart datetime;
DECLARE @ScrOrigEnd datetime;
DECLARE @ScrNewStart datetime;
DECLARE @ScrNewEnd datetime;
DECLARE @ScrHasTickets bit;

DECLARE screening_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT ScreeningId, MovieId, MovieTitle, Hall, OrigStart, OrigEnd
    FROM #NormPlan
    ORDER BY OrigStart, ScreeningId;

OPEN screening_cursor;
FETCH NEXT FROM screening_cursor
INTO @ScrId, @ScrMovieId, @ScrTitle, @ScrHall, @ScrOrigStart, @ScrOrigEnd;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ScrNewStart = NULL;
    SET @ScrNewEnd = NULL;
    SET @ScrHasTickets = CASE
        WHEN OBJECT_ID(N'dbo.Tickets', N'U') IS NULL THEN 0
        WHEN EXISTS (SELECT 1 FROM dbo.Tickets t WHERE t.Screening = @ScrId) THEN 1
        ELSE 0
    END;

    IF EXISTS (
        SELECT 1
        FROM #NormSlots sl
        WHERE sl.MovieId = @ScrMovieId
          AND sl.SlotStart = @ScrOrigStart
          AND sl.SlotEnd = @ScrOrigEnd
          AND NOT EXISTS (
              SELECT 1
              FROM #NormAssigned asn
              WHERE asn.Hall = @ScrHall
                AND asn.NewStart < sl.SlotEnd
                AND sl.SlotStart < asn.NewEnd
          )
          AND NOT EXISTS (
              SELECT 1
              FROM #NormAssigned asn
              WHERE asn.MovieId = @ScrMovieId
                AND asn.ScreeningId <> @ScrId
                AND asn.NewStart < sl.SlotEnd
                AND sl.SlotStart < asn.NewEnd
          )
    )
    BEGIN
        SELECT TOP (1)
            @ScrNewStart = sl.SlotStart,
            @ScrNewEnd = sl.SlotEnd
        FROM #NormSlots sl
        WHERE sl.MovieId = @ScrMovieId
          AND sl.SlotStart = @ScrOrigStart
          AND sl.SlotEnd = @ScrOrigEnd;
    END
    ELSE
    BEGIN
        SELECT TOP (1)
            @ScrNewStart = sl.SlotStart,
            @ScrNewEnd = sl.SlotEnd
        FROM #NormSlots sl
        WHERE sl.MovieId = @ScrMovieId
          AND (
              sl.DayDate = CAST(@ScrOrigStart AS date)
              OR (
                  CAST(@ScrOrigStart AS time) < CAST(N'09:00:00' AS time)
                  AND sl.DayDate = DATEADD(day, -1, CAST(@ScrOrigStart AS date))
              )
          )
          AND NOT EXISTS (
              SELECT 1
              FROM #NormAssigned asn
              WHERE asn.Hall = @ScrHall
                AND asn.NewStart < sl.SlotEnd
                AND sl.SlotStart < asn.NewEnd
          )
          AND NOT EXISTS (
              SELECT 1
              FROM #NormAssigned asn
              WHERE asn.MovieId = @ScrMovieId
                AND asn.NewStart < sl.SlotEnd
                AND sl.SlotStart < asn.NewEnd
          )
        ORDER BY
            ABS(DATEDIFF(minute, @ScrOrigStart, sl.SlotStart)),
            sl.SlotStart;
    END;

    IF @ScrNewStart IS NOT NULL
    BEGIN
        INSERT INTO #NormAssigned (ScreeningId, MovieId, Hall, NewStart, NewEnd)
        VALUES (@ScrId, @ScrMovieId, @ScrHall, @ScrNewStart, @ScrNewEnd);

        UPDATE #NormPlan
        SET
            NewStart = @ScrNewStart,
            NewEnd = @ScrNewEnd,
            Action = CASE
                WHEN @ScrNewStart = @ScrOrigStart AND @ScrNewEnd = @ScrOrigEnd THEN N'KEEP'
                ELSE N'UPDATE'
            END,
            Note = CASE
                WHEN @ScrNewStart = @ScrOrigStart AND @ScrNewEnd = @ScrOrigEnd THEN N'Already on grid'
                ELSE N'Snapped to nearest slot, same hall'
            END
        WHERE ScreeningId = @ScrId;
    END
    ELSE IF @ScrHasTickets = 1
    BEGIN
        UPDATE #NormPlan
        SET
            NewStart = @ScrOrigStart,
            NewEnd = @ScrOrigEnd,
            Action = N'KEEP',
            Note = N'No free slot; has tickets — left unchanged'
        WHERE ScreeningId = @ScrId;
    END
    ELSE
    BEGIN
        UPDATE #NormPlan
        SET
            Action = N'DELETE',
            Note = N'No free slot on grid for this hall/day'
        WHERE ScreeningId = @ScrId;
    END;

    FETCH NEXT FROM screening_cursor
    INTO @ScrId, @ScrMovieId, @ScrTitle, @ScrHall, @ScrOrigStart, @ScrOrigEnd;
END;

CLOSE screening_cursor;
DEALLOCATE screening_cursor;

PRINT N'--- Summary by action ---';
SELECT Action, COUNT(*) AS Cnt
FROM #NormPlan
GROUP BY Action
ORDER BY Action;

PRINT N'--- Summary by movie ---';
SELECT
    p.MovieId,
    p.MovieTitle,
    ms.SlotMinutes,
    SUM(CASE WHEN p.Action = N'KEEP' THEN 1 ELSE 0 END) AS Kept,
    SUM(CASE WHEN p.Action = N'UPDATE' THEN 1 ELSE 0 END) AS Updated,
    SUM(CASE WHEN p.Action = N'DELETE' THEN 1 ELSE 0 END) AS Deleted
FROM #NormPlan p
JOIN #NormMovieSlot ms ON ms.MovieId = p.MovieId
GROUP BY p.MovieId, p.MovieTitle, ms.SlotMinutes
ORDER BY p.MovieTitle;

PRINT N'--- Screenings to UPDATE (first 100) ---';
SELECT TOP (100)
    p.ScreeningId,
    p.MovieTitle,
    p.Hall,
    p.OrigStart,
    p.OrigEnd,
    p.NewStart,
    p.NewEnd,
    DATEDIFF(minute, p.OrigStart, p.NewStart) AS StartShiftMinutes
FROM #NormPlan p
WHERE p.Action = N'UPDATE'
ORDER BY p.MovieTitle, p.NewStart;

PRINT N'--- Screenings to DELETE ---';
SELECT
    p.ScreeningId,
    p.MovieTitle,
    p.Hall,
    p.OrigStart,
    p.OrigEnd,
    p.Note
FROM #NormPlan p
WHERE p.Action = N'DELETE'
ORDER BY p.MovieTitle, p.OrigStart;

PRINT N'--- Kept unchanged (has tickets, no slot) ---';
SELECT
    p.ScreeningId,
    p.MovieTitle,
    p.Hall,
    p.OrigStart,
    p.OrigEnd,
    p.Note
FROM #NormPlan p
WHERE p.Action = N'KEEP' AND p.Note LIKE N'%tickets%'
ORDER BY p.MovieTitle, p.OrigStart;

BEGIN TRANSACTION;

UPDATE s
SET
    s.StartTime = p.NewStart,
    s.EndTime = p.NewEnd
FROM dbo.Screening s
JOIN #NormPlan p ON p.ScreeningId = s.ScreeningId
WHERE p.Action = N'UPDATE';

DELETE s
FROM dbo.Screening s
JOIN #NormPlan p ON p.ScreeningId = s.ScreeningId
WHERE p.Action = N'DELETE';

PRINT N'--- Remaining misaligned screenings (should be 0 except ticket holds) ---';
SELECT
    s.ScreeningId,
    m.Title,
    ms.SlotMinutes,
    s.Hall,
    s.StartTime,
    s.EndTime,
    DATEDIFF(minute, s.StartTime, s.EndTime) AS StoredLength
FROM dbo.Screening s
JOIN dbo.Movie m ON m.Id = s.MovieId
JOIN #NormMovieSlot ms ON ms.MovieId = s.MovieId
WHERE (@MovieIdFilter IS NULL OR s.MovieId = @MovieIdFilter)
  AND NOT EXISTS (
      SELECT 1
      FROM #NormSlots sl
      WHERE sl.MovieId = s.MovieId
        AND sl.SlotStart = s.StartTime
        AND sl.SlotEnd = s.EndTime
  )
ORDER BY m.Title, s.StartTime;

-- Dry run by default. When satisfied, comment ROLLBACK and uncomment COMMIT.
ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;

PRINT N'Done. Transaction rolled back (dry run). Uncomment COMMIT and comment ROLLBACK to apply.';
