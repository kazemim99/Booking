namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// Onboarding "Do you personally provide services?" answer.
/// </summary>
public sealed record SetOwnerProvidesServicesRequest(bool ProvidesServices);
