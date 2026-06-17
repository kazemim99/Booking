#!/usr/bin/env bash
# =============================================================================
# seed-bookable-provider.sh — make a seeded provider fully BOOKABLE (dev/demo).
#
# Booking (CreateBookingCommandHandler) requires a provider that has:
#   - Status = Active
#   - an Active STAFF sub-provider (a Provider row, ParentProviderId = the org)
#   - an Active SERVICE that lists the staff in its QualifiedStaff (jsonb)
# The seed data ships none of this (staff-onboarding handlers are commented out),
# so this script creates the missing pieces directly in Postgres by CLONING an
# existing Active org provider (gets all NOT NULL columns for free) + a service.
#
# Prereqs: the Postgres dev container (booksy-pg-dev) is running and migrated.
# Usage:   bash scripts/seed-bookable-provider.sh
# Re-runnable: each run creates a fresh staff + service and prints their IDs.
# =============================================================================
set -euo pipefail

PG="${PG_CONTAINER:-booksy-pg-dev}"
DB="${PG_DB:-booksy}"
USER="${PG_USER:-booksy_admin}"
SC='"ServiceCatalog"'

psql() { docker exec -i "$PG" psql -U "$USER" -d "$DB" "$@"; }

# 1) Pick an Active organization provider that already has availability slots.
ORG=$(psql -t -A -c "
  select p.\"Id\" from $SC.\"Providers\" p
  where p.\"Status\"='Active' and p.\"ParentProviderId\" is null
  order by (select count(*) from $SC.\"ProviderAvailability\" a where a.\"ProviderId\"=p.\"Id\") desc
  limit 1;")
# 2) Pick any existing service row to clone its full column set.
SRC_SVC=$(psql -t -A -c "select \"Id\" from $SC.\"Services\" limit 1;")

if [ -z "$ORG" ] || [ -z "$SRC_SVC" ]; then
  echo "ERROR: need at least one Active org provider and one existing service row to clone." >&2
  exit 1
fi

STAFF=$(psql -t -A -c "select gen_random_uuid();")
STAFF_OWNER=$(psql -t -A -c "select gen_random_uuid();")
SVC=$(psql -t -A -c "select gen_random_uuid();")

psql -v ON_ERROR_STOP=1 <<SQL
BEGIN;
-- Staff sub-provider: clone the org row, turn it into an Active Individual under the org.
CREATE TEMP TABLE _stf ON COMMIT DROP AS SELECT * FROM $SC."Providers" WHERE "Id"='$ORG';
UPDATE _stf SET "Id"='$STAFF', "OwnerId"='$STAFF_OWNER', "ParentProviderId"='$ORG',
  "Status"='Active', "HierarchyType"='Individual', "IsIndependent"=false,
  "OwnerFirstName"='Demo', "OwnerLastName"='Staff';
INSERT INTO $SC."Providers" SELECT * FROM _stf;

-- Service: clone an existing row, attach to the org, make it Active and qualify the staff.
CREATE TEMP TABLE _svc ON COMMIT DROP AS SELECT * FROM $SC."Services" WHERE "Id"='$SRC_SVC';
UPDATE _svc SET "Id"='$SVC', "ProviderId"='$ORG', "Status"='Active',
  "Name"='Demo Haircut', "QualifiedStaff"='["$STAFF"]'::jsonb;
INSERT INTO $SC."Services" SELECT * FROM _svc;
COMMIT;
SQL

echo "=============================================================="
echo " Bookable provider ready:"
echo "   providerId (org) : $ORG"
echo "   serviceId        : $SVC   (Demo Haircut, Active)"
echo "   staffProviderId  : $STAFF (Demo Staff, Active)"
echo "=============================================================="
echo " A customer can now POST /api/v1/Bookings with these ids, or"
echo " book this provider's 'Demo Haircut' service in the browser."
