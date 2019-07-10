using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LBPCPlugIn
{
    public class Result<T>
    {
        public bool IsValid { get; private set; }
        public string Message { get; private set; }
        public T Value { get; private set; }
        public string Type { get; private set; }

        public static Result<T> Exception(Exception exception)
        {
            return new Result<T>()
            {
                IsValid = false,
                Message = exception.Message,
                Value = default(T),
                Type = string.Empty
            };
        }

        public static Result<T> Failure(string message = "")
        {
            return new Result<T>()
            {
                IsValid = false,
                Message = message,
                Value = default(T),
                Type = string.Empty
            };
        }

        public static Result<T> Success(T value, string type)
        {
            return new Result<T>()
            {
                IsValid = true,
                Message = String.Empty,
                Value = value,
                Type = type
            };
        }
    }
}
