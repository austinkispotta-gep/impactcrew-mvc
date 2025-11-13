namespace ImpactCrew.Models.Api.Knowledge;

public class DeleteKnowledgeRequest
{
    public string ModuleId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string BpcCode { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string RefId { get; set; } = string.Empty;
}

public class DeleteKnowledgeResponse
{
    public int ReturnValue { get; set; }
    public bool IsSuccess { get; set; }
    public object? Errors { get; set; }
    public object? Exception { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? CorrelationId { get; set; }
}
