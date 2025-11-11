using Microsoft.Data.SqlClient;
using Dapper;
using App.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Diagnostics; // ✅ AÑADIR ESTA LÍNEA

namespace App.Services
{
    public class PuntoReciclajeService : IPuntoReciclajeService
    {
        private readonly string _connectionString;

        public PuntoReciclajeService()
        {
            _connectionString = "Server=tcp:ecoelectroserver2025.database.windows.net,1433;Initial Catalog=ecoelectrodb;Persist Security Info=False;User ID=admin_ecoelectro;Password=Reciclaje2025;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }

        public async Task<List<PuntoReciclaje>> ObtenerTodosAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                SELECT 
                    p.recycling_point_id AS RecyclingPointId,
                    p.nombre_punto_reciclaje AS Nombre,
                    p.direccion AS Direccion,
                    p.region_punto_reciclaje AS Region,
                    p.comuna_punto_reciclaje AS Comuna,
                    p.lat AS Lat,
                    p.lng AS Lng,
                    p.telefono AS Telefono,
                    p.email AS Email,
                    p.website AS Web,
                    p.accepts_notes AS AcceptsNotes,
                    p.active AS Active,
                    a.accepts_recycle AS AcceptsRecycle,
                    a.accepts_reuse AS AcceptsReuse,
                    a.accepts_buyback AS AcceptsBuyback
                FROM punto_reciclaje p
                LEFT JOIN punto_reciclaje_acepta a ON p.recycling_point_id = a.recycling_point_id
                WHERE p.active = 1";

            var puntos = await connection.QueryAsync<PuntoReciclaje>(sql);

            // ✅ CORREGIDO: Sincronizar residuos desde AcceptsNotes
            foreach (var punto in puntos)
            {
                punto.SincronizarDesdeBD(); // Esto actualizará Residuos desde AcceptsNotes
            }

            return puntos.ToList();
        }

        public async Task<PuntoReciclaje?> ObtenerPorIdAsync(string id)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                SELECT 
                    p.recycling_point_id AS RecyclingPointId,
                    p.nombre_punto_reciclaje AS Nombre,
                    p.direccion AS Direccion,
                    p.region_punto_reciclaje AS Region,
                    p.comuna_punto_reciclaje AS Comuna,
                    p.lat AS Lat,
                    p.lng AS Lng,
                    p.telefono AS Telefono,
                    p.email AS Email,
                    p.website AS Web,
                    p.accepts_notes AS AcceptsNotes,
                    p.active AS Active,
                    a.accepts_recycle AS AcceptsRecycle,
                    a.accepts_reuse AS AcceptsReuse,
                    a.accepts_buyback AS AcceptsBuyback
                FROM punto_reciclaje p
                LEFT JOIN punto_reciclaje_acepta a ON p.recycling_point_id = a.recycling_point_id
                WHERE p.recycling_point_id = @Id";

            var punto = await connection.QueryFirstOrDefaultAsync<PuntoReciclaje>(sql, new { Id = id });

            if (punto != null)
            {
                // ✅ CORREGIDO: Sincronizar residuos desde AcceptsNotes
                punto.SincronizarDesdeBD();
            }

            return punto;
        }

