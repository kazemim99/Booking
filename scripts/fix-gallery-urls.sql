-- Fix malformed gallery image URLs in the database
-- This script removes the absolute URL prefix and keeps only the relative path

-- Preview the changes first (run this to see what will be changed)
SELECT
    Id,
    ThumbnailUrl AS OldThumbnailUrl,
    REPLACE(REPLACE(REPLACE(ThumbnailUrl, 'http://localhost:5010/https://localhost:7002/', ''), 'https://localhost:7002/', ''), 'http://localhost:5010/', '') AS NewThumbnailUrl,
    MediumUrl AS OldMediumUrl,
    REPLACE(REPLACE(REPLACE(MediumUrl, 'http://localhost:5010/https://localhost:7002/', ''), 'https://localhost:7002/', ''), 'http://localhost:5010/', '') AS NewMediumUrl,
    OriginalUrl AS OldOriginalUrl,
    REPLACE(REPLACE(REPLACE(OriginalUrl, 'http://localhost:5010/https://localhost:7002/', ''), 'https://localhost:7002/', ''), 'http://localhost:5010/', '') AS NewOriginalUrl
FROM GalleryImages
WHERE ThumbnailUrl LIKE '%http%' OR MediumUrl LIKE '%http%' OR OriginalUrl LIKE '%http%';

-- Apply the fix (uncomment and run this after verifying the preview above)
UPDATE GalleryImages
SET
    ThumbnailUrl = REPLACE(REPLACE(REPLACE(ThumbnailUrl, 'http://localhost:5010/https://localhost:7002/', ''), 'https://localhost:7002/', ''), 'http://localhost:5010/', ''),
    MediumUrl = REPLACE(REPLACE(REPLACE(MediumUrl, 'http://localhost:5010/https://localhost:7002/', ''), 'https://localhost:7002/', ''), 'http://localhost:5010/', ''),
    OriginalUrl = REPLACE(REPLACE(REPLACE(OriginalUrl, 'http://localhost:5010/https://localhost:7002/', ''), 'https://localhost:7002/', ''), 'http://localhost:5010/', '')
WHERE ThumbnailUrl LIKE '%http%' OR MediumUrl LIKE '%http%' OR OriginalUrl LIKE '%http%';

-- Verify the fix worked
SELECT Id, ThumbnailUrl, MediumUrl, OriginalUrl
FROM GalleryImages;
