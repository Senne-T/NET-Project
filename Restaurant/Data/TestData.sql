-- ============================================
-- Restaurant Test Data Script (COMPLETE)
-- ============================================
-- Realistische testdata met alle scenarios

USE [Restaurant];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ============================================
-- VERIFICATIE: Controleer of InitialData is uitgevoerd
-- ============================================
IF NOT EXISTS (SELECT 1 FROM Status WHERE Id = 5)
BEGIN
    PRINT '? FOUT: InitialData.sql is niet (volledig) uitgevoerd!';
    PRINT '   Status ID 5 (Niet Gestart) ontbreekt.';
    PRINT '';
    PRINT '   Voer eerst InitialData.sql uit voordat je TestData.sql draait.';
    PRINT '';
    RAISERROR('InitialData.sql moet eerst uitgevoerd worden', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

PRINT '============================================';
PRINT 'TESTDATA GENERATIE - COMPLETE VERSION';
PRINT 'Datum: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

-- Variabelen
DECLARE @Vandaag DATE = CAST(GETDATE() AS DATE);
DECLARE @Gisteren DATE = DATEADD(DAY, -1, @Vandaag);
DECLARE @Morgen DATE = DATEADD(DAY, 1, @Vandaag);
DECLARE @Overmorgen DATE = DATEADD(DAY, 2, @Vandaag);
DECLARE @Over10Dagen DATE = DATEADD(DAY, 10, @Vandaag);

-- ====================
-- CLEANUP
-- ====================
PRINT 'Cleanup actuele periode...';

DELETE FROM Bestelling WHERE ReservatieId IN (
    SELECT Id FROM Reservatie 
    WHERE Datum >= @Gisteren AND Datum <= @Overmorgen
    AND KlantId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE '%@example.be')
);

DELETE FROM TafelLijst WHERE ReservatieId IN (
    SELECT Id FROM Reservatie 
    WHERE Datum >= @Gisteren AND Datum <= @Overmorgen
    AND KlantId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE '%@example.be')
);

DELETE FROM Reservatie 
WHERE Datum >= @Gisteren AND Datum <= @Overmorgen
AND KlantId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE '%@example.be');

PRINT '? Cleanup voltooid';
PRINT '';

-- ====================
-- KLANTEN (25 met realistische namen)
-- ====================
PRINT 'Klanten aanmaken...';

-- Eerst bestaande testklanten opschonen (NIET de eigenaar en andere systeemaccounts)
DELETE FROM AspNetUserRoles 
WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE '%@example.be');

DELETE FROM AspNetUsers 
WHERE Email LIKE '%@example.be';

PRINT '? Oude testklanten verwijderd';

DECLARE @Voornamen TABLE (Id INT, Naam NVARCHAR(50));
DECLARE @Achternamen TABLE (Id INT, Naam NVARCHAR(50));

-- Voornamen toevoegen met expliciete Id
INSERT INTO @Voornamen (Id, Naam) VALUES 
(1, 'Emma'), (2, 'Lucas'), (3, 'Sophie'), (4, 'Noah'), (5, 'Marie'),
(6, 'Liam'), (7, 'Anna'), (8, 'Arthur'), (9, 'Louise'), (10, 'Louis'),
(11, 'Camille'), (12, 'Victor'), (13, 'Léa'), (14, 'Jules'), (15, 'Alice'),
(16, 'Hugo'), (17, 'Chloé'), (18, 'Gabriel'), (19, 'Manon'), (20, 'Nathan'),
(21, 'Olivia'), (22, 'Maxime'), (23, 'Charlotte'), (24, 'Thomas'), (25, 'Zoé');

