// `Startup` throughout this project means "the entry point of the system under test", used only as
// AsanRezerveHostFactory's WebApplicationFactory<T> type argument to locate the assembly holding the
// entry point.
//
// It used to resolve to two different retired per-service hosts depending on which of the three
// old projects a file lived in (AsanRezerve.API.Startup for ServiceCatalog, AsanRezerve.UserManagement.API's
// own Program for UserManagement) — both migrations to a modular monolith retired: in production
// only AsanRezerve.Host's Program runs, and it is the Host that composes both contexts, applies the
// cross-context in-process adapters (InProcessProviderInfoService, InProcessTokenService,
// InProcessPersonAccountProvisioningService), and wires the client rate limiter.
//
// The alias is named `Startup`, not `Program`: this project references AsanRezerve.Host, which
// transitively references BOTH AsanRezerve.UserManagement.API and AsanRezerve.ServiceCatalog.Api, and each of
// those (like every ASP.NET Core entry point using top-level statements) generates its OWN
// `public partial class Program` in the GLOBAL namespace — so a `Program` alias collides (CS0576)
// the moment this project references AsanRezerve.Host. `Startup` has no such collision (both
// AsanRezerve.API.Startup and AsanRezerve.ServiceCatalog.Api.Startup live in a namespace, not the global one).
global using Startup = AsanRezerve.Host.HostEntryPoint;

// The shared test harness (AsanRezerveHostFactory, AsanRezerveHostTestCollection, IntegrationTestBase<>,
// IResettableFake) and the two bounded-context base classes, globally used so every leaf test class
// resolves them without a per-file `using` — exactly as each context's own GlobalUsing.cs did before
// the three projects merged into this one (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 4). Leaf
// test classes deliberately keep the namespace each was declared in before the merge
// (AsanRezerve.ServiceCatalog.IntegrationTests.*, AsanRezerve.UserManagement.IntegrationTests.*,
// AsanRezerve.Host.CompositionTests) rather than being renamed to match their new folder: a leaf test
// class's namespace is never referenced by anything outside the file that declares it, so renaming
// ~65 files for that alone would have been a large, purely cosmetic diff. Only the shared
// infrastructure below — code other files actually depend on by name — moved to a namespace that
// reflects the merged project.
global using AsanRezerve.Host.IntegrationTests.Infrastructure;
global using AsanRezerve.Host.IntegrationTests.ServiceCatalog;
global using AsanRezerve.Host.IntegrationTests.UserManagement;

// ServiceCatalog-specific globals, unchanged from that suite's own GlobalUsing.cs.
global using AsanRezerve.ServiceCatalog.API.Controllers.V1;
global using AsanRezerve.ServiceCatalog.API.Models.Requests;
global using AsanRezerve.ServiceCatalog.API.Models.Responses;
global using FluentAssertions;
global using System.Net;
global using DayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;
