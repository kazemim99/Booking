﻿//namespace Booksy.SharedKernel.Domain.Abstractions
//{
//    /// <summary>
//    /// Base class for value objects
//    /// </summary>
//    public abstract class ValueObject
//    {
//        protected abstract IEnumerable<object?> GetAtomicValues();

//        public override bool Equals(object? obj)
//        {
//            if (obj == null || obj.GetType() != GetType())
//            {
//                return false;
//            }

//            var other = (ValueObject)obj;
//            return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
//        }

//        public override int GetHashCode()
//        {
//            return GetAtomicValues()
//                .Select(x => x?.GetHashCode() ?? 0)
//                .Aggregate((x, y) => x ^ y);
//        }

//        public static bool operator ==(ValueObject? left, ValueObject? right)
//        {
//            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
//                return true;

//            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
//                return false;

//            return left.Equals(right);
//        }

//        public static bool operator !=(ValueObject? left, ValueObject? right)
//        {
//            return !(left == right);
//        }
//    }
//}