-- Achternamen toevoegen met expliciete Id
INSERT INTO @Achternamen (Id, Naam) VALUES 
(1, 'Janssens'), (2, 'Peeters'), (3, 'Maes'), (4, 'Jacobs'), (5, 'Mertens'),
(6, 'Claes'), (7, 'Goossens'), (8, 'Wouters'), (9, 'De Smet'), (10, 'Vermeulen'),
(11, 'Dubois'), (12, 'Lambert'), (13, 'Martin'), (14, 'Bernard'), (15, 'Dupont'),
(16, 'Van den Berg'), (17, 'De Vries'), (18, 'Hendriks'), (19, 'Bakker'), (20, 'Vermeer'),
(21, 'Leroy'), (22, 'Simon'), (23, 'Laurent'), (24, 'Petit'), (25, 'Roux');

DECLARE @KlantCounter INT = 1;
DECLARE @UserId NVARCHAR(450);
DECLARE @Voornaam NVARCHAR(50);
DECLARE @Achternaam NVARCHAR(50);
DECLARE @Email NVARCHAR(256);
DECLARE @KlantRoleId NVARCHAR(450);

-- Haal KlantRoleId op
SELECT @KlantRoleId = Id FROM AspNetRoles WHERE NormalizedName = 'KLANT';

WHILE @KlantCounter <= 25
BEGIN
    SET @UserId = NEWID();
    
    -- Haal voornaam en achternaam op in aparte statements
    SELECT @Voornaam = Naam FROM @Voornamen WHERE Id = @KlantCounter;
    SELECT @Achternaam = Naam FROM @Achternamen WHERE Id = @KlantCounter;
    
    SET @Email = LOWER(@Voornaam) + '.' + LOWER(@Achternaam) + CAST(@KlantCounter AS NVARCHAR) + '@example.be';
    
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        Voornaam, Achternaam, Adres, Huisnummer, Postcode, Gemeente, LandId, Actief
    )
    VALUES (
        @UserId, @Email, UPPER(@Email), @Email, UPPER(@Email), 1,
        'AQAAAAIAAYagAAAAEJ1q8xH5Z2F5K3lJ4N1P0H6X9Y8W7V5U4T3S2R1Q0P9O8N7M6L5K4J3I2H1G0F==',
        NEWID(), NEWID(), 1, 0, 0, 0,
        @Voornaam, @Achternaam, 'Teststraat', CAST(@KlantCounter AS NVARCHAR), '2300', 'Turnhout', 1, 1
    );

    -- Rol toewijzen
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @KlantRoleId);

    SET @KlantCounter = @KlantCounter + 1;
END

DECLARE @AantalKlanten INT;
SELECT @AantalKlanten = COUNT(*) FROM AspNetUsers WHERE Email LIKE '%@example.be';
PRINT '? Klanten: ' + CAST(@AantalKlanten AS NVARCHAR);
PRINT '';
-- ====================
-- RESERVATIES
-- ====================
PRINT 'Reservaties aanmaken...';

DECLARE @Klanten TABLE (Id NVARCHAR(450), RowNum INT IDENTITY(1,1));
INSERT INTO @Klanten (Id) SELECT Id FROM AspNetUsers WHERE Email LIKE '%@example.be' ORDER BY NEWID();
DECLARE @MaxKlant INT = (SELECT COUNT(*) FROM @Klanten);
DECLARE @KlantIdx INT = 1;

DECLARE @Counter INT;
DECLARE @KlantId NVARCHAR(450);
DECLARE @TijdslotId INT;
DECLARE @AantalPersonen INT;
DECLARE @ReservatieId INT;
DECLARE @TafelId INT;
DECLARE @Score INT;
DECLARE @Opmerkingen NVARCHAR(MAX);

-- ====================
-- GISTEREN (12)
-- ====================

-- 6 betaald, geen enquête
SET @Counter = 1;
WHILE @Counter <= 6
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = CASE WHEN @Counter <= 2 THEN 1 WHEN @Counter <= 4 THEN 2 WHEN @Counter = 5 THEN 3 ELSE 4 END;
    SET @AantalPersonen = 2 + (@Counter % 5);
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Gisteren, @AantalPersonen, @TijdslotId, 'Afgelopen reservatie', 1, 0, -1, DATEADD(DAY, -2, @Gisteren));
    
    SET @ReservatieId = SCOPE_IDENTITY();
    SET @TafelId = CASE WHEN @AantalPersonen <= 2 THEN ((@Counter - 1) % 2) + 1 WHEN @AantalPersonen <= 4 THEN ((@Counter - 1) % 2) + 3 WHEN @AantalPersonen <= 6 THEN ((@Counter - 1) % 2) + 5 ELSE 7 END;
    
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, @TafelId);
    
    SET @Counter = @Counter + 1;
