-- ============================================================================
-- Backfill: existing Providers  →  OrganizationMemberships  (Phase 2, item 3)
-- ============================================================================
--
-- STATUS: STAGED FOR REVIEW — do NOT auto-apply. Run on a DB clone / staging
-- first, verify the counts, then run on production inside a transaction.
-- This script is NOT wired into the EF migration pipeline on purpose.
--
-- Idempotent: every statement is guarded by NOT EXISTS, so re-running it is safe
-- and creates no duplicates.
--
-- Requires PostgreSQL >= 13 for gen_random_uuid() (built-in). If older, replace
-- with uuid_generate_v4() and `CREATE EXTENSION IF NOT EXISTS "uuid-ossp";`.
--
-- Schema facts this relies on (verified against ProviderConfiguration.cs +
-- OrganizationMembershipConfiguration.cs + migration 20260721212656):
--   "ServiceCatalog"."Providers":  "Id" uuid, "OwnerId" uuid,
--       "HierarchyType" varchar (enum NAME: 'Organization' | 'Individual'),
--       "ParentProviderId" uuid null, "Status" varchar (enum NAME).
--   "ServiceCatalog".organization_memberships: id, person_id (null ok),
--       organization_id, status (enum NAME e.g. 'Active'), roles (CSV of enum
--       NAMES e.g. 'Owner' or 'Owner,StaffProvider'), joined_at, is_deleted,
--       created_at, "Version" — all NOT NULL except the nullable timestamps.
--   "ServiceCatalog".staff_profiles: membership_id (PK/FK), provides_services,
--       bio_override.
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- STEP 1 — Owner memberships (SAFE, unambiguous).
-- Every Organization provider gets an {Owner} membership for its OwnerId, if it
-- doesn't already have a live one. Going forward this is created at
-- registration-complete (SaveStep9CompleteCommandHandler); this backfills the
-- providers that onboarded before that fix.
-- ----------------------------------------------------------------------------
INSERT INTO "ServiceCatalog".organization_memberships
    (id, person_id, organization_id, status, roles, joined_at, is_deleted, created_at, "Version")
SELECT gen_random_uuid(), p."OwnerId", p."Id", 'Active', 'Owner', now(), false, now(), 1
FROM "ServiceCatalog"."Providers" p
WHERE p."HierarchyType" = 'Organization'
  AND p."OwnerId" IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM "ServiceCatalog".organization_memberships m
      WHERE m.organization_id = p."Id"
        AND m.person_id = p."OwnerId"
        AND m.status <> 'Terminated'
  );

-- ----------------------------------------------------------------------------
-- STEP 2 — Legacy sub-provider staff  →  StaffProvider memberships.
--
-- ⚠️ NEEDS YOUR SIGN-OFF. Legacy "staff" are Individual providers with a
-- ParentProviderId. Their OwnerId is EITHER a real UserManagement user (proper
-- invitation path) OR a synthetic UserId.CreateNew() with no users row (the
-- AddStaffToProvider path). Backfilling the synthetic ones would create
-- memberships whose person_id points at a phantom person (blank name/phone,
-- cannot authenticate).
--
-- DEFAULT = Option A (conservative): only backfill sub-providers whose OwnerId
-- resolves to a real, non-deleted user. Synthetic staff are intentionally
-- SKIPPED (report them with the query at the bottom, then decide per case).
--
-- Option B (backfill all, accept orphan person_ids): delete the
-- `EXISTS (... user_management.users ...)` clause below. NOT recommended.
-- ----------------------------------------------------------------------------
INSERT INTO "ServiceCatalog".organization_memberships
    (id, person_id, organization_id, status, roles, joined_at, is_deleted, created_at, "Version")
SELECT gen_random_uuid(), s."OwnerId", s."ParentProviderId", 'Active', 'StaffProvider', now(), false, now(), 1
FROM "ServiceCatalog"."Providers" s
WHERE s."HierarchyType" = 'Individual'
  AND s."ParentProviderId" IS NOT NULL
  -- Option A: real people only. Remove this clause for Option B.
  AND EXISTS (
      SELECT 1 FROM user_management.users u
      WHERE u.id = s."OwnerId" AND u."Status" <> 'Deleted'
  )
  AND NOT EXISTS (
      SELECT 1 FROM "ServiceCatalog".organization_memberships m
      WHERE m.organization_id = s."ParentProviderId"
        AND m.person_id = s."OwnerId"
        AND m.status <> 'Terminated'
  );

-- ----------------------------------------------------------------------------
-- STEP 3 — StaffProfiles for any StaffProvider membership missing one.
-- (Owners created in STEP 1 hold only {Owner} → no profile; STEP 2 rows do.)
-- ----------------------------------------------------------------------------
INSERT INTO "ServiceCatalog".staff_profiles (membership_id, provides_services)
SELECT m.id, true
FROM "ServiceCatalog".organization_memberships m
WHERE ('StaffProvider' = ANY (string_to_array(m.roles, ',')))
  AND m.status <> 'Terminated'
  AND NOT EXISTS (
      SELECT 1 FROM "ServiceCatalog".staff_profiles sp WHERE sp.membership_id = m.id
  );

-- Review the results before committing.
COMMIT;

-- ============================================================================
-- VERIFICATION (run separately after COMMIT)
-- ============================================================================
-- Organizations still missing an owner membership (should be 0):
--   SELECT count(*) FROM "ServiceCatalog"."Providers" p
--   WHERE p."HierarchyType" = 'Organization' AND p."OwnerId" IS NOT NULL
--     AND NOT EXISTS (SELECT 1 FROM "ServiceCatalog".organization_memberships m
--                     WHERE m.organization_id = p."Id" AND m.person_id = p."OwnerId"
--                       AND m.status <> 'Terminated');
--
-- SKIPPED synthetic sub-provider staff (person has no users row) — decide per case:
--   SELECT s."Id", s."ParentProviderId", s."OwnerId", s."OwnerFirstName", s."OwnerLastName"
--   FROM "ServiceCatalog"."Providers" s
--   WHERE s."HierarchyType" = 'Individual' AND s."ParentProviderId" IS NOT NULL
--     AND NOT EXISTS (SELECT 1 FROM user_management.users u WHERE u.id = s."OwnerId");
--
-- Membership counts by role/status:
--   SELECT status, roles, count(*) FROM "ServiceCatalog".organization_memberships
--   GROUP BY status, roles ORDER BY count(*) DESC;
-- ============================================================================
