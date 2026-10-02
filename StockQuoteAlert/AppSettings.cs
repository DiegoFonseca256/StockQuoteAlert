using Microsoft.Extensions.Configuration;

namespace StockQuoteAlert;

// Dados de acesso ao servidor SMTP (seção "Smtp" do appsettings.json)
public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AppSettings
{
    private const string FileName = "appsettings.json";

    public string RecipientEmail { get; set; } = "";
    public int IntervalSeconds { get; set; } = 60;
    public SmtpSettings Smtp { get; set; } = new();

    // Lê o appsettings.json da pasta do executável.
    // Variáveis de ambiente (ex: Smtp__Password, vinda do .env) sobrescrevem os valores do arquivo.
    public static AppSettings? Load()
    {
        var directory = AppContext.BaseDirectory;

        if (!File.Exists(Path.Combine(directory, FileName)))
        {
            Console.WriteLine($"Erro: arquivo '{FileName}' não encontrado em '{directory}'.");
            Console.WriteLine("Copie o 'appsettings.example.json' para 'appsettings.json' e preencha os dados.");
            return null;
        }

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile(FileName, optional: false)
                .AddEnvironmentVariables()
                .Build();

            return configuration.Get<AppSettings>() ?? new AppSettings();
        }
        catch (Exception ex)
        {
            // JSON mal formatado ou valor com tipo errado (ex: "Port": "abc")
            Console.WriteLine($"Erro ao ler '{FileName}': {ex.Message}");
            return null;
        }
    }
}
