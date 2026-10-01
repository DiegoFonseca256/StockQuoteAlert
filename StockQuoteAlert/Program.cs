using StockQuoteAlert;

// Teste da API brapi.dev

DotNetEnv.Env.Load();

//Se nenhum ticker for passado como argumento, usa alguns ativos de teste
var tickers = args.Length > 0 ? args : new[] { "PETR4", "VALE3", "ITUB4", "MGLU3" };
var token = Environment.GetEnvironmentVariable("BRAPI_API_KEY");

// Exibe informações sobre o token
Console.WriteLine(string.IsNullOrWhiteSpace(token)
    ? "BRAPI_API_KEY não definida: usando acesso sem token (apenas ativos de teste)."
    : "Usando token da variável BRAPI_API_KEY.");
Console.WriteLine();


var client = new BrapiClient(token);
var falhas = 0;

foreach (var ticker in tickers)
{
    try
    {
        var quote = await client.GetQuoteAsync(ticker);
        if (quote is null)
        {
            Console.WriteLine($"[FALHA] {ticker}: nenhuma cotação retornada");
            falhas++;
            continue;
        }

        quote.Print();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FALHA] {ticker}: {ex.Message}");
        falhas++;
    }
}

Console.WriteLine();
Console.WriteLine($"{tickers.Length - falhas}/{tickers.Length} consultas bem-sucedidas.");
return falhas == 0 ? 0 : 1;
