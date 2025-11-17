using App.Data;
using App.Models;
using App.Services; // IImageClassifier
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching; // si necesitas MainThread
using Microsoft.Maui.ApplicationModel; // Launcher, Clipboard
using CommunityToolkit.Maui.Alerts;

namespace App.Views;

[QueryProperty(nameof(PhotoPath), "photoPath")]

public partial class CameraResultPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly IImageClassifier _clf;
    private string? _photoPath;

    private readonly BlobStorageService _blobStorage;


    // Mantengo Empresas por compatibilidad aunque la UI use ComponentGroups
    public ObservableCollection<CompanyPickup> Empresas { get; } = new();

    // Nueva colección agrupada para la UI
    public ObservableCollection<ComponentGroup> ComponentGroups { get; } = new();

    public string? PhotoPath
    {
        get => _photoPath;
        set
        {
            _photoPath = value;
            _ = LoadAndClassifyAsync(); // dispara al setear
        }
    }

    public string? PredictedLabel { get; set; }
    public double PredictedConfidence { get; set; }
    private bool _initialized;


    public CameraResultPage(IImageClassifier clf, BlobStorageService blobStorage, DatabaseService db)
    {
        InitializeComponent();
        _clf = clf;
        _db = new DatabaseService();
        BindingContext = this;
        _db = db;
        _blobStorage = blobStorage;


        // Asegura la fuente de datos (opcional, el binding XAML ya lo hace)
        GroupedList.ItemsSource = ComponentGroups;
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
            ResultLabel.Text = $"En la foto hay un {TraducirLabel(label)}.";
            PredictedLabel = label;
            PredictedConfidence = prob;
            OnPropertyChanged(nameof(PredictedLabel));
            OnPropertyChanged(nameof(PredictedConfidence));

            // 0) Validar usuario en sesión
            var user = App.UsuarioEnSesion;
            if (user == null)
            {
                await DisplayAlert("Sesión", "No se detectó usuario en sesión.", "OK");
                return;
            }

            // 1) Subir imagen al Blob privado (con SAS). 
            //    Usa un nombre ordenado por usuario/fecha:
            var blobName = $"u{user.Id}/{DateTime.UtcNow:yyyy/MM/dd}/photo_{DateTime.UtcNow:HHmmssfff}.jpg";
            var blobUrl = await _blobStorage.UploadFileAsync(_photoPath, blobName);

            // 2) Buscar dispositivo_id
            var deviceId = await _db.GetDeviceIdByLabelAsync(label);
            if (!deviceId.HasValue)
            {
                await DisplayAlert("Aviso", $"Dispositivo '{label}' no está en catálogo.", "OK");
                // Igual puedes quedarte mostrando componentes si quieres, pero no insertará detección.
            }
            else
            {
                // 3) Insertar detección
                var status = prob >= 0.70 ? "OK" : "LOW_CONF";
                var detId = await _db.InsertDetectionAsync(
                    userId: user.Id,
                    dispositivoId: deviceId.Value,
                    imageUrl: blobUrl,        // URL firmada con SAS
                    status: status,
                    confidence: prob
                );
                //  Asignar puntos al usuario actual
                await _db.AsignarPuntosPorDeteccionUsuarioAsync(user.Id);
                await Toast.Make("Has ganado 100 puntos por tu detección.").Show();

                // 4) (Opcional) borrar archivo local
                try { File.Delete(_photoPath); } catch { /* no-op */ }

                await Toast.Make($"Detección guardada (id {detId}).").Show();
            }

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

            // Cargar TODAS las empresas para esos componentes en una sola consulta,
            // luego agrupar para armar ComponentGroups
            var compIds = componentes.Where(c => c.Id > 0).Select(c => c.Id).ToArray();
            var rows = compIds.Length > 0
                ? await _db.GetCompaniesForComponentsAsync(compIds, null, null)
                : new List<CompanyPickup>();

            // Actualizar UI agrupada
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ComponentGroups.Clear();
                foreach (var comp in componentes)
                {
                    // <-- Dedupe por PickupCompanyId para evitar repeticiones dentro del mismo componente
                    var companiesForComp = rows
                        .Where(r => r.ComponenteId == comp.Id)
                        .GroupBy(r => r.PickupCompanyId)
                        .Select(g => g.First())
                        .ToList();

                    // Si no hay empresas, agrego un placeholder para que el usuario vea el mensaje
                    if (companiesForComp.Count == 0)
                    {
                        companiesForComp.Add(new CompanyPickup
                        {
                            Nombre = "No se encontraron lugares registrados para este componente.",
                            CoverageNotes = ""
                        });
                    }

                    ComponentGroups.Add(new ComponentGroup
                    {
                        Component = comp,
                        Companies = new ObservableCollection<CompanyPickup>(companiesForComp)
                    });
                }
            });
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

    private async void OnWebsiteTapped(object sender, EventArgs e)
    {
        if (sender is Label lbl && lbl.BindingContext is CompanyPickup cp)
        {
            var url = (cp.Website ?? "").Trim();
            if (string.IsNullOrWhiteSpace(url))
                return;

            // Añadir esquema si falta
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                await DisplayAlert("Aviso", "La URL no es válida.", "OK");
                return;
            }

            try
            {
                await Launcher.OpenAsync(uri);
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "No se pudo abrir el sitio web.", "OK");
            }
        }
    }

    private async void OnPhoneTapped(object sender, EventArgs e)
    {
        if (sender is Label lbl && lbl.BindingContext is CompanyPickup cp)
        {
            var texto = (cp.Telefono ?? "").Trim();
            if (string.IsNullOrWhiteSpace(texto)) return;

            try
            {
                await Clipboard.Default.SetTextAsync(texto);
                await DisplayAlert("Copiado", "Número copiado al portapapeles.", "OK");
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "No se pudo copiar el número.", "OK");
            }
        }
    }

    private async void OnEmailTapped(object sender, EventArgs e)
    {
        if (sender is Label lbl && lbl.BindingContext is CompanyPickup cp)
        {
            var texto = (cp.Email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(texto)) return;

            try
            {
                await Clipboard.Default.SetTextAsync(texto);
                await DisplayAlert("Copiado", "Correo copiado al portapapeles.", "OK");
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "No se pudo copiar el correo.", "OK");
            }
        }
    }

    private static readonly Dictionary<string, string> LabelTraducciones = new()
    {
        { "battery", "bateria" },
        { "keyboard", "teclado" },
        { "microwave", "microondas" },
        { "mobile", "teléfono móvil" },
        { "mouse", "ratón" },
        { "pcb", "placa de circuitos PCB" },
        { "player", "reproductor de música" },
        { "printer", "impresora" },
        { "television", "televisor" },
        { "washing machine", "lavadora" },
    };

    private string TraducirLabel(string label)
    {
        if (LabelTraducciones.TryGetValue(label?.Trim().ToLower() ?? "", out var traduccion))
            return traduccion;
        return label; // Si no hay traducción, muestra el original
    }


}