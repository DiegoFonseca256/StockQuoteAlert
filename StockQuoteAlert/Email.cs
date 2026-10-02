using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

public class Email
{
    

    public string Provedor { get; private set; }
    public string Username { get; private set; }
    public string Password { get; private set; }

    public Email(string provedor, string username, string password)
    {
        Provedor = provedor ?? throw new ArgumentNullException(nameof(provedor));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Password = password ?? throw new ArgumentNullException(nameof(password));
    }

    public void SendEmail(string EmailTo, string subject, string body)
    {
        // Implementação do envio de e-mail usando SMTP ou outro serviço
        Console.WriteLine($"Enviando e-mail para {EmailTo} com assunto '{subject}' e corpo '{body}'");

        var massage = PrepareteEmail(EmailTo, subject, body);

        SendMailBySmtp(massage);

    }

    private MailMessage PrepareteEmail(string EmailTo, string subject, string body)
    {
        var mail = new MailMessage();
        mail.From = new MailAddress(Username);
        mail.To.Add(EmailTo);
        mail.Subject = subject;
        mail.Body = body;
        return mail;
    }

    private void SendMailBySmtp(MailMessage mail)
    {
        var smtpClient = new SmtpClient();
        smtpClient.Host = Provedor;
        smtpClient.Port = 587; // Porta padrão para envio de e-mails
        smtpClient.EnableSsl = true; // Habilita SSL para segurança

        smtpClient.Credentials = new System.Net.NetworkCredential(Username, Password);
        smtpClient.Send(mail);
        smtpClient.Dispose();



    }

}
