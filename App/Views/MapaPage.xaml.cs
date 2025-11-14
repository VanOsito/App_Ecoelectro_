using App.Models;
using App.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics; 
namespace App.Views;

public partial class MapaPage : ContentPage
{
    private ObservableCollection<PuntoReciclaje> _puntos = new();
    private ObservableCollection<PuntoReciclaje> _puntosCombinados = new();
    private Location _ubicacionActual = new(-33.4489, -70.6693);
    private string _filtroSeleccionado = "All";
    private readonly string _apiKey = "AIzaSyAoDxr91Rqn_b6bIW4f5jz6Yl3SvKs8pe4";
    private bool _googlePlacesCargado = false;

    // Sistema de rangos
    private int _rangoKm = 10;
    private readonly int[] _rangosDisponibles = { 5, 10, 15, 25, 50 };

    // Lista de dispositivos para el filtro
    private readonly string[] _dispositivos = {
        "Todos los dispositivos",
        "Battery",
        "Keyboard",
        "Microwave",
        "Mobile",
        "Mouse",
        "PCB",
        "Player",
        "Printer",
        "Television",
        "Washing Machine"
    };

    // Variables para gestión CRUD
    private ObservableCollection<PuntoReciclaje> _puntosParaGestion = new();
    private IPuntoReciclajeService _puntoService;
    private bool _panelGestionVisible = false;

    // Listas para formularios
    private readonly string[] _formatosDisponibles = {
        "Punto Verde", "Punto Limpio", "Centro de Acopio", "Reciclaje Municipal", "Punto Móvil"
    };

    private readonly string[] _residuosDisponibles = {
        "Battery", "Keyboard", "Microwave", "Mobile", "Mouse",
        "PCB", "Player", "Printer", "Television", "Washing Machine"
    };

    public MapaPage()
    {
        InitializeComponent();

        _puntoService = new PuntoReciclajeService();

        _ = CargarDatosCompletosAsync();
        _ = CargarPuntosParaGestionAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosCompletosAsync();
    }

    // ✅ CORREGIDO: Ahora incluye los puntos del CRUD
    private async Task CargarDatosCompletosAsync()
    {
        try
        {
            await CargarPuntosDesdeJsonAsync();
            await CargarPuntosCRUDAlMapa(); // ✅ NUEVO: Cargar puntos del CRUD
            await CargarPuntosDesdeGooglePlacesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
        }
    }

    private async Task CargarPuntosDesdeJsonAsync()
    {
        try
        {
            if (LblInfo != null)
                LblInfo.Text = "Cargando puntos locales...";

            using var stream = await FileSystem.OpenAppPackageFileAsync("puntos_reciclaje.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var puntos = JsonConvert.DeserializeObject<ObservableCollection<PuntoReciclaje>>(json)
                        ?? new ObservableCollection<PuntoReciclaje>();

            _puntos = puntos;
            await ObtenerUbicacionYMostrarPuntos();

            Console.WriteLine($"Cargados {_puntos.Count} puntos desde JSON local");
        }
        catch (FileNotFoundException)
        {
            await DisplayAlert("Advertencia", "No se encontró el archivo de puntos locales", "OK");
        }
        catch (JsonException jsonEx)
        {
            await DisplayAlert("Error", $"Error en formato JSON: {jsonEx.Message}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar puntos locales: {ex.Message}", "OK");
        }
    }

    private async Task CargarPuntosDesdeGooglePlacesAsync()
    {
        try
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                Console.WriteLine("Sin conexión a internet - No se cargarán puntos de Google Places");
                return;
            }

            if (LblInfo != null)
                LblInfo.Text = "Buscando puntos adicionales...";

            string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?" +
                        $"location={_ubicacionActual.Latitude},{_ubicacionActual.Longitude}&" +
                        $"radius={_rangoKm * 1000}&" +
                        $"keyword=reciclaje+electronico+reciclaje+electronicos+ewaste&" +
                        $"key={_apiKey}";

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            var response = await httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<GooglePlaceResponse>(response);

            if (data == null)
            {
                Console.WriteLine("Respuesta nula de Google Places API");
                return;
            }

            switch (data.Status)
            {
                case "OK":
                    await ProcesarPuntosGooglePlaces(data.Results ?? new List<GooglePlace>());
                    _googlePlacesCargado = true;
                    break;

                case "ZERO_RESULTS":
                    Console.WriteLine("No se encontraron puntos de reciclaje adicionales en Google Places");
                    if (LblInfo != null)
                        LblInfo.Text = $"{_puntos.Count} puntos locales - Sin puntos adicionales";
                    break;

                case "OVER_QUERY_LIMIT":
                    Console.WriteLine("Límite de consultas excedido en Google Places API");
                    break;

                case "REQUEST_DENIED":
                    Console.WriteLine($"Acceso denegado: {data.ErrorMessage}");
                    break;

                default:
                    Console.WriteLine($"Estado de Google Places: {data.Status}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Google Places: {ex.Message}");
        }
    }

