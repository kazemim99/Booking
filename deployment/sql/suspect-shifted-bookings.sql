-- =====================================================================================================================
-- suspect-shifted-bookings.sql — READ-ONLY. Lists upcoming bookings that may be stored 3h30 early.
--
-- Why: until commit 4363d268 (2026-09-22) the customer app sent the chosen slot as UTC. Booking times are the
-- salon's wall-clock with no zone (FOLLOW-UPS #63), so in Tehran a "14:00" slot was stored — and conflict-checked —
-- as 10:30. See openspec/changes/_inline/qa-walkthrough-2026-09-22 (task 3.0).
--
-- Why a list and not a fix: a booking does not record which app created it. The Vue web app and the salon's own
-- app always sent the right time, so an automatic "+3h30" would move correct bookings too. This lists every
-- CANDIDATE — booked online by a customer, before the fix, still upcoming — with the time they most likely chose,
-- so the salon can call the customer and confirm. Nothing here writes.
--
-- Run (on the box, as asan-rezerve):
--   docker exec -i asan-rezerve-postgres psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
--     -v fix_deployed_at="'2026-09-23 12:00:00+03:30'" < suspect-shifted-bookings.sql
-- Set fix_deployed_at to when the customer-app build with the fix went live.
-- =====================================================================================================================

SELECT
    b."BookingId"                                              AS booking_id,
    p."BusinessName"                                           AS salon,
    COALESCE(NULLIF(TRIM(CONCAT(up.first_name, ' ', up.last_name)), ''), '(no name)') AS customer,
    u."PhoneNumber"                                            AS customer_phone,
    b."Status"                                                 AS status,
    b."StartTime"                                              AS stored_start,
    -- The slot the customer most likely picked, if this booking came from the customer app.
    b."StartTime" + INTERVAL '3 hours 30 minutes'              AS likely_intended_start,
    b."RequestedAt"                                            AS booked_at
FROM "ServiceCatalog"."Bookings"  b
JOIN "ServiceCatalog"."Providers" p  ON p."Id" = b."ProviderId"
LEFT JOIN user_management.users          u  ON u.id = b."CustomerId"
LEFT JOIN user_management.user_profiles  up ON up.user_id = u.id
WHERE b."ProviderCustomerId" IS NULL            -- not a salon-entered walk-in (those were always right)
  AND b."CustomerId" <> p."OwnerId"             -- not the salon booking itself
  AND b."RequestedAt" < :fix_deployed_at::timestamptz
  AND b."StartTime" > NOW() - INTERVAL '1 day'  -- only what can still be acted on
  AND b."Status" IN ('Requested', 'Confirmed')
ORDER BY p."BusinessName", b."StartTime";
