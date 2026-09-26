## ADDED Requirements

### Requirement: The cache holds read models, never aggregates
The platform SHALL cache query results (read models) and SHALL NOT place domain aggregates in any cache.

#### Scenario: Provider reads go to the source
- **WHEN** a command loads a provider aggregate
- **THEN** it is read from the database, not from a cache

### Requirement: Cached reads are fresh after a change
Cached entries SHALL use absolute expiration and be tagged; every saved change to what a cached read returns SHALL
evict its tags after the save and again after the unit of work commits.

#### Scenario: Salon edits show at once
- **WHEN** a salon page has been read (and cached) and the owner updates the salon's profile
- **THEN** the next read of the salon page returns the updated data

#### Scenario: A new service shows on the salon page
- **WHEN** a salon page with services has been cached and the salon adds a service
- **THEN** the next read lists the new service

#### Scenario: A change committed without domain events
- **WHEN** a salon's row is changed and saved through the DbContext directly
- **THEN** the next salon page read returns the change

#### Scenario: Availability is never served from cache
- **WHEN** a slot is booked
- **THEN** the availability calendar no longer offers it on the next read

#### Scenario: Personal data is not cached without invalidation
- **WHEN** a customer adds a favourite or edits their profile
- **THEN** the next read reflects the change

### Requirement: Cache keys cover every query parameter
A cached query's key SHALL include every parameter that affects its result.

#### Scenario: Two searches differing in one filter
- **WHEN** two queries differ only in one filter
- **THEN** they are cached under different keys

### Requirement: The cache protects the database and survives Redis failure
Concurrent misses for one key SHALL run the underlying query once; a Redis failure SHALL degrade to the in-process
cache and the database without a per-request timeout, and the cache SHALL use the configured Redis
(`ConnectionStrings:Redis`) in every environment.

#### Scenario: Stampede
- **WHEN** 50 concurrent requests miss the same key
- **THEN** the query handler runs once

#### Scenario: Redis down
- **WHEN** Redis stops responding
- **THEN** after the failure threshold, cache calls stop touching Redis until the cooldown elapses and requests are
  served from L1 or the database

### Requirement: Admins observe and purge the cache
Administrators SHALL see hit/miss counts and hit ratio per query type, L1 statistics and the L2 state, and SHALL be
able to purge one tag or everything.

#### Scenario: Purge a salon
- **WHEN** an admin purges tag `provider:{id}`
- **THEN** the next salon page read is a miss served from the database
