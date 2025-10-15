using App.Views;
namespace App
{
    public partial class App : Application
    {
        public static bool UsuarioLogeado { get; set; } = false;
        public static string UsuarioActual { get; set; } = string.Empty;

        public App()
        {
            InitializeComponent();

        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            if (UsuarioLogeado)
                return new Window(new AppShellUsuario());
            else
                return new Window(new AppShell());
        }

    }
}