#!/bin/bash

# Script to fix EF Core pending migrations issue
# This removes the empty migration and creates a new one if needed

cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

echo "=== Removing empty ModifyUserProfile migration ==="
rm -f Migrations/20251110165859_ModifyUserProfile.cs
rm -f Migrations/20251110165859_ModifyUserProfile.Designer.cs

echo ""
echo "=== Checking for pending model changes ==="
dotnet ef migrations add RemovePendingChanges \
  --startup-project ../Booksy.ServiceCatalog.Api \
  --context ServiceCatalogDbContext

echo ""
echo "=== Migration created. Checking if it's empty... ==="
MIGRATION_FILE=$(ls -t Migrations/*_RemovePendingChanges.cs | head -1)

if grep -q "protected override void Up(MigrationBuilder migrationBuilder)" "$MIGRATION_FILE"; then
  echo "Migration file found: $MIGRATION_FILE"

  # Check if Up() method is empty
  if grep -A 3 "protected override void Up" "$MIGRATION_FILE" | grep -q "^        }$"; then
    echo "Migration is empty - no actual schema changes needed"
    echo "Removing empty migration..."
    rm -f Migrations/*_RemovePendingChanges.*
    echo ""
    echo "✅ No schema changes detected. The warning can be suppressed."
    echo ""
    echo "To suppress this warning, add this to ServiceCatalogDbContext.OnConfiguring:"
    echo ""
    echo "protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)"
    echo "{"
    echo "    optionsBuilder.ConfigureWarnings(warnings =>"
    echo "        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));"
    echo "}"
  else
    echo "Migration has schema changes. Apply it with:"
    echo "dotnet ef database update --startup-project ../Booksy.ServiceCatalog.Api --context ServiceCatalogDbContext"
  fi
fi

echo ""
echo "Done!"
