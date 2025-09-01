// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
// Booksy.Application.Common/Exceptions/ApplicationExceptions.cs
namespace Booksy.Core.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when access is forbidden
    /// </summary>
    [Serializable]
    public class ForbiddenException : ApplicationException
    {
        public override string ErrorCode => "ACCESS_FORBIDDEN";

        public ForbiddenException(string message)
            : base(message) { }

        public ForbiddenException()
            : base("You do not have permission to access this resource") { }

    }
}