END

-- 4 betaald met enquête (scores 2-5)
SET @Counter = 1;
WHILE @Counter <= 4
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = ((@Counter - 1) % 4) + 1;
    SET @AantalPersonen = 2 + (@Counter % 5);
    SET @Score = CASE @Counter WHEN 1 THEN 5 WHEN 2 THEN 4 WHEN 3 THEN 3 ELSE 2 END;
    SET @Opmerkingen = CASE @Score
        WHEN 5 THEN 'Uitstekende service en heerlijk eten! Absoluut een aanrader.'
        WHEN 4 THEN 'Zeer goed, alleen de wachttijd was iets langer dan verwacht.'
        WHEN 3 THEN 'Goed eten, maar bediening kan vriendelijker.'
        ELSE 'Niet helemaal naar verwachting, eten was lauw.'
    END;
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, EvaluatieOpmerkingen, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Gisteren, @AantalPersonen, @TijdslotId, 'Afgehandelde reservatie', 1, 0, @Score, @Opmerkingen, DATEADD(DAY, -2, @Gisteren));
    
    SET @ReservatieId = SCOPE_IDENTITY();
    SET @TafelId = CASE WHEN @AantalPersonen <= 2 THEN ((@Counter - 1) % 2) + 1 WHEN @AantalPersonen <= 4 THEN ((@Counter - 1) % 2) + 3 WHEN @AantalPersonen <= 6 THEN ((@Counter - 1) % 2) + 5 ELSE 7 END;
    
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, @TafelId);
    
    SET @Counter = @Counter + 1;
END

-- 2 niet opgedaagd
SET @Counter = 1;
WHILE @Counter <= 2
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = CASE WHEN @Counter = 1 THEN 3 ELSE 4 END;
    SET @AantalPersonen = 2 + (@Counter % 3);
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Gisteren, @AantalPersonen, @TijdslotId, 'Niet opgedaagd', 0, 0, -1, DATEADD(DAY, -2, @Gisteren));
    
    SET @ReservatieId = SCOPE_IDENTITY();
    SET @TafelId = CASE WHEN @AantalPersonen <= 2 THEN @Counter WHEN @AantalPersonen <= 4 THEN @Counter + 2 ELSE 5 END;
    
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, @TafelId);
    
    SET @Counter = @Counter + 1;
END

PRINT '? Gisteren: 12 (6 betaald zonder enquête, 4 met enquête, 2 no-show)';

-- ====================
-- VANDAAG (14)
-- ====================

-- Lunch nog te komen (3)
SET @Counter = 1;
WHILE @Counter <= 3
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = CASE WHEN @Counter % 2 = 0 THEN 1 ELSE 2 END;
    SET @AantalPersonen = 2 + (@Counter % 3);
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Vandaag, @AantalPersonen, @TijdslotId, 'Lunch reservatie', 0, 0, -1, @Vandaag);
    
    SET @ReservatieId = SCOPE_IDENTITY();
    SET @TafelId = CASE WHEN @AantalPersonen <= 2 THEN ((@Counter - 1) % 2) + 1 WHEN @AantalPersonen <= 4 THEN ((@Counter - 1) % 2) + 3 ELSE 5 END;
    
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, @TafelId);
    
    SET @Counter = @Counter + 1;
END

