-- Create databases if they don't exist
SELECT 'CREATE DATABASE asan_rezerve_user_management'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'asan_rezerve_user_management')\gexec

SELECT 'CREATE DATABASE asan_rezerve_service_catalog_dev'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'asan_rezerve_service_catalog_dev')\gexec

SELECT 'CREATE DATABASE asan_rezerve_service_catalog_prod'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'asan_rezerve_service_catalog_prod')\gexec