using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.Services
{
    public sealed class OnnxImageClassifier : IImageClassifier
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly string[] _labels;
        private const int Width = 224, Height = 224, Channels = 3;
        private readonly bool _inputIsNCHW;
        private readonly bool _applyMobilenetV3Preprocess = false; // ponlo en false si tu ONNX ya trae la Lambda adentro
        




        public OnnxImageClassifier()
        {
            // 1️⃣ Cargar modelo desde Resources/Raw
            using var modelStream = FileSystem.OpenAppPackageFileAsync("model.onnx").Result;
            using var ms = new MemoryStream();
            modelStream.CopyTo(ms);
            var modelBytes = ms.ToArray();

            _session = new InferenceSession(modelBytes);
            _inputName = _session.InputMetadata.Keys.First();

            // 2️⃣ Cargar labels.json
            using var labelStream = FileSystem.OpenAppPackageFileAsync("labels.json").Result;
            _labels = JsonSerializer.Deserialize<string[]>(labelStream)!;

            // --
        
            var info = _session.InputMetadata[_inputName];
            var dims = info.Dimensions; // típicamente [1,224,224,3] o [1,3,224,224]
            _inputIsNCHW = (dims.Length == 4 && dims[1] == 3 && dims[2] == 224 && dims[3] == 224);
            var md = _session.InputMetadata[_inputName];
            System.Diagnostics.Debug.WriteLine($"ONNX input: elemType={md.ElementType}, dims=[{string.Join(",", md.Dimensions)}]");

            System.Diagnostics.Debug.WriteLine($"ONNX input name: {_inputName}");
            System.Diagnostics.Debug.WriteLine($"ONNX input shape: [{string.Join(",", dims)}]");
            
            System.Diagnostics.Debug.WriteLine($"Input layout detected: {(_inputIsNCHW ? "NCHW" : "NHWC")}");
        }



        /// <summary>
        /// Ejecuta la inferencia sobre la imagen.
        /// </summary>
        public async Task<(string label, float prob, List<(string label, float prob)> top3)>

            PredictAsync(Stream imageStream)
        {
            
            // Preprocesar imagen a tensor
            var inputTensor = await CreateInputTensorAsync(imageStream);



            // Ejecutar inferencia
            using var results = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(_inputName, inputTensor) });
            var output = results.First().AsEnumerable<float>().ToArray();

            // Aplicar softmax si el modelo no lo tiene incluido
            var probs = Softmax(output);

            // Top-1 y Top-3
            var idx = Enumerable.Range(0, probs.Length).OrderByDescending(i => probs[i]).ToArray();
            var top1 = (_labels[idx[0]], probs[idx[0]]);
            var top3 = idx.Take(3).Select(i => (_labels[i], probs[i])).ToList();

            return (top1.Item1, top1.Item2, top3);
        }

        // 🧮 Softmax
        private static float[] Softmax(float[] z)
        {
            float max = z.Max();
            var exps = z.Select(v => MathF.Exp(v - max)).ToArray();
            float sum = exps.Sum();
            return exps.Select(v => v / sum).ToArray();
        }

        // 🖼️ Convertir imagen a tensor [1,224,224,3] float32 en [-1,1]

        private static SKBitmap Letterbox(SKBitmap src, int targetW, int targetH, SKColor pad)
        {
            float scale = Math.Min((float)targetW / src.Width, (float)targetH / src.Height);
            int nw = (int)(src.Width * scale);
            int nh = (int)(src.Height * scale);

            using var resized = new SKBitmap(nw, nh, src.ColorType, src.AlphaType);
            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
            src.ScalePixels(resized, sampling);

            var canvasBmp = new SKBitmap(targetW, targetH, src.ColorType, src.AlphaType);
            using var canvas = new SKCanvas(canvasBmp);
            canvas.Clear(pad);
            int ox = (targetW - nw) / 2;
            int oy = (targetH - nh) / 2;
            canvas.DrawBitmap(resized, new SKPoint(ox, oy));
            canvas.Flush();
            return canvasBmp;
        }

        private async Task<DenseTensor<float>> CreateInputTensorAsync(Stream src)
        {
            using var mem = new MemoryStream();
            await src.CopyToAsync(mem);
            mem.Position = 0;
            

            using var bmp = SKBitmap.Decode(mem);
            if (bmp is null) throw new InvalidOperationException("No se pudo decodificar la imagen.");

            // 1) Letterbox a 224x224 (evita cortar el objeto)
            using var boxed = Letterbox(bmp, Width, Height, new SKColor(127, 127, 127)); // gris neutro


            // 2) Crear tensor con el layout que el modelo pide
            DenseTensor<float> tensor = _inputIsNCHW
                ? new DenseTensor<float>(new[] { 1, Channels, Height, Width })   // NCHW
                : new DenseTensor<float>(new[] { 1, Height, Width, Channels });   // NHWC

            // 3) Copiar píxeles + normalizar
            if (_inputIsNCHW)
            {
                int rBase = 0 * Height * Width;
                int gBase = 1 * Height * Width;
                int bBase = 2 * Height * Width;

                for (int j = 0; j < Height; j++)
                {
                    for (int i = 0; i < Width; i++)
                    {
                        var c = boxed.GetPixel(i, j);
                        float r = c.Red, g = c.Green, b = c.Blue;
                        if (_applyMobilenetV3Preprocess)
                        {
                            r = (r / 127.5f) - 1f;
                            g = (g / 127.5f) - 1f;
                            b = (b / 127.5f) - 1f;
                        }
                        int idx = j * Width + i;
                        tensor.Buffer.Span[rBase + idx] = r;
                        tensor.Buffer.Span[gBase + idx] = g;
                        tensor.Buffer.Span[bBase + idx] = b;
                    }
                }
            }
            else
            {
                int k = 0;
                for (int j = 0; j < Height; j++)
                {
                    for (int i = 0; i < Width; i++)
                    {
                        var c = boxed.GetPixel(i, j);
                        float r = c.Red, g = c.Green, b = c.Blue;
                        if (_applyMobilenetV3Preprocess)
                        {
                            r = (r / 127.5f) - 1f;
                            g = (g / 127.5f) - 1f;
                            b = (b / 127.5f) - 1f;
                        }
                        tensor.Buffer.Span[k++] = r;
                        tensor.Buffer.Span[k++] = g;
                        tensor.Buffer.Span[k++] = b;
                    }
                }
            }
            return tensor;
        }

        public void Dispose() => _session.Dispose();
    }
}
