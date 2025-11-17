-- =====================================================
-- Assign All Active Staff to All Services (JSONB version)
-- =====================================================

-- For each service, set QualifiedStaff JSONB array to include all active staff from the same provider
UPDATE "ServiceCatalog"."Services" s
SET "QualifiedStaff" = (
    SELECT COALESCE(jsonb_agg(st."Id"), '[]'::jsonb)
    FROM "ServiceCatalog"."Staff" st
    WHERE st."ProviderId" = s."ProviderId"
      AND st."IsActive" = true
      AND st."IsDeleted" = false
)
WHERE s."IsDeleted" = false;

-- Verify the update
SELECT
    s."Id" as "ServiceId",
    s."Name" as "ServiceName",
    s."ProviderId",
    jsonb_array_length(s."QualifiedStaff") as "QualifiedStaffCount",
    s."QualifiedStaff"
FROM "ServiceCatalog"."Services" s
WHERE s."IsDeleted" = false
  AND s."ProviderId" = 'f4d1a77c-ad45-4c7e-9aa1-7eea059568f9'
ORDER BY s."Name";
