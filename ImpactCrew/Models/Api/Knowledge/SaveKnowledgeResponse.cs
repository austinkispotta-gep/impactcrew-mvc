namespace ImpactCrew.Models.Api.Knowledge;

public class SaveKnowledgeResponse
{
    public List<SaveKnowledgeReturnValue> ReturnValue { get; set; } = new();
    public bool IsSuccess { get; set; }
    public object? Errors { get; set; }
    public object? Exception { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? CorrelationId { get; set; }
}

public class SaveKnowledgeReturnValue
{
    public string RefId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public TableMetadata? TableMetadata { get; set; }
    public DmMetadata DmMetadata { get; set; } = new();
    public string? SharepointDeltaLink { get; set; }
    public object? FileMetadata { get; set; }
    public object? LibraryMetadata { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }
}

public class TableMetadata { }
