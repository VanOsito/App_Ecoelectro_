namespace App.Views;
using System;
using System.Diagnostics;
using System.IO;

using Utils;

public partial class InicioPage : ContentPage
{
	public InicioPage()
	{
		InitializeComponent();
	}

    // -- ON APPEARING --
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Mostrar la última foto si existe
        if (!string.IsNullOrWhiteSpace(PhotoStore.LastPhotoPath) && File.Exists(PhotoStore.LastPhotoPath))
        {
            LastPhoto.Source = ImageSource.FromFile(PhotoStore.LastPhotoPath);
        }
    }

    // -- TOMAR FOTO --
    private async void OnTakePhoto(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CameraPage));
    }


}