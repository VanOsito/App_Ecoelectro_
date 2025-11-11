using App.Services;
using App.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Maps;
using App.Views;
using App.Data;

namespace App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ✅ TUS SERVICIOS EXISTENTES (MANTENER):
            builder.Services.AddSingleton<IImageClassifier, OnnxImageClassifier>();
            builder.Services.AddTransient<CameraResultPage>();
            builder.Services.AddSingleton<DatabaseService>();

            // ❌ COMENTA TEMPORALMENTE LO NUEVO:
            // builder.Services.AddSingleton<IPuntoReciclajeService, PuntoReciclajeService>();
            // builder.Services.AddTransient<GestionPuntosViewModel>();
            // builder.Services.AddTransient<MapaViewModel>();
            // builder.Services.AddTransient<GestionPuntosPage>();

            return builder.Build();
        }
    }
}
