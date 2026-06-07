using System.Net;
using System.Net.Mail;

namespace Restaurant.Services
{
    public class MailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailService> _logger;

        public MailService(IConfiguration config, ILogger<MailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendMailAsync(string to, string subject, string body, Dictionary<string, string>? replacements = null)
        {
            try
            {
                if (replacements != null)
                {
                    foreach (var replacement in replacements)
                    {
                        subject = subject.Replace($"[{replacement.Key}]", replacement.Value);
                        body = body.Replace($"[{replacement.Key}]", replacement.Value);
                    }
                }

                // Replace escaped newlines (\\n from database) and actual newlines with <br> for HTML emails
                body = body.Replace("\\n", "<br>");  // Escaped backslash-n from SQL
                body = body.Replace("\n", "<br>");   // Actual newline characters
                body = body.Replace("\r\n", "<br>"); // Windows line endings
                body = body.Replace("\r", "<br>");   // Mac line endings

                var host = _config["Smtp:Host"];
                var portString = _config["Smtp:Port"];
                var username = _config["Smtp:Username"];
                var password = _config["Smtp:Passw0rd"];

                // Validatie
                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString) ||
                    string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogError("SMTP configuratie ontbreekt. Controleer appsettings.json");
                    throw new InvalidOperationException("SMTP configuratie is niet compleet.");
                }

                if (!int.TryParse(portString, out int port))
                {
                    _logger.LogError($"Ongeldige SMTP poort: {portString}");
                    throw new InvalidOperationException($"SMTP poort '{portString}' is geen geldig nummer.");
                }

                var client = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password)
                };

                var message = new MailMessage(username, to, subject, body)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(message);

                _logger.LogInformation($"Email succesvol verzonden naar {to} met onderwerp '{subject}'");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, $"SMTP fout bij verzenden email naar {to}: {ex.Message}");
                throw new InvalidOperationException($"Fout bij verzenden email: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Onverwachte fout bij verzenden email naar {to}");
                throw;
            }
        }
    }
}
