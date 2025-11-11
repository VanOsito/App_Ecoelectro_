using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace App.Models
{
    public class PuntoReciclaje
    {
        // CONSTRUCTOR CON VALORES POR DEFECTO CORREGIDOS
        public PuntoReciclaje()
        {
            // Valores por defecto consistentes
            Active = true;
            AcceptsRecycle = true;
            AcceptsReuse = false;
            AcceptsBuyback = false;
            Horario = "Lunes a Viernes 9:00-18:00";
            Costo = "Gratuito";
            Formato = "Punto Verde";
            Residuos = new List<string>();
            AcceptsNotes = "Punto de reciclaje electrónico";
            // Web ya está inicializado en la propiedad
        }

        // Propiedades existentes para JSON/Google Places
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

        // ✅ CORREGIDO: Sincronización entre Residuos y AcceptsNotes
        [JsonProperty("residuos")]
        public List<string> Residuos
        {
            get => _residuos;
            set
            {
                _residuos = value ?? new List<string>();
                // Sincronizar automáticamente con AcceptsNotes para BD
                ActualizarAcceptsNotesDesdeResiduos();
            }
        }
        private List<string> _residuos = new List<string>();

        [JsonProperty("formato")]
        public string Formato { get; set; } = string.Empty;

        // ✅ CORREGIDO: Propiedad Contacto ahora es calculada automáticamente
        [JsonProperty("contacto")]
        public string Contacto
        {
            get => GenerarContactoParaUI();
            set => ParsearContactoDesdeUI(value ?? string.Empty);
        }

        [JsonProperty("horario")]
        public string Horario { get; set; } = string.Empty;

        [JsonProperty("costo")]
        public string Costo { get; set; } = string.Empty;

        [JsonProperty("web")]
        public string Web { get; set; } = string.Empty;

        // Propiedades calculadas
        [JsonIgnore]
        public double DistanciaKm { get; set; }

        [JsonIgnore]
        public string DistanciaTexto => DistanciaKm > 0 ? $"{DistanciaKm:F1} km" : "";

        // PROPIEDADES PARA LA BASE DE DATOS
        [JsonIgnore]
        public string RecyclingPointId { get; set; } = string.Empty;

        [JsonIgnore]
        public string Telefono { get; set; } = string.Empty;

        [JsonIgnore]
        public string Email { get; set; } = string.Empty;

        // ✅ CORREGIDO: AcceptsNotes ahora se sincroniza con Residuos
        [JsonIgnore]
        public string AcceptsNotes
        {
            get => _acceptsNotes;
            set
            {
                _acceptsNotes = value ?? string.Empty;
                // Sincronizar automáticamente con Residuos cuando se carga desde BD
                ActualizarResiduosDesdeAcceptsNotes();
            }
        }
        private string _acceptsNotes = string.Empty;

        [JsonIgnore]
        public bool Active { get; set; } = true;

        [JsonIgnore]
        public bool AcceptsRecycle { get; set; }

        [JsonIgnore]
        public bool AcceptsReuse { get; set; }

        [JsonIgnore]
        public bool AcceptsBuyback { get; set; }

        // Propiedad para identificar si es punto del CRUD (BD)
        [JsonIgnore]
        public bool EsPuntoCRUD => !string.IsNullOrEmpty(RecyclingPointId) && RecyclingPointId.StartsWith("CRUD_");

        // ✅ NUEVOS MÉTODOS PARA SINCRONIZACIÓN RESIDUOS/ACCEPTS_NOTES
        private void ActualizarAcceptsNotesDesdeResiduos()
        {
            if (_residuos != null && _residuos.Any())
            {
                _acceptsNotes = string.Join(", ", _residuos);
            }
            else
            {
                _acceptsNotes = "Punto de reciclaje electrónico";
            }
        }

        private void ActualizarResiduosDesdeAcceptsNotes()
        {
            if (string.IsNullOrWhiteSpace(_acceptsNotes))
            {
                _residuos = new List<string>();
                return;
            }

            // Intentar parsear los residuos desde AcceptsNotes
            // Formato esperado: "Battery, Mobile, Television" o "Residuos: Battery, Mobile, Television"
            var contenido = _acceptsNotes.Replace("Residuos:", "").Trim();

            if (!string.IsNullOrWhiteSpace(contenido))
            {
                _residuos = contenido.Split(',')
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .ToList();
            }
            else
            {
                _residuos = new List<string>();
            }
        }

        // ✅ MÉTODOS PARA SINCRONIZACIÓN AUTOMÁTICA DE CONTACTO
        private string GenerarContactoParaUI()
        {
            // Generar string unificado para la UI basado en los campos separados de BD
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(Telefono))
                partes.Add($"Tel: {Telefono.Trim()}");

            if (!string.IsNullOrWhiteSpace(Email))
                partes.Add($"Email: {Email.Trim()}");

            if (!string.IsNullOrWhiteSpace(Web))
                partes.Add($"Web: {Web.Trim()}");

            return partes.Any() ? string.Join(" | ", partes) : "No especificado";
        }

        private void ParsearContactoDesdeUI(string contactoUI)
        {
            if (string.IsNullOrWhiteSpace(contactoUI))
            {
                Telefono = string.Empty;
                Email = string.Empty;
                // Web se mantiene separado
                return;
            }

            // Extraer información del string unificado de la UI
            Telefono = ExtraerTelefono(contactoUI);
            Email = ExtraerEmail(contactoUI);

            // Solo extraer Web si no está ya definida
            if (string.IsNullOrWhiteSpace(Web))
            {
                Web = ExtraerSitioWeb(contactoUI);
            }
        }

        // MÉTODOS DE EXTRACCIÓN MEJORADOS
        private string ExtraerTelefono(string texto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(texto))
                    return string.Empty;

                // Buscar patrones con prefijos
                if (texto.Contains("Tel:") || texto.Contains("Fono:") || texto.Contains("Telefono:"))
                {
                    var partes = texto.Split('|', ',', ';')
                        .FirstOrDefault(p => p.Contains("Tel:") || p.Contains("Fono:") || p.Contains("Telefono:"));

                    if (!string.IsNullOrEmpty(partes))
                    {
                        var soloNumeros = Regex.Replace(partes, @"[^\d+]", "");
                        if (!string.IsNullOrEmpty(soloNumeros))
                            return soloNumeros.Trim();
                    }
                }

                // Patrones para teléfonos chilenos
                var patrones = new[]
                {
                    @"(\+56\s?)?9\s?[0-9]{4}\s?[0-9]{4}",
                    @"(\+56\s?)?2\s?[0-9]{4}\s?[0-9]{4}",
                };

                foreach (var patron in patrones)
                {
                    var match = Regex.Match(texto, patron);
                    if (match.Success)
                    {
                        return Regex.Replace(match.Value, @"[^\d+]", "").Trim();
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extrayendo teléfono: {ex.Message}");
                return string.Empty;
            }
        }

        private string ExtraerEmail(string texto)
        {
            try
            {
                var patron = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
                var match = Regex.Match(texto, patron);
                return match.Success ? match.Value : string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extrayendo email: {ex.Message}");
                return string.Empty;
            }
        }

        private string ExtraerSitioWeb(string texto)
        {
            try
            {
                var patron = @"(https?://[^\s]+|www\.[^\s]+|[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/[^\s]*)";
                var match = Regex.Match(texto, patron);

                if (match.Success)
                {
                    var url = match.Value;
                    if (url.StartsWith("www.") && !url.StartsWith("http"))
                        return "https://" + url;
                    return url;
                }

                if (texto.Contains("Web:") || texto.Contains("Sitio:"))
                {
                    var partes = texto.Split('|', ',', ';')
                        .FirstOrDefault(p => p.Contains("Web:") || p.Contains("Sitio:"));

                    if (!string.IsNullOrEmpty(partes))
                    {
                        var web = partes.Replace("Web:", "").Replace("Sitio:", "").Trim();
                        if (!string.IsNullOrEmpty(web))
                            return web;
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extrayendo sitio web: {ex.Message}");
                return string.Empty;
            }
        }

        // ✅ MÉTODOS PARA CARGA DIRECTA DESDE BD (sin pasar por UI)
        public void CargarDesdeBD(string telefono, string email, string website, string acceptsNotes)
        {
            Telefono = telefono ?? string.Empty;
            Email = email ?? string.Empty;
            Web = website ?? string.Empty;
            AcceptsNotes = acceptsNotes ?? string.Empty;
            // Residuos y Contacto se generarán automáticamente
        }

        // ✅ MÉTODO PARA FORZAR SINCRONIZACIÓN DESDE BD
        public void SincronizarDesdeBD()
        {
            // Cuando cargas desde BD, forzar la sincronización de residuos
            ActualizarResiduosDesdeAcceptsNotes();
        }

        // MÉTODO DE VALIDACIÓN MEJORADO
        public bool EsValido()
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(Nombre))
                errores.Add("Nombre es requerido");

            if (string.IsNullOrWhiteSpace(Direccion))
                errores.Add("Dirección es requerida");

            if (string.IsNullOrWhiteSpace(Comuna))
                errores.Add("Comuna es requerida");

            if (string.IsNullOrWhiteSpace(Region))
                errores.Add("Región es requerida");

            if (Lat == 0 && Lng == 0)
                errores.Add("Coordenadas no válidas");

            if (Residuos == null || !Residuos.Any())
                errores.Add("Debe tener al menos un tipo de residuo");

            if (errores.Any())
            {
                System.Diagnostics.Debug.WriteLine($"Punto no válido: {string.Join(", ", errores)}");
                return false;
            }

            return true;
        }

        // MÉTODO MEJORADO PARA VALIDACIÓN DETALLADA
        public List<string> ObtenerErroresValidacion()
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(Nombre))
                errores.Add("El nombre es requerido");
            else if (Nombre.Length < 2)
                errores.Add("El nombre debe tener al menos 2 caracteres");

            if (string.IsNullOrWhiteSpace(Direccion))
                errores.Add("La dirección es requerida");
            else if (Direccion.Length < 5)
                errores.Add("La dirección debe tener al menos 5 caracteres");

            if (string.IsNullOrWhiteSpace(Comuna))
                errores.Add("La comuna es requerida");

            if (string.IsNullOrWhiteSpace(Region))
                errores.Add("La región es requerida");

            if (Math.Abs(Lat) < 0.001 && Math.Abs(Lng) < 0.001)
                errores.Add("Las coordenadas no son válidas");

            if (Residuos == null || !Residuos.Any())
                errores.Add("Debe especificar al menos un tipo de residuo");

            if (!string.IsNullOrWhiteSpace(Email) && !EsEmailValido(Email))
                errores.Add("El formato del email no es válido");

            return errores;
        }

        private bool EsEmailValido(string email)
        {
            try
            {
                var patron = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                return Regex.IsMatch(email, patron);
            }
            catch
            {
                return false;
            }
        }

        // MÉTODO TOSTRING MEJORADO
        public override string ToString()
        {
            return $"Punto: {Nombre} ({RecyclingPointId}) - {Direccion}, {Comuna} - Residuos: {string.Join(", ", Residuos)}";
        }

        // MÉTODO CLONAR PARA COPIAS
        public PuntoReciclaje Clonar()
        {
            return new PuntoReciclaje
            {
                Id = this.Id,
                Nombre = this.Nombre,
                Region = this.Region,
                Comuna = this.Comuna,
                Direccion = this.Direccion,
                Lat = this.Lat,
                Lng = this.Lng,
                Residuos = new List<string>(this.Residuos),
                Formato = this.Formato,
                // Contacto se generará automáticamente
                Horario = this.Horario,
                Costo = this.Costo,
                Web = this.Web,
                DistanciaKm = this.DistanciaKm,
                RecyclingPointId = this.RecyclingPointId,
                Telefono = this.Telefono,
                Email = this.Email,
                AcceptsNotes = this.AcceptsNotes,
                Active = this.Active,
                AcceptsRecycle = this.AcceptsRecycle,
                AcceptsReuse = this.AcceptsReuse,
                AcceptsBuyback = this.AcceptsBuyback
            };
        }

        // ✅ NUEVO: Método para comparar si dos puntos son equivalentes
        public bool EsEquivalenteA(PuntoReciclaje otro)
        {
            if (otro == null) return false;

            return Nombre == otro.Nombre &&
                   Direccion == otro.Direccion &&
                   Comuna == otro.Comuna &&
                   Math.Abs(Lat - otro.Lat) < 0.0001 &&
                   Math.Abs(Lng - otro.Lng) < 0.0001;
        }

        // ✅ NUEVO: Validación con tupla para mejor manejo
        public (bool esValido, List<string> errores) ValidarCompleto()
        {
            var errores = ObtenerErroresValidacion();
            return (!errores.Any(), errores);
        }
    }
}
