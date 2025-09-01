// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an entity is not found
    /// </summary>
    [Serializable]
    public class EntityNotFoundException : DomainException
    {
        public string EntityName { get; }
        public object EntityId { get; }
        public override string ErrorCode => "ENTITY_NOT_FOUND";

        public EntityNotFoundException(string entityName, object entityId)
            : base($"{entityName} with ID '{entityId}' was not found.")
        {
            EntityName = entityName;
            EntityId = entityId;
            ExtensionData = new Dictionary<string, object>
            {
                ["entityName"] = entityName,
                ["entityId"] = entityId.ToString() ?? string.Empty
            };
        }

        public EntityNotFoundException(string entityName, object entityId, string customMessage)
            : base(customMessage)
        {
            EntityName = entityName;
            EntityId = entityId;
            ExtensionData = new Dictionary<string, object>
            {
                ["entityName"] = entityName,
                ["entityId"] = entityId.ToString() ?? string.Empty
            };
        }

        protected EntityNotFoundException(SerializationInfo info, StreamingContext context)
        {
            EntityName = info.GetString(nameof(EntityName)) ?? "Unknown";
            EntityId = info.GetValue(nameof(EntityId), typeof(object)) ?? "Unknown";
        }
    }
}
