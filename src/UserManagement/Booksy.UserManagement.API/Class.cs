//using Booksy.Core.Application.Abstractions.Services;
//using Booksy.Infrastructure.Core.EventBus.Abstractions;
//using Booksy.UserManagement.Infrastructure.Persistence.Context;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.EntityFrameworkCore.Storage;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Options;
//using System.IO;

//public class UserManagementDbContextFactory : IDesignTimeDbContextFactory<UserManagementDbContext>
//{
//    public UserManagementDbContextFactory()
//    {
        
//    }
//    private readonly ICurrentUserService? _currentUserService;
//    private readonly IDateTimeProvider? _dateTimeProvider;
//    //private readonly List<IDomainEvent> _domainEvents = new();
//    private readonly IDomainEventDispatcher _eventDispatcher;
//    public UserManagementDbContextFactory(ICurrentUserService? currentUserService, IDateTimeProvider? dateTimeProvider, IDomainEventDispatcher eventDispatcher)
//    {
//        _currentUserService = currentUserService;
//        _dateTimeProvider = dateTimeProvider;
//        _eventDispatcher = eventDispatcher;
//    }

//    public UserManagementDbContext CreateDbContext(string[] args)
//    {
//        // load config from appsettings.json
//        var configuration = new ConfigurationBuilder()
//            .SetBasePath(Directory.GetCurrentDirectory())
//            .AddJsonFile("appsettings.json", optional: false)
//            .Build();

//        var optionsBuilder = new DbContextOptionsBuilder<UserManagementDbContext>();
//        var connectionString = configuration.GetConnectionString("UserManagement")
//                          ?? configuration.GetConnectionString("DefaultConnection");

//        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
//        {
//            npgsqlOptions.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.FullName);
//            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "user_management");
//            npgsqlOptions.CommandTimeout(30);
//            npgsqlOptions.EnableRetryOnFailure(
//                maxRetryCount: 3,
//            maxRetryDelay: TimeSpan.FromSeconds(5),
//                errorCodesToAdd: null);
//        });
//        return new UserManagementDbContext(optionsBuilder.Options,_currentUserService,_dateTimeProvider,_eventDispatcher);
//    }
//}
