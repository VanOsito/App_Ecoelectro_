using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json;
using App.Models; // para RegionChile

namespace App.Services
{
    public class RegionComunaService : IRegionComunaService
    {
        private const string ResourceName = "App.Resources.Raw.regiones_comunas.json";

        // Cache perezosa de la lista de regiones
        private readonly Lazy<Task<List<RegionChile.RegionChileModel>>> _lazyData;

        public RegionComunaService()
        {
            _lazyData = new Lazy<Task<List<RegionChile.RegionChileModel>>>(LoadAsync);
        }

        public async Task<List<string>> GetRegionesAsync()
        {
            var data = await _lazyData.Value;
            return data
                .Select(r => r.Nombre)
                .OrderBy(n => n)
                .ToList();
        }

        public async Task<List<string>> GetComunasAsync(string regionNombre)
        {
            var data = await _lazyData.Value;
            var reg = FindRegionByNombre(data, regionNombre);

            return reg?.Comunas
                       .Select(c => c.NombreComuna)
                       .OrderBy(c => c)
                       .ToList()
                   ?? new List<string>();
        }

        // ---------------- helpers ----------------

        private static RegionChile.RegionChileModel? FindRegionByNombre(
            List<RegionChile.RegionChileModel> data, string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;

            // 1) Exacto
            var exact = data.FirstOrDefault(r =>
                string.Equals(r.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // 2) Contiene (por si hay variaciones pequeñas)
            return data.FirstOrDefault(r =>
                r.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<List<RegionChile.RegionChileModel>> LoadAsync()
        {
            var asm = typeof(RegionComunaService).GetTypeInfo().Assembly;

            using var stream = asm.GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"No se pudo abrir el recurso embebido '{ResourceName}'. " +
                    "Asegúrate de que el archivo está en Resources/Raw y Build Action = MauiAsset.");
            }

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var list = JsonSerializer.Deserialize<List<RegionChile.RegionChileModel>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // aquí ya no mezclamos tipos: List o lista vacía
            return list ?? new List<RegionChile.RegionChileModel>();
        }
    }
}
