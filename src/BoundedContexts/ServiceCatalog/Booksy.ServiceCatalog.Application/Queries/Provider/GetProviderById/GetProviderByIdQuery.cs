﻿// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderById/GetProviderByIdQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    /// <summary>
    /// Query to get provider details by ID
    /// </summary>
    public sealed record GetProviderByIdQuery(
        Guid ProviderId,
        bool IncludeServices = false,
        bool IncludeStaff = false) : IQuery<ProviderDetailsViewModel?>;
}