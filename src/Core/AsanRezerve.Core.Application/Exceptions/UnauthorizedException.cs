// AsanRezerve.SharedKernel.Domain/Exceptions/DomainExceptions.cs
// AsanRezerve.Application.Common/Exceptions/ApplicationExceptions.cs
namespace AsanRezerve.Core.Application.Exceptions
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