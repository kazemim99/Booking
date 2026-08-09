-- ============================================================================
-- S8 — Global phone uniqueness at the DB level  (Phase 2, item 3)
-- ============================================================================
--
-- STATUS: STAGED FOR REVIEW — do NOT auto-apply. This is the DB half of S8
-- ("duplicate phone impossible"). The application already guards new writes
-- (ExistsByPhoneNumberAsync + canonical normalization); this closes the
-- TOCTOU race by making the database the final arbiter.
--
-- ⚠️ ORDER MATTERS: the unique index CREATION WILL FAIL if duplicates already
-- exist. Run STEP 1 (report), resolve any duplicates, THEN run STEP 2.
--
-- Column facts (UserConfiguration.cs / InitialCreate migration):
--   user_management.users."PhoneNumber" varchar(20) (canonical E.164 for new
--   rows; legacy rows MAY be non-canonical), "Status" varchar (enum NAME;
--   'Deleted' = soft-deleted, hidden by the app's global query filter).
-- ============================================================================

-- ----------------------------------------------------------------------------
-- STEP 1 — Dedupe report. Resolve every row before creating the index.
-- NOTE: this groups by the STORED value. If legacy phones were saved in mixed
-- formats (+98xxxx vs 09xxxx), true duplicates in different formats will NOT be
-- grouped here — canonicalize stored phones first if that is a concern
-- (PhoneNumber.From produces the canonical form the app now writes).
-- ----------------------------------------------------------------------------
SELECT "PhoneNumber",
       count(*)        AS duplicate_count,
       array_agg(id)   AS user_ids,
       array_agg("Type")   AS types,
       array_agg("Status") AS statuses
FROM user_management.users
WHERE "PhoneNumber" IS NOT NULL
  AND "Status" <> 'Deleted'
GROUP BY "PhoneNumber"
HAVING count(*) > 1
ORDER BY duplicate_count DESC;

-- Common cause (from the Phase-1 audit): the same human registered once as a
-- Customer and once as a Provider on the same phone (two rows, synthesized
-- emails). Decide the merge policy per pair (keep one, repoint memberships /
-- bookings, soft-delete the other) before STEP 2.

-- ----------------------------------------------------------------------------
-- STEP 2 — Partial unique index (run ONLY after STEP 1 is clean).
-- CONCURRENTLY avoids locking the table; it CANNOT run inside a transaction
-- block, so run this statement on its own. Excludes NULL phones and
-- soft-deleted rows (a deleted person's phone can be reclaimed — the app
-- reactivates the same person on return; see the architecture doc §7).
-- ----------------------------------------------------------------------------
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS ux_users_phone_active
    ON user_management.users ("PhoneNumber")
    WHERE "PhoneNumber" IS NOT NULL AND "Status" <> 'Deleted';

-- Verification: the index exists and is valid.
--   SELECT indexname, indexdef FROM pg_indexes
--   WHERE schemaname = 'user_management' AND indexname = 'ux_users_phone_active';
-- ============================================================================
