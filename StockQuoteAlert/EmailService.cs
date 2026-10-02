using System.Net;
using System.Net.Mail;

namespace StockQuoteAlert;

public class EmailService
{
    private readonly SmtpSettings _smtp;

    // Recebe host, porta, SSL e credenciais lidos do appsettings.json / .env
    public EmailService(SmtpSettings smtp)
    {
        _smtp = smtp ?? throw new ArgumentNullException(nameof(smtp));
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        Console.WriteLine($"Enviando e-mail para {to} com assunto '{subject}'");

        using var message = CreateMessage(to, subject, body);
        await SendViaSmtpAsync(message);
    }

    private MailMessage CreateMessage(string to, string subject, string body)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_smtp.Username),
            Subject = subject,
            Body = body
        };
        message.To.Add(to);
        return message;
    }

    private async Task SendViaSmtpAsync(MailMessage message)
    {
        // "using" fecha a conexão com o servidor mesmo se o envio falhar
        using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.EnableSsl,
            Credentials = new NetworkCredential(_smtp.Username, _smtp.Password)
        };

        await smtpClient.SendMailAsync(message);
    }
}
