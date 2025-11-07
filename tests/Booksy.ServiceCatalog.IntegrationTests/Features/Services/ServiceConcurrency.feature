Feature: Service Concurrency and Data Integrity
  As a system
  I want to handle concurrent operations safely
  So that data integrity is maintained under concurrent access

  Background:
    Given a provider "Concurrent Salon" exists with active status
    And I am authenticated as the provider
    And the provider has a service "Test Service" with:
      | Field     | Value          |
      | Price     | 50.00          |
      | Duration  | 60             |
      | Status    | Active         |
      | Version   | 1              |

  # ==================== OPTIMISTIC LOCKING ====================

  @concurrency @optimistic-locking @basic
  Scenario: Successful update with correct version
    When I update the service with version 1:
      | Field     | Value  |
      | Price     | 60.00  |
    Then the response status code should be 200
    And the service version should be 2

  @concurrency @optimistic-locking @stale-version
  Scenario: Update with stale version fails
    Given the service has been updated to version 2
    When I attempt to update the service with version 1:
      | Field     | Value  |
      | Price     | 70.00  |
    Then the response status code should be 409
    And the error message should contain "version"
    And the service should remain at version 2

  @concurrency @optimistic-locking @concurrent-updates
  Scenario: Two concurrent updates with same version
    Given two users are authenticated as the provider
    When both users simultaneously update the service at version 1
    Then one update should succeed with status 200
    And one update should fail with status 409
    And the service version should be 2

  @concurrency @optimistic-locking @version-increment
  Scenario: Version increments on each successful update
    When I perform 5 sequential updates to the service
    Then all updates should succeed
    And the service version should be 6

  @concurrency @optimistic-locking @missing-version
  Scenario: Update without version header uses latest
    When I update the service without providing version:
      | Field     | Value  |
      | Price     | 55.00  |
    Then the response status code should be 200
    And the update should use the latest version

  # ==================== RACE CONDITIONS ====================

  @concurrency @race-condition @create-duplicate
  Scenario: Prevent duplicate service creation
    Given no service with name "New Unique Service" exists
    When two requests simultaneously create "New Unique Service"
    Then one creation should succeed with status 201
    And one creation should fail with status 409
    And only one service should exist in database

  @concurrency @race-condition @delete-then-update
  Scenario: Update after concurrent delete fails
    Given user A prepares to delete the service
    And user B prepares to update the service
    When both operations execute simultaneously
    Then the delete should succeed
    And the update should fail with status 404

  @concurrency @race-condition @activate-deactivate
  Scenario: Concurrent activate and deactivate operations
    When activate and deactivate requests execute simultaneously
    Then one operation should succeed
    And one operation should fail with status 409
    And the service should have a consistent state

  @concurrency @race-condition @multiple-status-changes
  Scenario: Multiple concurrent status change operations
    Given 5 users attempt to change service status concurrently
    When all status change requests execute simultaneously
    Then only one should succeed
    And the service should be in a valid state

  # ==================== CONCURRENT READS ====================

  @concurrency @reads @multiple-readers
  Scenario: Multiple concurrent read operations
    When 100 users simultaneously read the service details
    Then all requests should succeed with status 200
    And all should receive identical data

  @concurrency @reads @read-during-write
  Scenario: Read during concurrent write
    Given a write operation is updating the service
    When a read operation executes during the write
    Then the read should succeed with status 200
    And the read should return consistent data

  @concurrency @reads @dirty-read-prevention
  Scenario: Prevent dirty reads
    Given a transaction is updating the service but not committed
    When another user reads the service
    Then the read should not see uncommitted changes

  # ==================== DEADLOCK PREVENTION ====================

  @concurrency @deadlock @circular-dependency
  Scenario: Prevent deadlock on circular updates
    Given service A and service B exist
    And transaction 1 locks service A then attempts service B
    And transaction 2 locks service B then attempts service A
    When both transactions execute
    Then one should succeed
    And one should fail or retry
    And no deadlock should occur

  @concurrency @deadlock @timeout
  Scenario: Lock timeout on prolonged wait
    Given service is locked by a long-running transaction
    When another update attempts to acquire lock
    Then the request should timeout after 30 seconds
    And return status 408 or 409

  # ==================== TRANSACTION ISOLATION ====================

  @concurrency @isolation @read-committed
  Scenario: Read committed isolation level
    Given a transaction updates the service price to 70.00
    And the transaction is not yet committed
    When another user reads the service
    Then they should see the original price 50.00
    When the transaction commits
    And the user reads again
    Then they should see the updated price 70.00

  @concurrency @isolation @repeatable-read
  Scenario: Repeatable read within transaction
    Given a transaction reads the service price as 50.00
    And another transaction updates the price to 70.00 and commits
    When the first transaction reads the price again
    Then it should still see 50.00 within the same transaction

  @concurrency @isolation @phantom-read-prevention
  Scenario: Prevent phantom reads in service lists
    Given a transaction reads all services (count: 5)
    And another transaction adds a new service and commits
    When the first transaction reads all services again
    Then it should see the same 5 services within transaction

  # ==================== CONCURRENT BOOKING SCENARIOS ====================

  @concurrency @bookings @simultaneous-bookings
  Scenario: Handle simultaneous bookings for same service
    Given the service has MaxConcurrentBookings set to 3
    And there are currently 2 active bookings
    When 5 customers simultaneously book the service for the same time
    Then only 1 booking should succeed
    And 4 bookings should fail with "no availability"
    And the total bookings should not exceed 3

  @concurrency @bookings @update-during-booking
  Scenario: Update service while booking is in progress
    Given a customer is creating a booking for the service
    When the provider updates the service price during booking
    Then the booking should use the price at booking start time
    And the price update should succeed

  @concurrency @bookings @delete-with-active-bookings
  Scenario: Prevent service deletion with active bookings
    Given 5 concurrent bookings are being created
    When provider attempts to delete the service
    Then delete should fail with status 400
    And the service should remain active

  # ==================== IDEMPOTENCY ====================

  @concurrency @idempotency @duplicate-create
  Scenario: Idempotent service creation with idempotency key
    Given I provide idempotency key "unique-key-123"
    When I send the same create request twice
    Then the first request should return 201
    And the second request should return 200
    And only one service should be created
    And both responses should contain the same service ID

  @concurrency @idempotency @duplicate-update
  Scenario: Idempotent update with idempotency key
    Given I provide idempotency key "update-key-456"
    When I send the same update request twice
    Then both requests should succeed
    And the service should be updated only once

  @concurrency @idempotency @expired-key
  Scenario: Idempotency key expires after 24 hours
    Given I used idempotency key "old-key" 25 hours ago
    When I use the same idempotency key for a new request
    Then the request should be treated as new
    And should succeed independently

  # ==================== DATABASE CONSTRAINTS ====================

  @concurrency @constraints @unique-name-per-provider
  Scenario: Enforce unique service name per provider
    When two requests simultaneously create services with name "Haircut"
    Then one should succeed with status 201
    And one should fail with status 409
    And only one "Haircut" service should exist for provider

  @concurrency @constraints @foreign-key-integrity
  Scenario: Maintain foreign key integrity under concurrency
    Given service has associated bookings and options
    When concurrent operations delete bookings and update service
    Then all foreign key relationships should remain valid
    And no orphaned records should exist

  @concurrency @constraints @cascade-delete
  Scenario: Cascade delete with concurrent access
    Given service has options and price tiers
    When service is deleted while options are being updated
    Then the delete should wait for option update
    And all related entities should be deleted together

  # ==================== LOAD TESTING SCENARIOS ====================

  @concurrency @load @high-read-volume
  Scenario: Handle high volume of concurrent reads
    When 1000 concurrent users read service details
    Then at least 95% of requests should succeed
    And average response time should be under 200ms
    And no database locks should occur

  @concurrency @load @high-write-volume
  Scenario: Handle high volume of concurrent writes
    Given 100 different services exist
    When 500 concurrent update requests are sent
    Then all requests should complete within 10 seconds
    And all updates should be processed correctly
    And data integrity should be maintained

  @concurrency @load @mixed-operations
  Scenario: Handle mixed read/write operations
    When 1000 concurrent operations occur (70% reads, 30% writes)
    Then all operations should complete successfully
    And read operations should not be blocked by writes
    And write operations should maintain consistency

  # ==================== DISTRIBUTED TRANSACTIONS ====================

  @concurrency @distributed @cross-service-update
  Scenario: Update service and update provider simultaneously
    Given service and provider updates are in separate transactions
    When both updates execute concurrently
    Then both should succeed or both should fail together
    And no partial updates should occur

  @concurrency @distributed @saga-pattern
  Scenario: Service creation with multiple steps
    Given service creation involves multiple steps:
      | Step                    | Service        |
      | Create service record   | Database       |
      | Upload service image    | Storage        |
      | Index for search        | Search Service |
      | Publish event           | Message Queue  |
    When any step fails
    Then all previous steps should be rolled back
    And the system should be in consistent state

  # ==================== RETRY LOGIC ====================

  @concurrency @retry @transient-failure
  Scenario: Retry on transient database failure
    Given database experiences temporary connection issue
    When I attempt to update the service
    Then the system should retry up to 3 times
    And the update should eventually succeed

  @concurrency @retry @deadlock-retry
  Scenario: Automatic retry on deadlock detection
    Given a deadlock occurs during update
    When the system detects the deadlock
    Then it should automatically retry the operation
    And the operation should succeed on retry

  @concurrency @retry @exponential-backoff
  Scenario: Exponential backoff on concurrent conflicts
    Given multiple concurrent updates cause conflicts
    When updates are retried
    Then retry delays should increase exponentially
    And all updates should eventually succeed

  # ==================== CACHE COHERENCY ====================

  @concurrency @cache @invalidation
  Scenario: Cache invalidation on service update
    Given service details are cached
    When the service is updated
    Then the cache should be invalidated immediately
    And subsequent reads should reflect new data

  @concurrency @cache @stale-cache
  Scenario: Detect and handle stale cache
    Given service details are cached
    And cache is updated in background
    When a read occurs during cache update
    Then the system should detect stale cache
    And fetch fresh data from database

  @concurrency @cache @distributed-cache-sync
  Scenario: Synchronize distributed cache nodes
    Given multiple cache nodes exist
    When service is updated on one node
    Then all cache nodes should be updated
    And reads from any node should return consistent data

  # ==================== EVENT ORDERING ====================

  @concurrency @events @ordered-processing
  Scenario: Events are processed in order
    When service undergoes multiple state changes:
      | Order | Action     | Status   |
      | 1     | Create     | Draft    |
      | 2     | Activate   | Active   |
      | 3     | Deactivate | Inactive |
      | 4     | Archive    | Archived |
    Then events should be published in correct order
    And event consumers should process them sequentially

  @concurrency @events @concurrent-event-publishing
  Scenario: Handle concurrent event publishing
    Given multiple services are updated simultaneously
    When all publish events to the event bus
    Then all events should be published successfully
    And no events should be lost or duplicated

  # ==================== PERFORMANCE DEGRADATION ====================

  @concurrency @performance @gradual-load
  Scenario: System performance under increasing load
    When concurrent user count increases from 10 to 1000
    Then response time should degrade gracefully
    And system should not crash or deadlock
    And error rate should remain below 5%

  @concurrency @performance @connection-pooling
  Scenario: Database connection pool under load
    Given connection pool has 20 connections
    When 100 concurrent requests are made
    Then requests should queue for available connections
    And all requests should complete successfully
    And no connection leaks should occur

  # ==================== AUDIT TRAIL CONSISTENCY ====================

  @concurrency @audit @concurrent-logging
  Scenario: Maintain audit trail under concurrency
    When 100 concurrent service updates occur
    Then all updates should be logged in audit trail
    And audit entries should be in correct sequence
    And no audit entries should be lost

  @concurrency @audit @versioned-audit
  Scenario: Audit trail tracks all version changes
    When concurrent updates result in version conflicts
    Then audit trail should record all attempted changes
    And should indicate which attempts succeeded vs failed
