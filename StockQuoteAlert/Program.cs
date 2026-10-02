namespace StockQuoteAlert;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        //------------------------------------------------Carregamento-------------------------------------------------------------------
        // Carrega variáveis de ambiente do arquivo .env (segredos: token da brapi e senha do SMTP)
        DotNetEnv.Env.TraversePath().Load();
        var token = Environment.GetEnvironmentVariable("BRAPI_API_KEY");
        // Lê o appsettings.json (e-mail de destino, SMTP e intervalo)
        var settings = AppSettings.Load();
        if (settings is null) return 1;
        // Cria o serviço de envio de e-mails
        var emailService = new EmailService(settings.Smtp);
        int? lastResult = null;
        // Cria o cliente da brapi
        var client = new BrapiClient(token);
        var interval = TimeSpan.FromSeconds(settings.IntervalSeconds); // tempo entre consultas
        //-------------------------------------------------------------------------------------------------------------------------------

        //------------------------------------------------Validação----------------------------------------------------------------------
        // Valida os argumentos: <ticker> <preço_venda> <preço_compra>
        var error = Validations.ValidateArgumentCount(args);
        if (error is not null) return Fail(error);

        error = Validations.ValidateTicker(args[0], out var ticker);
        if (error is not null) return Fail(error);

        error = Validations.ValidatePrices(args[1], args[2], out var sellPrice, out var buyPrice);
        if (error is not null) return Fail(error);

        // Confirma que o ticker existe antes de começar o monitoramento
        error = await Validations.ValidateTickerExistsAsync(client, ticker);
        if (error is not null) return Fail(error);

        // Exibe informações sobre o token
        Console.WriteLine(string.IsNullOrWhiteSpace(token)
            ? "BRAPI_API_KEY não definida: usando acesso sem token (apenas ativos de teste)."
            : "Usando token da variável BRAPI_API_KEY.");
        Console.WriteLine();

        Console.WriteLine($"Monitorando {ticker}: venda acima de {sellPrice}, compra abaixo de {buyPrice}.");
        //---------------------------------------------------------------------------------------------------------------------------------

        //------------------------------------------------Monitoramento--------------------------------------------------------------------
        // Ctrl+C sinaliza o cancelamento em vez de matar o processo na hora
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;  // impede o encerramento imediato
            cts.Cancel();     // avisa o loop para parar
        };

        Console.WriteLine("Pressione Ctrl+C para encerrar.");
        Console.WriteLine();

        var isFirstCheck = true;  // a primeira consulta acontece mesmo com o mercado fechado

        while (!cts.IsCancellationRequested)
        {
            // Fora do horário da B3: dorme até a próxima abertura em vez de consultar a API
            var now = MarketHours.NowInBrasilia();
            if (!isFirstCheck && !MarketHours.IsOpen(now))
            {
                var nextOpening = MarketHours.NextOpening(now);
                Console.WriteLine($"[{now:HH:mm:ss}] Mercado fechado. Próxima abertura: {nextOpening:dd/MM HH:mm}");

                try
                {
                    await Task.Delay(nextOpening - now, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;  // Ctrl+C durante a espera
                }
                continue;
            }
            isFirstCheck = false;

            try
            {
                var quote = await client.GetQuoteAsync(ticker);

                if (quote is null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [FALHA] {ticker}: nenhuma cotação retornada");
                }
                else
                {
                    var price = quote.RegularMarketPrice;
                    var result = IsPriceInRange(price, buyPrice, sellPrice);

                    var status = result switch
                    {
                        -1 => $"Preço {price} está abaixo do mínimo {buyPrice}",
                        1 => $"Preço {price} está acima do máximo {sellPrice}",
                        _ => $"Preço {price} está dentro do intervalo [{buyPrice}, {sellPrice}]"
                    };
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {ticker}: {status}");

                    if (result == 0)
                    {
                        lastResult = 0;  // voltou para a faixa: o próximo rompimento gera alerta de novo
                    }
                    else if (result != lastResult)
                    {
                        var (subject, body) = BuildAlert(result, ticker, price, sellPrice, buyPrice);

                        try
                        {
                            await emailService.SendEmailAsync(settings.RecipientEmail, subject, body);
                            lastResult = result;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [FALHA] Envio do e-mail: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Falha momentânea (rede, timeout, API fora do ar): registra e tenta na próxima volta
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [FALHA] {ticker}: {ex.Message}");
            }

            // Mercado fechado: volta direto para o topo, que espera até a abertura
            if (!MarketHours.IsOpen(MarketHours.NowInBrasilia()))
                continue;

            try
            {
                await Task.Delay(interval, cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;  // Ctrl+C durante a espera
            }
        }

        Console.WriteLine("Monitoramento encerrado.");
        return 0;
    }
    //---------------------------------------------------------------------------------------------------------------------------------

    //------------------------------------------------Funções auxiliares----------------------------------------------------------------------

    // Exibe o erro de validação e devolve o código de saída do programa
    private static int Fail(string message)
    {
        Console.WriteLine($"Erro: {message}");
        return 1;
    }

    //Função para verificar se o preço está dentro do intervalo: -1 = abaixo, 0 = dentro, 1 = acima
    private static int IsPriceInRange(decimal price, decimal min, decimal max)
    {
        if (price < min) return -1; // Abaixo do mínimo
        if (price > max) return 1;  // Acima do máximo
        return 0;                   // Dentro do intervalo
    }

    //Função para montar o assunto e o corpo do alerta: result 1 = venda, -1 = compra
    private static (string Subject, string Body) BuildAlert(
        int result, string ticker, decimal price, decimal sellPrice, decimal buyPrice)
    {
        return result == 1
            ? ($"[VENDA] {ticker} a R$ {price:F2}",
               $"A cotação de {ticker} está em R$ {price:F2}, acima do preço de referência para venda (R$ {sellPrice:F2}).\n" +
               $"Recomendação: VENDER.\n\nHorário: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            : ($"[COMPRA] {ticker} a R$ {price:F2}",
               $"A cotação de {ticker} está em R$ {price:F2}, abaixo do preço de referência para compra (R$ {buyPrice:F2}).\n" +
               $"Recomendação: COMPRAR.\n\nHorário: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
    }
}
//---------------------------------------------------------------------------------------------------------------------------------