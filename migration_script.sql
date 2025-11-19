START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN registered_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN password_reset_token_expires_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN password_reset_token_created_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN locked_until TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN last_password_change_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN last_login_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN deactivated_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN activation_token_expires_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN activation_token_created_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN activated_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN "PhoneVerifiedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN "LastModifiedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.users ALTER COLUMN "CreatedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.user_roles ALTER COLUMN expires_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.user_roles ALTER COLUMN assigned_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.user_roles ALTER COLUMN "LastModifiedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.user_roles ALTER COLUMN "CreatedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.user_profiles ALTER COLUMN updated_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.user_profiles ALTER COLUMN date_of_birth TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.user_profiles ALTER COLUMN created_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.user_profiles ALTER COLUMN "LastModifiedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.refresh_tokens ALTER COLUMN revoked_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.refresh_tokens ALTER COLUMN expires_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.refresh_tokens ALTER COLUMN created_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.phone_verifications ALTER COLUMN verified_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.phone_verifications ALTER COLUMN last_sent_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.phone_verifications ALTER COLUMN last_attempt_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.phone_verifications ALTER COLUMN expires_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.phone_verifications ALTER COLUMN created_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.phone_verifications ALTER COLUMN blocked_until TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.phone_verifications ALTER COLUMN "LastModifiedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.login_attempts ALTER COLUMN attempted_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.event_store ALTER COLUMN timestamp TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.customers ALTER COLUMN last_modified_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.customers ALTER COLUMN created_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.customers ADD notification_email_enabled boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.customers ADD notification_reminder_timing character varying(10) NOT NULL DEFAULT '24h';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.customers ADD notification_sms_enabled boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.customer_favorite_providers ALTER COLUMN added_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.customer_favorite_providers ALTER COLUMN "LastModifiedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.customer_favorite_providers ALTER COLUMN "CreatedAt" TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.authentication_sessions ALTER COLUMN started_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.authentication_sessions ALTER COLUMN last_activity_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.authentication_sessions ALTER COLUMN expires_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    ALTER TABLE user_management.authentication_sessions ALTER COLUMN ended_at TYPE timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    CREATE TABLE user_management.customer_booking_history (
        booking_id uuid NOT NULL,
        customer_id uuid NOT NULL,
        provider_id uuid NOT NULL,
        provider_name character varying(255) NOT NULL,
        service_name character varying(255) NOT NULL,
        start_time timestamp with time zone NOT NULL,
        status character varying(50) NOT NULL,
        total_price numeric(10,2),
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_customer_booking_history" PRIMARY KEY (booking_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    CREATE INDEX ix_customer_booking_history_customer_time ON user_management.customer_booking_history (customer_id, start_time DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    CREATE INDEX ix_customer_booking_history_provider ON user_management.customer_booking_history (provider_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    CREATE INDEX ix_customer_booking_history_status ON user_management.customer_booking_history (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251117155847_AddCustomerProfileFeatures') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251117155847_AddCustomerProfileFeatures', '9.0.4');
    END IF;
END $EF$;
COMMIT;