-- Lunch aanwezig met bestellingen (5 scenario's)
SET @Counter = 1;
WHILE @Counter <= 5
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = CASE WHEN @Counter % 2 = 0 THEN 1 ELSE 2 END;
    SET @AantalPersonen = 2 + (@Counter % 4);
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Vandaag, @AantalPersonen, @TijdslotId, 'Klant aanwezig - lunch', 0, 1, -1, @Vandaag);
    
    SET @ReservatieId = SCOPE_IDENTITY();
    SET @TafelId = CASE WHEN @AantalPersonen <= 2 THEN CASE WHEN @Counter % 2 = 0 THEN 1 ELSE 2 END WHEN @AantalPersonen <= 4 THEN CASE WHEN @Counter % 2 = 0 THEN 3 ELSE 4 END ELSE CASE WHEN @Counter % 2 = 0 THEN 5 ELSE 6 END END;
    
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, @TafelId);
    
    -- Scenario 1: Voorgerecht geserveerd, hoofdgerecht in behandeling, extra dranken
    IF @Counter = 1
    BEGIN
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 12, @AantalPersonen, DATEADD(MINUTE, -25, GETDATE()), 3, NULL); -- Garnaalkroketten - Geserveerd
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 14, @AantalPersonen, DATEADD(MINUTE, -10, GETDATE()), 1, 'Medium rare'); -- Steak - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 4, @AantalPersonen, DATEADD(MINUTE, -25, GETDATE()), 3, NULL); -- Cola - Geserveerd
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 7, 2, DATEADD(MINUTE, -5, GETDATE()), 1, NULL); -- Jupiler - In Behandeling (extra bestelling)
    END
    
    -- Scenario 2: Veel items in behandeling, voorgerecht geserveerd
    ELSE IF @Counter = 2
    BEGIN
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 11, @AantalPersonen, DATEADD(MINUTE, -30, GETDATE()), 3, NULL); -- Carpaccio - Geserveerd
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 15, @AantalPersonen, DATEADD(MINUTE, -12, GETDATE()), 1, NULL); -- Zalm - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 16, 1, DATEADD(MINUTE, -12, GETDATE()), 1, NULL); -- Lasagne - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 5, @AantalPersonen, DATEADD(MINUTE, -30, GETDATE()), 3, NULL); -- Spa - Geserveerd
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 10, 2, DATEADD(MINUTE, -10, GETDATE()), 1, NULL); -- Merlot - In Behandeling
    END
    
    -- Scenario 3: Net besteld - alles Niet Gestart
    ELSE IF @Counter = 3
    BEGIN
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 13, @AantalPersonen, DATEADD(MINUTE, -3, GETDATE()), 5, NULL); -- Soep - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 19, 2, DATEADD(MINUTE, -3, GETDATE()), 5, NULL); -- Margherita - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 20, 2, DATEADD(MINUTE, -3, GETDATE()), 5, NULL); -- Quattro Stagioni - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 4, @AantalPersonen, DATEADD(MINUTE, -3, GETDATE()), 5, NULL); -- Cola - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 7, @AantalPersonen, DATEADD(MINUTE, -3, GETDATE()), 5, NULL); -- Jupiler - Niet Gestart
    END
    
    -- Scenario 4: Hoofdgerecht in behandeling, dessert net besteld
    ELSE IF @Counter = 4
    BEGIN
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 12, @AantalPersonen, DATEADD(MINUTE, -35, GETDATE()), 3, NULL); -- Garnaalkroketten - Geserveerd
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 14, @AantalPersonen, DATEADD(MINUTE, -15, GETDATE()), 1, 'Well done'); -- Steak - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 17, 1, DATEADD(MINUTE, -15, GETDATE()), 1, NULL); -- Caesar Salade - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 21, 2, DATEADD(MINUTE, -2, GETDATE()), 5, NULL); -- Tiramisu - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 23, 1, DATEADD(MINUTE, -2, GETDATE()), 5, NULL); -- Dame Blanche - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 2, @AantalPersonen, DATEADD(MINUTE, -2, GETDATE()), 5, NULL); -- Cappuccino - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 9, 2, DATEADD(MINUTE, -35, GETDATE()), 3, NULL); -- Chardonnay - Geserveerd
    END
    
    -- Scenario 5: Mixed - diverse statussen, salades en hoofdgerechten
    ELSE
    BEGIN
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 11, @AantalPersonen, DATEADD(MINUTE, -20, GETDATE()), 3, NULL); -- Carpaccio - Geserveerd
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 18, 2, DATEADD(MINUTE, -8, GETDATE()), 1, NULL); -- Geitenkaas salade - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 15, 2, DATEADD(MINUTE, -8, GETDATE()), 1, NULL); -- Zalmfilet - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 24, 1, DATEADD(MINUTE, -8, GETDATE()), 1, 'Extra pittig'); -- Lobster - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 6, 2, DATEADD(MINUTE, -20, GETDATE()), 3, NULL); -- Jus d'Orange - Geserveerd
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 22, 2, DATEADD(MINUTE, -1, GETDATE()), 5, NULL); -- Chocolademousse - Niet Gestart
    END
    
    SET @Counter = @Counter + 1;
