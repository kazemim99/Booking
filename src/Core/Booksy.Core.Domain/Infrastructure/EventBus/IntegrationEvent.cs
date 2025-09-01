//namespace Booksy.SharedKernel.Domain.Abstractions
//{

//    /// <summary>
//    /// Base class for integration events
//    /// </summary>
//    public abstract class IntegrationEvent : IIntegrationEvent
//    {
//        protected IntegrationEvent()
//        {
//            EventId = Guid.NewGuid();
//            OccurredOn = DateTime.UtcNow;
//            EventVersion = 1;
//        }

//        public Guid EventId { get; private set; }
//        public DateTime OccurredOn { get; private set; }
//        public virtual string EventType => GetType().Name;
//        public virtual int EventVersion { get; protected set; }
//    }
//}