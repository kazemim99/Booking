

public class ErrorResult
{
    public string Message { get; set; }
    public string? Code { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }

    public ErrorResult(string message, string? code = null)
    {
        Message = message;
        Code = code;
    }
}
