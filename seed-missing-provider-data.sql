-- =====================================================
-- Complete Missing Provider Data for Booksy
-- Iranian Cities Geo-locations, Logos, and Gallery Images
-- =====================================================

-- Iranian Cities Real Coordinates
-- Tehran: 35.6892, 51.3890
-- Mashhad: 36.2605, 59.6168
-- Isfahan: 32.6546, 51.6680
-- Shiraz: 29.5918, 52.5836
-- Tabriz: 38.0962, 46.2738
-- Karaj: 35.8327, 50.9916
-- Qom: 34.6416, 50.8746
-- Ahvaz: 31.3203, 48.6693
-- Kerman: 30.2839, 57.0834
-- Rasht: 37.2808, 49.5832

-- =====================================================
-- Step 1: Update Provider Logos and Geo-locations
-- =====================================================

-- Tehran Providers (6 providers)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 35.6892 + (RANDOM() * 0.2 - 0.1), -- Add some variation within city
    "AddressLongitude" = 51.3890 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Tehran';

-- Mashhad Providers (2 providers)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 36.2605 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 59.6168 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Mashhad';

-- Isfahan Providers (2 providers)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 32.6546 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 51.6680 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Isfahan';

-- Shiraz Providers (2 providers)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 29.5918 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 52.5836 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Shiraz';

-- Tabriz Providers (2 providers)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 38.0962 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 46.2738 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Tabriz';

-- Karaj Providers (2 providers)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 35.8327 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 50.9916 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Karaj';

-- Qom Providers (1 provider)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 34.6416 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 50.8746 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Qom';

-- Ahvaz Providers (1 provider)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 31.3203 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 48.6693 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Ahvaz';

-- Kerman Providers (1 provider)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 30.2839 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 57.0834 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Kerman';

-- Rasht Providers (1 provider)
UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessLogoUrl" = 'https://ui-avatars.com/api/?name=' || SUBSTRING("BusinessName" FROM 1 FOR 2) || '&background=random&size=256',
    "AddressLatitude" = 37.2808 + (RANDOM() * 0.2 - 0.1),
    "AddressLongitude" = 49.5832 + (RANDOM() * 0.2 - 0.1),
    "AddressPostalCode" = LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0') || LPAD((FLOOR(RANDOM() * 90000) + 10000)::TEXT, 5, '0'),
    "BusinessWebsite" = 'https://' || REPLACE(LOWER("BusinessName"), ' ', '-') || '.ir',
    "LastModifiedAt" = NOW()
WHERE "AddressCity" = 'Rasht';

-- =====================================================
-- Step 2: Insert Gallery Images for Each Provider
-- =====================================================

-- Insert 3-5 gallery images per provider
INSERT INTO "ServiceCatalog"."provider_gallery_images"
(id, provider_id, image_url, thumbnail_url, medium_url, display_order, caption, alt_text, uploaded_at, is_active, is_primary, row_version)
SELECT
    gen_random_uuid() as id,
    p."Id" as provider_id,
    'https://picsum.photos/seed/' || p."Id" || '-' || g.img_num || '/1200/800' as image_url,
    'https://picsum.photos/seed/' || p."Id" || '-' || g.img_num || '/300/200' as thumbnail_url,
    'https://picsum.photos/seed/' || p."Id" || '-' || g.img_num || '/600/400' as medium_url,
    g.img_num as display_order,
    CASE g.img_num
        WHEN 1 THEN 'نمای اصلی سالن - Main salon view'
        WHEN 2 THEN 'فضای داخلی - Interior space'
        WHEN 3 THEN 'محیط کار - Work environment'
        WHEN 4 THEN 'تجهیزات - Equipment'
        WHEN 5 THEN 'نمونه کار - Sample work'
    END as caption,
    p."BusinessName" || ' - تصویر ' || g.img_num as alt_text,
    NOW() - (INTERVAL '1 day' * (30 - g.img_num * 5)) as uploaded_at,
    true as is_active,
    CASE WHEN g.img_num = 1 THEN true ELSE false END as is_primary,
    '\x00000000'::bytea as row_version
