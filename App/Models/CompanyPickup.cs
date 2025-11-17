using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Models
{
    public class CompanyPickup
    {
        public int PickupCompanyId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Website { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? CoverageNotes { get; set; }
        public int Prioridad { get; set; }
        public string? Notas { get; set; }
        public string? RegionCobertura { get; set; }
        public string? ComunaCobertura { get; set; }
        public int ComponenteId { get; set; }
        public string ComponenteNombre { get; set; } = "";
    }
}
