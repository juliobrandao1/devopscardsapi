using AzureBoardsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class CardsController : ControllerBase
{
    private readonly AzureDevOpsService _service;

    public CardsController(AzureDevOpsService service)
    {
        _service = service;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCard([FromBody] CardRequest request)
    {
        var result = await _service.CreateCard(request.Title, request.AssignedTo, request.IterationPath, request.StoryPoints);
        return Ok(result);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetCards([FromQuery] string iterationPath, [FromQuery] string? assignedTo)
    {
        var resultJson = await _service.GetCards(iterationPath, assignedTo);

        using var doc = JsonDocument.Parse(resultJson);
        var cards = new List<Card>();

        // WIQL retorna IDs dos work items
        if (doc.RootElement.TryGetProperty("workItems", out var workItems))
        {
            foreach (var wi in workItems.EnumerateArray())
            {
                int id = wi.GetProperty("id").GetInt32();

                // Buscar detalhes do Work Item
                var detailResponse = await _service.GetWorkItemDetails(id);
                using var detailDoc = JsonDocument.Parse(detailResponse);

                var fields = detailDoc.RootElement.GetProperty("fields");

                int? storyPoints = null;
                if (fields.TryGetProperty("Microsoft.VSTS.Scheduling.StoryPoints", out var sp))
                {
                    if (sp.ValueKind == JsonValueKind.Number)
                    {
                        // Pode ser int ou double
                        if (sp.TryGetInt32(out var intVal))
                            storyPoints = intVal;
                        else if (sp.TryGetDouble(out var dblVal))
                            storyPoints = (int)dblVal;
                    }
                    else if (sp.ValueKind == JsonValueKind.String)
                    {
                        if (int.TryParse(sp.GetString(), out var parsed))
                            storyPoints = parsed;
                    }
                }

                var card = new Card
                {
                    Id = id,
                    Title = fields.GetProperty("System.Title").GetString(),
                    AssignedTo = fields.TryGetProperty("System.AssignedTo", out var assignedField)
                        ? assignedField.GetProperty("displayName").GetString()
                        : null,
                    IterationPath = fields.GetProperty("System.IterationPath").GetString(),
                    StoryPoints = storyPoints
                };


                cards.Add(card);
            }
        }

        return Ok(cards);
    }

    [HttpGet("iterations")]
    public async Task<IActionResult> GetIterations()
    {
        var result = await _service.GetIterations();
        return Ok(result);
    }
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _service.GetUsers();
        return Ok(result);
    }

    [HttpGet("assigned-users")]
    public async Task<IActionResult> GetAssignedUsers([FromQuery] string iterationPath)
    {
        var result = await _service.GetAssignedUsers(iterationPath);
        return Ok(result);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateCard([FromBody] Card card)
    {
        if (card == null || card.Id <= 0)
            return BadRequest("Card inválido.");

        var result = await _service.UpdateCard(card);
        return Ok(result);
    }

}

public class CardRequest
{
    public string Title { get; set; }
    public string AssignedTo { get; set; }
    public string IterationPath { get; set; }
    public int StoryPoints { get; set; }

}
