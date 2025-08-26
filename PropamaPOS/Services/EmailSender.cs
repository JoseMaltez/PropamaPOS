using Microsoft.Extensions.Options;
using PropamaPOS.Models;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks; //Tareas asincronas

namespace PropamaPOS.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpOptions _opt;

        public EmailSender(IOptions<SmtpOptions> options)
        {
            _opt = options.Value;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            using var client = new SmtpClient(_opt.Host, _opt.Port)
            {
                EnableSsl = _opt.EnableSsl,
                Credentials = new NetworkCredential(_opt.User, _opt.Password)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_opt.From, _opt.DisplayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            await client.SendMailAsync(mail);
          }
        }
}
