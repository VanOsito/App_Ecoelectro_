using App.Data;
using App.Models;
using App.Services;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel; // Permissions
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

#if ANDROID
using Android.Content;
using Android.Provider;
using Android.OS;
using Android.App;
using Android.Net;
using Android.Runtime;
#endif

namespace App.Views;

public partial class ComponentesPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly BlobStorageService _blob;

    // Catálogo de componentes (solo lectura)
    private List<ComponentInfo> _catalogComponents = new();

    // HttpClient reutilizable
    private static readonly HttpClient _httpClient = new HttpClient();

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

        var esAdmin = string.Equals(user.Correo, "admin@admin.com", StringComparison.OrdinalIgnoreCase);
        if (!esAdmin)
        {
            await DisplayAlert("Acceso denegado", "No tienes permisos para ver todas lasdetecciones.", "OK");
            await Navigation.PopAsync();
            return;
        }

        // Cargar catálogo de componentes (solo lectura)
        _catalogComponents = await _db.GetAllComponentsCatalogAsync();

        // Cargar todas las detecciones (admin)
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

    // --- Exportar PDF (botón) ---
    private async void OnExportPdfClicked(object sender, EventArgs e)
    {
        try
        {
            if (Detections == null || Detections.Count == 0)
            {
                await DisplayAlert("Aviso", "No hay detecciones para exportar.", "OK");
                return;
            }

            var fileName = $"detections_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            var tmpPath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await GeneratePdfAsync(tmpPath);

            // Intentar guardar en Descargas (Android/Windows). Si falla, abrir diálogo compartir.
            var saved = await SaveFileToDownloadsAsync(tmpPath, fileName);

            if (saved)
            {
                await DisplayAlert("Listo", "PDF guardado en la carpeta Descargas.", "OK");
            }
            else
            {
                // Fallback: abrir diálogo de compartir/guardar
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Exportar detecciones",
                    File = new ShareFile(tmpPath)
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task GeneratePdfAsync(string filePath)
    {
        const float pageWidth = 595f;
        const float pageHeight = 842f;

        var titlePaint = new SKPaint { Typeface = SKTypeface.Default, TextSize = 18, IsAntialias = true, Color = SKColors.Black };
        var headerPaint = new SKPaint { Typeface = SKTypeface.Default, TextSize = 14, IsAntialias = true, Color = SKColors.Black };
        var normalPaint = new SKPaint { Typeface = SKTypeface.Default, TextSize = 12, IsAntialias = true, Color = SKColors.Black };
        var smallPaint = new SKPaint { Typeface = SKTypeface.Default, TextSize = 10, IsAntialias = true, Color = SKColors.Gray };

        using var fs = File.Create(filePath);
        using var doc = SKDocument.CreatePdf(fs);

        foreach (var item in Detections)
        {
            var canvas = doc.BeginPage(pageWidth, pageHeight);

            float margin = 40f;
            float x = margin;
            float y = margin;

            canvas.DrawText($"Detección: {item.Detection.DispositivoNombre}", x, y, titlePaint);
            y += 26f;
            canvas.DrawText($"Usuario: {item.Detection.UsuarioNombre}", x, y, normalPaint);
            y += 18f;
            canvas.DrawText($"Fecha: {item.Detection.DetectedAt:dd/MM/yyyy HH:mm}", x, y, normalPaint);
            y += 18f;

            if (!string.IsNullOrWhiteSpace(item.Detection.Status))
            {
                canvas.DrawText($"Estado: {item.Detection.Status}", x, y, normalPaint);
                y += 18f;
            }
            if (item.Detection.Confidence.HasValue)
            {
                canvas.DrawText($"Confianza: {item.Detection.Confidence.Value:P1}", x, y, normalPaint);
                y += 18f;
            }

            if (!string.IsNullOrWhiteSpace(item.Detection.ImageUrl))
            {
                try
                {
                    var bytes = await _httpClient.GetByteArrayAsync(item.Detection.ImageUrl);
                    using var bitmap = SKBitmap.Decode(bytes);
                    if (bitmap != null)
                    {
                        float maxW = 150f;
                        float maxH = 150f;
                        float scale = Math.Min(maxW / bitmap.Width, maxH / bitmap.Height);
                        float w = bitmap.Width * scale;
                        float h = bitmap.Height * scale;
                        var destRect = SKRect.Create(pageWidth - margin - w, margin, w, h);
                        canvas.DrawBitmap(bitmap, destRect);
                    }
                }
                catch
                {
                    // ignorar fallo en descarga/imagen
                }
            }

            y += 6f;
            canvas.DrawText("Componentes:", x, y, headerPaint);
            y += 20f;

            // Listado de componentes
            foreach (var comp in item.Components)
            {
                var line = $"• {comp.Nombre} — {comp.Estado}";
                canvas.DrawText(line, x + 10f, y, normalPaint);
                y += 16f;

                // Si se acerca al final de página, crear nueva página
                if (y > pageHeight - margin)
                {
                    doc.EndPage();
                    canvas = doc.BeginPage(pageWidth, pageHeight);
                    y = margin;
                }
            }

            doc.EndPage();
        }

        doc.Close();
        await Task.CompletedTask;
    }

    // Intenta guardar el archivo en Descargas; devuelve true si guardado allí.
    private async Task<bool> SaveFileToDownloadsAsync(string filePath, string fileName)
    {
        try
        {
            // ANDROID: usar MediaStore (API >= 29) o escritura a public Downloads (API < 29)
            if (OperatingSystem.IsAndroid())
            {
#if ANDROID
                // Usar Android.App.Application.Context para evitar ambigüedad con Microsoft.Maui.Application
                var context = Android.App.Application.Context;

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    var resolver = context.ContentResolver;
                    var values = new ContentValues();
                    values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
                    values.Put(MediaStore.IMediaColumns.MimeType, "application/pdf");
                    // Guardar en carpeta Descargas pública
                    values.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);

                    var external = MediaStore.Downloads.ExternalContentUri;
                    var uri = resolver.Insert(external, values);
                    if (uri == null) return false;

                    using var outStream = resolver.OpenOutputStream(uri);
                    using var inStream = File.OpenRead(filePath);
                    await inStream.CopyToAsync(outStream);
                    return true;
                }
                else
                {
                    // Permiso WRITE_EXTERNAL_STORAGE requerido en API < 29
                    var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                        if (status != PermissionStatus.Granted)
                            return false; // usuario no concedió permiso
                    }

                    var downloads = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
                    Directory.CreateDirectory(downloads);
                    var dest = Path.Combine(downloads, fileName);
                    File.Copy(filePath, dest, true);

                    // Notificar al MediaScanner para que aparezca en la carpeta
                    var uri = Android.Net.Uri.FromFile(new Java.IO.File(dest));
                    var intent = new Intent(Intent.ActionMediaScannerScanFile);
                    intent.SetData(uri);
                    context.SendBroadcast(intent);

                    return true;
                }
#else
                return false;
#endif
            }

            // WINDOWS: copiar a carpeta Descargas del usuario
            if (OperatingSystem.IsWindows())
            {
                var downloads = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
                Directory.CreateDirectory(downloads);
                var dest = Path.Combine(downloads, fileName);
                File.Copy(filePath, dest, true);
                return true;
            }

            // iOS/macOS u otros: no hay una "Descargas" pública accesible de forma estándar -> fallback a compartir
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error guardando en Descargas: {ex.Message}");
            return false;
        }
    }

    // --- Eliminar componente (solo UI) ---
    private void OnRemoveComponentClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ComponentInfo comp)
        {
            // Buscar el DetectionWithComponents padre en la jerarquía visual
            Element? parent = btn.Parent;
            while (parent != null && parent.BindingContext is not DetectionWithComponents)
            {
                parent = parent.Parent;
            }

            DetectionWithComponents? parentDetection = parent?.BindingContext as DetectionWithComponents;

            // Fallback: buscar por existencia del componente en cualquier Detection
            if (parentDetection == null)
                parentDetection = Detections.FirstOrDefault(d => d.Components.Any(c => c.Id == comp.Id && c.Nombre == comp.Nombre));

            if (parentDetection != null)
            {
                var toRemove = parentDetection.Components.FirstOrDefault(c => c.Id == comp.Id && c.Nombre == comp.Nombre);
                if (toRemove != null)
                {
                    parentDetection.Components.Remove(toRemove);
                }
            }
        }
    }

    // --- Agregar componente (solo UI) ---
    private async void OnAddComponentClicked(object sender, EventArgs e)
    {
        if (_catalogComponents == null || !_catalogComponents.Any())
        {
            await DisplayAlert("Aviso", "No hay componentes disponibles en catálogo.", "OK");
            return;
        }

        // Mostrar una lista simple (ActionSheet). Para catálogos grandes recomiendo una página modal con búsqueda.
        var options = _catalogComponents.Select(c => $"{c.Id} - {c.Nombre}").ToArray();
        var choice = await DisplayActionSheet("Selecciona un componente", "Cancelar", null, options);

        if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar") return;

        // Extraer id desde la opción (formato "id - nombre")
        var parts = choice.Split(" - ", 2);
        if (!int.TryParse(parts[0], out var compId)) return;

        var selected = _catalogComponents.FirstOrDefault(c => c.Id == compId);
        if (selected == null) return;

        // Determinar la detección destino (CommandParameter con detection id)
        int detectionId = 0;
        if (sender is Button btn && btn.CommandParameter is int id)
            detectionId = id;
        else
        {
            // fallback: si no viene CommandParameter, intentar buscar el primer detection (no ideal)
            await DisplayAlert("Error", "No se pudo determinar la detección destino.", "OK");
            return;
        }

        var item = Detections.FirstOrDefault(d => d.Detection.DetectionId == detectionId);
        if (item == null) return;

        // Evitar duplicados en pantalla
        if (item.Components.Any(c => c.Id == selected.Id))
        {
            await DisplayAlert("Aviso", "El componente ya está en la lista de esta detección.", "OK");
            return;
        }

        // Añadir una copia local (solo UI)
        var clone = new ComponentInfo
        {
            Id = selected.Id,
            Nombre = selected.Nombre,
            Descripcion = selected.Descripcion,
            Estado = selected.Estado,
            GuidanceUrl = selected.GuidanceUrl
        };

        item.Components.Add(clone);
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