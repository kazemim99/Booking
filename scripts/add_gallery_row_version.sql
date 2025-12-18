-- Add row_version column to provider_gallery_images table
-- This column is required by the ProviderConfiguration.cs EF Core mapping

ALTER TABLE "ServiceCatalog"."provider_gallery_images"
ADD COLUMN IF NOT EXISTS "row_version" bytea;

-- Make it a concurrency token that's automatically updated
-- Note: PostgreSQL doesn't have built-in row versioning like SQL Server
-- We'll use xmin system column or generate a trigger for updates
-- For now, just add the column - EF Core will handle the updates

SELECT 'Column added successfully' as result;
