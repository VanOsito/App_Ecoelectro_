using System;

namespace App.Models
{
    public class DetectionInfo
    {
        public int DetectionId { get; set; }
        public int UserId { get; set; }

        // nombre del usuario que hizo la detección 
        public string UsuarioNombre { get; set; } = "";

        public int DispositivoId { get; set; }
        public string DispositivoNombre { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public DateTime DetectedAt { get; set; }
        public string? Status { get; set; }
        public double? Confidence { get; set; }
    }
}