FROM
    "ServiceCatalog"."Providers" p
CROSS JOIN
    generate_series(1, 5) as g(img_num)
WHERE p."IsDeleted" = false;

-- =====================================================
-- Step 3: Update Profile Images
-- =====================================================

UPDATE "ServiceCatalog"."Providers"
SET
    "ProfileImageUrl" = 'https://picsum.photos/seed/' || "Id" || '-profile/400/400',
    "LastModifiedAt" = NOW()
WHERE "ProfileImageUrl" = 'profileImageUrl' OR "ProfileImageUrl" IS NULL;

-- =====================================================
-- Step 4: Add Social Media Links
-- =====================================================

UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessSocialMedia" = jsonb_build_object(
        'instagram', 'https://instagram.com/' || REPLACE(LOWER("BusinessName"), ' ', '_'),
        'telegram', 'https://t.me/' || REPLACE(LOWER("BusinessName"), ' ', '_'),
        'whatsapp', '+98' || LPAD((FLOOR(RANDOM() * 900000000) + 100000000)::TEXT, 9, '0')
    ),
    "LastModifiedAt" = NOW()
WHERE "BusinessSocialMedia" IS NULL;

-- =====================================================
-- Step 5: Add Business Tags
-- =====================================================

UPDATE "ServiceCatalog"."Providers"
SET
    "BusinessTags" = CASE "ProviderType"
        WHEN 'Salon' THEN '["زیبایی", "آرایشگاه", "آرایش", "میکاپ", "مو", "ناخن"]'::jsonb
        WHEN 'Spa' THEN '["اسپا", "ماساژ", "آرامش", "تراپی", "سلامت"]'::jsonb
        WHEN 'Clinic' THEN '["کلینیک", "زیبایی", "پوست", "مو", "لیزر"]'::jsonb
        WHEN 'GymFitness' THEN '["بدنسازی", "ورزش", "تناسب اندام", "فیتنس"]'::jsonb
        WHEN 'Individual' THEN '["آرایشگاه", "مردانه", "اصلاح", "کوتاهی"]'::jsonb
        WHEN 'Professional' THEN '["حرفه‌ای", "تخصصی", "درمانی", "سلامت"]'::jsonb
        ELSE '["زیبایی", "سلامت", "مراقبت"]'::jsonb
    END,
    "LastModifiedAt" = NOW()
WHERE "BusinessTags" IS NULL OR jsonb_array_length("BusinessTags") = 0;

-- =====================================================
-- Verification Queries
-- =====================================================

-- Count providers with logos
SELECT COUNT(*) as providers_with_logos
FROM "ServiceCatalog"."Providers"
WHERE "BusinessLogoUrl" IS NOT NULL AND LENGTH("BusinessLogoUrl") > 0;

-- Count gallery images
SELECT COUNT(*) as total_gallery_images
FROM "ServiceCatalog"."provider_gallery_images";

-- Gallery images per provider
SELECT p."BusinessName", COUNT(g.id) as gallery_count
FROM "ServiceCatalog"."Providers" p
LEFT JOIN "ServiceCatalog"."provider_gallery_images" g ON p."Id" = g.provider_id
GROUP BY p."Id", p."BusinessName"
ORDER BY p."BusinessName";

-- Show sample provider with all data
SELECT
    "Id",
    "BusinessName",
    "BusinessLogoUrl",
    "ProfileImageUrl",
    "BusinessWebsite",
    "AddressCity",
    "AddressLatitude",
    "AddressLongitude",
    "AddressPostalCode",
    "BusinessSocialMedia",
    "BusinessTags"
FROM "ServiceCatalog"."Providers"
LIMIT 3;

-- Show gallery images sample
SELECT
    p."BusinessName",
    g.image_url,
    g.caption,
    g.display_order,
    g.is_primary
FROM "ServiceCatalog"."provider_gallery_images" g
JOIN "ServiceCatalog"."Providers" p ON g.provider_id = p."Id"
ORDER BY p."BusinessName", g.display_order
LIMIT 10;
