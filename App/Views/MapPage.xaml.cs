using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using App.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.ApplicationModel;
using Newtonsoft.Json;
namespace App.Views;

public partial class MapPage : ContentPage
{
    private readonly string apiKey = "AIzaSyAoDxr91Rqn_b6bIW4f5jz6Yl3SvKs8pe4";
    private ObservableCollection<PuntoReciclaje> _puntos = new();
    private ObservableCollection<PuntoReciclaje> _puntosFiltrados = new();
    private Location _ubicacionActual = new(-33.4489, -70.6693);
    private string _filtroSeleccionado = "Todos los RAEE";

    public MapPage()
	{
		InitializeComponent();
        cvPlaces.ItemsSource = _puntosFiltrados;
        CargarFiltroPicker();
    }
    private void CargarFiltroPicker()
    {
        var dispositivos = new string[]
        {
                "Todos los RAEE",
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

        FiltroPicker.ItemsSource = dispositivos;
        FiltroPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ObtenerUbicacionYMostrarPuntos();
    }

    private async Task ObtenerUbicacionYMostrarPuntos()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.GetLocationAsync(request);
                _ubicacionActual = location ?? _ubicacionActual;
            }
        }
        catch
        {
            _ubicacionActual = new Location(-33.4489, -70.6693);
        }

        MapaPrincipal.MoveToRegion(MapSpan.FromCenterAndRadius(_ubicacionActual, Distance.FromKilometers(15)));

        // Cargar ambas fuentes
        await CargarPuntosDesdeJsonAsync();
        await CargarPuntosDesdeGooglePlacesAsync();
    }

    private async Task CargarPuntosDesdeJsonAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("puntos_reciclaje.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var puntos = JsonConvert.DeserializeObject<ObservableCollection<PuntoReciclaje>>(json)
                         ?? new ObservableCollection<PuntoReciclaje>();

            _puntos = puntos;
            MostrarPuntosFiltrados();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar puntos: {ex.Message}", "OK");
        }
    }

    private async Task CargarPuntosDesdeGooglePlacesAsync()
    {
        try
        {
            string keyword = _filtroSeleccionado switch
            {
                "Celular" => "reciclaje+celulares",
                "Computador" => "reciclaje+computadores",
                "Televisor" => "reciclaje+televisores",
                _ => "punto+de+reciclaje"
            };

            string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={_ubicacionActual.Latitude},{_ubicacionActual.Longitude}&radius=5000&keyword={keyword}&key={apiKey}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<GooglePlaceResponse>(response);

            if (data?.Results != null)
            {
                foreach (var place in data.Results)
                {
                    var loc = place.Geometry?.Location;
                    if (loc != null)
                    {
                        var pin = new Pin
                        {
                            Label = place.Nombre ?? "Sin nombre",
                            Address = place.Direccion ?? "Sin dirección",
                            Location = new Location(loc.Lat, loc.Lng),
                            Type = PinType.Place
                        };

                        MapaPrincipal.Pins.Add(pin);
                        _puntosFiltrados.Add(new PuntoReciclaje
                        {
                            Nombre = place.Nombre ?? "Sin nombre",
                            Direccion = place.Direccion ?? "Sin dirección",
                            Lat = loc.Lat,
                            Lng = loc.Lng
                        });
                    }
                }
            }

            LblInfo.Text = $"Mostrando {_puntosFiltrados.Count} puntos combinados - Filtro: {_filtroSeleccionado}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar desde Google Places: {ex.Message}", "OK");
        }
    }

    private void MostrarPuntosFiltrados()
    {
        if (MapaPrincipal == null) return;

        var puntosFiltrados = FiltrarPuntos();

        var puntosConDistancia = puntosFiltrados.Select(p =>
        {
            var puntoLocation = new Location(p.Lat, p.Lng);
            p.DistanciaKm = _ubicacionActual.CalculateDistance(puntoLocation, DistanceUnits.Kilometers);
            return p;
        }).OrderBy(p => p.DistanciaKm).ToList();

        foreach (var punto in puntosConDistancia)
        {
            _puntosFiltrados.Add(punto);

            var pin = new Pin
            {
                Label = punto.Nombre ?? "Sin nombre",
                Address = $"{punto.Direccion ?? ""} - {punto.Comuna ?? ""}",
                Location = new Location(punto.Lat, punto.Lng),
                Type = PinType.Place
            };

            MapaPrincipal.Pins.Add(pin);
        }

        LblInfo.Text = $"?? Mostrando {_puntosFiltrados.Count} puntos combinados - Filtro: {_filtroSeleccionado}";
    }

    private IEnumerable<PuntoReciclaje> FiltrarPuntos()
    {
        if (string.IsNullOrEmpty(_filtroSeleccionado) || _filtroSeleccionado == "Todos los RAEE")
            return _puntos;

        var keywords = ObtenerPalabrasClave(_filtroSeleccionado);

        return _puntos.Where(p =>
            p.Residuos != null &&
            p.Residuos.Any(r =>
                r != null && keywords.Any(k =>
                    r.Contains(k, StringComparison.OrdinalIgnoreCase))
            )
        );
    }

    private string[] ObtenerPalabrasClave(string dispositivo)
    {
        var mapa = new Dictionary<string, string[]>
            {
                { "Celular", new[] { "Mobile", "Cellphone", "Smartphone" } },
                { "Computador", new[] { "Keyboard", "Mouse", "PCB", "Printer" } },
                { "Televisor", new[] { "Television", "TV", "Monitor" } },
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

    private Color ObtenerColorPorResiduo(List<string> residuos)
    {
        if (residuos == null || !residuos.Any())
            return Color.FromArgb("#805AD5"); // Morado por defecto

        // Morado para electrónicos generales
        if (residuos.Any(r =>
            r.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Television", StringComparison.OrdinalIgnoreCase)))
            return Color.FromArgb("#805AD5"); // Morado

        // Verde para baterías y componentes verdes
        if (residuos.Any(r =>
            r.Contains("Battery", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("PCB", StringComparison.OrdinalIgnoreCase)))
            return Color.FromArgb("#38A169"); // Verde

        // Café para electrodomésticos grandes
        if (residuos.Any(r =>
            r.Contains("Washing", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Microwave", StringComparison.OrdinalIgnoreCase)))
            return Color.FromArgb("#A0522D"); // Café

        return Color.FromArgb("#805AD5"); // Morado por defecto
    }

    private async void OnActualizarUbicacionClicked(object sender, EventArgs e)
    {
        await ObtenerUbicacionYMostrarPuntos();
    }

    private async void OnFiltroCambiado(object sender, EventArgs e)
    {
        _filtroSeleccionado = FiltroPicker?.SelectedItem?.ToString() ?? "Todos los RAEE";
        _puntosFiltrados.Clear();
        MapaPrincipal.Pins.Clear();
        await ObtenerUbicacionYMostrarPuntos();
    }

    private void CvPlaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.FirstOrDefault() as PuntoReciclaje;
        if (selected != null)
        {
            var loc = new Location(selected.Lat, selected.Lng);
            MapaPrincipal.MoveToRegion(MapSpan.FromCenterAndRadius(loc, Distance.FromMeters(500)));
        }
    }
}
