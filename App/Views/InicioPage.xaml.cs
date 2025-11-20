using App.Data;
using App.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace App.Views
{
    public partial class InicioPage : ContentPage
    {
        private readonly DatabaseService _dbService = new DatabaseService();
        private ObservableCollection<ContenidoEducativo> _contenidoList = new();

        public bool EsAdmin { get; set; }
        public ICommand EliminarCommand { get; }

        public InicioPage()
        {
            InitializeComponent();

            lblNombreUsuario.Text = App.Usuarionombre ?? "Usuario";

            EsAdmin = App.UsuarioActual == "admin@admin.com";
            frmAgregar.IsVisible = EsAdmin;

            EliminarCommand = new Command<ContenidoEducativo>(async (item) => await EliminarContenido(item));

            contenidoList.ItemsSource = _contenidoList;
            BindingContext = this;

            CargarContenido();
        }

        private async void CargarContenido()
        {
            var contenido = await _dbService.ObtenerContenidoEducativoAsync();
            _contenidoList.Clear();

            foreach (var item in contenido)
            {
                item.TieneImagen = !string.IsNullOrEmpty(item.ImagenUrl);
                _contenidoList.Add(item);
            }
        }

        //private async void OnAgregarImagenClicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var result = await MediaPicker.PickPhotoAsync();
        //        if (result != null)
        //        {
        //            imgPreview.Source = result.FullPath;
        //            imgPreview.IsVisible = true;
        //            imgPreview.BindingContext = result.FullPath;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"No se pudo cargar la imagen: {ex.Message}", "OK");
        //    }
        //}

        private async void OnPublicarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitulo.Text) || string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                await DisplayAlert("Error", "Completa todos los campos antes de publicar.", "OK");
                return;
            }

            var nuevoContenido = new ContenidoEducativo
            {
                Titulo = txtTitulo.Text,
                Descripcion = txtDescripcion.Text,
                ImagenUrl = imgPreview.IsVisible ? imgPreview.BindingContext?.ToString() : null,
                FechaPublicacion = DateTime.Now,
                EsPredeterminado = false
            };

            await _dbService.InsertarContenidoEducativoAsync(nuevoContenido);

            txtTitulo.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            imgPreview.IsVisible = false;

            await DisplayAlert("Éxito", "Publicación agregada correctamente.", "OK");
            CargarContenido();
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

        //private async void OnPickPhotoClicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var photo = await MediaPicker.PickPhotoAsync();
        //        if (photo != null)
        //        {
        //            var filePath = await SavePhotoAsync(photo);
        //            await GoToResultPage(filePath);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
        //    }
        //}

        private async Task<string> SavePhotoAsync(FileResult photo)
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, "captures");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

            using var stream = await photo.OpenReadAsync();
            using var fileStream = File.OpenWrite(filePath);
            await stream.CopyToAsync(fileStream);

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


        private async Task EliminarContenido(ContenidoEducativo contenido)
        {
            bool confirmar = await DisplayAlert("Eliminar", $"¿Deseas eliminar \"{contenido.Titulo}\"?", "Sí", "No");
            if (!confirmar)
                return;

            await _dbService.EliminarContenidoEducativoAsync(contenido.Id);
            await DisplayAlert("Eliminado", "La publicación fue eliminada.", "OK");
            CargarContenido();
        }
    }
}

