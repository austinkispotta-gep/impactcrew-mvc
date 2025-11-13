namespace ImpactCrew.Models.Api.Agent;

public class GetAgentByIdResponse
{
    public GetAgentReturnValue ReturnValue { get; set; } = new();
    public bool IsSuccess { get; set; }
    public object? Errors { get; set; }
    public object? Exception { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? CorrelationId { get; set; }
}

public class GetAgentReturnValue
{
    public string AgentId { get; set; } = string.Empty;
    public string DbType { get; set; } = string.Empty;
    public List<AgentKnowledgeItem> Knowledge { get; set; } = new();
    public string RefId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AgentModel Model { get; set; } = new();
    public bool IsDisabled { get; set; }
    public bool IsRelatedQuestionsEnabled { get; set; }
    public bool IsSourcesEnabled { get; set; }
    public List<object> Examples { get; set; } = new();
    public List<PublishChannel> PublishChannels { get; set; } = new();
    public string BpcCode { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class PublishChannel
{
    public string RefId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AgentModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class AgentKnowledgeItem
{
    public string RefId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