END

-- Diner nog te komen (4)
SET @Counter = 1;
DECLARE @DinerOpmerking NVARCHAR(MAX);
WHILE @Counter <= 4
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = CASE WHEN @Counter % 2 = 0 THEN 3 ELSE 4 END;
    SET @AantalPersonen = 2 + (@Counter % 5);
    SET @DinerOpmerking = CASE @Counter WHEN 1 THEN 'Raamtafel graag' WHEN 2 THEN 'Vegetarisch menu' ELSE NULL END;
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Vandaag, @AantalPersonen, @TijdslotId, @DinerOpmerking, 0, 0, -1, @Vandaag);
    
    SET @ReservatieId = SCOPE_IDENTITY();
    SET @TafelId = CASE WHEN @AantalPersonen <= 2 THEN ((@Counter - 1) % 2) + 1 WHEN @AantalPersonen <= 4 THEN ((@Counter - 1) % 2) + 3 WHEN @AantalPersonen <= 6 THEN ((@Counter - 1) % 2) + 5 ELSE 7 END;
    
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, @TafelId);
    
    SET @Counter = @Counter + 1;
END

-- Diner net gearriveerd (2)
SET @Counter = 1;
WHILE @Counter <= 2
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = 3;
    SET @AantalPersonen = 2 + (@Counter % 3);
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Vandaag, @AantalPersonen, @TijdslotId, 'Diner net begonnen', 0, 1, -1, @Vandaag);
    
    SET @ReservatieId = SCOPE_IDENTITY();
    SET @TafelId = CASE WHEN @Counter = 1 THEN 7 ELSE 6 END;
    
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, @TafelId);
    
    -- Tafel 1: Net besteld - dranken en voorgerechten
    IF @Counter = 1
    BEGIN
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 8, @AantalPersonen, DATEADD(MINUTE, -5, GETDATE()), 5, NULL); -- Gin Tonic - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 9, @AantalPersonen, DATEADD(MINUTE, -5, GETDATE()), 5, NULL); -- Chardonnay - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 11, @AantalPersonen, DATEADD(MINUTE, -3, GETDATE()), 5, NULL); -- Carpaccio - Niet Gestart
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 12, @AantalPersonen, DATEADD(MINUTE, -3, GETDATE()), 5, NULL); -- Garnaalkroketten - Niet Gestart
    END
    -- Tafel 2: Dranken in behandeling, voorgerechten net besteld
    ELSE
    BEGIN
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 7, @AantalPersonen, DATEADD(MINUTE, -8, GETDATE()), 1, NULL); -- Jupiler - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 10, 2, DATEADD(MINUTE, -8, GETDATE()), 1, NULL); -- Merlot - In Behandeling
        INSERT INTO Bestelling (ReservatieId, ProductId, Aantal, TijdstipBestelling, StatusId, Opmerking)
        VALUES (@ReservatieId, 13, @AantalPersonen, DATEADD(MINUTE, -2, GETDATE()), 5, NULL); -- Soep - Niet Gestart
    END
    
    SET @Counter = @Counter + 1;
END

PRINT '? Vandaag: 14 (3 lunch komend, 5 lunch aanwezig, 4 diner komend, 2 diner gearriveerd)';

