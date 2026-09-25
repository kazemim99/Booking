#!/usr/bin/env bash
# =============================================================================
# seed-bookable-provider.sh — make a seeded provider fully BOOKABLE (dev/demo).
#
# Booking (CreateBookingCommandHandler) requires:
#   - a Provider with Status = Active
#   - an Active OrganizationMembership of it that provides services (the member
#     IS the bookable resource; there is no such thing as a staff Provider)
#   - an Active SERVICE listing that MEMBERSHIP id in its QualifiedStaff (jsonb)
#   - availability slots for that membership (ProviderAvailability.StaffId)
#
# The seeders ship none of this, so this script creates it directly in Postgres.
#
# This used to clone a Provider row into a "staff sub-provider"
# (ParentProviderId = the org, HierarchyType = 'Individual'). That model was
# retired: the RemoveProviderHierarchy migration DROPPED those columns, so the
# old script now fails outright against a migrated database — and, more to the
# point, it seeded a shape the booking engine no longer resolves.
#
# Prereqs: the Postgres dev container (asanrezerve-pg-dev) is running and migrated.
# Usage:   bash scripts/seed-bookable-provider.sh
# Re-runnable: each run creates a fresh member + service and prints their IDs.
# =============================================================================
set -euo pipefail

PG="${PG_CONTAINER:-asanrezerve-pg-dev}"
DB="${PG_DB:-asanrezerve}"
USER="${PG_USER:-asanrezerve_admin}"
SC='"ServiceCatalog"'

psql() { docker exec -i "$PG" psql -U "$USER" -d "$DB" "$@"; }

# 1) Pick an Active provider that already has availability slots to piggyback on
#    (its business hours are the ones the member's slots are cloned from).
ORG=$(psql -t -A -c "
  select p.\"Id\" from $SC.\"Providers\" p
  where p.\"Status\"='Active'
  order by (select count(*) from $SC.\"ProviderAvailability\" a where a.\"ProviderId\"=p.\"Id\") desc
  limit 1;")
# 2) Pick any existing service row to clone its full column set.
SRC_SVC=$(psql -t -A -c "select \"Id\" from $SC.\"Services\" limit 1;")

if [ -z "$ORG" ] || [ -z "$SRC_SVC" ]; then
  echo "ERROR: need at least one Active provider and one existing service row to clone." >&2
  exit 1
fi

MEMBERSHIP=$(psql -t -A -c "select gen_random_uuid();")
SVC=$(psql -t -A -c "select gen_random_uuid();")

psql -v ON_ERROR_STOP=1 <<SQL
BEGIN;

-- An UNCLAIMED membership: a real person the salon added who has no app account
-- yet (person_id NULL, name carried on the staff profile). That is the shape
-- AddStaffToProvider produces when it is given a phone it does not recognise,
-- and it needs no UserManagement row to exist.
INSERT INTO $SC.organization_memberships
  (id, person_id, organization_id, status, roles, invited_at, joined_at, created_at, is_deleted, "Version")
VALUES
  ('$MEMBERSHIP', NULL, '$ORG', 'Active', 'StaffProvider', now(), now(), now(), false, 1);

INSERT INTO $SC.staff_profiles
  (membership_id, provides_services, display_name, service_ids)
VALUES
  ('$MEMBERSHIP', true, 'Demo Staff', '[]'::jsonb);

-- Availability keyed to the MEMBER. Cloning the org's own slots is not enough:
-- lookups filter on StaffId now, so a member with no rows of their own has no
-- availability at all (and, before that filter existed, one member's booking
-- consumed every colleague's overlapping slot).
-- NB: the PK column is "AvailabilityId", not "Id" — the aggregate's Id property is
-- mapped to it (ProviderAvailabilityConfiguration), so the CLR name and the column
-- name differ here and only here.
INSERT INTO $SC."ProviderAvailability"
  ("AvailabilityId", "ProviderId", "StaffId", "Date", "StartTime", "EndTime", "Status",
   "CreatedAt", "CreatedBy", "IsDeleted", "Version")
SELECT gen_random_uuid(), a."ProviderId", '$MEMBERSHIP', a."Date", a."StartTime", a."EndTime",
       'Available', now(), 'seed-bookable-provider', false, 1
FROM $SC."ProviderAvailability" a
WHERE a."ProviderId" = '$ORG'
  AND a."StaffId" IS NULL
  AND a."Date" >= current_date;

-- Service: clone an existing row, attach to the org, make it Active and qualify
-- the MEMBERSHIP (QualifiedStaff holds membership ids).
CREATE TEMP TABLE _svc ON COMMIT DROP AS SELECT * FROM $SC."Services" WHERE "Id"='$SRC_SVC';
UPDATE _svc SET "Id"='$SVC', "ProviderId"='$ORG', "Status"='Active',
  "Name"='Demo Haircut', "QualifiedStaff"='["$MEMBERSHIP"]'::jsonb;
INSERT INTO $SC."Services" SELECT * FROM _svc;

COMMIT;
SQL

SLOTS=$(psql -t -A -c "select count(*) from $SC.\"ProviderAvailability\" where \"StaffId\"='$MEMBERSHIP';")

echo "=============================================================="
echo " Bookable provider ready:"
echo "   providerId (salon) : $ORG"
echo "   serviceId          : $SVC   (Demo Haircut, Active)"
echo "   staffId            : $MEMBERSHIP (Demo Staff — a MEMBERSHIP id)"
echo "   slots for member   : $SLOTS"
echo "=============================================================="
if [ "${SLOTS:-0}" = "0" ]; then
  echo " WARNING: no availability rows were cloned — the salon has no unassigned"
  echo " future slots to copy. The member will resolve but show no free times."
fi
echo " A customer can now POST /api/v1/Bookings with these ids (staffProviderId"
echo " takes the membership id), or book 'Demo Haircut' in the browser."
