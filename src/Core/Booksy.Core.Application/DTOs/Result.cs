// ========================================
// Booksy.Core.Application/DTOs/Result.cs
// ========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.Core.Application.DTOs
{
    /// <summary>
    /// Represents the result of an operation
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public ErrorResult? Error { get; }

        protected Result(bool isSuccess, T? value, ErrorResult? error)
        {
            if (isSuccess && error != null)
                throw new InvalidOperationException("A successful result cannot have an error");

            if (!isSuccess && error == null)
                throw new InvalidOperationException("A failed result must have an error");

            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null);
        }


        public static Result<T> Failure(string error)
        {
            return new Result<T>(false, default, new ErrorResult("",error));
        }
        public static Result<T> Failure(ErrorResult error)
        {
            return new Result<T>(false, default, error);
        }

        public static Result<T> Failure(string code, string message)
        {
            return Failure(new ErrorResult(code, message));
        }

        public static implicit operator Result<T>(T value)
        {
            return Success(value);
        }

        public static implicit operator Result<T>(ErrorResult error)
        {
            return Failure(error);
        }

        public Result<TOutput> Map<TOutput>(Func<T, TOutput> mapper)
        {
            return IsSuccess
                ? Result<TOutput>.Success(mapper(Value!))
                : Result<TOutput>.Failure(Error!);
        }

        public async Task<Result<TOutput>> MapAsync<TOutput>(Func<T, Task<TOutput>> mapper)
        {
            return IsSuccess
                ? Result<TOutput>.Success(await mapper(Value!))
                : Result<TOutput>.Failure(Error!);
        }

        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess)
                action(Value!);

            return this;
        }

        public Result<T> OnFailure(Action<ErrorResult> action)
        {
            if (IsFailure)
                action(Error!);

            return this;
        }

        //public T Match<T>(Func<T, T> onSuccess, Func<ErrorResult, T> onFailure)
        //{
        //    return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
        //}
    }

    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public ErrorResult? Error { get; }
        public string Value { get; }
        protected Result(bool isSuccess, string value, ErrorResult? error)
        {
            if (isSuccess && error != null)
                throw new InvalidOperationException("A successful result cannot have an error");

            if (!isSuccess && error == null)
                throw new InvalidOperationException("A failed result must have an error");

            IsSuccess = isSuccess;
            Error = error;
            Value = value;
        }


        public static Result Success(string value)
        {
            return new Result(true, value, null);
        }


        public static Result Success()
        {
            return new Result(true, null, null);
        }
        public static Result Failure(string value)
        {
            return new Result(false, value, null);
        }

        public static Result Failure(ValidationResult value)
        {
            return new Result(false, value.ErrorMessage, null);
        }
        public static Result Failure(ErrorResult error)
        {
            return new Result(false, default, error);
        }

        public static Result Failure(string code, string message)
        {
            return Failure(new ErrorResult(code, message));
        }
    }
}