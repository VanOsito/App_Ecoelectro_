using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Models
{
    public class ContenidoEducativo
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string? ImagenUrl { get; set; }
        public DateTime FechaPublicacion { get; set; }
        public bool EsPredeterminado { get; set; } = false;

        public bool EsAdminVisible { get; set; } = false;
        public bool TieneImagen { get; set; }
    }
}
