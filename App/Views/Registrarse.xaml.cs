using App.Data;
using App.Models;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static App.Models.RegionChile;



namespace App.Views;

public partial class Registrarse : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();

    public Registrarse()
    {
        InitializeComponent();
        _ = CargarRegiones();
        CargarDatosAsync();
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
        pickerRegion.ItemsSource = regiones;
        pickerRegion.ItemDisplayBinding = new Binding("Nombre");
    }

    private void pickerRegion_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (pickerRegion.SelectedItem is RegionChileModel regionSeleccionada)
        {
            pickerComuna.ItemsSource = regionSeleccionada.Comunas;
            pickerComuna.ItemDisplayBinding = new Binding("NombreComuna");
        }
        
    }

    private async void CargarDatosAsync()
    {
        var regiones = await CargarRegionesAsync();
        pickerRegion.ItemsSource = regiones;
        pickerRegion.ItemDisplayBinding = new Binding("Nombre");
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }
    private async void Registrarse_Clicked(object sender, EventArgs e)
    {
        var db = new DatabaseService();


        if (!chkTerminos.IsChecked)
        {
            await DisplayAlert("Aviso", "Debes aceptar los Términos y Condiciones para continuar.", "OK");
            return;
        }

        if (!IsValidEmail(CorreoEntry.Text))
        {
            await DisplayAlert("Error", "Por favor ingresa un correo electrónico válido.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
            string.IsNullOrWhiteSpace(ContraseñaEntry.Text) ||
            pickerRegion.SelectedItem == null ||
            pickerComuna.SelectedItem == null)
        {
            await DisplayAlert("Error", "Por favor complete todos los campos.", "OK");
            return;
        }


        var regionSeleccionada = pickerRegion.SelectedItem as RegionChileModel;
        var comunaSeleccionada = pickerComuna.SelectedItem as Comunas;

        
        var usuario = new Usuario
        {
            Nombre = NombreEntry.Text ?? string.Empty,
            Correo = CorreoEntry.Text ?? string.Empty,
            Contraseña = ContraseñaEntry.Text ?? string.Empty,
            RegionUsuario = regionSeleccionada?.Nombre ?? string.Empty,
            ComunaUsuario = comunaSeleccionada?.NombreComuna ?? string.Empty
        };

        bool exito = _dbService.RegistrarUsuario(usuario);

        if (exito)
        {
            await DisplayAlert("Éxito", "Usuario registrado correctamente", "OK");
            await Navigation.PushAsync(new LoginPage());
        }
        else
        {
            await DisplayAlert("Error", "No se pudo registrar el usuario", "OK");
        }
    }
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

}

