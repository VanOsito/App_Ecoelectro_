using System.Diagnostics;
using App.Utils;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
//using Microsoft.Maui.Essentials;

namespace App.Views;


public partial class CameraPage : ContentPage
{
    public CameraPage()
    {
        InitializeComponent();
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                var filePath = await SavePhotoAsync(photo);
                await GoToResultPage(filePath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo tomar la foto: {ex.Message}", "OK");
        }
    }

    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
            {
                var filePath = await SavePhotoAsync(photo);
                await GoToResultPage(filePath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
        }
    }

    private async Task<string> SavePhotoAsync(FileResult photo)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "captures");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

        using var stream = await photo.OpenReadAsync();
        using var fileStream = File.OpenWrite(filePath);
        await stream.CopyToAsync(fileStream);

        PhotoStore.LastPhotoPath = filePath;
        return filePath;
    }

    private async Task GoToResultPage(string filePath)
    {
        var uri = $"{nameof(CameraResultPage)}?photoPath={Uri.EscapeDataString(filePath)}";
        await Shell.Current.GoToAsync(uri);
    }

    private async void OnCancel(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}