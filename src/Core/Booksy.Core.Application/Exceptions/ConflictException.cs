// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
// Booksy.Application.Common/Exceptions/ApplicationExceptions.cs
namespace Booksy.Core.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when there's a conflict in the application layer
    /// </summary>
    [Serializable]
    public class ConflictException : ApplicationException
    {
        public override string ErrorCode => "RESOURCE_CONFLICT";

        public ConflictException(string message) : base(message) { }

    }
}