    private async Task ProcesarPuntosGooglePlaces(List<GooglePlace> places)
    {
        if (places == null || !places.Any()) return;

        int nuevosPuntos = 0;

        foreach (var place in places)
        {
            var loc = place.Geometry?.Location;
            if (loc != null)
            {
                bool yaExiste = _puntos.Any(p =>
                    Math.Abs(p.Lat - loc.Lat) < 0.001 &&
                    Math.Abs(p.Lng - loc.Lng) < 0.001);

                if (!yaExiste)
                {
                    var puntoGoogle = new PuntoReciclaje
                    {
                        Id = $"GOOGLE_{place.PlaceId}",
                        Nombre = place.Nombre ?? "Punto de Reciclaje",
                        Direccion = place.Direccion ?? "Dirección no disponible",
                        Comuna = "Google Places",
                        Region = "Región Metropolitana",
                        Lat = loc.Lat,
                        Lng = loc.Lng,
                        Residuos = new List<string> { "Mobile", "Television", "Battery", "Printer", "Player" },
                        Formato = "Centro de reciclaje",
                        Contacto = "Información en Google Maps",
                        Horario = place.OpeningHours?.OpenNow.HasValue == true ?
                                 (place.OpeningHours.OpenNow.Value ? "Abierto ahora" : "Cerrado") :
                                 "Consultar horarios",
                        Costo = "Consultar",
                        Web = ""
                    };

                    _puntos.Add(puntoGoogle);
                    nuevosPuntos++;
                }
            }
        }

        MostrarPuntosFiltrados();

        if (LblInfo != null)
        {
            if (nuevosPuntos > 0)
            {
                LblInfo.Text = $"{_puntos.Count} puntos ({nuevosPuntos} adicionales)";
            }
            else
            {
                LblInfo.Text = $"{_puntos.Count} puntos locales";
            }
        }

        await Task.CompletedTask;
    }

    private async Task ObtenerUbicacionYMostrarPuntos()
    {
        try
        {
            if (LblInfo != null)
                LblInfo.Text = "Obteniendo ubicación...";

            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    _ubicacionActual = new Location(location.Latitude, location.Longitude);
                    if (MapaPrincipal != null)
                    {
                        MapaPrincipal.MoveToRegion(MapSpan.FromCenterAndRadius(
                            _ubicacionActual, Distance.FromKilometers(_rangoKm)));
                    }

                    Console.WriteLine($"Ubicación obtenida: {_ubicacionActual.Latitude}, {_ubicacionActual.Longitude}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ubicación: {ex.Message}");
        }

        MostrarPuntosFiltrados();
        await Task.CompletedTask;
    }

