using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Restaurant.Data.UnitOfWork;

namespace Restaurant.Services
{
    public class TemplateMailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly MailService _mailService;
        private readonly ILogger<TemplateMailService> _logger;

        public TemplateMailService(IUnitOfWork unitOfWork, MailService mailService, ILogger<TemplateMailService> logger)
        {
            _unitOfWork = unitOfWork;
            _mailService = mailService;
            _logger = logger;
        }

        public async Task<bool> SendTemplateMailAsync(string templateNaam, string toEmail, Dictionary<string, string> placeholders)
        {
            // 1) Email basic check
            if (string.IsNullOrWhiteSpace(toEmail) || !MailAddress.TryCreate(toEmail, out _))
            {
                _logger.LogWarning("Ongeldig e-mailadres: {Email}", toEmail);
                return false;
            }

            // 2) Template ophalen (Trim + IgnoreCase, zodat "WelkomstMail", "welkomstmail ", ... allemaal werken)
            var templates = await _unitOfWork.MailRepository.GetMailAsync();
            var template = templates.FirstOrDefault(m =>
                !string.IsNullOrWhiteSpace(m.Naam) &&
                m.Naam.Trim().Equals(templateNaam?.Trim(), StringComparison.OrdinalIgnoreCase)
            );

            if (template == null)
            {
                _logger.LogError("Mailtemplate '{TemplateNaam}' niet gevonden.", templateNaam);
                return false;
            }

            var subjectTemplate = template.Onderwerp ?? "(geen onderwerp)";
            var bodyTemplate = template.Body ?? string.Empty;

            // 3) Placeholders invullen (tolerant: trim + ignorecase + remove spaces inside brackets)
            string Render(string text)
            {
                if (string.IsNullOrEmpty(text))
                    return string.Empty;

                if (placeholders == null || placeholders.Count == 0)
                    return text;

                foreach (var kv in placeholders)
                {
                    var key = (kv.Key ?? "").Trim();
                    var value = kv.Value ?? string.Empty;

                    // vervang standaard [KEY]
                    text = text.Replace($"[{key}]", value);

                    // vervang ook varianten met andere casing
                    text = text.Replace($"[{key.ToUpper()}]", value);
                    text = text.Replace($"[{key.ToLower()}]", value);

                    // vervang ook varianten met spaties: [ KEY ] of [KEY ]
                    text = text.Replace($"[ {key} ]", value);
                    text = text.Replace($"[ {key}]", value);
                    text = text.Replace($"[{key} ]", value);
                }

                return text;
            }

            var subject = Render(subjectTemplate);
            var body = Render(bodyTemplate);

            // 4) Verzenden
            try
            {
                await _mailService.SendMailAsync(toEmail, subject, body);
                _logger.LogInformation("Mail '{TemplateNaam}' verstuurd naar {Email}.", templateNaam, toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij het versturen van de mail '{TemplateNaam}' naar {Email}.", templateNaam, toEmail);
                return false;
            }
        }

    }
}
