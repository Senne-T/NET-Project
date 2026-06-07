using Restaurant.ViewModels.Enquete;
using Restaurant.ViewModels.Gebruiker;
using Restaurant.ViewModels.Gebruikers;
using Restaurant.ViewModels.Land;
using Restaurant.ViewModels.Mail;
using Restaurant.ViewModels.Parameter;
using Restaurant.ViewModels.Producten;
using Restaurant.ViewModels.Tafel;
using Restaurant.ViewModels.Reservatie;

namespace Restaurant.Configuration
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // === Reservatie Mappings ===
            // Reservatie <-> ReservatieViewModel
            CreateMap<Reservatie, ReservatieViewModel>()
                .ForMember(dest => dest.TijdSlotOpties, opt => opt.Ignore())
                .ForMember(dest => dest.IsBeschikbaar, opt => opt.Ignore())
                .ForMember(dest => dest.BeschikbaarheidsBericht, opt => opt.Ignore())
                .ForMember(dest => dest.RestaurantNaam, opt => opt.Ignore())
                .ForMember(dest => dest.RestaurantTelefoon, opt => opt.Ignore())
                .ForMember(dest => dest.RestaurantEmail, opt => opt.Ignore())
                .ForMember(dest => dest.MaxDagenVooruitReserveren, opt => opt.Ignore())
                .ForMember(dest => dest.AnnulatieTermijn, opt => opt.Ignore())
                .ForMember(dest => dest.MaxPersonenPerReservatie, opt => opt.Ignore())
                .ForMember(dest => dest.MinPersonenPerReservatie, opt => opt.Ignore())
                .ReverseMap();

            // Reservatie -> ReservatieOverzichtViewModel
            CreateMap<Reservatie, ReservatieOverzichtViewModel>()
                .ForMember(dest => dest.Datum, opt => opt.MapFrom(src => src.Datum ?? DateTime.MinValue))
                .ForMember(dest => dest.TijdslotNaam, opt => opt.MapFrom(src => src.Tijdslot != null ? src.Tijdslot.Naam : string.Empty))
                .ForMember(dest => dest.TafelIds, opt => opt.MapFrom(src => src.Tafellijsten != null ? src.Tafellijsten.Select(tl => tl.TafelId).ToList() : new List<int>()))
                .ForMember(dest => dest.KlantNaam, opt => opt.MapFrom(src => src.CustomUser != null ? $"{src.CustomUser.Voornaam} {src.CustomUser.Achternaam}" : string.Empty))
                .ForMember(dest => dest.TafelNummers, opt => opt.MapFrom(src => src.Tafellijsten != null && src.Tafellijsten.Any() ? string.Join(", ", src.Tafellijsten.Where(tl => tl.Tafel != null).Select(tl => tl.Tafel.TafelNummer)) : string.Empty));

            // Reservatie -> ReservatieListViewModel
            CreateMap<Reservatie, ReservatieListViewModel>()
                .ForMember(dest => dest.TijdSlotNaam, opt => opt.MapFrom(src => src.Tijdslot != null ? src.Tijdslot.Naam : "Onbekend"))
                .ForMember(dest => dest.TafelNummers, opt => opt.MapFrom(src => src.Tafellijsten != null && src.Tafellijsten.Any()
                    ? string.Join(", ", src.Tafellijsten.Where(tl => tl.Tafel != null).Select(tl => tl.Tafel.TafelNummer))
                    : "Geen tafels"))
                .ForMember(dest => dest.Bestaald, opt => opt.MapFrom(src => src.Bestaald))
                .ForMember(dest => dest.EvaluatieAantalSterren, opt => opt.MapFrom(src => src.EvaluatieAantalSterren))
                .ForMember(dest => dest.IsAanwezig, opt => opt.MapFrom(src => src.IsAanwezig));

            // Reservatie -> EvaluationViewModel
            CreateMap<Reservatie, EnqueteViewModel>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => $"{src.CustomUser.Voornaam} {src.CustomUser.Achternaam}"))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.CustomUser.PhoneNumber))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.CustomUser.Email))
                .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.EvaluatieAantalSterren))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.EvaluatieOpmerkingen));

            // Reservatie -> ReservatieMetFactuurViewModel
            CreateMap<Reservatie, ReservatieMetFactuurViewModel>()
                .ForMember(dest => dest.ReservatieId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.KlantNaam, opt => opt.MapFrom(src => $"{src.CustomUser.Voornaam} {src.CustomUser.Achternaam}"))
                .ForMember(dest => dest.Tijdslot, opt => opt.MapFrom(src => src.Tijdslot.Naam))
                .ForMember(dest => dest.Tafels, opt => opt.MapFrom(src => string.Join(", ", src.Tafellijsten.Select(tl => tl.Tafel.TafelNummer))))
                .ForMember(dest => dest.Betaald, opt => opt.MapFrom(src => src.Bestaald))
                .ForMember(dest => dest.TotaalBedrag, opt => opt.Ignore()); // Calculated in controller

            // Bestelling -> FactuurRegelViewModel
            CreateMap<Bestelling, FactuurRegelViewModel>()
                .ForMember(dest => dest.BestellingId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductNaam, opt => opt.MapFrom(src => src.Product.Naam))
                .ForMember(dest => dest.Prijs, opt => opt.MapFrom(src => src.Product.PrijsProducten
                    .OrderByDescending(p => p.DatumVanaf)
                    .First().Prijs))
                .ForMember(dest => dest.Geannuleerd, opt => opt.MapFrom(src => src.StatusId == 4));

            // Reservatie + Bestellingen -> FactuurViewModel
            CreateMap<Reservatie, FactuurViewModel>()
                .ForMember(dest => dest.ReservatieId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.KlantNaam, opt => opt.MapFrom(src => $"{src.CustomUser.Voornaam} {src.CustomUser.Achternaam}"))
                .ForMember(dest => dest.Tijdslot, opt => opt.MapFrom(src => src.Tijdslot.Naam))
                .ForMember(dest => dest.AantalPersonen, opt => opt.MapFrom(src => src.AantalPersonen))
                .ForMember(dest => dest.Tafels, opt => opt.MapFrom(src => string.Join(", ", src.Tafellijsten.Select(tl => tl.Tafel.TafelNummer))))
                .ForMember(dest => dest.IsBetaald, opt => opt.MapFrom(src => src.Bestaald))
                .ForMember(dest => dest.Regels, opt => opt.Ignore())
                .ForMember(dest => dest.Subtotaal, opt => opt.Ignore())
                .ForMember(dest => dest.BTW, opt => opt.Ignore())
                .ForMember(dest => dest.Totaal, opt => opt.Ignore());

            // Reservatie + Bestellingen -> AfrekeningViewModel
            CreateMap<Reservatie, AfrekeningViewModel>()
                .ForMember(dest => dest.ReservatieId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.KlantNaam, opt => opt.MapFrom(src => $"{src.CustomUser.Voornaam} {src.CustomUser.Achternaam}"))
                .ForMember(dest => dest.Datum, opt => opt.MapFrom(src => src.Datum))
                .ForMember(dest => dest.Tijdslot, opt => opt.MapFrom(src => src.Tijdslot.Naam))
                .ForMember(dest => dest.AantalPersonen, opt => opt.MapFrom(src => src.AantalPersonen))
                .ForMember(dest => dest.Tafels, opt => opt.MapFrom(src => string.Join(", ", src.Tafellijsten.Select(tl => tl.Tafel.TafelNummer))))
                .ForMember(dest => dest.Regels, opt => opt.Ignore())
                .ForMember(dest => dest.Subtotaal, opt => opt.Ignore())
                .ForMember(dest => dest.BTW, opt => opt.Ignore())
                .ForMember(dest => dest.Totaal, opt => opt.Ignore());

            // === Tijdslot Mappings ===
            // Tijdslot -> SelectListItem
            CreateMap<Tijdslot, SelectListItem>()
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Naam))
                .ForMember(dest => dest.Disabled, opt => opt.MapFrom(src => !src.Actief))
                .ForMember(dest => dest.Group, opt => opt.Ignore())
                .ForMember(dest => dest.Selected, opt => opt.Ignore());

            // Tijdslot -> TijdslotBeschikbaarheidViewModel
            CreateMap<Tijdslot, TijdslotBeschikbaarheidViewModel>()
                .ForMember(dest => dest.TijdSlotId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IsBeschikbaar, opt => opt.Ignore())
                .ForMember(dest => dest.BeschikbarePlaatsen, opt => opt.Ignore());

            // === Mail Mappings ===
            // Mail <-> MailViewModel
            CreateMap<Mail, MailViewModel>().ReverseMap();

            // Mail -> MailListViewModel
            CreateMap<Mail, MailListViewModel>();

            // === Gebruiker Mappings ===
            // ViewModel -> Model
            CreateMap<GebruikerRegisterViewModel, CustomUser>()
                .ForMember(x => x.UserName, y => y.MapFrom(z => z.Emailadres))
                .ForMember(x => x.Email, y => y.MapFrom(z => z.Emailadres));

            // Model -> ViewModel
            CreateMap<CustomUser, GebruikerEditViewModel>()
                .ForMember(x => x.Emailadres, y => y.MapFrom(z => z.Email));

            // Model -> ViewModel
            CreateMap<CustomUser, GebruikerDeleteViewModel>()
                .ForMember(x => x.Emailadres, y => y.MapFrom(z => z.Email));

            // === Gebruikers Mappings ===
            // Model -> ViewModel
            CreateMap<CustomUser, GebruikersViewModel>()
                .ForMember(x => x.Emailadres, y => y.MapFrom(z => z.Email));

            // ViewModel -> Model
            CreateMap<GebruikersCreateViewModel, CustomUser>()
                .ForMember(x => x.UserName, y => y.MapFrom(z => z.Emailadres))
                .ForMember(x => x.Email, y => y.MapFrom(z => z.Emailadres));

            // Model -> ViewModel
            CreateMap<CustomUser, GebruikersDetailsViewModel>()
                .ForMember(x => x.Emailadres, y => y.MapFrom(z => z.Email));

            // Model -> ViewModel
            CreateMap<CustomUser, GebruikersEditViewModel>()
                .ForMember(x => x.Emailadres, y => y.MapFrom(z => z.Email));

            // Model -> ViewModel
            CreateMap<CustomUser, GebruikersDeactiverenViewModel>()
                .ForMember(x => x.Emailadres, y => y.MapFrom(z => z.Email));

            // === Land Mappings ===
            // Model -> ViewModel
            CreateMap<Land, LandViewModel>();

            // === Product Mappings ===
            CreateMap<Product, ProductViewModel>().ReverseMap();

            CreateMap<Product, ProductDetailsViewModel>()
                .ForMember(dest => dest.CategorieNaam, opt => opt.MapFrom(src => src.Categorie.Naam))
                .ForMember(dest => dest.Prijs, opt => opt.MapFrom(src => src.PrijsProducten
                    .OrderByDescending(p => p.DatumVanaf)
                    .Select(p => p.Prijs)
                    .FirstOrDefault()));

            CreateMap<Product, ProductEditViewModel>()
                .ForMember(dest => dest.Prijs, opt => opt.MapFrom(src => src.PrijsProducten
                    .OrderByDescending(p => p.DatumVanaf)
                    .First().Prijs));

            CreateMap<ProductCreateViewModel, Product>();

            CreateMap<ProductEditViewModel, Product>()
                .ForMember(dest => dest.PrijsProducten, opt => opt.Ignore()); // prijs wordt apart afgehandeld

            CreateMap<Product, ProductDeleteViewModel>()
                .ForMember(dest => dest.CategorieNaam, opt => opt.MapFrom(src => src.Categorie.Naam))
                .ForMember(dest => dest.Prijs, opt => opt.MapFrom(src => src.PrijsProducten
                    .OrderByDescending(p => p.DatumVanaf)
                    .Select(p => p.Prijs)
                    .FirstOrDefault()));

            // === Parameter Mappings ===
            CreateMap<Parameter, ParameterViewModel>().ReverseMap();

            // === Tafel Mappings ===
            CreateMap<Models.Tafel, TafelViewModel>().ReverseMap();
            CreateMap<Models.Tafel, TafelDetailsViewModel>().ReverseMap();
            CreateMap<Models.Tafel, TafelDeleteViewModel>().ReverseMap();
            CreateMap<TafelCreateViewModel, Models.Tafel>();
            CreateMap<TafelEditViewModel, Models.Tafel>().ReverseMap();
        }
    }
}