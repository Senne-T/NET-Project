namespace Restaurant.Data.Repository
{
    public class ReservatieRepository : GenericRepository<Reservatie>, IReservatieRepository
    {
        private readonly IMapper _mapper;
        private readonly ILogger<ReservatieRepository> _logger;

        public ReservatieRepository(RestaurantContext context, IMapper mapper, ILogger<ReservatieRepository> logger) 
            : base(context)
        {
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> IsSluitingsdagAsync(DateTime datum)
        {
            var sluitingsdagen = await _context.Set<Sluitingsdag>()
                .Where(s => s.Datum == datum.Date)
                .ToListAsync();
            return sluitingsdagen.Any();
        }

        public List<List<int>> GetMogelijkeTafelCombinaties(int aantalPersonen)
        {
            var combinaties = new List<List<int>>();

            if (aantalPersonen <= 2)
                combinaties.Add(new List<int> { 2 });
            else if (aantalPersonen <= 4)
                combinaties.Add(new List<int> { 4 });
            else if (aantalPersonen <= 6)
                combinaties.Add(new List<int> { 6 });
            else if (aantalPersonen <= 8)
                combinaties.Add(new List<int> { 8 });
            else if (aantalPersonen <= 10)
            {
                combinaties.Add(new List<int> { 8, 2 });
                combinaties.Add(new List<int> { 6, 4 });
            }

            return combinaties;
        }

        private async Task<int> GetIntParameterAsync(string naam, int defaultWaarde)
        {
            var parameter = await _context.Set<Parameter>()
                .FirstOrDefaultAsync(p => p.Naam == naam);
            
            if (parameter?.Waarde != null && int.TryParse(parameter.Waarde, out int result))
                return result;
            
            return defaultWaarde;
        }

        private async Task<string> GetStringParameterAsync(string naam, string defaultWaarde = "")
        {
            var parameter = await _context.Set<Parameter>()
                .FirstOrDefaultAsync(p => p.Naam == naam);
            
            return parameter?.Waarde ?? defaultWaarde;
        }

        public async Task<BeschikbaarheidViewModel> ControleerBeschikbaarheidAsync(DateTime datum, int tijdSlotId, int aantalPersonen)
        {
            var result = new BeschikbaarheidViewModel
            {
                Datum = datum,
                TijdSlotId = tijdSlotId,
                AantalPersonen = aantalPersonen,
                IsBeschikbaar = false
            };

            if (datum.Date < DateTime.Today)
            {
                result.Bericht = "U kunt geen reservatie maken voor een datum in het verleden.";
                return result;
            }

            if (await IsSluitingsdagAsync(datum))
            {
                result.Bericht = "Het restaurant is gesloten op deze datum.";
                return result;
            }

            int maxPersonen = await GetIntParameterAsync("MaxPersonenPerReservatie", 10);
            int minPersonen = await GetIntParameterAsync("MinPersonenPerReservatie", 1);
            int maxDagen = await GetIntParameterAsync("MaxDagenVooruitReserveren", 90);
            string restaurantTelefoon = await GetStringParameterAsync("RestaurantTelefoon", "+32 9 123 45 67");

            if (maxPersonen <= 0)
            {
                _logger.LogError("Parameter 'MaxPersonenPerReservatie' heeft ongeldige waarde");
                result.Bericht = "Systeemfout: MaxPersonenPerReservatie niet correct geconfigureerd.";
                return result;
            }

            if (aantalPersonen > maxPersonen)
            {
                result.Bericht = $"Voor groepen vanaf {maxPersonen + 1} personen verzoeken wij u telefonisch contact op te nemen via {restaurantTelefoon}.";
                return result;
            }

            if (aantalPersonen < minPersonen)
            {
                result.Bericht = $"Aantal personen moet minimaal {minPersonen} zijn.";
                return result;
            }

            if (datum.Date > DateTime.Today.AddDays(maxDagen))
            {
                result.Bericht = $"U kunt maximaal {maxDagen} dagen vooruit reserveren.";
                return result;
            }

            var tijdslot = await _context.Set<Tijdslot>().FindAsync(tijdSlotId);
            if (tijdslot == null || !tijdslot.Actief)
            {
                result.Bericht = "Geselecteerd tijdslot is niet beschikbaar.";
                return result;
            }

            var geschikteTafels = await ZoekGeschikteTafelsAsync(datum, tijdSlotId, aantalPersonen);

            if (geschikteTafels.Any())
            {
                result.IsBeschikbaar = true;
                var combinaties = GetMogelijkeTafelCombinaties(aantalPersonen);
                
                if (geschikteTafels.Count > 1)
                {
                    var gebruikteTafel1 = await _context.Set<Tafel>().FindAsync(geschikteTafels[0]);
                    var gebruikteTafel2 = await _context.Set<Tafel>().FindAsync(geschikteTafels[1]);
                    
                    if (gebruikteTafel1 != null && gebruikteTafel2 != null)
                    {
                        result.Bericht = $"Tafel van {gebruikteTafel1.AantalPersonen} en {gebruikteTafel2.AantalPersonen} personen beschikbaar.";
                    }
                }
                else if (geschikteTafels.Count == 1)
                {
                    var gebruikteTafel = await _context.Set<Tafel>().FindAsync(geschikteTafels[0]);
                    if (gebruikteTafel != null)
                    {
                        result.Bericht = $"Tafel van {gebruikteTafel.AantalPersonen} personen beschikbaar.";
                    }
                }
            }
            else
            {
                var combinaties = GetMogelijkeTafelCombinaties(aantalPersonen);
                
                if (combinaties.Any())
                {
                    if (combinaties.Count > 1 && combinaties[0].Count > 1)
                    {
                        var combText = string.Join(" of ", combinaties.Select(c => 
                            c.Count > 1 ? $"{c[0]}+{c[1]}" : $"{c[0]}"
                        ));
                        result.Bericht = $"Geen tafelcombinatie beschikbaar ({combText} personen).";
                    }
                    else
                    {
                        var gevondenCombinatie = combinaties.FirstOrDefault();
                        if (gevondenCombinatie != null && gevondenCombinatie.Count > 1)
                        {
                            result.Bericht = $"Geen combinatie van tafel {gevondenCombinatie[0]} en {gevondenCombinatie[1]} personen beschikbaar.";
                        }
                        else if (gevondenCombinatie != null)
                        {
                            result.Bericht = $"Geen tafel van {gevondenCombinatie[0]} personen beschikbaar.";
                        }
                    }
                }
            }

            return result;
        }

        public async Task<int> BerekenAantalBeschikbareGroepenAsync(DateTime datum, int tijdSlotId, int aantalPersonen)
        {
            int maxPersonen = await GetIntParameterAsync("MaxPersonenPerReservatie", 10);

            if (maxPersonen <= 0)
            {
                _logger.LogError("Parameter 'MaxPersonenPerReservatie' ontbreekt of heeft ongeldige waarde");
                return 0;
            }

            if (aantalPersonen > maxPersonen)
                return 0;

            var mogelijkeCombinaties = GetMogelijkeTafelCombinaties(aantalPersonen);

            if (!mogelijkeCombinaties.Any())
                return 0;

            var bezeteTafels = await _context.Set<TafelLijst>()
                .Include(tl => tl.Reservatie)
                .Where(tl => tl.Reservatie.Datum == datum.Date && tl.Reservatie.TijdSlotId == tijdSlotId && !tl.Reservatie.Bestaald)
                .ToListAsync();
            
            var bezeteTafelIds = bezeteTafels.Select(tl => tl.TafelId).Distinct().ToList();

            // Probeer alle combinaties en return bij de eerste beschikbare
            foreach (var combinatie in mogelijkeCombinaties)
            {
                if (combinatie.Count > 1)
                {
                    var tafel1 = await _context.Set<Tafel>()
                        .Where(t => t.Actief && t.AantalPersonen == combinatie[0] && !bezeteTafelIds.Contains(t.Id))
                        .FirstOrDefaultAsync();

                    var tafel2 = await _context.Set<Tafel>()
                        .Where(t => t.Actief && t.AantalPersonen == combinatie[1] && !bezeteTafelIds.Contains(t.Id))
                        .FirstOrDefaultAsync();

                    if (tafel1 != null && tafel2 != null)
                        return 1; // Er is minstens 1 combinatie beschikbaar
                }
                else
                {
                    var aantalBeschikbaar = await _context.Set<Tafel>()
                        .Where(t => t.Actief && t.AantalPersonen == combinatie[0] && !bezeteTafelIds.Contains(t.Id))
                        .CountAsync();

                    if (aantalBeschikbaar > 0)
                        return aantalBeschikbaar;
                }
            }

            return 0;
        }

        public async Task<List<int>> ZoekGeschikteTafelsAsync(DateTime datum, int tijdSlotId, int aantalPersonen)
        {
            int maxPersonen = await GetIntParameterAsync("MaxPersonenPerReservatie", 10);

            if (maxPersonen <= 0)
            {
                _logger.LogError("Parameter 'MaxPersonenPerReservatie' ontbreekt bij zoeken tafels");
                return new List<int>();
            }

            if (aantalPersonen > maxPersonen)
                return new List<int>();

            var mogelijkeCombinaties = GetMogelijkeTafelCombinaties(aantalPersonen);

            if (!mogelijkeCombinaties.Any())
                return new List<int>();

            var bezeteTafels = await _context.Set<TafelLijst>()
                .Include(tl => tl.Reservatie)
                .Where(tl => tl.Reservatie.Datum == datum.Date && tl.Reservatie.TijdSlotId == tijdSlotId && !tl.Reservatie.Bestaald)
                .ToListAsync();
            
            var bezeteTafelIds = bezeteTafels.Select(tl => tl.TafelId).Distinct().ToList();
            var resultTafels = new List<int>();

            // Probeer elke combinatie totdat er een beschikbaar is
            foreach (var combinatie in mogelijkeCombinaties)
            {
                if (combinatie.Count > 1)
                {
                    // Voor combinaties (bijv. 8+2 of 6+4)
                    var tafel1 = await _context.Set<Tafel>()
                        .Where(t => t.Actief && t.AantalPersonen == combinatie[0] && !bezeteTafelIds.Contains(t.Id))
                        .OrderBy(t => t.Id)
                        .FirstOrDefaultAsync();

                    var tafel2 = await _context.Set<Tafel>()
                        .Where(t => t.Actief && t.AantalPersonen == combinatie[1] && !bezeteTafelIds.Contains(t.Id))
                        .OrderBy(t => t.Id)
                        .FirstOrDefaultAsync();

                    if (tafel1 != null && tafel2 != null)
                    {
                        resultTafels.Add(tafel1.Id);
                        resultTafels.Add(tafel2.Id);
                        _logger.LogInformation($"Tafelcombinatie gevonden: {combinatie[0]}+{combinatie[1]} personen (Tafel {tafel1.Id} + Tafel {tafel2.Id})");
                        break; // Alleen stoppen als we een VOLLEDIGE combinatie hebben
                    }
                    else
                    {
                        _logger.LogInformation($"Tafelcombinatie {combinatie[0]}+{combinatie[1]} niet beschikbaar, probeer volgende combinatie");
                    }
                }
                else
                {
                    // Voor enkele tafels
                    var tafel = await _context.Set<Tafel>()
                        .Where(t => t.Actief && t.AantalPersonen == combinatie[0] && !bezeteTafelIds.Contains(t.Id))
                        .OrderBy(t => t.Id)
                        .FirstOrDefaultAsync();

                    if (tafel != null)
                    {
                        resultTafels.Add(tafel.Id);
                        _logger.LogInformation($"Enkele tafel gevonden: {combinatie[0]} personen (Tafel {tafel.Id})");
                        break;
                    }
                }
            }

            return resultTafels;
        }

        public async Task<bool> MaakReservatieAsync(string klantId, ReservatieViewModel model)
        {
            try
            {
                var beschikbaarheid = await ControleerBeschikbaarheidAsync(model.Datum, model.TijdSlotId, model.AantalPersonen);

                if (!beschikbaarheid.IsBeschikbaar)
                {
                    _logger.LogWarning($"Reservatie niet mogelijk: {beschikbaarheid.Bericht}");
                    return false;
                }

                var geschikteTafels = await ZoekGeschikteTafelsAsync(model.Datum, model.TijdSlotId, model.AantalPersonen);

                if (!geschikteTafels.Any())
                {
                    _logger.LogWarning("Geen geschikte tafels gevonden");
                    return false;
                }

                var reservatie = new Reservatie
                {
                    KlantId = klantId,
                    Datum = model.Datum,
                    AantalPersonen = model.AantalPersonen,
                    TijdSlotId = model.TijdSlotId,
                    Opmerking = model.Opmerking,
                    Bestaald = false,
                    IsAanwezig = false,
                    EvaluatieAantalSterren = -1
                };

                await AddAsync(reservatie);
                await _context.SaveChangesAsync();

                foreach (var tafelId in geschikteTafels)
                {
                    var tafelLijst = new TafelLijst
                    {
                        ReservatieId = reservatie.Id,
                        TafelId = tafelId
                    };
                    await _context.Set<TafelLijst>().AddAsync(tafelLijst);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reservatie {reservatie.Id} succesvol aangemaakt via ReservatieRepository");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij het maken van reservatie");
                return false;
            }
        }

        public async Task<List<Reservatie>> GetReservatiesByUserIdAsync(string userId)
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.Tijdslot)
                .Include(r => r.Tafellijsten)
                    .ThenInclude(tl => tl.Tafel)
                .Where(r => r.KlantId == userId)
                .OrderBy(r => r.Datum)
                .ToListAsync();
        }

        public async Task<bool> AnnuleerReservatieAsync(int reservatieId, string userId)
        {
            try
            {
                var reservatie = await _context.Set<Reservatie>()
                    .FirstOrDefaultAsync(r => r.Id == reservatieId && r.KlantId == userId);

                if (reservatie == null)
                {
                    _logger.LogWarning($"Reservatie {reservatieId} niet gevonden voor user {userId}");
                    return false;
                }

                // Check if cancellation is allowed - prevent canceling within 24 hours
                int annulatieTermijn = await GetIntParameterAsync("AnnulatieTermijn", 24);
                if (reservatie.Datum.HasValue && reservatie.Datum.Value.Date <= DateTime.Today.AddHours(annulatieTermijn))
                {
                    _logger.LogWarning($"Kan geen reservatie annuleren binnen {annulatieTermijn} uur van de datum");
                    return false;
                }

                // Remove associated table assignments first
                var tafelLijsten = await _context.Set<TafelLijst>()
                    .Where(tl => tl.ReservatieId == reservatieId)
                    .ToListAsync();
                
                _context.Set<TafelLijst>().RemoveRange(tafelLijsten);

                // Delete the reservation
                Delete(reservatie);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reservatie {reservatieId} geannuleerd (verwijderd) door user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fout bij annuleren reservatie {reservatieId}");
                return false;
            }
        }

        public async Task<bool> WijzigReservatieAsync(int reservatieId, string userId, ReservatieViewModel model)
        {
            try
            {
                var reservatie = await _context.Set<Reservatie>()
                    .Include(r => r.Tafellijsten)
                    .FirstOrDefaultAsync(r => r.Id == reservatieId && r.KlantId == userId);

                if (reservatie == null)
                {
                    _logger.LogWarning($"Reservatie {reservatieId} niet gevonden voor user {userId}");
                    return false;
                }

                // Check availability for new details
                var beschikbaarheid = await ControleerBeschikbaarheidAsync(model.Datum, model.TijdSlotId, model.AantalPersonen);
                if (!beschikbaarheid.IsBeschikbaar)
                {
                    _logger.LogWarning($"Nieuwe details niet beschikbaar: {beschikbaarheid.Bericht}");
                    return false;
                }

                // Remove old table assignments
                _context.Set<TafelLijst>().RemoveRange(reservatie.Tafellijsten);

                // Find new tables
                var nieuweTafels = await ZoekGeschikteTafelsAsync(model.Datum, model.TijdSlotId, model.AantalPersonen);
                if (!nieuweTafels.Any())
                {
                    _logger.LogWarning("Geen nieuwe tafels gevonden voor wijziging");
                    return false;
                }

                // Update reservation
                reservatie.Datum = model.Datum;
                reservatie.AantalPersonen = model.AantalPersonen;
                reservatie.TijdSlotId = model.TijdSlotId;
                reservatie.Opmerking = model.Opmerking;

                Update(reservatie);

                // Add new table assignments
                foreach (var tafelId in nieuweTafels)
                {
                    var tafelLijst = new TafelLijst
                    {
                        ReservatieId = reservatie.Id,
                        TafelId = tafelId
                    };
                    await _context.Set<TafelLijst>().AddAsync(tafelLijst);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reservatie {reservatieId} gewijzigd door user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fout bij wijzigen reservatie {reservatieId}");
                return false;
            }
        }

        public async Task<List<Reservatie>> GetReservatiesVoorDatumEnTijdslotAsync(DateTime datum, int tijdslotId)
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.CustomUser)
                .Include(r => r.Tijdslot)
                .Include(r => r.Tafellijsten)
                    .ThenInclude(tl => tl.Tafel)
                .Where(r => r.Datum.HasValue && r.Datum.Value == datum && r.TijdSlotId == tijdslotId && !r.Bestaald)
                .ToListAsync();
        }

        public async Task<List<Reservatie>> GetReservatiesVoorDatumAsync(DateTime datum)
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.CustomUser)
                .Include(r => r.Tijdslot)
                .Include(r => r.Tafellijsten)
                    .ThenInclude(tl => tl.Tafel)
                .Where(r => r.Datum.HasValue && r.Datum.Value == datum && !r.Bestaald)
                .OrderBy(r => r.TijdSlotId)
                .ToListAsync();
        }

        public async Task<Reservatie?> GetReservatieWithUserAsync(int id)
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.CustomUser)
                .Include(r => r.Tijdslot)
                .Include(r => r.Tafellijsten)
                    .ThenInclude(tl => tl.Tafel)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Reservatie>> GetReservatiesMetBestellingenVoorDatumEnTijdslotAsync(DateTime datum, int tijdslotId)
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.Tafellijsten)
                    .ThenInclude(tl => tl.Tafel)
                .Include(r => r.CustomUser)
                .Include(r => r.Tijdslot)
                .Include(r => r.Bestellingen)
                    .ThenInclude(b => b.Product)
                        .ThenInclude(p => p.PrijsProducten)
                .Where(r => r.Datum.HasValue && r.Datum.Value == datum && r.TijdSlotId == tijdslotId)
                .ToListAsync();
        }

        public async Task<Reservatie?> GetReservatieMetBestellingenAsync(int id)
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.CustomUser)
                .Include(r => r.Tijdslot)
                .Include(r => r.Tafellijsten)
                    .ThenInclude(tl => tl.Tafel)
                .Include(r => r.Bestellingen)
                    .ThenInclude(b => b.Product)
                        .ThenInclude(p => p.PrijsProducten)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reservatie?> GetActiveReservationTodayAsync(string userId)
        {
            var today = DateTime.Today;
            return await _context.Set<Reservatie>()
                .FirstOrDefaultAsync(r => r.KlantId == userId && r.Datum == today && r.IsAanwezig);
        }

        public async Task<List<Reservatie>> GetAllEvaluationsAsync()
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.CustomUser)
                .Where(r => r.EvaluatieAantalSterren != -1)
                .OrderByDescending(r => r.Datum)
                .ToListAsync();
        }

        public async Task<List<Reservatie>> GetAllNonEvaluatedReservationsAsync()
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.CustomUser)
                .Where(r => r.EvaluatieAantalSterren == -1 && r.Bestaald && r.Datum.HasValue && r.Datum.Value.AddDays(14) >= DateTime.Today)
                .OrderByDescending(r => r.Datum)
                .ToListAsync();
        }

        public async Task<List<Reservatie>> GetReservatiesWachtendeWelkomstmailAsync(int binnenAantalDagen)
        {
            var today = DateTime.Today;
            var cutoff = DateTime.Today.AddDays(binnenAantalDagen);

            return await _context.Set<Reservatie>()
                .Include(r => r.CustomUser)
                .Include(r => r.Tijdslot)
                .Where(r =>
                    r.Datum.HasValue &&
                    r.WelkomstmailVerstuurdOp == null &&
                    r.Datum.Value.Date >= today &&
                    r.Datum.Value.Date <= cutoff)
                .ToListAsync();
        }

        public async Task<Reservatie?> FindReservatieByUserDateSlotAsync(string userId, DateTime datum, int tijdSlotId, int aantalPersonen)
        {
            return await _context.Set<Reservatie>()
                .Where(r =>
                    r.KlantId == userId &&
                    r.Datum.HasValue && r.Datum.Value.Date == datum.Date &&
                    r.TijdSlotId == tijdSlotId &&
                    r.AantalPersonen == aantalPersonen)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();
        }

        public async Task MarkWelkomstmailVerstuurdAsync(int reservatieId, DateTime when)
        {
            var reservatie = await _context.Set<Reservatie>()
                .FirstOrDefaultAsync(r => r.Id == reservatieId);
            if (reservatie == null)
            {
                _logger.LogWarning($"MarkWelkomstmailVerstuurdAsync: reservatie {reservatieId} niet gevonden.");
                return;
            }

            reservatie.WelkomstmailVerstuurdOp = when.Date;
            Update(reservatie);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Reservatie {reservatieId} gemarkeerd als welkomstmail verstuurd op {when.Date:yyyy-MM-dd}");
        }

        public async Task<List<Reservatie>> GetBetaaldeReservatiesVoorDatumAsync(DateTime datum)
        {
            return await _context.Set<Reservatie>()
                .Include(r => r.CustomUser)
                .Include(r => r.Tijdslot)
                .Include(r => r.Tafellijsten)
                    .ThenInclude(tl => tl.Tafel)
                .Where(r => r.Datum.HasValue && r.Datum.Value == datum && r.Bestaald)
                .OrderBy(r => r.TijdSlotId)
                .ToListAsync();
        }
    }
}
