namespace ImpactCrew.Models.Api.Agent;

public class AgentInvokeRequest
{
    public string Bpc { get; set; } = string.Empty; // bpcCode
    public string Environment { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AgentInvokeOptions Options { get; set; } = new();
}

public class AgentInvokeOptions
{
    public bool EnableDebug { get; set; } = true;
    public string SessionId { get; set; } = string.Empty;
    public bool ReturnOnlyCurrentMessages { get; set; } = true;
    public string RuntimeToken { get; set; } = string.Empty;
}