        public async Task<bool> GuardarAsync(PuntoReciclaje punto)
        {
            using var connection = new SqlConnection(_connectionString);

            if (string.IsNullOrEmpty(punto.RecyclingPointId))
            {
                punto.RecyclingPointId = $"CRUD_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            // ✅ CORREGIDO: Asegurar que AcceptsNotes esté sincronizado con Residuos
            if (punto.Residuos != null && punto.Residuos.Any())
            {
                punto.AcceptsNotes = string.Join(", ", punto.Residuos);
            }
            else
            {
                punto.AcceptsNotes = "Punto de reciclaje electrónico";
            }

            var sqlPunto = @"
                INSERT INTO punto_reciclaje 
                (recycling_point_id, nombre_punto_reciclaje, direccion, region_punto_reciclaje, 
                 comuna_punto_reciclaje, lat, lng, telefono, email, website, accepts_notes, active)
                VALUES 
                (@RecyclingPointId, @Nombre, @Direccion, @Region, 
                 @Comuna, @Lat, @Lng, @Telefono, @Email, @Web, @AcceptsNotes, @Active)";

            var sqlAcepta = @"
                INSERT INTO punto_reciclaje_acepta 
                (recycling_point_id, accepts_recycle, accepts_reuse, accepts_buyback)
                VALUES 
                (@RecyclingPointId, @AcceptsRecycle, @AcceptsReuse, @AcceptsBuyback)";

            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var parametrosPunto = new
                {
                    punto.RecyclingPointId,
                    punto.Nombre,
                    punto.Direccion,
                    Region = punto.Region,
                    Comuna = punto.Comuna,
                    punto.Lat,
                    punto.Lng,
                    punto.Telefono,
                    punto.Email,
                    Web = punto.Web,
                    punto.AcceptsNotes,
                    punto.Active
                };

                var parametrosAcepta = new
                {
                    punto.RecyclingPointId,
                    punto.AcceptsRecycle,
                    punto.AcceptsReuse,
                    punto.AcceptsBuyback
                };

                var resultPunto = await connection.ExecuteAsync(sqlPunto, parametrosPunto, transaction);
                var resultAcepta = await connection.ExecuteAsync(sqlAcepta, parametrosAcepta, transaction);

                transaction.Commit();
                return resultPunto > 0 && resultAcepta > 0;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"Error guardando punto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarAsync(PuntoReciclaje punto)
        {
            using var connection = new SqlConnection(_connectionString);

            // ✅ CORREGIDO: Asegurar que AcceptsNotes esté sincronizado con Residuos
            if (punto.Residuos != null && punto.Residuos.Any())
            {
                punto.AcceptsNotes = string.Join(", ", punto.Residuos);
            }
            else
            {
                punto.AcceptsNotes = "Punto de reciclaje electrónico";
            }

            var sqlPunto = @"
                UPDATE punto_reciclaje 
                SET nombre_punto_reciclaje = @Nombre,
                    direccion = @Direccion,
                    region_punto_reciclaje = @Region,
                    comuna_punto_reciclaje = @Comuna,
                    lat = @Lat,
                    lng = @Lng,
                    telefono = @Telefono,
                    email = @Email,
                    website = @Web,
                    accepts_notes = @AcceptsNotes,
                    active = @Active,
                    fecha_actualizacion = GETDATE()
                WHERE recycling_point_id = @RecyclingPointId";

            var sqlAcepta = @"
                UPDATE punto_reciclaje_acepta 
                SET accepts_recycle = @AcceptsRecycle,
                    accepts_reuse = @AcceptsReuse,
                    accepts_buyback = @AcceptsBuyback
                WHERE recycling_point_id = @RecyclingPointId";

            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var parametrosPunto = new
                {
                    punto.RecyclingPointId,
                    punto.Nombre,
                    punto.Direccion,
                    Region = punto.Region,
                    Comuna = punto.Comuna,
                    punto.Lat,
                    punto.Lng,
                    punto.Telefono,
                    punto.Email,
                    Web = punto.Web,
                    punto.AcceptsNotes,
                    punto.Active
                };

                var parametrosAcepta = new
                {
                    punto.RecyclingPointId,
                    punto.AcceptsRecycle,
                    punto.AcceptsReuse,
                    punto.AcceptsBuyback
                };

                var resultPunto = await connection.ExecuteAsync(sqlPunto, parametrosPunto, transaction);
                var resultAcepta = await connection.ExecuteAsync(sqlAcepta, parametrosAcepta, transaction);

                transaction.Commit();
                return resultPunto > 0 && resultAcepta > 0;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"Error actualizando punto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarAsync(string id)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = "DELETE FROM punto_reciclaje WHERE recycling_point_id = @Id";
            var result = await connection.ExecuteAsync(sql, new { Id = id });

            return result > 0;
        }

        public async Task<List<PuntoReciclaje>> BuscarPorUbicacionAsync(double lat, double lng, double radioKm)
        {
            var todosPuntos = await ObtenerTodosAsync();

            var puntos = todosPuntos.Where(p =>
            {
                var distancia = CalcularDistancia(lat, lng, p.Lat, p.Lng);
                p.DistanciaKm = distancia;
                return distancia <= radioKm;
            }).OrderBy(p => p.DistanciaKm).ToList();

            return puntos;
        }

        public async Task<List<PuntoReciclaje>> BuscarPorComunaAsync(string comuna)
        {
            var todosPuntos = await ObtenerTodosAsync();

            var puntos = todosPuntos.Where(p =>
                !string.IsNullOrEmpty(p.Comuna) &&
                p.Comuna.Equals(comuna, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            return puntos;
        }

        public async Task<bool> ProbarConexionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error de conexión: {ex.Message}");
                return false;
            }
        }

        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            const double radioTierraKm = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return radioTierraKm * c;
        }
    }
}