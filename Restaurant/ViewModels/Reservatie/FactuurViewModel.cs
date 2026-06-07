using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels.Reservatie
{
    public class FactuurViewModel
    {
        public int ReservatieId { get; set; }
        public string KlantNaam { get; set; } = string.Empty;
        public DateTime? Datum { get; set; }
        public string Tijdslot { get; set; } = string.Empty;
        public int AantalPersonen { get; set; }
        public string Tafels { get; set; } = string.Empty;
        
        public List<FactuurRegelViewModel> Regels { get; set; } = new();
        
        public decimal Subtotaal { get; set; }
        public decimal BTW { get; set; }
        public decimal Totaal { get; set; }
        
        public bool IsBetaald { get; set; }
    }
}
