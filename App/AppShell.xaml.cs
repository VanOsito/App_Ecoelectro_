using App.Views;


namespace App
{
    public partial class AppShell : Shell
    {
        
        public AppShell()
        {
            InitializeComponent();
            ConfigurarMapaPorPlataforma();

            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("InicioPage", typeof(InicioPage));
            Routing.RegisterRoute("Registrarse", typeof(Registrarse));
            Routing.RegisterRoute("GestionUsuariosPage", typeof(GestionUsuariosPage));

            
        }
        private void ConfigurarMapaPorPlataforma()
        {
#if WINDOWS
        Console.WriteLine("🖥️  Configurando para Windows - MapaWebPage");
        MapaContent.ContentTemplate = new DataTemplate(typeof(Views.MapaWebPage));
#else
            Console.WriteLine("📱 Configurando para móvil - MapaPage");
            MapaContent.ContentTemplate = new DataTemplate(typeof(Views.MapaPage));
#endif
        }
    }
}
