using App.Data;
using App.Models;

namespace App.Views;

public partial class HistorialPuntos : ContentPage
{
    private readonly DatabaseService _db = new DatabaseService();
    private readonly int _usuarioId;

    public HistorialPuntos(int usuarioId)
    {
        InitializeComponent();
        _usuarioId = usuarioId;
        CargarHistorial();
    }

    private async void CargarHistorial()
    {
        var historial = await _db.ObtenerHistorialPuntosAsync(_usuarioId);

        if (historial == null || historial.Count == 0)
        {
            await DisplayAlert("Historial vacío", "No tienes puntos registrados todavía.", "OK");
            HistorialList.ItemsSource = null;
            TotalLabel.Text = "Total: 0 pts";
            return;
        }

        HistorialList.ItemsSource = historial;
        TotalLabel.Text = $"Total: {historial.Sum(p => p.Cantidad)} pts";
    }
}
