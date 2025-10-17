using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Services
{
    //internal class IImageClassifier
    //{
    //}
    /// <summary>
    /// Contrato para los clasificadores de imágenes.
    /// </summary>
    public interface IImageClassifier : IDisposable
    {
        /// <summary>
        /// Realiza la inferencia sobre una imagen.
        /// </summary>
        /// <param name="imageStream">Stream con la imagen (jpg/png)</param>
        /// <returns>
        /// (Etiqueta principal, probabilidad, top3)
        /// </returns>
        Task<(string label, float prob, List<(string label, float prob)> top3)>
            PredictAsync(Stream imageStream);
    }
}
