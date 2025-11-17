using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Models
{
    public class ComponentOption
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public bool IsSelected { get; set; }
    }
}
