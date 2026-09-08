-- ============================================================================
-- Re-key existing bookings, availability and service qualifications from
-- legacy sub-provider ids to MembershipIds.
-- ============================================================================
--
-- STATUS: STAGED FOR REVIEW — do NOT auto-apply. Run on a clone first, compare
-- the before/after counts in the VERIFICATION section, then run on production
-- inside the transaction below.
--
-- WHY
-- ---
-- A "staff member" used to be a whole second Provider (HierarchyType =
-- 'Individual') glued to the salon by ParentProviderId, and a booking against
-- that person stored that provider's id in Bookings."StaffId". Staff are now
-- OrganizationMemberships, and everything written since stores the MembershipId
-- there instead — CreateBookingCommandHandler resolves membership → legacy
-- sub-provider → org-direct, in that order, precisely so both shapes keep
-- working during the transition (see BookableResourceResolver).
--
-- That fallback is what keeps ParentProviderId alive. This script removes the
-- need for it by rewriting the historic rows, so the legacy branch (and then the
-- column) can go.
--
-- ORDER: run backfill_memberships.sql FIRST. It creates the membership rows this
-- script maps onto; without it every lookup here finds nothing and the script
-- correctly does nothing.
--
-- WHAT IS *NOT* RE-KEYED, ON PURPOSE
-- ----------------------------------
--  * Bookings whose StaffId is the organization's own ProviderId (org-direct /
--    solo bookings). Those are not staff references at all and stay as they are.
--  * Sub-providers whose OwnerId has no users row (the synthetic accounts the old
--    AddStaffToProvider path minted). backfill_memberships.sql STEP 2 skips them
--    by default, so they have no membership to point at. Convert them first with
--    STEP 2b below if you want their history preserved; otherwise their bookings
--    keep pointing at a provider row that must then be retained.
--
-- Schema facts verified against the EF configurations:
--   "ServiceCatalog"."Bookings"             : "StaffId" uuid, "IndividualProviderId" uuid null
--   "ServiceCatalog"."ProviderAvailability" : "StaffId" uuid null, "ProviderId" uuid
--   "ServiceCatalog"."Services"             : "QualifiedStaff" jsonb (array of uuid)
--   "ServiceCatalog".organization_memberships : id, person_id, organization_id, status
--   "ServiceCatalog"."Providers"            : "Id", "OwnerId", "ParentProviderId", "HierarchyType"
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- The mapping every step below shares: legacy sub-provider  ->  the membership
-- that now represents the same human at the same salon.
--
-- Matched on (person, organization): the sub-provider's OwnerId is the person,
-- its ParentProviderId is the salon. Terminated memberships are excluded so a
-- person who left and rejoined maps to their CURRENT membership.
-- ----------------------------------------------------------------------------
CREATE TEMP TABLE staff_id_map ON COMMIT DROP AS
-- (a) Sub-providers backed by a REAL person: matched on (person, organization).
SELECT DISTINCT
       sp."Id"               AS legacy_provider_id,
       m.id                  AS membership_id,
       sp."ParentProviderId" AS organization_id
FROM "ServiceCatalog"."Providers" sp
JOIN "ServiceCatalog".organization_memberships m
  ON m.organization_id = sp."ParentProviderId"
 AND m.person_id       = sp."OwnerId"
 AND m.status <> 'Terminated'
WHERE sp."HierarchyType" = 'Individual'
  AND sp."ParentProviderId" IS NOT NULL

UNION

-- (b) Synthetic sub-providers converted to UNCLAIMED memberships by
-- backfill_memberships.sql STEP 2b. Those memberships have person_id NULL by
-- definition, so there is no person to join on and the only link back to the
-- legacy row is the display name STEP 2b copied from it, within the same salon.
--
-- Fragile if one salon had two account-less staff with the identical name — which
-- is exactly what the duplicate check below is for: it aborts rather than guessing.
-- Resolve those by hand (rename one, or map it manually) and re-run.
SELECT DISTINCT
       sp."Id"               AS legacy_provider_id,
       m.id                  AS membership_id,
       sp."ParentProviderId" AS organization_id
FROM "ServiceCatalog"."Providers" sp
JOIN "ServiceCatalog".organization_memberships m
  ON m.organization_id = sp."ParentProviderId"
 AND m.person_id IS NULL
 AND m.status <> 'Terminated'
JOIN "ServiceCatalog".staff_profiles sp2
  ON sp2.membership_id = m.id
 AND sp2.display_name = NULLIF(TRIM(CONCAT(sp."OwnerFirstName", ' ', sp."OwnerLastName")), '')
