using System.Net;
using System.Net.Mail;

namespace StockQuoteAlert;

public class Email
{
    private readonly ConfiguracaoSmtp _smtp;

    // Recebe host, porta, SSL e credenciais lidos do appsettings.json / .env
    public Email(ConfiguracaoSmtp smtp)
    {
        _smtp = smtp ?? throw new ArgumentNullException(nameof(smtp));
    }

    public async Task SendEmail(string emailTo, string subject, string body)
    {
        Console.WriteLine($"Enviando e-mail para {emailTo} com assunto '{subject}'");

        using var message = PrepareEmail(emailTo, subject, body);
        await SendEmailBySmtp(message);
    }

    private MailMessage PrepareEmail(string emailTo, string subject, string body)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(_smtp.Usuario),
            Subject = subject,
            Body = body
        };
        mail.To.Add(emailTo);
        return mail;
    }

    private async Task SendEmailBySmtp(MailMessage mail)
    {
        // "using" fecha a conexão com o servidor mesmo se o envio falhar
        using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Porta)
        {
            EnableSsl = _smtp.UsarSsl,
            Credentials = new NetworkCredential(_smtp.Usuario, _smtp.Senha)
        };

        await smtpClient.SendMailAsync(mail);
    }
}
