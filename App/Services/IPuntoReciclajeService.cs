using App.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Services
{
    public interface IPuntoReciclajeService
    {
        Task<List<PuntoReciclaje>> ObtenerTodosAsync();
        Task<PuntoReciclaje?> ObtenerPorIdAsync(string id);
        Task<bool> GuardarAsync(PuntoReciclaje punto);
        Task<bool> ActualizarAsync(PuntoReciclaje punto);
        Task<bool> EliminarAsync(string id);
        Task<List<PuntoReciclaje>> BuscarPorUbicacionAsync(double lat, double lng, double radioKm);
        Task<List<PuntoReciclaje>> BuscarPorComunaAsync(string comuna);
        Task<bool> ProbarConexionAsync();
    }
}