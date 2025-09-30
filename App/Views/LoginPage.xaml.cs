namespace App.Views;

public partial class LoginPage : ContentPage
{
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
            await DisplayAlert("Error", "Debe ingresar usuario y contraseña.", "OK");
            return;
        }

        if (usuario == "admin" && password == "1234")
        {
            await Shell.Current.GoToAsync("InicioPage");

        }
        else
        {
            await DisplayAlert("Error", "Usuario o contraseña incorrectos", "OK");
        }

        Preferences.Clear();

    }


}