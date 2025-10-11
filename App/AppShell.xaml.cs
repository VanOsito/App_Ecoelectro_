using App.Views;


namespace App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("InicioPage", typeof(InicioPage));
            Routing.RegisterRoute("Registrarse", typeof(Registrarse));
        }
    }
}
