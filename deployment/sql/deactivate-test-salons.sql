-- =====================================================================================================================
-- deactivate-test-salons.sql — WRITES. Takes the two test salons out of customer view. Deletes nothing.
--
-- Why: on production (2026-09-23) customer search listed two salons that are not businesses:
--   466e8bf3-c47d-417f-830d-9d0389f4eef6  «TEST notification check 6417131»  (Active)
--   9baeae5e-e9b5-4780-9816-16be959a02f0  «سالن تست خودکار»                    (Drafted)
-- See openspec/changes/customer-app-ux-review-fixes (task I.1). Both are set to Archived: the search production
-- runs today hides only Archived salons, and the domain cannot Deactivate a Drafted one. Their bookings, services,
-- photos and ledger rows stay exactly as they are.
--
-- Safe to re-run: a row changes only when its id AND its exact business name match AND it is not Archived yet, so a
-- second run reports "(0 rows)" and changes nothing. A salon renamed since this was decided is left alone.
-- Everything happens in one transaction.
--
-- Run (on the box, as booksy, from the directory holding this file):
--   docker exec -i booksy-postgres sh -c 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"' \
--     < deactivate-test-salons.sql
-- The output shows, in order: both salons as they are now; the rows archived, with the status each had (expect 2
-- rows the first time, 0 after); both salons afterwards. If fewer than 2 rows were archived on the first run, read
-- the "before" listing: a name that no longer matches exactly means someone renamed the salon — ask before hiding it.
--
-- Then flush the provider cache. Provider.Deactivate would have run ProviderCacheInvalidationEventHandler, which
-- removes "Provider:<id>" and every "Provider:owner:*" entry (RedisCacheService prefixes keys with Cache:KeyPrefix,
-- "booksy"). A raw UPDATE raises no event, so without this the salon page itself can keep serving the old status
-- from Redis until its sliding 15-minute expiry lapses (search and the map read the database and are right at once):
--   docker exec booksy-redis sh -c 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning DEL \
--     booksy:Provider:466e8bf3-c47d-417f-830d-9d0389f4eef6 booksy:Provider:9baeae5e-e9b5-4780-9816-16be959a02f0; \
--     redis-cli -a "$REDIS_PASSWORD" --no-auth-warning --scan --pattern "booksy:Provider:owner:*" \
--     | xargs -r redis-cli -a "$REDIS_PASSWORD" --no-auth-warning DEL'
-- If that deletes nothing (as on 2026-09-23), the API is using its in-memory cache (Cache:Provider=Redis needs
-- Cache__RedisConnectionString, which docker-compose.prod.yml does not set): restart booksy-api, or wait 15 minutes.
--
-- REVERSE (restores the status each salon had; only touches rows this script archived and nobody renamed since):
--   BEGIN;
--   UPDATE "ServiceCatalog"."Providers" AS p
--      SET "Status" = t.previous_status, "Version" = p."Version" + 1,
--          "LastModifiedAt" = NOW(), "LastModifiedBy" = 'deactivate-test-salons.sql (reverse)'
--     FROM (VALUES ('466e8bf3-c47d-417f-830d-9d0389f4eef6'::uuid, 'TEST notification check 6417131', 'Active'),
--                  ('9baeae5e-e9b5-4780-9816-16be959a02f0'::uuid, 'سالن تست خودکار',                 'Drafted'))
--          AS t(id, business_name, previous_status)
--    WHERE p."Id" = t.id AND p."BusinessName" = t.business_name AND p."Status" = 'Archived'
--   RETURNING p."Id", p."BusinessName", p."Status";
--   COMMIT;
-- then flush the cache again, as above.
-- =====================================================================================================================

SET client_encoding = 'UTF8';

BEGIN;

-- Before: what is there now.
SELECT p."Id" AS salon_id, p."BusinessName" AS salon, p."Status" AS status_before
FROM "ServiceCatalog"."Providers" p
WHERE p."Id" IN ('466e8bf3-c47d-417f-830d-9d0389f4eef6', '9baeae5e-e9b5-4780-9816-16be959a02f0')
ORDER BY p."Id";

-- Archive. "Status" is stored as the enum member name (ProviderConfiguration: HasConversion<string>()).
-- "Version" is the provider's concurrency token: bumping it makes any save still holding the old row fail loudly
-- instead of writing the old status back.
WITH targets (id, business_name) AS (
    VALUES ('466e8bf3-c47d-417f-830d-9d0389f4eef6'::uuid, 'TEST notification check 6417131'),
           ('9baeae5e-e9b5-4780-9816-16be959a02f0'::uuid, 'سالن تست خودکار')
)
UPDATE "ServiceCatalog"."Providers" AS p
   SET "Status"         = 'Archived',
       "Version"        = p."Version" + 1,
       "LastModifiedAt" = NOW(),
       "LastModifiedBy" = 'deactivate-test-salons.sql'
  FROM targets t, "ServiceCatalog"."Providers" AS old
 WHERE p."Id" = t.id
   AND p."BusinessName" = t.business_name
   AND p."Status" <> 'Archived'
   AND old."Id" = p."Id"
RETURNING p."Id" AS archived_salon_id, p."BusinessName" AS salon, old."Status" AS previous_status;

COMMIT;

-- After.
SELECT p."Id" AS salon_id, p."BusinessName" AS salon, p."Status" AS status_after
FROM "ServiceCatalog"."Providers" p
WHERE p."Id" IN ('466e8bf3-c47d-417f-830d-9d0389f4eef6', '9baeae5e-e9b5-4780-9816-16be959a02f0')
ORDER BY p."Id";
