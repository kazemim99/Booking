//// ========================================
//// Booksy.UserManagement.Domain/ValueObjects/UserId.cs
//// ========================================
//using Booksy.Core.Domain.Base;
//using Booksy.UserManagement.Domain.Exceptions;
//using System.Text.RegularExpressions;

//namespace Booksy.UserManagement.Domain.ValueObjects
//{
//    /// <summary>
//    /// Represents a validated email address
//    /// </summary>
//    public sealed partial class Email : ValueObject
//    {
//        private static readonly Regex EmailRegex = EmailValidationRegex();

//        public string Value { get; }
//        public string LocalPart { get; }
//        public string Domain { get; }

//        private Email(string value)
//        {
//            Value = value.ToLowerInvariant();
//            var parts = Value.Split('@');
//            LocalPart = parts[0];
//            Domain = parts[1];
//        }

//        public static Email Create(string email)
//        {
//            if (string.IsNullOrWhiteSpace(email))
//                throw new InvalidUserProfileException("Email address cannot be empty");

//            email = email.Trim().ToLowerInvariant();

//            if (!IsValid(email))
//                throw new InvalidUserProfileException($"Invalid email format: {email}");

//            return new Email(email);
//        }

//        public static bool IsValid(string email)
//        {
//            if (string.IsNullOrWhiteSpace(email))
//                return false;

//            if (email.Length > 256)
//                return false;

//            return EmailRegex.IsMatch(email);
//        }

//        public static bool TryCreate(string email, out Email? result)
//        {
//            result = null;

//            if (!IsValid(email))
//                return false;

//            result = new Email(email);
//            return true;
//        }

//        protected override IEnumerable<object?> GetAtomicValues()
//        {
//            yield return Value;
//        }

//        public override string ToString() => Value;

//        public static implicit operator string(Email email) => email.Value;

//        [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
//        private static partial Regex EmailValidationRegex();
//    }
//}


