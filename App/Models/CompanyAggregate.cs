using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Models
{
    public class CompanyAggregate
    {
        public int? PickupCompanyId { get; set; }          // null => crear
        public string Nombre { get; set; } = "";
        public string? Website { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? CoverageNotes { get; set; }
        public bool Active { get; set; } = true;

        // Coberturas (usa nombres completos, como en tu JSON y BD)
        public List<(string Region, string? Comuna)> Coberturas { get; set; } = new();

        // Componentes que atiende (IDs de componentes_catalogo)
        public List<int> ComponentesIds { get; set; } = new();
    }

 

    }
