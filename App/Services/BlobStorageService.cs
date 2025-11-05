using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.ComponentModel;

namespace App.Services
{
    public class BlobStorageService
    {
        private const string AccountName = "appecoelectro";
        private const string ContainerName = "detections";
        // Mantén el token en código solo temporalmente; lo ideal es que la SAS se genere en el backend.
        private const string SasToken = "sp=racwdli&st=2025-10-31T21:40:27Z&se=2026-10-31T05:31:27Z&spr=https&sv=2024-11-04&sr=c&sig=uptC8AYZcn2fT1NE4melfeFSjRhCehoUrM63AgX4uAU%3D";

        private BlobContainerClient GetContainer()
        {
            var containerUri = new Uri($"https://{AccountName}.blob.core.windows.net/{ContainerName}?{SasToken}");
            return new BlobContainerClient(containerUri);
        }

        public async Task<string> UploadFileAsync(string localPath)
            => await UploadFileAsync(localPath, Path.GetFileName(localPath));

        public async Task<string> UploadFileAsync(string localPath, string blobName)
        {
            var container = GetContainer();

            var blob = container.GetBlobClient(blobName);

            try
            {
                await using var fs = File.OpenRead(localPath);
                await blob.UploadAsync(fs, overwrite: true);
            }
            catch (RequestFailedException rfEx)
            {
                // Propaga la excepción con información útil para depuración
                throw new InvalidOperationException($"Error subiendo blob: {rfEx.Message} (Status: {rfEx.Status}, ErrorCode: {rfEx.ErrorCode})", rfEx);
            }

            // Asegurarse de devolver una URL utilizable (incluyendo la SAS)
            var baseUri = blob.Uri.ToString();
            var sasPart = SasToken;
            if (!string.IsNullOrWhiteSpace(sasPart))
            {
                if (sasPart.StartsWith("?")) sasPart = sasPart.Substring(1);
                return $"{baseUri}?{sasPart}";
            }

            return baseUri;
        }

        // Nuevo: intenta borrar un blob por su URL (si la SAS lo permite)
        public async Task<bool> DeleteBlobAsync(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl)) return false;
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var resp = await blobClient.DeleteIfExistsAsync();
                return resp.Value;
            }
            catch (RequestFailedException ex)
            {
                // Log y continúa (no detener la eliminación DB si no se puede borrar el blob)
                Console.WriteLine($"[BlobStorage] Delete error: {ex.Message} (Status {ex.Status}, Code {ex.ErrorCode})");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BlobStorage] Delete unexpected: {ex.Message}");
                return false;
            }
        }
    }
}

