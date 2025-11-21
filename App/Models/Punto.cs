using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Models
{
    public class Punto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int Cantidad { get; set; }
        public string Tipo { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
    }

}