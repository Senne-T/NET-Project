using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Restaurant.Data.UnitOfWork;

namespace Restaurant.Services
{
    public class WelkomstMailScheduler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WelkomstMailScheduler> _logger;

        public WelkomstMailScheduler(IServiceProvider serviceProvider, ILogger<WelkomstMailScheduler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WelkomstMailScheduler gestart.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Draai de taak één keer per 24 uur
                    await SendPendingWelkomstMailsAsync(stoppingToken);
                }
                catch (OperationCanceledException) { /* shutdown */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fout in WelkomstMailScheduler loop.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException) { /* shutdown */ }
            }

            _logger.LogInformation("WelkomstMailScheduler gestopt.");
        }

        private async Task SendPendingWelkomstMailsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var templateMailService = scope.ServiceProvider.GetRequiredService<TemplateMailService>();

            // Zoek alle reservaties binnen 7 dagen die nog geen welkomstmail kregen
            var pending = await unitOfWork.ReservatiesRepository.GetReservatiesWachtendeWelkomstmailAsync(7);

            _logger.LogInformation("WelkomstMailScheduler: gevonden {Count} te verzenden welkomstmails.", pending.Count);

            foreach (var r in pending)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (r.CustomUser?.Email == null)
                    {
                        _logger.LogWarning("Reservatie {ReservatieId} heeft geen email, overslaan.", r.Id);
                        continue;
                    }

                    var tijdSlotNaam = r.Tijdslot?.Naam ?? string.Empty;

                    var placeholders = new Dictionary<string, string>
                    {
                        ["VOORNAAM"] = r.CustomUser.Voornaam ?? string.Empty,
                        ["ACHTERNAAM"] = r.CustomUser.Achternaam ?? string.Empty,
                        ["DATUM"] = r.Datum?.ToString("dd/MM/yyyy") ?? string.Empty,
                        ["TIJD"] = tijdSlotNaam,
                        ["AANTAL"] = r.AantalPersonen.ToString(),
                        ["NAAM"] = "" // indien nodig kan parameter repository gebruikt worden
                    };

                    var sent = await templateMailService.SendTemplateMailAsync("WelkomstMail", r.CustomUser.Email, placeholders);
                    if (sent)
                    {
                        await unitOfWork.ReservatiesRepository.MarkWelkomstmailVerstuurdAsync(r.Id, DateTime.Today);
                        _logger.LogInformation("Welkomstmail verzonden voor reservatie {Id} naar {Email}", r.Id, r.CustomUser.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Kon welkomstmail niet verzenden voor reservatie {Id}", r.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fout bij verzenden welkomstmail voor reservatie {Id}", r.Id);
                }
            }
        }
    }
}
