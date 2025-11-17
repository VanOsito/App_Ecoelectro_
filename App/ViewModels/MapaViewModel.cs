using App.Models;
using App.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace App.ViewModels
{
    public class MapaViewModel : BaseViewModel
    {
        private readonly IPuntoReciclajeService _puntoService;
        private double _latitudActual = -33.4489; // Santiago por defecto
        private double _longitudActual = -70.6693;
        private double _radioBusquedaKm = 5;

        public ObservableCollection<PuntoReciclaje> PuntosCercanos { get; } = new ObservableCollection<PuntoReciclaje>();

        public double LatitudActual
        {
            get => _latitudActual;
            set => SetProperty(ref _latitudActual, value);
        }

        public double LongitudActual
        {
            get => _longitudActual;
            set => SetProperty(ref _longitudActual, value);
        }

        public double RadioBusquedaKm
        {
            get => _radioBusquedaKm;
            set
            {
                SetProperty(ref _radioBusquedaKm, value);
                _ = BuscarPuntosCercanosAsync();
            }
        }

        public ICommand BuscarPuntosCercanosCommand { get; }
        public ICommand ActualizarUbicacionCommand { get; }

        public MapaViewModel(IPuntoReciclajeService puntoService)
        {
            _puntoService = puntoService;

            BuscarPuntosCercanosCommand = new Command(async () => await BuscarPuntosCercanosAsync());
            ActualizarUbicacionCommand = new Command(async () => await ActualizarUbicacionActualAsync());

            Title = "Mapa de Puntos de Reciclaje";

            // Cargar puntos al inicializar
            Task.Run(async () => await BuscarPuntosCercanosAsync());
        }

        private async Task BuscarPuntosCercanosAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var puntos = await _puntoService.BuscarPorUbicacionAsync(
                    LatitudActual, LongitudActual, RadioBusquedaKm);

                PuntosCercanos.Clear();
                foreach (var punto in puntos)
                {
                    PuntosCercanos.Add(punto);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al buscar puntos cercanos: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ActualizarUbicacionActualAsync()
        {
            // Aquí implementarías la obtención de la ubicación actual del dispositivo
            // Por ahora usamos valores por defecto
            await BuscarPuntosCercanosAsync();
        }

        // ✅ CORRECTO: Usa filtrado local después de obtener todos los puntos
        public async Task BuscarPorTipoResiduo(string tipoResiduo)
        {
            try
            {
                // Obtener TODOS los puntos y filtrar localmente
                var todosPuntos = await _puntoService.ObtenerTodosAsync();

                var puntosFiltrados = todosPuntos.Where(p =>
                    p.Residuos != null &&
                    p.Residuos.Any(r =>
                        r != null &&
                        r.Contains(tipoResiduo, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                PuntosCercanos.Clear();
                foreach (var punto in puntosFiltrados)
                {
                    // Calcular distancia para los puntos filtrados
                    punto.DistanciaKm = CalcularDistancia(LatitudActual, LongitudActual, punto.Lat, punto.Lng);
                    PuntosCercanos.Add(punto);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al buscar por residuo: {ex.Message}");
            }
        }

        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            const double radioTierraKm = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return radioTierraKm * c;
        }
    }
}