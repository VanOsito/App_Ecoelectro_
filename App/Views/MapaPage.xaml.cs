using App.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

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
    private int _rangoKm = 10; // Rango por defecto: 10km
    private readonly int[] _rangosDisponibles = { 5, 10, 15, 25, 50 }; // Rangos en km

    // Lista de dispositivos para el filtro - EN INGLÉS
    private readonly string[] _dispositivos = {
        "All devices",
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

    public MapaPage()
    {
        InitializeComponent();
        _ = CargarDatosCompletosAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosCompletosAsync();
    }

    private async Task CargarDatosCompletosAsync()
    {
        try
        {
            await CargarPuntosDesdeJsonAsync();
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
            LblInfo.Text = "Cargando puntos locales...";

            using var stream = await FileSystem.OpenAppPackageFileAsync("puntos_reciclaje.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var puntos = JsonConvert.DeserializeObject<ObservableCollection<PuntoReciclaje>>(json)
                        ?? new ObservableCollection<PuntoReciclaje>();

            _puntos = puntos;
            await ObtenerUbicacionYMostrarPuntos();

            Console.WriteLine($"? Cargados {_puntos.Count} puntos desde JSON local");
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
            // Verificar conexión a internet
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                Console.WriteLine("?? Sin conexión a internet - No se cargarán puntos de Google Places");
                return;
            }

            LblInfo.Text = "Buscando puntos adicionales...";

            string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?" +
                        $"location={_ubicacionActual.Latitude},{_ubicacionActual.Longitude}&" +
                        $"radius={_rangoKm * 1000}&" + // Convertir km a metros
                        $"keyword=reciclaje+electronico+reciclaje+electronicos+ewaste&" +
                        $"key={_apiKey}";

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            var response = await httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<GooglePlaceResponse>(response);

            if (data == null)
            {
                Console.WriteLine("? Respuesta nula de Google Places API");
                return;
            }

            switch (data.Status)
            {
                case "OK":
                    await ProcesarPuntosGooglePlaces(data.Results);
                    _googlePlacesCargado = true;
                    break;

                case "ZERO_RESULTS":
                    Console.WriteLine("?? No se encontraron puntos de reciclaje adicionales en Google Places");
                    LblInfo.Text = $"{_puntos.Count} puntos locales - Sin puntos adicionales";
                    break;

                case "OVER_QUERY_LIMIT":
                    Console.WriteLine("? Límite de consultas excedido en Google Places API");
                    break;

                case "REQUEST_DENIED":
                    Console.WriteLine($"? Acceso denegado: {data.ErrorMessage}");
                    break;

                default:
                    Console.WriteLine($"? Estado de Google Places: {data.Status}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error Google Places: {ex.Message}");
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
                // Verificar si no existe ya en nuestros puntos JSON
                bool yaExiste = _puntos.Any(p =>
                    Math.Abs(p.Lat - loc.Lat) < 0.001 &&
                    Math.Abs(p.Lng - loc.Lng) < 0.001);

                if (!yaExiste)
                {
                    // Crear punto desde Google Places
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

        if (nuevosPuntos > 0)
        {
            LblInfo.Text = $"{_puntos.Count} puntos ({nuevosPuntos} adicionales)";
        }
        else
        {
            LblInfo.Text = $"{_puntos.Count} puntos locales";
        }

        await Task.CompletedTask;
    }

    private async Task ObtenerUbicacionYMostrarPuntos()
    {
        try
        {
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
                    MapaPrincipal.MoveToRegion(MapSpan.FromCenterAndRadius(
                        _ubicacionActual, Distance.FromKilometers(_rangoKm)));

                    Console.WriteLine($"?? Ubicación obtenida: {_ubicacionActual.Latitude}, {_ubicacionActual.Longitude}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"?? Error ubicación: {ex.Message}");
        }

        MostrarPuntosFiltrados();
        await Task.CompletedTask;
    }

    // NUEVO: Método para selector de rango
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
            LblRangoActual.Text = $"{km} km";

            // Actualizar vista del mapa
            MapaPrincipal.MoveToRegion(MapSpan.FromCenterAndRadius(
                _ubicacionActual, Distance.FromKilometers(_rangoKm)));

            // Recargar puntos de Google Places con nuevo rango
            await CargarPuntosDesdeGooglePlacesAsync();
        }
    }

    private async void OnFiltroClicked(object sender, EventArgs e)
    {
        var dispositivo = await DisplayActionSheet(
            "¿Qué dispositivo quieres reciclar?",
            "Cancelar",
            null,
            _dispositivos
        );

        if (dispositivo != null && dispositivo != "Cancelar")
        {
            _filtroSeleccionado = dispositivo == "All devices" ? "All" : dispositivo;
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

        // Actualizar UI
        LblContador.Text = $"{cantidadPuntos} puntos";

        if (_filtroSeleccionado == "All")
        {
            LblFiltroActual.Text = "Mostrando: Todos los dispositivos";
            LblInfo.Text = $"{cantidadPuntos} puntos disponibles ({_rangoKm} km)";
        }
        else
        {
            LblFiltroActual.Text = $"Mostrando: {_filtroSeleccionado}";
            LblInfo.Text = $"{cantidadPuntos} puntos encontrados ({_rangoKm} km)";
        }

        // Mostrar pines en el mapa
        foreach (var punto in puntosFiltrados)
        {
            var pin = new Pin
            {
                Label = punto.Nombre ?? "Sin nombre",
                Address = $"{punto.Direccion ?? ""} - {punto.Comuna ?? ""}",
                Type = PinType.Place,
                Location = new Location(punto.Lat, punto.Lng)
            };

            MapaPrincipal.Pins.Add(pin);
            _puntosCombinados.Add(punto);
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

    public Task MostrarFiltroDesdeIAAsync(string dispositivoDetectado)
    {
        if (string.IsNullOrEmpty(dispositivoDetectado))
            return Task.CompletedTask;

        _filtroSeleccionado = dispositivoDetectado;

        Dispatcher.Dispatch(MostrarPuntosFiltrados);

        return Task.CompletedTask;
    }
}