    private async void OnRangoClicked(object sender, EventArgs e)
    {
        var rangosTexto = _rangosDisponibles.Select(r => $"{r} km").ToArray();

        var rangoSeleccionado = await DisplayActionSheet(
            "Seleccionar rango de búsqueda",
            "Cancelar",
            null,
            rangosTexto
        );

        if (rangoSeleccionado != null && rangoSeleccionado != "Cancelar")
        {
            var km = int.Parse(rangoSeleccionado.Replace(" km", ""));
            _rangoKm = km;
            if (LblRangoActual != null)
                LblRangoActual.Text = $"{km} km";

            if (MapaPrincipal != null)
            {
                MapaPrincipal.MoveToRegion(MapSpan.FromCenterAndRadius(
                    _ubicacionActual, Distance.FromKilometers(_rangoKm)));
            }

            await CargarPuntosDesdeGooglePlacesAsync();
        }
    }

    private async void OnFiltroClicked(object sender, EventArgs e)
    {
        await MostrarOpcionesFiltro();
    }

    private async void OnFiltroFooterClicked(object sender, EventArgs e)
    {
        await MostrarOpcionesFiltro();
    }

    private async Task MostrarOpcionesFiltro()
    {
        var dispositivo = await DisplayActionSheet(
            "¿Qué dispositivo quieres reciclar?",
            "Cancelar",
            null,
            _dispositivos
        );

        if (dispositivo != null && dispositivo != "Cancelar")
        {
            _filtroSeleccionado = dispositivo == "Todos los dispositivos" ? "All" : dispositivo;
            MostrarPuntosFiltrados();
        }
    }

    private IEnumerable<PuntoReciclaje> FiltrarPuntos()
    {
        if (string.IsNullOrEmpty(_filtroSeleccionado) || _filtroSeleccionado == "All")
            return _puntos;

        var keywords = ObtenerPalabrasClave(_filtroSeleccionado);

        return _puntos.Where(p =>
            p.Residuos != null &&
            p.Residuos.Any(r =>
                r != null && keywords.Any(k => r.Contains(k, StringComparison.OrdinalIgnoreCase))
            )
        );
    }

    private void MostrarPuntosFiltrados()
    {
        if (MapaPrincipal == null) return;

        MapaPrincipal.Pins.Clear();
        _puntosCombinados.Clear();

        var puntosFiltrados = FiltrarPuntos();
        var cantidadPuntos = puntosFiltrados.Count();

        if (LblContador != null)
            LblContador.Text = $"{cantidadPuntos} puntos";

        if (_filtroSeleccionado == "All")
        {
            if (LblFiltroActual != null)
                LblFiltroActual.Text = "Todos los dispositivos";
            if (LblInfo != null)
                LblInfo.Text = $"{cantidadPuntos} puntos disponibles ({_rangoKm} km)";
        }
        else
        {
            if (LblFiltroActual != null)
                LblFiltroActual.Text = _filtroSeleccionado;
            if (LblInfo != null)
                LblInfo.Text = $"{cantidadPuntos} puntos de {_filtroSeleccionado} ({_rangoKm} km)";
        }

        foreach (var punto in puntosFiltrados)
        {
            var pin = new Pin
            {
                Label = punto.Nombre ?? "Sin nombre",
                Address = $"{punto.Direccion ?? ""} - {punto.Comuna ?? ""}",
                Type = PinType.Place,
                Location = new Location(punto.Lat, punto.Lng)
            };

            pin.MarkerClicked += (s, args) =>
            {
                args.HideInfoWindow = false;
                MostrarDetallesPuntoEnMapa(punto);
            };

            MapaPrincipal.Pins.Add(pin);
            _puntosCombinados.Add(punto);
        }

        if (!puntosFiltrados.Any() && LblInfo != null)
        {
            LblInfo.Text = "No hay puntos que coincidan con el filtro";
        }
    }