WHERE sp."HierarchyType" = 'Individual'
  AND sp."ParentProviderId" IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM user_management.users u WHERE u.id = sp."OwnerId");

-- A sub-provider must not map to two live memberships; ux_membership_person_org_active
-- should already make that impossible, so fail loudly rather than pick one at random.
DO $$
DECLARE dupes int;
BEGIN
    SELECT count(*) INTO dupes FROM (
        SELECT legacy_provider_id FROM staff_id_map
        GROUP BY legacy_provider_id HAVING count(*) > 1
    ) d;
    IF dupes > 0 THEN
        RAISE EXCEPTION
          'ABORT: % legacy sub-provider(s) map to more than one live membership. Resolve before re-keying.', dupes;
    END IF;
END $$;

-- ----------------------------------------------------------------------------
-- STEP 1 — Bookings.
-- IndividualProviderId is kept as the historical record of which provider row the
-- booking was originally made against; only the live pointer moves.
-- ----------------------------------------------------------------------------
UPDATE "ServiceCatalog"."Bookings" b
SET "StaffId" = map.membership_id
FROM staff_id_map map
WHERE b."StaffId" = map.legacy_provider_id;

-- ----------------------------------------------------------------------------
-- STEP 2 — Availability slots.
-- Member slots are owned by the ORGANIZATION and carry StaffId = MembershipId
-- (MemberBookabilityService). Legacy rows hang off the sub-provider, so both
-- columns move.
-- ----------------------------------------------------------------------------
UPDATE "ServiceCatalog"."ProviderAvailability" a
SET "StaffId"    = map.membership_id,
    "ProviderId" = map.organization_id
FROM staff_id_map map
WHERE a."StaffId" = map.legacy_provider_id
   OR a."ProviderId" = map.legacy_provider_id;

-- ----------------------------------------------------------------------------
-- STEP 3 — Service qualifications (jsonb array of staff ids).
-- Rebuilds each array element-by-element, substituting mapped ids and leaving
-- anything unmapped (org-direct, unconverted synthetics) untouched.
-- ----------------------------------------------------------------------------
UPDATE "ServiceCatalog"."Services" s
SET "QualifiedStaff" = remapped.arr
FROM (
    SELECT sv."Id" AS service_id,
           COALESCE(
               jsonb_agg(DISTINCT COALESCE(map.membership_id, (elem)::uuid)),
               '[]'::jsonb
           ) AS arr
    FROM "ServiceCatalog"."Services" sv
    CROSS JOIN LATERAL jsonb_array_elements_text(
        CASE WHEN jsonb_typeof(sv."QualifiedStaff") = 'array'
             THEN sv."QualifiedStaff" ELSE '[]'::jsonb END) AS elem
    LEFT JOIN staff_id_map map ON map.legacy_provider_id = (elem)::uuid
    GROUP BY sv."Id"
) AS remapped
WHERE s."Id" = remapped.service_id
  AND s."QualifiedStaff" IS DISTINCT FROM remapped.arr;

COMMIT;

-- ============================================================================
-- VERIFICATION (run separately, after COMMIT)
-- ============================================================================
-- Bookings still pointing at a legacy sub-provider (expected: only those whose
-- sub-provider had no real person, i.e. was skipped by the backfill):
--   SELECT count(*) FROM "ServiceCatalog"."Bookings" b
--   JOIN "ServiceCatalog"."Providers" p ON p."Id" = b."StaffId"
--   WHERE p."HierarchyType" = 'Individual' AND p."ParentProviderId" IS NOT NULL;
--
-- Bookings whose StaffId now resolves to a membership (should have grown by the
-- number STEP 1 reported):
--   SELECT count(*) FROM "ServiceCatalog"."Bookings" b
--   JOIN "ServiceCatalog".organization_memberships m ON m.id = b."StaffId";
--
-- Orphans — StaffId matching neither a membership, nor a provider row. Expect 0:
--   SELECT b."Id", b."StaffId" FROM "ServiceCatalog"."Bookings" b
--   WHERE NOT EXISTS (SELECT 1 FROM "ServiceCatalog".organization_memberships m WHERE m.id = b."StaffId")
--     AND NOT EXISTS (SELECT 1 FROM "ServiceCatalog"."Providers" p WHERE p."Id" = b."StaffId");
--
-- Once the first query returns 0 across every environment, the legacy
-- sub-provider branch in BookableResourceResolver and CreateBookingCommandHandler
-- can be deleted and Providers."ParentProviderId" dropped.
-- ============================================================================
