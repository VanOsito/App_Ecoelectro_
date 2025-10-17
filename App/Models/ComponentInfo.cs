namespace App.Models;



    public class ComponentInfo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Estado { get; set; } = "";       // Reciclable / Reutilizable / Vendible / Peligroso / —
        public string Descripcion { get; set; } = "";
        public string? GuidanceUrl { get; set; }
    }
