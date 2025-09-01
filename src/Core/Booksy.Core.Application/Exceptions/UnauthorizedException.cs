// Booksy.SharedKernel.Domain/Exceptions/DomainExceptions.cs
// Booksy.Application.Common/Exceptions/ApplicationExceptions.cs
namespace Booksy.Core.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when user is not authorized
    /// </summary>
    [Serializable]
    public class UnauthorizedException : ApplicationException
    {
        public override string ErrorCode => "UNAUTHORIZED";

        public UnauthorizedException(string message)
            : base(message) { }

        public UnauthorizedException()
            : base("You must be authenticated to access this resource") { }

    }
}