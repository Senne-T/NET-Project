using System.Collections.Generic;

namespace Restaurant.ViewModels.Bestelling
{
    public class BestellingMenuViewModel
    {
        public int ReservatieId { get; set; }

        public List<BestellingMenuItemViewModel> Items { get; set; }
            = new List<BestellingMenuItemViewModel>();
    }
}
