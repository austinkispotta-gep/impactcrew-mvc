namespace ImpactCrew.Models.Api.Attachment;

public class UploadResponse
{
    public UploadReturnValue ReturnValue { get; set; } = new();
    public bool IsSuccess { get; set; }
    public object? Errors { get; set; }
    public object? Exception { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? CorrelationId { get; set; }
}

public class UploadReturnValue
{
    public string RefId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int FailedCount { get; set; }
}
