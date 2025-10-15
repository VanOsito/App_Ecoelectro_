using static App.Models.RegionChile;
using App.Data;
using Microsoft.Maui.Controls;  
using System;
using App.Models;
namespace App.Views;

public partial class PerfilPage : ContentPage
{
	public PerfilPage()
	{
		InitializeComponent();

        if (App.UsuarioActual == "admin@admin.com")
            btnGestionUsuarios.IsVisible = true;
        else
            btnGestionUsuarios.IsVisible = false;
    }
    private async void cerrar(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Cerrar sesión", "¿Deseas cerrar sesión?", "Sí", "No");
        if (confirmar)
        {
            Application.Current.MainPage = new NavigationPage(new LoginPage());

        }
    }
    private async void gestion(object sender, EventArgs e)
    {

        await Navigation.PushAsync(new GestionUsuariosPage());


    }
    
}