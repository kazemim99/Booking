-- ========================================
-- Migration: Add Phone Verification Support
-- Date: 2025-01-09
-- Description: Adds PhoneVerifications table and updates Users table for passwordless authentication
-- ========================================

-- 1. Create PhoneVerifications table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PhoneVerifications' AND schema_id = SCHEMA_ID('user_management'))
BEGIN
    CREATE TABLE user_management.PhoneVerifications (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        PhoneNumber NVARCHAR(20) NOT NULL,
        CountryCode NVARCHAR(5) NOT NULL,
        HashedCode NVARCHAR(256) NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        IsVerified BIT NOT NULL DEFAULT 0,
        VerifiedAt DATETIME2 NULL,
        AttemptCount INT NOT NULL DEFAULT 0,
        MaxAttempts INT NOT NULL DEFAULT 3,
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    -- Create indexes
    CREATE INDEX IX_PhoneVerifications_PhoneNumber
        ON user_management.PhoneVerifications(PhoneNumber);

    CREATE INDEX IX_PhoneVerifications_ExpiresAt
        ON user_management.PhoneVerifications(ExpiresAt);

    CREATE INDEX IX_PhoneVerifications_PhoneNumber_Status
        ON user_management.PhoneVerifications(PhoneNumber, IsVerified, ExpiresAt);

    PRINT 'PhoneVerifications table created successfully';
END
ELSE
BEGIN
    PRINT 'PhoneVerifications table already exists';
END
GO

-- 2. Add phone-related columns to Users table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('user_management.Users') AND name = 'PhoneNumber')
BEGIN
    ALTER TABLE user_management.Users
    ADD PhoneNumber NVARCHAR(20) NULL,
        PhoneNumberVerified BIT NOT NULL DEFAULT 0,
        PhoneVerifiedAt DATETIME2 NULL;

    -- Create index
    CREATE INDEX IX_Users_PhoneNumber
        ON user_management.Users(PhoneNumber);

    PRINT 'Phone columns added to Users table successfully';
END
ELSE
BEGIN
    PRINT 'Phone columns already exist in Users table';
END
GO

-- 3. Create stored procedure for cleanup (optional - for background jobs)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CleanupExpiredPhoneVerifications' AND schema_id = SCHEMA_ID('user_management'))
BEGIN
    DROP PROCEDURE user_management.sp_CleanupExpiredPhoneVerifications;
END
GO

CREATE PROCEDURE user_management.sp_CleanupExpiredPhoneVerifications
    @CutoffDateTime DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default to 1 hour ago if not specified
    IF @CutoffDateTime IS NULL
        SET @CutoffDateTime = DATEADD(HOUR, -1, GETUTCDATE());

    DECLARE @DeletedCount INT;

    DELETE FROM user_management.PhoneVerifications
    WHERE ExpiresAt < @CutoffDateTime
      AND IsVerified = 0;

    SET @DeletedCount = @@ROWCOUNT;

    SELECT @DeletedCount AS DeletedCount;

    PRINT 'Cleaned up ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' expired phone verifications';
END
GO

-- 4. Grant permissions (adjust based on your security setup)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON user_management.PhoneVerifications TO booksy_api_user;
-- GRANT EXECUTE ON user_management.sp_CleanupExpiredPhoneVerifications TO booksy_api_user;

PRINT '========================================';
PRINT 'Phone Verification Migration Completed Successfully';
PRINT '========================================';
PRINT 'Tables Created/Updated:';
PRINT '  - user_management.PhoneVerifications (new)';
PRINT '  - user_management.Users (added phone columns)';
PRINT 'Stored Procedures:';
PRINT '  - user_management.sp_CleanupExpiredPhoneVerifications';
PRINT '========================================';
