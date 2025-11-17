using App.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;


namespace App.Data
{
    public class DatabaseService
    {
        private readonly string connectionString = "Server=tcp:ecoelectroserver2025.database.windows.net,1433;Initial Catalog=ecoelectrodb;Persist Security Info=False;User ID=admin_ecoelectro;Password=Reciclaje2025;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public async Task<bool> ProbarConexionAsync()
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al conectar: {ex.Message}");
                return false;
            }
        }

        public bool RegistrarUsuario(Usuario usuario)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 1️⃣ Registrar el nuevo usuario y obtener su ID
                    string query = @"INSERT INTO Usuarios 
                    (Nombre, Correo, Contraseña, RegionUsuario, ComunaUsuario, IdRol) 
                    OUTPUT INSERTED.Id
                    VALUES (@Nombre, @Correo, @Contraseña, @RegionUsuario, @ComunaUsuario, @IdRol)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);
                    cmd.Parameters.AddWithValue("@Correo", usuario.Correo);
                    cmd.Parameters.AddWithValue("@Contraseña", usuario.Contraseña);
                    cmd.Parameters.AddWithValue("@RegionUsuario", usuario.RegionUsuario);
                    cmd.Parameters.AddWithValue("@ComunaUsuario", usuario.ComunaUsuario);
                    cmd.Parameters.AddWithValue("@IdRol", 2);

                    int nuevoUsuarioId = (int)cmd.ExecuteScalar();

                    // 2️⃣ Insertar los puntos de bienvenida
                    string puntosQuery = @"INSERT INTO Puntos (UsuarioId, Cantidad, Tipo, Descripcion)
                           VALUES (@UsuarioId, @Cantidad, @Tipo, @Descripcion)";
                    SqlCommand puntosCmd = new SqlCommand(puntosQuery, conn);
                    puntosCmd.Parameters.AddWithValue("@UsuarioId", nuevoUsuarioId);
                    puntosCmd.Parameters.AddWithValue("@Cantidad", 100);
                    puntosCmd.Parameters.AddWithValue("@Tipo", "Asignación");
                    puntosCmd.Parameters.AddWithValue("@Descripcion", "Puntos de bienvenida por registro");
                    puntosCmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar: {ex.Message}");
                return false;
            }
        }


        public bool ValidarUsuario(string correo, string contrasena)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = "SELECT Id, IdRol FROM Usuarios WHERE Correo = @Correo AND Contraseña = @Contraseña";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Contraseña", contrasena);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idUsuario = Convert.ToInt32(reader["Id"]);
                        int idRol = reader["IdRol"] == DBNull.Value ? 0 : Convert.ToInt32(reader["IdRol"]);

                        reader.Close();

                        if (idRol == 1)
                            return true;

                        if (idRol == 0)
                        {
                            string updateQuery = "UPDATE Usuarios SET IdRol = 2 WHERE Id = @Id";
                            SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                            updateCmd.Parameters.AddWithValue("@Id", idUsuario);
                            updateCmd.ExecuteNonQuery();
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public async Task<List<Usuario>> ObtenerUsuarios()
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var query = "SELECT * FROM Usuarios";
            var command = new SqlCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();
            var usuarios = new List<Usuario>();

            while (await reader.ReadAsync())
            {
                usuarios.Add(new Usuario
                {
                    Id = (int)reader["Id"],
                    Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString()! : string.Empty,
                    Correo = reader["Correo"] != DBNull.Value ? reader["Correo"].ToString()! : string.Empty,
                    Contraseña = reader["Contraseña"] != DBNull.Value ? reader["Contraseña"].ToString()! : string.Empty,
                    RegionUsuario = reader["RegionUsuario"] != DBNull.Value ? reader["RegionUsuario"].ToString()! : string.Empty,
                    ComunaUsuario = reader["ComunaUsuario"] != DBNull.Value ? reader["ComunaUsuario"].ToString()! : string.Empty,
                });
            }
            return usuarios;
        }

        public async Task<bool> ActualizarUsuario(Usuario usuario)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var query = @"UPDATE Usuarios SET 
                        Nombre=@Nombre, 
                        Correo=@Correo, 
                        Contraseña=@Contraseña, 
                        RegionUsuario=@RegionUsuario, 
                        ComunaUsuario=@ComunaUsuario
                      WHERE Id=@Id";

            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", usuario.Id);
            command.Parameters.AddWithValue("@Nombre", usuario.Nombre);
            command.Parameters.AddWithValue("@Correo", usuario.Correo);
            command.Parameters.AddWithValue("@Contraseña", usuario.Contraseña);
            command.Parameters.AddWithValue("@RegionUsuario", usuario.RegionUsuario);
            command.Parameters.AddWithValue("@ComunaUsuario", usuario.ComunaUsuario);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> EliminarUsuario(int id)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var query = "DELETE FROM Usuarios WHERE Id=@Id";
            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public string ObtenerNombreUsuario(string correo, string contrasena)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Nombre FROM Usuarios WHERE Correo = @Correo AND Contraseña = @Contraseña";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Contraseña", contrasena);

                var result = cmd.ExecuteScalar();
                return result != null ? result.ToString()! : string.Empty;
            }
        }

        public string ObtenerNombre(string correo)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Nombre FROM Usuarios WHERE Correo = @Correo";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Correo", correo);

                var result = cmd.ExecuteScalar();
                return result != null ? result.ToString()! : string.Empty;
            }
        }

        private static string NormalizarEtiqueta(string raw)
        {
            var x = (raw ?? "").Trim();
            return x;
        }

        public async Task<List<ComponentInfo>> ObtenerComponentesPorDispositivoAsync(string etiquetaModelo)
        {
            var deviceLabel = NormalizarEtiqueta(etiquetaModelo);

            const string sql = @"
SELECT
    c.componente_id,
    c.nombre_componente,
    c.descripcion_componente,
    -- flags de la vista ya resueltos:
    v.is_recyclable, v.is_reusable, v.is_sellable, v.is_hazardous,
    v.guidance_short, v.guidance_url
FROM dbo.v_dispositivo_componentes v
JOIN dbo.componentes_catalogo c ON c.componente_id = v.componente_id
WHERE v.nombre_dispositivo = @label
ORDER BY c.nombre_componente;";

            var lista = new List<ComponentInfo>();

            try
            {
                using var cn = new SqlConnection(connectionString);
                await cn.OpenAsync();

                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@label", SqlDbType.NVarChar, 80) { Value = deviceLabel });

                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var isRecyclable = rd.GetBoolean(rd.GetOrdinal("is_recyclable"));
                    var isReusable = rd.GetBoolean(rd.GetOrdinal("is_reusable"));
                    var isSellable = rd.GetBoolean(rd.GetOrdinal("is_sellable"));
                    var isHazardous = rd.GetBoolean(rd.GetOrdinal("is_hazardous"));

                    string estado =
                        isRecyclable ? "Reciclable" :
                        isReusable ? "Reutilizable" :
                        isSellable ? "Vendible" :
                        isHazardous ? "Peligroso" : "—";

                    var guidanceShort = rd["guidance_short"] as string;
                    var desc = rd["descripcion_componente"] as string;

                    lista.Add(new ComponentInfo
                    {
                        Id = rd.GetInt32(rd.GetOrdinal("componente_id")),
                        Nombre = (string)rd["nombre_componente"],
                        Estado = estado,
                        Descripcion = !string.IsNullOrWhiteSpace(guidanceShort) ? guidanceShort : (desc ?? ""),
                        GuidanceUrl = rd["guidance_url"] as string
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error ObtenerComponentesPorDispositivoAsync: {ex.Message}");
            }

            return lista;
        }

        public async Task<int?> GetComponentIdByNameAsync(string componentName)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            var sql = @"SELECT componente_id FROM componentes_catalogo WHERE nombre_componente = @n";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@n", componentName);
            var o = await cmd.ExecuteScalarAsync();
            return o == null ? null : (int?)Convert.ToInt32(o);
        }

        public async Task<List<CompanyPickup>> GetCompaniesForComponentAsync(
            int componenteId, string? regionNombreCompleto, string? comunaNombre = null)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            var sql = @"
SELECT v.pickup_company_id, v.nombre, v.website, v.telefono, v.email, v.coverage_notes,
       v.prioridad, v.notas, v.region_cobertura, v.comuna_cobertura,
       v.componente_id, c.nombre_componente
FROM v_empresas_por_componente v
JOIN componentes_catalogo c ON c.componente_id = v.componente_id
WHERE v.componente_id = @cid
  AND (@region IS NULL 
       OR v.region_cobertura IS NULL 
       OR v.region_cobertura = @region)
  AND (@comuna IS NULL 
       OR v.comuna_cobertura IS NULL 
       OR v.comuna_cobertura = @comuna)
ORDER BY v.prioridad, v.nombre;";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cid", componenteId);
            cmd.Parameters.AddWithValue("@region", (object?)regionNombreCompleto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@comuna", (object?)comunaNombre ?? DBNull.Value);

            var list = new List<CompanyPickup>();
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new CompanyPickup
                {
                    PickupCompanyId = rd.GetInt32(0),
                    Nombre = rd.GetString(1),
                    Website = rd.IsDBNull(2) ? null : rd.GetString(2),
                    Telefono = rd.IsDBNull(3) ? null : rd.GetString(3),
                    Email = rd.IsDBNull(4) ? null : rd.GetString(4),
                    CoverageNotes = rd.IsDBNull(5) ? null : rd.GetString(5),
                    Prioridad = rd.GetInt32(6),
                    Notas = rd.IsDBNull(7) ? null : rd.GetString(7),
                    RegionCobertura = rd.IsDBNull(8) ? null : rd.GetString(8),
                    ComunaCobertura = rd.IsDBNull(9) ? null : rd.GetString(9),
                    ComponenteId = rd.GetInt32(10),
                    ComponenteNombre = rd.GetString(11)
                });
            }
            return list;
        }

        // -- COMPAÑIAS POR COMPONENTES (MULTI) --
        public async Task<List<CompanyPickup>> GetCompaniesForComponentsAsync(
            IEnumerable<int> componenteIds, string? regionNombreCompleto, string? comunaNombre = null)
        {
            var ids = componenteIds?.Distinct().ToArray() ?? Array.Empty<int>();
            if (ids.Length == 0) return new();

            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            var inParams = string.Join(",", ids.Select((_, i) => $"@p{i}"));
            var sql = $@"
SELECT v.pickup_company_id, v.nombre, v.website, v.telefono, v.email, v.coverage_notes,
       v.prioridad, v.notas, v.region_cobertura, v.comuna_cobertura,
       v.componente_id, c.nombre_componente
FROM v_empresas_por_componente v
JOIN componentes_catalogo c ON c.componente_id = v.componente_id
WHERE v.componente_id IN ({inParams})
  AND (@region IS NULL 
       OR v.region_cobertura IS NULL 
       OR v.region_cobertura = @region)
  AND (@comuna IS NULL 
       OR v.comuna_cobertura IS NULL 
       OR v.comuna_cobertura = @comuna)
ORDER BY v.componente_id, v.prioridad, v.nombre;";

            using var cmd = new SqlCommand(sql, cn);
            for (int i = 0; i < ids.Length; i++)
                cmd.Parameters.AddWithValue($"@p{i}", ids[i]);
            cmd.Parameters.AddWithValue("@region", (object?)regionNombreCompleto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@comuna", (object?)comunaNombre ?? DBNull.Value);

            var list = new List<CompanyPickup>();
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new CompanyPickup
                {
                    PickupCompanyId = rd.GetInt32(0),
                    Nombre = rd.GetString(1),
                    Website = rd.IsDBNull(2) ? null : rd.GetString(2),
                    Telefono = rd.IsDBNull(3) ? null : rd.GetString(3),
                    Email = rd.IsDBNull(4) ? null : rd.GetString(4),
                    CoverageNotes = rd.IsDBNull(5) ? null : rd.GetString(5),
                    Prioridad = rd.GetInt32(6),
                    Notas = rd.IsDBNull(7) ? null : rd.GetString(7),
                    RegionCobertura = rd.IsDBNull(8) ? null : rd.GetString(8),
                    ComunaCobertura = rd.IsDBNull(9) ? null : rd.GetString(9),
                    ComponenteId = rd.GetInt32(10),
                    ComponenteNombre = rd.GetString(11)
                });
            }
            return list;
        }

        public async Task<int?> GetDeviceIdByLabelAsync(string deviceLabel)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            const string sql = @"SELECT dispositivo_id
                                 FROM dispositivo_catalogo
                                 WHERE nombre_dispositivo = @label";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@label", SqlDbType.NVarChar, 80) { Value = deviceLabel });

            var o = await cmd.ExecuteScalarAsync();
            return o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o);
        }

        public async Task<bool> UpdateDetectionImageUrlAsync(int detectionId, string newImageUrl)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            const string sql = @"
                UPDATE dbo.detections
                   SET image_url = @url
                 WHERE detection_id = @id;";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@url", SqlDbType.NVarChar, 400) { Value = newImageUrl });
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = detectionId });

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> InsertDetectionAsync(
            int userId,
            int dispositivoId,
            string imageUrl,
            string? status,
            double? confidence)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            const string sql = @"
        INSERT INTO dbo.detections
            (user_id, dispositivo_id, image_url, detected_at, status, confidence_overall)
        OUTPUT INSERTED.detection_id
        VALUES
            (@uid, @did, @url, SYSUTCDATETIME(), @status, @conf);";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@uid", SqlDbType.Int) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@did", SqlDbType.Int) { Value = dispositivoId });
            cmd.Parameters.Add(new SqlParameter("@url", SqlDbType.NVarChar, 400) { Value = imageUrl });
            cmd.Parameters.Add(new SqlParameter("@status", SqlDbType.NVarChar, 20) { Value = (object?)status ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@conf", SqlDbType.Decimal)
            {
                Precision = 5,
                Scale = 4,
                Value = (object?)confidence ?? DBNull.Value
            });

            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        // Recupera detecciones de un usuario (ordenadas por fecha desc)
        public async Task<List<DetectionInfo>> GetDetectionsByUserAsync(int userId)
        {
            var lista = new List<DetectionInfo>();

            const string sql = @"
SELECT d.detection_id, d.user_id, d.dispositivo_id, dc.nombre_dispositivo, d.image_url, d.detected_at, d.status, d.confidence_overall
FROM dbo.detections d
LEFT JOIN dbo.dispositivo_catalogo dc ON d.dispositivo_id = dc.dispositivo_id
WHERE d.user_id = @uid
ORDER BY d.detected_at DESC;";

            try
            {
                using var cn = new SqlConnection(connectionString);
                await cn.OpenAsync();

                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@uid", userId);

                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var conf = rd.IsDBNull(rd.GetOrdinal("confidence_overall"))
                        ? (double?)null
                        : Convert.ToDouble(rd.GetDecimal(rd.GetOrdinal("confidence_overall")));

                    lista.Add(new DetectionInfo
                    {
                        DetectionId = rd.GetInt32(rd.GetOrdinal("detection_id")),
                        UserId = rd.GetInt32(rd.GetOrdinal("user_id")),
                        DispositivoId = rd.GetInt32(rd.GetOrdinal("dispositivo_id")),
                        DispositivoNombre = rd.IsDBNull(rd.GetOrdinal("nombre_dispositivo")) ? "" : rd.GetString(rd.GetOrdinal("nombre_dispositivo")),
                        ImageUrl = rd.IsDBNull(rd.GetOrdinal("image_url")) ? "" : rd.GetString(rd.GetOrdinal("image_url")),
                        DetectedAt = rd.GetDateTime(rd.GetOrdinal("detected_at")),
                        Status = rd.IsDBNull(rd.GetOrdinal("status")) ? null : rd.GetString(rd.GetOrdinal("status")),
                        Confidence = conf
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error GetDetectionsByUserAsync: {ex.Message}");
            }

            return lista;
        }

        //  Recupera todas las detecciones (administrador)
        public async Task<List<DetectionInfo>> GetAllDetectionsAsync()
        {
            var lista = new List<DetectionInfo>();

            const string sql = @"
SELECT d.detection_id, d.user_id, ISNULL(u.Nombre, '') AS usuario_nombre, d.dispositivo_id, dc.nombre_dispositivo, d.image_url, d.detected_at, d.status, d.confidence_overall
FROM dbo.detections d
LEFT JOIN dbo.dispositivo_catalogo dc ON d.dispositivo_id = dc.dispositivo_id
LEFT JOIN dbo.Usuarios u ON d.user_id = u.Id
ORDER BY d.detected_at DESC;";

            try
            {
                using var cn = new SqlConnection(connectionString);
                await cn.OpenAsync();

                using var cmd = new SqlCommand(sql, cn);

                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var conf = rd.IsDBNull(rd.GetOrdinal("confidence_overall"))
                        ? (double?)null
                        : Convert.ToDouble(rd.GetDecimal(rd.GetOrdinal("confidence_overall")));

                    lista.Add(new DetectionInfo
                    {
                        DetectionId = rd.GetInt32(rd.GetOrdinal("detection_id")),
                        UserId = rd.GetInt32(rd.GetOrdinal("user_id")),
                        UsuarioNombre = rd.IsDBNull(rd.GetOrdinal("usuario_nombre")) ? "" : rd.GetString(rd.GetOrdinal("usuario_nombre")),
                        DispositivoId = rd.GetInt32(rd.GetOrdinal("dispositivo_id")),
                        DispositivoNombre = rd.IsDBNull(rd.GetOrdinal("nombre_dispositivo")) ? "" : rd.GetString(rd.GetOrdinal("nombre_dispositivo")),
                        ImageUrl = rd.IsDBNull(rd.GetOrdinal("image_url")) ? "" : rd.GetString(rd.GetOrdinal("image_url")),
                        DetectedAt = rd.GetDateTime(rd.GetOrdinal("detected_at")),
                        Status = rd.IsDBNull(rd.GetOrdinal("status")) ? null : rd.GetString(rd.GetOrdinal("status")),
                        Confidence = conf
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error GetAllDetectionsAsync: {ex.Message}");
            }

            return lista;
        }

        // Elimina una detección por id
        public async Task<bool> DeleteDetectionAsync(int detectionId)
        {
            try
            {
                using var cn = new SqlConnection(connectionString);
                await cn.OpenAsync();

                const string sql = @"DELETE FROM dbo.detections WHERE detection_id = @id;";
                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", detectionId);

                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error DeleteDetectionAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ComponentInfo>> GetAllComponentsCatalogAsync()
        {
            var lista = new List<ComponentInfo>();

            const string sql = @"
SELECT componente_id, nombre_componente, descripcion_componente
FROM componentes_catalogo
ORDER BY nombre_componente;";

            try
            {
                using var cn = new SqlConnection(connectionString);
                await cn.OpenAsync();

                using var cmd = new SqlCommand(sql, cn);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    lista.Add(new ComponentInfo
                    {
                        Id = rd.GetInt32(0),
                        Nombre = rd.IsDBNull(1) ? "" : rd.GetString(1),
                        Descripcion = rd.IsDBNull(2) ? "" : rd.GetString(2)
                        // Estado/GuidanceUrl quedan vacíos (si necesitas más campos, amplía la consulta)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error GetAllComponentsCatalogAsync: {ex.Message}");
            }

            return lista;
        }


        // -------------------------------
        // SISTEMA DE PUNTOS
        // -------------------------------
        public async Task<bool> RegistrarPuntosAsync(Punto punto)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"INSERT INTO Puntos (UsuarioId, Cantidad, Tipo, Descripcion, Fecha)
                          VALUES (@UsuarioId, @Cantidad, @Tipo, @Descripcion, @Fecha)";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UsuarioId", punto.UsuarioId);
                    command.Parameters.AddWithValue("@Cantidad", punto.Cantidad);
                    command.Parameters.AddWithValue("@Tipo", punto.Tipo);
                    command.Parameters.AddWithValue("@Descripcion", punto.Descripcion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Fecha", punto.Fecha);

                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar puntos: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Punto>> ObtenerHistorialPuntosAsync(int usuarioId)
        {
            var lista = new List<Punto>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"SELECT * FROM Puntos WHERE UsuarioId = @UsuarioId ORDER BY Fecha DESC";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new Punto
                            {
                                Id = (int)reader["Id"],
                                UsuarioId = (int)reader["UsuarioId"],
                                Cantidad = (int)reader["Cantidad"],
                                Tipo = reader["Tipo"].ToString() ?? "",
                                Descripcion = reader["Descripcion"].ToString() ?? "",
                                Fecha = (DateTime)reader["Fecha"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener historial de puntos: {ex.Message}");
            }

            return lista;
        }

        public async Task<int> ObtenerTotalPuntosAsync(int usuarioId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT ISNULL(SUM(Cantidad), 0) FROM Puntos WHERE UsuarioId = @UsuarioId";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);

                    var total = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(total);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener total de puntos: {ex.Message}");
                return 0;
            }
        }
        public async Task AsignarPuntosPorDeteccionesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                    INSERT INTO Puntos (UsuarioId, Cantidad, Tipo, Descripcion)
                    SELECT 
                        d.UsuarioId, 
                        COUNT(d.Id) * 100 AS Cantidad, 
                        'Asignación', 
                        'Puntos otorgados por detecciones'
                    FROM Detections d
                    JOIN Usuarios u ON u.Id = d.UsuarioId
                    WHERE NOT EXISTS (
                        SELECT 1 
                        FROM Puntos p 
                        WHERE p.UsuarioId = d.UsuarioId 
                        AND p.Descripcion LIKE '%detecciones%'
                    )
                    GROUP BY d.UsuarioId;";

                    SqlCommand command = new SqlCommand(query, connection);
                    await command.ExecuteNonQueryAsync();
                }

                Console.WriteLine("Puntos asignados correctamente según detecciones.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al asignar puntos por detecciones: {ex.Message}");
            }
        }
        public async Task AsignarPuntosPorDeteccionUsuarioAsync(int usuarioId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                INSERT INTO Puntos (UsuarioId, Cantidad, Tipo, Descripcion)
                VALUES (@UsuarioId, 100, 'Asignación', 'Puntos por nueva detección');
            ";

                    using var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    await command.ExecuteNonQueryAsync();
                }

                Console.WriteLine($"Puntos asignados al usuario {usuarioId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al asignar puntos al usuario {usuarioId}: {ex.Message}");
            }
        }
        public async Task<int> ObtenerIdUsuarioPorCorreoAsync(string correo)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT Id FROM Usuarios WHERE Correo = @Correo";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Correo", correo);
                var result = await command.ExecuteScalarAsync();
                return result == null ? 0 : Convert.ToInt32(result);
            }
        }

        //Contenido Educativo

        public async Task<List<ContenidoEducativo>> ObtenerContenidoEducativoAsync()
        {
            var contenido = new List<ContenidoEducativo>();

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT Id, Titulo, Descripcion, ImagenUrl, FechaPublicacion, EsPredeterminado FROM ContenidoEducativo ORDER BY FechaPublicacion DESC";
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        contenido.Add(new ContenidoEducativo
                        {
                            Id = reader.GetInt32(0),
                            Titulo = reader.GetString(1),
                            Descripcion = reader.GetString(2),
                            ImagenUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                            FechaPublicacion = reader.GetDateTime(4),
                            EsPredeterminado = reader.GetBoolean(5)
                        });
                    }
                }
            }
            return contenido;
        }

        // Insertar publicación
        public async Task InsertarContenidoEducativoAsync(ContenidoEducativo contenido)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "INSERT INTO ContenidoEducativo (Titulo, Descripcion, ImagenUrl, FechaPublicacion, EsPredeterminado) " +
                               "VALUES (@Titulo, @Descripcion, @ImagenUrl, @FechaPublicacion, @EsPredeterminado)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Titulo", contenido.Titulo);
                    cmd.Parameters.AddWithValue("@Descripcion", contenido.Descripcion);
                    cmd.Parameters.AddWithValue("@ImagenUrl", (object)contenido.ImagenUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaPublicacion", contenido.FechaPublicacion);
                    cmd.Parameters.AddWithValue("@EsPredeterminado", contenido.EsPredeterminado);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        // ELIMINAR CONTENIDO EDUCATIVO
        public async Task EliminarContenidoEducativoAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "DELETE FROM ContenidoEducativo WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ---------- CRUD para compañías ----------

        

        public async Task<List<ComponentOption>> GetAllComponentsAsync()
        {
            var list = new List<ComponentOption>();
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            const string sql = @"SELECT componente_id, nombre_componente
                         FROM dbo.componentes_catalogo
                         WHERE active = 1
                         ORDER BY nombre_componente;";

            using var cmd = new SqlCommand(sql, cn);
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new ComponentOption
                {
                    Id = rd.GetInt32(0),
                    Nombre = rd.GetString(1)
                });
            }
            return list;
        }

        public async Task<List<CompanyRow>> GetCompaniesBasicAsync()
        {
            var list = new List<CompanyRow>();
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            const string sql = @"
                SELECT c.pickup_company_id,
                       c.nombre_compania_domicilio,
                       c.active,
                       ISNULL(cov.cnt,0) AS coberturas,
                       ISNULL(comp.cnt,0) AS componentes
                FROM dbo.compania_domicilio c
                OUTER APPLY (
                   SELECT COUNT(*) AS cnt
                   FROM dbo.domicilio_cobertura dc
                   WHERE dc.pickup_company_id = c.pickup_company_id
                ) cov
                OUTER APPLY (
                   SELECT COUNT(*) AS cnt
                   FROM dbo.compania_componente_mapa m
                   WHERE m.pickup_company_id = c.pickup_company_id
                ) comp
                ORDER BY c.nombre_compania_domicilio;";

            using var cmd = new SqlCommand(sql, cn);
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new CompanyRow
                {
                    PickupCompanyId = rd.GetInt32(0),
                    Nombre = rd.GetString(1),
                    Active = rd.GetBoolean(2),
                    Coberturas = rd.GetInt32(3),
                    Componentes = rd.GetInt32(4)
                });
            }
            return list;
        }

        


        public async Task<CompanyAggregate?> GetCompanyAggregateAsync(int companyId)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();

            // 1) Compañía
            const string sqlCompany = @"
        SELECT pickup_company_id, nombre_compania_domicilio, website, telefono, email, coverage_notes, active
        FROM dbo.compania_domicilio
        WHERE pickup_company_id = @id;";
            using var cmdC = new SqlCommand(sqlCompany, cn);
            cmdC.Parameters.AddWithValue("@id", companyId);
            using var rd = await cmdC.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return null;

            var agg = new CompanyAggregate
            {
                PickupCompanyId = rd.GetInt32(0),
                Nombre = rd.GetString(1),
                Website = rd.IsDBNull(2) ? null : rd.GetString(2),
                Telefono = rd.IsDBNull(3) ? null : rd.GetString(3),
                Email = rd.IsDBNull(4) ? null : rd.GetString(4),
                CoverageNotes = rd.IsDBNull(5) ? null : rd.GetString(5),
                Active = rd.GetBoolean(6)
            };
            rd.Close();

            // 2) Coberturas
            const string sqlCov = @"
        SELECT region_cobertura, comuna_cobertura
        FROM dbo.domicilio_cobertura
        WHERE pickup_company_id = @id
        ORDER BY region_cobertura, comuna_cobertura;";
            using var cmdCov = new SqlCommand(sqlCov, cn);
            cmdCov.Parameters.AddWithValue("@id", companyId);
            using var rd2 = await cmdCov.ExecuteReaderAsync();
            while (await rd2.ReadAsync())
            {
                var reg = rd2.GetString(0);
                var com = rd2.IsDBNull(1) ? null : rd2.GetString(1);
                agg.Coberturas.Add((reg, com));
            }
            rd2.Close();

            // 3) Componentes
            const string sqlComps = @"
        SELECT componente_id
        FROM dbo.compania_componente_mapa
        WHERE pickup_company_id = @id
        ORDER BY prioridad, componente_id;";
            using var cmdComp = new SqlCommand(sqlComps, cn);
            cmdComp.Parameters.AddWithValue("@id", companyId);
            using var rd3 = await cmdComp.ExecuteReaderAsync();
            while (await rd3.ReadAsync())
                agg.ComponentesIds.Add(rd3.GetInt32(0));

            return agg;
        }

        public async Task<int> UpsertCompanyAsync(CompanyAggregate agg)
        {
            if (string.IsNullOrWhiteSpace(agg.Nombre))
                throw new ArgumentException("El nombre es obligatorio.");

            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            try
            {
                int companyId;

                // A) Insert/Update compania_domicilio
                if (agg.PickupCompanyId is null)
                {
                    const string ins = @"
                INSERT INTO dbo.compania_domicilio
                    (nombre_compania_domicilio, website, telefono, email, coverage_notes, active)
                OUTPUT INSERTED.pickup_company_id
                VALUES (@n, @w, @t, @e, @notes, @act);";
                    using var cmd = new SqlCommand(ins, cn, tx);
                    cmd.Parameters.AddWithValue("@n", agg.Nombre);
                    cmd.Parameters.AddWithValue("@w", (object?)agg.Website ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@t", (object?)agg.Telefono ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@e", (object?)agg.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object?)agg.CoverageNotes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@act", agg.Active);
                    companyId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else
                {
                    const string upd = @"
                UPDATE dbo.compania_domicilio
                   SET nombre_compania_domicilio=@n,
                       website=@w, telefono=@t, email=@e,
                       coverage_notes=@notes, active=@act
                 WHERE pickup_company_id=@id;";
                    using var cmd = new SqlCommand(upd, cn, tx);
                    cmd.Parameters.AddWithValue("@n", agg.Nombre);
                    cmd.Parameters.AddWithValue("@w", (object?)agg.Website ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@t", (object?)agg.Telefono ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@e", (object?)agg.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object?)agg.CoverageNotes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@act", agg.Active);
                    cmd.Parameters.AddWithValue("@id", agg.PickupCompanyId!.Value);
                    await cmd.ExecuteNonQueryAsync();
                    companyId = agg.PickupCompanyId.Value;
                }

                // B) Reemplazar coberturas del company (limpio y vuelvo a insertar)
                {
                    const string del = @"DELETE FROM dbo.domicilio_cobertura WHERE pickup_company_id=@id;";
                    using var cmdDel = new SqlCommand(del, cn, tx);
                    cmdDel.Parameters.AddWithValue("@id", companyId);
                    await cmdDel.ExecuteNonQueryAsync();

                    const string ins = @"
                INSERT INTO dbo.domicilio_cobertura
                    (pickup_company_id, region_cobertura, comuna_cobertura, notes)
                VALUES (@id, @r, @c, NULL);";
                    foreach (var (reg, com) in agg.Coberturas.Distinct())
                    {
                        using var cmdIns = new SqlCommand(ins, cn, tx);
                        cmdIns.Parameters.AddWithValue("@id", companyId);
                        cmdIns.Parameters.AddWithValue("@r", reg);
                        cmdIns.Parameters.AddWithValue("@c", (object?)com ?? DBNull.Value);
                        await cmdIns.ExecuteNonQueryAsync();
                    }
                }

                // C) Reemplazar mapping de componentes (limpio y vuelvo a insertar)
                {
                    const string del = @"DELETE FROM dbo.compania_componente_mapa WHERE pickup_company_id=@id;";
                    using var cmdDel = new SqlCommand(del, cn, tx);
                    cmdDel.Parameters.AddWithValue("@id", companyId);
                    await cmdDel.ExecuteNonQueryAsync();

                    const string ins = @"
                INSERT INTO dbo.compania_componente_mapa
                    (pickup_company_id, componente_id, prioridad, notas)
                VALUES (@id, @cid, @prio, NULL);";
                    int prio = 100;
                    foreach (var cid in agg.ComponentesIds.Distinct())
                    {
                        using var cmdIns = new SqlCommand(ins, cn, tx);
                        cmdIns.Parameters.AddWithValue("@id", companyId);
                        cmdIns.Parameters.AddWithValue("@cid", cid);
                        cmdIns.Parameters.AddWithValue("@prio", prio); // puedes variar prio si quieres ordenar
                        await cmdIns.ExecuteNonQueryAsync();
                        prio += 10;
                    }
                }

                await tx.CommitAsync();
                return companyId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteCompanyAsync(int companyId)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            const string sql = @"DELETE FROM dbo.compania_domicilio WHERE pickup_company_id=@id;";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", companyId);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> SetCompanyActiveAsync(int companyId, bool active)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
            const string sql = @"UPDATE dbo.compania_domicilio SET active=@a WHERE pickup_company_id=@id;";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@a", active);
            cmd.Parameters.AddWithValue("@id", companyId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}





