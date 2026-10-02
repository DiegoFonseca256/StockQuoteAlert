using StockQuoteAlert;
using System.Text.RegularExpressions;

// Teste da API brapi.dev

// Procura o .env na pasta atual e nas pastas acima (no Visual Studio a pasta atual é bin\Debug\net10.0)
DotNetEnv.Env.TraversePath().Load();

//Se nenhum ticker for passado como argumento, usa alguns ativos de teste
var tickers = args.Length > 0 ? args : new[] { "PETR4", "VALE3", "ITUB4", "MGLU3" };
var token = Environment.GetEnvironmentVariable("BRAPI_API_KEY");

////Verifica se o número de argumentos é correto
//if (args.Length != 3)
//{
//    Console.WriteLine("Erro: Número incorreto de argumentos.");
//    Console.WriteLine("Uso: dotnet run -- <ticker> <preço_minimo> <preço_máximo>");
//    return 1;
//}

////Verifica Ticker
//bool VerificaTicker(string ticker)
//{
//    ticker.Trim().ToUpper();

//    if (!Regex.IsMatch(ticker, @"^[A-Z]{4}\d{1,2}$"))
//    {
//        Console.WriteLine($"'{ticker}' não parece um ticker válido (ex: PETR4, BOVA11).");
//        return false;
//    }
//    else
//    {
//        return true;
//    }
//}

////Verifica se o preço mínimo e máximo são válidos
//if (!decimal.TryParse(args[1], out decimal precoMinimo))
//{
//    Console.WriteLine("Erro: preço mínimo inválido.");
//    return 1;
//}

//if (!decimal.TryParse(args[2], out decimal precoMaximo))
//{
//    Console.WriteLine("Erro: preço máximo inválido.");
//    return 1;
//}



////Função para verificar se o preço está dentro do intervalo especificado
//int IsPriceInRange(decimal price, decimal min, decimal max) {
//    if(price <= min)
//    {
//        Console.WriteLine($"Preço {price} está abaixo do mínimo {min}");
//        return -1; // Abaixo do mínimo
//    }
//    else if (price >= max)
//    {
//        Console.WriteLine($"Preço {price} está acima do máximo {max}");
//        return 1; // Acima do máximo
//    }
//    else
//    {
//        Console.WriteLine($"Preço {price} está dentro do intervalo [{min}, {max}]");
//        return 0; // Dentro do intervalo
//    }

//}




//// Exibe informações sobre o token
//Console.WriteLine(string.IsNullOrWhiteSpace(token)
//    ? "BRAPI_API_KEY não definida: usando acesso sem token (apenas ativos de teste)."
//    : "Usando token da variável BRAPI_API_KEY.");
//Console.WriteLine();


//var client = new BrapiClient(token);
//var falhas = 0;

//foreach (var ticker in tickers)
//{
//    try
//    {
//        var quote = await client.GetQuoteAsync(ticker);
//        if (quote is null)
//        {
//            Console.WriteLine($"[FALHA] {ticker}: nenhuma cotação retornada");
//            falhas++;
//            continue;
//        }

//        quote.Print();
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"[FALHA] {ticker}: {ex.Message}");
//        falhas++;
//    }
//}

var gmail = new Email("smtp.gmail.com",
                      Environment.GetEnvironmentVariable("EMAIL_ADRESS"),
                      Environment.GetEnvironmentVariable("APP_PASSWORD"));

gmail.SendEmail(Environment.GetEnvironmentVariable("EMAIL_ADRESS"), "TESTE C#", "Hello World!");


//Console.WriteLine();
//Console.WriteLine($"{tickers.Length - falhas}/{tickers.Length} consultas bem-sucedidas.");
//return falhas == 0 ? 0 : 1;
return 0;

