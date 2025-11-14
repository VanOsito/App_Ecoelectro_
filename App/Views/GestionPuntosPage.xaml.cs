using App.ViewModels;
using App.Models;
using Microsoft.Maui.Controls;

namespace App.Views
{
    public partial class GestionPuntosPage : ContentPage
    {
        public GestionPuntosPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is GestionPuntosViewModel viewModel)
            {
                await viewModel.CargarPuntosAsync();
            }
        }

        private async void OnPuntoSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PuntoReciclaje puntoSeleccionado)
            {
                await MostrarDetallesPunto(puntoSeleccionado);

                if (sender is CollectionView collectionView)
                {
                    collectionView.SelectedItem = null;
                }
            }
        }

        private async Task MostrarDetallesPunto(PuntoReciclaje punto)
        {
            if (punto == null) return;

            string detalles = $"{punto.Nombre ?? "Sin nombre"}\n\n" +
                             $"Dirección: {punto.Direccion ?? "Sin dirección"}\n" +
                             $"Comuna: {punto.Comuna ?? "Sin comuna"}, {punto.Region ?? "Sin región"}\n\n" +
                             $"Contacto: {punto.Contacto ?? "No especificado"}\n" +
                             $"Horario: {punto.Horario ?? "No especificado"}\n" +
                             $"Costo: {punto.Costo ?? "Gratuito"}\n\n" +
                             $"Residuos aceptados: {string.Join(", ", punto.Residuos ?? new List<string>())}";

            await DisplayAlert("Detalles del Punto", detalles, "Cerrar");
        }
    }
}