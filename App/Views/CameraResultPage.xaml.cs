using App.Services; // IImageClassifier
using App.Data;
using App.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using SkiaSharp;

namespace App.Views;

[QueryProperty(nameof(PhotoPath), "photoPath")]

public partial class CameraResultPage : ContentPage
{

    private readonly DatabaseService _db;
    private readonly IImageClassifier _clf;
    private string? _photoPath;

    public string? PhotoPath
    {
        get => _photoPath;
        set
        {
            _photoPath = value;
            _ = LoadAndClassifyAsync(); // dispara al setear
        }
    }

    public CameraResultPage(IImageClassifier clf)
    {
        InitializeComponent();
        _clf = clf;
        _db = new DatabaseService();
    }

    private async Task LoadAndClassifyAsync()
    {
        if (string.IsNullOrWhiteSpace(_photoPath) || !File.Exists(_photoPath))
        {
            ResultLabel.Text = "No se encontró la foto.";
            Preview.Source = null;
            return;
        }

        try
        {
            Busy.IsVisible = Busy.IsRunning = true;
            ResultLabel.Text = "Clasificando...";

            // Mostrar preview
            Preview.Source = ImageSource.FromFile(_photoPath);

            // Clasificar
            using var fs = File.OpenRead(_photoPath);
            var (label, prob, top3) = await _clf.PredictAsync(fs);

            // Frase: “en la foto hay un <label>”
            ResultLabel.Text = $"En la foto hay un {label} ({prob:P1}).";

            var componentes = await _db.ObtenerComponentesPorDispositivoAsync(label);

            // Fallback si no hay mapeo todavía
            if (componentes.Count == 0)
            {
                componentes.Add(new ComponentInfo
                {
                    Nombre = "Sin componentes registrados",
                    Estado = "—",
                    Descripcion = "Este dispositivo aún no tiene componentes en catálogo."
                });
            }

            ComponentesList.ItemsSource = componentes;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
            ResultLabel.Text = "No se pudo clasificar la imagen.";
            Preview.Source = null;
        }
        finally
        {
            Busy.IsVisible = Busy.IsRunning = false;
        }
    }

    private async void OnClassifyAgain(object sender, EventArgs e)
    {
        await LoadAndClassifyAsync();
    }
}