    private async void MostrarDetallesPuntoEnMapa(PuntoReciclaje punto)
    {
        try
        {
            // ✅ CORREGIDO: Asegurar que los residuos estén sincronizados
            if (punto.Residuos == null || !punto.Residuos.Any())
            {
                // Si no hay residuos, intentar sincronizar desde AcceptsNotes
                punto.SincronizarDesdeBD();
            }

            string detalles = $"{punto.Nombre ?? "Sin nombre"}\n\n" +
                             $"Dirección: {punto.Direccion ?? "Sin dirección"}\n" +
                             $"Comuna: {punto.Comuna ?? "Sin comuna"}, {punto.Region ?? "Sin región"}\n\n" +
                             $"Contacto: {punto.Contacto ?? "No especificado"}\n" +
                             $"Horario: {punto.Horario ?? "No especificado"}\n" +
                             $"Costo: {punto.Costo ?? "Gratuito"}\n\n" +
                             $"Residuos aceptados: {(punto.Residuos != null && punto.Residuos.Any() ? string.Join(", ", punto.Residuos) : "No especificados")}";

            await DisplayAlert("Detalles del Punto", detalles, "Cerrar");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error mostrando detalles: {ex.Message}");
            await DisplayAlert("Error", "No se pudieron cargar los detalles del punto", "OK");
        }
    }

    private string[] ObtenerPalabrasClave(string dispositivo)
    {
        var mapa = new Dictionary<string, string[]>
        {
            { "Battery", new[] { "Battery", "Batteries", "Power Bank" } },
            { "Keyboard", new[] { "Keyboard" } },
            { "Microwave", new[] { "Microwave", "Microwave Oven" } },
            { "Mobile", new[] { "Mobile", "Cellphone", "Smartphone" } },
            { "Mouse", new[] { "Mouse", "Computer Mouse" } },
            { "PCB", new[] { "PCB", "Circuit Board", "Motherboard" } },
            { "Player", new[] { "Player", "Media Player", "Audio Player", "Video Player" } },
            { "Printer", new[] { "Printer", "Ink Cartridge", "Toner" } },
            { "Television", new[] { "Television", "TV", "Monitor" } },
            { "Washing Machine", new[] { "Washing Machine", "Washer" } }
        };

        return mapa.ContainsKey(dispositivo) ? mapa[dispositivo] : new[] { dispositivo };
    }

    private async void OnActualizarUbicacionClicked(object sender, EventArgs e)
    {
        try
        {
            if (LblInfo != null)
                LblInfo.Text = "Actualizando...";
            await ObtenerUbicacionYMostrarPuntos();

            if (!_googlePlacesCargado)
            {
                await CargarPuntosDesdeGooglePlacesAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al actualizar: {ex.Message}", "OK");
        }
    }

    // ✅ CORREGIDO: Método actualizado para usar filtrado local
    public Task MostrarFiltroDesdeIAAsync(string dispositivoDetectado)
    {
        if (string.IsNullOrEmpty(dispositivoDetectado))
            return Task.CompletedTask;

        _filtroSeleccionado = dispositivoDetectado;

        Dispatcher.Dispatch(MostrarPuntosFiltrados);

        return Task.CompletedTask;
    }

    // ============================================================
    // MÉTODOS PARA GESTIÓN CRUD - CORREGIDOS
    // ============================================================

    private async Task CargarPuntosParaGestionAsync()
    {
        try
        {
            var puntos = await _puntoService.ObtenerTodosAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _puntosParaGestion.Clear();
                foreach (var punto in puntos ?? new List<PuntoReciclaje>())
                {
                    _puntosParaGestion.Add(punto);
                }
                if (ListaGestionPuntos != null)
                    ListaGestionPuntos.ItemsSource = _puntosParaGestion;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando puntos para gestión: {ex.Message}");
        }
    }

    private void OnGestionFlotanteClicked(object sender, EventArgs e)
    {
        _panelGestionVisible = !_panelGestionVisible;
        if (PanelGestion != null)
            PanelGestion.IsVisible = _panelGestionVisible;

        if (_panelGestionVisible && PanelGestion != null)
        {
            PanelGestion.Scale = 0.8;
            PanelGestion.FadeTo(1, 200);
            PanelGestion.ScaleTo(1, 200);
        }
    }

    private void OnCerrarPanelClicked(object sender, EventArgs e)
    {
        _panelGestionVisible = false;
        if (PanelGestion != null)
            PanelGestion.IsVisible = false;
    }

    private void OnBusquedaGestionChanged(object sender, TextChangedEventArgs e)
    {
        var textoBusqueda = e.NewTextValue?.ToLower() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(textoBusqueda))
        {
            if (ListaGestionPuntos != null)
                ListaGestionPuntos.ItemsSource = _puntosParaGestion;
        }
        else
        {
            var filtrados = _puntosParaGestion.Where(p =>
                (p.Nombre?.ToLower().Contains(textoBusqueda) ?? false) ||
                (p.Comuna?.ToLower().Contains(textoBusqueda) ?? false) ||
                (p.Direccion?.ToLower().Contains(textoBusqueda) ?? false) ||
                (p.Residuos?.Any(r => r?.ToLower().Contains(textoBusqueda) ?? false) ?? false)
            ).ToList();

            if (ListaGestionPuntos != null)
                ListaGestionPuntos.ItemsSource = filtrados;
        }
    }

