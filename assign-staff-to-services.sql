-- =====================================================
-- Assign All Active Staff to All Services
-- =====================================================
-- This script ensures all active staff members are qualified for all services in their provider
-- Run this if you're seeing "No slots available" due to missing staff qualifications

-- For each service, set QualifiedStaff to include all active staff from the same provider
UPDATE "ServiceCatalog"."Services" s
SET "QualifiedStaff" = (
    SELECT array_agg(st."Id")
    FROM "ServiceCatalog"."Staff" st
    WHERE st."ProviderId" = s."ProviderId"
      AND st."IsActive" = true
      AND st."IsDeleted" = false
);

-- Verify the update
SELECT
    s."Id" as "ServiceId",
    s."Name" as "ServiceName",
    s."ProviderId",
    array_length(s."QualifiedStaff", 1) as "QualifiedStaffCount",
    s."QualifiedStaff"
FROM "ServiceCatalog"."Services" s
WHERE s."IsDeleted" = false
ORDER BY s."ProviderId", s."Name";
