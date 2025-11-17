using App.Data;
using App.Models;
using App.Services;
using Microsoft.Maui.Controls;
using System;
using static App.Models.RegionChile;

namespace App.Views
{
    public partial class PerfilPage : ContentPage
    {
        private readonly DatabaseService _dbService = new DatabaseService();

        public PerfilPage()
        {
            InitializeComponent();
            ConfigurarVisibilidadAdmin();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            CargarDatosUsuario();
        }

        // --- VISIBILIDAD ADMIN ---
        private void ConfigurarVisibilidadAdmin()
        {
            btnGestionUsuarios.IsVisible = App.UsuarioActual == "admin@admin.com";
            btnGestionDetecciones.IsVisible = App.UsuarioActual == "admin@admin.com";
            btnGestionCompanias.IsVisible = App.UsuarioActual == "admin@admin.com";
        }

        // -- USUARIO --
        private async void CargarDatosUsuario()
        {
            try
            {
                Usuario usuario = App.UsuarioEnSesion;

                // Si no está en memoria, buscar en la base de datos
                if (usuario == null && !string.IsNullOrEmpty(App.UsuarioActual))
                {
                    var usuarios = await _dbService.ObtenerUsuarios();
                    usuario = usuarios.FirstOrDefault(u => u.Correo == App.UsuarioActual);

                    if (usuario != null)
                        App.UsuarioEnSesion = usuario; // Guarda en memoria
                }

                if (usuario == null)
                {
                    lblNombre.Text = "Nombre del Usuario";
                    lblCorreo.Text = "correo@usuario.com";
                    lblRegion.Text = "Región no disponible";
                    lblComuna.Text = "Comuna no disponible";
                    lblPuntos.Text = "0";
                    return;
                }

                // Mostrar datos del usuario
                lblNombre.Text = usuario.Nombre;
                lblCorreo.Text = usuario.Correo;
                lblRegion.Text = usuario.RegionUsuario ?? "No especificada";
                lblComuna.Text = usuario.ComunaUsuario ?? "No especificada";

                await CargarPuntosUsuario(usuario.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar usuario: {ex.Message}");
                await DisplayAlert("Error", "No se pudieron cargar los datos del usuario.", "OK");
            }
        }

        //private async Task CargarPuntosUsuario(int usuarioId)
        //{
        //    try
        //    {
        //        int totalPuntos = await _dbService.ObtenerTotalPuntosAsync(usuarioId);
        //        lblPuntos.Text = totalPuntos.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error al cargar puntos: {ex.Message}");
        //        lblPuntos.Text = "0";
        //    }
        //}


        // -- EVENTOS BOTONES --
        private async void VerHistorial_Clicked(object sender, EventArgs e)
        {
            if (App.UsuarioEnSesion == null)
            {
                await DisplayAlert("Error", "No hay usuario en sesión.", "OK");
                return;
            }

            await Navigation.PushAsync(new HistorialPuntos(App.UsuarioEnSesion.Id));
        }

        private async void cerrar(object sender, EventArgs e)
        {
            bool confirmar = await DisplayAlert("Cerrar sesión", "¿Deseas cerrar sesión?", "Sí", "No");
            if (confirmar)
            {
                App.UsuarioActual = null;
                App.UsuarioEnSesion = null;
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
        }

        private async void gestionCompanias(object sender, EventArgs e)
        {
            // Reusar el _dbService que ya tienes y crear el servicio de regiones
            await Navigation.PushAsync(new GestionCompaniasPage(_dbService, new RegionComunaService()));
        }

        private async void gestionDetecciones(object sender, EventArgs e)
        {
            
            await Navigation.PushAsync(new ComponentesPage(_dbService, new BlobStorageService()));
        }
        private string ObtenerNivelUsuario(int puntos)
        {
            if (puntos < 1000)
            {
                imgMascota.Source = "conejo_bronce.png";
                return "Bronce";
            }
            else if (puntos < 5000)
            {
                imgMascota.Source = "conejo_platino.png";
                return "Plata";
            }
            else
            {
                imgMascota.Source = "conejo_golden.png";
                return "Gold";
            }
        }



        // Cargar puntos y nivel del usuario
        private async Task CargarPuntosUsuario(int usuarioId)
        {
            try
            {
                int totalPuntos = await _dbService.ObtenerTotalPuntosAsync(usuarioId);
                lblPuntos.Text = totalPuntos.ToString();

                string nivel = ObtenerNivelUsuario(totalPuntos);
                lblNivel.Text = $"Nivel: {nivel}";

                
                switch (nivel)
                {
                    case "Bronce":
                        imgMascota.Source = "conejo_bronce.png";
                        break;
                    case "Plata":
                        imgMascota.Source = "conejo_platino.png";
                        break;
                    case "Oro":
                        imgMascota.Source = "conejo_golden.png";
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar puntos: {ex.Message}");
                lblPuntos.Text = "0";
                lblNivel.Text = "Nivel: Bronce";
                imgMascota.Source = "conejo_bronce.png";
            }
        }
        private async void gestion(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GestionUsuariosPage());
        }
    }
}
