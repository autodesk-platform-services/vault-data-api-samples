public class ErrorResponse
{
    public int StatusCode { get; set; }
    public required string ErrorCode { get; set; }
    public required string Title { get; set; }
    public required string Detail { get; set; }
}