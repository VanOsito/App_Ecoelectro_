using App.Data;
using App.Models;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static App.Models.RegionChile;
namespace App.Views;

public partial class GestionUsuariosPage : ContentPage
{
    private DatabaseService db = new DatabaseService();
    private Usuario usuarioEditando;
    public GestionUsuariosPage()
	{
		InitializeComponent();
        _= CargarUsuarios();
        _ = CargarRegiones();
        CargarDatosAsync();

    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarUsuarios();

    }

    private async Task CargarUsuarios()
    {
        var usuarios = await db.ObtenerUsuarios();
        UsuariosCollectionView.ItemsSource = usuarios;
    }

    private void OnEditarClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var usuario = (Usuario)button.BindingContext;

        usuarioEditando = usuario;

        nombreEntry.Text = usuario.Nombre;
        correoEntry.Text = usuario.Correo;
        //contraseñaEntry.Text = usuario.Contraseña;

        
        formularioFrame.IsVisible = true;

        var regiones = regionPicker.ItemsSource as List<RegionChileModel>;

        var regionSeleccionada = regiones?.FirstOrDefault(r => r.Nombre == usuario.RegionUsuario);

        if (regionSeleccionada != null)
        {
            regionPicker.SelectedItem = regionSeleccionada;

            comunaPicker.ItemsSource = regionSeleccionada.Comunas;

            var comunaSeleccionada = regionSeleccionada.Comunas.FirstOrDefault(c => c.NombreComuna == usuario.ComunaUsuario);
            comunaPicker.SelectedItem = comunaSeleccionada;
        }
    }

    private async void EliminarUsuario_Clicked(object sender, EventArgs e)
    {
        var id = (int)((Button)sender).CommandParameter;
        bool confirmar = await DisplayAlert("Eliminar", "¿Estás seguro de eliminar este usuario?", "Sí", "No");

        if (!confirmar)
            return;

        bool eliminado = await db.EliminarUsuario(id);

        if (eliminado)
            await DisplayAlert("Éxito", "Usuario eliminado correctamente", "OK");
        else
            await DisplayAlert("Error", "No se pudo eliminar el usuario", "OK");

        await CargarUsuarios();
    }
    public async Task<List<RegionChileModel>> CargarRegionesAsync()
    {
        var assembly = typeof(Registrarse).GetTypeInfo().Assembly;


        foreach (var res in assembly.GetManifestResourceNames())
        {
            Console.WriteLine(res);
        }

        Stream? stream = assembly.GetManifestResourceStream("App.Resources.Raw.regiones_comunas.json");

        if (stream == null)
        {
            throw new InvalidOperationException("No se pudo abrir el archivo de regiones. Asegúrate de que esté en Resources/Raw y con Build Action = MauiAsset");
        }

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<List<RegionChileModel>>(json) ?? new List<RegionChileModel>();

    }


    private async Task CargarRegiones()
    {
        var regiones = await CargarRegionesAsync();
        regionPicker.ItemsSource = regiones;
        regionPicker.ItemDisplayBinding = new Binding("Nombre");
    }

    private void OnRegionChanged(object sender, EventArgs e)
    {
        if (regionPicker.SelectedItem is RegionChileModel regionSeleccionada)
        {
            comunaPicker.ItemsSource = regionSeleccionada.Comunas;
            comunaPicker.ItemDisplayBinding = new Binding("NombreComuna");
        }

    }

    private async void CargarDatosAsync()
    {
        var regiones = await CargarRegionesAsync();
        regionPicker.ItemsSource = regiones;
        regionPicker.ItemDisplayBinding = new Binding("Nombre");
    }
    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (usuarioEditando == null)
        {
            await DisplayAlert("Error", "No se ha seleccionado un usuario para editar.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(nombreEntry.Text) ||
            string.IsNullOrWhiteSpace(correoEntry.Text) ||
            regionPicker.SelectedItem == null ||
            comunaPicker.SelectedItem == null)
        {
            await DisplayAlert("Error", "Por favor completa todos los campos.", "OK");
            return;
        }

        var regionSeleccionada = regionPicker.SelectedItem as RegionChileModel;
        var comunaSeleccionada = comunaPicker.SelectedItem as Comunas;

        // Actualizar los valores del usuario
        usuarioEditando.Nombre = nombreEntry.Text;
        usuarioEditando.Correo = correoEntry.Text;
        usuarioEditando.RegionUsuario = regionSeleccionada?.Nombre ?? "";
        usuarioEditando.ComunaUsuario = comunaSeleccionada?.NombreComuna ?? "";

        bool actualizado = await db.ActualizarUsuario(usuarioEditando);

        if (actualizado)
        {
            await DisplayAlert("Éxito", "Usuario actualizado correctamente.", "OK");
            formularioFrame.IsVisible = false;
            await CargarUsuarios();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo actualizar el usuario.", "OK");
        }

    }
    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Cancelar", "¿Deseas cancelar la acción?", "Sí", "No");
        if (confirmar)
        {
            await CargarUsuarios();
        }
    }
}
