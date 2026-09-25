-- refactor-identity-and-membership §1.2/§1.3: read-only duplicate-phone-number report.
--
-- WHY THIS EXISTS
-- ----------------
-- §1.2 (a partial unique index on user_management.users."PhoneNumber") cannot simply be
-- added: it auto-applies via Database.Migrate() at host startup, and would fail to apply --
-- blocking every future deploy -- if the target database already holds duplicate phone
-- numbers among non-deleted users. This script is the safe, read-only first step: run it
-- against the real environment (staging/production) BEFORE adding that migration, to confirm
-- the data is actually clean.
--
-- This is not a theoretical concern. 2026-09-08: a new integration test
-- (AsanRezerve.UserManagement.IntegrationTests.Services.PersonProvisioningConcurrencyTests)
-- proved that two concurrent sign-ins for the same brand-new phone number (one as a
-- customer, one as a provider) currently create two separate person rows -- nothing at the
-- database level stops it. That test ran against a fresh, empty database; this script checks
-- whether the same thing has already happened for real.
--
-- Column names verified against the actual generated schema (`dotnet ef migrations script`),
-- not just the C# configuration -- users."PhoneNumber"/"NationalNumber"/"Status"/"Type"/
-- "CreatedAt" are PascalCase with no explicit HasColumnName (EF's default preserves the C#
-- property name, or they're the shared auditable-base columns); user_profiles' phone columns
-- are snake_case AND split (phone_country_code / phone_national_number) -- its configuration
-- maps a combined "phone_number" column and then immediately calls .Ignore() on the same
-- property in the next line, so that column is never actually created.
--
-- HOW TO RUN
-- ----------
-- psql "$CONNECTION_STRING" -f scripts/find-duplicate-phone-numbers.sql
-- (or paste into any Postgres client connected to the `asan_rezerve` database)
--
-- WHAT TO DO WITH THE RESULT
-- --------------------------
-- - Zero rows from Queries 1 and 2: the environment is clean; §1.2's migration can be added
--   and will apply without incident.
-- - Any rows: do NOT add §1.2's migration yet. Each group needs manual review (§1.3, "no
--   destructive merge") -- which of the duplicate accounts is the "real" one, whether any
--   have distinct data (bookings, memberships, payment history) that must be reconciled
--   rather than dropped, before the accounts can be merged or one soft-deleted.

-- ============================================================================
-- Query 1: exact duplicates -- two or more non-deleted users with the identical
-- stored "PhoneNumber" value. Always a genuine duplicate; no format ambiguity.
-- ============================================================================
SELECT
    u."PhoneNumber"                                AS phone_number,
    COUNT(*)                                       AS duplicate_count,
    ARRAY_AGG(u.id ORDER BY u."CreatedAt")          AS user_ids,
    ARRAY_AGG(u."Type" ORDER BY u."CreatedAt")      AS user_types,
    ARRAY_AGG(u."Status" ORDER BY u."CreatedAt")    AS statuses,
    ARRAY_AGG(u."CreatedAt" ORDER BY u."CreatedAt") AS created_at
FROM user_management.users u
WHERE u."PhoneNumber" IS NOT NULL
  AND u."Status" <> 'Deleted'
GROUP BY u."PhoneNumber"
HAVING COUNT(*) > 1
ORDER BY duplicate_count DESC;

-- ============================================================================
-- Query 2: possible duplicates stored in DIFFERENT formats -- e.g. one row's
-- "PhoneNumber" as "09121234567", another as "+989121234567" for the same underlying
-- number. Uses the schema's own "NationalNumber" column (populated by the PhoneNumber
-- value object at write time) rather than re-deriving it, so this matches exactly what
-- the app itself considers "the same number" -- see PersonDirectoryReadService, which
-- matches on this same column for the identical reason.
-- ============================================================================
SELECT
    u."NationalNumber"                             AS national_number,
    COUNT(*)                                       AS duplicate_count,
    ARRAY_AGG(DISTINCT u."PhoneNumber")             AS distinct_stored_formats,
    ARRAY_AGG(u.id ORDER BY u."CreatedAt")          AS user_ids,
    ARRAY_AGG(u."CreatedAt" ORDER BY u."CreatedAt") AS created_at
FROM user_management.users u
WHERE u."NationalNumber" IS NOT NULL
  AND u."Status" <> 'Deleted'
GROUP BY u."NationalNumber"
HAVING COUNT(DISTINCT u."PhoneNumber") > 1
ORDER BY duplicate_count DESC;

-- ============================================================================
-- Query 3 (§1.3's other half): users with NO phone number at all on the account
-- itself, whose profile carries one (as phone_national_number -- there is no combined
-- phone_number column on user_profiles) that was never backfilled onto
-- users."PhoneNumber" (WS1 §9.4 made every NEW creation path canonicalize it onto the
-- account; this finds pre-existing rows from before that path existed, which the app's
-- own phone-lookup seams -- IPersonDirectory, IUserRepository.GetByPhoneNumberAsync --
-- cannot see today).
-- ============================================================================
SELECT
    u.id                          AS user_id,
    u.email,
    u."Type"                      AS user_type,
    u."Status"                    AS status,
    u."CreatedAt"                 AS created_at,
    p.phone_national_number       AS profile_national_number,
    p.phone_country_code          AS profile_country_code
FROM user_management.users u
JOIN user_management.user_profiles p ON p.user_id = u.id
WHERE u."PhoneNumber" IS NULL
  AND u."Status" <> 'Deleted'
  AND p.phone_national_number IS NOT NULL
ORDER BY u."CreatedAt";
