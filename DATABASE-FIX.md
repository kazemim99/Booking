# Database Consistency Fix

## ❌ **Problem Identified**

The CI/CD pipeline was using **MS SQL Server** for testing, while the actual development and production environments use **PostgreSQL**. This created a critical mismatch that could cause:

1. **Tests passing in CI but failing in production**
2. **SQL syntax differences** (T-SQL vs PostgreSQL)
3. **Different database behaviors** and data types
4. **False confidence** in code quality

## 🔍 **What Was Wrong**

### Before the Fix:

| Environment | Database | Port | Connection String Format |
|-------------|----------|------|--------------------------|
| **CI/CD (GitHub Actions)** | MS SQL Server 2022 | 1433 | `Server=localhost,1433;...` |
| **Development (docker-compose.yml)** | PostgreSQL 16 | 5432 | `Host=postgres;Port=5432;...` |
| **Production (docker-compose.prod.yml)** | PostgreSQL 16 | 5432 | `Host=postgres;Port=5432;...` |

### Issues with MS SQL Server in CI/CD:

```yaml
# OLD - Using MS SQL Server ❌
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    env:
      ACCEPT_EULA: Y
      SA_PASSWORD: YourStrong@Passw0rd
      MSSQL_PID: Developer
    ports:
      - 1433:1433
```

```csharp
// Connection string difference ❌
// MS SQL: "Server=localhost,1433;Database=BooksyTest;User Id=sa;..."
// PostgreSQL: "Host=localhost;Port=5432;Database=booksy_test;Username=booksy_admin;..."
```

## ✅ **Solution Applied**

Changed both CI/CD workflows to use **PostgreSQL 16-alpine** to match your development and production environments.

### Files Updated:

1. **[.github/workflows/deploy.yml](.github/workflows/deploy.yml)** - Deployment pipeline
2. **[.github/workflows/dotnet.yml](.github/workflows/dotnet.yml)** - Existing CI/CD pipeline

### After the Fix:

| Environment | Database | Port | Connection String Format |
|-------------|----------|------|--------------------------|
| **CI/CD (GitHub Actions)** | PostgreSQL 16 ✅ | 5432 | `Host=localhost;Port=5432;...` |
| **Development** | PostgreSQL 16 ✅ | 5432 | `Host=postgres;Port=5432;...` |
| **Production** | PostgreSQL 16 ✅ | 5432 | `Host=postgres;Port=5432;...` |

### New Configuration:

```yaml
# NEW - Using PostgreSQL ✅
services:
  postgres:
    image: postgres:16-alpine
    env:
      POSTGRES_USER: booksy_admin
      POSTGRES_PASSWORD: TestPassword123!
      POSTGRES_DB: booksy_test
    ports:
      - 5432:5432
    options: >-
      --health-cmd "pg_isready -U booksy_admin -d booksy_test"
      --health-interval 10s
      --health-timeout 5s
      --health-retries 5
```

```yaml
# Updated connection string ✅
env:
  ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=booksy_test;Username=booksy_admin;Password=TestPassword123!;Include Error Detail=true"
  ConnectionStrings__Redis: "localhost:6379"
```

## 🎯 **Benefits of This Fix**

1. ✅ **Consistency** - Same database across all environments
2. ✅ **Accurate Testing** - Tests run against the actual database type used in production
3. ✅ **SQL Compatibility** - No more syntax differences between test and production
4. ✅ **Faster Feedback** - Catch PostgreSQL-specific issues early in CI/CD
5. ✅ **Smaller Image Size** - PostgreSQL Alpine image is lighter than MS SQL Server
6. ✅ **Faster Tests** - PostgreSQL typically starts faster than MS SQL Server

## 📊 **Technical Comparison**

### MS SQL Server vs PostgreSQL:

| Feature | MS SQL Server | PostgreSQL |
|---------|---------------|------------|
| Image Size | ~1.5 GB | ~200 MB (alpine) |
| Startup Time | 20-30 seconds | 5-10 seconds |
| Memory Usage | ~2 GB minimum | ~100 MB minimum |
| License | Proprietary | Open Source |
| Matches Your Stack | ❌ No | ✅ Yes |

### Connection String Differences:

```bash
# MS SQL Server (OLD) ❌
Server=localhost,1433;Database=BooksyTest;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;

# PostgreSQL (NEW) ✅
Host=localhost;Port=5432;Database=booksy_test;Username=booksy_admin;Password=TestPassword123!;Include Error Detail=true
```

### SQL Syntax Differences Examples:

```sql
-- MS SQL Server ❌
SELECT TOP 10 * FROM Users;
GETDATE()
ISNULL(column, 'default')

-- PostgreSQL ✅
SELECT * FROM Users LIMIT 10;
NOW()
COALESCE(column, 'default')
```

## 🚀 **Impact on Your Workflow**

### Before:
1. Code passes tests in CI (MS SQL)
2. Deploy to production (PostgreSQL)
3. **Possible runtime errors** due to SQL differences

### After:
1. Code passes tests in CI (PostgreSQL) ✅
2. Deploy to production (PostgreSQL) ✅
3. **Same behavior in both environments** ✅

## 📝 **What You Need to Do**

### Nothing! 🎉

The changes have been made to both workflow files. Next time you push to GitHub:

1. Tests will run against PostgreSQL
2. If you have any MS SQL-specific code, tests will now catch it
3. You'll have confidence that passing tests = working production code

### Optional: Review Your Code

If you have any database-specific code, review it for MS SQL Server assumptions:

```bash
# Search for potential MS SQL-specific code
git grep -i "TOP " -- "*.cs"
git grep -i "GETDATE" -- "*.cs"
git grep -i "ISNULL" -- "*.cs"
git grep -i "@@IDENTITY" -- "*.cs"
```

## 🧪 **Testing the Fix**

To verify the fix works:

```bash
# Commit the changes
git add .
git commit -m "Fix: Use PostgreSQL in CI/CD instead of MS SQL Server

- Replace MS SQL Server with PostgreSQL 16-alpine in GitHub Actions
- Update connection strings to PostgreSQL format
- Ensure consistency across dev, test, and production environments"

# Push and watch the workflow
git push origin master
```

Then check the Actions tab on GitHub to see tests running with PostgreSQL.

## 📚 **Additional Resources**

- [Npgsql - .NET PostgreSQL Driver](https://www.npgsql.org/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core with PostgreSQL](https://www.npgsql.org/efcore/)
- [Migrating from SQL Server to PostgreSQL](https://wiki.postgresql.org/wiki/Converting_from_other_Databases_to_PostgreSQL)

## ❓ **FAQ**

**Q: Will this break my existing code?**
A: No, if your code already works with PostgreSQL in development, it will work in CI/CD.

**Q: What if I was intentionally using MS SQL Server?**
A: That would create a mismatch with your docker-compose files. If you need MS SQL Server, you should update docker-compose.yml and docker-compose.prod.yml to use it instead.

**Q: Do I need to change my Entity Framework configurations?**
A: No, if you're already using Npgsql for PostgreSQL, everything should work as-is.

**Q: What about the test database?**
A: Each CI/CD run creates a fresh PostgreSQL instance with the database `booksy_test` - it's isolated and cleaned up automatically.

## ✅ **Summary**

- **Fixed**: CI/CD now uses PostgreSQL (matching dev and prod)
- **Benefit**: Consistent database across all environments
- **Action Required**: None - just push your code!
- **Result**: More reliable tests and fewer production surprises

---

**Status**: ✅ **Fixed and ready to deploy**
