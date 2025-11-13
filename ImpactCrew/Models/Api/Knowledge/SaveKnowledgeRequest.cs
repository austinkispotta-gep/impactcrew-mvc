namespace ImpactCrew.Models.Api.Knowledge;

public class SaveKnowledgeRequest
{
    public List<KnowledgeItem> KnowledgeItems { get; set; } = new();
    public string AgentId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string BpcCode { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
}

public class KnowledgeItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "table-file"; // fixed for excel in this scenario
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "loading";
    public string Url { get; set; } = string.Empty;
    public string IsExcelFile { get; set; } = "true";
    public DmMetadata DmMetadata { get; set; } = new();
}

public class DmMetadata
{
    public string AppId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
}
