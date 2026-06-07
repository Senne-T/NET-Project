namespace Restaurant.ViewModels.Reservatie
{
    public class ReservatieViewModel
    {
        [Required(ErrorMessage = "Datum is verplicht")]
        [DataType(DataType.Date)]
        [Display(Name = "Datum")]
        public DateTime Datum { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Tijdslot is verplicht")]
        [Display(Name = "Tijdslot")]
        public int TijdSlotId { get; set; }

        [Required(ErrorMessage = "Aantal personen is verplicht")]
        [Range(1, int.MaxValue, ErrorMessage = "Aantal personen moet minimaal 1 zijn")]
        [Display(Name = "Aantal personen")]
        public int AantalPersonen { get; set; } = 2;

        [Display(Name = "Opmerking")]
        [StringLength(500, ErrorMessage = "Opmerking mag maximaal 500 karakters zijn")]
        public string? Opmerking { get; set; }

        public List<SelectListItem>? TijdSlotOpties { get; set; }

        public bool IsBeschikbaar { get; set; } = true;

        public string? BeschikbaarheidsBericht { get; set; }

        // Individuele parameter waarden die vanuit de controller worden gezet
        public string? RestaurantNaam { get; set; }
        public string? RestaurantTelefoon { get; set; }
        public string? RestaurantEmail { get; set; }
        public int MaxDagenVooruitReserveren { get; set; } = 90;
        public int AnnulatieTermijn { get; set; } = 24;
        public int MaxPersonenPerReservatie { get; set; } = 10;
        public int MinPersonenPerReservatie { get; set; } = 1;
    }
}
