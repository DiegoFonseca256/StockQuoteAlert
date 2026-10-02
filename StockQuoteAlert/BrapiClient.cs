using System.Net.Http.Headers;
using System.Text.Json;

namespace StockQuoteAlert;

public class Quote
{
    public string Symbol { get; set; } = "";    
    public string? ShortName { get; set; }
    public decimal RegularMarketPrice { get; set; }
    public decimal RegularMarketChangePercent { get; set; }
    public string? Currency { get; set; }

    public void Print(){
        Console.WriteLine($"{Symbol} ({ShortName}) - {RegularMarketPrice} {Currency} ({RegularMarketChangePercent:+0.00;-0.00}%)");
    }
}

public class QuoteResponse
{
    public Quote[]? Results { get; set; }
}

public class BrapiClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://brapi.dev/api";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BrapiClient(string? token)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Sem token, a brapi só libera alguns ativos de teste (PETR4, MGLU3, VALE3, ITUB4)
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<Quote?> GetQuoteAsync(string ticker)
    {
        var url = $"{BaseUrl}/quote/{Uri.EscapeDataString(ticker)}";
        var response = await _httpClient.GetAsync(url);

        // Ticker inexistente: a brapi responde 404
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Erro {(int)response.StatusCode} ao consultar {ticker}: {body}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<QuoteResponse>(json, JsonOptions);
        return data?.Results?.FirstOrDefault();
    }
}
