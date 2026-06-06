-- DebitCards table for the Payment database
-- Each card has a starting balance of 1000; purchases deduct from the balance.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DebitCards')
BEGIN
    CREATE TABLE DebitCards (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        HolderName NVARCHAR(100) NOT NULL,
        CardNumber NVARCHAR(16) NOT NULL,
        ExpirationDate NVARCHAR(7) NOT NULL,
        CVC NVARCHAR(3) NOT NULL,
        Balance DECIMAL(10,2) NOT NULL DEFAULT 1000.00
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM DebitCards)
BEGIN
    INSERT INTO DebitCards (HolderName, CardNumber, ExpirationDate, CVC, Balance) VALUES
    ('ISRAEL ISRAELI', '1234567890123456', '12/2027', '123', 1000.00),
    ('RACHEL COHEN',   '9876543210987654', '06/2026', '456', 1000.00),
    ('DAVID LEVY',     '1111222233334444', '03/2028', '789', 1000.00),
    ('MICHAL GOLAN',   '5555666677778888', '09/2027', '321', 1000.00);
END
