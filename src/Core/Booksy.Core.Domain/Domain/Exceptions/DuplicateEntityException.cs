// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
using Booksy.Core.Domain.Errors;
using System.Runtime.Serialization;

namespace Booksy.Core.Domain.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a duplicate entity is detected
    /// </summary>
    [Serializable]
    public class DuplicateEntityException : DomainException
    {
        public string EntityName { get; }
        public string DuplicateField { get; }
        public object DuplicateValue { get; }
        public override ErrorCode ErrorCode => ErrorCode.DUPLICATE_ENTITY;

        public DuplicateEntityException(string entityName, string duplicateField, object duplicateValue)
            : base($"{entityName} with {duplicateField} '{duplicateValue}' already exists")
        {
            EntityName = entityName;
            DuplicateField = duplicateField;
            DuplicateValue = duplicateValue;
            ExtensionData = new Dictionary<string, object>
            {
                ["entityName"] = entityName,
                ["duplicateField"] = duplicateField,
                ["duplicateValue"] = duplicateValue?.ToString() ?? string.Empty
            };
        }

        protected DuplicateEntityException(SerializationInfo info, StreamingContext context)
         
        {
            EntityName = info.GetString(nameof(EntityName)) ?? "Unknown";
            DuplicateField = info.GetString(nameof(DuplicateField)) ?? "Unknown";
            DuplicateValue = info.GetValue(nameof(DuplicateValue), typeof(object)) ?? "Unknown";
        }
    }
}
