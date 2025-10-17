using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using App.Models;
using Microsoft.Data.SqlClient;

namespace App.Data
{
    public class DatabaseService
    {
        private readonly string connectionString = "Server=tcp:ecoelectroserver2025.database.windows.net,1433;Initial Catalog=ecoelectrodb;Persist Security Info=False;User ID=admin_ecoelectro;Password=Reciclaje2025;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public async Task<bool> ProbarConexionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return true; 
                }
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
                    string query = @"INSERT INTO Usuarios 
                                    (Nombre, Correo, Contraseña, RegionUsuario, ComunaUsuario) 
                                    VALUES (@Nombre, @Correo, @Contraseña, @RegionUsuario, @ComunaUsuario)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);
                    cmd.Parameters.AddWithValue("@Correo", usuario.Correo);
                    cmd.Parameters.AddWithValue("@Contraseña", usuario.Contraseña);
                    cmd.Parameters.AddWithValue("@RegionUsuario", usuario.RegionUsuario);
                    cmd.Parameters.AddWithValue("@ComunaUsuario", usuario.ComunaUsuario);
                    cmd.ExecuteNonQuery();
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
                string query = "SELECT COUNT(*) FROM Usuarios WHERE Correo = @Correo AND Contraseña = @Contraseña";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Contraseña", contrasena);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }
        public async Task<List<Usuario>> ObtenerUsuarios()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM Usuarios"; 
                var command = new SqlCommand(query, connection);

                var reader = await command.ExecuteReaderAsync();
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
        }

        public async Task<bool> ActualizarUsuario(Usuario usuario)
        {
            using (var connection = new SqlConnection(connectionString))
            {
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
        }

        public async Task<bool> EliminarUsuario(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "DELETE FROM Usuarios WHERE Id=@Id";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                return await command.ExecuteNonQueryAsync() > 0;
            }
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Nombre FROM Usuarios WHERE Correo = @Correo";
                SqlCommand cmd = new SqlCommand(query, connection);
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
    }
}



