global using AsanRezerve.Core.Domain.Base;
global using AsanRezerve.Core.Domain.ValueObjects;
global using AsanRezerve.ServiceCatalog.Domain.Enums;
global using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
global using AsanRezerve.Core.Domain.Abstractions.ValueObjects;
global using DayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;
global using AsanRezerve.Core.Application.Abstractions.CQRS;
global using AsanRezerve.Core.Application.Abstractions.Events;
global using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
global using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
global using AsanRezerve.ServiceCatalog.Application.Commands.Notifications.SendNotification;
global using AsanRezerve.ServiceCatalog.Domain.Events;
global using MediatR;
global using Microsoft.Extensions.Logging;

