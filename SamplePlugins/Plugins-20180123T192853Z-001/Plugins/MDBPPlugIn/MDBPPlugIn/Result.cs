using System;


namespace MDBPPlugIn
{
    public class Result<T>
    {
        public string Error
        {
            get;
            private set;
        }

        public bool IsValid
        {
            get;
            private set;
        }

        public T Value
        {
            get;
            private set;
        }

        public static Result<T> Exception(Exception exception)
        {
            return new Result<T>() { Error = exception.Message, IsValid = false, Value = default(T) };
        }

        public static Result<T> Failure(string message = "")
        {
            return new Result<T>() { Error = message, IsValid = false, Value = default(T) };
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>() { Error = String.Empty, IsValid = true, Value = value };
        }
    }
}
