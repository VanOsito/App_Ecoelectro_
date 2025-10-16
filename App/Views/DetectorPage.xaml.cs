namespace App.Views;

public partial class DetectorPage : ContentPage
{
	public DetectorPage()
	{
		InitializeComponent();
	}
    private async void OnDetectarClicked(object sender, EventArgs e)
    {
        var labelDetectado = SimuladorPicker.SelectedItem?.ToString();

        if (!string.IsNullOrEmpty(labelDetectado))
        {
            // Crear la p�gina del mapa
            var mapaPage = new MapaPage();

            // ? Llamar correctamente al m�todo p�blico
            await mapaPage.MostrarFiltroDesdeIAAsync(labelDetectado);

            // Navegar
            await Navigation.PushAsync(mapaPage);
        }
        else
        {
            await DisplayAlert("Error", "Selecciona un dispositivo antes de continuar.", "OK");
        }
    }
}