    private async void OnAgregarPuntoClicked(object sender, EventArgs e)
    {
        await MostrarFormularioCompletoPunto(null);
    }

    private async void OnEditarPuntoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PuntoReciclaje punto)
        {
            // Solo permitir editar puntos del CRUD (BD)
            if (!punto.EsPuntoCRUD)
            {
                await DisplayAlert("Información", "Solo se pueden editar puntos gestionados por el usuario", "OK");
                return;
            }
            await MostrarFormularioCompletoPunto(punto);
        }
    }

    private async void OnEliminarPuntoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PuntoReciclaje punto)
        {
            // Solo permitir eliminar puntos del CRUD (BD)
            if (!punto.EsPuntoCRUD)
            {
                await DisplayAlert("Información", "Solo se pueden eliminar puntos gestionados por el usuario", "OK");
                return;
            }

            bool confirmar = await DisplayAlert(
                "Confirmar Eliminación",
                $"¿Estás seguro de eliminar el punto '{punto.Nombre}'?",
                "Sí, eliminar", "Cancelar");

            if (confirmar)
            {
                var resultado = await _puntoService.EliminarAsync(punto.RecyclingPointId);
                if (resultado)
                {
                    await CargarPuntosParaGestionAsync();

                    // También eliminar del mapa si está visible
                    var puntoEnMapa = _puntos.FirstOrDefault(p => p.RecyclingPointId == punto.RecyclingPointId);
                    if (puntoEnMapa != null)
                    {
                        _puntos.Remove(puntoEnMapa);
                        MostrarPuntosFiltrados();
                    }

                    await DisplayAlert("Éxito", "Punto eliminado correctamente", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo eliminar el punto", "OK");
                }
            }
        }
    }

    // ✅ CORREGIDO: Método completo con todas las variables definidas
    private async Task MostrarFormularioCompletoPunto(PuntoReciclaje? puntoExistente)
    {
        try
        {
            string nombre = await DisplayPromptAsync(
                "Nombre del punto",
                "Ingrese el nombre del punto de reciclaje:",
                "Siguiente", "Cancelar",
                placeholder: puntoExistente?.Nombre ?? "Ej: Punto Verde Central",
                maxLength: 100) ?? "";

            if (string.IsNullOrWhiteSpace(nombre)) return;

            string direccion = await DisplayPromptAsync(
                "Dirección",
                "Ingrese la dirección completa:",
                "Siguiente", "Atrás",
                placeholder: puntoExistente?.Direccion ?? "Ej: Av. Principal 123, Santiago",
                maxLength: 200) ?? "";

            if (string.IsNullOrWhiteSpace(direccion)) return;

            string comuna = await DisplayPromptAsync(
                "Comuna",
                "Ingrese la comuna:",
                "Siguiente", "Atrás",
                placeholder: puntoExistente?.Comuna ?? "Ej: Santiago",
                maxLength: 50) ?? "";

            if (string.IsNullOrWhiteSpace(comuna)) return;

            string region = await DisplayPromptAsync(
                "Región",
                "Ingrese la región:",
                "Siguiente", "Atrás",
                placeholder: puntoExistente?.Region ?? "Ej: Metropolitana",
                maxLength: 50) ?? "";

            if (string.IsNullOrWhiteSpace(region)) return;

            string formato = await DisplayActionSheet(
                "Seleccione el formato del punto:",
                "Cancelar", null, _formatosDisponibles) ?? "";

            if (formato == "Cancelar" || string.IsNullOrEmpty(formato)) return;

            var residuosSeleccionados = await MostrarSelectorMultipleResiduos(
                puntoExistente?.Residuos ?? new List<string>());

            if (residuosSeleccionados == null || !residuosSeleccionados.Any())
            {
                await DisplayAlert("Advertencia", "Debe seleccionar al menos un tipo de residuo", "OK");
                return;
            }

            string telefono = await DisplayPromptAsync(
                "Teléfono",
                "Ingrese número de teléfono:",
                "Siguiente", "Atrás",
                placeholder: puntoExistente?.Telefono ?? "+56 9 1234 5678",
                maxLength: 20,
                keyboard: Keyboard.Telephone) ?? "";

            string email = await DisplayPromptAsync(
                "Email",
                "Ingrese dirección de email:",
                "Siguiente", "Saltar",
                 placeholder: puntoExistente?.Email ?? "email@ejemplo.com",
                 maxLength: 100,
                 keyboard: Keyboard.Email) ?? "";

            string website = await DisplayPromptAsync(
                "Sitio web",
                "Ingrese la página web (opcional):",
                "Siguiente", "Saltar",
                placeholder: puntoExistente?.Web ?? "Ej: www.mipunto.cl",
                maxLength: 100,
                keyboard: Keyboard.Url) ?? "";

            string horario = await DisplayPromptAsync(
                "Horario",
                "Ingrese el horario de atención:",
                "Siguiente", "Atrás",
                placeholder: puntoExistente?.Horario ?? "Ej: Lunes a Viernes 9:00-18:00",
                maxLength: 100) ?? "";

            string costo = await DisplayPromptAsync(
                "Costo",
                "Ingrese el costo (si aplica):",
                "Guardar", "Atrás",
                placeholder: puntoExistente?.Costo ?? "Ej: Gratuito",
                maxLength: 50) ?? "";

            // ✅ CORRECCIÓN: Generar contacto unificado para mostrar en confirmación
            var partesContacto = new List<string>();
            if (!string.IsNullOrWhiteSpace(telefono))
                partesContacto.Add($"Tel: {telefono}");
            if (!string.IsNullOrWhiteSpace(email))
                partesContacto.Add($"Email: {email}");
            if (!string.IsNullOrWhiteSpace(website))
                partesContacto.Add($"Web: {website}");

            string contactoParaMostrar = partesContacto.Any() ? string.Join(" | ", partesContacto) : "No especificado";

            bool confirmarGuardar = await DisplayAlert(
                "Confirmar Guardado",
                $"¿Guardar el punto '{nombre}'?\n\n" +
                $"Dirección: {direccion}\n" +
                $"Comuna: {comuna}, {region}\n" +
                $"Formato: {formato}\n" +
                $"Residuos: {string.Join(", ", residuosSeleccionados.Take(3))}" +
                $"{(residuosSeleccionados.Count() > 3 ? "..." : "")}\n" +
                $"Contacto: {contactoParaMostrar}",
                "Sí, guardar", "Cancelar");

            if (!confirmarGuardar) return;

            var punto = puntoExistente ?? new PuntoReciclaje();
            punto.Nombre = nombre;
            punto.Direccion = direccion;
            punto.Comuna = comuna;
            punto.Region = region;
            punto.Formato = formato;
            punto.Residuos = residuosSeleccionados.ToList();

            // ✅ CORRECCIÓN: Asignar propiedades separadas en lugar de Contacto
            punto.Telefono = telefono ?? string.Empty;
            punto.Email = email ?? string.Empty;
            punto.Web = website ?? string.Empty;
            punto.Horario = horario ?? "No especificado";
            punto.Costo = costo ?? "Gratuito";

            // La propiedad Contacto se generará automáticamente cuando se acceda a ella
            // NO asignar punto.Contacto directamente

            await MostrarGeocodingYUbicacion(punto, direccion, comuna, region);

            // Preguntar por tipos de aceptación para BD
            var aceptaReciclaje = await DisplayAlert("Tipo de Aceptación", "¿Acepta reciclaje?", "Sí", "No");
            var aceptaReuso = await DisplayAlert("Tipo de Aceptación", "¿Acepta reuso?", "Sí", "No");
            var aceptaCompra = await DisplayAlert("Tipo de Aceptación", "¿Acepta compra?", "Sí", "No");

            // Asignar propiedades para BD
            punto.AcceptsRecycle = aceptaReciclaje;
            punto.AcceptsReuse = aceptaReuso;
            punto.AcceptsBuyback = aceptaCompra;
            punto.Active = true;

            // ✅ CORRECCIÓN: No asignar AcceptsNotes manualmente - el servicio lo hará automáticamente
            // punto.AcceptsNotes se sincronizará automáticamente con Residuos en el servicio

            // GUARDAR EN BASE DE DATOS
            var resultado = puntoExistente == null
                ? await _puntoService.GuardarAsync(punto)
                : await _puntoService.ActualizarAsync(punto);

            if (resultado)
            {
                await ActualizarPuntosEnMapa(punto, puntoExistente == null);
                await CargarPuntosParaGestionAsync();

                if (punto.Lat != 0 && punto.Lng != 0 && MapaPrincipal != null)
                {
                    MapaPrincipal.MoveToRegion(MapSpan.FromCenterAndRadius(
                        new Location(punto.Lat, punto.Lng), Distance.FromKilometers(1)));
                }

                await DisplayAlert("Éxito",
                    puntoExistente == null ? "Punto agregado correctamente" : "Punto actualizado correctamente",
                    "OK");

                _panelGestionVisible = false;
                if (PanelGestion != null)
                    PanelGestion.IsVisible = false;
            }
            else
            {
                await DisplayAlert("Error", "No se pudo guardar el punto", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error en el formulario: {ex.Message}", "OK");
        }
    }

    private async Task<IEnumerable<string>?> MostrarSelectorMultipleResiduos(List<string> residuosActuales)
    {
        var residuosSeleccionados = new List<string>(residuosActuales ?? new List<string>());
        bool continuarSeleccion = true;

        while (continuarSeleccion)
        {
            var acciones = new List<string> { "Finalizar selección" };
            acciones.AddRange(_residuosDisponibles.Except(residuosSeleccionados));

            var seleccion = await DisplayActionSheet(
                $"Residuos aceptados ({residuosSeleccionados.Count} seleccionados)\nSeleccione un residuo a agregar:",
                "Cancelar", null, acciones.ToArray());

            if (seleccion == "Cancelar") return null;
            if (seleccion == "Finalizar selección") break;

            if (!string.IsNullOrEmpty(seleccion) && !residuosSeleccionados.Contains(seleccion))
            {
                residuosSeleccionados.Add(seleccion);

                if (residuosSeleccionados.Count > 0)
                {
                    await DisplayAlert("Residuo agregado",
                        $"{seleccion} agregado.\n\nResiduos seleccionados: {string.Join(", ", residuosSeleccionados)}",
                        "Continuar");
                }
            }
        }

        return residuosSeleccionados;
    }

    private async Task MostrarGeocodingYUbicacion(PuntoReciclaje punto, string direccion, string comuna, string region)
    {
        try
        {
            var locations = await Geocoding.GetLocationsAsync($"{direccion}, {comuna}, {region}");
            var location = locations?.FirstOrDefault();

            if (location != null)
            {
                punto.Lat = location.Latitude;
                punto.Lng = location.Longitude;

                await DisplayAlert("Ubicación confirmada",
                    $"Coordenadas obtenidas:\nLat: {punto.Lat:F6}\nLng: {punto.Lng:F6}",
                    "OK");
            }
            else
            {
                punto.Lat = _ubicacionActual.Latitude;
                punto.Lng = _ubicacionActual.Longitude;

                await DisplayAlert("Usando ubicación actual",
                    "No se pudo obtener coordenadas exactas de la dirección. Usando ubicación actual del mapa.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            punto.Lat = _ubicacionActual.Latitude;
            punto.Lng = _ubicacionActual.Longitude;
            Console.WriteLine($"Error en geocoding: {ex.Message}");
        }
    }

    private async Task ActualizarPuntosEnMapa(PuntoReciclaje punto, bool esNuevo)
    {
        try
        {
            if (esNuevo)
            {
                _puntos.Add(punto);
            }
            else
            {
                // Para edición, buscar por RecyclingPointId en lugar de Id
                var puntoExistente = _puntos.FirstOrDefault(p =>
                    p.EsPuntoCRUD && p.RecyclingPointId == punto.RecyclingPointId);

                if (puntoExistente != null)
                {
                    var index = _puntos.IndexOf(puntoExistente);
                    _puntos[index] = punto;
                }
                else
                {
                    _puntos.Add(punto);
                }
            }

            MostrarPuntosFiltrados();

            if (LblContador != null)
                LblContador.Text = $"{_puntos.Count} puntos";
            if (LblInfo != null)
                LblInfo.Text = $"{_puntos.Count} puntos disponibles ({_rangoKm} km)";

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error actualizando puntos en mapa: {ex.Message}");
            await DisplayAlert("Error", "No se pudo actualizar los puntos en el mapa", "OK");
        }
    }

    // ✅ CORREGIDO: Método único para cargar puntos del CRUD al mapa
    private async Task CargarPuntosCRUDAlMapa()
    {
        try
        {
            var puntosCRUD = await _puntoService.ObtenerTodosAsync();

            if (puntosCRUD != null && puntosCRUD.Any())
            {
                foreach (var puntoCRUD in puntosCRUD)
                {
                    // ✅ CORREGIDO: Asegurar sincronización de residuos
                    puntoCRUD.SincronizarDesdeBD();

                    // Verificar si ya existe en el mapa
                    bool yaExiste = _puntos.Any(p =>
                        p.EsPuntoCRUD && p.RecyclingPointId == puntoCRUD.RecyclingPointId);

                    if (!yaExiste)
                    {
                        _puntos.Add(puntoCRUD);
                    }
                }
                Console.WriteLine($"Cargados {puntosCRUD.Count()} puntos del CRUD al mapa");

                // ✅ ACTUALIZAR EL MAPA
                MostrarPuntosFiltrados();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando puntos CRUD al mapa: {ex.Message}");
        }
    }
}

// ✅ CLASES AUXILIARES PARA GOOGLE PLACES
public class GooglePlaceResponse
{
    [JsonProperty("results")]
    public List<GooglePlace>? Results { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("error_message")]
    public string? ErrorMessage { get; set; }
}

public class GooglePlace
{
    [JsonProperty("place_id")]
    public string? PlaceId { get; set; }

    [JsonProperty("name")]
    public string? Nombre { get; set; }

    [JsonProperty("vicinity")]
    public string? Direccion { get; set; }

    [JsonProperty("geometry")]
    public GoogleGeometry? Geometry { get; set; }

    [JsonProperty("opening_hours")]
    public GoogleOpeningHours? OpeningHours { get; set; }
}

public class GoogleGeometry
{
    [JsonProperty("location")]
    public GoogleLocation? Location { get; set; }
}

public class GoogleLocation
{
    [JsonProperty("lat")]
    public double Lat { get; set; }

    [JsonProperty("lng")]
    public double Lng { get; set; }
}

public class GoogleOpeningHours
{
    [JsonProperty("open_now")]
    public bool? OpenNow { get; set; }
}