-- ====================
-- EIGENAAR RESERVATIE VANDAAG (1)
-- ====================

-- Zoek Eigenaar account (komt uit IdentitySeeding.cs)
DECLARE @EigenaarId NVARCHAR(450);

SELECT @EigenaarId = Id FROM AspNetUsers WHERE Email = 'restaurant.testmail.tm+eigenaar@gmail.com';

-- Alleen reservatie toevoegen als Eigenaar bestaat
IF @EigenaarId IS NOT NULL
BEGIN
    -- Maak reservatie voor eigenaar - vandaag lunch, aanwezig, GEEN bestellingen
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@EigenaarId, @Vandaag, 2, 1, 'Eigenaar test reservatie', 0, 1, -1, @Vandaag);

    SET @ReservatieId = SCOPE_IDENTITY();

    -- Wijs tafel 2 toe (kleine tafel, niet in gebruik door andere lunch reservaties)
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, 8);

    PRINT '? Eigenaar reservatie: Vandaag lunch, aanwezig, nog geen bestellingen (Tafel T08)';
END
ELSE
BEGIN
    PRINT '??  Eigenaar account niet gevonden (wordt aangemaakt bij eerste start applicatie)';
END

-- ====================
-- MORGEN (10)
-- ====================

SET @Counter = 1;
DECLARE @MorgenOpmerking NVARCHAR(MAX);
WHILE @Counter <= 10
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = ((@Counter - 1) % 4) + 1;
    SET @AantalPersonen = 2 + (@Counter % 5);
    SET @MorgenOpmerking = CASE 
        WHEN @Counter = 1 THEN 'Verjaardag - graag verrassing bij dessert'
        WHEN @Counter = 3 THEN 'Nut allergie'
        WHEN @Counter = 5 THEN 'Kinderstoel nodig'
        WHEN @Counter = 7 THEN 'Lactose intolerant'
        ELSE NULL
    END;
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Morgen, @AantalPersonen, @TijdslotId, @MorgenOpmerking, 0, 0, -1, DATEADD(DAY, -1, @Morgen));
    
    SET @ReservatieId = SCOPE_IDENTITY();
    SET @TafelId = CASE WHEN @AantalPersonen <= 2 THEN ((@Counter - 1) % 2) + 1 WHEN @AantalPersonen <= 4 THEN ((@Counter - 1) % 2) + 3 WHEN @AantalPersonen <= 6 THEN ((@Counter - 1) % 2) + 5 ELSE 7 END;
    
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, @TafelId);
    
    SET @Counter = @Counter + 1;
END

PRINT '? Morgen: 10 (diverse speciale verzoeken)';

-- ====================
-- EIGENAAR RESERVATIE MORGEN (1)
-- ====================

IF @EigenaarId IS NOT NULL
BEGIN
    -- Maak reservatie voor eigenaar - morgen diner, NIET aanwezig
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@EigenaarId, @Morgen, 4, 4, 'Eigenaar reservatie - niet aanwezig', 0, 0, -1, DATEADD(DAY, -1, @Morgen));

    SET @ReservatieId = SCOPE_IDENTITY();

    -- Wijs tafel 4 toe
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, 4);

    PRINT '? Eigenaar reservatie: Morgen diner, niet aanwezig (Tafel T04)';
END

-- ====================
-- OVERMORGEN (8)
-- ====================

