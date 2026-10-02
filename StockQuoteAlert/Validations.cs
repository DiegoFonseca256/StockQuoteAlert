using System.Globalization;
using System.Text.RegularExpressions;

namespace StockQuoteAlert;

// Cada validação retorna null quando está tudo certo, ou a mensagem de erro.
// As funções não escrevem no console: quem chama decide como exibir o erro.
public static class Validations
{
    // Verifica se foram passados exatamente 3 argumentos
    public static string? ValidateArgumentCount(string[] args)
    {
        if (args.Length == 3)
            return null;

        return "número incorreto de argumentos.\n" +
               "Uso: dotnet run -- <ticker> <preço_venda> <preço_compra>\n" +
               "Ex:  dotnet run -- PETR4 22.67 22.59";
    }

    // Normaliza o ticker (ex: " petr4" -> "PETR4") e verifica o formato
    public static string? ValidateTicker(string text, out string ticker)
    {
        ticker = text.Trim().ToUpperInvariant();

        if (Regex.IsMatch(ticker, @"^[A-Z]{4}\d{1,2}$"))
            return null;

        return $"'{text}' não parece um ticker válido (ex: PETR4, BOVA11).";
    }

    // Consulta a brapi para confirmar que o ticker existe na B3
    public static async Task<string?> ValidateTickerExistsAsync(BrapiClient client, string ticker)
    {
        try
        {
            if (await client.GetQuoteAsync(ticker) is not null)
                return null;

            return $"ticker '{ticker}' não encontrado na B3.";
        }
        catch (Exception ex)
        {
            return $"falha ao consultar {ticker}: {ex.Message}";
        }
    }

    // Converte os dois preços e verifica se a compra é menor que a venda
    public static string? ValidatePrices(string sellText, string buyText, out decimal sellPrice, out decimal buyPrice)
    {
        buyPrice = 0;

        if (!TryParsePrice(sellText, out sellPrice))
            return $"preço de venda inválido: '{sellText}'.";

        if (!TryParsePrice(buyText, out buyPrice))
            return $"preço de compra inválido: '{buyText}'.";

        if (buyPrice >= sellPrice)
            return "o preço de compra deve ser menor que o preço de venda.";

        return null;
    }

    // Converte o texto em preço, aceitando "22.67" ou "22,67"
    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price)
        && price > 0;
}
