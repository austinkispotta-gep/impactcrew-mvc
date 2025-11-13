namespace ImpactCrew.Models.Api.Agent;

public class AgentInvokeResponse
{
    public AgentInvokeReturnValue ReturnValue { get; set; } = new();
    public bool IsSuccess { get; set; }
    public object? Errors { get; set; }
    public object? Exception { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? CorrelationId { get; set; }
}

public class AgentInvokeReturnValue
{
    public List<AgentMessage> Messages { get; set; } = new();
    public List<string> RelatedQuestions { get; set; } = new();
}

public class AgentMessage
{
    public string Role { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<AgentFile> Files { get; set; } = new();
    public List<object> Citations { get; set; } = new();
}

public class AgentFile
{
    // Matches sample: { "name": "Export-Approved...xlsx", "id": "AI-Export/..." }
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty; // path used for download
    // Convenience property for UI if needed
    public string FilePath => Id;
}
