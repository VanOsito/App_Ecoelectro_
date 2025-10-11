using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace App.Models
{
    public class RegionChile
    {
        public class Comunas
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("NombreComuna")]
            public string NombreComuna { get; set; } = string.Empty;
        }

        public class RegionChileModel
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("Nombre")]
            public string Nombre { get; set; } = string.Empty;

            [JsonPropertyName("Comunas")]
            public List<Comunas> Comunas { get; set; } = new();
        }
    }
}
