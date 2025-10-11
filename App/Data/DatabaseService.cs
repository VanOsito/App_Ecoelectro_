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
    }
}



