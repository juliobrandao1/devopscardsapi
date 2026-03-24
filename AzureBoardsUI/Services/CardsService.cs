using System.Net.Http;
using System.Net.Http.Json;

public class CardsService
{
    private readonly HttpClient _http;

    public CardsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Card>> GetCards(string iterationPath, string assignedTo = null)
    {
        var url = $"https://localhost:7051/api/cards/list?iterationPath={iterationPath}";

        if (!string.IsNullOrEmpty(assignedTo))
            url += $"&assignedTo={assignedTo}";      

        var result = await _http.GetFromJsonAsync<List<Card>>(url);
        return result ?? new List<Card>();
    }

    public async Task<string> CreateCard(Card card)
    {
        var response = await _http.PostAsJsonAsync("https://localhost:7051/api/cards/create", card);
        return await response.Content.ReadAsStringAsync();
    }
    public async Task<List<string>> GetIterations()
    {
        var result = await _http.GetFromJsonAsync<List<string>>("https://localhost:7051/api/cards/iterations");
        return result ?? new List<string>();
    }
    public async Task<List<string>> GetUsers()
    {
        var result = await _http.GetFromJsonAsync<List<string>>("https://localhost:7051/api/cards/users");
        return result ?? new List<string>();
    }

    public async Task<List<string>> GetAssignedUsers(string iterationPath)
    {
        var result = await _http.GetFromJsonAsync<List<string>>(
            $"https://localhost:7051/api/cards/assigned-users?iterationPath={iterationPath}");
        return result ?? new List<string>();
    }
    public async Task DeleteCard(int id)
    {
        await _http.DeleteAsync($"https://localhost:7051/api/cards/delete/{id}");
    }

    public async Task<string> UpdateCard(Card card)
    {
        var response = await _http.PutAsJsonAsync("https://localhost:7051/api/cards/update", card);
        return await response.Content.ReadAsStringAsync();
    }

}
