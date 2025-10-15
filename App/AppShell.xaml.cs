using App.Views;


namespace App
{
    public partial class AppShell : Shell
    {
        
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("InicioPage", typeof(InicioPage));
            Routing.RegisterRoute("Registrarse", typeof(Registrarse));
            Routing.RegisterRoute("GestionUsuariosPage", typeof(GestionUsuariosPage));

            
        }
    }
}
