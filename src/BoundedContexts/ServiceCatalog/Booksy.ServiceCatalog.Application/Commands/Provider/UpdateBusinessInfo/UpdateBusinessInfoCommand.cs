//using Booksy.Core.Application.Abstractions.CQRS;

//namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessInfo;

///// <summary>
///// Command to update provider business information
///// </summary>
//public sealed record UpdateBusinessInfoCommand(
//    Guid ProviderId,
//    string BusinessName,
//    string? Description,
//    Guid? IdempotencyKey = null) : ICommand<UpdateBusinessInfoResult>;

//public sealed record UpdateBusinessInfoResult(
//    Guid ProviderId,
//    string BusinessName,
//    DateTime UpdatedAt);
