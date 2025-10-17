using Newtonsoft.Json;
using System.Collections.Generic;

namespace App.Models
{
    public class PuntoReciclaje
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonProperty("region")]
        public string Region { get; set; } = string.Empty;

        [JsonProperty("comuna")]
        public string Comuna { get; set; } = string.Empty;

        [JsonProperty("direccion")]
        public string Direccion { get; set; } = string.Empty;

        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lng")]
        public double Lng { get; set; }

        [JsonProperty("residuos")]
        public List<string> Residuos { get; set; } = new List<string>();

        [JsonProperty("formato")]
        public string Formato { get; set; } = string.Empty;

        [JsonProperty("contacto")]
        public string Contacto { get; set; } = string.Empty;

        [JsonProperty("horario")]
        public string Horario { get; set; } = string.Empty;

        [JsonProperty("costo")]
        public string Costo { get; set; } = string.Empty;

        [JsonProperty("web")]
        public string Web { get; set; } = string.Empty;

        // Propiedad calculada (no proviene del JSON)
        [JsonIgnore]
        public double DistanciaKm { get; set; }

        [JsonIgnore]
        public string DistanciaTexto => DistanciaKm > 0 ? $"{DistanciaKm:F1} km" : "";
    }
}



