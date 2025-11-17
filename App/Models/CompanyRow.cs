using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace App.Models
{
    public class CompanyRow
    {
        public int PickupCompanyId { get; set; }
        public string Nombre { get; set; } = "";
        public bool Active { get; set; }
        public int Coberturas { get; set; }
        public int Componentes { get; set; }

        public string ActivoLabel => Active ? "Activo" : "Inactivo";
        public Color ActivoColor => Active ? Colors.Green : Colors.Gray;
        public string CoberturasResumen => $"{Coberturas} cobertura(s)";
        public string ComponentesResumen => $"{Componentes} componente(s)";
    }
}
