namespace App.Views;

public partial class MapaWebPage : ContentPage
{
    private const string apiKey = "AIzaSyAoDxr91Rqn_b6bIW4f5jz6Yl3SvKs8pe4";
    private Location _ubicacionActual = new(-33.4489, -70.6693);
    public MapaWebPage()
	{
		InitializeComponent();
        CargarMapaWeb();
    }
    private void CargarMapaWeb()
    {
        var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8' />
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Mapa Reciclaje</title>
                <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=places&callback=initMap' async defer></script>
                <style>
                    body {{ margin: 0; padding: 0; }}
                    #map {{ height: 100vh; width: 100%; }}
                </style>
            </head>
            <body>
                <div id='map'></div>
                <script>
                    function initMap() {{
                        const center = {{ lat: {_ubicacionActual.Latitude}, lng: {_ubicacionActual.Longitude} }};
                        
                        const map = new google.maps.Map(document.getElementById('map'), {{
                            zoom: 12,
                            center: center,
                            mapTypeId: google.maps.MapTypeId.ROADMAP
                        }});

                        // Marcador de ubicación actual
                        new google.maps.Marker({{
                            position: center,
                            map: map,
                            title: 'Tu ubicación',
                            icon: {{
                                url: 'http://maps.google.com/mapfiles/ms/icons/blue-dot.png'
                            }}
                        }});

                        // Buscar puntos de reciclaje
                        const service = new google.maps.places.PlacesService(map);
                        const request = {{
                            location: center,
                            radius: 5000,
                            keyword: 'punto de reciclaje electrodomestico'
                        }};

                        service.nearbySearch(request, (results, status) => {{
                            if (status === google.maps.places.PlacesServiceStatus.OK) {{
                                results.forEach(place => {{
                                    new google.maps.Marker({{
                                        map: map,
                                        position: place.geometry.location,
                                        title: place.name,
                                        icon: {{
                                            url: 'http://maps.google.com/mapfiles/ms/icons/green-dot.png'
                                        }}
                                    }});
                                }});
                            }}
                        }});
                    }}
                </script>
            </body>
            </html>";

        MapaWebView.Source = new HtmlWebViewSource { Html = htmlContent };
    }

    private async void OnActualizarUbicacionClicked(object sender, EventArgs e)
    {
        await ObtenerYActualizarUbicacion();
    }

    private async Task ObtenerYActualizarUbicacion()
    {
        try
        {
            var ubicacion = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
            if (ubicacion != null)
            {
                _ubicacionActual = ubicacion;
                CargarMapaWeb();
                await DisplayAlert("Éxito", "Ubicación actualizada", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo obtener ubicación: {ex.Message}", "OK");
        }
    }
}