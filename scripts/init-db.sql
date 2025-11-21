-- QuietMatch Database Initialization Script
-- This script creates separate databases for each microservice
-- Run automatically by PostgreSQL container on first startup

-- Create databases for each microservice
CREATE DATABASE identity_db;
CREATE DATABASE profile_db;
CREATE DATABASE matching_db;
CREATE DATABASE scheduling_db;
CREATE DATABASE notification_db;
CREATE DATABASE verification_db;
CREATE DATABASE payment_db;
CREATE DATABASE analytics_db;

-- Enable pgvector extension for matching_db (for future embedding-based matching)
\c matching_db;
CREATE EXTENSION IF NOT EXISTS vector;

-- Grant permissions (all services use same admin user for local dev)
-- In production, each service should have its own database user with limited permissions
GRANT ALL PRIVILEGES ON DATABASE identity_db TO admin;
GRANT ALL PRIVILEGES ON DATABASE profile_db TO admin;
GRANT ALL PRIVILEGES ON DATABASE matching_db TO admin;
GRANT ALL PRIVILEGES ON DATABASE scheduling_db TO admin;
GRANT ALL PRIVILEGES ON DATABASE notification_db TO admin;
GRANT ALL PRIVILEGES ON DATABASE verification_db TO admin;
GRANT ALL PRIVILEGES ON DATABASE payment_db TO admin;
GRANT ALL PRIVILEGES ON DATABASE analytics_db TO admin;

-- Switch back to default database
\c postgres;

-- Log completion
SELECT 'QuietMatch databases created successfully!' AS status;
