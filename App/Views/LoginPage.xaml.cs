using Microsoft.Maui.Controls;
using System;
using App.Data;
namespace App.Views;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    public LoginPage()
    {
        InitializeComponent();
    }
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string usuario = txtUsuario.Text;
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Debes ingresar usuario y contraseña.", "OK");
            return;
        }

        bool valido = _dbService.ValidarUsuario(usuario, password);

        if (valido)
        {
            
            var usuarios = await _dbService.ObtenerUsuarios();
            var usuarioActual = usuarios.FirstOrDefault(u => u.Correo == usuario);

           
            App.UsuarioEnSesion = usuarioActual;

            await DisplayAlert("Bienvenido", $"Inicio de sesión exitoso, {usuarioActual.Nombre}", "OK");
            Application.Current.MainPage = new AppShellUsuario();
        }
        else
        {
            await DisplayAlert("Error", "Correo o contraseña incorrectos", "OK");
        }
    }


    

    private async void Registrarse(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Registrarse());
    }
}