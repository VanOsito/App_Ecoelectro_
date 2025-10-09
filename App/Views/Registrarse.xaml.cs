using App.Models;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using static App.Models.RegionChile;



namespace App.Views;

public partial class Registrarse : ContentPage
{
    public List<RegionChile.RegionChileModel> ListaRegiones { get; set; } = new();

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

}

