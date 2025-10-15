using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using App.Models;   
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
                var query = "SELECT * FROM Usuarios"; // ajusta el nombre de tu tabla
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
    }
}