SET @Counter = 1;
DECLARE @OvermorgenOpmerking NVARCHAR(MAX);
WHILE @Counter <= 8
BEGIN
    SELECT @KlantId = Id FROM @Klanten WHERE RowNum = ((@KlantIdx - 1) % @MaxKlant) + 1;
    SET @KlantIdx = @KlantIdx + 1;
    SET @TijdslotId = ((@Counter - 1) % 4) + 1;
    SET @AantalPersonen = 2 + (@Counter % 7);
    SET @OvermorgenOpmerking = CASE 
        WHEN @Counter = 1 THEN 'Zakelijk diner'
        WHEN @Counter = 2 THEN 'Glutenvrij dieet voor 2 personen'
        WHEN @Counter = 4 THEN 'Stilte hoek graag, kleine kinderen'
        WHEN @Counter = 6 THEN 'Romantisch diner - kaarsjes graag'
        ELSE NULL
    END;
    
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@KlantId, @Overmorgen, @AantalPersonen, @TijdslotId, @OvermorgenOpmerking, 0, 0, -1, DATEADD(DAY, -2, @Overmorgen));
    
    SET @ReservatieId = SCOPE_IDENTITY();
    
    IF @AantalPersonen > 8
    BEGIN
        INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, 7);
        INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, 1);
    END
    ELSE IF @AantalPersonen > 6
    BEGIN
        INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, 7);
    END
    ELSE IF @AantalPersonen > 4
    BEGIN
        INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, ((@Counter - 1) % 2) + 5);
    END
    ELSE IF @AantalPersonen > 2
    BEGIN
        INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, ((@Counter - 1) % 2) + 3);
    END
    ELSE
    BEGIN
        INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, ((@Counter - 1) % 2) + 1);
    END
    
    SET @Counter = @Counter + 1;
END

PRINT '? Overmorgen: 8 (grote groepen, speciale gelegenheden)';

-- ====================
-- EIGENAAR RESERVATIE OVER 10 DAGEN (1)
-- ====================

IF @EigenaarId IS NOT NULL
BEGIN
    -- Maak reservatie voor eigenaar - over 10 dagen lunch, NIET aanwezig
    INSERT INTO Reservatie (KlantId, Datum, AantalPersonen, TijdSlotId, Opmerking, Bestaald, IsAanwezig, EvaluatieAantalSterren, WelkomstmailVerstuurdOp)
    VALUES (@EigenaarId, @Over10Dagen, 2, 1, 'Eigenaar reservatie - niet aanwezig', 0, 0, -1, NULL);

    SET @ReservatieId = SCOPE_IDENTITY();

    -- Wijs tafel 1 toe
    INSERT INTO TafelLijst (ReservatieId, TafelId) VALUES (@ReservatieId, 1);

    PRINT '? Eigenaar reservatie: Over 10 dagen lunch, niet aanwezig (Tafel T01)';
END

COMMIT TRANSACTION;

PRINT '';
PRINT '============================================';
PRINT '? TESTDATA SUCCESVOL AANGEMAAKT!';
PRINT '============================================';
PRINT '';
PRINT 'OVERZICHT:';

DECLARE @TotaalKlanten INT, @TotaalReservaties INT;
SELECT @TotaalKlanten = COUNT(*) FROM AspNetUsers WHERE Email LIKE '%@example.be';
SELECT @TotaalReservaties = COUNT(*) FROM Reservatie WHERE KlantId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE '%@example.be');

PRINT '  Klanten: ' + CAST(@TotaalKlanten AS NVARCHAR) + '/25';
PRINT '  Reservaties totaal: ' + CAST(@TotaalReservaties AS NVARCHAR);
PRINT '';
PRINT 'ACTUELE PERIODE:';
PRINT '  Gisteren: 12 (6 betaald zonder enquête, 4 met enquête, 2 no-show)';
PRINT '  Vandaag: 14 (lunch + diner, diverse statussen)';
PRINT '    + 1 Eigenaar reservatie (aanwezig, nog geen bestellingen - indien account bestaat)';
PRINT '  Morgen: 10 (speciale verzoeken)';
PRINT '    + 1 Eigenaar reservatie (niet aanwezig - indien account bestaat)';
PRINT '  Overmorgen: 8 (grote groepen)';
PRINT '  Over 10 dagen: 1 Eigenaar reservatie (niet aanwezig - indien account bestaat)';
PRINT '';
PRINT 'LOGIN:';
PRINT '  Klant: emma.janssens1@example.be | Test123!';
PRINT '  Eigenaar: restaurant.testmail.tm+eigenaar@gmail.com | Ww#123';
PRINT '';
PRINT '============================================';

SET NOCOUNT OFF;
GO
