using System.Net;
using System.Net.Mail;

namespace Api14.SMTP;

internal class SMTPService
{
    public void SendEmail(string toAddress, string subject, string body)
    {
        string senderEmail = "nucanasaraia@gmail.com";
        string appPassword = "msay wovl xltr gzwz";


        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(senderEmail);
        mail.To.Add(toAddress);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        var smtpClient = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            EnableSsl = true,
            Credentials = new NetworkCredential(senderEmail, appPassword)
        };

        smtpClient.Send(mail);

    }
}