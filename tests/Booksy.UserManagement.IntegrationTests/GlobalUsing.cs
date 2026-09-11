// `Startup` throughout this suite means "the entry point of the system under test" — used only as
// UserManagementTestWebApplicationFactory<TStartup>'s type argument to locate the assembly holding
// the entry point.
//
// It used to be `Program`, resolving to Booksy.UserManagement.API's own retired per-service host:
// in production only Booksy.Host's Program runs, and it is the Host that composes both bounded
// contexts, applies the cross-context in-process adapters (InProcessProviderInfoService,
// InProcessTokenService, InProcessPersonAccountProvisioningService), and wires the client rate
// limiter. Every assertion in this suite was being made against a host that no longer exists
// (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 3).
//
// The alias is named `Startup`, not `Program`, even though it is still Booksy.Host's `Program` it
// points at: Booksy.Host transitively references BOTH Booksy.UserManagement.API and
// Booksy.ServiceCatalog.Api, and each of those (like every ASP.NET Core entry point using top-level
// statements) generates its OWN `public partial class Program` in the GLOBAL namespace — so
// `global using Program = ...` collides with an actual type of that name (CS0576) the moment this
// project references Booksy.Host. `Startup` has no such collision (Booksy.API.Startup and
// Booksy.ServiceCatalog.Api.Startup both live in a namespace, not the global one) and matches the
// name the ServiceCatalog suite's own GlobalUsing.cs already uses for the identical alias.
global using Startup = Booksy.Host.HostEntryPoint;
