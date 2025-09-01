-- scripts/init-db.sql
-- Database initialization script for Booksy platform

-- Create schemas for each bounded context
CREATE SCHEMA IF NOT EXISTS user_management;
CREATE SCHEMA IF NOT EXISTS service_provider;
CREATE SCHEMA IF NOT EXISTS service_catalog;
CREATE SCHEMA IF NOT EXISTS booking;
CREATE SCHEMA IF NOT EXISTS payment;
CREATE SCHEMA IF NOT EXISTS notification;
CREATE SCHEMA IF NOT EXISTS review_rating;
CREATE SCHEMA IF NOT EXISTS analytics;
CREATE SCHEMA IF NOT EXISTS shared;

-- Grant permissions to the application user
GRANT ALL PRIVILEGES ON SCHEMA user_management TO booksy_user;
GRANT ALL PRIVILEGES ON SCHEMA service_provider TO booksy_user;
GRANT ALL PRIVILEGES ON SCHEMA service_catalog TO booksy_user;
GRANT ALL PRIVILEGES ON SCHEMA booking TO booksy_user;
GRANT ALL PRIVILEGES ON SCHEMA payment TO booksy_user;
GRANT ALL PRIVILEGES ON SCHEMA notification TO booksy_user;
GRANT ALL PRIVILEGES ON SCHEMA review_rating TO booksy_user;
GRANT ALL PRIVILEGES ON SCHEMA analytics TO booksy_user;
GRANT ALL PRIVILEGES ON SCHEMA shared TO booksy_user;

-- Set default search path
ALTER USER booksy_user SET search_path TO user_management, service_provider, service_catalog, booking, payment, notification, review_rating, analytics, shared, public;

-- Create extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create extension for full-text search (for service/provider search)
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- Create shared enums
CREATE TYPE shared.currency AS ENUM ('USD', 'EUR', 'GBP', 'CAD', 'AUD');
CREATE TYPE shared.service_location_type AS ENUM ('InHouse', 'Mobile', 'Both');

-- Audit table for tracking all changes
CREATE TABLE shared.audit_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    user_id UUID,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT
);

-- Create indexes for audit log
CREATE INDEX idx_audit_log_entity ON shared.audit_log(entity_type, entity_id);
CREATE INDEX idx_audit_log_user ON shared.audit_log(user_id);
CREATE INDEX idx_audit_log_timestamp ON shared.audit_log(timestamp DESC);

-- Event store for event sourcing
CREATE TABLE shared.event_store (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(255) NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    event_data JSONB NOT NULL,
    metadata JSONB,
    version INT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255)
);

-- Create indexes for event store
CREATE INDEX idx_event_store_aggregate ON shared.event_store(aggregate_id);
CREATE INDEX idx_event_store_aggregate_type ON shared.event_store(aggregate_type);
CREATE INDEX idx_event_store_created_at ON shared.event_store(created_at DESC);

-- Outbox pattern for reliable event publishing
CREATE TABLE shared.outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    payload JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMPTZ,
    retry_count INT DEFAULT 0,
    error_message TEXT
);

-- Create index for unprocessed events
CREATE INDEX idx_outbox_unprocessed ON shared.outbox_events(processed_at) WHERE processed_at IS NULL;

-- Create performance monitoring function
CREATE OR REPLACE FUNCTION shared.log_slow_queries()
RETURNS event_trigger AS $$
BEGIN
    -- Log queries that take more than 1 second
    RAISE LOG 'Slow query detected';
END;
$$ LANGUAGE plpgsql;

-- Enable row-level security for multi-tenancy support (future-proofing)
ALTER TABLE shared.audit_log ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.event_store ENABLE ROW LEVEL SECURITY;

-- Create function for automatic updated_at timestamp
CREATE OR REPLACE FUNCTION shared.trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = CURRENT_TIMESTAMP;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Performance settings (connection pooling hints)
ALTER DATABASE booksy_db SET max_connections = 200;
ALTER DATABASE booksy_db SET shared_buffers = '256MB';
ALTER DATABASE booksy_db SET effective_cache_size = '1GB';
ALTER DATABASE booksy_db SET maintenance_work_mem = '64MB';
ALTER DATABASE booksy_db SET checkpoint_completion_target = 0.9;
ALTER DATABASE booksy_db SET wal_buffers = '16MB';
ALTER DATABASE booksy_db SET random_page_cost = 1.1;