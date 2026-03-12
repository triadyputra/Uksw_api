using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;

namespace yayasanApi.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            string _pengirim = _config["EmailSettings:From"];
            string _smtpServer = _config["EmailSettings:SmtpServer"];
            string _smtpPort = _config["EmailSettings:Port"];
            string _smtpUser = _config["EmailSettings:Username"];
            string _smtpPass = _config["EmailSettings:Password"];

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_pengirim));  // Ganti dengan email pengirim
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();

            try
            {
                // Ganti sesuai data asli
                string smtpServer = _smtpServer;
                int smtpPort = int.Parse(_smtpPort);
                string smtpUser = _smtpUser;
                string smtpPass = _smtpPass;

                // Tentukan SSL/TLS
                SecureSocketOptions socketOptions = SecureSocketOptions.StartTls;

                // (Opsional: untuk testing, jangan aktifkan di production)
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await smtp.ConnectAsync(smtpServer, smtpPort, socketOptions);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gagal mengirim email: {ex.Message}");
                throw;
            }
        }
    }
}
