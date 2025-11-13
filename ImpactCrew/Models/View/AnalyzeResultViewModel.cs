using ImpactCrew.Models.Api.Agent;

namespace ImpactCrew.Models.View;

public class AnalyzeResultViewModel
{
    public string SessionId { get; set; } = string.Empty;
    public AgentInvokeResponse? AgentResponse { get; set; }
    public List<string> GeneratedFilePaths { get; set; } = new();
    public string? Error { get; set; }
}
