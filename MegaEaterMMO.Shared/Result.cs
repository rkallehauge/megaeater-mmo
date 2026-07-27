namespace MegaEaterMMO.Shared
{
    public class Result
    {
        public bool Success { get; protected set; }
        public List<Error>? Errors { get; protected set; } = new();

        public static Result OK()
        {
            return new Result { Success = true };
        }

        public static Result Fail(params Error[] errors)
            => new Result { Success = false, Errors = errors.ToList() };
    }

    public class Result<T> : Result
    {
        public T? Data { get; protected set; }
        public static Result<T> OK(T data)
        {
            return new Result<T> { Success = true, Data = data };
        }
        public new static Result<T> Fail(params Error[] errors)
            => new Result<T> { Success = false, Errors = errors.ToList() };
    }
}
