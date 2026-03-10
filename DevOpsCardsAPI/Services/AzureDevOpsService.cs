using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class AzureDevOpsService
{
    private readonly HttpClient _client;
    private readonly string _orgUrl;
    private readonly string _project;

    public AzureDevOpsService(IConfiguration config)
    {
        _orgUrl = config["AzureDevOps:OrganizationUrl"];
        _project = config["AzureDevOps:Project"];
        string pat = config["AzureDevOps:Pat"];

        _client = new HttpClient();
        _client.BaseAddress = new Uri(_orgUrl);
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", authToken);
    }

    // Criar card
    public async Task<string> CreateCard(string title, string assignedTo, string iterationPath)
    {
        var jsonContent = $@"
        [
          {{ ""op"": ""add"", ""path"": ""/fields/System.Title"", ""value"": ""{title}"" }},
          {{ ""op"": ""add"", ""path"": ""/fields/System.AssignedTo"", ""value"": ""{assignedTo}"" }},
          {{ ""op"": ""add"", ""path"": ""/fields/System.IterationPath"", ""value"": ""{iterationPath}"" }}
        ]";

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json-patch+json");
        var response = await _client.PostAsync(
            $"{_orgUrl}/{_project}/_apis/wit/workitems/$User Story?api-version=7.0", content);

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetCards(string iterationPath, string assignedTo = null)
    {
        var filterAssigned = string.IsNullOrEmpty(assignedTo)
            ? ""
            : $"AND [System.AssignedTo] = '{assignedTo}'";

        // filtro fixo de workspace
        var workspace = "Agile Delivery\\\\Workspace - Savannah27\\\\Workspace - Savannah27 (Credenciamento)";

        var wiql = $@"
{{
  ""query"": ""SELECT [System.Id], [System.Title], [System.AssignedTo], [System.IterationPath], [Microsoft.VSTS.Scheduling.StoryPoints]
              FROM WorkItems
              WHERE [System.WorkItemType] = 'User Story'
              AND [System.IterationPath] = '{iterationPath}'
              AND [System.AreaPath] = '{workspace}'
              {filterAssigned}""
}}";

        var content = new StringContent(wiql, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(
            $"{_orgUrl}/{_project}/_apis/wit/wiql?api-version=7.0", content);

        return await response.Content.ReadAsStringAsync();
    }


    public async Task<string> GetWorkItemDetails(int id)
    {
        var response = await _client.GetAsync(
            $"{_orgUrl}/{_project}/_apis/wit/workitems/{id}?api-version=7.0");

        return await response.Content.ReadAsStringAsync();
    }
    public async Task<List<string>> GetIterations()
    {
        var response = await _client.GetAsync(
            $"{_orgUrl}/{_project}/_apis/wit/classificationnodes/iterations?$depth=3&api-version=7.0");

        var json = await response.Content.ReadAsStringAsync();
        var iterations = new List<string>();

        using var doc = JsonDocument.Parse(json);
        ExtractIterations(doc.RootElement, iterations);

        return iterations;
    }

    private void ExtractIterations(JsonElement element, List<string> iterations)
    {
        if (element.TryGetProperty("path", out var pathProp))
        {
            var rawPath = pathProp.GetString();
            var formatted = rawPath.Replace(@"\Iteration\", @"\\").TrimStart('\\');
            iterations.Add(formatted);         
        }

        if (element.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                ExtractIterations(child, iterations);
            }
        }
    }

    public async Task<List<string>> GetUsers()
    {
        var response = await _client.GetAsync(
            $"{_orgUrl}/_apis/graph/users?api-version=7.0-preview.1");

        var json = await response.Content.ReadAsStringAsync();
        var users = new List<string>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("value", out var values))
        {
            foreach (var user in values.EnumerateArray())
            {
                if (user.TryGetProperty("principalName", out var principal))
                {
                    users.Add(principal.GetString());
                }
            }
        }

        return users;
    }

    public async Task<List<string>> GetAssignedUsers(string iterationPath)
    {
        var resultJson = await GetCards(iterationPath); // usa o WIQL já existente
        var users = new HashSet<string>();

        using var doc = JsonDocument.Parse(resultJson);
        if (doc.RootElement.TryGetProperty("workItems", out var workItems))
        {
            foreach (var wi in workItems.EnumerateArray())
            {
                int id = wi.GetProperty("id").GetInt32();
                var detailResponse = await GetWorkItemDetails(id);
                using var detailDoc = JsonDocument.Parse(detailResponse);

                var fields = detailDoc.RootElement.GetProperty("fields");

                if (fields.TryGetProperty("System.AssignedTo", out var assignedField))
                {
                    // AssignedTo é objeto → pegar displayName
                    if (assignedField.TryGetProperty("displayName", out var displayName))
                    {
                        users.Add(displayName.GetString());
                    }
                }
            }
        }

        return users.ToList();
    }


}
