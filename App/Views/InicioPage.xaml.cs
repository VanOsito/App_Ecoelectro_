namespace App.Views;

public partial class InicioPage : ContentPage
{
	public InicioPage()
	{
		InitializeComponent();
	}
    private async void cerrar(object sender, EventArgs e)
    {
        Preferences.Clear(); 

        await Shell.Current.GoToAsync("//LoginPage");

    }

}