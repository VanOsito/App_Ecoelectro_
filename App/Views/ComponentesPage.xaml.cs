using App.Data;
using App.Models;
using App.Services;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace App.Views;

public partial class ComponentesPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly BlobStorageService _blob;

    public ObservableCollection<DetectionWithComponents> Detections { get; } = new();

    public ComponentesPage(DatabaseService db, BlobStorageService blob)
    {
        InitializeComponent();
        _db = db;
        _blob = blob;
        BindingContext = this;
        DetectionsList.ItemsSource = Detections;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDetectionsAsync();
    }

    private async Task LoadDetectionsAsync()
    {
        Detections.Clear();

        var user = App.UsuarioEnSesion;
        if (user == null)
        {
            await DisplayAlert("Sesión", "No hay usuario en sesión.", "OK");
            await Navigation.PopAsync();
            return;
        }

        // Solo administrador ve todas las detecciones
        var esAdmin = string.Equals(user.Correo, "admin@admin.com", StringComparison.OrdinalIgnoreCase);

        if (!esAdmin)
        {
            // Protegemos la página por si se accede por ruta directa
            await DisplayAlert("Acceso denegado", "No tienes permisos para ver todas las detecciones.", "OK");
            await Navigation.PopAsync();
            return;
        }

        // Admin: cargar todas las detecciones
        var dets = await _db.GetAllDetectionsAsync();

        foreach (var d in dets)
        {
            var comps = await _db.ObtenerComponentesPorDispositivoAsync(d.DispositivoNombre ?? "");
            var item = new DetectionWithComponents
            {
                Detection = d,
                Components = new ObservableCollection<ComponentInfo>(comps)
            };
            Detections.Add(item);
        }
    }

    private async void OnDeleteDetectionClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int detId)
        {
            await DeleteDetectionAsync(detId);
        }
    }

    private async Task DeleteDetectionAsync(int detectionId)
    {
        var item = Detections.FirstOrDefault(x => x.Detection.DetectionId == detectionId);
        if (item == null) return;

        var confirm = await DisplayAlert("Confirmar", "¿Eliminar la detección y su imagen asociada?", "Eliminar", "Cancelar");
        if (!confirm) return;

        // Intentar borrar el blob (siempre que exista la URL)
        var imageUrl = item.Detection.ImageUrl;
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            await _blob.DeleteBlobAsync(imageUrl);
        }

        var ok = await _db.DeleteDetectionAsync(detectionId);
        if (ok)
        {
            Detections.Remove(item);
            await Toast.Make("Detección eliminada.").Show();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo eliminar la detección desde la base de datos.", "OK");
        }
    }
}