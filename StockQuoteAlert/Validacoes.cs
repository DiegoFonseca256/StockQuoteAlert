using System.Globalization;
using System.Text.RegularExpressions;

namespace StockQuoteAlert;

public static class Validacoes
{
    // Verifica se foram passados exatamente 3 argumentos
    public static bool ValidarQuantidadeDeArgumentos(string[] argumentos)
    {
        if (argumentos.Length == 3)
            return true;

        Console.WriteLine("Erro: número incorreto de argumentos.");
        Console.WriteLine("Uso: dotnet run -- <ticker> <preço_venda> <preço_compra>");
        Console.WriteLine("Ex:  dotnet run -- PETR4 22.67 22.59");
        return false;
    }

    // Normaliza o ticker (ex: " petr4" -> "PETR4") e verifica o formato
    public static bool ValidarTicker(string texto, out string ticker)
    {
        ticker = texto.Trim().ToUpperInvariant();

        if (Regex.IsMatch(ticker, @"^[A-Z]{4}\d{1,2}$"))
            return true;

        Console.WriteLine($"Erro: '{texto}' não parece um ticker válido (ex: PETR4, BOVA11).");
        return false;
    }

    // Converte os dois preços e verifica se a compra é menor que a venda
    public static bool ValidarPrecos(string textoVenda, string textoCompra, out decimal precoVenda, out decimal precoCompra)
    {
        precoCompra = 0;

        if (!TryParsePreco(textoVenda, out precoVenda))
        {
            Console.WriteLine($"Erro: preço de venda inválido: '{textoVenda}'.");
            return false;
        }

        if (!TryParsePreco(textoCompra, out precoCompra))
        {
            Console.WriteLine($"Erro: preço de compra inválido: '{textoCompra}'.");
            return false;
        }

        if (precoCompra >= precoVenda)
        {
            Console.WriteLine("Erro: o preço de compra deve ser menor que o preço de venda.");
            return false;
        }

        return true;
    }

    // Converte o texto em preço, aceitando "22.67" ou "22,67"
    private static bool TryParsePreco(string texto, out decimal preco) =>
        decimal.TryParse(texto.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out preco)
        && preco > 0;
}
