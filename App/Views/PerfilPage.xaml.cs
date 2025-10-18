using App.Data;
using App.Models;
using Microsoft.Maui.Controls;  
using System;
using static App.Models.RegionChile;
namespace App.Views;

public partial class PerfilPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    

    public PerfilPage()
	{
		InitializeComponent();
        if (App.UsuarioActual != null)
        {
            CargarDatosUsuario();
        }

        if (App.UsuarioActual == "admin@admin.com")
            btnGestionUsuarios.IsVisible = true;
        else
            btnGestionUsuarios.IsVisible = false;

    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (App.UsuarioActual != null)
        {
            CargarDatosUsuario();
        }
    }
    private void CargarDatosUsuario()
    {
        var usuario = App.UsuarioEnSesion;
        if (usuario == null)
            return;

        lblBienvenida.Text = $"Bienvenido, {usuario.Nombre}";
        lblNombre.Text = $"Nombre: {usuario.Nombre}";
        lblCorreo.Text = $"Correo: {usuario.Correo}";
        lblRegion.Text = $"Región: {usuario.RegionUsuario}";
        lblComuna.Text = $"Comuna: {usuario.ComunaUsuario}";
    }


    private async void cerrar(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Cerrar sesión", "¿Deseas cerrar sesión?", "Sí", "No");
        if (confirmar)
        {
            App.UsuarioEnSesion = null; // <--- Limpia la sesión
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
    }
    private async void gestion(object sender, EventArgs e)
    {

        await Navigation.PushAsync(new GestionUsuariosPage());


    }
    

}