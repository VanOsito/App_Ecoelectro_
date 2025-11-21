using App.Models;
using App.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace App.ViewModels
{
    public class GestionPuntosViewModel : BaseViewModel
    {
        private readonly IPuntoReciclajeService _puntoService;
        private string _textoBusqueda = string.Empty;

        public ObservableCollection<PuntoReciclaje> Puntos { get; } = new();
        public ObservableCollection<PuntoReciclaje> PuntosFiltrados { get; } = new();

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                SetProperty(ref _textoBusqueda, value ?? string.Empty);
                FiltrarPuntos();
            }
        }

        // Comandos
        public ICommand CargarPuntosCommand { get; }
        public ICommand AgregarPuntoCommand { get; }
        public ICommand EditarPuntoCommand { get; }
        public ICommand EliminarPuntoCommand { get; }
        public ICommand VerEnMapaCommand { get; }
        public ICommand BuscarCommand { get; }

        public GestionPuntosViewModel(IPuntoReciclajeService puntoService)
        {
            _puntoService = puntoService;

            // Inicializar comandos
            CargarPuntosCommand = new Command(async () => await CargarPuntosAsync());
            AgregarPuntoCommand = new Command(async () => await AgregarPuntoAsync());
            EditarPuntoCommand = new Command<PuntoReciclaje>(async (p) => await EditarPuntoAsync(p));
            EliminarPuntoCommand = new Command<PuntoReciclaje>(async (p) => await EliminarPuntoAsync(p));
            VerEnMapaCommand = new Command<PuntoReciclaje>(async (p) => await VerEnMapaAsync(p));
            BuscarCommand = new Command(() => FiltrarPuntos());

            Title = "Gestión de Puntos de Reciclaje";

            // Cargar datos iniciales
            Task.Run(async () => await CargarPuntosAsync());
        }

        public async Task CargarPuntosAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var puntos = await _puntoService.ObtenerTodosAsync();

                Puntos.Clear();
                foreach (var punto in puntos)
                {
                    if (punto != null)
                    {
                        Puntos.Add(punto);
                    }
                }

                FiltrarPuntos();
            }
            catch (Exception ex)
            {
                await MostrarAlerta("Error", $"Error al cargar puntos: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void FiltrarPuntos()
        {
            PuntosFiltrados.Clear();

            if (string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                foreach (var punto in Puntos)
                {
                    if (punto != null)
                    {
                        PuntosFiltrados.Add(punto);
                    }
                }
            }
            else
            {
                var busqueda = TextoBusqueda.ToLower();
                var filtrados = Puntos.Where(p =>
                    p != null &&
                    ((p.Nombre?.ToLower().Contains(busqueda) ?? false) ||
                    (p.Comuna?.ToLower().Contains(busqueda) ?? false) ||
                    (p.Direccion?.ToLower().Contains(busqueda) ?? false) ||
                    (p.Residuos?.Any(r => !string.IsNullOrEmpty(r) && r.ToLower().Contains(busqueda)) ?? false))
                );

                foreach (var punto in filtrados)
                {
                    if (punto != null)
                    {
                        PuntosFiltrados.Add(punto);
                    }
                }
            }
        }

        private async Task AgregarPuntoAsync()
        {
            await MostrarAlerta("Agregar", "Funcionalidad para agregar nuevo punto");
        }

        private async Task EditarPuntoAsync(PuntoReciclaje? punto)
        {
            if (punto != null && !string.IsNullOrEmpty(punto.Nombre))
            {
                await MostrarAlerta("Editar", $"Editar punto: {punto.Nombre}");
            }
        }

        private async Task EliminarPuntoAsync(PuntoReciclaje? punto)
        {
            if (punto != null && !string.IsNullOrEmpty(punto.Id) && !string.IsNullOrEmpty(punto.Nombre))
            {
                bool confirmar = await MostrarConfirmacion(
                    "Confirmar Eliminación",
                    $"¿Estás seguro de eliminar el punto '{punto.Nombre}'?",
                    "Sí", "No");

                if (confirmar)
                {
                    var resultado = await _puntoService.EliminarAsync(punto.Id);
                    if (resultado)
                    {
                        Puntos.Remove(punto);
                        FiltrarPuntos();
                        await MostrarAlerta("Éxito", "Punto eliminado correctamente");
                    }
                    else
                    {
                        await MostrarAlerta("Error", "No se pudo eliminar el punto");
                    }
                }
            }
        }

        private async Task VerEnMapaAsync(PuntoReciclaje? punto)
        {
            if (punto != null && !string.IsNullOrEmpty(punto.Nombre))
            {
                await MostrarAlerta("Mapa", $"Ver en mapa: {punto.Nombre}");
            }
        }

        // ✅ MÉTODOS COMPLETAMENTE CORREGIDOS - SIN POSIBLES NULLS
        private async Task MostrarAlerta(string titulo, string mensaje)
        {
            try
            {
                // Verificación completa de nulos
                if (Application.Current?.Windows == null ||
                    Application.Current.Windows.Count == 0 ||
                    Application.Current.Windows[0]?.Page == null)
                {
                    System.Diagnostics.Debug.WriteLine("No se puede mostrar alerta: Windows o Page es nulo");
                    return;
                }

                var page = Application.Current.Windows[0].Page;
                if (page != null)
                {
                    await page.DisplayAlert(titulo ?? "Alerta", mensaje ?? "Mensaje no disponible", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando alerta: {ex.Message}");
            }
        }

        private async Task<bool> MostrarConfirmacion(string titulo, string mensaje, string aceptar, string cancelar)
        {
            try
            {
                // Verificación completa de nulos
                if (Application.Current?.Windows == null ||
                    Application.Current.Windows.Count == 0 ||
                    Application.Current.Windows[0]?.Page == null)
                {
                    System.Diagnostics.Debug.WriteLine("No se puede mostrar confirmación: Windows o Page es nulo");
                    return false;
                }

                var page = Application.Current.Windows[0].Page;
                if (page != null)
                {
                    return await page.DisplayAlert(
                        titulo ?? "Confirmar",
                        mensaje ?? "¿Estás seguro?",
                        aceptar ?? "Sí",
                        cancelar ?? "No");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando confirmación: {ex.Message}");
            }

            return false;
        }
    }
}