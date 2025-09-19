//// ========================================
//// Booksy.ServiceCatalog.Domain/ValueObjects/StaffId.cs
//// ========================================
//using Booksy.Core.Domain.Base;

//namespace Booksy.ServiceCatalog.Domain.ValueObjects
//{
//    public sealed class StaffId : ValueObject
//    {
//        public Guid Value { get; }

//        private StaffId(Guid value)
//        {
//            if (value == Guid.Empty)
//                throw new ArgumentException("StaffId cannot be empty", nameof(value));

//            Value = value;
//        }

//        public static StaffId New() => new(Guid.NewGuid());
//        public static StaffId From(Guid value) => new(value);

//        public override string ToString() => Value.ToString();

//        protected override IEnumerable<object> GetAtomicValues()
//        {
//            yield return Value;
//        }

//        // Implicit conversions
//        public static implicit operator Guid(StaffId staffId) => staffId.Value;
//        public static implicit operator StaffId(Guid value) => From(value);
//    }
//}