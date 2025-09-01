// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
// Booksy.Application.Common/Exceptions/ApplicationExceptions.cs

// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
// Booksy.Application.Common/Exceptions/ApplicationExceptions.cs
namespace Booksy.Core.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a resource is not found in the application layer
    /// </summary>
    [Serializable]
    public class NotFoundException : ApplicationException
    {
        public string ResourceName { get; }
        public override string ErrorCode => "RESOURCE_NOT_FOUND";

        public NotFoundException(string resourceName)
            : base($"{resourceName} was not found")
        {
            ResourceName = resourceName;
        }

        public NotFoundException(string resourceName, object key)
            : base($"{resourceName} with key '{key}' was not found")
        {
            ResourceName = resourceName;
        }
    }
}