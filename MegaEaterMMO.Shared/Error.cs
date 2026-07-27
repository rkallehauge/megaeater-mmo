namespace MegaEaterMMO.Shared
{
    public class Error
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
