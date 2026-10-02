using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StockQuoteAlert;

// Modelo da resposta da brapi (os nomes espelham o JSON da API)
public class Quote
{
    public string Symbol { get; set; } = "";
    public string? ShortName { get; set; }
    public decimal RegularMarketPrice { get; set; }
    public decimal RegularMarketChangePercent { get; set; }
    public string? Currency { get; set; }
}

public class QuoteResponse
{
    public Quote[]? Results { get; set; }
}

public class BrapiClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://brapi.dev/api";

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
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Erro {(int)response.StatusCode} ao consultar {ticker}: {body}");
        }

        // Lê e converte o JSON de uma vez (já ignora maiúsculas/minúsculas nos nomes)
        var data = await response.Content.ReadFromJsonAsync<QuoteResponse>();
        return data?.Results?.FirstOrDefault();
    }
}
