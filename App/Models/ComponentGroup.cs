using System.Collections.ObjectModel;

namespace App.Models
{
    public class ComponentGroup
    {
        // Componento (info)
        public ComponentInfo Component { get; set; } = default!;

        // Empresas asociadas
        public ObservableCollection<CompanyPickup> Companies { get; set; } = new ObservableCollection<CompanyPickup>();
    }
}
