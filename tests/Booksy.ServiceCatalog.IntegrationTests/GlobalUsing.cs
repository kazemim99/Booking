
// `Startup` throughout this suite means "the entry point of the system under test", which is used only
// as WebApplicationFactory<T>'s type argument to locate the assembly holding the entry point.
//
// It used to resolve to Booksy.API.Startup, a class that physically lives in Booksy.ServiceCatalog.Api —
// a Microsoft.NET.Sdk.Web project with its own Program.cs. WebApplicationFactory therefore booted the
// ServiceCatalog *per-service host*, which the migration to a modular monolith retired: in production
// only Booksy.Host's Program runs, and it is the Host that composes both contexts, applies the
// cross-context in-process overrides, and configures the middleware pipeline. Every assertion in this
// suite was being made against a host that no longer exists.
//
// Aliasing to Booksy.Host's marker retargets all ~100 files at once without editing them individually.
global using Startup = Booksy.Host.HostEntryPoint;
global using Booksy.ServiceCatalog.API.Controllers.V1;
global using Booksy.ServiceCatalog.API.Models.Requests;
global using Booksy.ServiceCatalog.API.Models.Responses;
global using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
global using FluentAssertions;
global using System.Net;
global using DayOfWeek = Booksy.ServiceCatalog.Domain.Enums.DayOfWeek;
