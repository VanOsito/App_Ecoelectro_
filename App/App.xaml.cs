using App.Views;
namespace App
{
    public partial class App : Application
    {
        public static bool UsuarioLogeado { get; set; } = false;
        public static string UsuarioActual { get; set; } = string.Empty;
        public static string Usuarionombre { get; set; } = string.Empty;

        public App()
        {
            InitializeComponent();

        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window window;

            if (UsuarioLogeado)
                window = new Window(new AppShellUsuario());
            else
                window = new Window(new AppShell());

            
            _ = SolicitarPermisosUbicacionAsync();

            return window;
        }

        private async Task SolicitarPermisosUbicacionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status == PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Permisos de ubicación concedidos");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Permisos de ubicación no concedidos");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error permisos: {ex.Message}");
            }
        }

    }
}