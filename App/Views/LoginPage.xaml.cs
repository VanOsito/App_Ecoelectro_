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
            string nombre = _dbService.ObtenerNombreUsuario(usuario, password);

            // Guarda info básica
            App.UsuarioActual = usuario;
            App.Usuarionombre = nombre;

            // 🔹 Recupera el usuario completo desde la base de datos
            var usuarios = await _dbService.ObtenerUsuarios();
            var usuarioCompleto = usuarios.FirstOrDefault(u => u.Correo == usuario);

            if (usuarioCompleto != null)
            {
                App.UsuarioEnSesion = usuarioCompleto; //  guarda el objeto completo
            }

            await DisplayAlert("Bienvenido", $"Inicio de sesión exitoso, {nombre}", "OK");

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