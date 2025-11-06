global using Booksy.Core.Domain.Base;
global using Booksy.Core.Domain.ValueObjects;
global using Booksy.ServiceCatalog.Domain.Enums;
global using Booksy.ServiceCatalog.Domain.ValueObjects;
global using Booksy.Core.Domain.Abstractions.ValueObjects;
global using DayOfWeek = Booksy.ServiceCatalog.Domain.Enums.DayOfWeek;
global using Booksy.Core.Application.Abstractions.CQRS;
global using Booksy.Core.Application.Abstractions.Events;
global using Booksy.Infrastructure.Core.EventBus.Abstractions;
global using Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification;
global using Booksy.ServiceCatalog.Domain.Events;
global using MediatR;
global using Microsoft.Extensions.Logging;

