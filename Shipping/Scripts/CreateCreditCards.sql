-- טבלת כרטיסי אשראי לבדיקת תשלום (הרץ פעם אחת על Dtb)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CreditCards')
BEGIN
    CREATE TABLE CreditCards (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        HolderName NVARCHAR(100) NOT NULL,
        CardNumber NVARCHAR(16) NOT NULL,
        ExpirationDate NVARCHAR(7) NOT NULL,
        CVC NVARCHAR(3) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM CreditCards)
BEGIN
    INSERT INTO CreditCards (HolderName, CardNumber, ExpirationDate, CVC) VALUES
    ('ISRAEL ISRAELI', '1234567890123456', '12/2027', '123'),
    ('RACHEL COHEN',   '9876543210987654', '06/2026', '456'),
    ('DAVID LEVY',     '1111222233334444', '03/2028', '789'),
    ('MICHAL GOLAN',   '5555666677778888', '09/2027', '321');
END
