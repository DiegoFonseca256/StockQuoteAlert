using Microsoft.Extensions.Configuration;

namespace StockQuoteAlert;

// Dados de acesso ao servidor SMTP (seção "Smtp" do appsettings.json)
public class ConfiguracaoSmtp
{
    public string Host { get; set; } = "";
    public int Porta { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Usuario { get; set; } = "";
    public string Senha { get; set; } = "";
}

public class Configuracao
{
    private const string NomeArquivo = "appsettings.json";

    public string EmailDestino { get; set; } = "";
    public int IntervaloSegundos { get; set; } = 60;
    public ConfiguracaoSmtp Smtp { get; set; } = new();

    // Lê o appsettings.json da pasta do executável.
    // Variáveis de ambiente (ex: Smtp__Senha, vinda do .env) sobrescrevem os valores do arquivo.
    public static Configuracao? Carregar()
    {
        var pasta = AppContext.BaseDirectory;

        if (!File.Exists(Path.Combine(pasta, NomeArquivo)))
        {
            Console.WriteLine($"Erro: arquivo '{NomeArquivo}' não encontrado em '{pasta}'.");
            Console.WriteLine("Copie o 'appsettings.example.json' para 'appsettings.json' e preencha os dados.");
            return null;
        }

        try
        {
            var configuracao = new ConfigurationBuilder()
                .SetBasePath(pasta)
                .AddJsonFile(NomeArquivo, optional: false)
                .AddEnvironmentVariables()
                .Build();

            return configuracao.Get<Configuracao>() ?? new Configuracao();
        }
        catch (Exception ex)
        {
            // JSON mal formatado ou valor com tipo errado (ex: "Porta": "abc")
            Console.WriteLine($"Erro ao ler '{NomeArquivo}': {ex.Message}");
            return null;
        }
    }
}
