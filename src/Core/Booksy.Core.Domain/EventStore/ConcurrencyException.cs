//// Booksy.SharedKernel.Infrastructure.EventStore/IEventStore.cs
//using Booksy.SharedKernel.Domain.Exceptions;

//namespace Booksy.SharedKernel.EventStore;

///// <summary>
///// Exception thrown when there's a concurrency conflict
///// </summary>
//public class ConcurrencyException : DomainException
//{
//    public string AggregateId { get; }
//    public long ExpectedVersion { get; }
//    public long ActualVersion { get; }

//    public override string ErrorCode => "Concurrency_Confilict";

//    public ConcurrencyException(string aggregateId, long expectedVersion, long actualVersion)
//        : base($"Concurrency conflict for aggregate {aggregateId}. Expected version {expectedVersion}, but was {actualVersion}")
//    {
//        AggregateId = aggregateId;
//        ExpectedVersion = expectedVersion;
//        ActualVersion = actualVersion;
//    }
//}