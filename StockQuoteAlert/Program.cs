namespace StockQuoteAlert;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Carrega variáveis de ambiente do arquivo .env (segredos: token da brapi e senha do SMTP)
        DotNetEnv.Env.TraversePath().Load();
        var token = Environment.GetEnvironmentVariable("BRAPI_API_KEY");

        // Valida os argumentos: <ticker> <preço_venda> <preço_compra>
        if (!Validacoes.ValidarQuantidadeDeArgumentos(args)) return 1;
        if (!Validacoes.ValidarTicker(args[0], out var ticker)) return 1;
        if (!Validacoes.ValidarPrecos(args[1], args[2], out var precoVenda, out var precoCompra)) return 1;

        // Lê o appsettings.json (e-mail de destino, SMTP e intervalo)
        var config = Configuracao.Carregar();
        if (config is null) return 1;

        // Exibe informações sobre o token
        Console.WriteLine(string.IsNullOrWhiteSpace(token)
            ? "BRAPI_API_KEY não definida: usando acesso sem token (apenas ativos de teste)."
            : "Usando token da variável BRAPI_API_KEY.");
        Console.WriteLine();

        Console.WriteLine($"Monitorando {ticker}: venda acima de {precoVenda}, compra abaixo de {precoCompra}.");

        // Teste de envio de e-mail
        //var gmail = new Email(config.Smtp);
        //await gmail.SendEmail(config.EmailDestino, "TESTE C#", "Hello World!");

        var client = new BrapiClient(token);
        var intervalo = TimeSpan.FromSeconds(config.IntervaloSegundos); // tempo entre consultas

        // Confirma que o ticker existe antes de começar o monitoramento
        if (!await Validacoes.ValidarTickerNaB3(client, ticker)) return 1;

        // Ctrl+C sinaliza o cancelamento em vez de matar o processo na hora
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;  // impede o encerramento imediato
            cts.Cancel();     // avisa o loop para parar
        };

        Console.WriteLine("Pressione Ctrl+C para encerrar.");
        Console.WriteLine();

        while (!cts.IsCancellationRequested)
        {
            try
            {
                var quote = await client.GetQuoteAsync(ticker);

                if (quote is null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [FALHA] {ticker}: nenhuma cotação retornada");
                }
                else
                {
                    Console.Write($"[{DateTime.Now:HH:mm:ss}] {ticker}: ");
                    IsPriceInRange(quote.RegularMarketPrice, precoCompra, precoVenda);
                }
            }
            catch (Exception ex)
            {
                // Falha momentânea (rede, timeout, API fora do ar): registra e tenta na próxima volta
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [FALHA] {ticker}: {ex.Message}");
            }

            try
            {
                await Task.Delay(intervalo, cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;  // Ctrl+C durante a espera
            }
        }

        Console.WriteLine("Monitoramento encerrado.");
        return 0;
    }

    //Função para verificar se o preço está dentro do intervalo especificado
    private static int IsPriceInRange(decimal price, decimal min, decimal max)
    {
        if (price < min)
        {
            Console.WriteLine($"Preço {price} está abaixo do mínimo {min}");
            return -1; // Abaixo do mínimo
        }
        else if (price > max)
        {
            Console.WriteLine($"Preço {price} está acima do máximo {max}");
            return 1; // Acima do máximo
        }
        else
        {
            Console.WriteLine($"Preço {price} está dentro do intervalo [{min}, {max}]");
            return 0; // Dentro do intervalo
        }
    }

    //Função para montar o assunto e o corpo do alerta: resultado 1 = venda, -1 = compra
    private static (string Assunto, string Corpo) MontarAlerta(
        int resultado, string ticker, decimal preco, decimal precoVenda, decimal precoCompra)
    {
        return resultado == 1
            ? ($"[VENDA] {ticker} a R$ {preco:F2}",
               $"A cotação de {ticker} está em R$ {preco:F2}, acima do preço de referência para venda (R$ {precoVenda:F2}).\n" +
               $"Recomendação: VENDER.\n\nHorário: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            : ($"[COMPRA] {ticker} a R$ {preco:F2}",
               $"A cotação de {ticker} está em R$ {preco:F2}, abaixo do preço de referência para compra (R$ {precoCompra:F2}).\n" +
               $"Recomendação: COMPRAR.\n\nHorário: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
    }
}
