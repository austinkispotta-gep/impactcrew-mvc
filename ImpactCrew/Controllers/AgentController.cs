using ImpactCrew.Models.Api.Agent;
using ImpactCrew.Models.Api.Attachment;
using ImpactCrew.Models.Api.Knowledge;
using ImpactCrew.Models.Api.Ocr;
using ImpactCrew.Models.View;
using Microsoft.AspNetCore.Mvc;

namespace ImpactCrew.Controllers;

[Route("agent")]
public class AgentController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgentController> _logger;

    // Static config (could be moved to appsettings.json)
    private const string AgentId = "afcbc615-bec0-457a-99c8-d4ab8dd9bb06";
    private const string ModuleId = "894de768-1ac3-48a9-9fd0-ba61fa37dbee";
    private const string Version = "ca06e578-2049-45f5-9854-019a0f4c833a";
    private const string EnvironmentCode = "PROD";
    private const string BpcCode = "70022485";
    private const string AppId = "e913cd2e-4834-41dd-a7b6-33c0cbcd8235";
    private const string ModelCode = "gpt-4.1";
    private const string SubscriptionKey = "1cbd622fdcfd4753b4b43be776fe8c3f"; // sample

    public AgentController(IHttpClientFactory httpClientFactory, ILogger<AgentController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private void ApplyCommonHeaders(HttpClient client, string bearer)
    {
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("accept", "application/json, text/plain, */*");
        client.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9");
        client.DefaultRequestHeaders.Add("authorization", $"Bearer {bearer}");
        client.DefaultRequestHeaders.Add("ocp-apim-subscription-key", SubscriptionKey);
        client.DefaultRequestHeaders.Add("x-gep-transaction-scope-id", Guid.NewGuid().ToString());
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromForm] AnalyzeRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest("Authorization token is required.");
            if (dto.PoFile == null || dto.GrnFile == null || (dto.InvoiceFile == null && (dto.InvoicePdfs == null || dto.InvoicePdfs.Count == 0)))
            {
                return BadRequest("All required files must be uploaded.");
            }

            dto.Token = ParseToken(dto.Token);
            await DeleteExistingKnowledge(dto.Token);

            AgentInvokeResponse? invokeResponse = dto.InvoiceFile != null
                ? await ProcessExcelInvoice(dto, dto.CustomMessage, dto.Token)
                : await ProcessPdfInvoices(dto, dto.CustomMessage, dto.Token);

            var generatedFilePaths = ExtractFilePaths(invokeResponse);

            dto.MessageId++;

            var vm = new AnalyzeResultViewModel
            {
                SessionId = dto.SessionId,
                AgentResponse = invokeResponse,
                GeneratedFilePaths = generatedFilePaths
            };

            return Ok(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analyze failed");
            return StatusCode(500, ex.Message);
        }
    }

    private string ParseToken(string token) => token.Replace("Bearer", "", StringComparison.OrdinalIgnoreCase).Trim();

    [HttpGet("download-export")]
    public async Task<IActionResult> DownloadExport([FromQuery] string filePath, [FromQuery] string token, [FromQuery] string environment = EnvironmentCode)
    {
        try
        {
            token = ParseToken(token);
            if (string.IsNullOrWhiteSpace(token)) return BadRequest("Authorization token is required.");
            var client = _httpClientFactory.CreateClient();
            ApplyCommonHeaders(client, token);
            var requestUrl = $"https://api-build.gep.com/leo-portal-agentic-runtime-api/api/v1/Attachment/Download-Export?filePath={filePath}&environment={environment}";
            using var response = await client.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            var stream = await response.Content.ReadAsByteArrayAsync();
            return File(stream, "application/octet-stream", Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download export failed");
            return StatusCode(500, ex.Message);
        }
    }

    private List<string> ExtractFilePaths(AgentInvokeResponse? resp)
    {
        var list = new List<string>();
        if (resp?.ReturnValue?.Messages == null) return list;
        foreach (var msg in resp.ReturnValue.Messages)
        {
            if (msg.Files == null) continue;
            foreach (var f in msg.Files)
            {
                if (!string.IsNullOrWhiteSpace(f.FilePath)) list.Add(f.FilePath); else if (!string.IsNullOrWhiteSpace(f.Id)) list.Add(f.Id);
            }
        }
        return list.Distinct().ToList();
    }

    private async Task<AgentInvokeResponse?> ProcessExcelInvoice(AnalyzeRequestDto dto, string? customMessage, string bearer)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomMessage))
        {
            _ = await UploadKnowledgeFile(dto.PoFile!, "PO data.xlsx", "PO file", bearer, dto.SessionId);
            _ = await UploadKnowledgeFile(dto.GrnFile!, "GRN data.xlsx", "GRN file", bearer, dto.SessionId);
            _ = await UploadKnowledgeFile(dto.InvoiceFile!, "Invoice data.xlsx", "Invoice file", bearer, dto.SessionId);
        }
        var message = string.IsNullOrWhiteSpace(customMessage) ? "Analyze and generate excel report" : customMessage;
        return await InvokeAgent(dto.SessionId, message, bearer);
    }

    private async Task<AgentInvokeResponse?> ProcessPdfInvoices(AnalyzeRequestDto dto, string? customMessage, string bearer)
    {
        var invoiceTextBuilder = new System.Text.StringBuilder();
        if (dto.InvoicePdfs != null)
        {
            foreach (var pdf in dto.InvoicePdfs)
            {
                var ocr = await OcrPdf(pdf);
                invoiceTextBuilder.AppendLine(ocr?.Text);
            }
        }
        _ = await UploadKnowledgeFile(dto.PoFile!, "PO data.xlsx", "PO file", bearer, dto.SessionId);
        _ = await UploadKnowledgeFile(dto.GrnFile!, "GRN data.xlsx", "GRN file", bearer, dto.SessionId);
        var baseMessage = string.IsNullOrWhiteSpace(customMessage)
            ? $"Analyze and generate excel report. Invoice data: {invoiceTextBuilder}" : $"Analyze the data and await further instruction. Invoice data: {invoiceTextBuilder}\nUser question: {customMessage}";
        return await InvokeAgent(dto.SessionId, baseMessage, bearer);
    }

    private async Task<OcrResponse?> OcrPdf(IFormFile pdfFile)
    {
        var client = _httpClientFactory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var ms = new MemoryStream();
        await pdfFile.CopyToAsync(ms); ms.Position = 0;
        form.Add(new StreamContent(ms), "file", pdfFile.FileName);
        using var response = await client.PostAsync("https://impactcrew-ocr-gvc0bghsaqgabgg8.centralindia-01.azurewebsites.net/extract-text", form);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<OcrResponse>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task<string?> UploadKnowledgeFile(IFormFile file, string renamedFileName, string description, string bearer, string sessionId)
    {
        //var saveReq = new SaveKnowledgeRequest
        //{
        //    AgentId = AgentId,
        //    ModuleId = ModuleId,
        //    Version = Version,
        //    Environment = EnvironmentCode,
        //    BpcCode = BpcCode,
        //    AppId = AppId,
        //    KnowledgeItems = new List<KnowledgeItem> { new KnowledgeItem { Name = renamedFileName, Description = description } }
        //};
        //var client = _httpClientFactory.CreateClient();
        //ApplyCommonHeaders(client, bearer);
        //var saveJson = System.Text.Json.JsonSerializer.Serialize(saveReq);
        //using var saveResponse = await client.PostAsync("https://api-build.gep.com/leo-portal-aicomponent-api/api/v1/Knowledge/SaveKnowledge", new StringContent(saveJson, System.Text.Encoding.UTF8, "application/json"));
        //if (!saveResponse.IsSuccessStatusCode) return null;
        //var saveRaw = await saveResponse.Content.ReadAsStringAsync();
        //var saveObj = System.Text.Json.JsonSerializer.Deserialize<SaveKnowledgeResponse>(saveRaw, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        //var refId = saveObj?.ReturnValue.FirstOrDefault()?.RefId; if (refId == null) return null;

        var uploadClient = _httpClientFactory.CreateClient();
        ApplyCommonHeaders(uploadClient, bearer);
        using var form = new MultipartFormDataContent();
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms); ms.Position = 0;
        form.Add(new StreamContent(ms), "File", renamedFileName);
        form.Add(new StringContent("refId"), "RefId"); // changed to RefId
        form.Add(new StringContent(AgentId), "AgentId");
        form.Add(new StringContent("crate"), "dbType");
        form.Add(new StringContent("application"), "Type"); // changed to application
        //form.Add(new StringContent(description), "Description");
        form.Add(new StringContent(ModuleId), "ModuleId");
        form.Add(new StringContent(Version), "Version");
        form.Add(new StringContent(BpcCode), "BpcCode");
        form.Add(new StringContent(EnvironmentCode), "Environment");
        form.Add(new StringContent(ModelCode), "LanguageModelCode");
        form.Add(new StringContent(AppId), "AppId");
        //form.Add(new StringContent("false"), "useAzureDocIntelligence");
        form.Add(new StringContent(sessionId), "SessionId");
        using var uploadResponse = await uploadClient.PostAsync("https://api-build.gep.com/leo-portal-aicomponent-api/api/v1/Attachment/Upload", form);
        if (!uploadResponse.IsSuccessStatusCode) return null;
        return "refId"; // changed
    }

    private async Task<AgentInvokeResponse?> InvokeAgent(string sessionId, string message, string bearer)
    {
        var invokeReq = new AgentInvokeRequest
        {
            Bpc = BpcCode,
            Environment = EnvironmentCode,
            Version = Version,
            Message = message,
            Options = new AgentInvokeOptions { EnableDebug = true, SessionId = sessionId, ReturnOnlyCurrentMessages = true, RuntimeToken = string.Empty }
        };
        var client = _httpClientFactory.CreateClient();
        ApplyCommonHeaders(client, bearer);
        var json = System.Text.Json.JsonSerializer.Serialize(invokeReq);
        using var response = await client.PostAsync($"https://api-build.gep.com/leo-portal-agentic-runtime-api/api/v1/agents/{AgentId}/invoke", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<AgentInvokeResponse>(raw, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task DeleteExistingKnowledge(string bearer)
    {
        var metadata = await GetAgentMetadata(bearer);
        if (metadata?.ReturnValue?.Knowledge == null) return;
        foreach (var k in metadata.ReturnValue.Knowledge)
        {
            if (k.Name == "PO data.xlsx" || k.Name == "GRN data.xlsx" || k.Name == "Invoice data.xlsx")
            {
                var delReq = new DeleteKnowledgeRequest
                {
                    ModuleId = ModuleId,
                    Version = Version,
                    BpcCode = BpcCode,
                    Environment = EnvironmentCode,
                    AppId = AppId,
                    AgentId = AgentId,
                    RefId = k.RefId
                };
                var client = _httpClientFactory.CreateClient();
                ApplyCommonHeaders(client, bearer);
                var json = System.Text.Json.JsonSerializer.Serialize(delReq);
                var resp = await client.PostAsync("https://api-build.gep.com/leo-portal-aicomponent-api/api/v1/Knowledge/DeleteKnowledgeById", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
            }
        }
    }

    private async Task<GetAgentByIdResponse?> GetAgentMetadata(string bearer)
    {
        var client = _httpClientFactory.CreateClient();
        ApplyCommonHeaders(client, bearer);
        var url = $"https://api-build.gep.com/leo-portal-aicomponent-api/api/v1/Agent/GetModuleAgentById?moduleId={ModuleId}&version={Version}&agentId={AgentId}";
        using var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<GetAgentByIdResponse>(raw, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

public class AnalyzeRequestDto
{
    public IFormFile? PoFile { get; set; }
    public IFormFile? GrnFile { get; set; }
    public IFormFile? InvoiceFile { get; set; }
    public List<IFormFile>? InvoicePdfs { get; set; }
    public int MessageId { get; set; } = 1;
    public string SessionId { get; set; } = string.Empty;
    public string? CustomMessage { get; set; }
    public string Token { get; set; } = string.Empty; // user provided bearer token
}
